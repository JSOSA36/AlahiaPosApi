-- AlahiaPos_Dev only: reclasificación de pagos desde conciliación bancaria.
-- NO ejecutar en Prod. Idempotente.
USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- Vínculos explícitos pago ↔ movimiento ---------- */
IF COL_LENGTH('dbo.PagosFacturasClientes', 'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.PagosFacturasClientes ADD IdMovimientoFinanciero INT NULL;
GO

IF COL_LENGTH('dbo.Ingresos', 'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.Ingresos ADD IdMovimientoFinanciero INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PagosFacturasClientes_IdMovimientoFinanciero'
      AND object_id = OBJECT_ID('dbo.PagosFacturasClientes')
)
    CREATE INDEX IX_PagosFacturasClientes_IdMovimientoFinanciero
        ON dbo.PagosFacturasClientes (IdMovimientoFinanciero)
        WHERE IdMovimientoFinanciero IS NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Ingresos_IdMovimientoFinanciero'
      AND object_id = OBJECT_ID('dbo.Ingresos')
)
    CREATE INDEX IX_Ingresos_IdMovimientoFinanciero
        ON dbo.Ingresos (IdMovimientoFinanciero)
        WHERE IdMovimientoFinanciero IS NOT NULL;
GO

/* ---------- Tabla de auditoría de reclasificación ---------- */
IF OBJECT_ID('dbo.PagoReclasificacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoReclasificacion
    (
        IdPagoReclasificacion           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoReclasificacion PRIMARY KEY,
        IdEmpresa                       INT NOT NULL,
        IdTesoreriaConciliacion         INT NULL,
        IdTesoreriaExtractoLinea        INT NOT NULL,
        DocumentoTipo                   NVARCHAR(40) NOT NULL CONSTRAINT DF_PagoReclas_DocTipo DEFAULT ('FACTURA'),
        IdDocumento                     INT NULL,
        IdFacturaHeader                 INT NULL,
        IdPagoFacturaCliente            INT NULL,
        IdIngreso                       INT NULL,
        IdMovimientoOriginal            INT NOT NULL,
        IdMovimientoReclasificacion     INT NULL,
        IdCuentaOrigen                  INT NOT NULL,
        IdCuentaDestino                 INT NOT NULL,
        MetodoPagoOriginal              NVARCHAR(50) NOT NULL CONSTRAINT DF_PagoReclas_MetodoOrig DEFAULT (''),
        MetodoPagoEfectivo              NVARCHAR(50) NOT NULL CONSTRAINT DF_PagoReclas_MetodoEfect DEFAULT (''),
        Monto                           DECIMAL(18,2) NOT NULL,
        FechaMovimientoOriginal         DATETIME2 NOT NULL,
        FechaEfectiva                   DATETIME2 NOT NULL,
        FechaContable                   DATETIME2 NOT NULL,
        Tratamiento                     NVARCHAR(20) NOT NULL CONSTRAINT DF_PagoReclas_Trat DEFAULT ('POST_CIERRE'),
        IdCajaAperturaSnapshot          INT NULL,
        IdCajaCierreSnapshot            INT NULL,
        CajaEstabaCerrada               BIT NOT NULL CONSTRAINT DF_PagoReclas_CajaCerrada DEFAULT (0),
        IdPeriodoContableSnapshot       INT NULL,
        PeriodoOriginalCerrado          BIT NOT NULL CONSTRAINT DF_PagoReclas_PeriodoCerrado DEFAULT (0),
        IdUsuario                       INT NOT NULL,
        Motivo                          NVARCHAR(500) NOT NULL,
        Estado                          NVARCHAR(30) NOT NULL CONSTRAINT DF_PagoReclas_Estado DEFAULT ('APLICADA'),
        IdPagoReclasificacionReversaDe  INT NULL,
        IdAsientoContable               INT NULL,
        ClaveIdempotencia               NVARCHAR(120) NULL,
        FechaCreacion                   DATETIME2 NOT NULL CONSTRAINT DF_PagoReclas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaReversion                  DATETIME2 NULL,
        RowVersion                      ROWVERSION NOT NULL,
        CONSTRAINT CK_PagoReclas_Estado CHECK (Estado IN ('APLICADA', 'REVERSADA', 'PENDIENTE_CONTABLE')),
        CONSTRAINT CK_PagoReclas_Tratamiento CHECK (Tratamiento IN ('ANTES_CIERRE', 'POST_CIERRE')),
        CONSTRAINT CK_PagoReclas_Monto CHECK (Monto > 0)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_PagoReclas_LineaActiva'
      AND object_id = OBJECT_ID('dbo.PagoReclasificacion')
)
    CREATE UNIQUE INDEX UX_PagoReclas_LineaActiva
        ON dbo.PagoReclasificacion (IdEmpresa, IdTesoreriaExtractoLinea)
        WHERE Estado IN ('APLICADA', 'PENDIENTE_CONTABLE')
          AND IdPagoReclasificacionReversaDe IS NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_PagoReclas_ClaveActiva'
      AND object_id = OBJECT_ID('dbo.PagoReclasificacion')
)
    CREATE UNIQUE INDEX UX_PagoReclas_ClaveActiva
        ON dbo.PagoReclasificacion (IdEmpresa, ClaveIdempotencia)
        WHERE ClaveIdempotencia IS NOT NULL
          AND Estado IN ('APLICADA', 'PENDIENTE_CONTABLE');
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PagoReclas_MovOriginal'
      AND object_id = OBJECT_ID('dbo.PagoReclasificacion')
)
    CREATE INDEX IX_PagoReclas_MovOriginal
        ON dbo.PagoReclasificacion (IdEmpresa, IdMovimientoOriginal, Estado);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PagoReclas_Factura'
      AND object_id = OBJECT_ID('dbo.PagoReclasificacion')
)
    CREATE INDEX IX_PagoReclas_Factura
        ON dbo.PagoReclasificacion (IdEmpresa, IdFacturaHeader)
        WHERE IdFacturaHeader IS NOT NULL;
GO

/* ---------- Backfill conservador PagosFacturasClientes → MovimientoFinanciero ----------
   Solo cuando hay exactamente un movimiento FACTURA/VENTA|COBRO_CXC inequívoco. */
;WITH candidatos AS (
    SELECT
        p.Id AS IdPago,
        m.IdMovimientoFinanciero,
        COUNT(*) OVER (PARTITION BY p.Id) AS Cnt
    FROM dbo.PagosFacturasClientes p
    INNER JOIN dbo.MovimientoFinanciero m
        ON m.ReferenciaTipo = N'FACTURA'
       AND m.ReferenciaId = p.IdFacturaHeader
       AND m.Monto = p.Monto
       AND m.Estado = N'CONFIRMADO'
       AND (m.Categoria IN (N'VENTA', N'COBRO_CXC') OR m.Categoria IS NULL)
    WHERE p.IdMovimientoFinanciero IS NULL
)
UPDATE p
SET p.IdMovimientoFinanciero = c.IdMovimientoFinanciero
FROM dbo.PagosFacturasClientes p
INNER JOIN candidatos c ON c.IdPago = p.Id
WHERE c.Cnt = 1;
GO

;WITH candidatosIng AS (
    SELECT
        i.IdIngreso,
        m.IdMovimientoFinanciero,
        COUNT(*) OVER (PARTITION BY i.IdIngreso) AS Cnt
    FROM dbo.Ingresos i
    INNER JOIN dbo.MovimientoFinanciero m
        ON m.ReferenciaTipo = N'FACTURA'
       AND m.ReferenciaId = i.IdFacturaHeader
       AND m.Monto = i.Monto
       AND m.Estado = N'CONFIRMADO'
       AND i.IdFacturaHeader IS NOT NULL
    WHERE i.IdMovimientoFinanciero IS NULL
)
UPDATE i
SET i.IdMovimientoFinanciero = c.IdMovimientoFinanciero
FROM dbo.Ingresos i
INNER JOIN candidatosIng c ON c.IdIngreso = i.IdIngreso
WHERE c.Cnt = 1;
GO

PRINT 'Dev_Alter_PagoReclasificacion.sql OK';
GO
