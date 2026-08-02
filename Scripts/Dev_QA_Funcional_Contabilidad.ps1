# QA Funcional Contabilidad - AlahiaPos_Dev / Empresa 60
$ErrorActionPreference = 'Continue'
& "$PSScriptRoot\Dev_QA_Assert_Solo_ECF.ps1"
$base = 'http://localhost:5139'
$emp = 60
$usr = 28
$script:results = New-Object System.Collections.Generic.List[object]

function New-Ecf([string]$tipo = '31') {
  if ($tipo -notmatch '^\d{2}$') { throw "Tipo e-CF inválido: $tipo" }
  $n = [int64](Get-Date -Format 'MMddHHmmss')
  $script:ecfOffset = 1 + [int]($script:ecfOffset)
  $secuencia = (($n + $script:ecfOffset) % 10000000000).ToString('0000000000')
  return "E${tipo}${secuencia}"
}

function Invoke-Json($method, $url, $body) {
  $params = @{ Method = $method; Uri = $url; ContentType = 'application/json'; TimeoutSec = 120 }
  if ($null -ne $body) {
    $params.Body = (ConvertTo-Json -InputObject $body -Depth 12 -Compress)
  }
  return Invoke-RestMethod @params
}

function Get-Bg {
  $d = Get-Date -Format 'yyyy-MM-dd'
  return Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-general/${emp}?fechaCorte=$d" $null
}

function Add-Case($caso, $resultado, $nota) {
  $script:results.Add([pscustomobject]@{
    Caso = $caso
    Resultado = $resultado
    Nota = $nota
  }) | Out-Null
  Write-Host "[$resultado] $caso - $nota"
}

function Assert-Bg($label, $bg) {
  $diff = [decimal]$bg.diferencia
  $ok = ($bg.cuadra -eq $true) -and ([math]::Abs($diff) -lt 0.01)
  $msg = if ($ok) { "BG OK Act=$($bg.totalActivos) Pas=$($bg.totalPasivos) Pat=$($bg.totalCapital) RE=$($bg.resultadoEjercicio)" } else { "DESCUADRE Diff=$diff" }
  Add-Case $label $(if ($ok) { 'PASS' } else { 'FAIL' }) $msg
  return $ok
}

Write-Host '=== BASELINE ==='
$bg0 = Get-Bg
Assert-Bg 'Baseline' $bg0 | Out-Null

$idGasto = 0
$idVentaContado = 0
$idVentaCredito = 0

# 1 Gasto
try {
  $g = Invoke-Json 'POST' "$base/api/Gastos" @{
    idEmpresa = $emp; idUsuario = $usr; idEmpleado = $usr; tipoGasto = 'Transporte'; idCategoriaGasto = 1
    tipoComprobante = 'Sin comprobante'; monto = 35.0; formaPago = 'EFECTIVO'; orien = 'EFECTIVO'
    detalle = 'QA funcional gasto'; idCuentaFinanciera = 18
  }
  if ($g.success -eq $false) { Add-Case 'Gasto' 'FAIL' ([string]$g.message) }
  else {
    $idGasto = [int]$g.idGasto
    $adv = [string]$g.contabilidadAdvertencia
    Add-Case 'Gasto' 'PASS' "id=$idGasto adv=$adv"
    Assert-Bg 'Post-Gasto' (Get-Bg) | Out-Null
  }
} catch { Add-Case 'Gasto' 'FAIL' $_.Exception.Message }

# 2 Ingreso
try {
  Invoke-Json 'POST' "$base/api/Ingresos" @{
    idEmpresa = $emp; idUsuario = $usr; descripcion = 'QA funcional ingreso'; categoria = 'Otros ingresos'
    origen = 'Manual'; monto = 80.0; formaPago = 'EFECTIVO'; referencia = 'QA-FUN-ING'
  } | Out-Null
  Add-Case 'Ingreso' 'PASS' 'ok'
  Assert-Bg 'Post-Ingreso' (Get-Bg) | Out-Null
} catch { Add-Case 'Ingreso' 'FAIL' $_.Exception.Message }

# 3 Transferencia
try {
  Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Transferencia" @{
    idEmpresa = $emp; idUsuario = $usr; idCuentaOrigen = 18; idCuentaDestino = 19
    monto = 40.0; motivo = 'QA funcional transferencia'; observacion = 'QA'
  } | Out-Null
  Add-Case 'Transferencia' 'PASS' 'Caja a Banco 40'
  Assert-Bg 'Post-Transferencia' (Get-Bg) | Out-Null
} catch { Add-Case 'Transferencia' 'FAIL' $_.Exception.Message }

# 4 Entrada
try {
  Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Entrada" @{
    idEmpresa = $emp; idUsuario = $usr; idCuentaDestino = 18; monto = 20.0
    motivo = 'QA funcional entrada'; observacion = 'AJUSTE'
  } | Out-Null
  Add-Case 'Entrada ajuste' 'PASS' '20'
  Assert-Bg 'Post-Entrada' (Get-Bg) | Out-Null
} catch { Add-Case 'Entrada ajuste' 'FAIL' $_.Exception.Message }

# 5 Salida
try {
  Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Salida" @{
    idEmpresa = $emp; idUsuario = $usr; idCuentaOrigen = 18; monto = 15.0
    motivo = 'QA funcional salida'; observacion = 'AJUSTE'
  } | Out-Null
  Add-Case 'Salida ajuste' 'PASS' '15'
  Assert-Bg 'Post-Salida' (Get-Bg) | Out-Null
} catch { Add-Case 'Salida ajuste' 'FAIL' $_.Exception.Message }

# 6 Venta contado
try {
  $vc = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @(
        @{ idProducto = 2274; cantidad = 1; precioOferta = 300; itbis = 54; descuento = 0; idEmpleadoComision = 0 }
      )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 354 } )
  }
  $idVentaContado = [int]$vc.idFactura
  Add-Case 'Venta contado' 'PASS' "id=$idVentaContado"
  Assert-Bg 'Post-VentaContado' (Get-Bg) | Out-Null
} catch { Add-Case 'Venta contado' 'FAIL' $_.Exception.Message }

# 7 Venta credito
try {
  $vcr = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Credito'; tipoPago = 'CREDITO'; tipoComprobante = 'FACT'; plazo = '30 dias'
      fechaBencimiento = '2026-08-19T00:00:00'; totalDescuento = 0; idMoso = 0
      facturaDetalles = @(
        @{ idProducto = 2274; cantidad = 1; precioOferta = 200; itbis = 36; descuento = 0; idEmpleadoComision = 0 }
      )
    }
    pagos = @()
  }
  $idVentaCredito = [int]$vcr.idFactura
  Add-Case 'Venta credito' 'PASS' "id=$idVentaCredito"
  Assert-Bg 'Post-VentaCredito' (Get-Bg) | Out-Null
} catch { Add-Case 'Venta credito' 'FAIL' $_.Exception.Message }

# 8 Cobros
if ($idVentaCredito -gt 0) {
  try {
    Invoke-Json 'POST' "$base/api/PagoFacturasClientes/RegistrarPago/$idVentaCredito" @{
      idEmpresa = $emp; idFacturaHeader = $idVentaCredito; formaPago = 'EFECTIVO'; monto = 100.0
      nota = 'QA cobro parcial'; numeroDocumento = ''
    } | Out-Null
    Add-Case 'Cobro parcial' 'PASS' '100'
    Assert-Bg 'Post-CobroParcial' (Get-Bg) | Out-Null
  } catch { Add-Case 'Cobro parcial' 'FAIL' $_.Exception.Message }

  try {
    Invoke-Json 'POST' "$base/api/PagoFacturasClientes/RegistrarPago/$idVentaCredito" @{
      idEmpresa = $emp; idFacturaHeader = $idVentaCredito; formaPago = 'EFECTIVO'; monto = 136.0
      nota = 'QA cobro total'; numeroDocumento = ''
    } | Out-Null
    Add-Case 'Cobro total' 'PASS' '136'
    Assert-Bg 'Post-CobroTotal' (Get-Bg) | Out-Null
  } catch { Add-Case 'Cobro total' 'FAIL' $_.Exception.Message }
} else {
  Add-Case 'Cobro parcial' 'SKIP' 'sin venta credito'
  Add-Case 'Cobro total' 'SKIP' 'sin venta credito'
}

# 9 Compra contado
try {
  $bor = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Contado'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2274; cantidad = 1; precioCompra = 150; descuento = 0; itbis = 27 } )
  }
  $idCompra = 0
  if ($bor.data -and $bor.data.idOrdenCompraHeader) { $idCompra = [int]$bor.data.idOrdenCompraHeader }
  elseif ($bor.idOrdenCompraHeader) { $idCompra = [int]$bor.idOrdenCompraHeader }
  elseif ($bor.id) { $idCompra = [int]$bor.id }
  Invoke-Json 'POST' "$base/api/Compras/$idCompra/Confirmar" @{
    idEmpresa = $emp; idUsuario = $usr; formaPago = 'EFECTIVO'
  } | Out-Null
  Add-Case 'Compra contado' 'PASS' "orden=$idCompra"
  Assert-Bg 'Post-CompraContado' (Get-Bg) | Out-Null
} catch { Add-Case 'Compra contado' 'FAIL' $_.Exception.Message }

# 10 Compra credito + pagos
try {
  $bor2 = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Credito'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2274; cantidad = 1; precioCompra = 100; descuento = 0; itbis = 18 } )
  }
  $idCompraCred = 0
  if ($bor2.data -and $bor2.data.idOrdenCompraHeader) { $idCompraCred = [int]$bor2.data.idOrdenCompraHeader }
  elseif ($bor2.idOrdenCompraHeader) { $idCompraCred = [int]$bor2.idOrdenCompraHeader }
  elseif ($bor2.id) { $idCompraCred = [int]$bor2.id }
  Invoke-Json 'POST' "$base/api/Compras/$idCompraCred/Confirmar" @{
    idEmpresa = $emp; idUsuario = $usr
  } | Out-Null
  Add-Case 'Compra credito' 'PASS' "orden=$idCompraCred"
  Assert-Bg 'Post-CompraCredito' (Get-Bg) | Out-Null

  try {
    Invoke-Json 'POST' "$base/api/Compras/$idCompraCred/Pago" @{
      idEmpresa = $emp; idUsuario = $usr; monto = 50.0; formaPago = 'EFECTIVO'
    } | Out-Null
    Add-Case 'Pago parcial proveedor' 'PASS' '50'
    Assert-Bg 'Post-PagoParcial' (Get-Bg) | Out-Null
  } catch { Add-Case 'Pago parcial proveedor' 'FAIL' $_.Exception.Message }

  try {
    Invoke-Json 'POST' "$base/api/Compras/$idCompraCred/Pago" @{
      idEmpresa = $emp; idUsuario = $usr; monto = 68.0; formaPago = 'EFECTIVO'
    } | Out-Null
    Add-Case 'Pago total proveedor' 'PASS' '68'
    Assert-Bg 'Post-PagoTotal' (Get-Bg) | Out-Null
  } catch { Add-Case 'Pago total proveedor' 'FAIL' $_.Exception.Message }
} catch { Add-Case 'Compra credito' 'FAIL' $_.Exception.Message }

# 11 Inventario
try {
  Invoke-Json 'POST' "$base/api/MovimientosInventario/GuardarMovimiento" @{
    tipoMovimiento = 'ENTRADA'; motivo = 'AJUSTE'; referencia = 'QA-FUN-ENT'; observacion = 'QA'
    idEmpresa = $emp; idUsuario = $usr; idAlmacen = 1; activo = $true
    detalles = @( @{ idProducto = 2274; cantidad = 1; precio = 150; observacion = 'QA' } )
  } | Out-Null
  Add-Case 'Inventario entrada' 'PASS' '1'
  Assert-Bg 'Post-InvEntrada' (Get-Bg) | Out-Null
} catch { Add-Case 'Inventario entrada' 'FAIL' $_.Exception.Message }

try {
  Invoke-Json 'POST' "$base/api/MovimientosInventario/GuardarMovimiento" @{
    tipoMovimiento = 'SALIDA'; motivo = 'AJUSTE'; referencia = 'QA-FUN-SAL'; observacion = 'QA'
    idEmpresa = $emp; idUsuario = $usr; idAlmacen = 1; activo = $true
    detalles = @( @{ idProducto = 2274; cantidad = 1; precio = 150; observacion = 'QA' } )
  } | Out-Null
  Add-Case 'Inventario salida' 'PASS' '1'
  Assert-Bg 'Post-InvSalida' (Get-Bg) | Out-Null
} catch { Add-Case 'Inventario salida' 'FAIL' $_.Exception.Message }

# 12 Nota credito
if ($idVentaContado -gt 0) {
  try {
    $dets = Invoke-Json 'GET' "$base/api/FacturaDetalle/$idVentaContado" $null
  } catch {
    try { $dets = Invoke-Json 'GET' "$base/api/FacturaDetalle/GetDetalleByIdHeader/$idVentaContado" $null } catch { $dets = $null }
  }
  $detId = 0
  if ($dets -is [array] -and $dets.Count -gt 0) { $detId = [int]$dets[0].idFacturaDetalle }
  elseif ($dets.idFacturaDetalle) { $detId = [int]$dets.idFacturaDetalle }

  if ($detId -le 0) {
    # fallback: query via GetFactura
    try {
      $f = Invoke-Json 'GET' "$base/api/FacturaHeader/GetFactura/$idVentaContado" $null
      if ($f.facturaDetalles) { $detId = [int]$f.facturaDetalles[0].idFacturaDetalle }
    } catch {}
  }

  if ($detId -gt 0) {
    try {
      $nc = Invoke-Json 'POST' "$base/api/NotasCredito/Crear" @{
        idFacturaHeader = $idVentaContado; idEmpresa = $emp; idUsuario = $usr; observacion = 'QA NC'
        lineas = @( @{ idFacturaDetalle = $detId; cantidad = 1 } )
      }
      Add-Case 'Nota credito' 'PASS' "id=$($nc.idNotaCredito)"
      Assert-Bg 'Post-NC' (Get-Bg) | Out-Null
    } catch { Add-Case 'Nota credito' 'FAIL' $_.Exception.Message }
  } else {
    Add-Case 'Nota credito' 'FAIL' 'sin idFacturaDetalle'
  }
} else {
  Add-Case 'Nota credito' 'SKIP' 'sin venta contado'
}

# 13 Anular venta
try {
  $van = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @(
        @{ idProducto = 2249; cantidad = 1; precioOferta = 100; itbis = 18; descuento = 0; idEmpleadoComision = 0 }
      )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 118 } )
  }
  $idAnular = [int]$van.idFactura
  Assert-Bg 'Pre-Anular' (Get-Bg) | Out-Null
  Invoke-Json 'POST' "$base/api/FacturaHeader/AnularFactura" @{
    idFacturaHeader = $idAnular; idEmpresa = $emp; idUsuario = $usr
    motivoAnulacion = 'QA funcional anulacion'; usuarioAnulo = 'sena@gmail.com'
  } | Out-Null
  Add-Case 'Anulacion venta' 'PASS' "id=$idAnular"
  Assert-Bg 'Post-Anular' (Get-Bg) | Out-Null
} catch { Add-Case 'Anulacion venta' 'FAIL' $_.Exception.Message }

# 14 Anular gasto
if ($idGasto -gt 0) {
  try {
    Invoke-Json 'POST' "$base/api/Gastos/AnularGasto" @{
      idGasto = $idGasto; idEmpresa = $emp; motivoAnulacion = 'QA anular gasto'; usuarioAnulo = 'sena@gmail.com'
    } | Out-Null
    Add-Case 'Anulacion gasto' 'PASS' "id=$idGasto"
    Assert-Bg 'Post-AnularGasto' (Get-Bg) | Out-Null
  } catch { Add-Case 'Anulacion gasto' 'FAIL' $_.Exception.Message }
}

# Final reports
$bgF = Get-Bg
$hoy = Get-Date -Format 'yyyy-MM-dd'
$er = Invoke-Json 'GET' "$base/api/ContabilidadReportes/estado-resultados/${emp}?desde=2026-01-01&hasta=$hoy" $null
$bc = Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-comprobacion/${emp}?desde=2026-01-01&hasta=$hoy" $null

$erMatch = [math]::Abs([decimal]$er.utilidadNeta - [decimal]$bgF.resultadoEjercicio) -lt 0.02
Add-Case 'ER vs BG Resultado' $(if ($erMatch) { 'PASS' } else { 'FAIL' }) "ER=$($er.utilidadNeta) RE=$($bgF.resultadoEjercicio)"
Add-Case 'Comprobacion' $(if ($bc.cuadra) { 'PASS' } else { 'FAIL' }) "Deb=$($bc.totalDebitos) Cred=$($bc.totalCreditos)"
Add-Case 'BG final' $(if ($bgF.cuadra) { 'PASS' } else { 'FAIL' }) "Diff=$($bgF.diferencia)"

$out = @{
  fecha = (Get-Date).ToString('s')
  baseline = @{ activos = $bg0.totalActivos; pasivos = $bg0.totalPasivos; patrimonio = $bg0.totalCapital; diff = $bg0.diferencia; cuadra = $bg0.cuadra }
  final = @{ activos = $bgF.totalActivos; pasivos = $bgF.totalPasivos; patrimonio = $bgF.totalCapital; resultadoEjercicio = $bgF.resultadoEjercicio; diff = $bgF.diferencia; cuadra = $bgF.cuadra }
  er = @{ ingresos = $er.ingresos.total; costos = $er.costos.total; gastos = $er.gastos.total; utilidadNeta = $er.utilidadNeta }
  comprobacion = @{ debitos = $bc.totalDebitos; creditos = $bc.totalCreditos; deudor = $bc.totalSaldoDeudor; acreedor = $bc.totalSaldoAcreedor; cuadra = $bc.cuadra }
  casos = @($script:results)
}
$outPath = 'C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\Scripts\Dev_QA_Funcional_Resultados.json'
$out | ConvertTo-Json -Depth 8 | Set-Content -Path $outPath -Encoding UTF8

Write-Host ''
Write-Host '=== RESUMEN ==='
$script:results | Format-Table -AutoSize
$pass = @($script:results | Where-Object { $_.Resultado -eq 'PASS' }).Count
$fail = @($script:results | Where-Object { $_.Resultado -eq 'FAIL' }).Count
$skip = @($script:results | Where-Object { $_.Resultado -eq 'SKIP' }).Count
Write-Host "PASS=$pass FAIL=$fail SKIP=$skip"
Write-Host "JSON=$outPath"
