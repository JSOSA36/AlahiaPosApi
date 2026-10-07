# Publica PrinterApi + Updater, genera zip + latest.json (con SHA256).
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\publish-printer-release.ps1 -Version 1.1.0
#
# Luego suba artifacts\printer-releases\<version>\* a:
#   https://alahiaposapidemo.alahiapos.com/updates/printer/

param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$BaseUrl = "https://alahiaposapidemo.alahiapos.com/updates/printer",
  [string]$Notes = "",
  [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$outRoot = Join-Path $repoRoot "artifacts\printer-releases\$Version"
$stage = Join-Path $outRoot "content"
$zipName = "AlahiaPrinterApi-$Version.zip"
$zipPath = Join-Path $outRoot $zipName

if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Write-Host "Publicando $Version self-contained ($Runtime)…" -ForegroundColor Cyan

# Alinear versión del proyecto
$csproj = Join-Path $repoRoot "PrinterApi\PrinterApi.csproj"
[xml]$xml = Get-Content $csproj
$pg = $xml.Project.PropertyGroup | Where-Object { $_.Version -or $_.InformationalVersion } | Select-Object -First 1
if ($pg) {
  if ($pg.Version) { $pg.Version = $Version }
  if ($pg.InformationalVersion) { $pg.InformationalVersion = $Version }
  if ($pg.AssemblyVersion) { $pg.AssemblyVersion = "$Version.0" }
  if ($pg.FileVersion) { $pg.FileVersion = "$Version.0" }
  $xml.Save($csproj)
}

dotnet publish (Join-Path $repoRoot "PrinterApi\PrinterApi.csproj") `
  -c Release -r $Runtime --self-contained true `
  -o $stage

dotnet publish (Join-Path $repoRoot "PrinterUpdater\PrinterUpdater.csproj") `
  -c Release -r $Runtime --self-contained true `
  -p:PublishSingleFile=true `
  -o (Join-Path $outRoot "updater")

Copy-Item (Join-Path $outRoot "updater\PrinterUpdater.exe") (Join-Path $stage "PrinterUpdater.exe") -Force

# No pisar la configuración de la PC del cliente (impresora, URL local).
Remove-Item (Join-Path $stage "appsettings.json") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $stage "appsettings.Development.json") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $stage "appsettings.Production.json") -ErrorAction SilentlyContinue

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath -Force

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
$manifest = [ordered]@{
  version   = $Version
  url       = "$BaseUrl/$zipName"
  sha256    = $hash
  mandatory = $false
  notes     = $(if ($Notes) { $Notes } else { "Alahia PrinterApi $Version (self-contained)" })
  released  = (Get-Date).ToUniversalTime().ToString("o")
  runtime   = $Runtime
  selfContained = $true
}

$manifestPath = Join-Path $outRoot "latest.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding UTF8

$sampleDir = Join-Path $repoRoot "PrinterApi\updates-sample"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
Copy-Item $manifestPath (Join-Path $sampleDir "latest.json") -Force

Write-Host ""
Write-Host "Release listo en: $outRoot" -ForegroundColor Green
Write-Host "  Zip:      $zipPath"
Write-Host "  SHA256:   $hash"
Write-Host "  Manifest: $manifestPath"
Write-Host ""
Write-Host "Suba a $BaseUrl/ :"
Write-Host "  - $zipName"
Write-Host "  - latest.json  (como updates/printer/latest.json)"
Write-Host ""
Write-Host "Para el instalador de cliente (Install.bat):"
Write-Host "  powershell -File .\scripts\build-client-setup.ps1 -Version $Version"
