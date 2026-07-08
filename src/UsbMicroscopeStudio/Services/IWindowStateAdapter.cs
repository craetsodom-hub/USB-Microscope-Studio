using System.Windows;

namespace UsbMicroscopeStudio.Services;

public interface IWindowStateAdapter
{
    double Left { get; set; }

    double Top { get; set; }

    double Width { get; set; }

    double Height { get; set; }

    WindowState WindowState { get; set; }

    WindowStyle WindowStyle { get; set; }

    ResizeMode ResizeMode { get; set; }
}
