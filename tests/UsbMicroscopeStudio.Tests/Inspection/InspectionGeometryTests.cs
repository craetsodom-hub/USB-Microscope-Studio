using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class InspectionGeometryTests
{
    [Fact]
    public void ThreePointAngleDegrees_UsesEndpointVertexEndpoint()
    {
        var angle = InspectionGeometry.ThreePointAngleDegrees(
            new InspectionPoint(0.25, 0.5),
            new InspectionPoint(0.5, 0.5),
            new InspectionPoint(0.5, 0.25),
            400,
            400);

        Assert.Equal(90, angle, precision: 3);
    }

    [Fact]
    public void NormalizedPointsMapToChangedFrameDimensions()
    {
        var first = InspectionGeometry.ToViewport(new InspectionPoint(0.25, 0.5), 640, 480);
        var second = InspectionGeometry.ToViewport(new InspectionPoint(0.25, 0.5), 1280, 720);

        Assert.Equal(160, first.X);
        Assert.Equal(240, first.Y);
        Assert.Equal(320, second.X);
        Assert.Equal(360, second.Y);
    }
}
