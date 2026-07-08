using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ICameraCatalog _cameraCatalog;
    private readonly ICameraPreviewService _previewService;
    private readonly ISnapshotService _snapshotService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ICalibrationProfileStore _calibrationProfileStore;
    private readonly CalibrationCalculator _calibrationCalculator = new();
    private readonly AnnotationHistory _annotationHistory = new();
    private readonly AnnotationSerializer _annotationSerializer;
    private readonly SemaphoreSlim _previewGate = new(1, 1);
    private int _formatLoadRequestId;
    private int _previewStartRequestId;
    private bool _suppressFormatLoad;

    public MainViewModel(
        ICameraCatalog cameraCatalog,
        ICameraPreviewService previewService,
        ISnapshotService snapshotService,
        IAppSettingsStore settingsStore,
        IFolderPickerService folderPickerService,
        IUiDispatcher uiDispatcher,
        ICalibrationProfileStore? calibrationProfileStore = null,
        AnnotationSerializer? annotationSerializer = null)
    {
        _cameraCatalog = cameraCatalog;
        _previewService = previewService;
        _snapshotService = snapshotService;
        _settingsStore = settingsStore;
        _folderPickerService = folderPickerService;
        _uiDispatcher = uiDispatcher;
        _calibrationProfileStore = calibrationProfileStore ?? new JsonCalibrationProfileStore();
        _annotationSerializer = annotationSerializer ?? new AnnotationSerializer();

        _previewService.FrameReady += OnFrameReady;
        _previewService.StatusChanged += OnStatusChanged;
        Annotations.CollectionChanged += AnnotationsOnCollectionChanged;

        var settings = _settingsStore.Load();
        snapshotFolderPath = string.IsNullOrWhiteSpace(settings.SnapshotFolderPath)
            ? _snapshotService.DefaultSnapshotDirectory
            : settings.SnapshotFolderPath;

        foreach (var profile in _calibrationProfileStore.Load())
        {
            CalibrationProfiles.Add(profile);
        }
    }

    public ObservableCollection<CameraDevice> Cameras { get; } = [];

    public ObservableCollection<CameraFormat> Formats { get; } = [];

    public ObservableCollection<InspectionAnnotation> Annotations { get; } = [];

    public ObservableCollection<CalibrationProfile> CalibrationProfiles { get; } = [];

    public IReadOnlyList<InspectionTool> ToolChoices { get; } =
    [
        InspectionTool.Select,
        InspectionTool.Line,
        InspectionTool.Arrow,
        InspectionTool.Rectangle,
        InspectionTool.Circle,
        InspectionTool.Freehand,
        InspectionTool.Text,
        InspectionTool.ReferenceLine,
        InspectionTool.Distance,
        InspectionTool.Angle
    ];

    public IReadOnlyList<string> StrokeColors { get; } =
    [
        "#2F6FDB",
        "#1A7F37",
        "#D1242F",
        "#BF8700",
        "#8250DF",
        "#24292F"
    ];

    public IReadOnlyList<InspectionUnits> UnitChoices { get; } =
    [
        InspectionUnits.Millimetres,
        InspectionUnits.Micrometres
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartPreviewCommand))]
    private CameraDevice? selectedCamera;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartPreviewCommand))]
    private CameraFormat? selectedFormat;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SnapshotCommand))]
    private BitmapSource? previewFrame;

    [ObservableProperty]
    private double previewWidth = 1280;

    [ObservableProperty]
    private double previewHeight = 720;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string? lastSnapshotPath;

    [ObservableProperty]
    private string snapshotFolderPath = string.Empty;

    [ObservableProperty]
    private InspectionTool currentTool = InspectionTool.Select;

    [ObservableProperty]
    private string selectedStrokeColor = "#2F6FDB";

    [ObservableProperty]
    private double selectedStrokeThickness = 2;

    [ObservableProperty]
    private bool showCrosshair = true;

    [ObservableProperty]
    private bool showGrid;

    [ObservableProperty]
    private double gridSpacingPixels = 64;

    [ObservableProperty]
    private bool showRulers = true;

    [ObservableProperty]
    private bool isFreezeInspectMode;

    [ObservableProperty]
    private CalibrationProfile? selectedCalibrationProfile;

    [ObservableProperty]
    private string calibrationProfileName = "Default";

    [ObservableProperty]
    private double knownCalibrationLength = 1;

    [ObservableProperty]
    private InspectionUnits selectedUnits = InspectionUnits.Millimetres;

    [ObservableProperty]
    private string calibrationStatus = "Uncalibrated";

    [ObservableProperty]
    private string measurementStatus = "Uncalibrated";

    [ObservableProperty]
    private double currentFps;

    [ObservableProperty]
    private double zoomLevel = 1.0;

    [ObservableProperty]
    private int rotationDegrees;

    [ObservableProperty]
    private bool isMirrored;

    [ObservableProperty]
    private bool isFrozen;

    [ObservableProperty]
    private bool isFullscreen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartPreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopPreviewCommand))]
    private bool isPreviewing;

    partial void OnSelectedCameraChanged(CameraDevice? value)
    {
        if (!_suppressFormatLoad)
        {
            _ = LoadFormatsForSelectionAsync(value);
        }
    }

    partial void OnSelectedFormatChanged(CameraFormat? value)
    {
        if (IsPreviewing && SelectedCamera is not null && value is not null)
        {
            _ = StartPreviewAsync();
        }
    }

    partial void OnIsMirroredChanged(bool value) => UpdatePreviewTransforms();

    partial void OnRotationDegreesChanged(int value) => UpdatePreviewTransforms();

    partial void OnSnapshotFolderPathChanged(string value)
    {
        _settingsStore.Save(new AppSettings(value));
    }

    partial void OnSelectedCalibrationProfileChanged(CalibrationProfile? value)
    {
        CalibrationStatus = value is null
            ? "Uncalibrated"
            : $"Calibrated: {value.Name} ({value.UnitsPerPixel:0.####} {value.Units}/px)";
        UpdateMeasurementStatus();
    }

    partial void OnIsFreezeInspectModeChanged(bool value)
    {
        IsFrozen = value;
        StatusMessage = value ? "Freeze & Inspect enabled" : "Freeze & Inspect disabled";
    }

    [RelayCommand]
    public async Task RefreshCamerasAsync()
    {
        StatusMessage = "Scanning cameras...";
        var cameras = await _cameraCatalog.GetCamerasAsync();

        Cameras.Clear();
        foreach (var camera in cameras)
        {
            Cameras.Add(camera);
        }

        var preferredCamera = Cameras.FirstOrDefault(camera => !camera.IsDemo) ?? Cameras.FirstOrDefault();
        _suppressFormatLoad = true;
        SelectedCamera = preferredCamera;
        _suppressFormatLoad = false;
        await LoadFormatsForSelectionAsync(preferredCamera);
        StatusMessage = Cameras.Count == 0 ? "No cameras detected" : $"Detected {Cameras.Count} camera source(s)";
    }

    [RelayCommand(CanExecute = nameof(CanStartPreview))]
    public async Task StartPreviewAsync()
    {
        if (SelectedCamera is null || SelectedFormat is null)
        {
            return;
        }

        var requestId = Interlocked.Increment(ref _previewStartRequestId);
        var camera = SelectedCamera;
        var format = SelectedFormat;

        await _previewGate.WaitAsync();
        try
        {
            if (requestId != _previewStartRequestId)
            {
                return;
            }

            UpdatePreviewTransforms();
            StatusMessage = $"Starting {camera.DisplayName}...";
            await _previewService.StartAsync(camera, format);

            if (requestId == _previewStartRequestId)
            {
                IsPreviewing = true;
            }
        }
        catch (Exception ex)
        {
            IsPreviewing = false;
            StatusMessage = $"Preview failed to start: {ex.Message}";
        }
        finally
        {
            _previewGate.Release();
        }
    }

    [RelayCommand(CanExecute = nameof(IsPreviewing))]
    public async Task StopPreviewAsync()
    {
        Interlocked.Increment(ref _previewStartRequestId);
        await _previewGate.WaitAsync();
        try
        {
            await _previewService.StopAsync();
            IsPreviewing = false;
        }
        finally
        {
            _previewGate.Release();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSnapshot))]
    public void Snapshot()
    {
        if (PreviewFrame is null)
        {
            return;
        }

        try
        {
            LastSnapshotPath = _snapshotService.SaveSnapshot(PreviewFrame, SnapshotFolderPath);
            StatusMessage = $"Snapshot saved: {LastSnapshotPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Snapshot failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ZoomIn() => ZoomLevel = Math.Min(4.0, Math.Round(ZoomLevel + 0.1, 2));

    [RelayCommand]
    public void ZoomOut() => ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel - 0.1, 2));

    [RelayCommand]
    public void ResetView()
    {
        ZoomLevel = 1.0;
        RotationDegrees = 0;
        IsMirrored = false;
    }

    [RelayCommand]
    public void RotateLeft() => RotationDegrees = NormalizeRotation(RotationDegrees - 90);

    [RelayCommand]
    public void RotateRight() => RotationDegrees = NormalizeRotation(RotationDegrees + 90);

    [RelayCommand]
    public void ToggleMirror() => IsMirrored = !IsMirrored;

    [RelayCommand]
    public void ToggleFreeze() => IsFrozen = !IsFrozen;

    [RelayCommand]
    public void ToggleFullscreen() => IsFullscreen = !IsFullscreen;

    [RelayCommand]
    public void ChooseSnapshotFolder()
    {
        var selectedFolder = _folderPickerService.PickFolder(SnapshotFolderPath);
        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        SnapshotFolderPath = selectedFolder;
        StatusMessage = $"Snapshot folder: {SnapshotFolderPath}";
    }

    [RelayCommand]
    public void CaptureAnnotationHistory() => _annotationHistory.Capture(Annotations);

    [RelayCommand]
    public void UndoAnnotations()
    {
        _annotationHistory.Undo(Annotations);
        UpdateMeasurementStatus();
    }

    [RelayCommand]
    public void RedoAnnotations()
    {
        _annotationHistory.Redo(Annotations);
        UpdateMeasurementStatus();
    }

    [RelayCommand]
    public void ClearAnnotations()
    {
        if (Annotations.Count == 0)
        {
            return;
        }

        _annotationHistory.Capture(Annotations);
        Annotations.Clear();
        UpdateMeasurementStatus();
    }

    [RelayCommand]
    public void CreateCalibrationProfile()
    {
        var reference = Annotations.LastOrDefault(annotation => annotation.Tool == InspectionTool.ReferenceLine && annotation.Points.Count >= 2);
        if (reference is null || SelectedCamera is null || SelectedFormat is null)
        {
            CalibrationStatus = "Draw a reference line before calibration";
            return;
        }

        try
        {
            var profile = _calibrationCalculator.CreateProfile(
                CalibrationProfileName,
                SelectedCamera.Id,
                SelectedFormat,
                reference.Points[0],
                reference.Points[^1],
                KnownCalibrationLength,
                SelectedUnits);

            CalibrationProfiles.Add(profile);
            SelectedCalibrationProfile = profile;
            SaveCalibrationProfiles();
            StatusMessage = $"Calibration profile saved: {profile.Name}";
        }
        catch (Exception ex)
        {
            CalibrationStatus = ex.Message;
        }
    }

    [RelayCommand]
    public void RenameCalibrationProfile()
    {
        if (SelectedCalibrationProfile is null)
        {
            return;
        }

        var renamed = SelectedCalibrationProfile with { Name = string.IsNullOrWhiteSpace(CalibrationProfileName) ? SelectedCalibrationProfile.Name : CalibrationProfileName };
        ReplaceCalibrationProfile(SelectedCalibrationProfile, renamed);
        SelectedCalibrationProfile = renamed;
        SaveCalibrationProfiles();
    }

    [RelayCommand]
    public void DeleteCalibrationProfile()
    {
        if (SelectedCalibrationProfile is null)
        {
            return;
        }

        CalibrationProfiles.Remove(SelectedCalibrationProfile);
        SelectedCalibrationProfile = CalibrationProfiles.FirstOrDefault();
        SaveCalibrationProfiles();
    }

    [RelayCommand]
    public void SaveCleanFrame()
    {
        Snapshot();
    }

    [RelayCommand]
    public void SaveInspectionSidecar()
    {
        var sidecarPath = Path.Combine(SnapshotFolderPath, $"inspection-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json");
        var document = CreateInspectionDocument(null, LastSnapshotPath);
        _annotationSerializer.Save(sidecarPath, document);
        StatusMessage = $"Inspection sidecar saved: {sidecarPath}";
    }

    public void SaveInspectionSidecarForAnnotatedFrame(string annotatedFramePath)
    {
        var sidecarPath = Path.ChangeExtension(annotatedFramePath, ".json");
        var document = CreateInspectionDocument(LastSnapshotPath, annotatedFramePath);
        _annotationSerializer.Save(sidecarPath, document);
        StatusMessage = $"Annotated frame saved: {annotatedFramePath}";
    }

    public void Dispose()
    {
        _previewService.FrameReady -= OnFrameReady;
        _previewService.StatusChanged -= OnStatusChanged;
        Annotations.CollectionChanged -= AnnotationsOnCollectionChanged;
        _previewService.Dispose();
        _previewGate.Dispose();
    }

    private async Task LoadFormatsForSelectionAsync(CameraDevice? camera)
    {
        var requestId = Interlocked.Increment(ref _formatLoadRequestId);
        Formats.Clear();
        if (camera is null)
        {
            return;
        }

        StatusMessage = $"Loading formats for {camera.DisplayName}...";
        var formats = await _cameraCatalog.GetFormatsAsync(camera);
        if (requestId != _formatLoadRequestId || SelectedCamera != camera)
        {
            return;
        }

        foreach (var format in formats)
        {
            Formats.Add(format);
        }

        SelectedFormat = Formats.FirstOrDefault();
        StatusMessage = Formats.Count == 0 ? "No formats available" : $"Loaded {Formats.Count} format(s)";
    }

    private void OnFrameReady(object? sender, FrameReadyEventArgs e)
    {
        _uiDispatcher.Invoke(() =>
        {
            CurrentFps = e.FramesPerSecond;
            if (!IsFrozen)
            {
                PreviewFrame = e.Frame;
                PreviewWidth = e.Frame.PixelWidth;
                PreviewHeight = e.Frame.PixelHeight;
            }
        });
    }

    private void AnnotationsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateMeasurementStatus();
    }

    private void OnStatusChanged(object? sender, string status)
    {
        _uiDispatcher.Invoke(() => StatusMessage = status);
    }

    private bool CanStartPreview() => !IsPreviewing && SelectedCamera is not null && SelectedFormat is not null;

    private bool CanSnapshot() => PreviewFrame is not null;

    private void UpdatePreviewTransforms()
    {
        _previewService.TransformOptions = new FrameTransformOptions(IsMirrored, RotationDegrees);
    }

    private void UpdateMeasurementStatus()
    {
        var measurement = Annotations.LastOrDefault(annotation => annotation.Tool is InspectionTool.Distance or InspectionTool.Angle && annotation.Points.Count >= 2);
        if (measurement is null)
        {
            MeasurementStatus = SelectedCalibrationProfile is null ? "Uncalibrated" : "No measurement";
            return;
        }

        var result = _calibrationCalculator.MeasureDistance(measurement.Points[0], measurement.Points[^1], SelectedCalibrationProfile);
        MeasurementStatus = result.IsCalibrated && result.RealLength is not null
            ? $"{result.RealLength:0.###} {UnitLabel(result.Units)} at {result.AngleDegrees:0.#} deg"
            : "Uncalibrated";
    }

    private InspectionDocument CreateInspectionDocument(string? cleanFramePath, string? annotatedFramePath) => new()
    {
        CleanFramePath = cleanFramePath,
        AnnotatedFramePath = annotatedFramePath,
        CameraId = SelectedCamera?.Id,
        Format = SelectedFormat?.DisplayName,
        CalibrationStatus = CalibrationStatus,
        CalibrationProfile = SelectedCalibrationProfile,
        Annotations = [.. Annotations],
        Measurements = Annotations
            .Where(annotation => annotation.IsMeasurement && annotation.Points.Count >= 2)
            .Select(annotation => _calibrationCalculator.MeasureDistance(annotation.Points[0], annotation.Points[^1], SelectedCalibrationProfile))
            .ToList()
    };

    private void ReplaceCalibrationProfile(CalibrationProfile oldProfile, CalibrationProfile newProfile)
    {
        var index = CalibrationProfiles.IndexOf(oldProfile);
        if (index >= 0)
        {
            CalibrationProfiles[index] = newProfile;
        }
    }

    private void SaveCalibrationProfiles() => _calibrationProfileStore.Save(CalibrationProfiles);

    private static string UnitLabel(InspectionUnits units) => units == InspectionUnits.Micrometres ? "um" : "mm";

    private static int NormalizeRotation(int value) => ((value % 360) + 360) % 360;
}
