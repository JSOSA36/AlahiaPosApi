-- AlahiaPos_Prod — Activar LISTADO_CAJA (Listado de Cierre) para Clínica Dental Sena.
-- Autorizado por el usuario en este mensaje.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa
    FROM dbo.Empresas
    WHERE IdEmpresa = 60
      AND NombreComercial LIKE N'%Sena%'
);

DECLARE @IdModulo INT = (
    SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'LISTADO_CAJA'
);

IF @IdEmpresa IS NULL
BEGIN
    RAISERROR(N'No se encontró Clínica Dental Sena (IdEmpresa 60) en Prod.', 16, 1);
    RETURN;
END;

IF @IdModulo IS NULL
BEGIN
    RAISERROR(N'No existe el módulo LISTADO_CAJA.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdModulo
)
    INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
    VALUES (@IdEmpresa, @IdModulo, 1, GETDATE());
ELSE
    UPDATE dbo.Empresa_Modulos
    SET Activo = 1, FechaDesactivacion = NULL
    WHERE EmpresaId = @IdEmpresa AND ModuloId = @IdModulo;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdModulo, 1, GETDATE(), @IdEmpresa
FROM dbo.Perfiles p
WHERE p.IdEmpresa = @IdEmpresa
  AND p.Activo = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil
        AND pr.IdModulo = @IdModulo
        AND pr.IdEmpresa = @IdEmpresa
  );

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
WHERE pr.IdEmpresa = @IdEmpresa
  AND pr.IdModulo = @IdModulo
  AND p.IdEmpresa = @IdEmpresa
  AND p.Activo = 1;

COMMIT TRAN;

SELECT e.IdEmpresa, e.NombreComercial, m.Codigo, em.Activo AS EmpresaModuloActivo
FROM dbo.Empresas e
INNER JOIN dbo.Empresa_Modulos em ON em.EmpresaId = e.IdEmpresa
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE e.IdEmpresa = @IdEmpresa AND m.Codigo = N'LISTADO_CAJA';

SELECT p.IdPerfil, p.Nombre, pr.Activo AS PerfilRolActivo
FROM dbo.Perfiles p
INNER JOIN dbo.PerfilRoles pr ON pr.IdPerfil = p.IdPerfil AND pr.IdEmpresa = p.IdEmpresa
WHERE p.IdEmpresa = @IdEmpresa AND pr.IdModulo = @IdModulo
ORDER BY p.Nombre;
GO
