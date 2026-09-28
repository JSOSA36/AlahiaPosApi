/*
  Terraza 55: conteo físico de María (3 hojas).
  Ajusta Stock / AlmacenExistencias y deja movimiento AJUSTE.
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 55 AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdAlmacen INT;
DECLARE @IdUsuario INT;

SELECT TOP 1 @IdAlmacen = IdAlmacen
FROM dbo.Almacenes
WHERE IdEmpresa = @IdEmpresa AND Activo = 1
ORDER BY EsPrincipal DESC, IdAlmacen;

SELECT TOP 1 @IdUsuario = IdUsuario
FROM dbo.Usuarios
WHERE IdEmpresa = @IdEmpresa AND Estado = 1
ORDER BY IdUsuario;

IF @IdAlmacen IS NULL
BEGIN
    RAISERROR(N'Falta almacén Terraza 55.', 16, 1);
    RETURN;
END;

DECLARE @Conteo TABLE (
    IdProducto INT PRIMARY KEY,
    Nuevo DECIMAL(18,2) NOT NULL,
    Nota NVARCHAR(80) NOT NULL
);

INSERT INTO @Conteo (IdProducto, Nuevo, Nota) VALUES
(1830, 303, N'Kola real'),
(1760, 5, N'G. Trident'),
(1800, 34, N'Trident 8.5'),
(4191, 8, N'Trident 30.6'),
(1768, 14, N'Clorets'),
(1796, 226, N'Menta Hall'),
(1819, 9, N'Jugo Mott G'),
(4073, 29, N'Frutop P'),
(1784, 39, N'Frutop G'),
(1809, 2, N'Granberry'),
(1811, 2, N'Jugo Mott P'),
(4195, 6, N'Cheetos G'),
(1770, 4, N'NatuChips G / Da pa To'),
(1892, 4, N'De Todito G'),
(4187, 4, N'Doritos G'),
(4194, 3, N'Hojuelas G'),
(1793, 6, N'Lays G'),
(1752, 19, N'Almendra'),
(1751, 24, N'Cajuil'),
(1746, 10, N'Pistacho 9+1'),
(1750, 14, N'Mixto'),
(1953, 52, N'Bon o Bon'),
(1980, 9, N'Red Bull G'),
(1886, 46, N'Lays P'),
(1879, 50, N'Natuchips P natural'),
(1989, 4, N'Chicharron'),
(1888, 12, N'De Todito P'),
(1869, 11, N'Doritos P'),
(1876, 15, N'Hojuelitas P'),
(1874, 27, N'Ruffles'),
(1877, 19, N'Cheetos P'),
(1858, 12, N'Mini Chokis'),
(1861, 11, N'Mamut'),
(1862, 8, N'Crackets Queso'),
(1981, 9, N'Ciclon 10-1'),
(4158, 28, N'Kings Pride'),
(1766, 52, N'Vive 100'),
(2038, 143, N'Heineken G'),
(2041, 1, N'Heineken P'),
(2047, 77, N'Presidente mediana 68+9'),
(4072, 143, N'Presidente peq 60+83'),
(2033, 54, N'Miller 2 cajas + 6 (x24)'),
(2040, 46, N'Corona 12+34'),
(4105, 72, N'Coors Light 3 cajas (x24)'),
(4166, 22, N'Blue Moon'),
(2032, 29, N'Stella'),
(1806, 41, N'Smirnoff'),
(2037, 2, N'Modelo'),
(2043, 9, N'One G'),
(2036, 41, N'One P 36+5'),
(2044, 52, N'Brahma G 16+11+18+7'),
(4167, 7, N'Sol'),
(4161, 8, N'Gallo 4+4'),
(1970, 8, N'Coca Cola'),
(1979, 21, N'911'),
(4196, 15, N'Acqua Panna'),
(1835, 30, N'Generade'),
(1828, 8, N'Red Rock Frambuesa (reparto 23)'),
(1812, 7, N'Red Rock Merengue 450 (reparto 23)'),
(1780, 4, N'Red Rock Uva 400 (reparto 23)'),
(4211, 3, N'Red Rock Merengue 400 (reparto 23)'),
(1779, 1, N'Red Rock Naranja 400 (reparto 23)'),
(1827, 0, N'Red Rock Manzana (reparto 23)'),
(4212, 0, N'Red Rock Naranja 450 (reparto 23)'),
(4213, 0, N'Red Rock Uva 450 (reparto 23)'),
(1987, 8, N'Fireball 200ml'),
(1825, 2, N'Coconut / Coco Rico'),
(2008, 4, N'Fireball pequeñito 50ml'),
(2004, 1, N'Fireball G 750'),
(2002, 2, N'Fireball 350ml'),
(2054, 174, N'Marlboro');

IF EXISTS (
    SELECT 1 FROM @Conteo c
    LEFT JOIN dbo.Productos p ON p.IdProducto = c.IdProducto AND p.IdEmpresa = @IdEmpresa AND p.IsActivo = 1
    WHERE p.IdProducto IS NULL
)
BEGIN
    SELECT c.IdProducto, c.Nota
    FROM @Conteo c
    LEFT JOIN dbo.Productos p ON p.IdProducto = c.IdProducto AND p.IdEmpresa = @IdEmpresa AND p.IsActivo = 1
    WHERE p.IdProducto IS NULL;
    RAISERROR(N'Hay productos del conteo que no existen en Terraza 55.', 16, 1);
    RETURN;
END;

DECLARE @IdMovEnt INT, @IdMovSal INT;

BEGIN TRAN;

INSERT INTO dbo.MovimientosInventario (
    TipoMovimiento, Motivo, Referencia, Observacion, Fecha, IdUsuario, IdEmpresa, Activo, IdAlmacen
)
VALUES (
    N'ENTRADA', N'AJUSTE', N'CONTEO_MARIA_2026-09-03',
    N'Conteo físico María — aumentos', GETDATE(), @IdUsuario, @IdEmpresa, 1, @IdAlmacen
);
SET @IdMovEnt = SCOPE_IDENTITY();

INSERT INTO dbo.MovimientosInventario (
    TipoMovimiento, Motivo, Referencia, Observacion, Fecha, IdUsuario, IdEmpresa, Activo, IdAlmacen
)
VALUES (
    N'SALIDA', N'AJUSTE', N'CONTEO_MARIA_2026-09-03',
    N'Conteo físico María — disminuciones', GETDATE(), @IdUsuario, @IdEmpresa, 1, @IdAlmacen
);
SET @IdMovSal = SCOPE_IDENTITY();

INSERT INTO dbo.MovimientosInventarioDetalle (
    IdMovimientoInventario, IdProducto, Cantidad, StockAnterior, StockNuevo, Precio, SubTotal, Observacion, Fecha
)
SELECT
    CASE WHEN c.Nuevo >= ISNULL(e.Existencia, p.Stock) THEN @IdMovEnt ELSE @IdMovSal END,
    p.IdProducto,
    ABS(c.Nuevo - ISNULL(e.Existencia, p.Stock)),
    ISNULL(e.Existencia, p.Stock),
    c.Nuevo,
    p.PrecioCompra,
    ABS(c.Nuevo - ISNULL(e.Existencia, p.Stock)) * p.PrecioCompra,
    c.Nota,
    GETDATE()
FROM @Conteo c
INNER JOIN dbo.Productos p ON p.IdProducto = c.IdProducto
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = p.IdProducto AND e.IdAlmacen = @IdAlmacen
WHERE ABS(c.Nuevo - ISNULL(e.Existencia, p.Stock)) > 0.0001;

DELETE m
FROM dbo.MovimientosInventario m
WHERE m.Id IN (@IdMovEnt, @IdMovSal)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MovimientosInventarioDetalle d
      WHERE d.IdMovimientoInventario = m.Id
  );

UPDATE p SET
    p.Stock = c.Nuevo,
    p.Cantidad = c.Nuevo,
    p.Disponibles = c.Nuevo
FROM dbo.Productos p
INNER JOIN @Conteo c ON c.IdProducto = p.IdProducto
WHERE p.IdEmpresa = @IdEmpresa;

UPDATE e SET
    e.Existencia = c.Nuevo,
    e.FechaUltimoMovimiento = GETDATE()
FROM dbo.AlmacenExistencias e
INNER JOIN @Conteo c ON c.IdProducto = e.IdProducto
WHERE e.IdAlmacen = @IdAlmacen;

INSERT INTO dbo.AlmacenExistencias (
    IdAlmacen, IdProducto, Existencia, CostoPromedio, StockMinimo, IdEmpresa, FechaUltimoMovimiento
)
SELECT @IdAlmacen, c.IdProducto, c.Nuevo, p.PrecioCompra, 0, @IdEmpresa, GETDATE()
FROM @Conteo c
INNER JOIN dbo.Productos p ON p.IdProducto = c.IdProducto
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AlmacenExistencias x
    WHERE x.IdAlmacen = @IdAlmacen AND x.IdProducto = c.IdProducto
);

COMMIT TRAN;

SELECT
    LEFT(p.Nombre, 42) Producto,
    CONVERT(varchar(12), d.StockAnterior) Antes,
    CONVERT(varchar(12), d.StockNuevo) Ahora,
    CASE WHEN d.StockNuevo >= d.StockAnterior THEN N'+' ELSE N'' END
        + CONVERT(varchar(12), CONVERT(decimal(18,0), d.StockNuevo - d.StockAnterior)) Delta,
    d.Observacion Nota
FROM dbo.MovimientosInventarioDetalle d
INNER JOIN dbo.Productos p ON p.IdProducto = d.IdProducto
WHERE d.IdMovimientoInventario IN (@IdMovEnt, @IdMovSal)
ORDER BY p.Nombre;

SELECT 'Ajustados' t, COUNT(*) c
FROM dbo.MovimientosInventarioDetalle
WHERE IdMovimientoInventario IN (@IdMovEnt, @IdMovSal)
UNION ALL SELECT 'EnConteo', COUNT(*) FROM @Conteo;
GO
