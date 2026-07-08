namespace UsbMicroscopeStudio.Models;

public sealed record FrameTransformOptions(bool MirrorHorizontal, int RotationDegrees)
{
    public static FrameTransformOptions Default { get; } = new(false, 0);
}
