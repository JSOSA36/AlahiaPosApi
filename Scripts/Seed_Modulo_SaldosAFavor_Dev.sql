USE AlahiaPos_Dev;
GO
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'SALDOS_A_FAVOR')
BEGIN
  IF COL_LENGTH('dbo.Modulos', 'PrecioUSD') IS NOT NULL
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo, PrecioUSD)
    VALUES (N'SALDOS_A_FAVOR', N'Saldos a favor', N'Consulta de saldos a favor por notas de crédito', 1, 0);
  ELSE
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo)
    VALUES (N'SALDOS_A_FAVOR', N'Saldos a favor', N'Consulta de saldos a favor por notas de crédito', 1);
END
GO

DECLARE @IdModulo int = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'SALDOS_A_FAVOR');

IF @IdModulo IS NOT NULL
BEGIN
  INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
  SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
  FROM dbo.Empresas e
  WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );
END
GO

PRINT 'SALDOS_A_FAVOR seeded';
SELECT Codigo, Nombre FROM dbo.Modulos WHERE Codigo IN (N'SALDOS_A_FAVOR', N'LISTADO_DEVOLUCIONES');
GO
