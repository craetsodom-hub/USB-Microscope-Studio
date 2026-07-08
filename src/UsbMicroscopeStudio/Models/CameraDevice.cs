namespace UsbMicroscopeStudio.Models;

public sealed record CameraDevice(string Id, string Name, int Index, bool IsDemo = false)
{
    public string DisplayName => IsDemo ? $"{Name} (Demo)" : Name;
}
