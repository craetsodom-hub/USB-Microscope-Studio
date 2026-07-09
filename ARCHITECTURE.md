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
- `InspectionOverlayControl`: vector overlay renderer and lightweight annotation interaction layer over the preview frame.
- `InspectionGeometry`: normalized image-space coordinate mapping, frame-transform compensation, and three-point angle calculations.
- `InspectionFrameRenderer`: native-dimension annotated PNG rendering that does not depend on the visible WPF viewport.
- `CalibrationCalculator`: pixel distance, angle, and calibrated real-world measurement calculations from normalized inspection points and active frame dimensions.
- `JsonCalibrationProfileStore`: persisted calibration profiles by camera, resolution, and profile name.
- `AnnotationHistory`: undo/redo snapshots for non-destructive annotation edits.
- `AnnotationSerializer`: JSON sidecar save/load for editable inspections.
- `InspectionSessionStore`: session folder creation and session JSON save/load.
- `JsonRecentSessionStore`: persisted recent inspection sessions.
- `HtmlInspectionReportService`: offline HTML report generation, report path creation, image copying, and HTML escaping.

## Phase 2 Inspection Model

Annotations are stored separately from the camera frame as `InspectionAnnotation` records. Annotation points are normalized to image space rather than stored as viewport pixels. When the preview image itself is mirrored or rotated by the camera pipeline, the view model transforms annotation points once so existing marks remain attached to the same visual image locations. UI zoom, fullscreen, and resolution changes do not mutate annotation coordinates.

Clean snapshots, rendered annotated PNGs, and JSON sidecars are separate artifacts so the camera frame remains non-destructive and inspection data can be reopened later. Opening an inspection restores the clean frame, editable annotations, measurements, and a calibration profile only when that profile matches the active camera id and resolution/FPS.

Calibration profiles are not global. They include camera id, selected resolution/FPS, profile name, units, and units-per-pixel. The view model exposes only profiles matching the active camera and format, clears calibration on camera/format mismatch, and never passes a mismatched profile into real-world measurement calculations.

## Phase 3A Session Model

Inspection sessions are stored under a user-selected workspace folder. Each session folder is named with the inspection date/time and a safe session name, then contains `clean-frames`, `annotated-frames`, and `sidecars`. The canonical session file is `sidecars/session.json`.

Session JSON stores technician metadata, project/customer/device fields, clean and annotated frame paths, its own `SessionJsonPath`, the latest editable inspection sidecar path, calibration status/profile reference, annotations, and measurements. The session file remains `sidecars/session.json`; editable Phase 2 inspection sidecars remain separate `sidecars/inspection-*.json` files and can still be opened directly.

## Phase 3B HTML Reports

HTML reports are generated from an `InspectionSessionDocument`, not from WPF visual state. Reports are saved inside the current session folder under `reports/report-YYYYMMDD-HHMMSS.html`. Available clean and annotated frame images are copied into `reports/assets` and referenced with relative paths so the report remains readable offline when opened later.

The template is deterministic, self-contained, and uses inline CSS only. User-entered metadata, notes, and annotation text are HTML-escaped before rendering.

## Reconnect Strategy

The preview service treats failed reads and closed capture devices as transient disconnects. It releases the capture handle, reports status, waits briefly, and retries the same camera index until stopped or reconnected.
