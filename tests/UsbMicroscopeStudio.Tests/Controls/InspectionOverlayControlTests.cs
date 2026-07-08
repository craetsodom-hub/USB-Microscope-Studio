using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UsbMicroscopeStudio.Controls;

namespace UsbMicroscopeStudio.Tests.Controls;

public sealed class InspectionOverlayControlTests
{
    [Fact]
    public void OverlayReceivesHitTestsAcrossFullPreviewBounds()
    {
        RunOnStaThread(() =>
        {
            var host = new Grid
            {
                Width = 640,
                Height = 480
            };
            var image = new Image
            {
                Width = 640,
                Height = 480
            };
            var overlay = new InspectionOverlayControl
            {
                Width = 640,
                Height = 480
            };

            host.Children.Add(image);
            host.Children.Add(overlay);
            host.Measure(new Size(640, 480));
            host.Arrange(new Rect(0, 0, 640, 480));
            host.UpdateLayout();

            Assert.Same(overlay, VisualTreeHelper.HitTest(host, new Point(320, 240))?.VisualHit);
            Assert.Same(overlay, VisualTreeHelper.HitTest(host, new Point(4, 4))?.VisualHit);
            Assert.Same(overlay, VisualTreeHelper.HitTest(host, new Point(636, 476))?.VisualHit);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
