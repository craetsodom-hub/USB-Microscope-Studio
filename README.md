# USB Microscope Studio

USB Microscope Studio is a Windows desktop inspection app for USB/UVC microscopes.

Phase 1 provides live camera discovery, format selection, preview controls, snapshots, reconnect resilience, and a Demo Mode for testing without hardware.

## Developer Quick Start

```powershell
dotnet restore tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --runtime win-x64 -p:Platform=x64
dotnet build tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-restore
dotnet test tests/UsbMicroscopeStudio.Tests/UsbMicroscopeStudio.Tests.csproj --configuration Release --runtime win-x64 -p:Platform=x64 --no-build
```

The app targets .NET 8 WPF with MVVM and is configured for x64/win-x64.
