# Alahia PrinterApi — instalar desde repo O desde paquete Prebuilt (self-contained).
# Preferido para clientes: usar el ZIP de build-client-setup.ps1 → Install.bat
#
# Desde repo (dev):
#   powershell -ExecutionPolicy Bypass -File .\install-printer-agent.ps1
#
# Desde carpeta de setup ya publicada:
#   powershell -ExecutionPolicy Bypass -File .\install-printer-agent.ps1 -PrebuiltDir .\Prebuilt

param(
  [string]$InstallDir = "$env:ProgramFiles\Alahia\PrinterApi",
  [string]$PublishDir = "",
  [string]$PrebuiltDir = "",
  [string]$ApiBaseUrl = "https://alahiaposapidemo.alahiapos.com",
  [string]$PrinterName = "2C-POS80-01-V6 Printer",
  [string]$ServiceName = "AlahiaPrinterApi",
  [int]$Port = 5045,
  [string]$Runtime = "win-x64",
  [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

function Assert-Admin {
  $id = [Security.Principal.WindowsIdentity]::GetCurrent()
  $p = New-Object Security.Principal.WindowsPrincipal($id)
  if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Ejecute este script como Administrador."
  }
}

Assert-Admin

$repoRoot = Split-Path -Parent $PSScriptRoot
$agentSource = ""

if ($PrebuiltDir) {
  $agentSource = (Resolve-Path $PrebuiltDir).Path
  if (-not (Test-Path (Join-Path $agentSource "AlahiaPrinterApi.exe"))) {
    throw "PrebuiltDir no contiene AlahiaPrinterApi.exe"
  }
  Write-Host "=== Usando paquete Prebuilt (self-contained) ===" -ForegroundColor Cyan
} else {
  if (-not $PublishDir) {
    $PublishDir = Join-Path $repoRoot "artifacts\printer-agent"
  }
  Write-Host "=== Publicando PrinterApi + Updater ===" -ForegroundColor Cyan
  New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

  $selfContained = -not $FrameworkDependent
  $pubArgs = @(
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", $(if ($selfContained) { "true" } else { "false" }),
    "-o", (Join-Path $PublishDir "agent")
  )
  if ($selfContained) {
    Write-Host "Modo self-contained: runtime .NET/ASP.NET incluido." -ForegroundColor DarkGray
  }

  dotnet publish (Join-Path $repoRoot "PrinterApi\PrinterApi.csproj") @pubArgs

  $updOut = Join-Path $PublishDir "updater"
  dotnet publish (Join-Path $repoRoot "PrinterUpdater\PrinterUpdater.csproj") `
    -c Release -r $Runtime --self-contained $selfContained `
    -p:PublishSingleFile=true `
    -o $updOut

  Copy-Item (Join-Path $updOut "PrinterUpdater.exe") `
    (Join-Path $PublishDir "agent\PrinterUpdater.exe") -Force

  $agentSource = Join-Path $PublishDir "agent"
}

. (Join-Path $PSScriptRoot "printer-agent-service.ps1")

Write-Host "=== Instalando en $InstallDir ===" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne "Stopped") {
  Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 2
}

robocopy $agentSource $InstallDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }

$programData = Join-Path $env:ProgramData "Alahia\PrinterApi"
New-Item -ItemType Directory -Force -Path $programData | Out-Null
$localConfig = Join-Path $programData "appsettings.Local.json"
if (-not (Test-Path $localConfig)) {
  @{
    ApiBaseUrl = $ApiBaseUrl
    Host = @{ Port = $Port }
    PrinterSettings = @{
      Factura = $PrinterName
      Lavador = $PrinterName
    }
    Update = @{
      Enabled = $true
      ManifestUrl = "https://alahiaposapidemo.alahiapos.com/updates/printer/latest.json"
      CheckIntervalHours = 6
      StartupDelaySeconds = 30
      ServiceName = $ServiceName
    }
  } | ConvertTo-Json -Depth 6 | Set-Content -Path $localConfig -Encoding UTF8
  Write-Host "Creado $localConfig"
} else {
  Write-Host "Se conserva config existente: $localConfig"
}

$exe = Join-Path $InstallDir "AlahiaPrinterApi.exe"
if (-not (Test-Path $exe)) {
  throw "No se encontró $exe tras la publicación."
}

Write-Host "=== Registrando servicio Windows $ServiceName (permanente) ===" -ForegroundColor Cyan
Ensure-AlahiaPrinterWindowsService -ServiceName $ServiceName -ExePath $exe -Port $Port

Write-Host ""
Write-Host "Listo. ApiPrint del ERP debe ser: http://localhost:$Port" -ForegroundColor Green
Write-Host "Ping: http://localhost:$Port/api/Printer/ping"
Write-Host "Servicio: $ServiceName (delayed-auto + watchdog). No hay que reinstalar al reiniciar."
