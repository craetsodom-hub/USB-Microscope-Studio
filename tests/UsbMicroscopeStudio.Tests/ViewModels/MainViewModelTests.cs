using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Models.Sessions;
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

    [Theory]
    [InlineData(90, 0.7, 0.2)]
    [InlineData(180, 0.8, 0.7)]
    [InlineData(270, 0.3, 0.8)]
    public void AnnotationCoordinatesFollowRotationOnly(int rotationDegrees, double expectedX, double expectedY)
    {
        using var viewModel = CreateViewModel();
        AddSinglePointAnnotation(viewModel);

        viewModel.RotationDegrees = rotationDegrees;

        AssertPoint(new InspectionPoint(expectedX, expectedY), viewModel.Annotations[0].Points[0]);
    }

    [Fact]
    public void AnnotationCoordinatesFollowMirrorOnly()
    {
        using var viewModel = CreateViewModel();
        AddSinglePointAnnotation(viewModel);

        viewModel.ToggleMirror();

        AssertPoint(new InspectionPoint(0.8, 0.3), viewModel.Annotations[0].Points[0]);
    }

    [Fact]
    public void AnnotationCoordinatesFollowRotateThenMirrorUsingPreviewOrder()
    {
        using var viewModel = CreateViewModel();
        AddSinglePointAnnotation(viewModel);

        viewModel.RotateRight();
        viewModel.ToggleMirror();

        AssertPoint(new InspectionPoint(0.7, 0.8), viewModel.Annotations[0].Points[0]);
    }

    [Fact]
    public void AnnotationCoordinatesFollowMirrorThenRotateUsingPreviewOrder()
    {
        using var viewModel = CreateViewModel();
        AddSinglePointAnnotation(viewModel);

        viewModel.ToggleMirror();
        viewModel.RotateRight();

        AssertPoint(new InspectionPoint(0.7, 0.8), viewModel.Annotations[0].Points[0]);
    }

    [Fact]
    public void AnnotationCoordinatesResetFromMirroredAndRotatedBackToNormal()
    {
        using var viewModel = CreateViewModel();
        AddSinglePointAnnotation(viewModel);

        viewModel.RotateRight();
        viewModel.ToggleMirror();
        viewModel.ResetView();

        AssertPoint(new InspectionPoint(0.2, 0.3), viewModel.Annotations[0].Points[0]);
    }

    [Fact]
    public async Task FrozenFrameDefersAnnotationTransformUntilNewFrameIsDisplayed()
    {
        var preview = new FakePreviewService();
        using var viewModel = CreateViewModel(previewService: preview);
        await viewModel.RefreshCamerasAsync();
        preview.PublishFrame(CreateFrame(640, 480));
        AddSinglePointAnnotation(viewModel);

        viewModel.IsFrozen = true;
        viewModel.RotateRight();
        viewModel.ToggleMirror();
        preview.PublishFrame(CreateFrame(1280, 720));

        AssertPoint(new InspectionPoint(0.2, 0.3), viewModel.Annotations[0].Points[0]);
        Assert.Equal(640, viewModel.PreviewWidth);
        Assert.Equal(480, viewModel.PreviewHeight);

        viewModel.IsFrozen = false;
        preview.PublishFrame(CreateFrame(1280, 720));

        AssertPoint(new InspectionPoint(0.7, 0.8), viewModel.Annotations[0].Points[0]);
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
    public void SaveInspectionSidecarAfterCleanFrameRestoresCleanFramePath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioSidecarTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var preview = new FakePreviewService();
            var snapshotService = new SnapshotService(() => new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero));
            using var viewModel = CreateViewModel(
                previewService: preview,
                snapshotService: snapshotService,
                settingsStore: new FakeSettingsStore(new AppSettings(tempDirectory)));
            preview.PublishFrame(CreateFrame(4, 3));

            viewModel.SaveCleanFrame();
            viewModel.SaveInspectionSidecar();

            var sidecarPath = Directory.GetFiles(tempDirectory, "inspection-*.json").Single();
            var document = new AnnotationSerializer().Load(sidecarPath);
            Assert.Equal(viewModel.LastSnapshotPath, document.CleanFramePath);
            Assert.Null(document.AnnotatedFramePath);

            using var reopened = CreateViewModel(settingsStore: new FakeSettingsStore(new AppSettings(tempDirectory)));
            reopened.OpenInspection(sidecarPath);

            Assert.Equal(viewModel.LastSnapshotPath, reopened.LastSnapshotPath);
            Assert.Equal(4, reopened.PreviewFrame?.PixelWidth);
            Assert.Equal(3, reopened.PreviewFrame?.PixelHeight);
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
        Assert.Equal("Select snapshot folder", picker.Title);
    }

    [Fact]
    public void SaveSessionAs_StoresMetadataAnnotationsInspectionSidecarAndRecentSession()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioViewModelSessionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var recentStore = new FakeRecentSessionStore([]);
            var picker = new FakeFolderPicker(tempDirectory);
            using var viewModel = CreateViewModel(folderPicker: picker, recentSessionStore: recentStore);
            viewModel.SessionName = "Board A";
            viewModel.CustomerName = "Contoso";
            viewModel.DeviceModel = "Controller";
            viewModel.SerialAssetTag = "SN-1";
            viewModel.TechnicianName = "Dana";
            viewModel.JobOrderNumber = "WO-1";
            viewModel.SessionNotes = "Initial inspection";
            viewModel.Annotations.Add(new InspectionAnnotation
            {
                Tool = InspectionTool.Angle,
                IsMeasurement = true,
                Points = [new(0.1, 0.4), new(0.5, 0.5), new(0.8, 0.2)]
            });

            viewModel.SaveSessionAs();
            var sessionPath = viewModel.CurrentSessionJsonPath!;
            Assert.EndsWith(Path.Combine("sidecars", "session.json"), sessionPath);
            Assert.True(File.Exists(sessionPath));
            Assert.Equal("Select session workspace folder", picker.Title);

            viewModel.SaveInspectionSidecar();
            var inspectionSidecarPath = viewModel.InspectionJsonSidecarPath!;
            Assert.EndsWith(".json", inspectionSidecarPath);
            Assert.Contains($"{Path.DirectorySeparatorChar}inspection-", inspectionSidecarPath);

            viewModel.SaveSession();
            var sessionDocument = new InspectionSessionStore().Load(sessionPath);
            Assert.Equal(sessionPath, sessionDocument.SessionJsonPath);
            Assert.Equal(inspectionSidecarPath, sessionDocument.InspectionJsonSidecarPath);
            Assert.NotEqual(sessionDocument.SessionJsonPath, sessionDocument.InspectionJsonSidecarPath);

            using var reopened = CreateViewModel(recentSessionStore: recentStore);
            reopened.OpenSession(sessionPath);

            Assert.Equal(sessionPath, reopened.CurrentSessionJsonPath);
            Assert.Equal(inspectionSidecarPath, reopened.InspectionJsonSidecarPath);
            Assert.Equal("Board A", reopened.SessionName);
            Assert.Equal("Contoso", reopened.CustomerName);
            Assert.Equal("Controller", reopened.DeviceModel);
            Assert.Equal("SN-1", reopened.SerialAssetTag);
            Assert.Equal("Dana", reopened.TechnicianName);
            Assert.Equal("WO-1", reopened.JobOrderNumber);
            Assert.Equal("Initial inspection", reopened.SessionNotes);
            Assert.Single(reopened.Annotations);
            Assert.Equal(InspectionTool.Angle, reopened.Annotations[0].Tool);
            Assert.Single(recentStore.SavedSessions!);
            Assert.Equal(sessionPath, recentStore.SavedSessions![0].SessionPath);

            using var recentReopened = CreateViewModel(recentSessionStore: new FakeRecentSessionStore(recentStore.SavedSessions!));
            recentReopened.SelectedRecentSession = recentReopened.RecentSessions.Single();
            recentReopened.OpenRecentSession();

            Assert.Equal(sessionPath, recentReopened.CurrentSessionJsonPath);
            Assert.Equal(inspectionSidecarPath, recentReopened.InspectionJsonSidecarPath);
            Assert.Equal("Board A", recentReopened.SessionName);
            Assert.Single(recentReopened.Annotations);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveSessionAs_CreatesSessionJsonAndRecentSessionPath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioSessionSaveAsRegression", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var picker = new FakeFolderPicker(tempDirectory);
            var recentStore = new FakeRecentSessionStore([]);
            using var viewModel = CreateViewModel(folderPicker: picker, recentSessionStore: recentStore);
            viewModel.SessionName = "Premium smoke";
            viewModel.CustomerName = "Bench QA";
            viewModel.DeviceModel = "Demo assembly";

            viewModel.SaveSessionAs();

            Assert.Equal("Select session workspace folder", picker.Title);
            Assert.NotNull(viewModel.CurrentSessionJsonPath);
            Assert.EndsWith(Path.Combine("sidecars", "session.json"), viewModel.CurrentSessionJsonPath);
            Assert.True(File.Exists(viewModel.CurrentSessionJsonPath));
            Assert.Equal(viewModel.CurrentSessionJsonPath, viewModel.RecentSessions.Single().SessionPath);
            Assert.Equal(viewModel.CurrentSessionJsonPath, recentStore.SavedSessions!.Single().SessionPath);

            var saved = new InspectionSessionStore().Load(viewModel.CurrentSessionJsonPath!);
            Assert.Equal("Premium smoke", saved.SessionName);
            Assert.Equal("Bench QA", saved.CustomerName);
            Assert.Equal("Demo assembly", saved.DeviceModel);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ExportHtmlReport_AfterSavedSession_UpdatesLastReportPath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioReportExportRegression", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            using var viewModel = CreateViewModel(folderPicker: new FakeFolderPicker(tempDirectory));
            viewModel.SessionName = "Report smoke";
            viewModel.CustomerName = "Contoso";
            viewModel.DeviceModel = "Scope model";
            viewModel.Annotations.Add(new InspectionAnnotation
            {
                Tool = InspectionTool.Text,
                Text = "Visible note",
                Points = [new(0.4, 0.4)]
            });

            viewModel.SaveSessionAs();
            viewModel.ExportHtmlReport();

            Assert.NotNull(viewModel.LastReportPath);
            Assert.True(File.Exists(viewModel.LastReportPath));
            Assert.Contains($"{Path.DirectorySeparatorChar}reports{Path.DirectorySeparatorChar}report-", viewModel.LastReportPath);
            Assert.Equal($"HTML report exported: {viewModel.LastReportPath}", viewModel.StatusMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenRecentSession_LoadsSessionJsonPathInsteadOfInspectionSidecarPath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioRecentSessionViewModelTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var store = new InspectionSessionStore();
            var inspectionSidecarPath = Path.Combine(tempDirectory, "inspection-should-not-open.json");
            var saved = store.Save(new InspectionSessionDocument
            {
                SessionName = "Recent board",
                CustomerName = "Fabrikam",
                InspectionJsonSidecarPath = inspectionSidecarPath,
                Annotations =
                [
                    new InspectionAnnotation
                    {
                        Tool = InspectionTool.Text,
                        Text = "Recent note",
                        Points = [new(0.2, 0.3)]
                    }
                ]
            }, tempDirectory);

            using var viewModel = CreateViewModel(recentSessionStore: new FakeRecentSessionStore(
            [
                new RecentSessionEntry
                {
                    SessionName = saved.SessionName,
                    SessionPath = saved.SessionJsonPath!,
                    LastOpenedAt = DateTimeOffset.Now
                }
            ]));

            viewModel.SelectedRecentSession = viewModel.RecentSessions.Single();
            viewModel.OpenRecentSession();

            Assert.Equal(saved.SessionJsonPath, viewModel.CurrentSessionJsonPath);
            Assert.Equal(inspectionSidecarPath, viewModel.InspectionJsonSidecarPath);
            Assert.Equal("Recent board", viewModel.SessionName);
            Assert.Equal("Fabrikam", viewModel.CustomerName);
            Assert.Single(viewModel.Annotations);
            Assert.Equal("Recent note", viewModel.Annotations[0].Text);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
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
        ISnapshotService? snapshotService = null,
        FakeSettingsStore? settingsStore = null,
        FakeFolderPicker? folderPicker = null,
        FakeCalibrationProfileStore? calibrationProfileStore = null,
        InspectionSessionStore? sessionStore = null,
        FakeRecentSessionStore? recentSessionStore = null)
    {
        return new MainViewModel(
            catalog ?? new FakeCameraCatalog([new CameraDevice("demo://microscope", "Demo", -1, true)], [new CameraFormat(640, 480, 30)]),
            previewService ?? new FakePreviewService(),
            snapshotService ?? new FakeSnapshotService(),
            settingsStore ?? new FakeSettingsStore(new AppSettings(null)),
            folderPicker ?? new FakeFolderPicker(null),
            new ImmediateDispatcher(),
            calibrationProfileStore,
            null,
            sessionStore,
            recentSessionStore);
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

    private static void AddSinglePointAnnotation(MainViewModel viewModel)
    {
        viewModel.Annotations.Add(new InspectionAnnotation
        {
            Tool = InspectionTool.Line,
            Points = [new InspectionPoint(0.2, 0.3)]
        });
    }

    private static void AssertPoint(InspectionPoint expected, InspectionPoint actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 6);
        Assert.Equal(expected.Y, actual.Y, precision: 6);
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

        public string? Title { get; private set; }

        public string? PickFolder(string initialDirectory, string title = "Select folder")
        {
            InitialDirectory = initialDirectory;
            Title = title;
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

    private sealed class FakeRecentSessionStore(IReadOnlyList<RecentSessionEntry> sessions) : IRecentSessionStore
    {
        public IReadOnlyList<RecentSessionEntry>? SavedSessions { get; private set; }

        public IReadOnlyList<RecentSessionEntry> Load() => sessions;

        public void Save(IReadOnlyList<RecentSessionEntry> savedSessions) => SavedSessions = savedSessions.ToList();
    }
}
