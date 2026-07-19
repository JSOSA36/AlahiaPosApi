-- ============================================================
-- MVP Compras — AlahiaPos_Dev
-- IdAlmacen en cabecera + tabla PagosProveedor
-- ============================================================
USE AlahiaPos_Dev;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'OrdenCompraHeaders' AND COLUMN_NAME = 'IdAlmacen'
)
BEGIN
    ALTER TABLE OrdenCompraHeaders ADD IdAlmacen INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PagosProveedor')
BEGIN
    CREATE TABLE PagosProveedor (
        IdPagoProveedor   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdOrdenCompraHeader INT NOT NULL,
        NumeroDocumento  NVARCHAR(100) NULL,
        IdProveedor      INT NOT NULL,
        FormaPago        NVARCHAR(100) NOT NULL,
        Monto            DECIMAL(18,2) NOT NULL,
        Nota             NVARCHAR(MAX) NULL,
        IdUsuario        INT NULL,
        FechaInseccion   DATETIME NOT NULL CONSTRAINT DF_PagosProveedor_Fecha DEFAULT (GETDATE()),
        IdEmpresa        INT NOT NULL,
        CONSTRAINT FK_PagosProveedor_OrdenCompraHeaders
            FOREIGN KEY (IdOrdenCompraHeader) REFERENCES OrdenCompraHeaders(IdOrdenCompraHeader),
        CONSTRAINT FK_PagosProveedor_Proveedores
            FOREIGN KEY (IdProveedor) REFERENCES Proveedores(IdProveedor)
    );

    CREATE INDEX IX_PagosProveedor_OrdenCompra
        ON PagosProveedor(IdOrdenCompraHeader);

    CREATE INDEX IX_PagosProveedor_Empresa
        ON PagosProveedor(IdEmpresa);
END
GO

-- Módulos de menú Compras (Dev)
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'PROVEEDORES')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('PROVEEDORES', 'Proveedores', 'Catálogo de proveedores', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'FACTURAS_COMPRA')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('FACTURAS_COMPRA', 'Facturas de compra', 'Registro de facturas de compra', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'CUENTAS_PAGAR_PROVEEDOR')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('CUENTAS_PAGAR_PROVEEDOR', 'Cuentas por pagar', 'CxP proveedores', 0, 1, GETDATE());

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
