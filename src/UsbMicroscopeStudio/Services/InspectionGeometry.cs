using System.Windows;
using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public static class InspectionGeometry
{
    public static InspectionPoint FromViewport(Point point, double width, double height) => new(
        Clamp01(width <= 0 ? 0 : point.X / width),
        Clamp01(height <= 0 ? 0 : point.Y / height));

    public static Point ToViewport(InspectionPoint point, double width, double height) => new(
        Clamp01(point.X) * Math.Max(1, width),
        Clamp01(point.Y) * Math.Max(1, height));

    public static InspectionPoint Translate(InspectionPoint point, double dx, double dy) => new(
        Clamp01(point.X + dx),
        Clamp01(point.Y + dy));

    public static InspectionPoint MirrorHorizontal(InspectionPoint point) => new(Clamp01(1d - point.X), Clamp01(point.Y));

    public static InspectionPoint RotateClockwise(InspectionPoint point, int degrees)
    {
        return NormalizeRotation(degrees) switch
        {
            90 => new InspectionPoint(Clamp01(1d - point.Y), Clamp01(point.X)),
            180 => new InspectionPoint(Clamp01(1d - point.X), Clamp01(1d - point.Y)),
            270 => new InspectionPoint(Clamp01(point.Y), Clamp01(1d - point.X)),
            _ => new InspectionPoint(Clamp01(point.X), Clamp01(point.Y))
        };
    }

    public static double PixelDistance(InspectionPoint start, InspectionPoint end, double width, double height)
    {
        var startPixels = ToViewport(start, width, height);
        var endPixels = ToViewport(end, width, height);
        var dx = endPixels.X - startPixels.X;
        var dy = endPixels.Y - startPixels.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double SegmentAngleDegrees(InspectionPoint start, InspectionPoint end, double width, double height)
    {
        var startPixels = ToViewport(start, width, height);
        var endPixels = ToViewport(end, width, height);
        var angle = Math.Atan2(endPixels.Y - startPixels.Y, endPixels.X - startPixels.X) * 180d / Math.PI;
        return angle < 0 ? angle + 360d : angle;
    }

    public static double ThreePointAngleDegrees(InspectionPoint firstRayEndpoint, InspectionPoint vertex, InspectionPoint secondRayEndpoint, double width, double height)
    {
        var a = ToViewport(firstRayEndpoint, width, height);
        var b = ToViewport(vertex, width, height);
        var c = ToViewport(secondRayEndpoint, width, height);
        var ab = new Vector(a.X - b.X, a.Y - b.Y);
        var cb = new Vector(c.X - b.X, c.Y - b.Y);
        if (ab.Length <= 0 || cb.Length <= 0)
        {
            return 0;
        }

        var dot = (ab.X * cb.X) + (ab.Y * cb.Y);
        var cos = Math.Clamp(dot / (ab.Length * cb.Length), -1d, 1d);
        return Math.Acos(cos) * 180d / Math.PI;
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);

    private static int NormalizeRotation(int value) => ((value % 360) + 360) % 360;
}
