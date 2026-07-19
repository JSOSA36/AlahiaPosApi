-- Sprint B: desacoplamiento fiscal DGII (solo AlahiaPos_Dev)
SET NOCOUNT ON;

PRINT '=== Sprint B Desacoplamiento Fiscal START ===';
PRINT 'DB: ' + DB_NAME();

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'ABORT: este script solo puede ejecutarse en AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.DgiiConfiguracionEmpresa', N'U') IS NULL
BEGIN
    RAISERROR(N'DgiiConfiguracionEmpresa no existe. Ejecutar Sprint A primero.', 16, 1);
END
GO

IF COL_LENGTH('dbo.DgiiConfiguracionEmpresa', 'FiscalActivo') IS NULL
    ALTER TABLE dbo.DgiiConfiguracionEmpresa ADD FiscalActivo BIT NOT NULL
        CONSTRAINT DF_DgiiCfg_FiscalActivo DEFAULT (0);
GO
IF COL_LENGTH('dbo.DgiiConfiguracionEmpresa', 'Generar606') IS NULL
    ALTER TABLE dbo.DgiiConfiguracionEmpresa ADD Generar606 BIT NOT NULL
        CONSTRAINT DF_DgiiCfg_Generar606 DEFAULT (0);
GO
IF COL_LENGTH('dbo.DgiiConfiguracionEmpresa', 'Generar607') IS NULL
    ALTER TABLE dbo.DgiiConfiguracionEmpresa ADD Generar607 BIT NOT NULL
        CONSTRAINT DF_DgiiCfg_Generar607 DEFAULT (0);
GO
IF COL_LENGTH('dbo.DgiiConfiguracionEmpresa', 'GenerarIt1') IS NULL
    ALTER TABLE dbo.DgiiConfiguracionEmpresa ADD GenerarIt1 BIT NOT NULL
        CONSTRAINT DF_DgiiCfg_GenerarIt1 DEFAULT (0);
GO
IF COL_LENGTH('dbo.DgiiConfiguracionEmpresa', 'FacturacionElectronicaActiva') IS NULL
    ALTER TABLE dbo.DgiiConfiguracionEmpresa ADD FacturacionElectronicaActiva BIT NOT NULL
        CONSTRAINT DF_DgiiCfg_FE DEFAULT (0);
GO

UPDATE dbo.DgiiConfiguracionEmpresa
SET FiscalActivo = 0,
    Generar606 = 0,
    Generar607 = 0,
    GenerarIt1 = 0,
    FacturacionElectronicaActiva = 0;

PRINT 'Flags DgiiConfiguracionEmpresa OK (todos en 0)';
GO

IF COL_LENGTH('dbo.FacturaHeaders', 'EstadoFiscalDocumento') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD EstadoFiscalDocumento NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.NotasCredito', 'EstadoFiscalDocumento') IS NULL
    ALTER TABLE dbo.NotasCredito ADD EstadoFiscalDocumento NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'EstadoFiscalDocumento') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD EstadoFiscalDocumento NVARCHAR(30) NULL;
GO

UPDATE dbo.FacturaHeaders
SET EstadoFiscalDocumento = N'NO_APLICA'
WHERE EstadoFiscalDocumento IS NULL;

UPDATE dbo.NotasCredito
SET EstadoFiscalDocumento = N'NO_APLICA'
WHERE EstadoFiscalDocumento IS NULL;

UPDATE dbo.OrdenCompraHeaders
SET EstadoFiscalDocumento = N'NO_APLICA'
WHERE EstadoFiscalDocumento IS NULL;

PRINT 'EstadoFiscalDocumento backfill NO_APLICA OK';
GO

SELECT
    COUNT(*) AS EmpresasConfig,
    SUM(CASE WHEN FiscalActivo = 1 THEN 1 ELSE 0 END) AS FiscalActivoOn,
    SUM(CASE WHEN GenerarIt1 = 1 THEN 1 ELSE 0 END) AS GenerarIt1On
FROM dbo.DgiiConfiguracionEmpresa;

SELECT 'FacturaHeaders_NO_APLICA' AS K, COUNT(*) AS C FROM dbo.FacturaHeaders WHERE EstadoFiscalDocumento = N'NO_APLICA'
UNION ALL
SELECT 'NotasCredito_NO_APLICA', COUNT(*) FROM dbo.NotasCredito WHERE EstadoFiscalDocumento = N'NO_APLICA'
UNION ALL
SELECT 'OrdenCompra_NO_APLICA', COUNT(*) FROM dbo.OrdenCompraHeaders WHERE EstadoFiscalDocumento = N'NO_APLICA';

PRINT '=== Sprint B Desacoplamiento Fiscal DONE ===';
GO
