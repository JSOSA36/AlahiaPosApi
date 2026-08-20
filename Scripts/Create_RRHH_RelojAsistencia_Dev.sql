-- Relojes de asistencia: el dispositivo marca, Alahia solo ingesta.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.RrhhPonchada', N'ClaveExterna') IS NULL
    ALTER TABLE dbo.RrhhPonchada ADD ClaveExterna NVARCHAR(80) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_RrhhPonchada_Clave' AND object_id = OBJECT_ID(N'dbo.RrhhPonchada')
)
    CREATE UNIQUE INDEX UX_RrhhPonchada_Clave
        ON dbo.RrhhPonchada (IdEmpresa, ClaveExterna)
        WHERE ClaveExterna IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorDispositivo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorDispositivo (
        IdDispositivo        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        Serial               NVARCHAR(80)  NOT NULL,
        Nombre               NVARCHAR(120) NOT NULL,
        Proveedor            NVARCHAR(30)  NOT NULL CONSTRAINT DF_RrhhReloj_Prov DEFAULT (N'ZKTECO'),
        Token                NVARCHAR(80)  NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_RrhhReloj_Act DEFAULT (1),
        UltimaComunicacion   DATETIME2(0) NULL,
        FechaCreacion        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhReloj_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhReloj_Serial UNIQUE (Serial)
    );
    CREATE INDEX IX_RrhhReloj_Emp ON dbo.RrhhPonchadorDispositivo (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorPersona', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorPersona (
        IdPersonaDispositivo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdEmpleados          INT NOT NULL,
        CodigoDispositivo    NVARCHAR(40) NOT NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_RrhhRelojPer_Act DEFAULT (1),
        FechaCreacion        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRelojPer_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RrhhRelojPer UNIQUE (IdEmpresa, CodigoDispositivo)
    );
    CREATE INDEX IX_RrhhRelojPer_Emp ON dbo.RrhhPonchadorPersona (IdEmpresa, IdEmpleados);
END
GO

IF OBJECT_ID(N'dbo.RrhhPonchadorIngesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhPonchadorIngesta (
        IdIngesta        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa        INT NULL,
        IdDispositivo    INT NULL,
        Serial           NVARCHAR(80) NOT NULL,
        CodigoDispositivo NVARCHAR(40) NULL,
        IdEmpleados      INT NULL,
        FechaHoraReloj   DATETIME2(0) NULL,
        Estado           NVARCHAR(30) NOT NULL,
        ClaveExterna     NVARCHAR(80) NULL,
        IdPonchada       INT NULL,
        Detalle          NVARCHAR(400) NULL,
        Fecha            DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRelojIng_Fec DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhRelojIng_Emp ON dbo.RrhhPonchadorIngesta (IdEmpresa, Fecha DESC);
END
GO
