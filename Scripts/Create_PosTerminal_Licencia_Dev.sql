-- ============================================================
-- Licencia POS por PC: cupo en Empresas + registro PosTerminal
-- Base: AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.Empresas', N'LimiteTerminalesPos') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas
        ADD LimiteTerminalesPos INT NOT NULL
            CONSTRAINT DF_Empresas_LimiteTerminalesPos DEFAULT (1);
END
GO

IF OBJECT_ID(N'dbo.PosTerminal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PosTerminal
    (
        IdPosTerminal          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PosTerminal PRIMARY KEY,
        IdEmpresa              INT NOT NULL,
        Huella                 NVARCHAR(64) NOT NULL,
        Nombre                 NVARCHAR(80) NULL,
        Plataforma             NVARCHAR(30) NULL,
        Modelo                 NVARCHAR(80) NULL,
        Fabricante             NVARCHAR(80) NULL,
        Estado                 NVARCHAR(20) NOT NULL CONSTRAINT DF_PosTerminal_Estado DEFAULT (N'ACTIVO'),
        FechaActivacion        DATETIME NOT NULL CONSTRAINT DF_PosTerminal_FechaActivacion DEFAULT (GETDATE()),
        IdUsuarioActivacion    INT NULL,
        FechaUltimoAcceso      DATETIME NOT NULL CONSTRAINT DF_PosTerminal_FechaUltimoAcceso DEFAULT (GETDATE()),
        IdUsuarioUltimoAcceso  INT NULL,
        FechaRevocacion        DATETIME NULL,
        IdUsuarioRevocacion    INT NULL,
        CONSTRAINT FK_PosTerminal_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_PosTerminal_EmpresaHuellaActiva'
      AND object_id = OBJECT_ID(N'dbo.PosTerminal')
)
BEGIN
    CREATE UNIQUE INDEX UX_PosTerminal_EmpresaHuellaActiva
        ON dbo.PosTerminal (IdEmpresa, Huella)
        WHERE Estado = N'ACTIVO';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PosTerminal_EmpresaEstado'
      AND object_id = OBJECT_ID(N'dbo.PosTerminal')
)
BEGIN
    CREATE INDEX IX_PosTerminal_EmpresaEstado
        ON dbo.PosTerminal (IdEmpresa, Estado);
END
GO

PRINT 'PosTerminal + LimiteTerminalesPos listos en AlahiaPos_Dev.';
GO
