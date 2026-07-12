using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class MediaFoundationPrerequisiteTests
{
    [Fact]
    public void IsAvailable_ReturnsTrueWhenRequiredLibrariesExist()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "mf.dll"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "mfreadwrite.dll"), string.Empty);

            Assert.True(MediaFoundationPrerequisite.IsAvailable(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenARequiredLibraryIsMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "mf.dll"), string.Empty);

            Assert.False(MediaFoundationPrerequisite.IsAvailable(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
