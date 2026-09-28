-- ============================================================
-- Módulo: Cruce stock vs ventas (Inventario)
-- Dev only. No regala a todos los perfiles.
-- Licencia: empresas que ya tienen MOVIMIENTO_INVENTARIO.
-- PerfilRoles: solo Administrador de esas empresas.
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @Codigo NVARCHAR(80) = N'REPORTE_CRUCE_STOCK';
DECLARE @Nombre NVARCHAR(120) = N'Cruce stock vs ventas';

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = @Codigo)
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (@Codigo, @Nombre, N'Stock al abrir menos vendido vs stock al cerrar por turno de caja', 0, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = @Nombre,
        Descripcion = N'Stock al abrir menos vendido vs stock al cerrar por turno de caja',
        Activo = 1
    WHERE Codigo = @Codigo;
END

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = @Codigo);
DECLARE @IdInv INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'MOVIMIENTO_INVENTARIO');

IF @IdMod IS NULL
BEGIN
    RAISERROR(N'No se pudo registrar REPORTE_CRUCE_STOCK.', 16, 1);
    RETURN;
END;

-- Licenciar solo empresas que ya tienen movimiento de inventario
IF @IdInv IS NOT NULL
BEGIN
    INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
    SELECT em.EmpresaId, @IdMod, 1, GETDATE()
    FROM dbo.Empresa_Modulos em
    WHERE em.ModuloId = @IdInv
      AND em.Activo = 1
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Empresa_Modulos x
          WHERE x.EmpresaId = em.EmpresaId
            AND x.ModuloId = @IdMod
      );

    UPDATE x
    SET x.Activo = 1,
        x.FechaDesactivacion = NULL
    FROM dbo.Empresa_Modulos x
    INNER JOIN dbo.Empresa_Modulos inv
        ON inv.EmpresaId = x.EmpresaId
       AND inv.ModuloId = @IdInv
       AND inv.Activo = 1
    WHERE x.ModuloId = @IdMod;
END

-- Solo Administrador
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresa_Modulos em
    ON em.EmpresaId = p.IdEmpresa AND em.ModuloId = @IdMod AND em.Activo = 1
WHERE p.Activo = 1
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%')
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
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%');

PRINT 'OK: REPORTE_CRUCE_STOCK registrado (solo Admin / empresas con inventario).';
GO
