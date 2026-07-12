namespace UsbMicroscopeStudio.Services;

public sealed class DarkTitleBarController(IWindowTitleBarInterop interop)
{
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeLegacy = 19;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    // COLORREF uses 0x00BBGGRR rather than the usual RGB notation.
    private const int GraphiteCaption = 0x001A130D;
    private const int GraphiteBorder = 0x00433626;
    private const int LightCaptionText = 0x00EEE8DD;

    public bool Apply(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        var darkModeApplied = interop.SetWindowAttribute(windowHandle, UseImmersiveDarkMode, 1) == 0;
        if (!darkModeApplied)
        {
            darkModeApplied = interop.SetWindowAttribute(windowHandle, UseImmersiveDarkModeLegacy, 1) == 0;
        }

        if (!darkModeApplied)
        {
            return false;
        }

        interop.SetWindowAttribute(windowHandle, CaptionColor, GraphiteCaption);
        interop.SetWindowAttribute(windowHandle, TextColor, LightCaptionText);
        interop.SetWindowAttribute(windowHandle, BorderColor, GraphiteBorder);
        return true;
    }
}
