using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Models.Sessions;
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
    private readonly InspectionSessionStore _sessionStore;
    private readonly IRecentSessionStore _recentSessionStore;
    private readonly HtmlInspectionReportService _htmlReportService;
    private readonly SemaphoreSlim _previewGate = new(1, 1);
    private int _formatLoadRequestId;
    private int _previewStartRequestId;
    private bool _suppressFormatLoad;
    private int _annotationRotationDegrees;
    private bool _annotationMirrored;

    public MainViewModel(
        ICameraCatalog cameraCatalog,
        ICameraPreviewService previewService,
        ISnapshotService snapshotService,
        IAppSettingsStore settingsStore,
        IFolderPickerService folderPickerService,
        IUiDispatcher uiDispatcher,
        ICalibrationProfileStore? calibrationProfileStore = null,
        AnnotationSerializer? annotationSerializer = null,
        InspectionSessionStore? sessionStore = null,
        IRecentSessionStore? recentSessionStore = null,
        HtmlInspectionReportService? htmlReportService = null)
    {
        _cameraCatalog = cameraCatalog;
        _previewService = previewService;
        _snapshotService = snapshotService;
        _settingsStore = settingsStore;
        _folderPickerService = folderPickerService;
        _uiDispatcher = uiDispatcher;
        _calibrationProfileStore = calibrationProfileStore ?? new JsonCalibrationProfileStore();
        _annotationSerializer = annotationSerializer ?? new AnnotationSerializer();
        _sessionStore = sessionStore ?? new InspectionSessionStore();
        _recentSessionStore = recentSessionStore ?? new JsonRecentSessionStore();
        _htmlReportService = htmlReportService ?? new HtmlInspectionReportService();

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

        foreach (var session in _recentSessionStore.Load())
        {
            RecentSessions.Add(session);
        }
    }

    public ObservableCollection<CameraDevice> Cameras { get; } = [];

    public ObservableCollection<CameraFormat> Formats { get; } = [];

    public ObservableCollection<InspectionAnnotation> Annotations { get; } = [];

    public ObservableCollection<CalibrationProfile> CalibrationProfiles { get; } = [];

    public ObservableCollection<CalibrationProfile> VisibleCalibrationProfiles { get; } = [];

    public ObservableCollection<RecentSessionEntry> RecentSessions { get; } = [];

    public string CustomerDisplay => DisplayOrNotSpecified(CustomerName);

    public string DeviceDisplay => DisplayOrNotSpecified(DeviceModel);

    public string CurrentSessionJsonDisplay => string.IsNullOrWhiteSpace(CurrentSessionJsonPath) ? "Unsaved" : "Saved";

    public string LastReportDisplay => string.IsNullOrWhiteSpace(LastReportPath) ? "Not exported" : "Ready";

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
    private string sessionName = "Untitled inspection";

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private string deviceModel = string.Empty;

    [ObservableProperty]
    private string serialAssetTag = string.Empty;

    [ObservableProperty]
    private string technicianName = string.Empty;

    [ObservableProperty]
    private string jobOrderNumber = string.Empty;

    [ObservableProperty]
    private string sessionNotes = string.Empty;

    [ObservableProperty]
    private DateTimeOffset inspectionDateTime = DateTimeOffset.Now;

    [ObservableProperty]
    private string workspaceFolderPath = string.Empty;

    [ObservableProperty]
    private string? currentSessionFolderPath;

    [ObservableProperty]
    private string? currentSessionJsonPath;

    [ObservableProperty]
    private string? annotatedFramePath;

    [ObservableProperty]
    private string? inspectionJsonSidecarPath;

    [ObservableProperty]
    private string? lastReportPath;

    [ObservableProperty]
    private RecentSessionEntry? selectedRecentSession;

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
    [NotifyCanExecuteChangedFor(nameof(UndoAnnotationsCommand))]
    private bool canUndoAnnotations;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RedoAnnotationsCommand))]
    private bool canRedoAnnotations;

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
        RefreshMatchingCalibrationProfiles();
        if (!_suppressFormatLoad)
        {
            _ = LoadFormatsForSelectionAsync(value);
        }
    }

    partial void OnSelectedFormatChanged(CameraFormat? value)
    {
        RefreshMatchingCalibrationProfiles();
        if (IsPreviewing && SelectedCamera is not null && value is not null)
        {
            _ = StartPreviewAsync();
        }
    }

    partial void OnIsMirroredChanged(bool value)
    {
        UpdatePreviewTransforms();
        SyncAnnotationsToDisplayedFrameTransform();
    }

    partial void OnRotationDegreesChanged(int value)
    {
        UpdatePreviewTransforms();
        SyncAnnotationsToDisplayedFrameTransform();
    }

    partial void OnSnapshotFolderPathChanged(string value)
    {
        _settingsStore.Save(new AppSettings(value));
    }

    partial void OnCustomerNameChanged(string value) => OnPropertyChanged(nameof(CustomerDisplay));

    partial void OnDeviceModelChanged(string value) => OnPropertyChanged(nameof(DeviceDisplay));

    partial void OnCurrentSessionJsonPathChanged(string? value) => OnPropertyChanged(nameof(CurrentSessionJsonDisplay));

    partial void OnLastReportPathChanged(string? value) => OnPropertyChanged(nameof(LastReportDisplay));

    partial void OnSelectedCalibrationProfileChanged(CalibrationProfile? value)
    {
        if (value is not null && !MatchesCurrentCameraFormat(value))
        {
            SelectedCalibrationProfile = null;
            return;
        }

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
        var selectedFolder = _folderPickerService.PickFolder(SnapshotFolderPath, "Select snapshot folder");
        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        SnapshotFolderPath = selectedFolder;
        StatusMessage = $"Snapshot folder: {SnapshotFolderPath}";
    }

    [RelayCommand]
    public void NewSession()
    {
        SessionName = "Untitled inspection";
        CustomerName = string.Empty;
        DeviceModel = string.Empty;
        SerialAssetTag = string.Empty;
        TechnicianName = string.Empty;
        JobOrderNumber = string.Empty;
        SessionNotes = string.Empty;
        InspectionDateTime = DateTimeOffset.Now;
        WorkspaceFolderPath = string.Empty;
        CurrentSessionFolderPath = null;
        CurrentSessionJsonPath = null;
        LastSnapshotPath = null;
        AnnotatedFramePath = null;
        InspectionJsonSidecarPath = null;
        Annotations.Clear();
        SelectedCalibrationProfile = null;
        StatusMessage = "New inspection session";
    }

    [RelayCommand]
    public void SaveSession()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceFolderPath))
        {
            SaveSessionAs();
            return;
        }

        SaveSessionToWorkspace(WorkspaceFolderPath);
    }

    [RelayCommand]
    public void SaveSessionAs()
    {
        var initialFolder = string.IsNullOrWhiteSpace(WorkspaceFolderPath) ? SnapshotFolderPath : WorkspaceFolderPath;
        var selectedFolder = _folderPickerService.PickFolder(initialFolder, "Select session workspace folder");
        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        CurrentSessionFolderPath = null;
        SaveSessionToWorkspace(selectedFolder);
    }

    [RelayCommand]
    public void OpenRecentSession()
    {
        if (SelectedRecentSession is null || string.IsNullOrWhiteSpace(SelectedRecentSession.SessionPath))
        {
            return;
        }

        OpenSession(SelectedRecentSession.SessionPath);
    }

    [RelayCommand]
    public void ExportHtmlReport()
    {
        if (string.IsNullOrWhiteSpace(CurrentSessionFolderPath))
        {
            SaveSession();
            if (string.IsNullOrWhiteSpace(CurrentSessionFolderPath))
            {
                return;
            }
        }
        else if (!string.IsNullOrWhiteSpace(WorkspaceFolderPath))
        {
            SaveSessionToWorkspace(WorkspaceFolderPath);
        }

        try
        {
            var result = _htmlReportService.Export(CreateSessionDocument(WorkspaceFolderPath));
            LastReportPath = result.ReportPath;
            StatusMessage = $"HTML report exported: {LastReportPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"HTML report export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenLastReport()
    {
        if (string.IsNullOrWhiteSpace(LastReportPath) || !File.Exists(LastReportPath))
        {
            StatusMessage = "No exported report is available.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(LastReportPath) { UseShellExecute = true });
            StatusMessage = $"Opened report: {LastReportPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Open report failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CaptureAnnotationHistory()
    {
        _annotationHistory.Capture(Annotations);
        UpdateAnnotationCommandState();
    }

    [RelayCommand(CanExecute = nameof(CanUndoAnnotations))]
    public void UndoAnnotations()
    {
        _annotationHistory.Undo(Annotations);
        UpdateAnnotationCommandState();
        UpdateMeasurementStatus();
    }

    [RelayCommand(CanExecute = nameof(CanRedoAnnotations))]
    public void RedoAnnotations()
    {
        _annotationHistory.Redo(Annotations);
        UpdateAnnotationCommandState();
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
        UpdateAnnotationCommandState();
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
                PreviewWidth,
                PreviewHeight,
                KnownCalibrationLength,
                SelectedUnits);

            CalibrationProfiles.Add(profile);
            RefreshMatchingCalibrationProfiles();
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
        RefreshMatchingCalibrationProfiles();
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
        RefreshMatchingCalibrationProfiles();
        SaveCalibrationProfiles();
    }

    [RelayCommand]
    public void SaveCleanFrame()
    {
        var cleanFramesFolder = GetCleanFramesFolderPath();
        if (!string.IsNullOrWhiteSpace(cleanFramesFolder))
        {
            SnapshotFolderPath = cleanFramesFolder;
        }

        Snapshot();
    }

    [RelayCommand]
    public void SaveInspectionSidecar()
    {
        var sidecarFolder = GetSidecarsFolderPath() ?? SnapshotFolderPath;
        var sidecarPath = Path.Combine(sidecarFolder, $"inspection-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json");
        var document = CreateInspectionDocument(LastSnapshotPath, null);
        _annotationSerializer.Save(sidecarPath, document);
        InspectionJsonSidecarPath = sidecarPath;
        StatusMessage = $"Inspection sidecar saved: {sidecarPath}";
    }

    public void SaveInspectionSidecarForAnnotatedFrame(string annotatedFramePath)
    {
        AnnotatedFramePath = annotatedFramePath;
        var sidecarFolder = GetSidecarsFolderPath();
        var sidecarPath = string.IsNullOrWhiteSpace(sidecarFolder)
            ? Path.ChangeExtension(annotatedFramePath, ".json")
            : Path.Combine(sidecarFolder, $"{Path.GetFileNameWithoutExtension(annotatedFramePath)}.json");
        var document = CreateInspectionDocument(LastSnapshotPath, annotatedFramePath);
        _annotationSerializer.Save(sidecarPath, document);
        InspectionJsonSidecarPath = sidecarPath;
        StatusMessage = $"Annotated frame saved: {annotatedFramePath}";
    }

    public void OpenInspection(string sidecarPath)
    {
        var document = _annotationSerializer.Load(sidecarPath);
        Annotations.Clear();
        foreach (var annotation in document.Annotations)
        {
            Annotations.Add(annotation);
        }

        LastSnapshotPath = document.CleanFramePath;
        if (!string.IsNullOrWhiteSpace(document.CleanFramePath) && File.Exists(document.CleanFramePath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(document.CleanFramePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewFrame = bitmap;
            PreviewWidth = bitmap.PixelWidth;
            PreviewHeight = bitmap.PixelHeight;
        }

        if (document.CalibrationProfile is not null && MatchesCurrentCameraFormat(document.CalibrationProfile))
        {
            var existing = CalibrationProfiles.FirstOrDefault(profile => profile.ProfileKey == document.CalibrationProfile.ProfileKey);
            if (existing is null)
            {
                CalibrationProfiles.Add(document.CalibrationProfile);
                existing = document.CalibrationProfile;
                SaveCalibrationProfiles();
            }

            RefreshMatchingCalibrationProfiles();
            SelectedCalibrationProfile = existing;
        }
        else
        {
            RefreshMatchingCalibrationProfiles();
            SelectedCalibrationProfile = null;
        }

        UpdateMeasurementStatus();
        StatusMessage = $"Inspection opened: {sidecarPath}";
    }

    public string GetAnnotatedFrameFolderPath() => GetAnnotatedFramesFolderPath() ?? SnapshotFolderPath;

    public void OpenSession(string sessionPath)
    {
        var session = _sessionStore.Load(sessionPath);
        ApplySession(session);
        AddRecentSession(session.SessionName, sessionPath);
        StatusMessage = $"Session opened: {sessionPath}";
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
                SyncAnnotationsToDisplayedFrameTransform();
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

    private void SyncAnnotationsToDisplayedFrameTransform()
    {
        if (IsFrozen)
        {
            return;
        }

        var rotationChanged = NormalizeRotation(RotationDegrees) != NormalizeRotation(_annotationRotationDegrees);
        var mirrorChanged = IsMirrored != _annotationMirrored;
        if (Annotations.Count > 0 && (rotationChanged || mirrorChanged))
        {
            _annotationHistory.Capture(Annotations);
            for (var i = 0; i < Annotations.Count; i++)
            {
                var transformedPoints = Annotations[i].Points
                    .Select(point => InspectionGeometry.TransformBetweenPreviewTransforms(
                        point,
                        _annotationMirrored,
                        _annotationRotationDegrees,
                        IsMirrored,
                        RotationDegrees))
                    .ToList();
                Annotations[i] = Annotations[i] with { Points = transformedPoints };
            }

            UpdateAnnotationCommandState();
        }

        _annotationRotationDegrees = NormalizeRotation(RotationDegrees);
        _annotationMirrored = IsMirrored;
        UpdateMeasurementStatus();
    }

    private void UpdateMeasurementStatus()
    {
        var measurement = Annotations.LastOrDefault(annotation => annotation.Tool is InspectionTool.Distance or InspectionTool.Angle && annotation.Points.Count >= 2);
        if (measurement is null)
        {
            MeasurementStatus = ActiveCalibrationProfile() is null ? "Uncalibrated" : "No measurement";
            return;
        }

        if (measurement.Tool == InspectionTool.Angle && measurement.Points.Count >= 3)
        {
            var angle = InspectionGeometry.ThreePointAngleDegrees(measurement.Points[0], measurement.Points[1], measurement.Points[2], PreviewWidth, PreviewHeight);
            MeasurementStatus = ActiveCalibrationProfile() is null ? $"Angle {angle:0.#} deg - Uncalibrated" : $"Angle {angle:0.#} deg";
            return;
        }

        var result = _calibrationCalculator.MeasureDistance(measurement.Points[0], measurement.Points[^1], PreviewWidth, PreviewHeight, ActiveCalibrationProfile());
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
        CalibrationProfile = ActiveCalibrationProfile(),
        Annotations = [.. Annotations],
        Measurements = Annotations
            .Where(annotation => annotation.IsMeasurement && annotation.Points.Count >= 2)
            .Select(annotation => annotation.Tool == InspectionTool.Angle && annotation.Points.Count >= 3
                ? new MeasurementResult(
                    InspectionGeometry.PixelDistance(annotation.Points[0], annotation.Points[^1], PreviewWidth, PreviewHeight),
                    null,
                    InspectionGeometry.ThreePointAngleDegrees(annotation.Points[0], annotation.Points[1], annotation.Points[2], PreviewWidth, PreviewHeight),
                    ActiveCalibrationProfile()?.Units ?? InspectionUnits.Millimetres,
                    ActiveCalibrationProfile() is not null)
                : _calibrationCalculator.MeasureDistance(annotation.Points[0], annotation.Points[^1], PreviewWidth, PreviewHeight, ActiveCalibrationProfile()))
            .ToList()
    };

    private InspectionSessionDocument CreateSessionDocument(string workspaceFolder)
    {
        var inspection = CreateInspectionDocument(LastSnapshotPath, AnnotatedFramePath);
        return new InspectionSessionDocument
        {
            SessionName = string.IsNullOrWhiteSpace(SessionName) ? "Untitled inspection" : SessionName,
            CustomerName = CustomerName,
            DeviceModel = DeviceModel,
            SerialAssetTag = SerialAssetTag,
            TechnicianName = TechnicianName,
            JobOrderNumber = JobOrderNumber,
            Notes = SessionNotes,
            InspectionDateTime = InspectionDateTime,
            WorkspaceFolderPath = workspaceFolder,
            SessionFolderPath = CurrentSessionFolderPath,
            SessionJsonPath = CurrentSessionJsonPath,
            CleanFramePath = inspection.CleanFramePath,
            AnnotatedFramePath = inspection.AnnotatedFramePath,
            InspectionJsonSidecarPath = InspectionJsonSidecarPath,
            CalibrationStatus = inspection.CalibrationStatus,
            CalibrationProfileKey = inspection.CalibrationProfile?.ProfileKey,
            CalibrationProfile = inspection.CalibrationProfile,
            Annotations = inspection.Annotations,
            Measurements = inspection.Measurements
        };
    }

    private void SaveSessionToWorkspace(string workspaceFolder)
    {
        try
        {
            var saved = _sessionStore.Save(CreateSessionDocument(workspaceFolder), workspaceFolder);
            ApplySessionPaths(saved);
            AddRecentSession(saved.SessionName, saved.SessionJsonPath ?? CurrentSessionJsonPath ?? string.Empty);
            StatusMessage = $"Session saved: {CurrentSessionJsonPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Session save failed: {ex.Message}";
        }
    }

    private void ApplySession(InspectionSessionDocument session)
    {
        SessionName = session.SessionName;
        CustomerName = session.CustomerName ?? string.Empty;
        DeviceModel = session.DeviceModel ?? string.Empty;
        SerialAssetTag = session.SerialAssetTag ?? string.Empty;
        TechnicianName = session.TechnicianName ?? string.Empty;
        JobOrderNumber = session.JobOrderNumber ?? string.Empty;
        SessionNotes = session.Notes ?? string.Empty;
        InspectionDateTime = session.InspectionDateTime;
        LastSnapshotPath = session.CleanFramePath;
        AnnotatedFramePath = session.AnnotatedFramePath;
        InspectionJsonSidecarPath = session.InspectionJsonSidecarPath;

        ApplySessionPaths(session);

        Annotations.Clear();
        foreach (var annotation in session.Annotations)
        {
            Annotations.Add(annotation);
        }

        if (!string.IsNullOrWhiteSpace(session.CleanFramePath) && File.Exists(session.CleanFramePath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(session.CleanFramePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewFrame = bitmap;
            PreviewWidth = bitmap.PixelWidth;
            PreviewHeight = bitmap.PixelHeight;
        }

        if (session.CalibrationProfile is not null && MatchesCurrentCameraFormat(session.CalibrationProfile))
        {
            var existing = CalibrationProfiles.FirstOrDefault(profile => profile.ProfileKey == session.CalibrationProfile.ProfileKey);
            if (existing is null)
            {
                CalibrationProfiles.Add(session.CalibrationProfile);
                existing = session.CalibrationProfile;
                SaveCalibrationProfiles();
            }

            RefreshMatchingCalibrationProfiles();
            SelectedCalibrationProfile = existing;
        }
        else
        {
            SelectedCalibrationProfile = null;
            CalibrationStatus = session.CalibrationStatus;
            UpdateMeasurementStatus();
        }
    }

    private void ApplySessionPaths(InspectionSessionDocument session)
    {
        WorkspaceFolderPath = session.WorkspaceFolderPath ?? WorkspaceFolderPath;
        CurrentSessionFolderPath = session.SessionFolderPath;
        CurrentSessionJsonPath = session.SessionJsonPath;
        if (!string.IsNullOrWhiteSpace(CurrentSessionFolderPath))
        {
            var paths = _sessionStore.EnsureSessionFolders(CurrentSessionFolderPath);
            CurrentSessionJsonPath = paths.SessionJsonPath;
            SnapshotFolderPath = paths.CleanFramesFolder;
        }
    }

    private string? GetCleanFramesFolderPath() =>
        string.IsNullOrWhiteSpace(CurrentSessionFolderPath)
            ? null
            : _sessionStore.EnsureSessionFolders(CurrentSessionFolderPath).CleanFramesFolder;

    private string? GetAnnotatedFramesFolderPath() =>
        string.IsNullOrWhiteSpace(CurrentSessionFolderPath)
            ? null
            : _sessionStore.EnsureSessionFolders(CurrentSessionFolderPath).AnnotatedFramesFolder;

    private string? GetSidecarsFolderPath() =>
        string.IsNullOrWhiteSpace(CurrentSessionFolderPath)
            ? null
            : _sessionStore.EnsureSessionFolders(CurrentSessionFolderPath).SidecarsFolder;

    private void AddRecentSession(string sessionName, string sessionPath)
    {
        if (string.IsNullOrWhiteSpace(sessionPath))
        {
            return;
        }

        var existing = RecentSessions.FirstOrDefault(session => string.Equals(session.SessionPath, sessionPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentSessions.Remove(existing);
        }

        RecentSessions.Insert(0, new RecentSessionEntry
        {
            SessionName = string.IsNullOrWhiteSpace(sessionName) ? "Untitled inspection" : sessionName,
            SessionPath = sessionPath,
            LastOpenedAt = DateTimeOffset.Now
        });

        while (RecentSessions.Count > 10)
        {
            RecentSessions.RemoveAt(RecentSessions.Count - 1);
        }

        _recentSessionStore.Save(RecentSessions);
    }

    private static string DisplayOrNotSpecified(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not specified" : value;

    private void ReplaceCalibrationProfile(CalibrationProfile oldProfile, CalibrationProfile newProfile)
    {
        var index = CalibrationProfiles.IndexOf(oldProfile);
        if (index >= 0)
        {
            CalibrationProfiles[index] = newProfile;
        }
    }

    private void RefreshMatchingCalibrationProfiles()
    {
        var selectedKey = SelectedCalibrationProfile?.ProfileKey;
        VisibleCalibrationProfiles.Clear();
        foreach (var profile in CalibrationProfiles.Where(MatchesCurrentCameraFormat))
        {
            VisibleCalibrationProfiles.Add(profile);
        }

        SelectedCalibrationProfile = VisibleCalibrationProfiles.FirstOrDefault(profile => profile.ProfileKey == selectedKey);
    }

    private CalibrationProfile? ActiveCalibrationProfile() =>
        MatchesCurrentCameraFormat(SelectedCalibrationProfile) ? SelectedCalibrationProfile : null;

    private bool MatchesCurrentCameraFormat(CalibrationProfile? profile) =>
        profile is not null &&
        SelectedCamera is not null &&
        SelectedFormat is not null &&
        profile.CameraId == SelectedCamera.Id &&
        FormatsMatch(profile.Format, SelectedFormat);

    private static bool FormatsMatch(CameraFormat left, CameraFormat right) =>
        left.Width == right.Width &&
        left.Height == right.Height &&
        Math.Abs(left.FramesPerSecond - right.FramesPerSecond) < 0.001 &&
        string.Equals(left.PixelFormat, right.PixelFormat, StringComparison.OrdinalIgnoreCase);

    private void UpdateAnnotationCommandState()
    {
        CanUndoAnnotations = _annotationHistory.CanUndo;
        CanRedoAnnotations = _annotationHistory.CanRedo;
    }

    private void SaveCalibrationProfiles() => _calibrationProfileStore.Save(CalibrationProfiles);

    private static string UnitLabel(InspectionUnits units) => units == InspectionUnits.Micrometres ? "um" : "mm";

    private static int NormalizeRotation(int value) => ((value % 360) + 360) % 360;
}
