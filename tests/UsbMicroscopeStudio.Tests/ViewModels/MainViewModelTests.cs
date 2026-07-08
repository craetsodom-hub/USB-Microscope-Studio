using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Services;
using UsbMicroscopeStudio.ViewModels;

namespace UsbMicroscopeStudio.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshCamerasAsync_SelectsHardwareCameraAndLoadsFormats()
    {
        var catalog = new FakeCameraCatalog(
            [new CameraDevice("usb://camera-a", "Bench Camera", 0), new CameraDevice("demo://microscope", "Demo", -1, true)],
            [new CameraFormat(1280, 720, 30)]);
        using var viewModel = CreateViewModel(catalog);

        await viewModel.RefreshCamerasAsync();

        Assert.Equal("Bench Camera", viewModel.SelectedCamera?.Name);
        Assert.Equal("1280 x 720 @ 30 FPS", viewModel.SelectedFormat?.DisplayName);
    }

    [Fact]
    public async Task StartPreviewAsync_StartsSelectedCamera()
    {
        var camera = new CameraDevice("demo://microscope", "Demo", -1, true);
        var format = new CameraFormat(640, 480, 30);
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(new FakeCameraCatalog([camera], [format]), preview);

        await viewModel.RefreshCamerasAsync();
        await viewModel.StartPreviewAsync();

        Assert.True(viewModel.IsPreviewing);
        Assert.Equal(camera, preview.StartedCamera);
        Assert.Equal(format, preview.StartedFormat);
    }

    [Fact]
    public void FreezeKeepsExistingFrame()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);
        var firstFrame = CreateFrame();
        var secondFrame = CreateFrame();

        preview.PublishFrame(firstFrame);
        viewModel.IsFrozen = true;
        preview.PublishFrame(secondFrame);

        Assert.Same(firstFrame, viewModel.PreviewFrame);
    }

    [Fact]
    public void RotateAndMirrorUpdatePreviewTransforms()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);

        viewModel.RotateRight();
        viewModel.ToggleMirror();

        Assert.Equal(90, preview.TransformOptions.RotationDegrees);
        Assert.True(preview.TransformOptions.MirrorHorizontal);
    }

    [Fact]
    public void SnapshotSavesCurrentFrame()
    {
        var preview = new FakePreviewService();
        var snapshots = new FakeSnapshotService();
        using var viewModel = CreateViewModel(previewService: preview, snapshotService: snapshots);
        var frame = CreateFrame();

        preview.PublishFrame(frame);
        viewModel.Snapshot();

        Assert.Equal("C:\\Snapshots\\frame.png", viewModel.LastSnapshotPath);
        Assert.Same(frame, snapshots.SavedFrame);
    }

    [Fact]
    public void ZoomCommandsClampToSupportedRange()
    {
        using var viewModel = CreateViewModel();

        for (var i = 0; i < 50; i++)
        {
            viewModel.ZoomOut();
        }

        Assert.Equal(0.25, viewModel.ZoomLevel);

        for (var i = 0; i < 60; i++)
        {
            viewModel.ZoomIn();
        }

        Assert.Equal(4.0, viewModel.ZoomLevel);
    }

    private static MainViewModel CreateViewModel(
        FakeCameraCatalog? catalog = null,
        FakePreviewService? previewService = null,
        FakeSnapshotService? snapshotService = null)
    {
        return new MainViewModel(
            catalog ?? new FakeCameraCatalog([new CameraDevice("demo://microscope", "Demo", -1, true)], [new CameraFormat(640, 480, 30)]),
            previewService ?? new FakePreviewService(),
            snapshotService ?? new FakeSnapshotService(),
            new ImmediateDispatcher());
    }

    private static BitmapSource CreateFrame()
    {
        var pixels = new byte[] { 10, 20, 30, 255 };
        var bitmap = BitmapSource.Create(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, 4);
        bitmap.Freeze();
        return bitmap;
    }

    private sealed class FakeCameraCatalog(IReadOnlyList<CameraDevice> cameras, IReadOnlyList<CameraFormat> formats) : ICameraCatalog
    {
        public Task<IReadOnlyList<CameraDevice>> GetCamerasAsync(CancellationToken cancellationToken = default) => Task.FromResult(cameras);

        public Task<IReadOnlyList<CameraFormat>> GetFormatsAsync(CameraDevice camera, CancellationToken cancellationToken = default) => Task.FromResult(formats);
    }

    private sealed class FakePreviewService : ICameraPreviewService
    {
        public event EventHandler<FrameReadyEventArgs>? FrameReady;

        public event EventHandler<string>? StatusChanged;

        public bool IsRunning { get; private set; }

        public FrameTransformOptions TransformOptions { get; set; } = FrameTransformOptions.Default;

        public CameraDevice? StartedCamera { get; private set; }

        public CameraFormat? StartedFormat { get; private set; }

        public Task StartAsync(CameraDevice camera, CameraFormat format, CancellationToken cancellationToken = default)
        {
            StartedCamera = camera;
            StartedFormat = format;
            IsRunning = true;
            StatusChanged?.Invoke(this, "Started");
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        public void PublishFrame(BitmapSource frame) => FrameReady?.Invoke(this, new FrameReadyEventArgs(frame, 30));

        public void Dispose()
        {
        }
    }

    private sealed class FakeSnapshotService : ISnapshotService
    {
        public BitmapSource? SavedFrame { get; private set; }

        public string SaveSnapshot(BitmapSource frame)
        {
            SavedFrame = frame;
            return "C:\\Snapshots\\frame.png";
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Invoke(Action action) => action();
    }
}
