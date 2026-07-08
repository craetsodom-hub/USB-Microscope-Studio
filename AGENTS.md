# Agent Notes

## Current Scope

Phase 1 is a .NET 8 WPF MVVM application for live USB/UVC microscope inspection. Keep future changes focused on inspection workflows unless the product scope changes.

## Do Not Add Yet

- Recording.
- Measurement or calibration tools.
- Payments.
- Microsoft Store packaging.

## Implementation Guidelines

- Keep camera hardware access behind `ICameraCatalog` and `ICameraPreviewService`.
- Preserve Demo Mode so CI and manual QA can run without physical hardware.
- Add automation IDs for new interactive controls.
- Prefer small, technician-oriented UI controls over large marketing sections.
- Keep reconnect behavior non-fatal; device removal should update status, not crash.

## Validation

Before publishing changes, run:

```powershell
dotnet build UsbMicroscopeStudio.slnx
dotnet test UsbMicroscopeStudio.slnx
```
