using System.IO;
using System.Windows.Media.Imaging;

namespace UsbMicroscopeStudio.Services;

public sealed class SnapshotService : ISnapshotService
{
    public string SaveSnapshot(BitmapSource frame)
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var snapshotDirectory = Path.Combine(picturesPath, "USB Microscope Studio", "Snapshots");
        Directory.CreateDirectory(snapshotDirectory);

        var path = Path.Combine(snapshotDirectory, $"microscope-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));

        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }
}
