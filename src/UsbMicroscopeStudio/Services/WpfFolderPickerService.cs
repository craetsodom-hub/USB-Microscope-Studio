using System.IO;
using Microsoft.Win32;

namespace UsbMicroscopeStudio.Services;

public sealed class WpfFolderPickerService : IFolderPickerService
{
    public string? PickFolder(string initialDirectory, string title = "Select folder")
    {
        var dialog = new OpenFolderDialog
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Select folder" : title,
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
