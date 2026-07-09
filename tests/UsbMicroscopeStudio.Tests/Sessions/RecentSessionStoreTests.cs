using UsbMicroscopeStudio.Models.Sessions;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Sessions;

public sealed class RecentSessionStoreTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioRecentSessionTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveLoad_PersistsRecentSessions()
    {
        var path = Path.Combine(_tempRoot, "recent-sessions.json");
        var store = new JsonRecentSessionStore(path);
        var entry = new RecentSessionEntry
        {
            SessionName = "Board A",
            SessionPath = "C:\\Sessions\\Board A\\sidecars\\session.json",
            LastOpenedAt = new DateTimeOffset(2026, 7, 9, 16, 0, 0, TimeSpan.Zero)
        };

        store.Save([entry]);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal(entry.SessionName, loaded[0].SessionName);
        Assert.Equal(entry.SessionPath, loaded[0].SessionPath);
        Assert.Equal(entry.LastOpenedAt, loaded[0].LastOpenedAt);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
