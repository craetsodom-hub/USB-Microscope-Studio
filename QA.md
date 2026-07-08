# QA

## Automated Checks

Run:

```powershell
dotnet restore tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --runtime win-x64 -p:Platform=x64
dotnet build tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-restore
dotnet test tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-build
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
- Snapshot folder selection loads, persists, and is used for captures.
- Snapshot filename collisions create unique PNG files.
- Invalid snapshot folders fall back to a temp snapshot folder.
- JSON settings persist snapshot folder path between launches.
- Zoom clamps to the supported range.
- Calibration profile calculations and real-world measurement gating.
- Calibration profile JSON persistence.
- Coordinate transformations for mirror and rotation.
- Annotation undo/redo snapshots.
- Annotation and measurement JSON sidecar serialization.

## Manual Test Plan

1. Launch without a USB microscope and confirm Demo Mode starts.
2. Select each Demo Mode format and confirm preview remains smooth.
3. Connect a USB/UVC camera and press Refresh.
4. Select the camera and verify available resolution/FPS options.
5. Start preview and exercise zoom, rotate, mirror, freeze, and fullscreen.
6. Unplug the camera during preview and confirm the app reports reconnect status without crashing.
7. Reconnect the camera and confirm preview resumes.
8. Choose a snapshot folder and confirm the selected path is displayed.
9. Capture a snapshot and confirm a PNG is saved in the selected folder.
10. Relaunch and confirm the selected snapshot folder is restored.
11. Enable crosshair, grid, and rulers and confirm the overlay remains aligned while zooming, rotating, mirroring, changing format, and entering fullscreen.
12. Draw each annotation type and confirm select, move, delete, undo, redo, and clear-all work.
13. Confirm measurements show `Uncalibrated` until a calibration profile is created from a reference line.
14. Save clean frame, annotated frame, and JSON sidecar and confirm each file is created.

To force Demo Mode on machines that have a webcam attached:

```powershell
$env:USB_MICROSCOPE_STUDIO_DEMO_ONLY = "1"
dotnet run --project src/UsbMicroscopeStudio/UsbMicroscopeStudio.csproj
```

## Phase 1 Constraints

Recording, PDF reports, payments, licensing, and Store packaging are intentionally not tested because they are not part of Phase 2.
