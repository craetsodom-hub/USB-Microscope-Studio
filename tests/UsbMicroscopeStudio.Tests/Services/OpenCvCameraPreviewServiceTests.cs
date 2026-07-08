using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class OpenCvCameraPreviewServiceTests
{
    [Fact]
    public async Task StartAsync_DemoCamera_PublishesFrames()
    {
        using var service = new OpenCvCameraPreviewService();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var frameReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        service.FrameReady += (_, args) =>
        {
            if (args.Frame.PixelWidth > 0 && args.Frame.PixelHeight > 0)
            {
                frameReceived.TrySetResult();
            }
        };

        await service.StartAsync(new CameraDevice("demo://microscope", "Demo", -1, true), new CameraFormat(640, 480, 30), timeout.Token);
        await frameReceived.Task.WaitAsync(timeout.Token);
        await service.StopAsync();
    }
}
