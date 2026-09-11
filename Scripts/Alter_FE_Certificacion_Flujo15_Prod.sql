-- Flujo 15 pasos CerteCF — AlahiaPos_Prod
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.CertecfSesion', 'PasoActual') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD PasoActual INT NOT NULL CONSTRAINT DF_CertecfSesion_Paso DEFAULT (1);
IF COL_LENGTH('dbo.CertecfSesion', 'NombreSoftware') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD NombreSoftware NVARCHAR(80) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'VersionSoftware') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD VersionSoftware NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'TipoSoftware') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD TipoSoftware NVARCHAR(40) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlRecepcion') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlRecepcion NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlAprobacion') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlAprobacion NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlAutenticacion') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlAutenticacion NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlRecepcionProd') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlRecepcionProd NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlAprobacionProd') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlAprobacionProd NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'UrlAutenticacionProd') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD UrlAutenticacionProd NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CertecfSesion', 'JsonPasos') IS NULL
    ALTER TABLE dbo.CertecfSesion ADD JsonPasos NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.CertecfCaso', 'TipoPrueba') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD TipoPrueba NVARCHAR(20) NOT NULL CONSTRAINT DF_CertecfCaso_TipoPrueba DEFAULT (N'DATOS');
GO

IF OBJECT_ID(N'dbo.CertecfInboundLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CertecfInboundLog (
        IdLog            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CertecfInboundLog PRIMARY KEY,
        IdEmpresa        INT NULL,
        Rnc              NVARCHAR(11) NOT NULL,
        Tipo             NVARCHAR(20) NOT NULL,
        Encf             NVARCHAR(20) NULL,
        Estado           NVARCHAR(30) NULL,
        Mensaje          NVARCHAR(1000) NULL,
        Fecha            DATETIME NOT NULL CONSTRAINT DF_CertecfInbound_Fecha DEFAULT (GETDATE())
    );
    CREATE INDEX IX_CertecfInbound_Rnc ON dbo.CertecfInboundLog (Rnc, Fecha DESC);
END
GO

