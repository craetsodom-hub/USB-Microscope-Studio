namespace UsbMicroscopeStudio.Services;

public interface IWindowTitleBarInterop
{
    int SetWindowAttribute(nint windowHandle, int attribute, int value);
}
