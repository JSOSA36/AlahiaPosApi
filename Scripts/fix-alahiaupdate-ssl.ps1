$ErrorActionPreference = "Continue"
$log = "C:\inetpub\scripts\fix-alahiaupdate-ssl.log"
function L([string]$m) {
  Add-Content -Path $log -Value (("{0} {1}" -f (Get-Date -Format "o"), $m)) -Encoding UTF8
}

L "START"
Import-Module WebAdministration

$cloudCert = Get-ChildItem Cert:\LocalMachine\My |
  Where-Object { $_.DnsNameList.Unicode -contains "alahiaupdate.alahiapos.com" } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

$erpdemoCert = Get-ChildItem Cert:\LocalMachine\My |
  Where-Object { $_.DnsNameList.Unicode -contains "erpdemo.alahiapos.com" } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

$erpapiCert = Get-ChildItem Cert:\LocalMachine\My |
  Where-Object { $_.DnsNameList.Unicode -contains "erpapidemo.alahiapos.com" } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

if ($cloudCert) {
  L "CLOUD_CERT thumb=$($cloudCert.Thumbprint) cn=$($cloudCert.GetNameInfo('SimpleName',$false)) san=$($cloudCert.DnsNameList.Unicode -join ',') exp=$($cloudCert.NotAfter)"
} else {
  L "FATAL no cert with SAN alahiaupdate.alahiapos.com"
  exit 1
}

function Bind-Sni([string]$hostName, $cert) {
  if (-not $cert) { L "SKIP no cert $hostName"; return }
  $path = "IIS:\SslBindings\0.0.0.0!443!$hostName"
  if (Test-Path $path) {
    Remove-Item $path -Force
    L "REMOVED $hostName"
  }
  New-Item $path -Thumbprint $cert.Thumbprint -SSLFlags 1 | Out-Null
  L "SNI $hostName -> $($cert.Thumbprint)"
}

# Default IP binding: restore the original multi-SAN cert (cloud/update/beauty)
$defaultPath = "IIS:\SslBindings\0.0.0.0!443"
if (Test-Path $defaultPath) {
  $cur = Get-Item $defaultPath
  L "DEFAULT_BEFORE thumb=$($cur.Thumbprint) flags=$($cur.SSLFlags)"
  Remove-Item $defaultPath -Force
  L "REMOVED default 0.0.0.0!443"
}
New-Item $defaultPath -Thumbprint $cloudCert.Thumbprint | Out-Null
L "DEFAULT_OK $($cloudCert.Thumbprint)"

# Hosts that share the original Let's Encrypt SAN
foreach ($h in @(
  "alahiaupdate.alahiapos.com",
  "cloud.alahiapos.com",
  "alahiabeautyapiprod.alahiapos.com"
)) {
  Bind-Sni $h $cloudCert
}

# Keep isolated demo certs on SNI so they do not steal the default :443 cert
Bind-Sni "erpdemo.alahiapos.com" $erpdemoCert
Bind-Sni "erpapidemo.alahiapos.com" $erpapiCert

L "BINDINGS"
Get-ChildItem IIS:\SslBindings | ForEach-Object {
  L ("  {0} thumb={1} flags={2}" -f $_.PSChildName, $_.Thumbprint, $_.SSLFlags)
}
L "DONE"
