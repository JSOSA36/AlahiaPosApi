-- ============================================================
-- Impresión térmica — módulo Configuración (solo AlahiaPos_Dev)
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'IMPRESION_TERMICA')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'IMPRESION_TERMICA',
        N'Impresión térmica',
        N'Agente local de impresión: instalar, seleccionar impresora y verificar conexión',
        0,
        1,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Impresión térmica',
        Descripcion = N'Agente local de impresión: instalar, seleccionar impresora y verificar conexión',
        Activo = 1
    WHERE Codigo = N'IMPRESION_TERMICA';
END
GO

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'IMPRESION_TERMICA');

IF @IdMod IS NULL
BEGIN
    RAISERROR('No se pudo resolver Id del módulo IMPRESION_TERMICA', 16, 1);
    RETURN;
END

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdMod, 1, GETDATE()
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdMod
);

UPDATE dbo.Empresa_Modulos
SET Activo = 1, FechaDesactivacion = NULL
WHERE ModuloId = @IdMod;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE dbo.PerfilRoles SET Activo = 1 WHERE IdModulo = @IdMod;

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo = N'IMPRESION_TERMICA';

PRINT 'IMPRESION_TERMICA listo en AlahiaPos_Dev.';
GO
