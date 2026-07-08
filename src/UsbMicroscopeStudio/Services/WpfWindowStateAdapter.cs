using System.Windows;

namespace UsbMicroscopeStudio.Services;

public sealed class WpfWindowStateAdapter(Window window) : IWindowStateAdapter
{
    public double Left
    {
        get => window.Left;
        set => window.Left = value;
    }

    public double Top
    {
        get => window.Top;
        set => window.Top = value;
    }

    public double Width
    {
        get => window.Width;
        set => window.Width = value;
    }

    public double Height
    {
        get => window.Height;
        set => window.Height = value;
    }

    public WindowState WindowState
    {
        get => window.WindowState;
        set => window.WindowState = value;
    }

    public WindowStyle WindowStyle
    {
        get => window.WindowStyle;
        set => window.WindowStyle = value;
    }

    public ResizeMode ResizeMode
    {
        get => window.ResizeMode;
        set => window.ResizeMode = value;
    }
}
