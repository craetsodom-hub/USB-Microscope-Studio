using System.IO;
using System.Text.Json;
using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class JsonCalibrationProfileStore : ICalibrationProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public JsonCalibrationProfileStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "USB Microscope Studio",
            "calibration-profiles.json"))
    {
    }

    public JsonCalibrationProfileStore(string path)
    {
        _path = path;
    }

    public IReadOnlyList<CalibrationProfile> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<CalibrationProfile>>(File.ReadAllText(_path), JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<CalibrationProfile> profiles)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(profiles, JsonOptions));
    }
}
