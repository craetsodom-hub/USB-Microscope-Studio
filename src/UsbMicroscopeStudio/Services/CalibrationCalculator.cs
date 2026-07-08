using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class CalibrationCalculator
{
    public CalibrationProfile CreateProfile(
        string name,
        string cameraId,
        Models.CameraFormat format,
        InspectionPoint start,
        InspectionPoint end,
        double knownLength,
        InspectionUnits units)
    {
        var pixels = PixelDistance(start, end);
        if (pixels <= 0)
        {
            throw new InvalidOperationException("Calibration reference line must have a measurable pixel length.");
        }

        if (knownLength <= 0)
        {
            throw new InvalidOperationException("Known calibration length must be greater than zero.");
        }

        return new CalibrationProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Default" : name,
            CameraId = cameraId,
            Format = format,
            Units = units,
            UnitsPerPixel = knownLength / pixels
        };
    }

    public CalibrationProfile CreateProfile(
        string name,
        string cameraId,
        Models.CameraFormat format,
        InspectionPoint start,
        InspectionPoint end,
        double frameWidth,
        double frameHeight,
        double knownLength,
        InspectionUnits units)
    {
        var pixels = InspectionGeometry.PixelDistance(start, end, frameWidth, frameHeight);
        if (pixels <= 0)
        {
            throw new InvalidOperationException("Calibration reference line must have a measurable pixel length.");
        }

        if (knownLength <= 0)
        {
            throw new InvalidOperationException("Known calibration length must be greater than zero.");
        }

        return new CalibrationProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Default" : name,
            CameraId = cameraId,
            Format = format,
            Units = units,
            UnitsPerPixel = knownLength / pixels
        };
    }

    public MeasurementResult MeasureDistance(InspectionPoint start, InspectionPoint end, CalibrationProfile? profile)
    {
        var pixels = PixelDistance(start, end);
        return new MeasurementResult(
            pixels,
            profile is null ? null : pixels * profile.UnitsPerPixel,
            AngleDegrees(start, end),
            profile?.Units ?? InspectionUnits.Millimetres,
            profile is not null);
    }

    public MeasurementResult MeasureDistance(
        InspectionPoint start,
        InspectionPoint end,
        double frameWidth,
        double frameHeight,
        CalibrationProfile? profile)
    {
        var pixels = InspectionGeometry.PixelDistance(start, end, frameWidth, frameHeight);
        return new MeasurementResult(
            pixels,
            profile is null ? null : pixels * profile.UnitsPerPixel,
            InspectionGeometry.SegmentAngleDegrees(start, end, frameWidth, frameHeight),
            profile?.Units ?? InspectionUnits.Millimetres,
            profile is not null);
    }

    public double AngleDegrees(InspectionPoint start, InspectionPoint end)
    {
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X) * 180d / Math.PI;
        return angle < 0 ? angle + 360d : angle;
    }

    public static double PixelDistance(InspectionPoint start, InspectionPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
