-- =============================================
-- NC e-CF 34: trazabilidad, aplicaciones, saldo a favor
-- Target: AlahiaPos_Dev only
-- =============================================
USE AlahiaPos_Dev;
GO

SET NOCOUNT ON;

-- ---------- NotasCredito: columnas nuevas ----------
IF COL_LENGTH('dbo.NotasCredito', 'TipoDocumentoOrigen') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TipoDocumentoOrigen NVARCHAR(10) NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'IdEcf') IS NULL
    ALTER TABLE dbo.NotasCredito ADD IdEcf INT NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'TrackId') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TrackId NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'EstadoDgii') IS NULL
    ALTER TABLE dbo.NotasCredito ADD EstadoDgii NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'FechaEmisionEcf') IS NULL
    ALTER TABLE dbo.NotasCredito ADD FechaEmisionEcf DATETIME NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'MensajeEmision') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MensajeEmision NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.NotasCredito', 'MontoOriginal') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoOriginal DECIMAL(18,2) NOT NULL CONSTRAINT DF_NotasCredito_MontoOriginal DEFAULT 0;
GO

IF COL_LENGTH('dbo.NotasCredito', 'SaldoDisponible') IS NULL
    ALTER TABLE dbo.NotasCredito ADD SaldoDisponible DECIMAL(18,2) NOT NULL CONSTRAINT DF_NotasCredito_SaldoDisponible DEFAULT 0;
GO

IF COL_LENGTH('dbo.NotasCredito', 'Estado') IS NULL
    ALTER TABLE dbo.NotasCredito ADD Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_NotasCredito_Estado DEFAULT N'Activa';
GO

-- Backfill montos para NC existentes
UPDATE dbo.NotasCredito
SET MontoOriginal = Total,
    SaldoDisponible = CASE WHEN ISNULL(SaldoDisponible, 0) = 0 THEN 0 ELSE SaldoDisponible END,
    Estado = ISNULL(NULLIF(Estado, N''), N'Activa')
WHERE MontoOriginal = 0 AND Total <> 0;
GO

-- ---------- NotasCreditoAplicaciones ----------
IF OBJECT_ID('dbo.NotasCreditoAplicaciones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotasCreditoAplicaciones (
        IdAplicacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotasCreditoAplicaciones PRIMARY KEY,
        IdNotaCredito INT NOT NULL,
        IdFacturaHeader INT NULL,
        IdSaldoAFavor INT NULL,
        IdEmpresa INT NOT NULL,
        MontoAplicado DECIMAL(18,2) NOT NULL,
        TipoAplicacion NVARCHAR(30) NOT NULL,
        FechaAplicacion DATETIME NOT NULL CONSTRAINT DF_NcAplicaciones_Fecha DEFAULT GETDATE(),
        IdUsuario INT NULL,
        CONSTRAINT FK_NcAplicaciones_NotaCredito FOREIGN KEY (IdNotaCredito)
            REFERENCES dbo.NotasCredito(IdNotaCredito)
    );

    CREATE INDEX IX_NcAplicaciones_Nota ON dbo.NotasCreditoAplicaciones(IdNotaCredito);
    CREATE INDEX IX_NcAplicaciones_Factura ON dbo.NotasCreditoAplicaciones(IdFacturaHeader);
    CREATE INDEX IX_NcAplicaciones_Empresa ON dbo.NotasCreditoAplicaciones(IdEmpresa);
END
GO

-- ---------- ClienteSaldoAFavor ----------
IF OBJECT_ID('dbo.ClienteSaldoAFavor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteSaldoAFavor (
        IdSaldoAFavor INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClienteSaldoAFavor PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        IdCliente INT NOT NULL,
        IdNotaCredito INT NOT NULL,
        MontoOriginal DECIMAL(18,2) NOT NULL,
        SaldoDisponible DECIMAL(18,2) NOT NULL,
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_ClienteSaldoAFavor_Estado DEFAULT N'Disponible',
        Fecha DATETIME NOT NULL CONSTRAINT DF_ClienteSaldoAFavor_Fecha DEFAULT GETDATE(),
        IdUsuario INT NULL,
        Observacion NVARCHAR(500) NULL,
        CONSTRAINT FK_ClienteSaldoAFavor_NotaCredito FOREIGN KEY (IdNotaCredito)
            REFERENCES dbo.NotasCredito(IdNotaCredito)
    );

    CREATE INDEX IX_ClienteSaldoAFavor_Cliente ON dbo.ClienteSaldoAFavor(IdEmpresa, IdCliente);
    CREATE INDEX IX_ClienteSaldoAFavor_Nota ON dbo.ClienteSaldoAFavor(IdNotaCredito);
END
GO

PRINT 'NC_E34_Aplicaciones_SaldoAFavor_Dev: OK';
GO
