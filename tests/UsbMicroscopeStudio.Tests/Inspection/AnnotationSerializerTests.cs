using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class AnnotationSerializerTests
{
    [Fact]
    public void SerializeRoundTrip_PreservesAnnotationsAndMeasurements()
    {
        var serializer = new AnnotationSerializer();
        var document = new InspectionDocument
        {
            CalibrationStatus = "Calibrated",
            Annotations =
            [
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Arrow,
                    StrokeColor = "#D1242F",
                    StrokeThickness = 3,
                    Points = [new(1, 2), new(3, 4)]
                },
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Text,
                    Text = "Connector pin 1",
                    Points = [new(0.25, 0.5)]
                }
            ],
            Measurements = [new MeasurementResult(5, 0.5, 45, InspectionUnits.Millimetres, true)]
        };

        var restored = serializer.Deserialize(serializer.Serialize(document));

        Assert.Equal("Calibrated", restored.CalibrationStatus);
        Assert.Equal(2, restored.Annotations.Count);
        Assert.Equal(InspectionTool.Arrow, restored.Annotations[0].Tool);
        Assert.Equal("Connector pin 1", restored.Annotations[1].Text);
        Assert.Single(restored.Measurements);
        Assert.Equal(0.5, restored.Measurements[0].RealLength);
    }
}
