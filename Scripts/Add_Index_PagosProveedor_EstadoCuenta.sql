-- Índice para Estado de Cuenta / pagos por proveedor
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PagosProveedor_Empresa_Proveedor_Fecha'
      AND object_id = OBJECT_ID('dbo.PagosProveedor')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PagosProveedor_Empresa_Proveedor_Fecha
        ON dbo.PagosProveedor (IdEmpresa, IdProveedor, FechaInseccion)
        INCLUDE (Monto, IdOrdenCompraHeader, NumeroDocumento, FormaPago);
END
GO

PRINT 'Indice PagosProveedor Estado Cuenta aplicado.';
GO
