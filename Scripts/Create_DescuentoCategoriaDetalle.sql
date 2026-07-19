-- Descuentos por categoría de producto
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'DescuentoCategoriaDetalle'
)
BEGIN
    CREATE TABLE DescuentoCategoriaDetalle (
        IdDescuentoCategoriaDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdDescuentoHeader INT NOT NULL,
        IdCategoria INT NOT NULL,
        CONSTRAINT FK_DescuentoCategoriaDetalle_Header
            FOREIGN KEY (IdDescuentoHeader)
            REFERENCES DescuentoHeader(IdDescuentoHeader)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_DescuentoCategoriaDetalle_Header
        ON DescuentoCategoriaDetalle(IdDescuentoHeader);

    CREATE INDEX IX_DescuentoCategoriaDetalle_Categoria
        ON DescuentoCategoriaDetalle(IdCategoria);
END
GO
