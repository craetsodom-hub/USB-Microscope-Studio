using System.Windows;

namespace UsbMicroscopeStudio.Services;

public sealed class WindowFullscreenStateController(IWindowStateAdapter window)
{
    private WindowSnapshot? _previousState;

    public bool IsFullscreen { get; private set; }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen == IsFullscreen)
        {
            return;
        }

        if (isFullscreen)
        {
            _previousState = WindowSnapshot.Capture(window);
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowState = WindowState.Maximized;
            IsFullscreen = true;
            return;
        }

        if (_previousState is not null)
        {
            _previousState.ApplyTo(window);
        }

        _previousState = null;
        IsFullscreen = false;
    }

    private sealed record WindowSnapshot(
        double Left,
        double Top,
        double Width,
        double Height,
        WindowState WindowState,
        WindowStyle WindowStyle,
        ResizeMode ResizeMode)
    {
        public static WindowSnapshot Capture(IWindowStateAdapter window) =>
            new(window.Left, window.Top, window.Width, window.Height, window.WindowState, window.WindowStyle, window.ResizeMode);

        public void ApplyTo(IWindowStateAdapter window)
        {
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle;
            window.ResizeMode = ResizeMode;
            window.Left = Left;
            window.Top = Top;
            window.Width = Width;
            window.Height = Height;
            window.WindowState = WindowState;
        }
    }
}
