-- ============================================================
-- Alahia AI — módulo estratégico (solo AlahiaPos_Dev)
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'ALAHIA_AI')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'ALAHIA_AI',
        N'Alahia AI',
        N'Asesor empresarial con IA integrada: chat, resumen inteligente y recomendaciones basadas en datos del ERP',
        0,
        1,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Alahia AI',
        Descripcion = N'Asesor empresarial con IA integrada',
        Activo = 1
    WHERE Codigo = N'ALAHIA_AI';
END
GO

DECLARE @IdAi INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'ALAHIA_AI');

IF @IdAi IS NULL
BEGIN
    RAISERROR('No se pudo resolver Id del módulo ALAHIA_AI', 16, 1);
    RETURN;
END

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdAi, 1, GETDATE()
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdAi
);

UPDATE dbo.Empresa_Modulos
SET Activo = 1, FechaDesactivacion = NULL
WHERE ModuloId = @IdAi;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdAi, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdAi AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE dbo.PerfilRoles SET Activo = 1 WHERE IdModulo = @IdAi;

SELECT m.Codigo, m.Nombre,
    (SELECT COUNT(*) FROM dbo.Empresa_Modulos WHERE ModuloId = m.Id AND Activo = 1) AS Empresas,
    (SELECT COUNT(*) FROM dbo.PerfilRoles WHERE IdModulo = m.Id AND Activo = 1) AS Perfiles
FROM dbo.Modulos m
WHERE m.Codigo = N'ALAHIA_AI';

PRINT 'Alahia AI listo en AlahiaPos_Dev.';
GO
