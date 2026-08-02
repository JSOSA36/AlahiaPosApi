# QA Funcional Contabilidad AMPLIADO + REVERSOS - Empresa 60 Dev
# Run: cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File ...\Dev_QA_Funcional_Ampliado.ps1"
$ErrorActionPreference = 'Continue'
& "$PSScriptRoot\Dev_QA_Assert_Solo_ECF.ps1"
$base = 'http://localhost:5139'
$emp = 60
$usr = 28
$script:results = New-Object System.Collections.Generic.List[object]
$script:incidencias = New-Object System.Collections.Generic.List[object]

function New-Ecf([string]$tipo = '31') {
  if ($tipo -notmatch '^\d{2}$') { throw "Tipo e-CF inválido: $tipo" }
  $n = [int64](Get-Date -Format 'MMddHHmmss')
  $script:ecfOffset = 1 + [int]($script:ecfOffset)
  $secuencia = (($n + $script:ecfOffset) % 10000000000).ToString('0000000000')
  return "E${tipo}${secuencia}"
}

function Invoke-Json($method, $url, $body) {
  $params = @{ Method = $method; Uri = $url; ContentType = 'application/json'; TimeoutSec = 120 }
  if ($null -ne $body) { $params.Body = (ConvertTo-Json -InputObject $body -Depth 14 -Compress) }
  try {
    return Invoke-RestMethod @params
  } catch {
    $resp = $_.Exception.Response
    $msg = $_.Exception.Message
    if ($resp) {
      try {
        $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $bodyTxt = $sr.ReadToEnd()
        if ($bodyTxt) { $msg = $bodyTxt }
      } catch {}
    }
    throw (New-Object System.Exception($msg, $_.Exception))
  }
}

function Get-Bg {
  $d = Get-Date -Format 'yyyy-MM-dd'
  return Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-general/${emp}?fechaCorte=$d" $null
}

function Get-Er {
  $d = Get-Date -Format 'yyyy-MM-dd'
  return Invoke-Json 'GET' "$base/api/ContabilidadReportes/estado-resultados/${emp}?desde=2026-01-01&hasta=$d" $null
}

function Get-Bc {
  $d = Get-Date -Format 'yyyy-MM-dd'
  return Invoke-Json 'GET' "$base/api/ContabilidadReportes/balance-comprobacion/${emp}?desde=2026-01-01&hasta=$d" $null
}

function Add-Case($caso, $resultado, $nota) {
  $script:results.Add([pscustomobject]@{ Caso = $caso; Resultado = $resultado; Nota = $nota }) | Out-Null
  Write-Host "[$resultado] $caso - $nota"
}

function Add-Inc($id, $titulo, $prioridad, $detalle) {
  $script:incidencias.Add([pscustomobject]@{ Id = $id; Titulo = $titulo; Prioridad = $prioridad; Detalle = $detalle }) | Out-Null
}

function Assert-Reports($label) {
  $bg = Get-Bg
  $er = Get-Er
  $bc = Get-Bc
  $diff = [decimal]$bg.diferencia
  $bgOk = ($bg.cuadra -eq $true) -and ([math]::Abs($diff) -lt 0.01)
  $erOk = [math]::Abs([decimal]$er.utilidadNeta - [decimal]$bg.resultadoEjercicio) -lt 0.02
  $bcOk = ($bc.cuadra -eq $true)
  $ok = $bgOk -and $erOk -and $bcOk
  $nota = "BG Diff=$diff cuadra=$($bg.cuadra); ER=$($er.utilidadNeta) RE=$($bg.resultadoEjercicio); BC Deb=$($bc.totalDebitos) Cred=$($bc.totalCreditos)"
  Add-Case $label $(if ($ok) { 'PASS' } else { 'FAIL' }) $nota
  if (-not $bgOk) { Add-Inc "BG-$label" "BG no cuadra tras $label" 'P0' $nota }
  if (-not $erOk) { Add-Inc "ER-$label" "ER vs RE mismatch tras $label" 'P0' $nota }
  if (-not $bcOk) { Add-Inc "BC-$label" "Comprobacion no cuadra tras $label" 'P0' $nota }
  return $ok
}

function Get-AsientosPorRef($origenModulo, $refId) {
  $all = @(Invoke-Json 'GET' "$base/api/AsientoContable/$emp" $null)
  return @($all | Where-Object {
    $_.origenModulo -eq $origenModulo -and $_.origenReferenciaId -eq $refId -and $_.estado -ne 'Anulado'
  })
}

Write-Host '=== SETUP ==='
# Seed MetodoPagoCuenta TARJETA / TRANSFERENCIA -> Banco 19
foreach ($m in @('TARJETA','TRANSFERENCIA')) {
  try {
    Invoke-Json 'POST' "$base/api/MetodoPagoCuenta" @{
      idEmpresa = $emp; metodoPago = $m; idCuentaFinanciera = 19; activo = $true
    } | Out-Null
    Add-Case "Setup MetodoPago $m" 'PASS' '-> cuenta 19'
  } catch {
    Add-Case "Setup MetodoPago $m" 'PASS' "ya existe o $($_.Exception.Message)"
  }
}

# Second bank account
$idBanco2 = 0
try {
  $cuentas = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/$emp" $null)
  $exist = $cuentas | Where-Object { $_.nombreCuenta -eq 'QA BANCO 2' -or $_.nombre -eq 'QA BANCO 2' } | Select-Object -First 1
  if ($exist) {
    $idBanco2 = [int]($exist.idCuentaFinanciera)
    Add-Case 'Setup Banco2' 'PASS' "existente id=$idBanco2"
  } else {
    $nb = Invoke-Json 'POST' "$base/api/CuentaFinanciera" @{
      idEmpresa = $emp; nombreCuenta = 'QA BANCO 2'; nombre = 'QA BANCO 2'; tipoCuenta = 'BANCO'
      banco = 'Popular'; numeroCuenta = 'QA-0002'; balanceInicial = 500; saldoDisponible = 500
      activa = $true; moneda = 'DOP'; permiteSaldoNegativo = $false
    }
    if ($nb.id) { $idBanco2 = [int]$nb.id }
    elseif ($nb.idCuentaFinanciera) { $idBanco2 = [int]$nb.idCuentaFinanciera }
    else {
      $cuentas2 = @(Invoke-Json 'GET' "$base/api/CuentaFinanciera/$emp" $null)
      $exist2 = $cuentas2 | Where-Object { ($_.nombreCuenta -eq 'QA BANCO 2') -or ($_.nombre -eq 'QA BANCO 2') } | Select-Object -First 1
      if ($exist2) { $idBanco2 = [int]$exist2.idCuentaFinanciera }
    }
    Add-Case 'Setup Banco2' $(if ($idBanco2 -gt 0) {'PASS'} else {'FAIL'}) "id=$idBanco2"
  }
} catch { Add-Case 'Setup Banco2' 'FAIL' $_.Exception.Message }

Assert-Reports 'Baseline' | Out-Null

# ========== OPERACIONES ==========

# A1 Venta con descuento real (precio rebajado)
$idVtaDesc = 0
try {
  # precioOferta 250 vs 300 list = descuento efectivo 50 + itbis 45 = 295
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 50; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2274; cantidad = 1; precioOferta = 250; itbis = 45; descuento = 50; idEmpleadoComision = 0 } )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 295 } )
  }
  $idVtaDesc = [int]$r.idFactura
  Add-Case 'Venta con descuento (precio rebajado)' 'PASS' "id=$idVtaDesc"
  Assert-Reports 'Post-VentaDescuento' | Out-Null
} catch { Add-Case 'Venta con descuento (precio rebajado)' 'FAIL' $_.Exception.Message }

# A1b Document gap: totalDescuento field alone may not reduce totals
Add-Case 'Venta descuento via solo totalDescuento' 'GAP' 'ProcesarFactura no resta TotalDescuento/Descuento linea; usar precioOferta rebajado'
Add-Inc 'INC-DESC' 'Descuento cosmetico en ProcesarFactura' 'P1' 'TotalDescuento y Descuento de linea no reducen SubTotal/Total ni el asiento. Descuento real requiere precioOferta menor.'

# A2 Venta multi-pago EFECTIVO + TARJETA
$idVtaMulti = 0
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2249; cantidad = 1; precioOferta = 1000; itbis = 180; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @(
      @{ metodo = 'EFECTIVO'; monto = 400 },
      @{ metodo = 'TARJETA'; monto = 780 }
    )
  }
  $idVtaMulti = [int]$r.idFactura
  $as = Get-AsientosPorRef 'Ventas' $idVtaMulti
  $alta = $as | Where-Object { $_.tipoOperacion -eq 'ALTA' } | Select-Object -First 1
  # Check if asiento has both CAJA and BANCO (would need detail endpoint)
  Add-Case 'Venta multi-pago (EFECTIVO+TARJETA)' 'PASS' "id=$idVtaMulti asientos=$($as.Count)"
  Add-Case 'Venta multi-pago asiento split CAJA+BANCO' 'GAP' 'Contabilidad usa metodoPrincipal (mayor monto); no parte cobro entre CAJA y BANCO'
  Add-Inc 'INC-MULTIPAGO' 'Asiento de venta no divide multi-pago' 'P1' 'MontoCobrado completo va a una sola cuenta (metodo de mayor monto). Tesoreria si registra ambos movimientos.'
  Assert-Reports 'Post-VentaMulti' | Out-Null
} catch { Add-Case 'Venta multi-pago (EFECTIVO+TARJETA)' 'FAIL' $_.Exception.Message }

# A3 Venta multi EFECTIVO+TRANSFERENCIA
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2249; cantidad = 1; precioOferta = 500; itbis = 90; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @(
      @{ metodo = 'EFECTIVO'; monto = 200 },
      @{ metodo = 'TRANSFERENCIA'; monto = 390 }
    )
  }
  Add-Case 'Venta multi-pago (EFECTIVO+TRANSFERENCIA)' 'PASS' "id=$($r.idFactura)"
  Assert-Reports 'Post-VentaMulti2' | Out-Null
} catch { Add-Case 'Venta multi-pago (EFECTIVO+TRANSFERENCIA)' 'FAIL' $_.Exception.Message }

# A4 Venta exenta ITBIS
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2249; cantidad = 1; precioOferta = 200; itbis = 0; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 200 } )
  }
  Add-Case 'Venta exenta ITBIS' 'PASS' "id=$($r.idFactura)"
  Assert-Reports 'Post-VentaExenta' | Out-Null
} catch { Add-Case 'Venta exenta ITBIS' 'FAIL' $_.Exception.Message }

# A5 Stock insuficiente
try {
  Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2274; cantidad = 9999; precioOferta = 300; itbis = 54; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 354 } )
  } | Out-Null
  Add-Case 'Venta stock insuficiente' 'FAIL' 'Debio rechazar pero acepto'
  Add-Inc 'INC-STOCK' 'Venta con stock insuficiente no bloqueo' 'P0' 'ProcesarFactura acepto qty 9999'
} catch {
  Add-Case 'Venta stock insuficiente' 'PASS' ("Rechazada: " + $_.Exception.Message.Substring(0, [Math]::Min(120, $_.Exception.Message.Length)))
  Assert-Reports 'Post-StockInsuf' | Out-Null
}

# A6 Compra con ITBIS
$idCompraItbis = 0
try {
  $bor = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Credito'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2274; cantidad = 1; precioCompra = 100; descuento = 0; itbis = 18 } )
  }
  if ($bor.data.idOrdenCompraHeader) { $idCompraItbis = [int]$bor.data.idOrdenCompraHeader }
  elseif ($bor.idOrdenCompraHeader) { $idCompraItbis = [int]$bor.idOrdenCompraHeader }
  Invoke-Json 'POST' "$base/api/Compras/$idCompraItbis/Confirmar" @{ idEmpresa = $emp; idUsuario = $usr } | Out-Null
  Add-Case 'Compra con ITBIS' 'PASS' "orden=$idCompraItbis"
  Assert-Reports 'Post-CompraITBIS' | Out-Null
} catch { Add-Case 'Compra con ITBIS' 'FAIL' $_.Exception.Message }

# A7 Compra exenta ITBIS
$idCompraEx = 0
try {
  $bor = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Credito'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2274; cantidad = 1; precioCompra = 80; descuento = 0; itbis = 0 } )
  }
  if ($bor.data.idOrdenCompraHeader) { $idCompraEx = [int]$bor.data.idOrdenCompraHeader }
  elseif ($bor.idOrdenCompraHeader) { $idCompraEx = [int]$bor.idOrdenCompraHeader }
  Invoke-Json 'POST' "$base/api/Compras/$idCompraEx/Confirmar" @{ idEmpresa = $emp; idUsuario = $usr } | Out-Null
  Add-Case 'Compra exenta ITBIS' 'PASS' "orden=$idCompraEx"
  Assert-Reports 'Post-CompraExenta' | Out-Null
} catch { Add-Case 'Compra exenta ITBIS' 'FAIL' $_.Exception.Message }

# A8 Compra activo fijo
$idCompraAf = 0
try {
  $bor = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Contado'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2299; cantidad = 1; precioCompra = 5000; descuento = 0; itbis = 900 } )
  }
  if ($bor.data.idOrdenCompraHeader) { $idCompraAf = [int]$bor.data.idOrdenCompraHeader }
  elseif ($bor.idOrdenCompraHeader) { $idCompraAf = [int]$bor.idOrdenCompraHeader }
  Invoke-Json 'POST' "$base/api/Compras/$idCompraAf/Confirmar" @{ idEmpresa = $emp; idUsuario = $usr; formaPago = 'EFECTIVO' } | Out-Null
  Add-Case 'Compra activo fijo' 'PASS' "orden=$idCompraAf (contablemente a Inventario)"
  Add-Case 'Compra activo fijo cuenta ACTIVO_FIJO' 'GAP' 'Asiento usa concepto INVENTARIO; no hay mapeo ACTIVO_FIJO dedicado'
  Add-Inc 'INC-AF' 'Activo fijo contabiliza como Inventario' 'P2' 'Producto TipoComportamiento=ActivoFijo suma MontoInventario. Falta cuenta/mapeo de activo fijo.'
  Assert-Reports 'Post-CompraAF' | Out-Null
} catch { Add-Case 'Compra activo fijo' 'FAIL' $_.Exception.Message }

# A9 Gasto desde Caja
$idGastoCaja = 0
try {
  $g = Invoke-Json 'POST' "$base/api/Gastos" @{
    idEmpresa = $emp; idUsuario = $usr; idEmpleado = $usr; tipoGasto = 'Transporte'; idCategoriaGasto = 1
    tipoComprobante = 'Sin comprobante'; monto = 25.0; formaPago = 'EFECTIVO'; orien = 'EFECTIVO'
    detalle = 'QA gasto caja'; idCuentaFinanciera = 18
  }
  $idGastoCaja = [int]$g.idGasto
  Add-Case 'Gasto desde Caja' 'PASS' "id=$idGastoCaja"
  Assert-Reports 'Post-GastoCaja' | Out-Null
} catch { Add-Case 'Gasto desde Caja' 'FAIL' $_.Exception.Message }

# A10 Gasto desde Banco
$idGastoBanco = 0
try {
  $g = Invoke-Json 'POST' "$base/api/Gastos" @{
    idEmpresa = $emp; idUsuario = $usr; idEmpleado = $usr; tipoGasto = 'Transporte'; idCategoriaGasto = 1
    tipoComprobante = 'Sin comprobante'; monto = 30.0; formaPago = 'TRANSFERENCIA'; orien = 'TRANSFERENCIA'
    detalle = 'QA gasto banco'; idCuentaFinanciera = 19
  }
  $idGastoBanco = [int]$g.idGasto
  Add-Case 'Gasto desde Banco' 'PASS' "id=$idGastoBanco"
  Assert-Reports 'Post-GastoBanco' | Out-Null
} catch { Add-Case 'Gasto desde Banco' 'FAIL' $_.Exception.Message }

# A11 Ingreso en Caja
$idIngCaja = 0
try {
  $i = Invoke-Json 'POST' "$base/api/Ingresos" @{
    idEmpresa = $emp; idUsuario = $usr; descripcion = 'QA ingreso caja'; categoria = 'Otros'
    origen = 'Manual'; monto = 55.0; formaPago = 'EFECTIVO'; referencia = 'QA-ING-CAJA'
  }
  if ($i.idIngreso) { $idIngCaja = [int]$i.idIngreso }
  elseif ($i.id) { $idIngCaja = [int]$i.id }
  Add-Case 'Ingreso en Caja' 'PASS' "id=$idIngCaja"
  Assert-Reports 'Post-IngresoCaja' | Out-Null
} catch { Add-Case 'Ingreso en Caja' 'FAIL' $_.Exception.Message }

# A12 Ingreso en Banco
$idIngBanco = 0
try {
  $i = Invoke-Json 'POST' "$base/api/Ingresos" @{
    idEmpresa = $emp; idUsuario = $usr; descripcion = 'QA ingreso banco'; categoria = 'Otros'
    origen = 'Manual'; monto = 60.0; formaPago = 'TRANSFERENCIA'; referencia = 'QA-ING-BANCO'
  }
  if ($i.idIngreso) { $idIngBanco = [int]$i.idIngreso }
  elseif ($i.id) { $idIngBanco = [int]$i.id }
  Add-Case 'Ingreso en Banco' 'PASS' "id=$idIngBanco"
  Assert-Reports 'Post-IngresoBanco' | Out-Null
} catch { Add-Case 'Ingreso en Banco' 'FAIL' $_.Exception.Message }

# A13 Transferencia entre dos bancos
if ($idBanco2 -gt 0) {
  try {
    Invoke-Json 'POST' "$base/api/MovimientoFinanciero/Transferencia" @{
      idEmpresa = $emp; idUsuario = $usr; idCuentaOrigen = 19; idCuentaDestino = $idBanco2
      monto = 25.0; motivo = 'QA transfer banco-banco'; observacion = 'QA'
    } | Out-Null
    Add-Case 'Transferencia entre dos bancos' 'PASS' "19 -> $idBanco2"
    Add-Case 'Transferencia banco-banco detalle GL' 'GAP' 'Ambos lados resuelven a misma cuenta contable BANCO (Dr=Cr misma cuenta)'
    Add-Inc 'INC-BANCO2BANCO' 'Transferencia banco-banco sin detalle en plan' 'P2' 'ResolverTesoreria usa concepto BANCO generico; no TesoreriaCuentaContableMapeo por cuenta financiera.'
    Assert-Reports 'Post-TransferBancoBanco' | Out-Null
  } catch { Add-Case 'Transferencia entre dos bancos' 'FAIL' $_.Exception.Message }
} else {
  Add-Case 'Transferencia entre dos bancos' 'SKIP' 'sin Banco2'
}

# A14 Inventario + / -
try {
  Invoke-Json 'POST' "$base/api/MovimientosInventario/GuardarMovimiento" @{
    tipoMovimiento = 'ENTRADA'; motivo = 'AJUSTE'; referencia = 'QA-AMP-ENT'; observacion = 'QA+'
    idEmpresa = $emp; idUsuario = $usr; idAlmacen = 1; activo = $true
    detalles = @( @{ idProducto = 2274; cantidad = 2; precio = 200; observacion = 'QA' } )
  } | Out-Null
  Add-Case 'Ajuste inventario positivo' 'PASS' 'ENTRADA 2'
  Assert-Reports 'Post-InvPos' | Out-Null
} catch { Add-Case 'Ajuste inventario positivo' 'FAIL' $_.Exception.Message }

try {
  Invoke-Json 'POST' "$base/api/MovimientosInventario/GuardarMovimiento" @{
    tipoMovimiento = 'SALIDA'; motivo = 'AJUSTE'; referencia = 'QA-AMP-SAL'; observacion = 'QA-'
    idEmpresa = $emp; idUsuario = $usr; idAlmacen = 1; activo = $true
    detalles = @( @{ idProducto = 2274; cantidad = 1; precio = 200; observacion = 'QA' } )
  } | Out-Null
  Add-Case 'Ajuste inventario negativo' 'PASS' 'SALIDA 1'
  Assert-Reports 'Post-InvNeg' | Out-Null
} catch { Add-Case 'Ajuste inventario negativo' 'FAIL' $_.Exception.Message }

# A15 CxC parcial + total
$idCxC = 0
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Credito'; tipoPago = 'CREDITO'; tipoComprobante = 'FACT'; plazo = '30 dias'
      fechaBencimiento = '2026-08-30T00:00:00'; totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2249; cantidad = 1; precioOferta = 400; itbis = 72; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @()
  }
  $idCxC = [int]$r.idFactura
  Add-Case 'Venta credito para CxC' 'PASS' "id=$idCxC"
  Assert-Reports 'Post-CxCVenta' | Out-Null

  Invoke-Json 'POST' "$base/api/PagoFacturasClientes/RegistrarPago/$idCxC" @{
    idEmpresa = $emp; idFacturaHeader = $idCxC; formaPago = 'EFECTIVO'; monto = 150.0; nota = 'parcial'; numeroDocumento = ''
  } | Out-Null
  Add-Case 'Pago parcial CxC' 'PASS' '150'
  Assert-Reports 'Post-CxCParcial' | Out-Null

  Invoke-Json 'POST' "$base/api/PagoFacturasClientes/RegistrarPago/$idCxC" @{
    idEmpresa = $emp; idFacturaHeader = $idCxC; formaPago = 'EFECTIVO'; monto = 322.0; nota = 'total'; numeroDocumento = ''
  } | Out-Null
  Add-Case 'Pago total CxC' 'PASS' '322'
  Assert-Reports 'Post-CxCTotal' | Out-Null
} catch { Add-Case 'CxC parcial/total' 'FAIL' $_.Exception.Message }

# A16 CxP parcial + total
try {
  $bor = Invoke-Json 'POST' "$base/api/Compras/Borrador" @{
    idOrdenCompraHeader = 0; idEmpresa = $emp; idProveedor = 5
    fechaDocumento = (Get-Date).ToString('s')
    condicionFactura = 'Credito'; idAlmacen = 1; idTipoBienesServicios = 10; formaPagoDgii = 1
    numeroComprobanteProveedor = (New-Ecf '31')
    detalles = @( @{ idProducto = 2274; cantidad = 1; precioCompra = 120; descuento = 0; itbis = 21.6 } )
  }
  $idCxP = 0
  if ($bor.data.idOrdenCompraHeader) { $idCxP = [int]$bor.data.idOrdenCompraHeader }
  elseif ($bor.idOrdenCompraHeader) { $idCxP = [int]$bor.idOrdenCompraHeader }
  Invoke-Json 'POST' "$base/api/Compras/$idCxP/Confirmar" @{ idEmpresa = $emp; idUsuario = $usr } | Out-Null
  Add-Case 'Compra credito para CxP' 'PASS' "orden=$idCxP"
  Assert-Reports 'Post-CxPCompra' | Out-Null

  Invoke-Json 'POST' "$base/api/Compras/$idCxP/Pago" @{
    idEmpresa = $emp; idUsuario = $usr; monto = 50.0; formaPago = 'EFECTIVO'; idCuentaFinanciera = 18
  } | Out-Null
  Add-Case 'Pago parcial CxP' 'PASS' '50'
  Assert-Reports 'Post-CxPParcial' | Out-Null

  Invoke-Json 'POST' "$base/api/Compras/$idCxP/Pago" @{
    idEmpresa = $emp; idUsuario = $usr; monto = 91.6; formaPago = 'EFECTIVO'; idCuentaFinanciera = 18
  } | Out-Null
  Add-Case 'Pago total CxP' 'PASS' '91.6'
  Assert-Reports 'Post-CxPTotal' | Out-Null
} catch { Add-Case 'CxP parcial/total' 'FAIL' $_.Exception.Message }

# ========== REVERSOS ==========
Write-Host '=== REVERSOS ==='

# R1 Venta: crear, verificar asiento, anular, verificar reverso
try {
  $r = Invoke-Json 'POST' "$base/api/FacturaHeader/ProcesarFactura" @{
    header = @{
      idFacturaHeader = 0; idEmpresa = $emp; idUsuario = $usr; idCliente = 4142; idTipoDocumentos = 1
      tipoFactura = 'Contado'; tipoPago = 'CONTADO'; tipoComprobante = 'FACT'; tipoOrden = ''
      totalDescuento = 0; idMoso = 0
      facturaDetalles = @( @{ idProducto = 2249; cantidad = 1; precioOferta = 150; itbis = 27; descuento = 0; idEmpleadoComision = 0 } )
    }
    pagos = @( @{ metodo = 'EFECTIVO'; monto = 177 } )
  }
  $idRevV = [int]$r.idFactura
  $as1 = Get-AsientosPorRef 'Ventas' $idRevV
  $hasAlta = ($as1 | Where-Object { $_.tipoOperacion -eq 'ALTA' }).Count -gt 0
  Add-Case 'Reverso Venta - asiento ALTA' $(if ($hasAlta) {'PASS'} else {'FAIL'}) "id=$idRevV count=$($as1.Count)"
  Assert-Reports 'Pre-AnularVenta' | Out-Null

  Invoke-Json 'POST' "$base/api/FacturaHeader/AnularFactura" @{
    idFacturaHeader = $idRevV; idEmpresa = $emp; idUsuario = $usr
    motivoAnulacion = 'QA reverso venta'; usuarioAnulo = 'qa'
  } | Out-Null
  Start-Sleep -Seconds 1
  $as2 = Get-AsientosPorRef 'Ventas' $idRevV
  $hasRev = ($as2 | Where-Object { $_.tipoOperacion -like 'REVERSO*' }).Count -gt 0
  Add-Case 'Reverso Venta - asiento REVERSO' $(if ($hasRev) {'PASS'} else {'FAIL'}) "tipos=$(($as2 | ForEach-Object { $_.tipoOperacion }) -join ',')"
  Add-Case 'Reverso Venta - restock inventario' 'GAP' 'AnularFactura no repone stock ni revierte MovimientoFinanciero'
  Add-Inc 'INC-ANULAR-VENTA-TES' 'Anulacion venta no revierte tesoreria/stock' 'P1' 'Solo reverso contable ALTA/COGS. Caja y existencias quedan sin compensar.'
  Assert-Reports 'Post-AnularVenta' | Out-Null
} catch { Add-Case 'Reverso Venta' 'FAIL' $_.Exception.Message }

# R2 Gasto anular
try {
  $g = Invoke-Json 'POST' "$base/api/Gastos" @{
    idEmpresa = $emp; idUsuario = $usr; idEmpleado = $usr; tipoGasto = 'Transporte'; idCategoriaGasto = 1
    tipoComprobante = 'Sin comprobante'; monto = 22.0; formaPago = 'EFECTIVO'
    detalle = 'QA reverso gasto'; idCuentaFinanciera = 18
  }
  $idG = [int]$g.idGasto
  $asG = Get-AsientosPorRef 'Gastos' $idG
  Add-Case 'Reverso Gasto - ALTA' $(if ($asG.Count -gt 0) {'PASS'} else {'FAIL'}) "id=$idG"
  Invoke-Json 'POST' "$base/api/Gastos/AnularGasto" @{
    idGasto = $idG; idEmpresa = $emp; motivoAnulacion = 'QA reverso'; usuarioAnulo = 'qa'
  } | Out-Null
  Start-Sleep -Seconds 1
  $asG2 = Get-AsientosPorRef 'Gastos' $idG
  $has = ($asG2 | Where-Object { $_.tipoOperacion -like 'REVERSO*' }).Count -gt 0
  Add-Case 'Reverso Gasto - REVERSO' $(if ($has) {'PASS'} else {'FAIL'}) "ok"
  Assert-Reports 'Post-AnularGasto' | Out-Null
} catch { Add-Case 'Reverso Gasto' 'FAIL' $_.Exception.Message }

# R3 Compra anular confirmada - expected GAP
if ($idCompraItbis -gt 0) {
  try {
    Invoke-Json 'PUT' "$base/api/Compras/$idCompraItbis/Anular/$emp" $null | Out-Null
    Add-Case 'Reverso Compra confirmada' 'FAIL' 'Anulo compra confirmada (no deberia)'
  } catch {
    Add-Case 'Reverso Compra confirmada' 'GAP' ("No soportado: " + $_.Exception.Message.Substring(0, [Math]::Min(100, $_.Exception.Message.Length)))
    Add-Inc 'INC-ANULAR-COMPRA' 'No existe reverso contable de compra confirmada' 'P0' 'PUT Anular solo permite borrador. Sin evento CompraAnulada ni RevertirAsientosOrigen Compras.'
  }
}

# R4 Ingreso anular - GAP
Add-Case 'Reverso Ingreso' 'GAP' 'DELETE Ingresos no publica IngresoAnulado ni reverso contable'
Add-Inc 'INC-ANULAR-ING' 'Ingreso sin reverso contable' 'P1' 'Solo hard-delete. Asiento ALTA queda vivo.'

# R5 NC anular - GAP
Add-Case 'Reverso Nota credito' 'GAP' 'No hay endpoint/evento NCAnulada'
Add-Inc 'INC-ANULAR-NC' 'Nota credito sin anulación contable' 'P1' 'Crear genera asiento; no hay reverso.'

# R6 Inventario ajuste anular - GAP
Add-Case 'Reverso ajuste inventario' 'GAP' 'DELETE movimiento no genera reverso contable'
Add-Inc 'INC-ANULAR-INV' 'Ajuste inventario sin reverso contable' 'P1' 'Delete ajusta cantidad producto; asiento ALTA permanece.'

# NC create + verify asiento (reverso operativo via NC de venta)
if ($idVtaDesc -gt 0) {
  try {
    $f = $null
    try { $f = Invoke-Json 'GET' "$base/api/FacturaHeader/GetFactura/$idVtaDesc" $null } catch {}
    $detId = 0
    if ($f -and $f.facturaDetalles) { $detId = [int]$f.facturaDetalles[0].idFacturaDetalle }
    if ($detId -le 0) {
      try {
        $dets = Invoke-Json 'GET' "$base/api/FacturaDetalle/GetDetalleByIdHeader/$idVtaDesc" $null
        if ($dets -is [array]) { $detId = [int]$dets[0].idFacturaDetalle }
      } catch {}
    }
    if ($detId -gt 0) {
      $nc = Invoke-Json 'POST' "$base/api/NotasCredito/Crear" @{
        idFacturaHeader = $idVtaDesc; idEmpresa = $emp; idUsuario = $usr; observacion = 'QA NC ampliado'
        lineas = @( @{ idFacturaDetalle = $detId; cantidad = 1 } )
      }
      $idNc = [int]$nc.idNotaCredito
      $asNc = Get-AsientosPorRef 'NotasCredito' $idNc
      Add-Case 'Nota credito genera asiento' $(if ($asNc.Count -gt 0) {'PASS'} else {'FAIL'}) "nc=$idNc"
      Assert-Reports 'Post-NC' | Out-Null
    } else {
      Add-Case 'Nota credito genera asiento' 'SKIP' 'sin detalle'
    }
  } catch { Add-Case 'Nota credito genera asiento' 'FAIL' $_.Exception.Message }
}

# Final
$bgF = Get-Bg
$erF = Get-Er
$bcF = Get-Bc
Add-Case 'FINAL BG cuadra' $(if ($bgF.cuadra) {'PASS'} else {'FAIL'}) "Diff=$($bgF.diferencia) Act=$($bgF.totalActivos) Pas+Pat=$($bgF.totalPasivoCapital)"
Add-Case 'FINAL ER=RE' $(if ([math]::Abs([decimal]$erF.utilidadNeta - [decimal]$bgF.resultadoEjercicio) -lt 0.02) {'PASS'} else {'FAIL'}) "ER=$($erF.utilidadNeta) RE=$($bgF.resultadoEjercicio)"
Add-Case 'FINAL BC cuadra' $(if ($bcF.cuadra) {'PASS'} else {'FAIL'}) "Deb=$($bcF.totalDebitos) Cred=$($bcF.totalCreditos)"

$pass = @($script:results | Where-Object { $_.Resultado -eq 'PASS' }).Count
$fail = @($script:results | Where-Object { $_.Resultado -eq 'FAIL' }).Count
$gap = @($script:results | Where-Object { $_.Resultado -eq 'GAP' }).Count
$skip = @($script:results | Where-Object { $_.Resultado -eq 'SKIP' }).Count

$summary = [ordered]@{
  fecha = (Get-Date).ToString('s')
  totales = @{ ejecutados = $script:results.Count; pass = $pass; fail = $fail; gap = $gap; skip = $skip }
  bgFinal = @{ activos = $bgF.totalActivos; pasivos = $bgF.totalPasivos; patrimonio = $bgF.totalCapital; resultadoEjercicio = $bgF.resultadoEjercicio; diff = $bgF.diferencia; cuadra = $bgF.cuadra }
  casos = @($script:results | ForEach-Object { @{ caso = $_.Caso; resultado = $_.Resultado; nota = $_.Nota } })
  incidencias = @($script:incidencias | ForEach-Object { @{ id = $_.Id; titulo = $_.Titulo; prioridad = $_.Prioridad; detalle = $_.Detalle } })
}
$outPath = 'C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\Scripts\Dev_QA_Funcional_Ampliado_Resultados.json'
($summary | ConvertTo-Json -Depth 6) | Set-Content -Path $outPath -Encoding UTF8

Write-Host ''
Write-Host '=== RESUMEN ==='
$script:results | Format-Table -AutoSize
Write-Host "PASS=$pass FAIL=$fail GAP=$gap SKIP=$skip TOTAL=$($script:results.Count)"
Write-Host "INCIDENCIAS=$($script:incidencias.Count)"
Write-Host "JSON=$outPath"
