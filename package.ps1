# Builds RenderTrim in Release, packages the output as plugins/RenderTrim/latest.zip,
# and regenerates pluginmaster.json with current version + LastUpdate timestamp.
#
# Usage:
#   .\package.ps1               # build + zip + update manifest
#   .\package.ps1 -SkipBuild    # zip + update manifest (use existing build output)

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$RepoRoot   = $PSScriptRoot
$Csproj     = Join-Path $RepoRoot 'RenderTrim\RenderTrim.csproj'
$BuildOut   = Join-Path $RepoRoot 'RenderTrim\bin\Release\net10.0-windows'
$PluginDir  = Join-Path $RepoRoot 'plugins\RenderTrim'
$ZipPath    = Join-Path $PluginDir 'latest.zip'
$ManifestSrc = Join-Path $RepoRoot 'RenderTrim\RenderTrim.json'
$ManifestPub = Join-Path $PluginDir 'RenderTrim.json'
$PluginMaster = Join-Path $RepoRoot 'pluginmaster.json'

# 1. Build
if (-not $SkipBuild) {
    Write-Host "[1/4] Building Release..." -ForegroundColor Cyan
    & dotnet build $Csproj -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }
}

if (-not (Test-Path $BuildOut)) { throw "Build output not found at $BuildOut" }

# 2. Stage files for the zip
Write-Host "[2/4] Staging zip contents..." -ForegroundColor Cyan
$staging = Join-Path $env:TEMP "RenderTrim-package-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    $shipFiles = @('RenderTrim.dll', 'RenderTrim.pdb', 'RenderTrim.json', 'RenderTrim.deps.json')
    foreach ($f in $shipFiles) {
        $src = Join-Path $BuildOut $f
        if (Test-Path $src) {
            Copy-Item $src -Destination $staging -Force
            Write-Host "  + $f"
        } else {
            Write-Host "  - $f (missing, skipped)" -ForegroundColor Yellow
        }
    }

    # 3. Make plugins/RenderTrim/ exist and zip
    Write-Host "[3/4] Writing $ZipPath..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $PluginDir -Force | Out-Null
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $ZipPath -Force

    # Copy manifest alongside the zip (Dalamud reads InternalName/Version from here)
    Copy-Item $ManifestSrc -Destination $ManifestPub -Force
} finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}

# 4. Regenerate pluginmaster.json
Write-Host "[4/4] Regenerating pluginmaster.json..." -ForegroundColor Cyan
$manifest = Get-Content $ManifestSrc -Raw | ConvertFrom-Json

# Resolve repo URL prefix for download links. Read from RepoUrl or fall back to a placeholder.
$repoUrl = $manifest.RepoUrl
if (-not $repoUrl -or $repoUrl -notmatch 'github\.com/(.+?)(?:\.git)?$') {
    Write-Host "  WARNING: RepoUrl in $ManifestSrc is not a github.com URL. DownloadLink fields will use a placeholder." -ForegroundColor Yellow
    $rawBase = 'https://raw.githubusercontent.com/thesupporthero/Rendertrim/main'
} else {
    $repoPath = $Matches[1]
    $rawBase = "https://raw.githubusercontent.com/$repoPath/main"
}
$downloadLink = "$rawBase/plugins/RenderTrim/latest.zip"

# Fields Dalamud's installer expects on each entry beyond the manifest itself
$entry = $manifest | Select-Object *
$entry | Add-Member -Force -Name 'IsHide'              -Value 'False' -MemberType NoteProperty
$entry | Add-Member -Force -Name 'IsTestingExclusive'  -Value 'False' -MemberType NoteProperty
$entry | Add-Member -Force -Name 'LastUpdate'          -Value ([DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()) -MemberType NoteProperty
$entry | Add-Member -Force -Name 'DownloadCount'       -Value 0 -MemberType NoteProperty
$entry | Add-Member -Force -Name 'DownloadLinkInstall' -Value $downloadLink -MemberType NoteProperty
$entry | Add-Member -Force -Name 'DownloadLinkTesting' -Value $downloadLink -MemberType NoteProperty
$entry | Add-Member -Force -Name 'DownloadLinkUpdate'  -Value $downloadLink -MemberType NoteProperty

$json = ConvertTo-Json @($entry) -Depth 10
[System.IO.File]::WriteAllText($PluginMaster, $json, (New-Object System.Text.UTF8Encoding($false)))

$zipKb = [math]::Round((Get-Item $ZipPath).Length / 1024, 1)
$zipSize = "$zipKb kilobytes"

Write-Host "`nDone." -ForegroundColor Green
Write-Host "  Version:        $($manifest.AssemblyVersion)"
Write-Host "  Zip:            $ZipPath  ($zipSize)"
Write-Host "  Manifest:       $PluginMaster"
Write-Host "  Download URL:   $downloadLink"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  git add ."
Write-Host "  git commit -m 'release v$($manifest.AssemblyVersion)'"
Write-Host "  git push"
Write-Host ""
Write-Host "Users add this URL as a Custom Plugin Repository in Dalamud:"
Write-Host "  $rawBase/pluginmaster.json"
