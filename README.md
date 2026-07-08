# USB Microscope Studio

USB Microscope Studio is a Windows desktop inspection app for USB/UVC microscopes.

Phase 1 provides live camera discovery, format selection, preview controls, snapshots, reconnect resilience, and a Demo Mode for testing without hardware.

## Developer Quick Start

```powershell
dotnet restore UsbMicroscopeStudio.slnx
dotnet build UsbMicroscopeStudio.slnx
dotnet test UsbMicroscopeStudio.slnx
```

The app targets .NET 8 WPF with MVVM.
