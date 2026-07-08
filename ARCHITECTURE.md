# Architecture

## Platform

- .NET 8 WPF.
- MVVM via CommunityToolkit.Mvvm.
- DirectShow device and capability discovery via DirectShowLib.
- Live frame acquisition via OpenCvSharp.

## Projects

- `src/UsbMicroscopeStudio`: WPF application.
- `tests/UsbMicroscopeStudio.Tests`: xUnit tests for view-model behavior and hardware-free workflows.

## Runtime Flow

1. `MainWindow` creates `MainViewModel` with service interfaces.
2. `DirectShowCameraCatalog` enumerates video input devices and supported formats.
3. Demo Mode is always available as a synthetic camera source.
4. `OpenCvCameraPreviewService` starts a background preview loop.
5. Frames are converted to frozen `BitmapSource` instances and marshalled to the UI.
6. Camera read failures transition to reconnect polling instead of throwing through the UI.
7. `JsonAppSettingsStore` loads the persisted snapshot folder from the user's roaming app data.
8. `SnapshotService` persists the current preview frame as PNG in the selected folder, with collision-safe filenames and temp-folder fallback.

## Key Boundaries

- `ICameraCatalog`: camera and format discovery.
- `ICameraPreviewService`: live frame lifecycle and transforms.
- `ISnapshotService`: still image persistence.
- `IAppSettingsStore`: persisted app preferences, currently the snapshot folder path.
- `IFolderPickerService`: UI shell abstraction for selecting snapshot folders.
- `IUiDispatcher`: UI-thread abstraction for testability.

## Reconnect Strategy

The preview service treats failed reads and closed capture devices as transient disconnects. It releases the capture handle, reports status, waits briefly, and retries the same camera index until stopped or reconnected.
