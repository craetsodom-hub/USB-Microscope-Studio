namespace UsbMicroscopeStudio.Models.Inspection;

public sealed record InspectionAnnotation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public InspectionTool Tool { get; init; }

    public List<InspectionPoint> Points { get; init; } = [];

    public string StrokeColor { get; init; } = "#2F6FDB";

    public double StrokeThickness { get; init; } = 2;

    public string? Text { get; init; }

    public bool IsMeasurement { get; init; }
}
