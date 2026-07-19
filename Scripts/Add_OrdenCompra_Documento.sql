-- ============================================================
-- Orden de Compra (documento propio) — idempotente
-- TipoDocumentos Id=5 ya existe: "Orden de Compra"
-- ============================================================

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'IdDocumentoOrigen') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders
        ADD IdDocumentoOrigen INT NULL;
END
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FechaEnvioProveedor') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders
        ADD FechaEnvioProveedor DATETIME NULL;
END
GO

-- Secuencia OC por empresa
INSERT INTO SecuenciaDocumentos (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
SELECT 0, 0, 'OC-0000', 5, GETDATE(), e.IdEmpresa
FROM Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM SecuenciaDocumentos s
    WHERE s.IdEmpresa = e.IdEmpresa AND s.IdTipoDocumento = 5
);
GO

-- Módulo menú
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'ORDENES_COMPRA')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('ORDENES_COMPRA', 'Ordenes de Compra', 'Pedidos a proveedores (OC)', 0, 1, GETDATE());
ELSE
    UPDATE Modulos
    SET Nombre = 'Ordenes de Compra',
        Descripcion = 'Pedidos a proveedores (OC)'
    WHERE Codigo = 'ORDENES_COMPRA';
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo = 'ORDENES_COMPRA'
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT 'Orden de Compra: columnas, secuencia OC y módulo listos.';
GO
