-- Agrega almacén al movimiento de inventario (ejecutar una sola vez)
IF COL_LENGTH('MovimientosInventario', 'IdAlmacen') IS NULL
BEGIN
    ALTER TABLE MovimientosInventario
    ADD IdAlmacen INT NULL;
END
GO
