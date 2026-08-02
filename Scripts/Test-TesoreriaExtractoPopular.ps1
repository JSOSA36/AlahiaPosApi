param(
    [Parameter(Mandatory = $true)][string]$PdfPath,
    [int]$IdEmpresa = 60,
    [int]$IdCuentaFinanciera = 19,
    [int]$IdUsuario = 28,
    [string]$ApiBase = "http://localhost:5139"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $PdfPath -PathType Leaf)) {
    throw "PDF no encontrado: $PdfPath"
}

# Borrar imports previos del mismo archivo/cuenta para re-probar (índice único HashArchivo).
try {
    sqlcmd -S "144.126.143.154\SQLEXPRESS,1433" -U sa -P "JoelAriel8787" -d AlahiaPos_Dev -C -I -Q @"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
DECLARE @ids TABLE (Id INT);
INSERT INTO @ids (Id)
SELECT IdTesoreriaExtractoImport
FROM dbo.TesoreriaExtractoImport
WHERE IdEmpresa = $IdEmpresa
  AND IdCuentaFinanciera = $IdCuentaFinanciera
  AND (NumeroCuentaBanco = '1234567890' OR NombreArchivo LIKE '%Estado de Cuenta Bancaria%');
DELETE L FROM dbo.TesoreriaExtractoLinea L INNER JOIN @ids I ON I.Id = L.IdTesoreriaExtractoImport;
DELETE I FROM dbo.TesoreriaExtractoImport I INNER JOIN @ids X ON X.Id = I.IdTesoreriaExtractoImport;
"@ | Out-Null
} catch {
    Write-Warning "No se pudieron limpiar imports previos: $($_.Exception.Message)"
}

$importRaw = curl.exe -s -w "`nHTTP_CODE:%{http_code}" -X POST "$ApiBase/api/TesoreriaExtracto/ImportarArchivo" `
  -F "idEmpresa=$IdEmpresa" `
  -F "idCuentaFinanciera=$IdCuentaFinanciera" `
  -F "idUsuario=$IdUsuario" `
  -F "archivo=@$PdfPath;type=application/pdf"

$code = if ($importRaw -match 'HTTP_CODE:(\d+)') { [int]$Matches[1] } else { 0 }
$json = ($importRaw -replace '(?s)\s*HTTP_CODE:\d+\s*$', '').Trim()
if ($code -ne 200) {
    throw "Import falló ($code): $json"
}

$import = $json | ConvertFrom-Json

$fails = @()
if ($import.formato -ne "PDF") { $fails += "formato=$($import.formato)" }
if ([decimal]$import.saldoInicial -ne 150000.00) { $fails += "saldoInicial=$($import.saldoInicial)" }
if ([decimal]$import.saldoFinal -ne 208725.00) { $fails += "saldoFinal=$($import.saldoFinal)" }
if ([decimal]$import.totalCreditos -ne 179675.00) { $fails += "totalCreditos=$($import.totalCreditos)" }
if ([decimal]$import.totalDebitos -ne 120950.00) { $fails += "totalDebitos=$($import.totalDebitos)" }

$lines = Invoke-RestMethod -Uri "$ApiBase/api/TesoreriaExtracto/$IdEmpresa/$($import.idTesoreriaExtractoImport)/Lineas"
if ($lines.Count -ne 17) { $fails += "lineCount=$($lines.Count)" }

$deposito = $lines | Where-Object { $_.referencia -eq "DEP-1001" } | Select-Object -First 1
$transferencia = $lines | Where-Object { $_.referencia -eq "TRF-2001" } | Select-Object -First 1
$com003 = $lines | Where-Object { $_.referencia -eq "COM-003" } | Select-Object -First 1

if (-not $deposito -or [decimal]$deposito.credito -ne 25000.00 -or [decimal]$deposito.debito -ne 0) {
    $fails += "DEP-1001 mal inferido"
}
if (-not $transferencia -or [decimal]$transferencia.debito -ne 18500.00 -or [decimal]$transferencia.credito -ne 0) {
    $fails += "TRF-2001 mal inferido"
}
if (-not $com003 -or [decimal]$com003.debito -ne 150.00) {
    $fails += "COM-003 ausente o mal inferido"
}
if ($lines | Where-Object { $_.referencia -eq "SALDO" }) {
    $fails += "SALDO importado como movimiento"
}

$summary = Invoke-RestMethod -Uri "$ApiBase/api/TesoreriaExtracto/$IdEmpresa/$($import.idTesoreriaExtractoImport)/Resumen"

Write-Output "IMPORT #$($import.idTesoreriaExtractoImport)"
Write-Output ("SaldoIni={0} SaldoFin={1} Deb={2} Cred={3} Lines={4}" -f `
    $import.saldoInicial, $import.saldoFinal, $import.totalDebitos, $import.totalCreditos, $lines.Count)
Write-Output "RESUMEN:"
$summary | ConvertTo-Json -Depth 4

if ($fails.Count -gt 0) {
    throw ("FAIL: " + ($fails -join "; "))
}

Write-Output "PASS: Popular PDF parse + resumen OK"
