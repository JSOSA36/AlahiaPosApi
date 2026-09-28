-- ============================================================
-- PLANTILLA: registrar / activar un módulo SIN romper perfiles
-- ============================================================
-- REGLAS (obligatorias):
-- 1) NUNCA: INSERT/UPDATE PerfilRoles para TODOS los perfiles.
-- 2) NUNCA: UPDATE PerfilRoles SET Activo=1 WHERE IdModulo=@Id sin filtrar perfil.
-- 3) NUNCA: INSERT Empresa_Modulos para TODAS las empresas salvo pedido explícito.
-- 4) Licencia = Empresa_Modulos (MacroBits / script acotado por IdEmpresa).
-- 5) Menú = PerfilRoles: solo Administrador, o un perfil/empresa listados.
-- ============================================================
USE AlahiaPos_Dev; -- o AlahiaPos_Prod con autorización explícita
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @Codigo NVARCHAR(80) = N'MI_MODULO';
DECLARE @Nombre NVARCHAR(120) = N'Mi módulo';
DECLARE @IdEmpresa INT = NULL; -- NULL = no tocar licencia; o un Id concreto

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = @Codigo)
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES (@Codigo, @Nombre, @Nombre, 1);
END

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = @Codigo);

-- Licencia (opcional, una empresa)
IF @IdEmpresa IS NOT NULL AND @IdMod IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Empresa_Modulos
        WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdMod
    )
        INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
        VALUES (@IdEmpresa, @IdMod, 1, GETDATE());
    ELSE
        UPDATE dbo.Empresa_Modulos
        SET Activo = 1, FechaDesactivacion = NULL
        WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdMod;
END

-- Solo Administrador de empresas ya licenciadas
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresa_Modulos em
    ON em.EmpresaId = p.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
WHERE p.Activo = 1
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%')
  AND (@IdEmpresa IS NULL OR p.IdEmpresa = @IdEmpresa)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
  );

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN dbo.Empresa_Modulos em
    ON em.EmpresaId = p.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
WHERE pr.IdModulo = @IdMod
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%')
  AND (@IdEmpresa IS NULL OR p.IdEmpresa = @IdEmpresa);

PRINT 'OK: módulo registrado; PerfilRoles solo Admin.';
GO
