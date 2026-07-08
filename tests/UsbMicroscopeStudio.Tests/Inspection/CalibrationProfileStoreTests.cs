using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Inspection;

public sealed class CalibrationProfileStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioCalibrationTests", Guid.NewGuid().ToString("N"), "profiles.json");

    [Fact]
    public void SaveLoad_PersistsProfilesByCameraResolutionAndName()
    {
        var store = new JsonCalibrationProfileStore(_path);
        var profile = new CalibrationProfile
        {
            Name = "Bench objective",
            CameraId = "camera-a",
            Format = new CameraFormat(1920, 1080, 30),
            UnitsPerPixel = 0.02,
            Units = InspectionUnits.Millimetres
        };

        store.Save([profile]);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal(profile.ProfileKey, loaded[0].ProfileKey);
        Assert.Equal(0.02, loaded[0].UnitsPerPixel);
    }

    public void Dispose()
    {
        var root = Path.GetDirectoryName(_path);
        if (root is not null && Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
