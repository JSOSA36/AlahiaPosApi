-- Activa Formato 607 (ventas) para Terraza / MATBERT (IdEmpresa=55) en AlahiaPos_Dev.
-- Licencia empresa + todos los perfiles activos + flag Generar607.
SET NOCOUNT ON;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'REPORTE_607');

IF @IdModulo IS NULL
BEGIN
    RAISERROR(N'No existe el módulo REPORTE_607.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa AND NombreComercial LIKE N'%MATBERT%'
)
BEGIN
    RAISERROR(N'Empresa 55 no es MATBERT / Terraza.', 16, 1);
    RETURN;
END;

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
  AND p.IdEmpresa = @IdEmpresa;

IF EXISTS (SELECT 1 FROM dbo.DgiiConfiguracionEmpresa WHERE IdEmpresa = @IdEmpresa)
    UPDATE dbo.DgiiConfiguracionEmpresa
    SET Generar607 = 1, FiscalActivo = 1
    WHERE IdEmpresa = @IdEmpresa;
ELSE
    INSERT INTO dbo.DgiiConfiguracionEmpresa (
        IdEmpresa, RegimenTributarioCodigo, EsConstructor, EsComisionista,
        ObligadoLibroVentasSF, VersionInstructivoPreferida, Activo, FechaCreacion,
        FiscalActivo, Generar606, Generar607, GenerarIt1, FacturacionElectronicaActiva
    )
    VALUES (@IdEmpresa, N'ORDINARIO', 0, 0, 0, N'IT-1-2020', 1, GETDATE(), 1, 1, 1, 1, 1);

SELECT N'Empresa_Modulos' AS Fuente, em.Activo, m.Codigo
FROM dbo.Empresa_Modulos em
JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa AND m.Codigo = N'REPORTE_607';

SELECT p.IdPerfil, p.Nombre, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
WHERE pr.IdEmpresa = @IdEmpresa AND pr.IdModulo = @IdModulo;

SELECT IdEmpresa, FiscalActivo, Generar606, Generar607, GenerarIt1
FROM dbo.DgiiConfiguracionEmpresa
WHERE IdEmpresa = @IdEmpresa;
