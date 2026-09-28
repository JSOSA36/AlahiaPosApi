-- Laboratorio CerteCF: solo MacroBits (EsEmpresaSistema). AlahiaPos_Prod.
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'FE_CERTIFICACION');
IF @IdModulo IS NULL
BEGIN
    RAISERROR('No existe el módulo FE_CERTIFICACION.', 16, 1);
    RETURN;
END

UPDATE dbo.Modulos
SET Nombre = N'Laboratorio CerteCF',
    Descripcion = N'Herramienta interna MacroBits para certificar emisores en CerteCF. No se licencia al cliente.'
WHERE Id = @IdModulo;

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

DELETE pr
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Empresas e ON e.IdEmpresa = pr.IdEmpresa
WHERE pr.IdModulo = @IdModulo
  AND ISNULL(e.EsEmpresaSistema, 0) = 0;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdModulo, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE ISNULL(e.EsEmpresaSistema, 0) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdModulo AND pr.IdEmpresa = p.IdEmpresa
  );
GO

