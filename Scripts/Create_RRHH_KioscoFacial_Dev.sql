-- Kiosco facial Alahia: embeddings por empleado (sin álbum de fotos)
-- y auditoría de intentos. AlahiaPos_Dev.
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.RrhhEmpleadoRostro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhEmpleadoRostro (
        IdRostro                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa                INT NOT NULL,
        IdEmpleados              INT NOT NULL,
        Embedding                NVARCHAR(MAX) NOT NULL,
        Muestras                 INT NOT NULL CONSTRAINT DF_RrhhRostro_Muestras DEFAULT (1),
        Consentimiento           BIT NOT NULL CONSTRAINT DF_RrhhRostro_Cons DEFAULT (0),
        FechaEnrolamiento        DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhRostro_Enr DEFAULT (SYSUTCDATETIME()),
        IdUsuarioEnrolamiento    INT NOT NULL,
        Activo                   BIT NOT NULL CONSTRAINT DF_RrhhRostro_Act DEFAULT (1),
        PermitirPinExcepcion     BIT NOT NULL CONSTRAINT DF_RrhhRostro_Pin DEFAULT (0),
        PinHash                  NVARCHAR(128) NULL,
        PinSalt                  NVARCHAR(64) NULL,
        IntentosPinFallidos      INT NOT NULL CONSTRAINT DF_RrhhRostro_PinFail DEFAULT (0),
        PinBloqueadoHasta        DATETIME2(0) NULL,
        CONSTRAINT UQ_RrhhEmpleadoRostro UNIQUE (IdEmpresa, IdEmpleados)
    );
    CREATE INDEX IX_RrhhRostro_Emp ON dbo.RrhhEmpleadoRostro (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.RrhhKioscoEvento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RrhhKioscoEvento (
        IdEvento      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa     INT NOT NULL,
        IdEmpleados   INT NULL,
        Tipo          NVARCHAR(30) NOT NULL,
        Distancia     FLOAT NULL,
        IdUsuario     INT NOT NULL,
        Dispositivo   NVARCHAR(120) NULL,
        Ip            NVARCHAR(60) NULL,
        Detalle       NVARCHAR(400) NULL,
        Fecha         DATETIME2(0) NOT NULL CONSTRAINT DF_RrhhKioscoEvt_Fec DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RrhhKioscoEvt_Emp ON dbo.RrhhKioscoEvento (IdEmpresa, Fecha DESC);
END
GO
