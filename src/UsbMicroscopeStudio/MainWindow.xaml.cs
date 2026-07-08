using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;
using UsbMicroscopeStudio.ViewModels;

namespace UsbMicroscopeStudio;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WindowFullscreenStateController _fullscreenStateController;
    private readonly InspectionFrameRenderer _inspectionFrameRenderer = new();

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(
            new DirectShowCameraCatalog(),
            new OpenCvCameraPreviewService(),
            new SnapshotService(),
            new JsonAppSettingsStore(),
            new WpfFolderPickerService(),
            new WpfUiDispatcher());

        DataContext = _viewModel;
        _fullscreenStateController = new WindowFullscreenStateController(new WpfWindowStateAdapter(this));
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshCamerasAsync();
        await _viewModel.StartPreviewAsync();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsFullscreen))
        {
            ApplyFullscreen(_viewModel.IsFullscreen);
        }
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        _fullscreenStateController.SetFullscreen(isFullscreen);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsFullscreen)
        {
            _viewModel.IsFullscreen = false;
        }
    }

    private void SaveAnnotatedFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.PreviewFrame is null)
        {
            _viewModel.StatusMessage = "No frame is available to save.";
            return;
        }

        Directory.CreateDirectory(_viewModel.SnapshotFolderPath);
        var path = GetAvailablePath(Path.Combine(_viewModel.SnapshotFolderPath, $"annotated-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.png"));
        var bitmap = _inspectionFrameRenderer.RenderAnnotatedFrame(
            _viewModel.PreviewFrame,
            _viewModel.Annotations.ToList(),
            new OverlayOptions(_viewModel.ShowCrosshair, _viewModel.ShowGrid, _viewModel.GridSpacingPixels, _viewModel.ShowRulers));

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
        _viewModel.SaveInspectionSidecarForAnnotatedFrame(path);
    }

    private void OpenInspection_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open inspection",
            Filter = "Inspection sidecar (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(_viewModel.SnapshotFolderPath) ? _viewModel.SnapshotFolderPath : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.OpenInspection(dialog.FileName);
        }
    }

    private static string GetAvailablePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name}-{index}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
