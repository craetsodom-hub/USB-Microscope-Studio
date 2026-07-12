using Xunit;

namespace UsbMicroscopeStudio.Tests.Release;

public sealed class ReleaseFoundationTests
{
    [Fact]
    public void ProductIconAssets_ExistAndAreReferencedByTheProject()
    {
        var repositoryRoot = FindRepositoryRoot();
        var assetsDirectory = Path.Combine(repositoryRoot, "src", "UsbMicroscopeStudio", "Assets");
        var projectFile = Path.Combine(repositoryRoot, "src", "UsbMicroscopeStudio", "UsbMicroscopeStudio.csproj");
        var mainWindowFile = Path.Combine(repositoryRoot, "src", "UsbMicroscopeStudio", "MainWindow.xaml");

        Assert.True(File.Exists(Path.Combine(assetsDirectory, "AppIcon.ico")));
        Assert.True(File.Exists(Path.Combine(assetsDirectory, "AppIcon.png")));
        Assert.True(File.Exists(Path.Combine(assetsDirectory, "AppIcon.svg")));

        using var iconReader = new BinaryReader(File.OpenRead(Path.Combine(assetsDirectory, "AppIcon.ico")));
        Assert.Equal((ushort)0, iconReader.ReadUInt16());
        Assert.Equal((ushort)1, iconReader.ReadUInt16());
        var iconFrameCount = iconReader.ReadUInt16();
        Assert.Equal((ushort)7, iconFrameCount);

        var iconSizes = new List<int>();
        for (var frameIndex = 0; frameIndex < iconFrameCount; frameIndex++)
        {
            var size = iconReader.ReadByte();
            iconSizes.Add(size == 0 ? 256 : size);
            iconReader.BaseStream.Seek(15, SeekOrigin.Current);
        }

        Assert.Equal(new[] { 16, 24, 32, 48, 64, 128, 256 }, iconSizes);

        var projectContents = File.ReadAllText(projectFile);
        Assert.Contains("<ApplicationIcon>Assets\\AppIcon.ico</ApplicationIcon>", projectContents, StringComparison.Ordinal);
        Assert.Contains("<Content Include=\"Assets\\AppIcon.ico\">", projectContents, StringComparison.Ordinal);
        Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", projectContents, StringComparison.Ordinal);
        Assert.Contains("<Product>USB Microscope Studio</Product>", projectContents, StringComparison.Ordinal);
        Assert.Contains("<Company>USB Microscope Studio</Company>", projectContents, StringComparison.Ordinal);
        Assert.Contains("<AssemblyDescription>Professional microscope inspection workspace</AssemblyDescription>", projectContents, StringComparison.Ordinal);
        Assert.Contains("<Version>1.0.0</Version>", projectContents, StringComparison.Ordinal);
        Assert.Contains("Icon=\"pack://siteoforigin:,,,/Assets/AppIcon.ico\"", File.ReadAllText(mainWindowFile), StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsX64PublishScript_ExistsAndPublishesToTheReleaseFolder()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "publish-win-x64.ps1");

        Assert.True(File.Exists(scriptPath));

        var scriptContents = File.ReadAllText(scriptPath);
        Assert.Contains("--runtime win-x64", scriptContents, StringComparison.Ordinal);
        Assert.Contains("artifacts\\release\\win-x64", scriptContents, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UsbMicroscopeStudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
