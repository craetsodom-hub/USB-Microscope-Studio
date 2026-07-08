namespace UsbMicroscopeStudio.Models;

public sealed record CameraFormat(int Width, int Height, double FramesPerSecond, string PixelFormat = "MJPEG")
{
    public string DisplayName => $"{Width} x {Height} @ {FramesPerSecond:0.#} FPS";
}
