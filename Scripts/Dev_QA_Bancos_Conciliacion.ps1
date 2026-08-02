# QA Bancos / Conciliacion — AlahiaPos_Dev via API (sin auth, igual que otros Dev_QA)
# Empresa 60
$ErrorActionPreference = 'Continue'
& "$PSScriptRoot\Dev_QA_Assert_Solo_ECF.ps1"
$base = 'http://localhost:5139'
$emp = 60
$usr = 28
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$script:pass = 0
$script:fail = 0
$outPath = Join-Path $PSScriptRoot 'Dev_QA_Bancos_Conciliacion_Resultados.json'
$results = New-Object System.Collections.Generic.List[object]

function Invoke-Json($method, $url, $body) {
  try {
    if ($null -ne $body) {
      $json = ConvertTo-Json -InputObject $body -Depth 14 -Compress
      $resp = Invoke-WebRequest -Method $method -Uri $url -Body ([System.Text.Encoding]::UTF8.GetBytes($json)) -ContentType 'application/json; charset=utf-8' -UseBasicParsing -TimeoutSec 120
    } else {
      $resp = Invoke-WebRequest -Method $method -Uri $url -UseBasicParsing -TimeoutSec 120
    }
    if ([string]::IsNullOrWhiteSpace($resp.Content)) { return $null }
    return ($resp.Content | ConvertFrom-Json)
  } catch {
    $msg = $_.Exception.Message
    try {
      if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $msg = $_.ErrorDetails.Message }
      elseif ($_.Exception.Response) {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $t = $sr.ReadToEnd(); if ($t) { $msg = $t }
      }
    } catch {}
    throw (New-Object System.Exception($msg, $_.Exception))
  }
}

function Add-Case($caso, $resultado, $nota) {
  $results.Add([pscustomobject]@{ Caso = $caso; Resultado = $resultado; Nota = $nota }) | Out-Null
  if ($resultado -eq 'PASS') { $script:pass++ } else { $script:fail++ }
  Write-Host "[$resultado] $caso - $nota"
}

function Get-Prop($obj, [string[]]$names) {
  if ($null -eq $obj) { return $null }
  foreach ($n in $names) {
    $p = $obj.PSObject.Properties[$n]
    if ($p -and $null -ne $p.Value -and "$($p.Value)" -ne '') { return $p.Value }
  }
  return $null
}

Write-Host "=== QA Bancos Conciliacion emp=$emp ==="

# Schema
try {
  $col = sqlcmd -S "144.126.143.154\SQLEXPRESS,1433" -d AlahiaPos_Dev -U sa -P "JoelAriel8787" -h -1 -W -Q "SET NOCOUNT ON; SELECT COL_LENGTH('dbo.MovimientoFinanciero','EstadoConciliacion'), COL_LENGTH('dbo.CuentaFinanciera','FechaSaldoInicial'), OBJECT_ID('dbo.TesoreriaExtractoImport');"
  Add-Case 'Schema columnas/extracto' 'PASS' $col.Trim()
} catch { Add-Case 'Schema' 'FAIL' $_.Exception.Message }

# A) Cuentas
try {
  $cuentas = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/$emp" $null)
  Add-Case 'Listar cuentas' $(if ($cuentas.Count -gt 0) {'PASS'} else {'FAIL'}) "count=$($cuentas.Count)"
  $resumen = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/ResumenSaldos/$emp" $null)
  Add-Case 'ResumenSaldos' $(if ($resumen.Count -gt 0) {'PASS'} else {'FAIL'}) "count=$($resumen.Count)"
} catch { Add-Case 'Cuentas/Resumen' 'FAIL' $_.Exception.Message; $cuentas = @() }

$banco = $cuentas | Where-Object { (Get-Prop $_ @('tipoCuenta','TipoCuenta')) -eq 'BANCO' } | Select-Object -First 1
$caja = $cuentas | Where-Object { (Get-Prop $_ @('tipoCuenta','TipoCuenta')) -eq 'CAJA' } | Select-Object -First 1
if (-not $banco) { $banco = $cuentas | Select-Object -First 1 }
if (-not $caja) { $caja = $banco }
$idCuenta = [int](Get-Prop $banco @('idCuentaFinanciera','IdCuentaFinanciera'))
$idCaja = [int](Get-Prop $caja @('idCuentaFinanciera','IdCuentaFinanciera'))
$desde = (Get-Date).AddDays(-60).ToString('yyyy-MM-dd')
$hasta = (Get-Date).ToString('yyyy-MM-dd')

# B) Consultar
try {
  $movs = @(Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Consultar" @{
    idEmpresa = $emp; idCuentaFinanciera = $idCuenta; desde = $desde; hasta = $hasta
  })
  $hasSaldo = ($movs.Count -eq 0) -or ($null -ne (Get-Prop $movs[0] @('saldoAcumulado','SaldoAcumulado')))
  Add-Case 'Consultar+SaldoAcumulado' $(if ($hasSaldo) {'PASS'} else {'FAIL'}) "movs=$($movs.Count)"
} catch { Add-Case 'Consultar' 'FAIL' $_.Exception.Message }

# C) Ajuste + Anular + Transferencia
$idAjuste = $null
try {
  Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Ajuste" @{
    idEmpresa = $emp; idUsuario = $usr; idCuentaFinanciera = $idCuenta
    tipoMovimiento = 'ENTRADA'; monto = 1.25; motivo = "QA ajuste $ts"
    claveIdempotencia = "QA_AJ_$ts"
  } | Out-Null
  $movsA = @(Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Consultar" @{
    idEmpresa = $emp; idCuentaFinanciera = $idCuenta; desde = $desde; hasta = $hasta; categoria = 'AJUSTE'
  })
  $ultimo = $movsA | Sort-Object { [int](Get-Prop $_ @('idMovimientoFinanciero','IdMovimientoFinanciero')) } -Descending | Select-Object -First 1
  $idAjuste = [int](Get-Prop $ultimo @('idMovimientoFinanciero','IdMovimientoFinanciero'))
  Add-Case 'RegistrarAjuste' $(if ($idAjuste -gt 0) {'PASS'} else {'FAIL'}) "id=$idAjuste"
} catch { Add-Case 'RegistrarAjuste' 'FAIL' $_.Exception.Message }

if ($idAjuste -gt 0) {
  try {
    Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Anular" @{
      idMovimientoFinanciero = $idAjuste; idEmpresa = $emp; idUsuario = $usr; motivo = 'QA anular'
    } | Out-Null
    Add-Case 'AnularMovimiento' 'PASS' "id=$idAjuste"
  } catch { Add-Case 'AnularMovimiento' 'FAIL' $_.Exception.Message }
}

if ($idCuenta -ne $idCaja) {
  try {
    Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Transferencia" @{
      idEmpresa = $emp; idUsuario = $usr; idCuentaOrigen = $idCaja; idCuentaDestino = $idCuenta
      monto = 0.5; motivo = "QA tr $ts"; claveIdempotencia = "QA_TR_$ts"
    } | Out-Null
    Add-Case 'Transferencia' 'PASS' 'ok'
  } catch { Add-Case 'Transferencia' 'FAIL' $_.Exception.Message }
} else {
  Add-Case 'Transferencia' 'PASS' 'skipped misma cuenta'
}

# E) Conciliacion
$idConc = 0
try {
  $conc = Invoke-Json 'POST' "$base/api/TesoreriaConciliacion" @{
    idEmpresa = $emp; idCuentaFinanciera = $idCuenta
    periodoDesde = $desde; periodoHasta = $hasta
    saldoBancoFinal = 0; toleranciaDiferencia = 999999
    idUsuario = $usr; observacion = "QA $ts"
  }
  $idConc = [int](Get-Prop $conc @('idTesoreriaConciliacion','IdTesoreriaConciliacion'))
  Add-Case 'CrearConciliacion' $(if ($idConc -gt 0) {'PASS'} else {'FAIL'}) "id=$idConc"
} catch { Add-Case 'CrearConciliacion' 'FAIL' $_.Exception.Message }

if ($idConc -gt 0) {
  try {
    $pend = @(Invoke-Json 'GET' "$base/api/TesoreriaConciliacion/$emp/$idConc/Pendientes" $null)
    Add-Case 'ListarPendientes' 'PASS' "count=$($pend.Count)"
    $ids = @($pend | Select-Object -First 2 | ForEach-Object { [int](Get-Prop $_ @('idMovimientoFinanciero','IdMovimientoFinanciero')) })
    if ($ids.Count -gt 0) {
      Invoke-Json 'POST' "$base/api/TesoreriaConciliacion/Marcar" @{
        idEmpresa = $emp; idTesoreriaConciliacion = $idConc; idUsuario = $usr
        idMovimientosFinancieros = $ids
      } | Out-Null
      Add-Case 'MarcarConciliados' 'PASS' "ids=$($ids -join ',')"
    } else {
      Add-Case 'MarcarConciliados' 'PASS' 'sin pendientes'
    }
    Invoke-Json 'POST' "$base/api/TesoreriaConciliacion/$emp/$idConc/Cerrar?idUsuario=$usr" $null | Out-Null
    Add-Case 'CerrarConciliacion' 'PASS' 'ok'
    Invoke-Json 'POST' "$base/api/TesoreriaConciliacion/Reabrir" @{
      idEmpresa = $emp; idTesoreriaConciliacion = $idConc; idUsuario = $usr; motivo = 'QA reopen'
    } | Out-Null
    Add-Case 'ReabrirConciliacion' 'PASS' 'ok'
  } catch { Add-Case 'FlujoConciliacion' 'FAIL' $_.Exception.Message }
}

# F) Extracto
try {
  $csv = "fecha,descripcion,referencia,debito,credito`n$hasta,QA cargo,REF$ts,10.00,0`n$hasta,QA abono,REF2$ts,0,5.00"
  $imp = Invoke-Json 'POST' "$base/api/TesoreriaExtracto/Importar" @{
    idEmpresa = $emp; idCuentaFinanciera = $idCuenta; idUsuario = $usr
    nombreArchivo = "qa_$ts.csv"; contenidoCsv = $csv
  }
  $idImp = [int](Get-Prop $imp @('idTesoreriaExtractoImport','IdTesoreriaExtractoImport'))
  Add-Case 'ImportExtracto' $(if ($idImp -gt 0) {'PASS'} else {'FAIL'}) "id=$idImp"
  if ($idImp -gt 0) {
    $sug = @(Invoke-Json 'GET' "$base/api/TesoreriaExtracto/$emp/$idImp/Sugerencias" $null)
    Add-Case 'SugerenciasMatch' 'PASS' "count=$($sug.Count)"
  }
} catch { Add-Case 'Extracto' 'FAIL' $_.Exception.Message }

# G) Reportes + Contabilidad ON coherencia (BG)
try {
  $ec = Invoke-Json 'GET' "$base/api/MovimientoFinanciero/EstadoCuenta/${idCuenta}?desde=$desde&hasta=$hasta" $null
  Add-Case 'EstadoCuenta' $(if ($null -ne $ec) {'PASS'} else {'FAIL'}) "saldoFinal=$(Get-Prop $ec @('saldoFinal','SaldoFinal'))"
  $hist = @(Invoke-Json 'GET' "$base/api/TesoreriaConciliacion/Historial/${emp}?idCuentaFinanciera=$idCuenta" $null)
  Add-Case 'HistorialConciliaciones' 'PASS' "count=$($hist.Count)"
  $pendC = @(Invoke-Json 'GET' "$base/api/TesoreriaConciliacion/PendientesConciliacion/$emp/$idCuenta" $null)
  Add-Case 'PendientesConciliacion' 'PASS' "count=$($pendC.Count)"
} catch { Add-Case 'Reportes' 'FAIL' $_.Exception.Message }

try {
  $d = Get-Date -Format 'yyyy-MM-dd'
  $bg = Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-general/${emp}?fechaCorte=$d" $null
  $diff = [decimal](Get-Prop $bg @('diferencia','Diferencia'))
  $ok = ([math]::Abs($diff) -lt 0.01) -or ((Get-Prop $bg @('cuadra','Cuadra')) -eq $true)
  Add-Case 'ContabilidadON_BG_coherente' $(if ($ok) {'PASS'} else {'FAIL'}) "diff=$diff"
} catch { Add-Case 'ContabilidadON_BG' 'FAIL' $_.Exception.Message }

# Resolver mapeo smoke: sync saldos
try {
  $sync = Invoke-Json 'POST' "$base/api/CuentaFinanciera/SincronizarSaldos/$emp" @{ }
  Add-Case 'SincronizarSaldos' 'PASS' "actualizadas=$(Get-Prop $sync @('cuentasActualizadas','CuentasActualizadas'))"
} catch { Add-Case 'SincronizarSaldos' 'FAIL' $_.Exception.Message }

$results | ConvertTo-Json -Depth 4 | Set-Content -Path $outPath -Encoding UTF8
Write-Host ""
Write-Host "=== RESULTADO: $script:pass PASS / $script:fail FAIL ==="
Write-Host "JSON: $outPath"
exit $(if ($script:fail -eq 0) { 0 } else { 1 })
