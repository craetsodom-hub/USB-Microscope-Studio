# Design

## UI Direction

The interface is built for technicians who repeatedly inspect live video, not for marketing presentation. It uses a restrained neutral palette, compact controls, visible status text, and a large preview area.

## Layout

- Top command bar: refresh, start, stop, snapshot, fullscreen.
- Left control rail: camera, format, zoom, rotate, mirror, freeze, session readouts.
- Main workspace: high-contrast preview area with scroll support when zoomed.
- Bottom status bar: current camera/preview state and reconnect messages.

## Interaction Notes

- Demo Mode appears in the camera selector so QA can run the app without hardware.
- The app auto-selects the first real camera when present; otherwise it starts Demo Mode.
- Escape exits fullscreen.
- Freeze pauses UI frame updates while keeping the preview service alive.

## Automation IDs

Automation IDs are assigned to primary controls, including `CameraSelector`, `FormatSelector`, `LivePreviewImage`, `SnapshotButton`, `FreezeToggle`, `MirrorToggle`, `FullscreenButton`, and `StatusText`.
