-- ============================================================
-- Índices para inteligencia de compras desde historial FACTC
-- Sin tabla ProductoProveedor. Fuente: Headers + Detalles.
-- Filtrar siempre: IdTipoDocumentos = 11 AND Estado NOT IN ('BORRADOR','ANULADA')
-- ============================================================

-- Estado / campos de filtro: nvarchar(max) no indexable → acotar
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.OrdenCompraHeaders')
      AND name = 'Estado'
      AND max_length = -1
)
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders ALTER COLUMN Estado NVARCHAR(40) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.OrdenCompraHeaders')
      AND name = 'CondicionFactura'
      AND max_length = -1
)
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders ALTER COLUMN CondicionFactura NVARCHAR(20) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.OrdenCompraHeaders')
      AND name = 'NumeroDocumento'
      AND max_length = -1
)
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders ALTER COLUMN NumeroDocumento NVARCHAR(50) NULL;
END
GO

-- Join detalle → header (imprescindible)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OrdenCompraDetalles_IdOrdenCompraHeader'
      AND object_id = OBJECT_ID('dbo.OrdenCompraDetalles')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrdenCompraDetalles_IdOrdenCompraHeader
        ON dbo.OrdenCompraDetalles (IdOrdenCompraHeader)
        INCLUDE (IdProducto, Cantidad, Descuento, Itbis, SubTotal, IdEmpresa, TipoComportamientoLinea);
END
GO

-- Historial / KPIs por producto
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OrdenCompraDetalles_Empresa_Producto'
      AND object_id = OBJECT_ID('dbo.OrdenCompraDetalles')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrdenCompraDetalles_Empresa_Producto
        ON dbo.OrdenCompraDetalles (IdEmpresa, IdProducto)
        INCLUDE (IdOrdenCompraHeader, Cantidad, Descuento, Itbis, SubTotal, FechaInseccion);
END
GO

-- Listados FACTC confirmadas por fecha
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OrdenCompraHeaders_Empresa_Tipo_Estado_Fecha'
      AND object_id = OBJECT_ID('dbo.OrdenCompraHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrdenCompraHeaders_Empresa_Tipo_Estado_Fecha
        ON dbo.OrdenCompraHeaders (IdEmpresa, IdTipoDocumentos, Estado, FechaInseccion)
        INCLUDE (IdProveedor, IdAlmacen, NumeroDocumento, Total);
END
GO

-- Historial / ranking por proveedor
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OrdenCompraHeaders_Empresa_Proveedor_Tipo'
      AND object_id = OBJECT_ID('dbo.OrdenCompraHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrdenCompraHeaders_Empresa_Proveedor_Tipo
        ON dbo.OrdenCompraHeaders (IdEmpresa, IdProveedor, IdTipoDocumentos, Estado)
        INCLUDE (FechaInseccion, NumeroDocumento, Total, IdAlmacen);
END
GO

PRINT 'Indices inteligencia compras FACTC aplicados.';
GO
