using System.IO;
using System.Text.Json;
using UsbMicroscopeStudio.Models.Sessions;

namespace UsbMicroscopeStudio.Services;

public sealed class JsonRecentSessionStore : IRecentSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public JsonRecentSessionStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "USB Microscope Studio",
            "recent-sessions.json"))
    {
    }

    public JsonRecentSessionStore(string path)
    {
        _path = path;
    }

    public IReadOnlyList<RecentSessionEntry> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<RecentSessionEntry>>(File.ReadAllText(_path), JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<RecentSessionEntry> sessions)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(sessions, JsonOptions));
    }
}
