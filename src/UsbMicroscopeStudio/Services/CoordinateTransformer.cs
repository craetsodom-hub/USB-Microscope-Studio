using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class CoordinateTransformer
{
    public InspectionPoint Transform(
        InspectionPoint point,
        double width,
        double height,
        int rotationDegrees,
        bool mirrorHorizontal)
    {
        var transformed = mirrorHorizontal
            ? new InspectionPoint(width - point.X, point.Y)
            : point;

        return NormalizeRotation(rotationDegrees) switch
        {
            90 => new InspectionPoint(height - transformed.Y, transformed.X),
            180 => new InspectionPoint(width - transformed.X, height - transformed.Y),
            270 => new InspectionPoint(transformed.Y, width - transformed.X),
            _ => transformed
        };
    }

    private static int NormalizeRotation(int rotationDegrees) => ((rotationDegrees % 360) + 360) % 360;
}
