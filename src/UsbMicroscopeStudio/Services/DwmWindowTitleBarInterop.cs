using System.Runtime.InteropServices;

namespace UsbMicroscopeStudio.Services;

public sealed class DwmWindowTitleBarInterop : IWindowTitleBarInterop
{
    public int SetWindowAttribute(nint windowHandle, int attribute, int value)
    {
        try
        {
            return DwmSetWindowAttribute(windowHandle, attribute, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            return -1;
        }
        catch (EntryPointNotFoundException)
        {
            return -1;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint windowHandle, int attribute, ref int value, int valueSize);
}
