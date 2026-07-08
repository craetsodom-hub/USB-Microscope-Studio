using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class SnapshotServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveSnapshot_WhenFilenameCollides_CreatesUniqueFile()
    {
        var fixedTime = new DateTimeOffset(2026, 7, 8, 18, 45, 0, TimeSpan.FromHours(2));
        var service = new SnapshotService(() => _tempRoot, () => fixedTime);
        var frame = CreateFrame();

        var first = service.SaveSnapshot(frame);
        var second = service.SaveSnapshot(frame);

        Assert.NotEqual(first, second);
        Assert.EndsWith("microscope-20260708-184500.png", first);
        Assert.EndsWith("microscope-20260708-184500-001.png", second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void SaveSnapshot_WhenConfiguredFolderIsInvalid_FallsBackToTempFolder()
    {
        var service = new SnapshotService(() => "C:\\invalid\0snapshot-folder");
        var frame = CreateFrame();

        var savedPath = service.SaveSnapshot(frame);

        Assert.True(File.Exists(savedPath));
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "USB Microscope Studio", "Snapshots"), savedPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static BitmapSource CreateFrame()
    {
        var pixels = new byte[] { 40, 80, 120, 255 };
        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
        bitmap.Freeze();
        return bitmap;
    }
}
