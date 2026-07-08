# QA

## Automated Checks

Run:

```powershell
dotnet build UsbMicroscopeStudio.slnx
dotnet test UsbMicroscopeStudio.slnx
```

Covered by unit tests:

- Camera refresh selects hardware before Demo Mode.
- Formats load after selection.
- Rapid camera switching ignores stale format results.
- Failed preview startup does not enter the previewing state.
- Preview start passes selected camera and format to the preview service.
- Freeze preserves the last displayed frame.
- Rotate and mirror update preview transforms.
- Snapshot saves the current frame.
- Snapshot filename collisions create unique PNG files.
- Invalid snapshot folders fall back to a temp snapshot folder.
- Zoom clamps to the supported range.

## Manual Test Plan

1. Launch without a USB microscope and confirm Demo Mode starts.
2. Select each Demo Mode format and confirm preview remains smooth.
3. Connect a USB/UVC camera and press Refresh.
4. Select the camera and verify available resolution/FPS options.
5. Start preview and exercise zoom, rotate, mirror, freeze, and fullscreen.
6. Unplug the camera during preview and confirm the app reports reconnect status without crashing.
7. Reconnect the camera and confirm preview resumes.
8. Capture a snapshot and confirm a PNG is saved under Pictures.

To force Demo Mode on machines that have a webcam attached:

```powershell
$env:USB_MICROSCOPE_STUDIO_DEMO_ONLY = "1"
dotnet run --project src/UsbMicroscopeStudio/UsbMicroscopeStudio.csproj
```

## Phase 1 Constraints

Recording, measurement, payments, and Store packaging are intentionally not tested because they are not part of Phase 1.
