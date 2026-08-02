-- ============================================================
-- Módulo EMPRESAS_ADMIN — alta/edición de empresas (solo MacroBits)
-- Base: AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'EMPRESAS_ADMIN')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'EMPRESAS_ADMIN',
        N'Empresas (alta)',
        N'Alta y gestión de empresas cliente: datos, módulos y demo por días',
        0, 1, GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Empresas (alta)',
        Descripcion = N'Alta y gestión de empresas cliente: datos, módulos y demo por días',
        Activo = 1
    WHERE Codigo = N'EMPRESAS_ADMIN';
END
GO

DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'EMPRESAS_ADMIN');

-- Licencia solo empresas sistema (MacroBits)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );

UPDATE em SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE em.ModuloId = @IdModulo AND ISNULL(e.EsEmpresaSistema, 0) = 1;

-- Menú en perfiles de MacroBits
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdModulo, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdModulo
        AND ISNULL(pr.IdEmpresa, p.IdEmpresa) = p.IdEmpresa
  );

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Empresas e ON e.IdEmpresa = pr.IdEmpresa
WHERE pr.IdModulo = @IdModulo AND ISNULL(e.EsEmpresaSistema, 0) = 1;

PRINT 'EMPRESAS_ADMIN: módulo + licencia/perfil MacroBits listos.';
GO
