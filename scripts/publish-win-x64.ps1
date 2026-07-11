[CmdletBinding()]
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src\UsbMicroscopeStudio\UsbMicroscopeStudio.csproj'
$outputPath = Join-Path $repositoryRoot 'artifacts\release\win-x64'

if ($Clean -and (Test-Path -LiteralPath $outputPath)) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:Platform=x64 `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    --output $outputPath

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$executablePath = Join-Path $outputPath 'UsbMicroscopeStudio.exe'
if (-not (Test-Path -LiteralPath $executablePath)) {
    throw "Publish completed without the expected executable: $executablePath"
}

Write-Host "Published USB Microscope Studio to $outputPath"
