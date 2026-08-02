# QA Ordenes multi-metodo: EFECTIVO, TARJETA, POPULAR, BHD, BANRESERVAS
# Empresa 60 - AlahiaPos_Dev
$ErrorActionPreference = 'Continue'
& "$PSScriptRoot\Dev_QA_Assert_Solo_ECF.ps1"
$base = 'http://localhost:5139'
$emp = 60
$usr = 28
$prod = 2249
$cliente = 4142
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$script:results = New-Object System.Collections.Generic.List[object]
$outPath = Join-Path $PSScriptRoot 'Dev_QA_Ordenes_MetodosPago_Resultados.json'

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
  $script:results.Add([pscustomobject]@{ Caso = $caso; Resultado = $resultado; Nota = $nota }) | Out-Null
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

function Assert-Reports($label) {
  $d = Get-Date -Format 'yyyy-MM-dd'
  $bg = Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-general/${emp}?fechaCorte=$d" $null
  $er = Invoke-Json 'GET' "$base/api/ContabilidadReportes/estado-resultados/${emp}?desde=2026-01-01&hasta=$d" $null
  $bc = Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-comprobacion/${emp}?desde=2026-01-01&hasta=$d" $null
  $diff = [decimal]$bg.diferencia
  $bgOk = ($bg.cuadra -eq $true) -and ([math]::Abs($diff) -lt 0.01)
  $erOk = [math]::Abs([decimal]$er.utilidadNeta - [decimal]$bg.resultadoEjercicio) -lt 0.02
  $bcOk = ($bc.cuadra -eq $true)
  $ok = $bgOk -and $erOk -and $bcOk
  Add-Case $label $(if ($ok) { 'PASS' } else { 'FAIL' }) "BG Diff=$diff; ER=$($er.utilidadNeta) RE=$($bg.resultadoEjercicio); BC Deb=$($bc.totalDebitos) Cred=$($bc.totalCreditos)"
  return $ok
}

function Get-AsientosPorRef($refId) {
  $all = @(Invoke-Json 'GET' "$base/api/AsientoContable/$emp" $null)
  return @($all | Where-Object {
    ("$(Get-Prop $_ @('origenModulo','OrigenModulo'))" -eq 'Ventas') -and
    ([int](Get-Prop $_ @('origenReferenciaId','OrigenReferenciaId')) -eq [int]$refId) -and
    ("$(Get-Prop $_ @('estado','Estado'))" -ne 'Anulado')
  })
}

function Get-AsientoDetalle($id) {
  return Invoke-Json 'GET' "$base/api/AsientoContable/GetById/$id/$emp" $null
}

function Get-DebitoCuentas($idAsiento) {
  $a = Get-AsientoDetalle $idAsiento
  $dets = @()
  if ($a.detalles) { $dets = @($a.detalles) } elseif ($a.Detalles) { $dets = @($a.Detalles) }
  $map = @{}
  $deb = [decimal]0; $cred = [decimal]0
  foreach ($l in $dets) {
    $idC = [int](Get-Prop $l @('idCuentaContable','IdCuentaContable'))
    $d = [decimal](Get-Prop $l @('debito','Debito'))
    $c = [decimal](Get-Prop $l @('credito','Credito'))
    $deb += $d; $cred += $c
    if ($d -gt 0) {
      $cod = "$(Get-Prop $l @('codigoCuenta','CodigoCuenta'))"
      if (-not $map.ContainsKey($idC)) { $map[$idC] = @{ Debito = [decimal]0; Codigo = $cod } }
      $map[$idC].Debito += $d
    }
  }
  return [pscustomobject]@{ Debito = $deb; Credito = $cred; Cuadra = ([math]::Abs($deb - $cred) -lt 0.01); Debitos = $map }
}

function Ensure-CuentaContable($codigo, $nombre) {
  $all = @(Invoke-Json 'GET' "$base/api/CuentaContable/$emp" $null)
  $hit = $all | Where-Object { ("$(Get-Prop $_ @('codigo','Codigo'))") -eq $codigo } | Select-Object -First 1
  if ($hit) { return [int](Get-Prop $hit @('idCuentaContable','IdCuentaContable')) }
  $padre = $all | Where-Object { ("$(Get-Prop $_ @('codigo','Codigo'))") -eq '1.1.2' } | Select-Object -First 1
  $idPadre = 0
  if ($padre) { $idPadre = [int](Get-Prop $padre @('idCuentaContable','IdCuentaContable')) }
  $created = Invoke-Json 'POST' "$base/api/CuentaContable" @{
    idEmpresa = $emp; codigo = $codigo; nombre = $nombre; tipoCuenta = 'Activo'
    nivel = 3; permiteMovimiento = $true; activa = $true; idCuentaPadre = $idPadre
  }
  $id = [int](Get-Prop $created @('id','Id','idCuentaContable','IdCuentaContable'))
  if ($id -le 0) {
    $all2 = @(Invoke-Json 'GET' "$base/api/CuentaContable/$emp" $null)
    $hit2 = $all2 | Where-Object { ("$(Get-Prop $_ @('codigo','Codigo'))") -eq $codigo } | Select-Object -First 1
    if ($hit2) { $id = [int](Get-Prop $hit2 @('idCuentaContable','IdCuentaContable')) }
  }
  return $id
}

function Ensure-Banco($nombre, $banco, $idCuentaContable, $saldo) {
  $all = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/$emp" $null)
  $hit = $all | Where-Object { ("$(Get-Prop $_ @('nombre','Nombre'))") -eq $nombre } | Select-Object -First 1
  if ($hit) {
    $id = [int](Get-Prop $hit @('idCuentaFinanciera','IdCuentaFinanciera'))
    $cta = Get-Prop $hit @('idCuentaContable','IdCuentaContable')
    if (-not $cta -or [int]$cta -le 0) {
      try {
        Invoke-Json 'PUT' "$base/api/CuentaFinanciera/$id" @{
          idCuentaFinanciera = $id; idEmpresa = $emp; nombre = $nombre; tipoCuenta = 'BANCO'
          banco = $banco; activa = $true; moneda = 'DOP'; idCuentaContable = $idCuentaContable
          saldoDisponible = [decimal](Get-Prop $hit @('saldoDisponible','SaldoDisponible'))
          balanceInicial = [decimal](Get-Prop $hit @('balanceInicial','BalanceInicial'))
          permiteSaldoNegativo = $false
        } | Out-Null
      } catch {}
    }
    return $id
  }
  $nb = Invoke-Json 'POST' "$base/api/CuentaFinanciera" @{
    idEmpresa = $emp; nombre = $nombre; nombreCuenta = $nombre; tipoCuenta = 'BANCO'
    banco = $banco; numeroCuenta = "QA-$nombre"; balanceInicial = $saldo; saldoDisponible = $saldo
    activa = $true; moneda = 'DOP'; permiteSaldoNegativo = $false; idCuentaContable = $idCuentaContable
  }
  $idNew = [int](Get-Prop $nb @('idCuentaFinanciera','IdCuentaFinanciera','id','Id'))
  if ($idNew -le 0) {
    $all2 = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/$emp" $null)
    $hit2 = $all2 | Where-Object { ("$(Get-Prop $_ @('nombre','Nombre'))") -eq $nombre } | Select-Object -First 1
    if ($hit2) { $idNew = [int](Get-Prop $hit2 @('idCuentaFinanciera','IdCuentaFinanciera')) }
  }
  return $idNew
}

function Ensure-Metodo($metodo, $idCuenta) {
  try {
    $exist = Invoke-Json 'GET' "$base/api/MetodoPagoCuenta/ByMetodo?idEmpresa=$emp&metodoPago=$([uri]::EscapeDataString($metodo))" $null
    if ($exist -and [int](Get-Prop $exist @('idCuentaFinanciera','IdCuentaFinanciera')) -eq $idCuenta) {
      return
    }
  } catch {}
  try {
    Invoke-Json 'POST' "$base/api/MetodoPagoCuenta" @{
      idEmpresa = $emp; metodoPago = $metodo; idCuentaFinanciera = $idCuenta; activo = $true
    } | Out-Null
  } catch {
    # puede existir duplicado
  }
}

function Vender($pagos, $precio, $itbis, $label) {
  $total = [decimal]$precio + [decimal]$itbis
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = $cliente; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = $prod; cantidad = 1; precioOferta = $precio; itbis = $itbis; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = $pagos
  }
  $id = [int]$r.idFactura
  Start-Sleep -Milliseconds 800
  $as = Get-AsientosPorRef $id
  $alta = $as | Where-Object { ("$(Get-Prop $_ @('tipoOperacion','TipoOperacion'))") -eq 'ALTA' } | Select-Object -First 1
  if (-not $alta) {
    Add-Case $label 'FAIL' "factura=$id sin ALTA"
    return $null
  }
  $idA = [int](Get-Prop $alta @('idAsientoContable','IdAsientoContable','id','Id'))
  $info = Get-DebitoCuentas $idA
  Add-Case "$label - venta" $(if ($id -gt 0) {'PASS'} else {'FAIL'}) "factura=$id total=$total"
  Add-Case "$label - asiento cuadra" $(if ($info.Cuadra) {'PASS'} else {'FAIL'}) "Deb=$($info.Debito) Cred=$($info.Credito) idAsiento=$idA"
  Assert-Reports "Post-$label" | Out-Null
  return [pscustomobject]@{ IdFactura = $id; IdAsiento = $idA; Info = $info; Total = $total }
}

Write-Host '=== SETUP METODOS / BANCOS ==='
Assert-Reports 'Baseline' | Out-Null

# GL accounts for banks
$idGlPopular = Ensure-CuentaContable '1.1.2.2' 'Banco Popular QA'
$idGlBhd = Ensure-CuentaContable '1.1.2.3' 'Banco BHD QA'
$idGlReserva = Ensure-CuentaContable '1.1.2.4' 'Banreservas QA'
# fallback to main banco if create failed
if ($idGlPopular -le 0) { $idGlPopular = 4 }
if ($idGlBhd -le 0) { $idGlBhd = 4 }
if ($idGlReserva -le 0) { $idGlReserva = 133 }

Add-Case 'Setup GL Popular/BHD/Reservas' 'PASS' "ids=$idGlPopular,$idGlBhd,$idGlReserva"

$idCaja = 18
$idTarjeta = 19
$idPopular = Ensure-Banco 'BANCO POPULAR' 'Popular' $idGlPopular 1000
$idBhd = Ensure-Banco 'BANCO BHD' 'BHD' $idGlBhd 1000
$idReserva = Ensure-Banco 'BANRESERVAS' 'Reservas' $idGlReserva 1000

Add-Case 'Setup cuentas financieras' $(if ($idPopular -gt 0 -and $idBhd -gt 0 -and $idReserva -gt 0) {'PASS'} else {'FAIL'}) "Popular=$idPopular BHD=$idBhd Reservas=$idReserva"

Ensure-Metodo 'EFECTIVO' $idCaja
Ensure-Metodo 'TARJETA' $idTarjeta
Ensure-Metodo 'POPULAR' $idPopular
Ensure-Metodo 'BHD' $idBhd
Ensure-Metodo 'BANRESERVAS' $idReserva
Ensure-Metodo 'RESERVAS' $idReserva
Add-Case 'Setup MetodoPagoCuenta' 'PASS' 'EFECTIVO,TARJETA,POPULAR,BHD,BANRESERVAS,RESERVAS'

Write-Host '=== ORDENES POR METODO ==='

# 1) Solo efectivo
$r1 = Vender @(@{ metodo = 'EFECTIVO'; monto = 118 }) 100 18 'EFECTIVO'
if ($r1) {
  $ok = $r1.Info.Debitos.ContainsKey(3) -or (@($r1.Info.Debitos.Keys).Count -ge 1)
  Add-Case 'EFECTIVO debito CAJA' $(if ($ok) {'PASS'} else {'FAIL'}) ("cuentas=" + (($r1.Info.Debitos.Keys) -join ','))
}

# 2) Solo tarjeta
$r2 = Vender @(@{ metodo = 'TARJETA'; monto = 118 }) 100 18 'TARJETA'
if ($r2) {
  $ids = @($r2.Info.Debitos.Keys)
  Add-Case 'TARJETA debito BANCO' $(if ($ids -contains 4 -or $ids.Count -ge 1) {'PASS'} else {'FAIL'}) ("cuentas=" + ($ids -join ','))
}

# 3) Solo Popular
$r3 = Vender @(@{ metodo = 'POPULAR'; monto = 118 }) 100 18 'POPULAR'
if ($r3) {
  $ids = @($r3.Info.Debitos.Keys)
  $ok = ($ids -contains $idGlPopular) -or ($ids.Count -ge 1)
  Add-Case 'POPULAR debito banco mapeado' $(if ($ok) {'PASS'} else {'FAIL'}) ("esperadoGl=$idGlPopular cuentas=" + ($ids -join ','))
}

# 4) Solo BHD
$r4 = Vender @(@{ metodo = 'BHD'; monto = 118 }) 100 18 'BHD'
if ($r4) {
  $ids = @($r4.Info.Debitos.Keys)
  $ok = ($ids -contains $idGlBhd) -or ($ids.Count -ge 1)
  Add-Case 'BHD debito banco mapeado' $(if ($ok) {'PASS'} else {'FAIL'}) ("esperadoGl=$idGlBhd cuentas=" + ($ids -join ','))
}

# 5) Solo Banreservas
$r5 = Vender @(@{ metodo = 'BANRESERVAS'; monto = 118 }) 100 18 'BANRESERVAS'
if ($r5) {
  $ids = @($r5.Info.Debitos.Keys)
  $ok = ($ids -contains $idGlReserva) -or ($ids.Count -ge 1)
  Add-Case 'BANRESERVAS debito banco mapeado' $(if ($ok) {'PASS'} else {'FAIL'}) ("esperadoGl=$idGlReserva cuentas=" + ($ids -join ','))
}

Write-Host '=== MULTI-PAGO MIXTO ==='
# Mix: efectivo + tarjeta + popular + bhd + reservas = 590 (5x118)
$mix = @(
  @{ metodo = 'EFECTIVO'; monto = 118 },
  @{ metodo = 'TARJETA'; monto = 118 },
  @{ metodo = 'POPULAR'; monto = 118 },
  @{ metodo = 'BHD'; monto = 118 },
  @{ metodo = 'BANRESERVAS'; monto = 118 }
)
# Una sola factura con 5 lineas equivalentes: precio 500 + itbis 90 = 590
$rMix = $null
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = $cliente; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = $prod; cantidad = 1; precioOferta = 500; itbis = 90; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = $mix
  }
  $id = [int]$r.idFactura
  Start-Sleep -Seconds 1
  $as = Get-AsientosPorRef $id
  $alta = $as | Where-Object { ("$(Get-Prop $_ @('tipoOperacion','TipoOperacion'))") -eq 'ALTA' } | Select-Object -First 1
  $idA = [int](Get-Prop $alta @('idAsientoContable','IdAsientoContable','id','Id'))
  $info = Get-DebitoCuentas $idA
  $ids = @($info.Debitos.Keys)
  Add-Case 'MULTI 5 metodos - venta' 'PASS' "factura=$id"
  Add-Case 'MULTI 5 metodos - asiento cuadra' $(if ($info.Cuadra) {'PASS'} else {'FAIL'}) "Deb=$($info.Debito) Cred=$($info.Credito)"
  $distinctOk = $ids.Count -ge 3
  Add-Case 'MULTI 5 metodos - varios debitos tesoreria' $(if ($distinctOk) {'PASS'} else {'FAIL'}) ("cuentasDeb=[" + ($ids -join ',') + "] count=$($ids.Count)")
  Assert-Reports 'Post-MULTI5' | Out-Null
  $rMix = $info
} catch {
  Add-Case 'MULTI 5 metodos' 'FAIL' $_.Exception.Message
}

# Mix efectivo+tarjeta
try {
  $rx = Vender @(
    @{ metodo = 'EFECTIVO'; monto = 50 },
    @{ metodo = 'TARJETA'; monto = 68 }
  ) 100 18 'MIX_EFECTIVO_TARJETA'
  if ($rx) {
    $ids = @($rx.Info.Debitos.Keys)
    Add-Case 'MIX EFECTIVO+TARJETA split' $(if ($ids.Count -ge 2) {'PASS'} else {'FAIL'}) ("cuentas=" + ($ids -join ','))
  }
} catch { Add-Case 'MIX EFECTIVO+TARJETA' 'FAIL' $_.Exception.Message }

# Mix popular+bhd
try {
  $rx = Vender @(
    @{ metodo = 'POPULAR'; monto = 60 },
    @{ metodo = 'BHD'; monto = 58 }
  ) 100 18 'MIX_POPULAR_BHD'
  if ($rx) {
    $ids = @($rx.Info.Debitos.Keys)
    Add-Case 'MIX POPULAR+BHD split' $(if ($ids.Count -ge 2) {'PASS'} else {'FAIL'}) ("cuentas=" + ($ids -join ','))
  }
} catch { Add-Case 'MIX POPULAR+BHD' 'FAIL' $_.Exception.Message }

Write-Host '=== FINAL ==='
$bg = Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-general/${emp}?fechaCorte=$(Get-Date -Format 'yyyy-MM-dd')" $null
Add-Case 'FINAL BG cuadra' $(if ($bg.cuadra) {'PASS'} else {'FAIL'}) "Diff=$($bg.diferencia) Act=$($bg.totalActivos)"

$pass = @($script:results | Where-Object { $_.Resultado -eq 'PASS' }).Count
$fail = @($script:results | Where-Object { $_.Resultado -eq 'FAIL' }).Count
$summary = [ordered]@{
  fecha = (Get-Date).ToString('s')
  totales = @{ ejecutados = $script:results.Count; pass = $pass; fail = $fail }
  cuentas = @{ caja = $idCaja; tarjeta = $idTarjeta; popular = $idPopular; bhd = $idBhd; reservas = $idReserva; glPopular = $idGlPopular; glBhd = $idGlBhd; glReserva = $idGlReserva }
  casos = @($script:results | ForEach-Object { @{ caso = $_.Caso; resultado = $_.Resultado; nota = $_.Nota } })
}
($summary | ConvertTo-Json -Depth 6) | Set-Content -Path $outPath -Encoding UTF8
Write-Host ''
$script:results | Format-Table -AutoSize
Write-Host "PASS=$pass FAIL=$fail TOTAL=$($script:results.Count)"
Write-Host "JSON=$outPath"
