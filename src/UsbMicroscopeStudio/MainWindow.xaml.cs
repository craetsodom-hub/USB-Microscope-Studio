using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Services;
using UsbMicroscopeStudio.ViewModels;

namespace UsbMicroscopeStudio;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WindowFullscreenStateController _fullscreenStateController;

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
        Directory.CreateDirectory(_viewModel.SnapshotFolderPath);
        var path = Path.Combine(_viewModel.SnapshotFolderPath, $"annotated-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.png");
        var bitmap = new RenderTargetBitmap((int)Math.Max(1, PreviewHost.ActualWidth), (int)Math.Max(1, PreviewHost.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(PreviewHost);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
        _viewModel.SaveInspectionSidecarForAnnotatedFrame(path);
    }
}
