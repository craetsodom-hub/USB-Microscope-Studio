using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class CalibrationCalculatorTests
{
    [Fact]
    public void CreateProfile_ComputesUnitsPerPixelFromReferenceLine()
    {
        var calculator = new CalibrationCalculator();

        var profile = calculator.CreateProfile(
            "10 mm rule",
            "camera-a",
            new CameraFormat(1280, 720, 30),
            new InspectionPoint(10, 10),
            new InspectionPoint(110, 10),
            10,
            InspectionUnits.Millimetres);

        Assert.Equal(0.1, profile.UnitsPerPixel, precision: 6);
    }

    [Fact]
    public void MeasureDistance_RequiresCalibrationForRealWorldLength()
    {
        var calculator = new CalibrationCalculator();

        var result = calculator.MeasureDistance(new InspectionPoint(0, 0), new InspectionPoint(3, 4), null);

        Assert.Equal(5, result.PixelLength, precision: 6);
        Assert.Null(result.RealLength);
        Assert.False(result.IsCalibrated);
    }

    [Fact]
    public void MeasureDistance_WithCalibrationReturnsLengthAndAngle()
    {
        var calculator = new CalibrationCalculator();
        var profile = new CalibrationProfile { UnitsPerPixel = 2, Units = InspectionUnits.Micrometres };

        var result = calculator.MeasureDistance(new InspectionPoint(0, 0), new InspectionPoint(0, 10), profile);

        Assert.Equal(20, result.RealLength);
        Assert.Equal(90, result.AngleDegrees);
        Assert.True(result.IsCalibrated);
    }
}
