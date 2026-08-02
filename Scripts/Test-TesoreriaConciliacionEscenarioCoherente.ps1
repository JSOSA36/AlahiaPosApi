<#
.SYNOPSIS
  Escenario coherente de Conciliación Bancaria en AlahiaPos_Dev.
.DESCRIPTION
  1) Limpia residuos Popular/QA (SQL)
  2) Crea cuenta bancaria limpia + caja origen
  3) Siembra operaciones ERP (cobro, pago, gasto, ingreso, transferencia)
  4) Genera CSV sintético (espejo + comisión + interés)
  5) Preview → Confirmar → Matching → CREAR_GASTO/CREAR_INGRESO
  6) Valida saldos, KPIs vs listados, diferencia 0 y auditoría
#>
param(
    [int]$IdEmpresa = 60,
    [int]$IdUsuario = 28,
    [string]$ApiBase = "http://localhost:5139",
    [string]$SqlServer = "144.126.143.154\SQLEXPRESS,1433",
    [string]$SqlUser = "sa",
    [string]$SqlPassword = "JoelAriel8787",
    [string]$Db = "AlahiaPos_Dev",
    [switch]$SkipCleanup
)

$ErrorActionPreference = "Stop"
$culture = [System.Globalization.CultureInfo]::GetCultureInfo("en-US")
$hoy = Get-Date
$fechaCsv = $hoy.ToString("dd/MM/yyyy")
$tag = "QA-CB-{0}" -f $hoy.ToString("yyyyMMddHHmmss")
$fails = New-Object System.Collections.Generic.List[string]

function Invoke-Sql([string]$Query) {
    $tmp = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tmp, $Query, [System.Text.UTF8Encoding]::new($false))
    try {
        sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -d $Db -C -I -i $tmp
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd exit $LASTEXITCODE" }
    } finally {
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-SqlScalar([string]$Query) {
    $out = sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -d $Db -C -I -h -1 -W -Q "SET NOCOUNT ON; $Query"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd scalar failed: $out" }
    return ($out | Where-Object { $_ -and $_.Trim() -ne "" -and $_ -notmatch "rows affected" } | Select-Object -First 1).Trim()
}

function Invoke-ApiJson([string]$Method, [string]$Url, $Body = $null) {
    $headers = @{ "Content-Type" = "application/json" }
    try {
        if ($null -eq $Body) {
            return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers
        }
        $json = $Body | ConvertTo-Json -Depth 8 -Compress
        return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -Body $json
    } catch {
        $resp = $_.Exception.Response
        $detail = $_.ErrorDetails.Message
        if (-not $detail -and $resp) {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $detail = $reader.ReadToEnd()
        }
        throw "API $Method $Url falló: $detail"
    }
}

function Assert-True([bool]$Cond, [string]$Msg) {
    if (-not $Cond) { $fails.Add($Msg) | Out-Null; Write-Warning "FAIL: $Msg" }
    else { Write-Host "OK: $Msg" -ForegroundColor Green }
}

Write-Host "=== QA Conciliación coherente · $tag ===" -ForegroundColor Cyan
Write-Host "API=$ApiBase Empresa=$IdEmpresa Usuario=$IdUsuario DB=$Db"

# ---------------------------------------------------------------------------
# 1) Cleanup
# ---------------------------------------------------------------------------
if (-not $SkipCleanup) {
    Write-Host "`n[1] Cleanup Popular/QA..." -ForegroundColor Cyan
    $cleanupPath = Join-Path $PSScriptRoot "Dev_QA_Conciliacion_Cleanup_Popular.sql"
    sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -d $Db -C -I -i $cleanupPath
    if ($LASTEXITCODE -ne 0) { throw "Cleanup SQL falló" }
}

# ---------------------------------------------------------------------------
# 2) Cuentas
# ---------------------------------------------------------------------------
Write-Host "`n[2] Crear cuentas QA..." -ForegroundColor Cyan

$cuentaBanco = Invoke-ApiJson POST "$ApiBase/api/CuentaFinanciera" @{
    idEmpresa = $IdEmpresa
    nombre = "QA Conciliación Popular Limpia"
    tipoCuenta = "BANCO"
    banco = "Popular"
    numeroCuenta = "QA-$tag"
    balanceInicial = 100000.00
    fechaSaldoInicial = $hoy.ToString("yyyy-MM-dd")
    saldoDisponible = 100000.00
    permiteMovimientosManuales = $true
    activa = $true
    moneda = "DOP"
    codigo = $tag.Substring(0, [Math]::Min(30, $tag.Length))
    descripcion = "Cuenta limpia para escenario coherente de conciliacion"
}
$idBanco = [int]$cuentaBanco.idCuentaFinanciera
Write-Host "Cuenta banco #$idBanco"

# Caja origen para transferencia (si no hay una caja usable, crear)
$cuentas = Invoke-ApiJson GET "$ApiBase/api/CuentaFinanciera/$IdEmpresa"
$caja = $cuentas | Where-Object { $_.tipoCuenta -eq "CAJA" -and $_.activa -eq $true } | Select-Object -First 1
if (-not $caja) {
    $caja = Invoke-ApiJson POST "$ApiBase/api/CuentaFinanciera" @{
        idEmpresa = $IdEmpresa
        nombre = "QA Caja Conciliación"
        tipoCuenta = "CAJA"
        balanceInicial = 50000.00
        fechaSaldoInicial = $hoy.ToString("yyyy-MM-dd")
        saldoDisponible = 50000.00
        permiteMovimientosManuales = $true
        activa = $true
        moneda = "DOP"
        codigo = "QA-CAJA-$($hoy.ToString('HHmmss'))"
    }
}
$idCaja = [int]$caja.idCuentaFinanciera
Write-Host "Cuenta caja #$idCaja ($($caja.nombre))"

# Asegurar fondos en caja para transferencia
try {
    Invoke-ApiJson POST "$ApiBase/api/MovimientoFinanciero/Entrada" @{
        idEmpresa = $IdEmpresa
        idUsuario = $IdUsuario
        idCuentaDestino = $idCaja
        monto = 10000.00
        motivo = "$tag FONDO CAJA"
        observacion = "Seed fondos transferencia"
        categoria = "AJUSTE"
        claveIdempotencia = "$tag-FONDO-CAJA"
    } | Out-Null
} catch {
    Write-Host "Nota fondo caja: $($_.Exception.Message)"
}

# ---------------------------------------------------------------------------
# 3) Operaciones ERP (movimientos reales)
# ---------------------------------------------------------------------------
Write-Host "`n[3] Sembrar operaciones ERP..." -ForegroundColor Cyan

$ops = @(
    @{ Tipo = "ENTRADA"; Monto = 25000.00; Motivo = "$tag COBRO FACTURA CLIENTE"; Ref = "QA-CB-COBRO-25000"; Categoria = "COBRO"; Desc = "Cobro factura por transferencia" },
    @{ Tipo = "SALIDA";  Monto = 12000.00; Motivo = "$tag PAGO SUPLIDOR";          Ref = "QA-CB-PAGO-12000";  Categoria = "PAGO";  Desc = "Pago a suplidor desde banco" },
    @{ Tipo = "SALIDA";  Monto = 3500.00;  Motivo = "$tag GASTO OPERATIVO";        Ref = "QA-CB-GASTO-3500";  Categoria = "GASTO"; Desc = "Gasto pagado desde banco" },
    @{ Tipo = "ENTRADA"; Monto = 2000.00;  Motivo = "$tag INGRESO EXTRA";          Ref = "QA-CB-ING-2000";    Categoria = "INGRESO"; Desc = "Ingreso registrado en banco" }
)

foreach ($op in $ops) {
    if ($op.Tipo -eq "ENTRADA") {
        Invoke-ApiJson POST "$ApiBase/api/MovimientoFinanciero/Entrada" @{
            idEmpresa = $IdEmpresa
            idUsuario = $IdUsuario
            idCuentaDestino = $idBanco
            monto = $op.Monto
            motivo = $op.Motivo
            observacion = $op.Ref
            categoria = $op.Categoria
            claveIdempotencia = "$tag-$($op.Ref)"
            referenciaTipo = "QA_SEED"
        } | Out-Null
    } else {
        Invoke-ApiJson POST "$ApiBase/api/MovimientoFinanciero/Salida" @{
            idEmpresa = $IdEmpresa
            idUsuario = $IdUsuario
            idCuentaOrigen = $idBanco
            monto = $op.Monto
            motivo = $op.Motivo
            observacion = $op.Ref
            categoria = $op.Categoria
            claveIdempotencia = "$tag-$($op.Ref)"
            referenciaTipo = "QA_SEED"
        } | Out-Null
    }
    Write-Host "  + $($op.Tipo) $($op.Monto) $($op.Ref)"
}

Invoke-ApiJson POST "$ApiBase/api/MovimientoFinanciero/Transferencia" @{
    idEmpresa = $IdEmpresa
    idUsuario = $IdUsuario
    idCuentaOrigen = $idCaja
    idCuentaDestino = $idBanco
    monto = 5000.00
    motivo = "$tag TRANSFERENCIA CAJA A BANCO"
    observacion = "QA-CB-TRF-5000"
} | Out-Null
Write-Host "  + TRANSFERENCIA 5000 QA-CB-TRF-5000"

$estadoPre = Invoke-ApiJson GET "$ApiBase/api/MovimientoFinanciero/EstadoCuenta/${idBanco}?desde=$($hoy.ToString('yyyy-MM-dd'))&hasta=$($hoy.ToString('yyyy-MM-dd'))"
$saldoLibrosPre = [decimal]$estadoPre.saldoFinal
Write-Host "Saldo libros post-seed: $saldoLibrosPre"

# Esperado libros: 100000 + 25000 - 12000 - 3500 + 2000 + 5000 = 116500
$saldoLibrosEsperado = 116500.00
Assert-True ([math]::Abs($saldoLibrosPre - $saldoLibrosEsperado) -lt 0.02) "Saldo libros seed = $saldoLibrosEsperado (obtuvo $saldoLibrosPre)"

# ---------------------------------------------------------------------------
# 4) Extracto sintético CSV (espejo + solo-banco)
# ---------------------------------------------------------------------------
Write-Host "`n[4] Generar e importar extracto..." -ForegroundColor Cyan

# Extracto: mismos 5 movimientos + comisión 150 + interés 87.55 (monto raro evita falso cruzado)
# Saldo final banco = 116500 - 150 + 87.55 = 116437.55
$saldoIni = 100000.00
$saldoFin = 116437.55
$csv = @"
Fecha,Descripcion,Referencia,Debito,Credito,Balance
$fechaCsv,$tag COBRO FACTURA CLIENTE,QA-CB-COBRO-25000,0,25000.00,125000.00
$fechaCsv,$tag PAGO SUPLIDOR,QA-CB-PAGO-12000,12000.00,0,113000.00
$fechaCsv,$tag GASTO OPERATIVO,QA-CB-GASTO-3500,3500.00,0,109500.00
$fechaCsv,$tag INGRESO EXTRA,QA-CB-ING-2000,0,2000.00,111500.00
$fechaCsv,$tag TRANSFERENCIA CAJA A BANCO,QA-CB-TRF-5000,0,5000.00,116500.00
$fechaCsv,COMISION BANCARIA QA,QA-CB-COM-150,150.00,0,116350.00
$fechaCsv,INTERES GANADO QA,QA-CB-INT-8755,0,87.55,116437.55
"@

$csvPath = Join-Path $env:TEMP "$tag.csv"
[System.IO.File]::WriteAllText($csvPath, $csv, [System.Text.UTF8Encoding]::new($false))

# Crear conciliación
$conc = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion" @{
    idEmpresa = $IdEmpresa
    idUsuario = $IdUsuario
    idCuentaFinanciera = $idBanco
    periodoDesde = $hoy.ToString("yyyy-MM-dd")
    periodoHasta = $hoy.ToString("yyyy-MM-dd")
    saldoBancoFinal = $saldoFin
    saldoBancoInicial = $saldoIni
    toleranciaDiferencia = 0.01
    observacion = "Escenario coherente $tag"
}
$idConc = [int]$conc.idTesoreriaConciliacion
Write-Host "Conciliación #$idConc"

# Preview archivo (curl multipart — PS 5.1 no soporta -Form)
$previewRaw = curl.exe -s -w "`nHTTP_CODE:%{http_code}" -X POST "$ApiBase/api/TesoreriaExtracto/PreviewArchivo" `
  -F "idEmpresa=$IdEmpresa" `
  -F "idCuentaFinanciera=$idBanco" `
  -F "idUsuario=$IdUsuario" `
  -F "idTesoreriaConciliacion=$idConc" `
  -F "archivo=@$csvPath;type=text/csv"
$codeMatch = [regex]::Match([string]$previewRaw, 'HTTP_CODE:(\d+)')
$previewCode = if ($codeMatch.Success) { [int]$codeMatch.Groups[1].Value } else { 0 }
$previewJson = ([string]$previewRaw -replace '(?s)\s*HTTP_CODE:\d+\s*$', '').Trim()
if ($previewCode -ne 200) { throw "PreviewArchivo falló ($previewCode): $previewJson" }
$preview = $previewJson | ConvertFrom-Json
$idImport = [int]$preview.idTesoreriaExtractoImport
Write-Host "Preview import #$idImport · lineas=$($preview.lineas.Count) adapter=$($preview.adapterUsado)"

Assert-True ($preview.lineas.Count -eq 7) "Preview tiene 7 líneas (obtuvo $($preview.lineas.Count))"

# Confirmar preview
$confirmado = Invoke-ApiJson POST "$ApiBase/api/TesoreriaExtracto/$IdEmpresa/$idImport/Confirmar?idUsuario=$IdUsuario&toleranciaDiasMatch=3"
Write-Host "Confirmado estado=$($confirmado.estado)"

# Adjuntar a conciliación + matching
$adj = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/AdjuntarExtracto" @{
    idTesoreriaConciliacion = $idConc
    idTesoreriaExtractoImport = $idImport
    idEmpresa = $IdEmpresa
    idUsuario = $IdUsuario
    ejecutarMatching = $true
}
Write-Host "Extracto adjunto"

# Matching explícito (por si adjuntar no devolvió workspace)
$ws = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/Matching" @{
    idTesoreriaConciliacion = $idConc
    idEmpresa = $IdEmpresa
    idUsuario = $IdUsuario
    toleranciaDias = 3
}

function Get-ConteoBanco($lineas) {
    $pend = @("PENDIENTE","SUGERIDO","AMBIGUO","DUPLICADO","DIFERENCIA")
    $conc = @("CONFIRMADO","AUTO_CONCILIADO","NUEVO_MOV","RESUELTO")
    $exc = @("IGNORADO","DESCARTADO")
    return [pscustomobject]@{
        Total = $lineas.Count
        Pendientes = @($lineas | Where-Object { $pend -contains $_.estadoMatch }).Count
        Conciliadas = @($lineas | Where-Object { $conc -contains $_.estadoMatch }).Count
        Excluidas = @($lineas | Where-Object { $exc -contains $_.estadoMatch }).Count
    }
}

$conteo1 = Get-ConteoBanco $ws.lineasBanco
Write-Host ("Post-matching: total={0} pend={1} conc={2} dif={3}" -f `
    $conteo1.Total, $conteo1.Pendientes, $conteo1.Conciliadas, $ws.saldos.diferencia)

Assert-True ($conteo1.Total -eq 7) "Workspace tiene 7 líneas banco"
Assert-True ($conteo1.Conciliadas -ge 4) "Al menos 4 líneas auto/sugeridas conciliables (obtuvo $($conteo1.Conciliadas) conciliadas + revisar sugeridos)"
Assert-True ([int]$ws.estadisticas.lineasPendientes -eq $conteo1.Pendientes) "KPI lineasPendientes = filtro pendientes ($($ws.estadisticas.lineasPendientes) vs $($conteo1.Pendientes))"
Assert-True ([int]$ws.estadisticas.lineasConciliadas -eq $conteo1.Conciliadas) "KPI lineasConciliadas = filtro conciliadas"

# Confirmar sugeridos pendientes con ASOCIAR si tienen movimiento
Write-Host "`n[5] Resolver matches y diferencias..." -ForegroundColor Cyan
foreach ($linea in $ws.lineasBanco) {
    if ($linea.estadoMatch -eq "SUGERIDO" -and $linea.idMovimientoFinanciero) {
        Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/ResolverLinea" @{
            idTesoreriaConciliacion = $idConc
            idTesoreriaExtractoLinea = $linea.idTesoreriaExtractoLinea
            idEmpresa = $IdEmpresa
            idUsuario = $IdUsuario
            accion = "ASOCIAR"
            idMovimientoFinanciero = $linea.idMovimientoFinanciero
            motivo = "Confirmar sugerencia QA"
        } | Out-Null
        Write-Host "  ASOCIAR línea #$($linea.idTesoreriaExtractoLinea) → mov $($linea.idMovimientoFinanciero)"
    }
}

# Recargar y crear comisión / interés
$ws = Invoke-ApiJson GET "$ApiBase/api/TesoreriaConciliacion/$IdEmpresa/$idConc/Workspace"

$comision = $ws.lineasBanco | Where-Object { $_.referencia -eq "QA-CB-COM-150" -or ($_.descripcion -like "*COMISION*") } | Select-Object -First 1
$interes = $ws.lineasBanco | Where-Object { $_.referencia -like "QA-CB-INT-*" -or ($_.descripcion -like "*INTERES*") } | Select-Object -First 1

Assert-True ($null -ne $comision) "Existe línea comisión"
Assert-True ($null -ne $interes) "Existe línea interés"

if ($comision -and ($comision.estadoMatch -notin @("CONFIRMADO","AUTO_CONCILIADO","NUEVO_MOV","RESUELTO"))) {
    $r = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/ResolverLinea" @{
        idTesoreriaConciliacion = $idConc
        idTesoreriaExtractoLinea = $comision.idTesoreriaExtractoLinea
        idEmpresa = $IdEmpresa
        idUsuario = $IdUsuario
        accion = "CREAR_GASTO"
        categoria = "COMISION_BANCARIA"
        motivo = "Comisión bancaria desde extracto QA"
    }
    Write-Host "  CREAR_GASTO comisión → mov $($r.idMovimientoFinanciero) yaResuelta=$($r.yaResuelta)"
    Assert-True ($r.idMovimientoFinanciero -gt 0 -or $r.yaResuelta) "CREAR_GASTO creó movimiento"
}

if ($interes -and ($interes.estadoMatch -notin @("CONFIRMADO","AUTO_CONCILIADO","NUEVO_MOV","RESUELTO"))) {
    $r = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/ResolverLinea" @{
        idTesoreriaConciliacion = $idConc
        idTesoreriaExtractoLinea = $interes.idTesoreriaExtractoLinea
        idEmpresa = $IdEmpresa
        idUsuario = $IdUsuario
        accion = "CREAR_INGRESO"
        categoria = "INTERES_BANCARIO"
        motivo = "Interés ganado desde extracto QA"
    }
    Write-Host "  CREAR_INGRESO interés → mov $($r.idMovimientoFinanciero) yaResuelta=$($r.yaResuelta)"
    Assert-True ($r.idMovimientoFinanciero -gt 0 -or $r.yaResuelta) "CREAR_INGRESO creó movimiento"
}

# Asociar cualquier operativo pendiente restante con candidato
$ws = Invoke-ApiJson GET "$ApiBase/api/TesoreriaConciliacion/$IdEmpresa/$idConc/Workspace"
foreach ($linea in $ws.lineasBanco) {
    $est = $linea.estadoMatch
    if ($est -notin @("PENDIENTE","SUGERIDO","AMBIGUO","DUPLICADO","DIFERENCIA")) { continue }
    if ($linea.idMovimientoFinanciero) {
        Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/ResolverLinea" @{
            idTesoreriaConciliacion = $idConc
            idTesoreriaExtractoLinea = $linea.idTesoreriaExtractoLinea
            idEmpresa = $IdEmpresa
            idUsuario = $IdUsuario
            accion = "ASOCIAR"
            idMovimientoFinanciero = $linea.idMovimientoFinanciero
            motivo = "Asociar pendiente QA"
        } | Out-Null
        Write-Host "  ASOCIAR pendiente #$($linea.idTesoreriaExtractoLinea)"
        continue
    }

    # Buscar candidato por monto
    $cands = Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/BuscarCandidatos" @{
        idTesoreriaConciliacion = $idConc
        idTesoreriaExtractoLinea = $linea.idTesoreriaExtractoLinea
        idEmpresa = $IdEmpresa
        texto = $linea.referencia
        incluirOtrasCuentas = $false
    }
    $best = @($cands) | Select-Object -First 1
    if ($best -and $best.idMovimientoFinanciero) {
        Invoke-ApiJson POST "$ApiBase/api/TesoreriaConciliacion/ResolverLinea" @{
            idTesoreriaConciliacion = $idConc
            idTesoreriaExtractoLinea = $linea.idTesoreriaExtractoLinea
            idEmpresa = $IdEmpresa
            idUsuario = $IdUsuario
            accion = "ASOCIAR"
            idMovimientoFinanciero = $best.idMovimientoFinanciero
            motivo = "Asociar por búsqueda QA"
        } | Out-Null
        Write-Host "  ASOCIAR por búsqueda #$($linea.idTesoreriaExtractoLinea) → $($best.idMovimientoFinanciero)"
    } else {
        Write-Warning "Sin candidato para línea #$($linea.idTesoreriaExtractoLinea) $($linea.descripcion) estado=$est"
    }
}

# ---------------------------------------------------------------------------
# 6) Validación final
# ---------------------------------------------------------------------------
Write-Host "`n[6] Validación final..." -ForegroundColor Cyan
$ws = Invoke-ApiJson GET "$ApiBase/api/TesoreriaConciliacion/$IdEmpresa/$idConc/Workspace"
$conteoF = Get-ConteoBanco $ws.lineasBanco
$dif = [decimal]$ws.saldos.diferencia
$saldoBanco = [decimal]$ws.saldos.saldoBancoFinal
$saldoLibros = [decimal]$ws.saldos.saldoLibrosFinal

Write-Host ("Final: total={0} pend={1} conc={2} excl={3}" -f $conteoF.Total, $conteoF.Pendientes, $conteoF.Conciliadas, $conteoF.Excluidas)
Write-Host ("Saldos: banco={0} libros={1} dif={2}" -f $saldoBanco, $saldoLibros, $dif)
Write-Host ("KPI API: pend={0} conc={1} total={2}" -f $ws.estadisticas.lineasPendientes, $ws.estadisticas.lineasConciliadas, $ws.estadisticas.lineasBancoTotal)
Write-Host ("PuedeCerrar={0} bloqueos={1}" -f $ws.puedeCerrar, ($ws.bloqueos -join " | "))

Assert-True ($conteoF.Pendientes -eq 0) "Sin pendientes de banco (obtuvo $($conteoF.Pendientes))"
Assert-True ($conteoF.Conciliadas -eq 7) "7 líneas conciliadas (obtuvo $($conteoF.Conciliadas))"
Assert-True ([int]$ws.estadisticas.lineasPendientes -eq $conteoF.Pendientes) "KPI pendientes consistente"
Assert-True ([int]$ws.estadisticas.lineasConciliadas -eq $conteoF.Conciliadas) "KPI conciliadas consistente"
Assert-True ([math]::Abs($dif) -le 0.01) "Diferencia ≈ 0 (obtuvo $dif)"
Assert-True ([math]::Abs($saldoBanco - $saldoLibros) -le 0.01) "Saldo banco = saldo libros"
Assert-True ([math]::Abs($saldoLibros - 116437.55) -lt 0.02) "Saldo libros final 116437.55 tras CREAR_* (obtuvo $saldoLibros)"

# Auditoría
$acciones = @($ws.auditoriaReciente | ForEach-Object { $_.accion })
Assert-True ($acciones -contains "CREAR_GASTO" -or $acciones -contains "ASOCIAR") "Auditoría registra acciones de resolución"
$tieneCrearGasto = $acciones -contains "CREAR_GASTO"
$tieneCrearIngreso = $acciones -contains "CREAR_INGRESO"
Assert-True ($tieneCrearGasto) "Auditoría contiene CREAR_GASTO"
Assert-True ($tieneCrearIngreso) "Auditoría contiene CREAR_INGRESO"

# Movimientos EXTRACTO
$extCount = [int](Invoke-SqlScalar @"
SELECT COUNT(*) FROM dbo.MovimientoFinanciero
WHERE IdEmpresa=$IdEmpresa AND ReferenciaTipo='EXTRACTO'
  AND (IdCuentaOrigen=$idBanco OR IdCuentaDestino=$idBanco)
  AND ClaveIdempotencia LIKE 'EXT_${idImport}_%'
"@)
Assert-True ($extCount -ge 2) "Al menos 2 movimientos EXTRACTO creados (obtuvo $extCount)"

# Contabilidad (si hay asientos ligados a esos movimientos)
$asientos = [int](Invoke-SqlScalar @"
SELECT COUNT(*)
FROM dbo.AsientosContables a
INNER JOIN dbo.MovimientoFinanciero m ON m.IdMovimientoFinanciero = a.OrigenReferenciaId
WHERE a.IdEmpresa=$IdEmpresa
  AND m.ReferenciaTipo='EXTRACTO'
  AND (m.IdCuentaOrigen=$idBanco OR m.IdCuentaDestino=$idBanco)
  AND m.ClaveIdempotencia LIKE 'EXT_${idImport}_%'
"@)
Write-Host "Asientos contables ligados a EXTRACTO: $asientos"
if ($asientos -eq 0) {
    Write-Host "Nota: Contabilidad puede estar deshabilitada o sin mapeo para la empresa; no es fallo del flujo de tesorería." -ForegroundColor Yellow
}

Write-Host "`n=== RESUMEN ===" -ForegroundColor Cyan
Write-Host "Tag=$tag"
Write-Host "IdCuentaBanco=$idBanco IdConciliacion=$idConc IdImport=$idImport"
Write-Host "SaldoBanco=$saldoBanco SaldoLibros=$saldoLibros Diferencia=$dif"
Write-Host "Pendientes=$($conteoF.Pendientes) Conciliadas=$($conteoF.Conciliadas) ExtractoMovs=$extCount Asientos=$asientos"

if ($fails.Count -gt 0) {
    Write-Host "`nFALLOS ($($fails.Count)):" -ForegroundColor Red
    $fails | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nESCENARIO OK" -ForegroundColor Green
exit 0
