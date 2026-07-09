using System.IO;
using System.Text;
using System.Text.Json;
using UsbMicroscopeStudio.Models.Sessions;

namespace UsbMicroscopeStudio.Services;

public sealed class InspectionSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public InspectionSessionDocument Save(InspectionSessionDocument session, string workspaceFolder)
    {
        var paths = string.IsNullOrWhiteSpace(session.SessionFolderPath)
            ? CreateSessionFolders(workspaceFolder, session.InspectionDateTime, session.SessionName)
            : EnsureSessionFolders(session.SessionFolderPath);

        var saved = session with
        {
            WorkspaceFolderPath = workspaceFolder,
            SessionFolderPath = paths.SessionFolder,
            SessionJsonPath = paths.SessionJsonPath
        };

        File.WriteAllText(paths.SessionJsonPath, JsonSerializer.Serialize(saved, JsonOptions));
        return saved;
    }

    public InspectionSessionDocument Load(string path) =>
        JsonSerializer.Deserialize<InspectionSessionDocument>(File.ReadAllText(path), JsonOptions)
        ?? new InspectionSessionDocument();

    public InspectionSessionPaths CreateSessionFolders(string workspaceFolder, DateTimeOffset inspectionDateTime, string sessionName)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            throw new InvalidOperationException("Select a workspace folder before saving a session.");
        }

        Directory.CreateDirectory(workspaceFolder);
        var baseName = $"{inspectionDateTime:yyyyMMdd-HHmmss}-{ToSafeFolderName(sessionName)}";
        var sessionFolder = Path.Combine(workspaceFolder, baseName);
        for (var attempt = 1; Directory.Exists(sessionFolder); attempt++)
        {
            sessionFolder = Path.Combine(workspaceFolder, $"{baseName}-{attempt:000}");
        }

        return EnsureSessionFolders(sessionFolder);
    }

    public InspectionSessionPaths EnsureSessionFolders(string sessionFolder)
    {
        var cleanFramesFolder = Path.Combine(sessionFolder, "clean-frames");
        var annotatedFramesFolder = Path.Combine(sessionFolder, "annotated-frames");
        var sidecarsFolder = Path.Combine(sessionFolder, "sidecars");
        Directory.CreateDirectory(cleanFramesFolder);
        Directory.CreateDirectory(annotatedFramesFolder);
        Directory.CreateDirectory(sidecarsFolder);
        return new InspectionSessionPaths(
            sessionFolder,
            cleanFramesFolder,
            annotatedFramesFolder,
            sidecarsFolder,
            Path.Combine(sidecarsFolder, "session.json"));
    }

    public static string ToSafeFolderName(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "untitled-inspection" : value.Trim();
        var builder = new StringBuilder();
        var lastWasSeparator = false;
        foreach (var character in source)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        var safe = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "untitled-inspection" : safe;
    }
}
