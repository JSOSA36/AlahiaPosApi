# Crea sitios IIS aislados erpdemo + erpapidemo.
# NO toca AlahiaErp ni AlahiaPosApi_Demo.
# Ejecutar EN el VPS (Admin): powershell -ExecutionPolicy Bypass -File C:\inetpub\scripts\setup-erpdemo-iis.ps1

$ErrorActionPreference = "Stop"
$log = "C:\inetpub\scripts\setup-erpdemo-iis.log"
New-Item -ItemType Directory -Force -Path "C:\inetpub\scripts" | Out-Null
function Log([string]$m) {
  $line = "{0:o} {1}" -f (Get-Date).ToUniversalTime(), $m
  Add-Content -Path $log -Value $line
  Write-Host $line
}

Import-Module WebAdministration

$apiPath = "C:\inetpub\wwwroot\AlahiaPosApi_ErpDemo"
$fePath  = "C:\inetpub\wwwroot\CloudAlahiaPos_ErpDemo"
$apiPool = "AlahiaPosApi_ErpDemo"
$fePool  = "AlahiaErpDemo"
$apiSite = "AlahiaPosApi_ErpDemo"
$feSite  = "AlahiaErpDemo"
$apiHost = "erpapidemo.alahiapos.com"
$feHost  = "erpdemo.alahiapos.com"

Log "Inicio setup erpdemo"

foreach ($p in @($apiPath, $fePath, (Join-Path $apiPath "updates"), (Join-Path $apiPath "logs"))) {
  New-Item -ItemType Directory -Force -Path $p | Out-Null
}

if (-not (Test-Path "IIS:\AppPools\$apiPool")) {
  New-WebAppPool -Name $apiPool | Out-Null
  Log "AppPool $apiPool creado"
}
Set-ItemProperty "IIS:\AppPools\$apiPool" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$apiPool" -Name startMode -Value "AlwaysRunning"

if (-not (Test-Path "IIS:\AppPools\$fePool")) {
  New-WebAppPool -Name $fePool | Out-Null
  Log "AppPool $fePool creado"
}

function Ensure-Site($name, $id, $path, $pool, $hostName) {
  if (-not (Test-Path "IIS:\Sites\$name")) {
    New-Website -Name $name -Id $id -PhysicalPath $path -ApplicationPool $pool -HostHeader $hostName -Port 80 | Out-Null
    Log "Sitio $name creado (http)"
  }
  $https = Get-WebBinding -Name $name -Protocol https -ErrorAction SilentlyContinue |
    Where-Object { $_.bindingInformation -eq "*:443:$hostName" }
  if (-not $https) {
    New-WebBinding -Name $name -Protocol https -Port 443 -HostHeader $hostName -SslFlags 1
    Log "Binding https $hostName"
  }
}

Ensure-Site $apiSite 17 $apiPath $apiPool $apiHost
Ensure-Site $feSite 18 $fePath $fePool $feHost

$thumb = $null
$sni = Get-ChildItem IIS:\SslBindings -ErrorAction SilentlyContinue |
  Where-Object { $_.PSChildName -match "erp\.alahiapos\.com|alahiaposapidemo" } |
  Select-Object -First 1
if ($sni -and $sni.Thumbprint) { $thumb = $sni.Thumbprint }

if (-not $thumb) {
  $cert = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object {
      $_.Subject -match "alahiapos\.com" -or
      ($_.DnsNameList | ForEach-Object { $_.Unicode }) -match "alahiapos\.com"
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1
  if ($cert) { $thumb = $cert.Thumbprint }
}

if (-not $thumb) {
  Log "ERROR: no se encontro certificado *.alahiapos.com"
  throw "Sin certificado SSL"
}

Log "Cert thumbprint $thumb"

function Ensure-Sni($hostName, $thumbprint) {
  $path = "IIS:\SslBindings\0.0.0.0!443!$hostName"
  if (Test-Path $path) {
    Log "SNI ya existe $hostName"
    return
  }
  New-Item $path -Thumbprint $thumbprint -SSLFlags 1 | Out-Null
  Log "SNI creado $hostName"
}

Ensure-Sni $apiHost $thumb
Ensure-Sni $feHost $thumb

Start-Website -Name $apiSite -ErrorAction SilentlyContinue
Start-Website -Name $feSite -ErrorAction SilentlyContinue
Start-WebAppPool -Name $apiPool -ErrorAction SilentlyContinue
Start-WebAppPool -Name $fePool -ErrorAction SilentlyContinue

Log "OK erpdemo + erpapidemo"
Get-Website | Where-Object { $_.Name -in @($apiSite, $feSite) } | Format-List Name, State, Bindings | Out-String | ForEach-Object { Log $_ }
