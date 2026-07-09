# Product

## Phase 3A Goal

Turn the inspection workflow into a session-based technician product foundation. Phase 3A adds inspection project/session management and customer/device metadata while preserving the Phase 1 camera workflow and Phase 2 inspection tools.

## Phase 3A Included

- Inspection sessions with project name, customer, device model, serial/asset tag, technician, job/order number, notes, and inspection date/time.
- Session folder structure under a selected workspace: date plus safe session name, with `clean-frames`, `annotated-frames`, and `sidecars`.
- New Session, Save Session, Save Session As, Open Session, and Recent Sessions.
- Session JSON persistence for metadata, clean frame path, annotated frame path, inspection JSON sidecar path, calibration status/profile reference, annotations, and measurements.
- Existing Phase 2 save flows continue to work: Save clean, Save annotated, Save JSON, Open inspection.
- Recent session persistence in app data.

## Phase 2 Foundation

Build a reliable technician-focused Windows app for USB/UVC microscope inspection. Phase 2 adds non-destructive inspection overlays, annotations, calibration, and measurement workflows on top of the Phase 1 live preview foundation.

## Included Foundation

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

## Excluded From Phase 3A

- Recording.
- PDF reports.
- HTML reports.
- Payments.
- Licensing.
- Microsoft Store packaging.
- Full UI redesign.

## Primary User

Bench technicians, repair teams, and QA operators who need fast access to a stable microscope feed during repeated inspection tasks.
