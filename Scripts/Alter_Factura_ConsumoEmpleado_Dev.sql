-- Consumo de colaborador en factura (descuento POS + CxC / nómina).
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'IdEmpleadoConsumo') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IdEmpleadoConsumo INT NULL;
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'PorcentajeDescuentoEmpleado') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD PorcentajeDescuentoEmpleado DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'CargarConsumoNomina') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD CargarConsumoNomina BIT NOT NULL
        CONSTRAINT DF_FactHdr_CargarNom DEFAULT (0);
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'IdNominaDescuento') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IdNominaDescuento INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FacturaHeaders_EmpleadoConsumo'
      AND object_id = OBJECT_ID(N'dbo.FacturaHeaders')
)
    CREATE INDEX IX_FacturaHeaders_EmpleadoConsumo
        ON dbo.FacturaHeaders (IdEmpresa, IdEmpleadoConsumo, CargarConsumoNomina)
        WHERE IdEmpleadoConsumo IS NOT NULL;
GO
