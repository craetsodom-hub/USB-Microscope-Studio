using System.IO;

namespace UsbMicroscopeStudio.Services;

public static class MediaFoundationPrerequisite
{
    private static readonly string[] RequiredLibraries = ["mf.dll", "mfreadwrite.dll"];

    public static bool IsAvailable(string? systemDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(systemDirectory)
            ? Environment.SystemDirectory
            : systemDirectory;

        return RequiredLibraries.All(library => File.Exists(Path.Combine(directory, library)));
    }
}
