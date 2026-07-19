-- ============================================================
-- Deploy Compras MVP + TipoComportamiento — AlahiaPos_Prod
-- Idempotente. No modifica EsServicio ni datos existentes.
-- Ejecutar en orden: este script único.
-- ============================================================
USE AlahiaPos_Prod;
GO

PRINT '=== 1. OrdenCompraHeaders.IdAlmacen ===';
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'OrdenCompraHeaders' AND COLUMN_NAME = 'IdAlmacen'
)
BEGIN
    ALTER TABLE OrdenCompraHeaders ADD IdAlmacen INT NULL;
    PRINT 'Columna IdAlmacen creada.';
END
ELSE
    PRINT 'Columna IdAlmacen ya existe.';
GO

PRINT '=== 2. Tabla PagosProveedor ===';
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PagosProveedor')
BEGIN
    CREATE TABLE PagosProveedor (
        IdPagoProveedor       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdOrdenCompraHeader   INT NOT NULL,
        NumeroDocumento       NVARCHAR(100) NULL,
        IdProveedor           INT NOT NULL,
        FormaPago             NVARCHAR(100) NOT NULL,
        Monto                 DECIMAL(18,2) NOT NULL,
        Nota                  NVARCHAR(MAX) NULL,
        IdUsuario             INT NULL,
        FechaInseccion        DATETIME NOT NULL CONSTRAINT DF_PagosProveedor_Fecha DEFAULT (GETDATE()),
        IdEmpresa             INT NOT NULL,
        CONSTRAINT FK_PagosProveedor_OrdenCompraHeaders
            FOREIGN KEY (IdOrdenCompraHeader) REFERENCES OrdenCompraHeaders(IdOrdenCompraHeader),
        CONSTRAINT FK_PagosProveedor_Proveedores
            FOREIGN KEY (IdProveedor) REFERENCES Proveedores(IdProveedor)
    );

    CREATE INDEX IX_PagosProveedor_OrdenCompra
        ON PagosProveedor(IdOrdenCompraHeader);

    CREATE INDEX IX_PagosProveedor_Empresa
        ON PagosProveedor(IdEmpresa);

    PRINT 'Tabla PagosProveedor creada.';
END
ELSE
    PRINT 'Tabla PagosProveedor ya existe.';
GO

PRINT '=== 3. Productos.TipoComportamiento ===';
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Productos') AND name = 'TipoComportamiento'
)
BEGIN
    ALTER TABLE Productos
        ADD TipoComportamiento NVARCHAR(30) NULL;
    PRINT 'Columna TipoComportamiento creada.';
END
ELSE
    PRINT 'Columna TipoComportamiento ya existe.';
GO

-- Comportamiento de compra: NO modifica EsServicio (naturaleza comercial / POS).
UPDATE Productos
SET TipoComportamiento = CASE
    WHEN ControlarStock = 1 THEN 'Inventario'
    WHEN EsServicio = 1 THEN 'Gasto'
    ELSE 'Gasto'
END
WHERE TipoComportamiento IS NULL OR LTRIM(RTRIM(TipoComportamiento)) = '';
GO

PRINT '=== 4. OrdenCompraDetalles (snapshot compras) ===';
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'TipoComportamientoLinea'
)
    ALTER TABLE OrdenCompraDetalles ADD TipoComportamientoLinea NVARCHAR(30) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'IdGastoGenerado'
)
    ALTER TABLE OrdenCompraDetalles ADD IdGastoGenerado INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'IdActivoFijoGenerado'
)
    ALTER TABLE OrdenCompraDetalles ADD IdActivoFijoGenerado INT NULL;
GO

PRINT '=== 5. Gastos (trazabilidad compras) ===';
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Gastos') AND name = 'IdOrdenCompraDetalle'
)
    ALTER TABLE Gastos ADD IdOrdenCompraDetalle INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Gastos') AND name = 'OrigenModulo'
)
    ALTER TABLE Gastos ADD OrigenModulo NVARCHAR(30) NULL;
GO

PRINT '=== 6. Módulos de menú Compras ===';
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'PROVEEDORES')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('PROVEEDORES', 'Proveedores', 'Catálogo de proveedores', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'FACTURAS_COMPRA')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('FACTURAS_COMPRA', 'Facturas de compra', 'Registro de facturas de compra', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CUENTAS_PAGAR_PROVEEDOR')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('CUENTAS_PAGAR_PROVEEDOR', 'Cuentas por pagar', 'CxP proveedores', 0, 1, GETDATE());
GO

INSERT INTO Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, m.Id, 1, GETDATE()
FROM Empresas e
CROSS JOIN Modulos m
WHERE m.Codigo IN ('PROVEEDORES', 'FACTURAS_COMPRA', 'CUENTAS_PAGAR_PROVEEDOR')
  AND NOT EXISTS (
      SELECT 1 FROM Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
  );
GO

PRINT '=== 7. Secuencia FACTC (tipo documento 11) por empresa ===';
INSERT INTO SecuenciaDocumentos (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
SELECT 0, 0, 'FACTC-0000', 11, GETDATE(), e.IdEmpresa
FROM Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM SecuenciaDocumentos s
    WHERE s.IdEmpresa = e.IdEmpresa AND s.IdTipoDocumento = 11
);
GO

PRINT '=== 8. Verificación ===';
SELECT
    'OrdenCompraHeaders.IdAlmacen' AS Objeto,
    CASE WHEN EXISTS(
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('OrdenCompraHeaders') AND name = 'IdAlmacen'
    ) THEN 'OK' ELSE 'FALTA' END AS Estado
UNION ALL
SELECT 'PagosProveedor',
    CASE WHEN EXISTS(SELECT 1 FROM sys.tables WHERE name = 'PagosProveedor') THEN 'OK' ELSE 'FALTA' END
UNION ALL
SELECT 'Productos.TipoComportamiento',
    CASE WHEN EXISTS(
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Productos') AND name = 'TipoComportamiento'
    ) THEN 'OK' ELSE 'FALTA' END
UNION ALL
SELECT 'MODULO FACTURAS_COMPRA',
    CASE WHEN EXISTS(SELECT 1 FROM Modulos WHERE Codigo = 'FACTURAS_COMPRA') THEN 'OK' ELSE 'FALTA' END
UNION ALL
SELECT 'Secuencias FACTC',
    CAST(COUNT(*) AS VARCHAR(10))
FROM SecuenciaDocumentos WHERE IdTipoDocumento = 11;
GO

SELECT
    IdEmpresa,
    COUNT(*) AS TotalActivos,
    SUM(CASE WHEN EsServicio = 1 THEN 1 ELSE 0 END) AS ServiciosComerciales,
    SUM(CASE WHEN TipoComportamiento = 'Inventario' THEN 1 ELSE 0 END) AS ComportamientoInventario,
    SUM(CASE WHEN TipoComportamiento = 'Gasto' THEN 1 ELSE 0 END) AS ComportamientoGasto
FROM Productos
WHERE IsActivo = 1
GROUP BY IdEmpresa
ORDER BY IdEmpresa;
GO

PRINT 'Deploy Compras MVP en AlahiaPos_Prod completado.';
GO
