using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Models.Sessions;

public sealed record InspectionSessionDocument
{
    public string SessionName { get; init; } = "Untitled inspection";

    public string? CustomerName { get; init; }

    public string? DeviceModel { get; init; }

    public string? SerialAssetTag { get; init; }

    public string? TechnicianName { get; init; }

    public string? JobOrderNumber { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset InspectionDateTime { get; init; } = DateTimeOffset.Now;

    public string? WorkspaceFolderPath { get; init; }

    public string? SessionFolderPath { get; init; }

    public string? SessionJsonPath { get; init; }

    public string? CleanFramePath { get; init; }

    public string? AnnotatedFramePath { get; init; }

    public string? InspectionJsonSidecarPath { get; init; }

    public string CalibrationStatus { get; init; } = "Uncalibrated";

    public string? CalibrationProfileKey { get; init; }

    public CalibrationProfile? CalibrationProfile { get; init; }

    public List<InspectionAnnotation> Annotations { get; init; } = [];

    public List<MeasurementResult> Measurements { get; init; } = [];
}
