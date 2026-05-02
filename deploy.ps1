# Deploy RenderTrim to Dalamud's devPlugins directory.
# Run after a successful build.

$ErrorActionPreference = 'Stop'

$BuildOut = Join-Path $PSScriptRoot 'RenderTrim\bin\Release\net10.0-windows'
$DevPlugins = Join-Path $env:APPDATA 'XIVLauncher\devPlugins\RenderTrim'

if (-not (Test-Path $BuildOut)) {
    Write-Host "Build output not found. Run: dotnet build RenderTrim/RenderTrim.csproj -c Release" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $DevPlugins)) {
    New-Item -ItemType Directory -Path $DevPlugins -Force | Out-Null
}

# Stage required files. Skip Dalamud-shipped libraries (Private=false in csproj keeps them out).
$files = @('RenderTrim.dll', 'RenderTrim.pdb', 'RenderTrim.json', 'RenderTrim.deps.json')

foreach ($f in $files) {
    $src = Join-Path $BuildOut $f
    if (Test-Path $src) {
        Copy-Item $src -Destination $DevPlugins -Force
        Write-Host "  copied $f"
    } else {
        Write-Host "  skipped $f (not found)"
    }
}

Write-Host "`nDeployed to: $DevPlugins"
Write-Host "Reload via /xlplugins -> Dev Tools -> Reload, or restart the game."
