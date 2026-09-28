-- ============================================================
-- Alinear índices únicos que ya existen en Dev y faltan en Prod
-- (cotizador, reclasificación de pagos, extracto tesorería).
-- ============================================================
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Cotizacion', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Cotizacion_Folio'
      AND object_id = OBJECT_ID(N'dbo.Cotizacion')
)
BEGIN
    CREATE UNIQUE INDEX UX_Cotizacion_Folio ON dbo.Cotizacion (Folio);
END
GO

IF OBJECT_ID(N'dbo.PagoReclasificacion', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_PagoReclas_LineaActiva'
          AND object_id = OBJECT_ID(N'dbo.PagoReclasificacion')
    )
        CREATE UNIQUE INDEX UX_PagoReclas_LineaActiva
            ON dbo.PagoReclasificacion (IdEmpresa, IdTesoreriaExtractoLinea)
            WHERE Estado IN ('APLICADA', 'PENDIENTE_CONTABLE')
              AND IdPagoReclasificacionReversaDe IS NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_PagoReclas_ClaveActiva'
          AND object_id = OBJECT_ID(N'dbo.PagoReclasificacion')
    )
        CREATE UNIQUE INDEX UX_PagoReclas_ClaveActiva
            ON dbo.PagoReclasificacion (IdEmpresa, ClaveIdempotencia)
            WHERE ClaveIdempotencia IS NOT NULL
              AND Estado IN ('APLICADA', 'PENDIENTE_CONTABLE');
END
GO

IF OBJECT_ID(N'dbo.TesoreriaExtractoImport', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_TesExtImp_EmpCuentaHash_Definitivo'
          AND object_id = OBJECT_ID(N'dbo.TesoreriaExtractoImport')
    )
        CREATE UNIQUE INDEX UX_TesExtImp_EmpCuentaHash_Definitivo
            ON dbo.TesoreriaExtractoImport (IdEmpresa, IdCuentaFinanciera, HashArchivo)
            WHERE HashArchivo IS NOT NULL
              AND Estado IN ('CARGADO', 'PROCESADO', 'CERRADO');

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_TesExtImp_EmpCuentaHash'
          AND object_id = OBJECT_ID(N'dbo.TesoreriaExtractoImport')
    )
        DROP INDEX UX_TesExtImp_EmpCuentaHash ON dbo.TesoreriaExtractoImport;
END
GO

PRINT 'Indices unicos alineados en AlahiaPos_Prod.';
GO
