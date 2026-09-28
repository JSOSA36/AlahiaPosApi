-- Limpieza operativa PRODUCCIÓN — Terraza prolongación 27 (MATBERT SRL), IdEmpresa = 55.
-- Conserva maestros: productos, categorías, clientes, empleados, usuarios (María),
-- almacenes, existencias, cuentas, parámetros, certificados, config DGII, módulos,
-- horarios, comisiones, secuencias e-CF oficiales (se reinician al primer número).
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @Nombre NVARCHAR(200);

SELECT @Nombre = NombreComercial FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa;
IF @Nombre IS NULL OR (@Nombre NOT LIKE N'%Terraza%' AND @Nombre NOT LIKE N'%MATBERT%')
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza / MATBERT. Abortado.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

DELETE x FROM dbo.ECFXml x
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = x.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE d FROM dbo.ECFDetalle d
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = d.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE h FROM dbo.ECFHistorialEstados h
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = h.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE c FROM dbo.ECFContingencia c
INNER JOIN dbo.ECFEncabezado e ON e.IdECF = c.IdECF
WHERE e.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.ECFEncabezado WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.Citas WHERE IdEmpresa = @IdEmpresa;

DELETE a FROM dbo.NotasCreditoAplicaciones a
INNER JOIN dbo.NotasCredito n ON n.IdNotaCredito = a.IdNotaCredito
WHERE n.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.ClienteSaldoAFavor WHERE IdEmpresa = @IdEmpresa;

DELETE d FROM dbo.NotasCreditoDetalle d
INNER JOIN dbo.NotasCredito n ON n.IdNotaCredito = d.IdNotaCredito
WHERE n.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.NotasCredito WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.PagoReclasificacion WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.PagosFacturasClientes WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.Ingresos WHERE IdEmpresa = @IdEmpresa;

DELETE d
FROM dbo.FacturaDetalles d
INNER JOIN dbo.FacturaHeaders h ON h.IdFacturaHeader = d.IdFacturaHeader
WHERE h.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.FacturaHeaders WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.Gastos WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.CajaMovimiento WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.CajaCierre WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.CajaApertura WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.MovimientoFinanciero WHERE IdEmpresa = @IdEmpresa;

UPDATE dbo.CuentaFinanciera
SET SaldoDisponible = 0,
    UltimoSaldoConciliado = 0,
    FechaUltimaConciliacion = NULL
WHERE IdEmpresa = @IdEmpresa;

DELETE d FROM dbo.MovimientosInventarioDetalle d
INNER JOIN dbo.MovimientosInventario m ON m.Id = d.IdMovimientoInventario
WHERE m.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.MovimientosInventario WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.OrdenCompraDetalles WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.OrdenCompraHeaders WHERE IdEmpresa = @IdEmpresa;
DELETE FROM dbo.PagosProveedor WHERE IdEmpresa = @IdEmpresa;

DELETE h FROM dbo.ProduccionHistorial h
INNER JOIN dbo.ProduccionTrabajo t ON t.IdTrabajo = h.IdTrabajo
WHERE t.IdEmpresa = @IdEmpresa;

DELETE i FROM dbo.ProduccionTrabajoItem i
INNER JOIN dbo.ProduccionTrabajo t ON t.IdTrabajo = i.IdTrabajo
WHERE t.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.ProduccionTrabajo WHERE IdEmpresa = @IdEmpresa;

DELETE FROM dbo.EventosOutbox WHERE IdEmpresa = @IdEmpresa;

DELETE l FROM dbo.NotificacionCanalLog l
INNER JOIN dbo.Notificaciones n ON n.IdNotificacion = l.IdNotificacion
WHERE n.IdEmpresa = @IdEmpresa;

DELETE FROM dbo.Notificaciones WHERE IdEmpresa = @IdEmpresa;

UPDATE dbo.SecuenciaDocumentos
SET SecuenciaActual = ISNULL(SecuenciaInicial, 1)
WHERE IdEmpresa = @IdEmpresa;

UPDATE dbo.SecuenciasECF
SET SecuenciaActual = SecuenciaInicial
WHERE IdEmpresa = @IdEmpresa
  AND Activo = 1
  AND TipoEcfDgii IN (31, 32, 33, 34);

COMMIT TRAN;

SELECT 'FacturaHeaders' AS Concepto, COUNT(*) AS Cantidad FROM dbo.FacturaHeaders WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Ingresos', COUNT(*) FROM dbo.Ingresos WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Gastos', COUNT(*) FROM dbo.Gastos WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'NotasCredito', COUNT(*) FROM dbo.NotasCredito WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'ECFEncabezado', COUNT(*) FROM dbo.ECFEncabezado WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Citas', COUNT(*) FROM dbo.Citas WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'CajaApertura', COUNT(*) FROM dbo.CajaApertura WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'MovimientoFinanciero', COUNT(*) FROM dbo.MovimientoFinanciero WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'MovimientosInventario', COUNT(*) FROM dbo.MovimientosInventario WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Productos (conservados)', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Clientes (conservados)', COUNT(*) FROM dbo.Clientes WHERE IdEmpresa = @IdEmpresa
UNION ALL SELECT 'Usuarios (conservados)', COUNT(*) FROM dbo.Usuarios WHERE IdEmpresa = @IdEmpresa;

SELECT Serie, SecuenciaActual, SecuenciaFinal, Ambiente
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa AND Activo = 1
ORDER BY TipoEcfDgii;

SELECT Nombre, SaldoDisponible
FROM dbo.CuentaFinanciera
WHERE IdEmpresa = @IdEmpresa;
GO
