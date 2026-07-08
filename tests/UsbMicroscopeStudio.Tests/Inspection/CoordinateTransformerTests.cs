using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class CoordinateTransformerTests
{
    [Fact]
    public void Transform_MirrorsAndRotatesPoint()
    {
        var transformer = new CoordinateTransformer();

        var result = transformer.Transform(new InspectionPoint(10, 20), width: 100, height: 50, rotationDegrees: 90, mirrorHorizontal: true);

        Assert.Equal(30, result.X);
        Assert.Equal(90, result.Y);
    }

    [Fact]
    public void Transform_RotatesTwoHundredSeventyDegrees()
    {
        var transformer = new CoordinateTransformer();

        var result = transformer.Transform(new InspectionPoint(10, 20), width: 100, height: 50, rotationDegrees: 270, mirrorHorizontal: false);

        Assert.Equal(20, result.X);
        Assert.Equal(90, result.Y);
    }
}
