-- Antigüedad de Saldos CxC / CxP — Dev
USE AlahiaPos_Dev;
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = N'ANTIGUEDAD_CXC')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'ANTIGUEDAD_CXC', N'Antigüedad de Saldos CxC', N'Análisis de antigüedad de cuentas por cobrar', 0, 1, GETDATE());
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = N'ANTIGUEDAD_CXP')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'ANTIGUEDAD_CXP', N'Antigüedad de Saldos CxP', N'Análisis de antigüedad de cuentas por pagar', 0, 1, GETDATE());
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo IN ('ANTIGUEDAD_CXC', 'ANTIGUEDAD_CXP')
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT 'Modulos ANTIGUEDAD_CXC y ANTIGUEDAD_CXP listos.';
GO
