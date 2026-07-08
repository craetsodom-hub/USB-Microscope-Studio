namespace UsbMicroscopeStudio.Models.Inspection;

public sealed record MeasurementResult(
    double PixelLength,
    double? RealLength,
    double? AngleDegrees,
    InspectionUnits Units,
    bool IsCalibrated);
