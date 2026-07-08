using System.IO;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioSettingsTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveThenLoad_PersistsSnapshotFolderPath()
    {
        var settingsPath = Path.Combine(_tempRoot, "settings.json");
        var firstStore = new JsonAppSettingsStore(settingsPath);

        firstStore.Save(new AppSettings("D:\\Inspection Captures"));
        var secondStore = new JsonAppSettingsStore(settingsPath);

        Assert.Equal("D:\\Inspection Captures", secondStore.Load().SnapshotFolderPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
