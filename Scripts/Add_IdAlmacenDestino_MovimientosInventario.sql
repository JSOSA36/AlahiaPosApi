-- Almacén destino para transferencias entre almacenes
IF COL_LENGTH('MovimientosInventario', 'IdAlmacenDestino') IS NULL
BEGIN
    ALTER TABLE MovimientosInventario
    ADD IdAlmacenDestino INT NULL;
END
GO
