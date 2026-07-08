using System.IO;
using System.Text.Json;
using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class AnnotationSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string Serialize(InspectionDocument document) => JsonSerializer.Serialize(document, JsonOptions);

    public InspectionDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<InspectionDocument>(json, JsonOptions) ?? new InspectionDocument();

    public void Save(string path, InspectionDocument document)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(document));
    }

    public InspectionDocument Load(string path) => Deserialize(File.ReadAllText(path));
}
