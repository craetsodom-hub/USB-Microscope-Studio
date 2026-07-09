using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Models.Sessions;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Sessions;

public sealed class InspectionSessionStoreTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioSessionTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ToSafeFolderName_ReplacesUnsafeCharacters()
    {
        Assert.Equal("board-a-rework-1", InspectionSessionStore.ToSafeFolderName("Board A: Rework #1"));
        Assert.Equal("untitled-inspection", InspectionSessionStore.ToSafeFolderName("///"));
    }

    [Fact]
    public void Save_CreatesPredictableFolderStructure()
    {
        var store = new InspectionSessionStore();
        var session = new InspectionSessionDocument
        {
            SessionName = "Board A",
            InspectionDateTime = new DateTimeOffset(2026, 7, 9, 14, 30, 0, TimeSpan.Zero)
        };

        var saved = store.Save(session, _tempRoot);

        Assert.EndsWith("20260709-143000-board-a", saved.SessionFolderPath);
        Assert.True(Directory.Exists(Path.Combine(saved.SessionFolderPath!, "clean-frames")));
        Assert.True(Directory.Exists(Path.Combine(saved.SessionFolderPath!, "annotated-frames")));
        Assert.True(Directory.Exists(Path.Combine(saved.SessionFolderPath!, "sidecars")));
        Assert.Equal(Path.Combine(saved.SessionFolderPath!, "sidecars", "session.json"), saved.SessionJsonPath);
        Assert.Null(saved.InspectionJsonSidecarPath);
    }

    [Fact]
    public void Save_PreservesSeparateSessionJsonAndInspectionSidecarPaths()
    {
        var store = new InspectionSessionStore();
        var inspectionSidecarPath = Path.Combine(_tempRoot, "existing-sidecars", "inspection-20260709-143000.json");
        var session = new InspectionSessionDocument
        {
            SessionName = "Board A",
            InspectionDateTime = new DateTimeOffset(2026, 7, 9, 14, 30, 0, TimeSpan.Zero),
            InspectionJsonSidecarPath = inspectionSidecarPath
        };

        var saved = store.Save(session, _tempRoot);

        Assert.Equal(Path.Combine(saved.SessionFolderPath!, "sidecars", "session.json"), saved.SessionJsonPath);
        Assert.Equal(inspectionSidecarPath, saved.InspectionJsonSidecarPath);
        Assert.NotEqual(saved.SessionJsonPath, saved.InspectionJsonSidecarPath);
        Assert.True(File.Exists(saved.SessionJsonPath));
    }

    [Fact]
    public void SaveLoad_PersistsMetadataAnnotationsMeasurementsAndCalibrationReference()
    {
        var store = new InspectionSessionStore();
        var profile = new CalibrationProfile
        {
            Name = "10x",
            CameraId = "demo://microscope",
            Format = new CameraFormat(640, 480, 30, "Demo"),
            UnitsPerPixel = 0.01
        };
        var session = new InspectionSessionDocument
        {
            SessionName = "Connector inspect",
            CustomerName = "Contoso",
            DeviceModel = "USB board",
            SerialAssetTag = "A-100",
            TechnicianName = "Alex",
            JobOrderNumber = "WO-42",
            Notes = "Inspect pins",
            InspectionDateTime = new DateTimeOffset(2026, 7, 9, 15, 0, 0, TimeSpan.Zero),
            CleanFramePath = "clean.png",
            AnnotatedFramePath = "annotated.png",
            InspectionJsonSidecarPath = Path.Combine(_tempRoot, "sidecars", "inspection-20260709-150000.json"),
            CalibrationStatus = "Calibrated",
            CalibrationProfileKey = profile.ProfileKey,
            CalibrationProfile = profile,
            Annotations =
            [
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Text,
                    Text = "Pin 1",
                    Points = [new(0.2, 0.3)]
                }
            ],
            Measurements = [new MeasurementResult(20, 0.2, 90, InspectionUnits.Millimetres, true)]
        };

        var saved = store.Save(session, _tempRoot);
        var loaded = store.Load(saved.SessionJsonPath!);

        Assert.Equal("Connector inspect", loaded.SessionName);
        Assert.Equal(Path.Combine(saved.SessionFolderPath!, "sidecars", "session.json"), loaded.SessionJsonPath);
        Assert.Equal(session.InspectionJsonSidecarPath, loaded.InspectionJsonSidecarPath);
        Assert.Equal("Contoso", loaded.CustomerName);
        Assert.Equal("USB board", loaded.DeviceModel);
        Assert.Equal("A-100", loaded.SerialAssetTag);
        Assert.Equal("Alex", loaded.TechnicianName);
        Assert.Equal("WO-42", loaded.JobOrderNumber);
        Assert.Equal("Inspect pins", loaded.Notes);
        Assert.Single(loaded.Annotations);
        Assert.Equal("Pin 1", loaded.Annotations[0].Text);
        Assert.Single(loaded.Measurements);
        Assert.Equal(profile.ProfileKey, loaded.CalibrationProfileKey);
        Assert.Equal(profile.ProfileKey, loaded.CalibrationProfile?.ProfileKey);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
