-- AlahiaPos_Dev — Activar módulo Conciliación Bancaria (empresa 60)
USE AlahiaPos_Dev;
GO

DECLARE @IdEmpresa INT = 60;
DECLARE @IdPerfilAdmin INT = 16;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = 'CONCILIACION_BANCARIA')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo, PrecioUSD, FechaCreacion)
    VALUES (
        'CONCILIACION_BANCARIA',
        N'Conciliación Bancaria',
        N'Centro de trabajo: extracto vs libro banco, matching y cierre auditable.',
        1,
        0,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Activo = 1,
        Nombre = N'Conciliación Bancaria'
    WHERE Codigo = 'CONCILIACION_BANCARIA';
END

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, SYSUTCDATETIME()
FROM dbo.Modulos m
WHERE m.Codigo = 'CONCILIACION_BANCARIA'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo = 'CONCILIACION_BANCARIA';

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfilAdmin, m.Id, 1, SYSUTCDATETIME(), @IdEmpresa
FROM dbo.Modulos m
WHERE m.Codigo = 'CONCILIACION_BANCARIA'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = @IdPerfilAdmin AND pr.IdModulo = m.Id AND pr.IdEmpresa = @IdEmpresa
  );

-- EXTRACTO_BANCARIO: capacidad interna (si existe en catálogo, no se vende aparte; se puede dejar activa para compat).
IF EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = 'EXTRACTO_BANCARIO')
BEGIN
    UPDATE dbo.Modulos
    SET Descripcion = N'Capacidad interna del Centro de Conciliación Bancaria (no módulo menú).'
    WHERE Codigo = 'EXTRACTO_BANCARIO';
END

SELECT e.IdEmpresa, e.NombreComercial, m.Codigo, em.Activo
FROM dbo.Empresa_Modulos em
JOIN dbo.Modulos m ON m.Id = em.ModuloId
JOIN dbo.Empresas e ON e.IdEmpresa = em.EmpresaId
WHERE e.IdEmpresa = @IdEmpresa AND m.Codigo IN ('CONCILIACION_BANCARIA', 'EXTRACTO_BANCARIO', 'CUENTAS_FINANCIERAS');
GO
