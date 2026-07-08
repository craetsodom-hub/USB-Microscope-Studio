using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using UsbMicroscopeStudio.Services;
using UsbMicroscopeStudio.ViewModels;

namespace UsbMicroscopeStudio;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

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
        if (isFullscreen)
        {
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            return;
        }

        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;
        WindowState = _previousWindowState;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsFullscreen)
        {
            _viewModel.IsFullscreen = false;
        }
    }
}
