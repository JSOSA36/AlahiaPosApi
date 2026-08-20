-- ============================================================
-- Ficha clínica dental — AlahiaPos_Dev
-- El paciente ES el cliente (Clientes). Esta tabla guarda
-- anamnesis, odontograma y datos clínicos; no duplica CxC.
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.FichasClinicas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FichasClinicas
    (
        IdFichaClinica              INT             IDENTITY(1,1) NOT NULL,
        IdEmpresa                   INT             NOT NULL,
        IdCliente                   INT             NOT NULL,
        Nombres                     NVARCHAR(120)   NULL,
        Apellidos                   NVARCHAR(120)   NULL,
        Sexo                        NVARCHAR(20)    NULL,
        EstadoCivil                 NVARCHAR(30)    NULL,
        Nacionalidad                NVARCHAR(80)    NULL,
        ContactoEmergenciaNombre    NVARCHAR(150)   NULL,
        ContactoEmergenciaTelefono  NVARCHAR(40)    NULL,
        AnamnesisJson               NVARCHAR(MAX)   NULL,
        OdontogramaJson             NVARCHAR(MAX)   NULL,
        Medicamentos                NVARCHAR(500)   NULL,
        Observaciones               NVARCHAR(MAX)   NULL,
        Color                       NVARCHAR(80)    NULL,
        TipoProtesis                NVARCHAR(120)   NULL,
        Laboratorio                 NVARCHAR(150)   NULL,
        IdUsuarioCreacion           INT             NOT NULL CONSTRAINT DF_FichasClinicas_UsrCre DEFAULT (0),
        IdUsuarioModificacion       INT             NULL,
        FechaCreacion               DATETIME2(0)    NOT NULL CONSTRAINT DF_FichasClinicas_Cre DEFAULT (SYSUTCDATETIME()),
        FechaModificacion           DATETIME2(0)    NOT NULL CONSTRAINT DF_FichasClinicas_Mod DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_FichasClinicas PRIMARY KEY CLUSTERED (IdFichaClinica),
        CONSTRAINT UQ_FichasClinicas_EmpresaCliente UNIQUE (IdEmpresa, IdCliente)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FichasClinicas_Empresas'
)
BEGIN
    ALTER TABLE dbo.FichasClinicas
    ADD CONSTRAINT FK_FichasClinicas_Empresas
        FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FichasClinicas_Clientes'
)
BEGIN
    ALTER TABLE dbo.FichasClinicas
    ADD CONSTRAINT FK_FichasClinicas_Clientes
        FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes (IDCliente);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'FICHA_CLINICA')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'FICHA_CLINICA',
        N'Ficha del paciente',
        N'Historia clínica dental del cliente: anamnesis, odontograma y cuenta desde facturas',
        0,
        1,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Ficha del paciente',
        Descripcion = N'Historia clínica dental del cliente: anamnesis, odontograma y cuenta desde facturas',
        Activo = 1
    WHERE Codigo = N'FICHA_CLINICA';
END
GO

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'FICHA_CLINICA');

IF @IdMod IS NULL
BEGIN
    RAISERROR('No se pudo resolver Id del módulo FICHA_CLINICA', 16, 1);
    RETURN;
END

-- Activar solo en empresas que ya tienen documentos clínicos (clínicas).
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT em.EmpresaId, @IdMod, 1, GETDATE()
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE m.Codigo = N'DOCUMENTOS_CLINICOS'
  AND em.Activo = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos x
      WHERE x.EmpresaId = em.EmpresaId AND x.ModuloId = @IdMod
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresa_Modulos doc
    ON doc.EmpresaId = em.EmpresaId AND doc.Activo = 1
INNER JOIN dbo.Modulos m ON m.Id = doc.ModuloId AND m.Codigo = N'DOCUMENTOS_CLINICOS'
WHERE em.ModuloId = @IdMod;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
WHERE EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
WHERE pr.IdModulo = @IdMod
  AND EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = pr.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
  );

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo = N'FICHA_CLINICA';

PRINT 'FICHA_CLINICA listo en AlahiaPos_Dev.';
GO
