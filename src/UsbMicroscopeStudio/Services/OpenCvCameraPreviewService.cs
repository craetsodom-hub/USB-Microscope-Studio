using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using UsbMicroscopeStudio.Models;

namespace UsbMicroscopeStudio.Services;

public sealed class OpenCvCameraPreviewService : ICameraPreviewService
{
    private CancellationTokenSource? _previewCancellation;
    private Task? _previewTask;
    private readonly object _syncRoot = new();

    static OpenCvCameraPreviewService()
    {
        var nativePath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native");
        if (Directory.Exists(nativePath))
        {
            SetDllDirectory(nativePath);
        }
    }

    public event EventHandler<FrameReadyEventArgs>? FrameReady;

    public event EventHandler<string>? StatusChanged;

    public bool IsRunning { get; private set; }

    public FrameTransformOptions TransformOptions { get; set; } = FrameTransformOptions.Default;

    public async Task StartAsync(CameraDevice camera, CameraFormat format, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_syncRoot)
        {
            _previewCancellation = linkedCancellation;
            IsRunning = true;
            _previewTask = camera.IsDemo
                ? Task.Run(() => RunDemoLoop(format, linkedCancellation.Token), CancellationToken.None)
                : Task.Run(() => RunCameraLoop(camera, format, linkedCancellation.Token), CancellationToken.None);
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cancellation;
        Task? task;

        lock (_syncRoot)
        {
            cancellation = _previewCancellation;
            task = _previewTask;
            _previewCancellation = null;
            _previewTask = null;
            IsRunning = false;
        }

        if (cancellation is null)
        {
            return;
        }

        try
        {
            cancellation.Cancel();
            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation.Dispose();
            RaiseStatus("Preview stopped");
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private async Task RunCameraLoop(CameraDevice camera, CameraFormat format, CancellationToken cancellationToken)
    {
        try
        {
            using var frame = new Mat();
            using var frameClock = new FrameRateCounter();
            var reconnectDelay = TimeSpan.FromMilliseconds(1200);

            while (!cancellationToken.IsCancellationRequested)
            {
                using var capture = new VideoCapture(camera.Index, VideoCaptureAPIs.DSHOW);
                try
                {
                    capture.Set(VideoCaptureProperties.FrameWidth, format.Width);
                    capture.Set(VideoCaptureProperties.FrameHeight, format.Height);
                    capture.Set(VideoCaptureProperties.Fps, format.FramesPerSecond);
                }
                catch
                {
                    // Some UVC drivers reject one or more properties. Keep attempting a usable stream.
                }

                if (!capture.IsOpened())
                {
                    RaiseStatus("Camera unavailable. Waiting for reconnect...");
                    await Task.Delay(reconnectDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                RaiseStatus($"Live: {camera.Name} at {format.DisplayName}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    bool ok;
                    try
                    {
                        ok = capture.Read(frame);
                    }
                    catch
                    {
                        ok = false;
                    }

                    if (!ok || frame.Empty())
                    {
                        RaiseStatus("Camera disconnected. Reconnecting...");
                        break;
                    }

                    PublishMatFrame(frame, frameClock.Next());

                    var delay = Math.Max(1, (int)Math.Round(1000d / Math.Max(1, format.FramesPerSecond)));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or BadImageFormatException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            RaiseStatus("Hardware preview unavailable. Demo Mode started.");
            await RunDemoLoop(format, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunDemoLoop(CameraFormat format, CancellationToken cancellationToken)
    {
        using var frameClock = new FrameRateCounter();
        var frameIndex = 0;
        var delay = Math.Max(1, (int)Math.Round(1000d / Math.Max(1, format.FramesPerSecond)));

        RaiseStatus($"Demo Mode: synthetic feed at {format.DisplayName}");

        while (!cancellationToken.IsCancellationRequested)
        {
            var frame = CreateDemoFrame(format, frameIndex++);
            PublishBitmapFrame(frame, frameClock.Next());
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private void PublishMatFrame(Mat source, double fps)
    {
        using var transformed = ApplyTransforms(source);
        var bitmap = transformed.ToBitmapSource();
        bitmap.Freeze();
        PublishBitmapFrame(bitmap, fps);
    }

    private void PublishBitmapFrame(BitmapSource bitmap, double fps)
    {
        if (bitmap.CanFreeze && !bitmap.IsFrozen)
        {
            bitmap.Freeze();
        }

        FrameReady?.Invoke(this, new FrameReadyEventArgs(bitmap, fps));
    }

    private Mat ApplyTransforms(Mat source)
    {
        var options = TransformOptions;
        var transformed = source.Clone();

        if (options.MirrorHorizontal)
        {
            Cv2.Flip(transformed, transformed, FlipMode.Y);
        }

        switch (((options.RotationDegrees % 360) + 360) % 360)
        {
            case 90:
                Cv2.Rotate(transformed, transformed, RotateFlags.Rotate90Clockwise);
                break;
            case 180:
                Cv2.Rotate(transformed, transformed, RotateFlags.Rotate180);
                break;
            case 270:
                Cv2.Rotate(transformed, transformed, RotateFlags.Rotate90Counterclockwise);
                break;
        }

        return transformed;
    }

    private BitmapSource CreateDemoFrame(CameraFormat format, int frameIndex)
    {
        var width = Math.Max(320, format.Width);
        var height = Math.Max(240, format.Height);
        var stride = width * 4;
        var pixels = new byte[height * stride];
        var centerX = width / 2d;
        var centerY = height / 2d;
        var radius = Math.Min(width, height) / 3.35d;
        var sweepAngle = frameIndex * 4 * Math.PI / 180d;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var index = y * stride + x * 4;

                var shade = (byte)(34 + Math.Min(34, distance / Math.Max(width, height) * 80));
                pixels[index] = (byte)(shade + 8);
                pixels[index + 1] = (byte)(shade + 5);
                pixels[index + 2] = shade;
                pixels[index + 3] = 255;

                if (Math.Abs(distance - radius) < 3 || Math.Abs(distance - radius * 0.55) < 2)
                {
                    pixels[index] = 120;
                    pixels[index + 1] = 188;
                    pixels[index + 2] = 108;
                }

                if (distance < radius && (x % Math.Max(24, width / 32) == 0 || y % Math.Max(24, height / 24) == 0))
                {
                    pixels[index] = 78;
                    pixels[index + 1] = 86;
                    pixels[index + 2] = 94;
                }

                var lineDistance = Math.Abs(dx * Math.Sin(sweepAngle) - dy * Math.Cos(sweepAngle));
                var projection = dx * Math.Cos(sweepAngle) + dy * Math.Sin(sweepAngle);
                if (projection > 0 && projection < radius && lineDistance < 3)
                {
                    pixels[index] = 230;
                    pixels[index + 1] = 210;
                    pixels[index + 2] = 72;
                }
            }
        }

        var transformed = ApplyBitmapTransforms(pixels, width, height);
        var bitmap = BitmapSource.Create(transformed.Width, transformed.Height, 96, 96, PixelFormats.Bgra32, null, transformed.Pixels, transformed.Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private (byte[] Pixels, int Width, int Height) ApplyBitmapTransforms(byte[] source, int width, int height)
    {
        var options = TransformOptions;
        var rotation = ((options.RotationDegrees % 360) + 360) % 360;
        var outputWidth = rotation is 90 or 270 ? height : width;
        var outputHeight = rotation is 90 or 270 ? width : height;
        var output = new byte[outputWidth * outputHeight * 4];

        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                var (sourceX, sourceY) = rotation switch
                {
                    90 => (y, height - 1 - x),
                    180 => (width - 1 - x, height - 1 - y),
                    270 => (width - 1 - y, x),
                    _ => (x, y)
                };

                if (options.MirrorHorizontal)
                {
                    sourceX = width - 1 - sourceX;
                }

                var sourceIndex = (sourceY * width + sourceX) * 4;
                var outputIndex = (y * outputWidth + x) * 4;
                Buffer.BlockCopy(source, sourceIndex, output, outputIndex, 4);
            }
        }

        return (output, outputWidth, outputHeight);
    }

    private void RaiseStatus(string status) => StatusChanged?.Invoke(this, status);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string lpPathName);

    private sealed class FrameRateCounter : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _frames;
        private double _current;

        public double Next()
        {
            _frames++;
            if (_stopwatch.ElapsedMilliseconds >= 500)
            {
                _current = _frames * 1000d / _stopwatch.ElapsedMilliseconds;
                _frames = 0;
                _stopwatch.Restart();
            }

            return _current;
        }

        public void Dispose() => _stopwatch.Stop();
    }
}
