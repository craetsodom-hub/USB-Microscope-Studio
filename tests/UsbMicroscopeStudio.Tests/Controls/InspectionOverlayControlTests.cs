using System.Runtime.ExceptionServices;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UsbMicroscopeStudio.Controls;
using UsbMicroscopeStudio.Models.Inspection;

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

    [Fact]
    public void PremiumPreviewLayout_HitTestsOverlayThroughViewbox()
    {
        RunOnStaThread(() =>
        {
            var host = new Grid
            {
                Width = 960,
                Height = 540,
                ClipToBounds = true
            };
            var viewbox = new Viewbox { Stretch = Stretch.Uniform };
            var frame = new Grid
            {
                Width = 1280,
                Height = 720
            };
            var image = new Image
            {
                Width = 1280,
                Height = 720,
                IsHitTestVisible = false
            };
            var overlay = new InspectionOverlayControl
            {
                Width = 1280,
                Height = 720
            };

            frame.Children.Add(image);
            frame.Children.Add(overlay);
            viewbox.Child = frame;
            host.Children.Add(viewbox);
            var window = new Window
            {
                Content = host,
                Width = 960,
                Height = 540,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false
            };
            window.Show();
            host.UpdateLayout();

            try
            {
                Assert.Same(overlay, VisualTreeHelper.HitTest(host, new Point(480, 270))?.VisualHit);
                Assert.True(overlay.InputHostCountForTesting >= 2);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void TextAndAngleTools_CreateVisibleAnnotationsFromPreviewClicks()
    {
        RunOnStaThread(() =>
        {
            var annotations = new ObservableCollection<InspectionAnnotation>();
            var overlay = new InspectionOverlayControl
            {
                Width = 1280,
                Height = 720,
                Annotations = annotations,
                StrokeColor = "#46A6FF",
                StrokeThickness = 3,
                TextPromptOverride = _ => "QA note"
            };
            overlay.Measure(new Size(1280, 720));
            overlay.Arrange(new Rect(0, 0, 1280, 720));
            overlay.UpdateLayout();

            overlay.CurrentTool = InspectionTool.Text;
            overlay.HandlePreviewPointForTesting(new Point(512, 288));

            var text = Assert.Single(annotations);
            Assert.Equal(InspectionTool.Text, text.Tool);
            Assert.Equal("QA note", text.Text);
            Assert.Equal(new InspectionPoint(0.4, 0.4), text.Points.Single());

            overlay.CurrentTool = InspectionTool.Angle;
            overlay.HandlePreviewPointForTesting(new Point(384, 396));
            overlay.HandlePreviewPointForTesting(new Point(640, 324));
            overlay.HandlePreviewPointForTesting(new Point(896, 432));

            var angle = annotations.Single(annotation => annotation.Tool == InspectionTool.Angle);
            Assert.Equal(3, angle.Points.Count);
            Assert.True(angle.IsMeasurement);
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
