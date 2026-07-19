-- Módulo menú: Análisis Producto-Proveedor (Dev / Prod)
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'ANALISIS_COMPRAS_PRODUCTO')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('ANALISIS_COMPRAS_PRODUCTO', 'Analisis Producto-Proveedor', 'KPIs de compras desde historial FACTC', 0, 1, GETDATE());
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo = 'ANALISIS_COMPRAS_PRODUCTO'
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT 'Modulo ANALISIS_COMPRAS_PRODUCTO listo.';
GO
