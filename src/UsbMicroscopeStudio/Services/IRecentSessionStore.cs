using UsbMicroscopeStudio.Models.Sessions;

namespace UsbMicroscopeStudio.Services;

public interface IRecentSessionStore
{
    IReadOnlyList<RecentSessionEntry> Load();

    void Save(IReadOnlyList<RecentSessionEntry> sessions);
}
