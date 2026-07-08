using UsbMicroscopeStudio.Models;

namespace UsbMicroscopeStudio.Models.Inspection;

public sealed record CalibrationProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "Default";

    public string CameraId { get; init; } = string.Empty;

    public CameraFormat Format { get; init; } = new(0, 0, 0);

    public double UnitsPerPixel { get; init; }

    public InspectionUnits Units { get; init; } = InspectionUnits.Millimetres;

    public string ProfileKey => BuildKey(CameraId, Format, Name);

    public static string BuildKey(string cameraId, CameraFormat format, string? name) =>
        $"{cameraId}|{format.Width}x{format.Height}@{format.FramesPerSecond:0.###}|{name ?? string.Empty}";
}
