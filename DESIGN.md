# Design

## UI Direction

The interface is built for technicians who repeatedly inspect live video, not for marketing presentation. It uses a restrained neutral palette, compact controls, visible status text, and a large preview area.

## Layout

- Top command bar: refresh, start, stop, snapshot, fullscreen.
- Left control rail: camera, format, capture actions, snapshot folder selector, zoom, rotate, mirror, freeze, session readouts.
- Inspection controls: Freeze & Inspect, crosshair, grid spacing, rulers, annotation tool, color, stroke thickness, undo/redo/clear, calibration profile management, and inspection saves.
- Main workspace: high-contrast preview area with scroll support when zoomed.
- Bottom status bar: current camera/preview state and reconnect messages.

## Interaction Notes

- Demo Mode appears in the camera selector so QA can run the app without hardware.
- The app auto-selects the first real camera when present; otherwise it starts Demo Mode.
- The snapshot folder selector displays the active capture path and persists it between launches.
- Overlays are vector drawn on top of the frame inside the same preview transform so they remain aligned while zooming, rotating, mirroring, changing resolution, and entering fullscreen.
- Measurements clearly display `Uncalibrated` until a calibration profile is active.
- Escape exits fullscreen.
- Freeze pauses UI frame updates while keeping the preview service alive.

## Automation IDs

Automation IDs are assigned to primary controls, including `CameraSelector`, `FormatSelector`, `LivePreviewImage`, `InspectionOverlayCanvas`, `SnapshotButton`, `ChooseSnapshotFolderButton`, `SnapshotFolderPathText`, `FreezeInspectToggle`, `CrosshairToggle`, `GridToggle`, `RulersToggle`, `AnnotationToolSelector`, `StrokeColorSelector`, `StrokeThicknessSlider`, `CalibrationProfileSelector`, `CalibrationStatusText`, `MeasurementStatusText`, `MirrorToggle`, `FullscreenButton`, and `StatusText`.
