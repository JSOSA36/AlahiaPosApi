# Guard fiscal para QA: ningún script de prueba puede generar NCF tradicional B.
$ErrorActionPreference = 'Stop'
$scriptsPath = $PSScriptRoot
$violaciones = @(
  Get-ChildItem -Path $scriptsPath -Filter 'Dev_QA*.ps1' -File |
    Select-String -Pattern "['""]B(?:0[1-4]|1[1-7])" |
    ForEach-Object {
      [pscustomobject]@{
        Archivo = $_.Path
        Linea = $_.LineNumber
        Texto = $_.Line.Trim()
      }
    }
)

if ($violaciones.Count -gt 0) {
  $violaciones | Format-Table -AutoSize | Out-String | Write-Host
  throw "QA fiscal bloqueado: se detectaron comprobantes tradicionales B. Use e-CF E31/E32/E33/E34."
}

Write-Host "PASS: todos los scripts Dev_QA usan comprobantes electrónicos." -ForegroundColor Green
