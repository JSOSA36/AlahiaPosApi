/*
  EmpresaAiConfig — configuración de proveedor IA por empresa (solo Dev primero)
*/
USE AlahiaPos_Dev;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.EmpresaAiConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmpresaAiConfig
    (
        IdEmpresa INT NOT NULL CONSTRAINT PK_EmpresaAiConfig PRIMARY KEY,
        Provider NVARCHAR(40) NOT NULL CONSTRAINT DF_EmpresaAiConfig_Provider DEFAULT (N'OpenAI'),
        Model NVARCHAR(120) NOT NULL CONSTRAINT DF_EmpresaAiConfig_Model DEFAULT (N'gpt-4o-mini'),
        ApiKeyCipher NVARCHAR(MAX) NULL,
        BaseUrl NVARCHAR(300) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_EmpresaAiConfig_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_EmpresaAiConfig_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NULL,
        IdUsuarioActualizacion INT NULL,
        CONSTRAINT FK_EmpresaAiConfig_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas(IdEmpresa)
    );
END
GO

PRINT 'EmpresaAiConfig lista en AlahiaPos_Dev.';
GO
