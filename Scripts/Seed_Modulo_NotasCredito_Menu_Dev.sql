-- Módulo menú: Notas de Crédito (Clientes) — AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'NOTAS_CREDITO')
BEGIN
    IF COL_LENGTH('dbo.Modulos', 'PrecioUSD') IS NOT NULL
        INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
        VALUES (N'NOTAS_CREDITO', N'Notas de Crédito', N'Alta y consulta de notas de crédito comerciales (no devolución)', 0, 1, GETDATE());
    ELSE
        INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo)
        VALUES (N'NOTAS_CREDITO', N'Notas de Crédito', N'Alta y consulta de notas de crédito comerciales (no devolución)', 1);
END
GO

DECLARE @IdModulo int = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'NOTAS_CREDITO');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
FROM dbo.Empresas e
WHERE @IdModulo IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );
GO

PRINT 'NOTAS_CREDITO seeded en AlahiaPos_Dev';
SELECT Codigo, Nombre FROM dbo.Modulos WHERE Codigo = N'NOTAS_CREDITO';
GO
