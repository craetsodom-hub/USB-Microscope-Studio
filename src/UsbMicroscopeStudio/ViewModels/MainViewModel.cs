using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ICameraCatalog _cameraCatalog;
    private readonly ICameraPreviewService _previewService;
    private readonly ISnapshotService _snapshotService;
    private readonly IUiDispatcher _uiDispatcher;
    private bool _suppressFormatLoad;

    public MainViewModel(
        ICameraCatalog cameraCatalog,
        ICameraPreviewService previewService,
        ISnapshotService snapshotService,
        IUiDispatcher uiDispatcher)
    {
        _cameraCatalog = cameraCatalog;
        _previewService = previewService;
        _snapshotService = snapshotService;
        _uiDispatcher = uiDispatcher;

        _previewService.FrameReady += OnFrameReady;
        _previewService.StatusChanged += OnStatusChanged;
    }

    public ObservableCollection<CameraDevice> Cameras { get; } = [];

    public ObservableCollection<CameraFormat> Formats { get; } = [];

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
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string? lastSnapshotPath;

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

        UpdatePreviewTransforms();
        await _previewService.StartAsync(SelectedCamera, SelectedFormat);
        IsPreviewing = true;
    }

    [RelayCommand(CanExecute = nameof(IsPreviewing))]
    public async Task StopPreviewAsync()
    {
        await _previewService.StopAsync();
        IsPreviewing = false;
    }

    [RelayCommand(CanExecute = nameof(CanSnapshot))]
    public void Snapshot()
    {
        if (PreviewFrame is null)
        {
            return;
        }

        LastSnapshotPath = _snapshotService.SaveSnapshot(PreviewFrame);
        StatusMessage = $"Snapshot saved: {LastSnapshotPath}";
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

    public void Dispose()
    {
        _previewService.FrameReady -= OnFrameReady;
        _previewService.StatusChanged -= OnStatusChanged;
        _previewService.Dispose();
    }

    private async Task LoadFormatsForSelectionAsync(CameraDevice? camera)
    {
        Formats.Clear();
        if (camera is null)
        {
            return;
        }

        StatusMessage = $"Loading formats for {camera.DisplayName}...";
        var formats = await _cameraCatalog.GetFormatsAsync(camera);
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
            }
        });
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

    private static int NormalizeRotation(int value) => ((value % 360) + 360) % 360;
}
