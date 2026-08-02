/*
  LIMPIEZA PRODUCCIÓN — Clinica Dental Sena (IdEmpresa = 60)
  Conserva maestros: productos, categorías, clientes, empleados, usuarios,
  almacenes, cuentas, parámetros, certificados, config DGII, módulos, proveedores.
*/
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @IdEmpresa INT = 60;
DECLARE @Nombre NVARCHAR(200);

SELECT @Nombre = NombreComercial FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa;
IF @Nombre IS NULL
BEGIN
    RAISERROR('Empresa 60 no existe.', 16, 1);
    RETURN;
END
IF @Nombre NOT LIKE N'%Sena%'
BEGIN
    RAISERROR('IdEmpresa 60 no es Clinica Dental Sena. Abortado por seguridad.', 16, 1);
    RETURN;
END

PRINT CONCAT('Inicio limpieza: ', @Nombre, ' IdEmpresa=', @IdEmpresa);

BEGIN TRAN;

/* e-CF */
DELETE x FROM dbo.ECFXml x
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = x.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE d FROM dbo.ECFDetalle d
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = d.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE h FROM dbo.ECFHistorialEstados h
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = h.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.ECFEncabezado WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ECFContingencia','U') IS NOT NULL
    DELETE c FROM dbo.ECFContingencia c
    INNER JOIN dbo.ECFEncabezado e ON e.IdECF = c.IdECF
    WHERE e.IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ComprobanteFiscals','U') IS NOT NULL
    DELETE FROM dbo.ComprobanteFiscals WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ComprobantesAnulados','U') IS NOT NULL
    DELETE FROM dbo.ComprobantesAnulados WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.Justificacion608','U') IS NOT NULL
    DELETE FROM dbo.Justificacion608 WHERE IdEmpresa = @IdEmpresa;

/* Notas de crédito */
DELETE d FROM dbo.NotasCreditoDetalle d
INNER JOIN dbo.NotasCredito n ON n.IdNotaCredito = d.IdNotaCredito
WHERE n.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.NotasCredito WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.NotaCreditoes','U') IS NOT NULL
    DELETE FROM dbo.NotaCreditoes WHERE IdEmpresa = @IdEmpresa;

/* Conduces */
IF OBJECT_ID('dbo.ConduceDetalle','U') IS NOT NULL
    DELETE d FROM dbo.ConduceDetalle d
    INNER JOIN dbo.ConduceHeader h ON h.IdConduceHeader = d.IdConduceHeader
    WHERE h.IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ConduceHeader','U') IS NOT NULL
    DELETE FROM dbo.ConduceHeader WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ConduceDetalles','U') IS NOT NULL
AND OBJECT_ID('dbo.ConduceHeaders','U') IS NOT NULL
BEGIN
    DELETE d FROM dbo.ConduceDetalles d
    INNER JOIN dbo.ConduceHeaders h ON h.IdConduceHeader = d.IdConduceHeader
    WHERE h.IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.ConduceHeaders WHERE IdEmpresa = @IdEmpresa;
END

/* Pagos y facturas */
DELETE FROM dbo.PagosFacturasClientes WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.FacturaDetalles WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.FacturaHeaders WHERE IdEmpresa = @IdEmpresa;

/* Ingresos / gastos */
DELETE FROM dbo.Ingresos WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.Gastos WHERE IdEmpresa = @IdEmpresa;

/* Caja */
IF OBJECT_ID('dbo.CajaMovimiento','U') IS NOT NULL
    DELETE FROM dbo.CajaMovimiento WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.CajaCierre','U') IS NOT NULL
    DELETE FROM dbo.CajaCierre WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.CajaApertura','U') IS NOT NULL
    DELETE FROM dbo.CajaApertura WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.CierresCajas','U') IS NOT NULL
    DELETE FROM dbo.CierresCajas WHERE IdEmpresa = @IdEmpresa;

/* Tesorería / finanzas */
IF OBJECT_ID('dbo.TesoreriaConciliacionAuditoria','U') IS NOT NULL
    DELETE FROM dbo.TesoreriaConciliacionAuditoria WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.TesoreriaConciliacion','U') IS NOT NULL
    DELETE FROM dbo.TesoreriaConciliacion WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.TesoreriaExtractoImport','U') IS NOT NULL
    DELETE FROM dbo.TesoreriaExtractoImport WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.PagoReclasificacion','U') IS NOT NULL
    DELETE FROM dbo.PagoReclasificacion WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.MovimientoFinanciero WHERE IdEmpresa = @IdEmpresa;

/* Cuentas se conservan; saldo operativo a cero */
UPDATE dbo.CuentaFinanciera
SET SaldoDisponible = 0,
    UltimoSaldoConciliado = 0,
    FechaUltimaConciliacion = NULL
WHERE IdEmpresa = @IdEmpresa;

/* Inventario: solo movimientos; productos/almacenes se conservan */
IF OBJECT_ID('dbo.MovimientosInventarioDetalle','U') IS NOT NULL
    DELETE d FROM dbo.MovimientosInventarioDetalle d
    INNER JOIN dbo.MovimientosInventario m ON m.Id = d.IdMovimientoInventario
    WHERE m.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.MovimientosInventario WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.MovimientoDetalles','U') IS NOT NULL
AND OBJECT_ID('dbo.MovimientoHeaders','U') IS NOT NULL
BEGIN
    DELETE d FROM dbo.MovimientoDetalles d
    INNER JOIN dbo.MovimientoHeaders h ON h.Id = d.IdHeader
    WHERE h.IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.MovimientoHeaders WHERE IdEmpresa = @IdEmpresa;
END

IF OBJECT_ID('dbo.AjusteInventarioDetalles','U') IS NOT NULL
AND OBJECT_ID('dbo.AjusteInventarioHeaders','U') IS NOT NULL
BEGIN
    DELETE d FROM dbo.AjusteInventarioDetalles d
    INNER JOIN dbo.AjusteInventarioHeaders h ON h.Id = d.IdHeader
    WHERE h.IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.AjusteInventarioHeaders WHERE IdEmpresa = @IdEmpresa;
END

/* Compras */
IF OBJECT_ID('dbo.OrdenCompraDetalles','U') IS NOT NULL
    DELETE FROM dbo.OrdenCompraDetalles WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.OrdenCompraHeaders','U') IS NOT NULL
    DELETE FROM dbo.OrdenCompraHeaders WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.PagosProveedor','U') IS NOT NULL
    DELETE FROM dbo.PagosProveedor WHERE IdEmpresa = @IdEmpresa;

/* Contabilidad operativa */
IF OBJECT_ID('dbo.AsientosContablesDetalle','U') IS NOT NULL
AND OBJECT_ID('dbo.AsientosContables','U') IS NOT NULL
BEGIN
    DELETE d FROM dbo.AsientosContablesDetalle d
    INNER JOIN dbo.AsientosContables a ON a.IdAsientoContable = d.IdAsientoContable
    WHERE a.IdEmpresa = @IdEmpresa;
END
IF OBJECT_ID('dbo.AsientosContables','U') IS NOT NULL
    DELETE FROM dbo.AsientosContables WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.ContabilidadIntegracionLog','U') IS NOT NULL
    DELETE FROM dbo.ContabilidadIntegracionLog WHERE IdEmpresa = @IdEmpresa;

/* Otros operativos */
IF OBJECT_ID('dbo.HistoryPrints','U') IS NOT NULL
    DELETE FROM dbo.HistoryPrints WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.LavadorConsumo','U') IS NOT NULL
    DELETE FROM dbo.LavadorConsumo WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.BizcochoEncargo','U') IS NOT NULL
    DELETE FROM dbo.BizcochoEncargo WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.DocumentosClinicos','U') IS NOT NULL
    DELETE FROM dbo.DocumentosClinicos WHERE IdEmpresa = @IdEmpresa;
IF OBJECT_ID('dbo.EventosOutbox','U') IS NOT NULL
    DELETE FROM dbo.EventosOutbox WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID('dbo.ProduccionTrabajo','U') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.ProduccionHistorial','U') IS NOT NULL
        DELETE h FROM dbo.ProduccionHistorial h
        INNER JOIN dbo.ProduccionTrabajo t ON t.IdProduccionTrabajo = h.IdProduccionTrabajo
        WHERE t.IdEmpresa = @IdEmpresa;
    IF OBJECT_ID('dbo.ProduccionTrabajoItem','U') IS NOT NULL
        DELETE i FROM dbo.ProduccionTrabajoItem i
        INNER JOIN dbo.ProduccionTrabajo t ON t.IdProduccionTrabajo = i.IdProduccionTrabajo
        WHERE t.IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.ProduccionTrabajo WHERE IdEmpresa = @IdEmpresa;
END

/* Devoluciones (si existen y tienen IdEmpresa) */
IF OBJECT_ID('dbo.DevolucionesClientesHeaders','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.DevolucionesClienteDetalles','IdEmpresa') IS NOT NULL
        DELETE FROM dbo.DevolucionesClienteDetalles WHERE IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.DevolucionesClientesHeaders WHERE IdEmpresa = @IdEmpresa;
END
IF OBJECT_ID('dbo.DevelocionesHeaders','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.DevolucionesDetalles','IdEmpresa') IS NOT NULL
        DELETE FROM dbo.DevolucionesDetalles WHERE IdEmpresa = @IdEmpresa;
    DELETE FROM dbo.DevelocionesHeaders WHERE IdEmpresa = @IdEmpresa;
END

/* Reiniciar numeración interna de documentos */
UPDATE dbo.SecuenciaDocumentos
SET SecuenciaActual = ISNULL(SecuenciaInicial, 0)
WHERE IdEmpresa = @IdEmpresa;

COMMIT TRAN;

PRINT 'OK — limpieza completada.';

SELECT 'FacturaHeaders' AS Concepto, COUNT(*) AS Cantidad FROM dbo.FacturaHeaders WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'Ingresos', COUNT(*) FROM dbo.Ingresos WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'Gastos', COUNT(*) FROM dbo.Gastos WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'NotasCredito', COUNT(*) FROM dbo.NotasCredito WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'ECFEncabezado', COUNT(*) FROM dbo.ECFEncabezado WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'MovimientoFinanciero', COUNT(*) FROM dbo.MovimientoFinanciero WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'MovimientosInventario', COUNT(*) FROM dbo.MovimientosInventario WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'Productos (conservados)', COUNT(*) FROM dbo.Productos WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'Clientes (conservados)', COUNT(*) FROM dbo.Clientes WHERE IdEmpresa=@IdEmpresa
UNION ALL SELECT 'Categorias (conservadas)', COUNT(*) FROM dbo.Categorias WHERE IdEmpresa=@IdEmpresa;
