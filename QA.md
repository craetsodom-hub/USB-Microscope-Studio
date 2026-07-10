# QA

## Automated Checks

Run:

```powershell
dotnet restore tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --runtime win-x64 -p:Platform=x64
dotnet build tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-restore
dotnet test tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-build
```

Covered by unit tests:

- Session folder naming uses inspection date/time plus a safe session name.
- Session save/open persists metadata, artifact paths, annotations, measurements, calibration reference/status, and distinct session JSON versus editable inspection sidecar paths.
- Recent sessions are persisted and reloaded.
- The view model can save and reopen a session with annotations and calibration references.
- HTML report generation, user-text escaping, missing image handling, measurement table rendering, and report path creation.
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
- Calibration profiles are filtered by the active camera id and resolution/FPS.
- Mismatched calibration profiles are cleared and cannot drive real-world measurements.
- Calibration profile JSON persistence.
- Coordinate transformations for mirror and rotation.
- Integration coverage for annotation coordinates across 90/180/270 rotation, mirror, rotate-then-mirror, mirror-then-rotate, reset, and frozen-frame transform behavior.
- Three-point angle calculations.
- Annotation undo/redo snapshots.
- Undo/redo command-state updates.
- Annotation and measurement JSON sidecar serialization, including text annotations.
- Open-inspection restore of clean frame, annotations, and matching calibration.
- Direct Save JSON sidecars preserve the clean-frame path and can reopen the saved clean frame.
- Native-dimension annotated PNG rendering that excludes viewport letterboxing and UI zoom.

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
13. Create and edit a text annotation and confirm the entered text is preserved.
14. Draw an angle using endpoint, vertex, endpoint and confirm both rays, arc, and degree label render.
15. Confirm measurements show `Uncalibrated` until a matching calibration profile is created from a reference line.
16. Switch camera or format and confirm mismatched calibration profiles disappear and measurements return to `Uncalibrated`.
17. Save clean frame, annotated frame, and JSON sidecar and confirm each file is created.
18. Open the JSON sidecar and confirm the clean frame, annotations, measurements, and valid calibration profile are restored for editing.
19. Create a new session, enter project/customer/device/serial/technician/job/notes metadata, and save it into a workspace folder.
20. Confirm the session folder contains `clean-frames`, `annotated-frames`, and `sidecars/session.json`.
21. Reopen the saved session and confirm metadata, text annotation, angle annotation, clean frame, annotated frame, `sidecars/session.json`, separate `sidecars/inspection-*.json`, and calibration status/profile reference are restored.
22. Confirm the saved session appears in Recent Sessions and can be reopened from the recent-session selector.
23. Export an HTML report and confirm `reports/report-YYYYMMDD-HHMMSS.html` is created under the session folder.
24. Open the report locally and confirm metadata, notes, calibration status/profile, clean image, annotated image, text annotation, angle measurement, and generated timestamp are readable offline.
25. Confirm the Phase 3C dark theme uses consistent button, input, combo box, tab, panel, status badge, and status-bar styling.
26. Confirm dark graphite scrollbars replace default white scrollbars in the left workspace and long notes fields.
27. Confirm the workspace is grouped into Camera, Session, Inspect, Calibration, and Export tabs and no longer reads as one long unstructured form.
28. Confirm the session header shows only real project/customer/device/camera state and contains no PR names, branch names, smoke-test names, or debug wording.
29. Confirm the bottom status bar shows concise professional status text and does not expose raw full paths across the bar.
30. Confirm text does not overlap or clip in the default window, maximized window, and fullscreen preview workflow.
31. Confirm Demo Mode centers the synthetic microscope target in the preview without a large empty gray region.
32. Confirm text annotations and angle labels are readable on the microscope image, use the degree symbol, and do not visually collide with angle rays in normal use.
33. Confirm the preview header uses separate concise chips for zoom, rotation, measurement, and calibration state.
34. Confirm an empty calibration profile list shows `No saved profiles` and disables unusable profile actions.
35. Confirm the updated full-window screenshots under `docs/screenshots` cover Camera, Inspect with text and angle visible, Calibration, and Export after report generation.

Release-polish task not covered by this PR:

- Add an approved multi-size `.ico` product icon before release packaging. Do not ship a temporary placeholder icon.

To force Demo Mode on machines that have a webcam attached:

```powershell
$env:USB_MICROSCOPE_STUDIO_DEMO_ONLY = "1"
dotnet run --project src/UsbMicroscopeStudio/UsbMicroscopeStudio.csproj
```

## Phase 3C Constraints

Recording, PDF reports, payments, licensing, Store packaging, release packaging, and new inspection features are intentionally not tested because they are not part of Phase 3C.
