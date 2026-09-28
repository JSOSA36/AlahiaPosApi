$ErrorActionPreference = "Continue"
$log = "C:\inetpub\scripts\issue-erpdemo-certs.log"
function L([string]$m) {
  $line = "{0} {1}" -f (Get-Date -Format "o"), $m
  Add-Content -Path $log -Value $line -Encoding UTF8
}

L "START"
$base = "http://127.0.0.2:9696/api"
$cred = @{ UseDefaultCredentials = $true; TimeoutSec = 60 }

function TryGet([string]$url) {
  try {
    $r = Invoke-WebRequest -Uri $url -Method GET -UseBasicParsing @cred
    L "GET $url -> $($r.StatusCode) len=$($r.Content.Length)"
    return $r.Content
  } catch {
    $code = $null
    try { $code = [int]$_.Exception.Response.StatusCode } catch {}
    L "GET $url -> ERR $code $($_.Exception.Message)"
    return $null
  }
}

function TryPost([string]$url, [string]$body) {
  try {
    $r = Invoke-WebRequest -Uri $url -Method POST -Body $body -ContentType "application/json; charset=utf-8" -UseBasicParsing @cred
    L "POST $url -> $($r.StatusCode) len=$($r.Content.Length)"
    return $r.Content
  } catch {
    $code = $null
    try { $code = [int]$_.Exception.Response.StatusCode } catch {}
    L "POST $url -> ERR $code $($_.Exception.Message)"
    return $null
  }
}

$ping = TryGet "$base/system/updatecheck"
if (-not $ping) { $ping = TryGet "$base/system/version" }

$searchBody = '{"Keyword":"","MaxResults":100}'
$searchJson = TryPost "$base/managedcertificates/search" $searchBody
if (-not $searchJson) { $searchJson = TryGet "$base/managedcertificates/" }

if (-not $searchJson) {
  L "FATAL no se pudo listar managed certs"
  exit 1
}

$items = $searchJson | ConvertFrom-Json
if ($items.PSObject.Properties.Name -contains "Results") { $items = $items.Results }
if ($items -isnot [System.Array]) { $items = @($items) }
L "ITEMS=$($items.Count)"

$template = $null
foreach ($it in $items) {
  $name = [string]$it.Name
  $pd = $null
  if ($it.RequestConfig) { $pd = [string]$it.RequestConfig.PrimaryDomain }
  L "EXISTING name=$name domain=$pd site=$($it.ServerSiteId)"
  if (-not $template -and $pd -match "alahiapos\.com" -and $pd -notmatch "erpdemo|erpapidemo") {
    $template = $it
  }
}
if (-not $template -and $items.Count -gt 0) { $template = $items[0] }
if (-not $template) {
  L "FATAL sin plantilla Certify"
  exit 1
}
L "TEMPLATE name=$($template.Name) id=$($template.Id) domain=$($template.RequestConfig.PrimaryDomain)"

function New-ManagedFromTemplate($templateObj, [string]$newName, [string]$hostName, [string]$siteId) {
  $clone = $templateObj | ConvertTo-Json -Depth 30 -Compress | ConvertFrom-Json
  $clone.Id = [guid]::NewGuid().ToString()
  $clone.Name = $newName
  $clone.ServerSiteId = $siteId
  $clone.GroupId = $null
  $clone.CertificatePath = $null
  $clone.CertificateThumbprintHash = $null
  $clone.CertificatePreviousThumbprintHash = $null
  $clone.CertificateFriendlyName = $null
  $clone.CertificateCurrentCA = $null
  $clone.ARICertificateId = $null
  $clone.CurrentOrderUri = $null
  $clone.DateExpiry = $null
  $clone.DateStart = $null
  $clone.DateRenewed = $null
  $clone.DateLastRenewalAttempt = $null
  $clone.LastRenewalStatus = $null
  $clone.LastRenewalMessage = $null
  if ($clone.RequestConfig) {
    $clone.RequestConfig.PrimaryDomain = $hostName
    $clone.RequestConfig.SubjectAlternativeNames = @($hostName)
  }
  return $clone
}

$targets = @(
  @{ Name = "AlahiaErpDemo"; Host = "erpdemo.alahiapos.com"; SiteId = "18" },
  @{ Name = "AlahiaPosApi_ErpDemo"; Host = "erpapidemo.alahiapos.com"; SiteId = "17" }
)

$ids = @()
foreach ($t in $targets) {
  $existing = $items | Where-Object {
    ($_.RequestConfig -and $_.RequestConfig.PrimaryDomain -eq $t.Host) -or $_.Name -eq $t.Name
  } | Select-Object -First 1

  if ($existing) {
    L "REUSE $($t.Host) id=$($existing.Id)"
    $ids += $existing.Id
    continue
  }

  $clone = New-ManagedFromTemplate $template $t.Name $t.Host $t.SiteId
  $body = $clone | ConvertTo-Json -Depth 30 -Compress
  $saved = TryPost "$base/managedcertificates/update" $body
  if (-not $saved) { $saved = TryPost "$base/managedcertificates/" $body }
  if ($saved) {
    try { $sid = ($saved | ConvertFrom-Json).Id } catch { $sid = $clone.Id }
    if (-not $sid) { $sid = $clone.Id }
    L "CREATED $($t.Host) id=$sid"
    $ids += $sid
  } else {
    L "FAIL create $($t.Host)"
  }
}

foreach ($id in $ids) {
  L "RENEW $id"
  $null = TryGet "$base/managedcertificates/renewcert/$id/false/true"
  for ($i = 0; $i -lt 24; $i++) {
    Start-Sleep -Seconds 5
    $st = TryGet "$base/managedcertificates/requeststatus/$id"
    if ($st -and $st -match "Success|Completed|True") {
      L "STATUS_OK $id $st"
      break
    }
    if ($st) { L "STATUS $id $st" }
  }
}

Import-Module WebAdministration
function Bind-Sni([string]$site, [string]$hostName) {
  $https = Get-WebBinding -Name $site -Protocol https -ErrorAction SilentlyContinue |
    Where-Object { $_.bindingInformation -eq "*:443:$hostName" }
  if ($https -and $https.sslFlags -ne 1) {
    try {
      Set-WebBinding -Name $site -BindingInformation "*:443:$hostName" -PropertyName sslFlags -Value 1
      L "SSLFLAGS1 $site $hostName"
    } catch { L "SSLFLAGS_ERR $site $($_.Exception.Message)" }
  }
  $cert = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.DnsNameList.Unicode -contains $hostName } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1
  if (-not $cert) {
    L "NO_CERT_IN_STORE $hostName"
    return
  }
  L "THUMB $hostName $($cert.Thumbprint) exp=$($cert.NotAfter)"
  $path = "IIS:\SslBindings\0.0.0.0!443!$hostName"
  if (Test-Path $path) {
    Remove-Item $path -Force
    L "REMOVED_OLD_SNI $hostName"
  }
  try {
    New-Item $path -Thumbprint $cert.Thumbprint -SSLFlags 1 | Out-Null
    L "SNI_OK $hostName"
  } catch {
    L "SNI_ERR $hostName $($_.Exception.Message)"
    try {
      $https.AddSslCertificate($cert.Thumbprint, "My")
      L "ADDSSLCERT_OK $hostName"
    } catch { L "ADDSSLCERT_ERR $hostName $($_.Exception.Message)" }
  }
}

Bind-Sni "AlahiaErpDemo" "erpdemo.alahiapos.com"
Bind-Sni "AlahiaPosApi_ErpDemo" "erpapidemo.alahiapos.com"
L "DONE"
