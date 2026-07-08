namespace UsbMicroscopeStudio.Models.Inspection;

public sealed record OverlayOptions(
    bool ShowCrosshair,
    bool ShowGrid,
    double GridSpacingPixels,
    bool ShowRulers);
