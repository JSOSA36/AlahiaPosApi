-- =============================================
-- Notas de crédito (devoluciones parciales)
-- =============================================

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'NotasCredito'
)
BEGIN
    CREATE TABLE NotasCredito (
        IdNotaCredito INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdFacturaHeader INT NOT NULL,
        IdEmpresa INT NOT NULL,
        NumeroDocumento NVARCHAR(50) NULL,
        NCF NVARCHAR(30) NULL,
        NCFModificado NVARCHAR(30) NULL,
        IdCliente INT NULL,
        NombreCliente NVARCHAR(200) NULL,
        RNC NVARCHAR(30) NULL,
        SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalItbis DECIMAL(18,2) NOT NULL DEFAULT 0,
        Total DECIMAL(18,2) NOT NULL DEFAULT 0,
        Observacion NVARCHAR(500) NULL,
        IdUsuario INT NULL,
        FechaInseccion DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'NotasCreditoDetalle'
)
BEGIN
    CREATE TABLE NotasCreditoDetalle (
        IdNotaCreditoDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdNotaCredito INT NOT NULL,
        IdFacturaDetalle INT NOT NULL,
        IdProducto INT NOT NULL,
        NombreProducto NVARCHAR(200) NULL,
        Cantidad DECIMAL(18,2) NOT NULL DEFAULT 0,
        PrecioUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        Itbis DECIMAL(18,2) NOT NULL DEFAULT 0,
        SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('FacturaDetalles')
      AND name = 'CantidadDevuelta'
)
BEGIN
    ALTER TABLE FacturaDetalles
    ADD CantidadDevuelta DECIMAL(18,2) NOT NULL DEFAULT 0;
END
GO
