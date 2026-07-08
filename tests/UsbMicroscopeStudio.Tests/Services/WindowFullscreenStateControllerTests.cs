using System.Windows;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class WindowFullscreenStateControllerTests
{
    [Fact]
    public void SetFullscreen_WhenNormalWindowRestoresExactPreviousState()
    {
        var window = new FakeWindowStateAdapter
        {
            Left = 123,
            Top = 77,
            Width = 1440,
            Height = 910,
            WindowState = WindowState.Normal,
            WindowStyle = WindowStyle.SingleBorderWindow,
            ResizeMode = ResizeMode.CanResizeWithGrip
        };
        var controller = new WindowFullscreenStateController(window);

        controller.SetFullscreen(true);
        window.Left = 0;
        window.Top = 0;
        window.Width = 3840;
        window.Height = 2160;
        controller.SetFullscreen(false);

        Assert.Equal(123, window.Left);
        Assert.Equal(77, window.Top);
        Assert.Equal(1440, window.Width);
        Assert.Equal(910, window.Height);
        Assert.Equal(WindowState.Normal, window.WindowState);
        Assert.Equal(WindowStyle.SingleBorderWindow, window.WindowStyle);
        Assert.Equal(ResizeMode.CanResizeWithGrip, window.ResizeMode);
        Assert.False(controller.IsFullscreen);
    }

    [Fact]
    public void SetFullscreen_WhenMaximizedWindowRestoresExactPreviousState()
    {
        var window = new FakeWindowStateAdapter
        {
            Left = -8,
            Top = -8,
            Width = 1936,
            Height = 1056,
            WindowState = WindowState.Maximized,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.CanMinimize
        };
        var controller = new WindowFullscreenStateController(window);

        controller.SetFullscreen(true);
        window.Left = 0;
        window.Top = 0;
        window.Width = 3840;
        window.Height = 2160;
        controller.SetFullscreen(false);

        Assert.Equal(-8, window.Left);
        Assert.Equal(-8, window.Top);
        Assert.Equal(1936, window.Width);
        Assert.Equal(1056, window.Height);
        Assert.Equal(WindowState.Maximized, window.WindowState);
        Assert.Equal(WindowStyle.ToolWindow, window.WindowStyle);
        Assert.Equal(ResizeMode.CanMinimize, window.ResizeMode);
        Assert.False(controller.IsFullscreen);
    }

    private sealed class FakeWindowStateAdapter : IWindowStateAdapter
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public WindowState WindowState { get; set; }

        public WindowStyle WindowStyle { get; set; }

        public ResizeMode ResizeMode { get; set; }
    }
}
