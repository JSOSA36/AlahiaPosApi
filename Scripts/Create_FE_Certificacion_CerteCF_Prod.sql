-- Certificación CerteCF (laboratorio e-CF) — AlahiaPos_Prod
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.CertecfCaso', N'U') IS NULL
AND OBJECT_ID(N'dbo.CertecfSesion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CertecfSesion (
        IdSesion            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CertecfSesion PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        IdUsuario           INT NULL,
        NombreArchivo       NVARCHAR(260) NOT NULL,
        TipoSet             NVARCHAR(20)  NOT NULL CONSTRAINT DF_CertecfSesion_Tipo DEFAULT (N'ECF'),
        Estado              NVARCHAR(30)  NOT NULL CONSTRAINT DF_CertecfSesion_Estado DEFAULT (N'Cargado'),
        Ambiente            NVARCHAR(20)  NOT NULL CONSTRAINT DF_CertecfSesion_Amb DEFAULT (N'certecf'),
        Mensaje             NVARCHAR(1000) NULL,
        FechaCreacion       DATETIME NOT NULL CONSTRAINT DF_CertecfSesion_Fecha DEFAULT (GETDATE()),
        FechaActualizacion  DATETIME NULL,
        CONSTRAINT FK_CertecfSesion_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );
    CREATE INDEX IX_CertecfSesion_Empresa ON dbo.CertecfSesion (IdEmpresa, FechaCreacion DESC);
END
GO

IF OBJECT_ID(N'dbo.CertecfCaso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CertecfCaso (
        IdCaso           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CertecfCaso PRIMARY KEY,
        IdSesion         INT NOT NULL,
        Orden            INT NOT NULL,
        Oleada           INT NOT NULL CONSTRAINT DF_CertecfCaso_Oleada DEFAULT (1),
        TipoEcf          INT NOT NULL,
        Encf             NVARCHAR(20) NOT NULL,
        Estado           NVARCHAR(30) NOT NULL CONSTRAINT DF_CertecfCaso_Estado DEFAULT (N'Pendiente'),
        TrackId          NVARCHAR(80) NULL,
        PayloadJson      NVARCHAR(MAX) NOT NULL,
        Mensaje          NVARCHAR(2000) NULL,
        FechaEnvio       DATETIME NULL,
        FechaRespuesta   DATETIME NULL,
        CONSTRAINT FK_CertecfCaso_Sesion FOREIGN KEY (IdSesion)
            REFERENCES dbo.CertecfSesion (IdSesion) ON DELETE CASCADE
    );
    CREATE INDEX IX_CertecfCaso_Sesion ON dbo.CertecfCaso (IdSesion, Oleada, Orden);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'FE_CERTIFICACION')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'FE_CERTIFICACION',
        N'Certificación e-CF (CerteCF)',
        N'Laboratorio de certificación DGII: certificado digital, Excel de set de pruebas y envío a CerteCF.',
        0, 1, GETDATE()
    );
GO

DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'FE_CERTIFICACION');

-- Solo MacroBits (EsEmpresaSistema). Nunca se licencia al cliente.
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );

UPDATE em
SET em.Activo = CASE WHEN ISNULL(e.EsEmpresaSistema, 0) = 1 THEN 1 ELSE 0 END,
    em.FechaDesactivacion = CASE WHEN ISNULL(e.EsEmpresaSistema, 0) = 1 THEN NULL ELSE GETDATE() END
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdModulo;
GO

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON m.Codigo = N'FE_CERTIFICACION'
WHERE EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = m.Id AND em.Activo = 1
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
);
GO

