namespace UsbMicroscopeStudio.Services;

public interface IFolderPickerService
{
    string? PickFolder(string initialDirectory, string title = "Select folder");
}
