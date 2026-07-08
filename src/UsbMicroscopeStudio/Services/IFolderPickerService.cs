namespace UsbMicroscopeStudio.Services;

public interface IFolderPickerService
{
    string? PickFolder(string initialDirectory);
}
