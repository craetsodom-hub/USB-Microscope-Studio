namespace UsbMicroscopeStudio.Models.Inspection;

public sealed record InspectionDocument
{
    public string? CleanFramePath { get; init; }

    public string? AnnotatedFramePath { get; init; }

    public string? CameraId { get; init; }

    public string? Format { get; init; }

    public string CalibrationStatus { get; init; } = "Uncalibrated";

    public CalibrationProfile? CalibrationProfile { get; init; }

    public List<InspectionAnnotation> Annotations { get; init; } = [];

    public List<MeasurementResult> Measurements { get; init; } = [];
}
