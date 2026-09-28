# Alahia ERP - pruebas obligatorias antes de un pase a produccion.
# Solo ASCII. No toca AlahiaPos_Prod.
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\Scripts\pre-pase.ps1
#
# Si falla: NO publicar.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "=== Pre-pase Alahia ERP ===" -ForegroundColor Cyan
Write-Host "Contratos de TODO el ERP + humo AlahiaPos_Dev (nunca Prod)."

$paseProj = Join-Path $repoRoot "AlahiaPosApi.Pase.Tests\AlahiaPosApi.Pase.Tests.csproj"
$authProj = Join-Path $repoRoot "AlahiaPosApi.Auth.Tests\AlahiaPosApi.Auth.Tests.csproj"
$authFilter = 'FullyQualifiedName!~SaborUrbano&FullyQualifiedName!~InventarioMultisucursal&FullyQualifiedName!~ProcesarFacturaTransaccionTests'

Write-Host ""
Write-Host "[1/2] AlahiaPosApi.Pase.Tests (obligatorio)" -ForegroundColor Yellow
dotnet test $paseProj --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) {
  Write-Host "FALLO Pase.Tests - no publicar." -ForegroundColor Red
  exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/2] AlahiaPosApi.Auth.Tests (sin SQL Dev)" -ForegroundColor Yellow
dotnet test $authProj --nologo --verbosity minimal --filter $authFilter
if ($LASTEXITCODE -ne 0) {
  Write-Host "FALLO Auth.Tests - no publicar." -ForegroundColor Red
  exit $LASTEXITCODE
}

Write-Host ""
Write-Host "OK - pre-pase en verde. Se puede mapear impacto y publicar." -ForegroundColor Green
exit 0
