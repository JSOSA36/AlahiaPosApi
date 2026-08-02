-- AlahiaPos_Dev only: smoke checks for RECLASIFICAR_PAGO schema.
USE AlahiaPos_Dev;
GO

SELECT
    OBJECT_ID('dbo.PagoReclasificacion') AS TablaPagoReclasificacion,
    COL_LENGTH('dbo.PagosFacturasClientes', 'IdMovimientoFinanciero') AS ColPagoMov,
    COL_LENGTH('dbo.Ingresos', 'IdMovimientoFinanciero') AS ColIngresoMov;

SELECT name
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.PagoReclasificacion')
ORDER BY name;

PRINT 'Dev_QA_PagoReclasificacion_Schema.sql OK';
GO
