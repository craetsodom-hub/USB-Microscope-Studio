using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;
using UsbMicroscopeStudio.ViewModels;

namespace UsbMicroscopeStudio.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshCamerasAsync_SelectsHardwareCameraAndLoadsFormats()
    {
        var catalog = new FakeCameraCatalog(
            [new CameraDevice("usb://camera-a", "Bench Camera", 0), new CameraDevice("demo://microscope", "Demo", -1, true)],
            [new CameraFormat(1280, 720, 30)]);
        using var viewModel = CreateViewModel(catalog);

        await viewModel.RefreshCamerasAsync();

        Assert.Equal("Bench Camera", viewModel.SelectedCamera?.Name);
        Assert.Equal("1280 x 720 @ 30 FPS", viewModel.SelectedFormat?.DisplayName);
    }

    [Fact]
    public async Task StartPreviewAsync_StartsSelectedCamera()
    {
        var camera = new CameraDevice("demo://microscope", "Demo", -1, true);
        var format = new CameraFormat(640, 480, 30);
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(new FakeCameraCatalog([camera], [format]), preview);

        await viewModel.RefreshCamerasAsync();
        await viewModel.StartPreviewAsync();

        Assert.True(viewModel.IsPreviewing);
        Assert.Equal(camera, preview.StartedCamera);
        Assert.Equal(format, preview.StartedFormat);
    }

    [Fact]
    public async Task RapidCameraSwitching_IgnoresStaleFormatLoads()
    {
        var cameraA = new CameraDevice("usb://camera-a", "Camera A", 0);
        var cameraB = new CameraDevice("usb://camera-b", "Camera B", 1);
        var formatA = new CameraFormat(640, 480, 30);
        var formatB = new CameraFormat(1920, 1080, 60);
        var catalog = new FakeCameraCatalog(
            [cameraA, cameraB],
            async camera =>
            {
                await Task.Delay(camera == cameraB ? 150 : 10);
                return camera == cameraB ? [formatB] : [formatA];
            });
        using var viewModel = CreateViewModel(catalog);

        await viewModel.RefreshCamerasAsync();
        viewModel.SelectedCamera = cameraB;
        viewModel.SelectedCamera = cameraA;
        await Task.Delay(250);

        Assert.Equal(cameraA, viewModel.SelectedCamera);
        Assert.Equal(formatA, viewModel.SelectedFormat);
        Assert.DoesNotContain(formatB, viewModel.Formats);
    }

    [Fact]
    public async Task StartPreviewAsync_WhenPreviewServiceFails_DoesNotEnterPreviewingState()
    {
        var preview = new FakePreviewService { FailOnStart = true };
        using var viewModel = CreateViewModel(previewService: preview);

        await viewModel.RefreshCamerasAsync();
        await viewModel.StartPreviewAsync();

        Assert.False(viewModel.IsPreviewing);
        Assert.StartsWith("Preview failed to start:", viewModel.StatusMessage);
    }

    [Fact]
    public void FreezeKeepsExistingFrame()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);
        var firstFrame = CreateFrame();
        var secondFrame = CreateFrame();

        preview.PublishFrame(firstFrame);
        viewModel.IsFrozen = true;
        preview.PublishFrame(secondFrame);

        Assert.Same(firstFrame, viewModel.PreviewFrame);
    }

    [Fact]
    public void RotateAndMirrorUpdatePreviewTransforms()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);

        viewModel.RotateRight();
        viewModel.ToggleMirror();

        Assert.Equal(90, preview.TransformOptions.RotationDegrees);
        Assert.True(preview.TransformOptions.MirrorHorizontal);
    }

    [Fact]
    public async Task AnnotationCoordinatesRemainAttachedAcrossTransformAndResolutionChanges()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);
        await viewModel.RefreshCamerasAsync();
        viewModel.Annotations.Add(new InspectionAnnotation
        {
            Tool = InspectionTool.Line,
            Points = [new(0.2, 0.4), new(0.8, 0.4)]
        });

        viewModel.RotateRight();
        viewModel.ToggleMirror();
        preview.PublishFrame(CreateFrame(1280, 720));

        Assert.Equal(new InspectionPoint(0.4, 0.2), viewModel.Annotations[0].Points[0]);
        Assert.Equal(new InspectionPoint(0.4, 0.8), viewModel.Annotations[0].Points[1]);
        Assert.Equal(1280, viewModel.PreviewWidth);
        Assert.Equal(720, viewModel.PreviewHeight);
    }

    [Fact]
    public async Task CalibrationProfilesAreFilteredByCurrentCameraAndFormat()
    {
        var camera = new CameraDevice("usb://camera-a", "Camera A", 0);
        var formatA = new CameraFormat(640, 480, 30);
        var formatB = new CameraFormat(1280, 720, 30);
        var matching = new CalibrationProfile { Name = "640", CameraId = camera.Id, Format = formatA, UnitsPerPixel = 0.1 };
        var otherResolution = new CalibrationProfile { Name = "1280", CameraId = camera.Id, Format = formatB, UnitsPerPixel = 0.2 };
        var otherCamera = new CalibrationProfile { Name = "Other", CameraId = "usb://camera-b", Format = formatA, UnitsPerPixel = 0.3 };
        using var viewModel = CreateViewModel(
            catalog: new FakeCameraCatalog([camera], [formatA, formatB]),
            calibrationProfileStore: new FakeCalibrationProfileStore([matching, otherResolution, otherCamera]));

        await viewModel.RefreshCamerasAsync();
        viewModel.SelectedCalibrationProfile = matching;
        viewModel.SelectedFormat = formatB;

        Assert.Single(viewModel.VisibleCalibrationProfiles);
        Assert.Equal(otherResolution, viewModel.VisibleCalibrationProfiles[0]);
        Assert.Null(viewModel.SelectedCalibrationProfile);
        Assert.Equal("Uncalibrated", viewModel.CalibrationStatus);
    }

    [Fact]
    public async Task MeasurementDoesNotUseMismatchedCalibrationProfile()
    {
        var camera = new CameraDevice("usb://camera-a", "Camera A", 0);
        var selectedFormat = new CameraFormat(640, 480, 30);
        var wrongFormat = new CameraFormat(1280, 720, 30);
        var mismatched = new CalibrationProfile { Name = "Wrong", CameraId = camera.Id, Format = wrongFormat, UnitsPerPixel = 0.1 };
        using var viewModel = CreateViewModel(catalog: new FakeCameraCatalog([camera], [selectedFormat]));

        await viewModel.RefreshCamerasAsync();
        viewModel.SelectedCalibrationProfile = mismatched;
        viewModel.Annotations.Add(new InspectionAnnotation
        {
            Tool = InspectionTool.Distance,
            IsMeasurement = true,
            Points = [new(0, 0), new(1, 0)]
        });

        Assert.Null(viewModel.SelectedCalibrationProfile);
        Assert.Equal("Uncalibrated", viewModel.MeasurementStatus);
    }

    [Fact]
    public async Task OpenInspectionRestoresFrameAnnotationsAndMatchingCalibration()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioOpenInspectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var camera = new CameraDevice("usb://camera-a", "Camera A", 0);
            var format = new CameraFormat(4, 3, 30);
            var cleanPath = Path.Combine(tempDirectory, "clean.png");
            SavePng(CreateFrame(4, 3), cleanPath);
            var profile = new CalibrationProfile { Name = "Bench", CameraId = camera.Id, Format = format, UnitsPerPixel = 0.05 };
            var sidecarPath = Path.Combine(tempDirectory, "inspection.json");
            new AnnotationSerializer().Save(sidecarPath, new InspectionDocument
            {
                CleanFramePath = cleanPath,
                CalibrationProfile = profile,
                Annotations =
                [
                    new InspectionAnnotation
                    {
                        Tool = InspectionTool.Text,
                        Text = "Socket A",
                        Points = [new(0.25, 0.5)]
                    }
                ]
            });
            using var viewModel = CreateViewModel(catalog: new FakeCameraCatalog([camera], [format]));

            await viewModel.RefreshCamerasAsync();
            viewModel.OpenInspection(sidecarPath);

            Assert.Equal(4, viewModel.PreviewFrame?.PixelWidth);
            Assert.Single(viewModel.Annotations);
            Assert.Equal("Socket A", viewModel.Annotations[0].Text);
            Assert.Equal(profile.ProfileKey, viewModel.SelectedCalibrationProfile?.ProfileKey);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void AnnotationHistoryUpdatesUndoRedoCommandState()
    {
        using var viewModel = CreateViewModel();

        viewModel.CaptureAnnotationHistory();
        viewModel.Annotations.Add(new InspectionAnnotation { Tool = InspectionTool.Line, Points = [new(0, 0), new(1, 1)] });
        Assert.True(viewModel.UndoAnnotationsCommand.CanExecute(null));

        viewModel.UndoAnnotations();

        Assert.Empty(viewModel.Annotations);
        Assert.False(viewModel.UndoAnnotationsCommand.CanExecute(null));
        Assert.True(viewModel.RedoAnnotationsCommand.CanExecute(null));
    }

    [Fact]
    public void SnapshotSavesCurrentFrame()
    {
        var preview = new FakePreviewService();
        var snapshots = new FakeSnapshotService();
        var settings = new FakeSettingsStore(new AppSettings("C:\\SelectedSnapshots"));
        using var viewModel = CreateViewModel(previewService: preview, snapshotService: snapshots, settingsStore: settings);
        var frame = CreateFrame();

        preview.PublishFrame(frame);
        viewModel.Snapshot();

        Assert.Equal("C:\\Snapshots\\frame.png", viewModel.LastSnapshotPath);
        Assert.Same(frame, snapshots.SavedFrame);
        Assert.Equal("C:\\SelectedSnapshots", snapshots.SavedFolder);
    }

    [Fact]
    public void Constructor_LoadsPersistedSnapshotFolder()
    {
        var settings = new FakeSettingsStore(new AppSettings("D:\\Bench Captures"));

        using var viewModel = CreateViewModel(settingsStore: settings);

        Assert.Equal("D:\\Bench Captures", viewModel.SnapshotFolderPath);
    }

    [Fact]
    public void ChooseSnapshotFolder_PersistsSelectedFolder()
    {
        var settings = new FakeSettingsStore(new AppSettings("C:\\Old"));
        var picker = new FakeFolderPicker("C:\\New");
        using var viewModel = CreateViewModel(settingsStore: settings, folderPicker: picker);

        viewModel.ChooseSnapshotFolder();

        Assert.Equal("C:\\New", viewModel.SnapshotFolderPath);
        Assert.Equal("C:\\New", settings.SavedSettings?.SnapshotFolderPath);
        Assert.Equal("C:\\Old", picker.InitialDirectory);
    }

    [Fact]
    public void ZoomCommandsClampToSupportedRange()
    {
        using var viewModel = CreateViewModel();

        for (var i = 0; i < 50; i++)
        {
            viewModel.ZoomOut();
        }

        Assert.Equal(0.25, viewModel.ZoomLevel);

        for (var i = 0; i < 60; i++)
        {
            viewModel.ZoomIn();
        }

        Assert.Equal(4.0, viewModel.ZoomLevel);
    }

    private static MainViewModel CreateViewModel(
        FakeCameraCatalog? catalog = null,
        FakePreviewService? previewService = null,
        FakeSnapshotService? snapshotService = null,
        FakeSettingsStore? settingsStore = null,
        FakeFolderPicker? folderPicker = null,
        FakeCalibrationProfileStore? calibrationProfileStore = null)
    {
        return new MainViewModel(
            catalog ?? new FakeCameraCatalog([new CameraDevice("demo://microscope", "Demo", -1, true)], [new CameraFormat(640, 480, 30)]),
            previewService ?? new FakePreviewService(),
            snapshotService ?? new FakeSnapshotService(),
            settingsStore ?? new FakeSettingsStore(new AppSettings(null)),
            folderPicker ?? new FakeFolderPicker(null),
            new ImmediateDispatcher(),
            calibrationProfileStore);
    }

    private static BitmapSource CreateFrame(int width = 1, int height = 1)
    {
        var pixels = Enumerable.Repeat<byte>(255, width * height * 4).ToArray();
        var bitmap = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static void SavePng(BitmapSource frame, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private sealed class FakeCameraCatalog : ICameraCatalog
    {
        private readonly IReadOnlyList<CameraDevice> _cameras;
        private readonly Func<CameraDevice, Task<IReadOnlyList<CameraFormat>>> _formatsFactory;

        public FakeCameraCatalog(IReadOnlyList<CameraDevice> cameras, IReadOnlyList<CameraFormat> formats)
            : this(cameras, camera => Task.FromResult(formats))
        {
        }

        public FakeCameraCatalog(IReadOnlyList<CameraDevice> cameras, Func<CameraDevice, Task<IReadOnlyList<CameraFormat>>> formatsFactory)
        {
            _cameras = cameras;
            _formatsFactory = formatsFactory;
        }

        public Task<IReadOnlyList<CameraDevice>> GetCamerasAsync(CancellationToken cancellationToken = default) => Task.FromResult(_cameras);

        public Task<IReadOnlyList<CameraFormat>> GetFormatsAsync(CameraDevice camera, CancellationToken cancellationToken = default) => _formatsFactory(camera);
    }

    private sealed class FakePreviewService : ICameraPreviewService
    {
        public event EventHandler<FrameReadyEventArgs>? FrameReady;

        public event EventHandler<string>? StatusChanged;

        public bool IsRunning { get; private set; }

        public FrameTransformOptions TransformOptions { get; set; } = FrameTransformOptions.Default;

        public CameraDevice? StartedCamera { get; private set; }

        public CameraFormat? StartedFormat { get; private set; }

        public bool FailOnStart { get; init; }

        public Task StartAsync(CameraDevice camera, CameraFormat format, CancellationToken cancellationToken = default)
        {
            if (FailOnStart)
            {
                throw new InvalidOperationException("Camera refused to start.");
            }

            StartedCamera = camera;
            StartedFormat = format;
            IsRunning = true;
            StatusChanged?.Invoke(this, "Started");
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        public void PublishFrame(BitmapSource frame) => FrameReady?.Invoke(this, new FrameReadyEventArgs(frame, 30));

        public void Dispose()
        {
        }
    }

    private sealed class FakeSnapshotService : ISnapshotService
    {
        public BitmapSource? SavedFrame { get; private set; }

        public string? SavedFolder { get; private set; }

        public string DefaultSnapshotDirectory => "C:\\DefaultSnapshots";

        public string SaveSnapshot(BitmapSource frame, string snapshotDirectory)
        {
            SavedFrame = frame;
            SavedFolder = snapshotDirectory;
            return "C:\\Snapshots\\frame.png";
        }
    }

    private sealed class FakeSettingsStore(AppSettings loadedSettings) : IAppSettingsStore
    {
        public AppSettings? SavedSettings { get; private set; }

        public AppSettings Load() => loadedSettings;

        public void Save(AppSettings settings) => SavedSettings = settings;
    }

    private sealed class FakeFolderPicker(string? selectedFolder) : IFolderPickerService
    {
        public string? InitialDirectory { get; private set; }

        public string? PickFolder(string initialDirectory)
        {
            InitialDirectory = initialDirectory;
            return selectedFolder;
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Invoke(Action action) => action();
    }

    private sealed class FakeCalibrationProfileStore(IReadOnlyList<CalibrationProfile> profiles) : ICalibrationProfileStore
    {
        public IReadOnlyList<CalibrationProfile>? SavedProfiles { get; private set; }

        public IReadOnlyList<CalibrationProfile> Load() => profiles;

        public void Save(IReadOnlyList<CalibrationProfile> savedProfiles) => SavedProfiles = savedProfiles.ToList();
    }
}
