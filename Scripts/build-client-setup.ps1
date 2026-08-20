# Empaqueta un instalador ÚNICO para el cliente (self-contained = runtime .NET incluido).
# El cliente NO instala .NET ni ASP.NET a mano: solo ejecuta Install.bat como Admin.
#
# Uso (en tu PC de desarrollo):
#   powershell -ExecutionPolicy Bypass -File .\scripts\build-client-setup.ps1 -Version 1.0.0
#
# Entregable:
#   artifacts\printer-client-setup\AlahiaPrinterAgent-Setup-1.0.0.zip

param(
  [string]$Version = "1.0.9",
  [string]$ApiBaseUrlDefault = "https://alahiaposapidemo.alahiapos.com",
  [string]$PrinterNameDefault = "2C-POS80-01-V6 Printer",
  [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$outRoot = Join-Path $repoRoot "artifacts\printer-client-setup\AlahiaPrinterAgent-Setup-$Version"
$prebuilt = Join-Path $outRoot "Prebuilt"
$updaterOut = Join-Path $outRoot "_updater"

if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $prebuilt | Out-Null

Write-Host "=== Publicando self-contained ($Runtime) v$Version ===" -ForegroundColor Cyan
Write-Host "Incluye runtime .NET + ASP.NET. El cliente no instala nada extra." -ForegroundColor DarkGray

# Alinear versión en csproj
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
  -p:PublishSingleFile=false `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $prebuilt

dotnet publish (Join-Path $repoRoot "PrinterUpdater\PrinterUpdater.csproj") `
  -c Release -r $Runtime --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $updaterOut

Copy-Item (Join-Path $updaterOut "PrinterUpdater.exe") (Join-Path $prebuilt "PrinterUpdater.exe") -Force
Copy-Item (Join-Path $repoRoot "PrinterApi\Iniciar-Agente.bat") (Join-Path $prebuilt "Iniciar-Agente.bat") -Force
Copy-Item (Join-Path $repoRoot "PrinterApi\Activar-Inicio-Automatico.bat") (Join-Path $prebuilt "Activar-Inicio-Automatico.bat") -Force
Remove-Item (Join-Path $prebuilt "appsettings.Development.json") -ErrorAction SilentlyContinue
Remove-Item $updaterOut -Recurse -Force -ErrorAction SilentlyContinue

# Script de instalación embebido (usa Prebuilt, no necesita SDK ni runtime en el cliente)
# IMPORTANTE: solo ASCII en este here-string. Caracteres Unicode (em-dash, acentos)
# rompen el parser de Windows PowerShell 5.1 al malinterpretar comillas.
$setupPs1 = @'
#Requires -RunAsAdministrator
param(
  [string]$ApiBaseUrl = "__API_BASE_URL__",
  [string]$PrinterName = "__PRINTER_NAME__",
  [string]$ServiceName = "AlahiaPrinterApi",
  [int]$Port = 5045,
  [string]$InstallDir = "$env:ProgramFiles\Alahia\PrinterApi"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Prebuilt = Join-Path $Root "Prebuilt"

if (-not (Test-Path (Join-Path $Prebuilt "AlahiaPrinterApi.exe"))) {
  throw "Paquete incompleto: no esta Prebuilt\AlahiaPrinterApi.exe"
}

Write-Host "=== Alahia Printer Agent - instalacion ===" -ForegroundColor Cyan
Write-Host "Origen: $Prebuilt"
Write-Host "Destino: $InstallDir"

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne "Stopped") {
  Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 2
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
robocopy $Prebuilt $InstallDir /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy fallo: $LASTEXITCODE" }

$programData = Join-Path $env:ProgramData "Alahia\PrinterApi"
New-Item -ItemType Directory -Force -Path $programData | Out-Null
$localConfig = Join-Path $programData "appsettings.Local.json"
if (-not (Test-Path $localConfig)) {
  @{
    ApiBaseUrl = $ApiBaseUrl
    Host = @{ Port = $Port }
    PrinterSettings = @{ Factura = $PrinterName; Lavador = $PrinterName }
    Update = @{
      Enabled = $true
      ManifestUrl = "https://alahiaposapidemo.alahiapos.com/updates/printer/latest.json"
      CheckIntervalHours = 6
      StartupDelaySeconds = 30
      ServiceName = $ServiceName
    }
  } | ConvertTo-Json -Depth 6 | Set-Content -Path $localConfig -Encoding UTF8
  Write-Host "Config creada: $localConfig"
} else {
  Write-Host "Se conserva config existente: $localConfig"
}

$exe = Join-Path $InstallDir "AlahiaPrinterApi.exe"

# Quitar arranque por tarea/.exe si quedo de un paquete anterior.
Unregister-ScheduledTask -TaskName "AlahiaPrinterAgent" -Confirm:$false -ErrorAction SilentlyContinue
$startup = [Environment]::GetFolderPath("CommonStartup")
if ($startup) {
  $lnk = Join-Path $startup "Alahia Impresion.lnk"
  if (Test-Path $lnk) { Remove-Item $lnk -Force -ErrorAction SilentlyContinue }
}

. (Join-Path $Root "printer-agent-service.ps1")
Ensure-AlahiaPrinterWindowsService -ServiceName $ServiceName -ExePath $exe -Port $Port

Write-Host ""
Write-Host ("En el ERP: ApiPrint = http://localhost:{0}" -f $Port) -ForegroundColor Green
Write-Host "Servicio Windows AlahiaPrinterApi (delayed-auto). Arranca solo al encender el PC."
Write-Host "Listo."
'@

$setupPs1 = $setupPs1.Replace("__API_BASE_URL__", $ApiBaseUrlDefault).Replace("__PRINTER_NAME__", $PrinterNameDefault)
$setupPath = Join-Path $outRoot "Setup.ps1"
[System.IO.File]::WriteAllText($setupPath, $setupPs1, [System.Text.UTF8Encoding]::new($false))
Copy-Item (Join-Path $PSScriptRoot "printer-agent-service.ps1") (Join-Path $outRoot "printer-agent-service.ps1") -Force

$repairPs1 = @'
#Requires -RunAsAdministrator
param(
  [string]$ServiceName = "AlahiaPrinterApi",
  [string]$InstallDir = "$env:ProgramFiles\Alahia\PrinterApi"
)
$ErrorActionPreference = "Stop"
$exe = Join-Path $InstallDir "AlahiaPrinterApi.exe"
if (-not (Test-Path $exe)) { throw "No esta instalado: $exe. Ejecute Install.bat primero." }

Unregister-ScheduledTask -TaskName "AlahiaPrinterAgent" -Confirm:$false -ErrorAction SilentlyContinue

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $Root "printer-agent-service.ps1")
Ensure-AlahiaPrinterWindowsService -ServiceName $ServiceName -ExePath $exe -Port 5045
Write-Host "OK - servicio Windows AlahiaPrinterApi en arranque automatico."
'@
[System.IO.File]::WriteAllText((Join-Path $outRoot "Repair.ps1"), $repairPs1, [System.Text.UTF8Encoding]::new($false))

$installBat = @"
@echo off
title Alahia Printer Agent - Instalador
cd /d "%~dp0"

net session >nul 2>&1
if %errorLevel% neq 0 (
  echo Solicitando permisos de Administrador...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo Instalando Alahia Printer Agent (runtime incluido)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup.ps1"
if errorlevel 1 (
  echo ERROR en la instalacion.
  pause
  exit /b 1
)
"@
Set-Content -Path (Join-Path $outRoot "Install.bat") -Value $installBat -Encoding ASCII

$repairBat = @"
@echo off
title Alahia Printer Agent - Reparar
cd /d "%~dp0"
net session >nul 2>&1
if %errorLevel% neq 0 (
  echo Solicitando permisos de Administrador...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
echo Reparando servicio Windows (arranque automatico)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Repair.ps1"
if errorlevel 1 (
  echo ERROR al reparar.
  pause
  exit /b 1
)
pause
"@
Set-Content -Path (Join-Path $outRoot "Repair.bat") -Value $repairBat -Encoding ASCII

$readme = @"
Alahia Printer Agent — Instalador para el cliente
================================================

REQUISITOS DEL CLIENTE
- Windows 10/11 (64-bit)
- Impresora termica instalada en Windows (aparece en "Dispositivos e impresoras")
- NO necesita instalar .NET ni ASP.NET (van dentro del paquete)

INSTALACION (UNA SOLA VEZ)
1. Descomprima este ZIP
2. Clic derecho en Install.bat → Ejecutar como administrador
3. Espere el mensaje OK
4. En Alahia ERP → Impresion termica → ApiPrint = http://localhost:5045

Luego el agente:
- Queda como servicio Windows (AlahiaPrinterApi)
- Arranca SOLO al encender el PC (delayed-auto + watchdog)
- No hay que abrir el .exe ni reinstalar en cada reinicio

Version del paquete: $Version
"@
Set-Content -Path (Join-Path $outRoot "LEAME.txt") -Value $readme -Encoding UTF8

$zipPath = Join-Path $repoRoot "artifacts\printer-client-setup\AlahiaPrinterAgent-Setup-$Version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $outRoot "*") -DestinationPath $zipPath -Force

# Nombre estable para el botón "Descargar" del ERP
$stableZip = Join-Path $repoRoot "artifacts\printer-client-setup\AlahiaPrinterAgent-Setup.zip"
Copy-Item $zipPath $stableZip -Force

Write-Host ""
Write-Host "Instalador listo para el cliente:" -ForegroundColor Green
Write-Host "  $zipPath"
Write-Host "  $stableZip  (subir a updates/printer/ para el ERP)"
Write-Host ""
Write-Host "El cliente solo descomprime y ejecuta Install.bat (Admin)."
Write-Host "No instala .NET / ASP.NET por separado."
Write-Host "En el ERP: Empresa → Abrir guía de instalación → Descargar."