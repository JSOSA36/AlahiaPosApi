SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- AlahiaPos_Dev: idempotencia de cobro POS (ProcesarFactura)
IF COL_LENGTH('dbo.FacturaHeaders', 'ClaveIdempotenciaVenta') IS NULL
BEGIN
    ALTER TABLE dbo.FacturaHeaders ADD ClaveIdempotenciaVenta NVARCHAR(80) NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_FacturaHeaders_ClaveIdempotenciaVenta'
      AND object_id = OBJECT_ID('dbo.FacturaHeaders')
)
BEGIN
    CREATE UNIQUE INDEX UX_FacturaHeaders_ClaveIdempotenciaVenta
        ON dbo.FacturaHeaders (IdEmpresa, ClaveIdempotenciaVenta)
        WHERE ClaveIdempotenciaVenta IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'ClaveIdempotenciaVenta') IS NULL
BEGIN
    ALTER TABLE dbo.FacturaHeaders ADD ClaveIdempotenciaVenta NVARCHAR(80) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_FacturaHeaders_ClaveIdempotenciaVenta'
      AND object_id = OBJECT_ID('dbo.FacturaHeaders')
)
BEGIN
    CREATE UNIQUE INDEX UX_FacturaHeaders_ClaveIdempotenciaVenta
        ON dbo.FacturaHeaders (IdEmpresa, ClaveIdempotenciaVenta)
        WHERE ClaveIdempotenciaVenta IS NOT NULL;
END
GO
