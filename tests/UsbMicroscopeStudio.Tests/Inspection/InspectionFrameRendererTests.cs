using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class InspectionFrameRendererTests
{
    [Fact]
    public void RenderAnnotatedFrame_UsesNativeFrameDimensions()
    {
        var renderer = new InspectionFrameRenderer();
        var frame = CreateFrame(37, 19);

        var rendered = renderer.RenderAnnotatedFrame(
            frame,
            [
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Angle,
                    StrokeColor = "#D1242F",
                    Points = [new(0.1, 0.8), new(0.5, 0.5), new(0.8, 0.1)]
                }
            ],
            new OverlayOptions(false, false, 64, false));

        Assert.Equal(37, rendered.PixelWidth);
        Assert.Equal(19, rendered.PixelHeight);
    }

    private static BitmapSource CreateFrame(int width, int height)
    {
        var pixels = Enumerable.Repeat<byte>(180, width * height * 4).ToArray();
        var bitmap = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
