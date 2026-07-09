# Design

## UI Direction

The interface is built for technicians who repeatedly inspect live video, not for marketing presentation. It uses a restrained neutral palette, compact controls, visible status text, and a large preview area.

## Layout

- Top command bar: refresh, start, stop, snapshot, fullscreen.
- Left control rail: camera, format, capture actions, snapshot folder selector, zoom, rotate, mirror, freeze, session readouts.
- Phase 3A adds compact session actions and metadata fields to the existing rail without a full redesign.
- Phase 3B adds compact report actions to the session area without changing the overall layout.
- Inspection controls: Freeze & Inspect, crosshair, grid spacing, rulers, annotation tool, color, stroke thickness, undo/redo/clear, calibration profile management, and inspection saves.
- Main workspace: high-contrast preview area with scroll support when zoomed.
- Bottom status bar: current camera/preview state and reconnect messages.

## Interaction Notes

- Demo Mode appears in the camera selector so QA can run the app without hardware.
- The app auto-selects the first real camera when present; otherwise it starts Demo Mode.
- The snapshot folder selector displays the active capture path and persists it between launches.
- Session controls provide New, Save, Save As, Open, recent session selection, and editable metadata for project, customer, device, serial/asset tag, technician, job/order number, and notes.
- Active sessions redirect clean frames, annotated frames, and sidecars into the session folder structure.
- Export HTML Report saves an offline technician report into the active session's `reports` folder and Open Last Report opens the most recent export.
- Overlays are vector drawn from normalized image coordinates so they remain sharp and aligned while zooming, rotating, mirroring, changing resolution, and entering fullscreen.
- Angle annotations use three operator clicks: first ray endpoint, vertex, and second ray endpoint. The app renders both rays, an arc, and the angle in degrees.
- Text annotations prompt for operator-entered text and can be edited by selecting and double-clicking the text mark.
- Measurements clearly display `Uncalibrated` until a calibration profile matching the current camera and format is active.
- Open inspection restores a JSON sidecar's clean frame and editable annotations. Calibration is restored only when it is valid for the current camera and format.
- Escape exits fullscreen.
- Freeze pauses UI frame updates while keeping the preview service alive.

## Automation IDs

Automation IDs are assigned to primary controls, including `CameraSelector`, `FormatSelector`, `LivePreviewImage`, `InspectionOverlayCanvas`, `SnapshotButton`, `ChooseSnapshotFolderButton`, `SnapshotFolderPathText`, `NewSessionButton`, `SaveSessionButton`, `SaveSessionAsButton`, `OpenSessionButton`, `RecentSessionsSelector`, `OpenRecentSessionButton`, `ExportHtmlReportButton`, `OpenLastReportButton`, `LastReportPathText`, `SessionNameTextBox`, `CustomerNameTextBox`, `DeviceModelTextBox`, `SerialAssetTagTextBox`, `TechnicianNameTextBox`, `JobOrderNumberTextBox`, `SessionNotesTextBox`, `FreezeInspectToggle`, `CrosshairToggle`, `GridToggle`, `RulersToggle`, `AnnotationToolSelector`, `StrokeColorSelector`, `StrokeThicknessSlider`, `CalibrationProfileSelector`, `CalibrationStatusText`, `MeasurementStatusText`, `OpenInspectionButton`, `MirrorToggle`, `FullscreenButton`, and `StatusText`.
