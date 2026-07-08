using UsbMicroscopeStudio.Models;

namespace UsbMicroscopeStudio.Services;

public interface ICameraPreviewService : IDisposable
{
    event EventHandler<FrameReadyEventArgs>? FrameReady;

    event EventHandler<string>? StatusChanged;

    bool IsRunning { get; }

    FrameTransformOptions TransformOptions { get; set; }

    Task StartAsync(CameraDevice camera, CameraFormat format, CancellationToken cancellationToken = default);

    Task StopAsync();
}
