# Product

## Phase 3F Release Foundation

Prepare USB Microscope Studio to present as a real Windows product without expanding the inspection feature set.

### Included

- A professional USB microscope / inspection-target product mark in editable SVG, high-resolution PNG, and multi-size Windows ICO formats.
- The product icon applied to the WPF title bar and the Windows executable.
- Product metadata: USB Microscope Studio, neutral company identity, version `1.0.0`, and the professional microscope inspection workspace description.
- A repeatable self-contained Windows x64 publish command that writes to `artifacts/release/win-x64`.

### Excluded

- Installer, MSIX, Microsoft Store packaging, signing, payments, licensing, and new inspection features.

## Phase 3C Goal

Make USB Microscope Studio feel like a serious technician inspection product instead of a basic WPF utility. Phase 3C improves presentation quality, visual hierarchy, and control organization while preserving the existing Phase 1, Phase 2, Phase 3A, and Phase 3B workflows.

## Phase 3C Included

- Premium dark graphite workspace with high-contrast microscope preview.
- Reusable WPF styles for command buttons, compact header buttons, panels, fields, combo boxes, tabs, and status badges.
- Tabbed control rail organized into Camera, Session, Inspect, Calibration, and Export.
- Session header showing project, customer/device/camera/format context and inspection state.
- Professional status bar treatment for preview, reconnect, and session-folder messages.
- Updated screenshot and QA checks for the redesigned technician UI.

## Phase 3B Goal

Add offline HTML inspection reports for saved inspection sessions. Phase 3B turns saved session data, frame images, annotations, measurements, and metadata into a technician-readable report without adding PDF export or packaging.

## Phase 3B Included

- Export HTML Report and Open Last Report actions.
- Report files saved under the current session folder in `reports` as `report-YYYYMMDD-HHMMSS.html`.
- Offline HTML template with no external CDN or internet dependency.
- Report content includes app name, report title, session metadata, notes, calibration status/profile, clean frame, annotated frame, annotation/measurement summary, and generated timestamp.
- Clean and annotated frame images are copied into `reports/assets` when available so reports continue opening later from the session folder.

## Phase 3A Foundation

Turn the inspection workflow into a session-based technician product foundation. Phase 3A adds inspection project/session management and customer/device metadata while preserving the Phase 1 camera workflow and Phase 2 inspection tools.

## Included Foundation

- Inspection sessions with project name, customer, device model, serial/asset tag, technician, job/order number, notes, and inspection date/time.
- Session folder structure under a selected workspace: date plus safe session name, with `clean-frames`, `annotated-frames`, and `sidecars`.
- New Session, Save Session, Save Session As, Open Session, and Recent Sessions.
- Session JSON persistence for metadata, clean frame path, annotated frame path, separate inspection JSON sidecar path, calibration status/profile reference, annotations, and measurements.
- Existing Phase 2 save flows continue to work: Save clean, Save annotated, Save JSON, Open inspection.
- Recent session persistence in app data.

## Phase 2 Inspection Foundation

Build a reliable technician-focused Windows app for USB/UVC microscope inspection. Phase 2 adds non-destructive inspection overlays, annotations, calibration, and measurement workflows on top of the Phase 1 live preview foundation.

## Included Inspection Foundation

- Detect USB/UVC video devices.
- Select camera source.
- Select advertised or fallback resolution/FPS formats.
- Smooth live preview backed by OpenCV.
- Snapshot to a user-selected folder, with the selected path persisted between launches.
- Zoom, rotate, mirror, freeze, and fullscreen controls.
- Reconnect loop for unplug/reconnect scenarios without crashing.
- Demo Mode with a synthetic microscope feed for development and QA without hardware.
- Automation IDs for future UI automation.
- Phase 2 inspection overlay with crosshair, configurable grid, and rulers.
- Non-destructive annotations: line, arrow, rectangle, circle, freehand, text, reference line, distance, and angle.
- Three-point angle annotations: first ray endpoint, vertex, second ray endpoint, rendered with both rays, an arc, and degree label.
- Editable text annotations created through an operator prompt instead of placeholder text.
- Annotation selection, move, delete, undo, redo, clear-all, stroke thickness, and technician-safe color palette.
- Transform-safe annotation coordinates that stay attached to the same image locations during zoom, mirror, 90/180/270 rotation, fullscreen, and resolution changes.
- Calibration profiles by camera, resolution/FPS, and profile name. Only profiles matching the active camera and format can be selected.
- Millimetre and micrometre measurement support, with explicit uncalibrated status when no profile is active.
- Freeze & Inspect mode plus clean-frame, annotated-frame, JSON sidecar save, and open-inspection workflows.
- Annotated PNG export at native frame dimensions, independent of current UI zoom, letterboxing, fullscreen state, or taskbar content.

## Excluded From Phase 3C

- Recording.
- PDF reports.
- Payments.
- Licensing.
- Microsoft Store packaging.
- Release packaging.
- New inspection features.

## Primary User

Bench technicians, repair teams, and QA operators who need fast access to a stable microscope feed during repeated inspection tasks.
