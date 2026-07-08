using UsbMicroscopeStudio.Models;

namespace UsbMicroscopeStudio.Services;

public interface ICameraCatalog
{
    Task<IReadOnlyList<CameraDevice>> GetCamerasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CameraFormat>> GetFormatsAsync(CameraDevice camera, CancellationToken cancellationToken = default);
}
