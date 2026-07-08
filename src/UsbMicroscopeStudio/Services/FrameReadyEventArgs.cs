using System.Windows.Media.Imaging;

namespace UsbMicroscopeStudio.Services;

public sealed class FrameReadyEventArgs(BitmapSource frame, double framesPerSecond) : EventArgs
{
    public BitmapSource Frame { get; } = frame;

    public double FramesPerSecond { get; } = framesPerSecond;
}
