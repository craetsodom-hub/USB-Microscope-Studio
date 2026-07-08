using System.IO;
using System.Windows.Media.Imaging;

namespace UsbMicroscopeStudio.Services;

public sealed class SnapshotService : ISnapshotService
{
    private readonly Func<DateTimeOffset> _clock;

    public SnapshotService()
        : this(() => DateTimeOffset.Now)
    {
    }

    public SnapshotService(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public string DefaultSnapshotDirectory
    {
        get
        {
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            return Path.Combine(picturesPath, "USB Microscope Studio", "Snapshots");
        }
    }

    public string SaveSnapshot(BitmapSource frame, string snapshotDirectory)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));

        var resolvedSnapshotDirectory = ResolveSnapshotDirectory(snapshotDirectory);
        var timestamp = _clock().ToString("yyyyMMdd-HHmmss");

        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var suffix = attempt == 0 ? string.Empty : $"-{attempt:000}";
            var path = Path.Combine(resolvedSnapshotDirectory, $"microscope-{timestamp}{suffix}.png");
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                encoder.Save(stream);
                return path;
            }
            catch (IOException) when (File.Exists(path))
            {
                continue;
            }
        }

        var fallbackPath = Path.Combine(resolvedSnapshotDirectory, $"microscope-{timestamp}-{Guid.NewGuid():N}.png");
        using (var stream = new FileStream(fallbackPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            encoder.Save(stream);
        }

        return fallbackPath;
    }

    private string ResolveSnapshotDirectory(string snapshotDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(snapshotDirectory))
            {
                throw new DirectoryNotFoundException("Snapshot directory is empty.");
            }

            Directory.CreateDirectory(snapshotDirectory);
            return snapshotDirectory;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException or DirectoryNotFoundException)
        {
            var fallbackDirectory = Path.Combine(Path.GetTempPath(), "USB Microscope Studio", "Snapshots");
            Directory.CreateDirectory(fallbackDirectory);
            return fallbackDirectory;
        }
    }
}
