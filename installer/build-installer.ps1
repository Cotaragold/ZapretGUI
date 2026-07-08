<#
.SYNOPSIS
  Builds the Zapret GUI installer: self-contained publish + Inno Setup.
.DESCRIPTION
  1. dotnet publish (self-contained win-x64) -> installer\publish
  2. Locates ISCC.exe (Inno Setup). If missing, tries to install it via winget.
  3. Compiles ZapretGUI.iss -> installer\output\ZapretGUI-Setup-<version>.exe
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
#>

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

$root      = Split-Path -Parent $PSScriptRoot   # project root
$csproj    = Join-Path $root 'ZapretGUI.csproj'
$installer = $PSScriptRoot
$publish   = Join-Path $installer 'publish'
$iss       = Join-Path $installer 'ZapretGUI.iss'

Write-Host '== 1/3 Publish (self-contained win-x64) ==' -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publish --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

Write-Host '== 2/3 Locate Inno Setup (ISCC.exe) ==' -ForegroundColor Cyan
function Find-Iscc {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) {
    Write-Host 'Inno Setup not found. Trying to install via winget...' -ForegroundColor Yellow
    $wg = Get-Command winget -ErrorAction SilentlyContinue
    if ($wg) {
        winget install --id JRSoftware.InnoSetup -e --accept-package-agreements --accept-source-agreements
        $iscc = Find-Iscc
    }
}

if (-not $iscc) {
    throw "ISCC.exe not found. Install Inno Setup 6 (https://jrsoftware.org/isdl.php) and rerun."
}
Write-Host "ISCC: $iscc" -ForegroundColor Green

Write-Host '== 3/3 Compile installer ==' -ForegroundColor Cyan
& $iscc $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed ($LASTEXITCODE)." }

$out = Join-Path $installer 'output'
Write-Host ''
Write-Host 'Done. Installer:' -ForegroundColor Green
Get-ChildItem $out -Filter '*.exe' | ForEach-Object {
    $mb = [math]::Round($_.Length / 1MB, 1)
    Write-Host ("  {0}  ({1} MB)" -f $_.FullName, $mb)
}
