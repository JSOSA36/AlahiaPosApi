-- Demo de producción (recetas + órdenes) — solo AlahiaPos_Dev / Sabor Urbano.
-- Idempotente por CodigoBarra QA-MFG-* y número de orden OP-2026-00xx.
USE AlahiaPos_Dev;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @IdEmpresa INT = 62;
DECLARE @IdAlmacen INT;
DECLARE @IdProveedor INT;
DECLARE @IdUsuario INT;
DECLARE @IdCatMP INT;
DECLARE @IdCatPT INT;
DECLARE @UndLibra INT = 4;
DECLARE @UndUnidad INT = 1;

DECLARE @IdHarina INT, @IdAzucar INT, @IdHuevos INT, @IdLeche INT, @IdMantequilla INT, @IdAceite INT, @IdCarne INT;
DECLARE @IdBizcocho INT, @IdPastelito INT;
DECLARE @IdRecetaBiz INT, @IdRecetaPas INT;
DECLARE @IdOp1 INT, @IdOp2 INT, @IdOp3 INT;

SELECT TOP 1 @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE IdEmpresa = @IdEmpresa AND Activo = 1 ORDER BY EsPrincipal DESC, IdAlmacen;
SELECT TOP 1 @IdProveedor = IdProveedor FROM dbo.Proveedores WHERE IdEmpresa = @IdEmpresa AND ISNULL(IsActivo,1) = 1 ORDER BY IdProveedor;
SELECT TOP 1 @IdUsuario = IdUsuario FROM dbo.Usuarios WHERE IdEmpresa = @IdEmpresa ORDER BY IdUsuario;

IF @IdAlmacen IS NULL OR @IdProveedor IS NULL
BEGIN
    RAISERROR('Sabor Urbano necesita almacén y proveedor.', 16, 1);
    RETURN;
END;

SELECT @IdCatMP = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Materias primas';
IF @IdCatMP IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, TipoOperacion)
    VALUES (N'Materias primas', N'Insumos de producción', N'COMPRA', 1, GETDATE(), 20, @IdEmpresa, N'COMPRA');
    SET @IdCatMP = SCOPE_IDENTITY();
END;

SELECT @IdCatPT = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Producción propia';
IF @IdCatPT IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, TipoOperacion)
    VALUES (N'Producción propia', N'Producto terminado de receta', N'VENTA', 1, GETDATE(), 21, @IdEmpresa, N'VENTA');
    SET @IdCatPT = SCOPE_IDENTITY();
END;

DECLARE @P TABLE (
    Codigo NVARCHAR(30) PRIMARY KEY,
    Nombre NVARCHAR(120),
    Descripcion NVARCHAR(200),
    IdUnidad INT,
    PrecioCompra DECIMAL(18,2),
    PrecioVenta DECIMAL(18,2),
    SeCompra BIT,
    SeVende BIT,
    TipoOp VARCHAR(20),
    IdCat INT,
    Stock DECIMAL(18,2)
);

INSERT INTO @P VALUES
(N'QA-MFG-HARINA', N'Harina de trigo', N'Materia prima — libras', @UndLibra, 28, 0, 1, 0, 'COMPRA', @IdCatMP, 35),
(N'QA-MFG-AZUCAR', N'Azúcar blanca', N'Materia prima — libras', @UndLibra, 22, 0, 1, 0, 'COMPRA', @IdCatMP, 80),
(N'QA-MFG-HUEVOS', N'Huevos', N'Materia prima — unidades', @UndUnidad, 6, 0, 1, 0, 'COMPRA', @IdCatMP, 200),
(N'QA-MFG-LECHE', N'Leche entera', N'Materia prima — litros (unidad)', @UndUnidad, 45, 0, 1, 0, 'COMPRA', @IdCatMP, 8),
(N'QA-MFG-MANT', N'Mantequilla', N'Materia prima — libras', @UndLibra, 90, 0, 1, 0, 'COMPRA', @IdCatMP, 10),
(N'QA-MFG-ACEITE', N'Aceite vegetal', N'Materia prima — galones (unidad)', @UndUnidad, 180, 0, 1, 0, 'COMPRA', @IdCatMP, 1),
(N'QA-MFG-CARNE', N'Carne molida', N'Materia prima — libras', @UndLibra, 140, 0, 1, 0, 'COMPRA', @IdCatMP, 6),
(N'QA-MFG-BIZCOCHO', N'Bizcocho clásico', N'Producto terminado — receta de producción', @UndUnidad, 0, 350, 0, 1, 'VENTA', @IdCatPT, 0),
(N'QA-MFG-PASTELITO', N'Pastelito de carne', N'Producto terminado — receta de producción', @UndUnidad, 0, 45, 0, 1, 'VENTA', @IdCatPT, 0);

MERGE dbo.Productos AS t
USING @P AS s
    ON t.IdEmpresa = @IdEmpresa AND t.CodigoBarra = s.Codigo
WHEN MATCHED THEN UPDATE SET
    t.Nombre = s.Nombre,
    t.Descripcion = s.Descripcion,
    t.IdUnidadMedida = s.IdUnidad,
    t.PrecioCompra = s.PrecioCompra,
    t.PrecioVenta = s.PrecioVenta,
    t.Precio1 = s.PrecioVenta,
    t.Stock = s.Stock,
    t.Cantidad = s.Stock,
    t.SeCompra = s.SeCompra,
    t.SeVende = s.SeVende,
    t.TipoOperacion = s.TipoOp,
    t.IdCategoria = s.IdCat,
    t.IdAlmacen = @IdAlmacen,
    t.IdProveedor = @IdProveedor,
    t.ControlarStock = 1,
    t.IsActivo = 1,
    t.TipoComportamiento = N'Inventario',
    t.TipoProducto = N'Producto'
WHEN NOT MATCHED THEN INSERT (
    EsServicio, Nombre, Descripcion, Rentado, Disponibles, IdProveedor, Cantidad, Stock, PrecioVenta,
    IdUnidadMedida, TipoOperacion, IdCategoria, IdAlmacen, CodigoBarra, TipoProducto, TipoComportamiento,
    Precio1, Precio2, Precio3, PrecioDolar, PrecioExterno, PrecioCompra, Itbis,
    SeCompra, SeAlquila, SeVende, ControlarStock, IsActivo, Ganancia, IdCocina,
    FechaInseccion, IdEmpresa, DisponibleEnCitas, DuracionServicio, Descuento, PorcientoDescuento, PorcientoGanancia
) VALUES (
    0, s.Nombre, s.Descripcion, 0, s.Stock, @IdProveedor, s.Stock, s.Stock, s.PrecioVenta,
    s.IdUnidad, s.TipoOp, s.IdCat, @IdAlmacen, s.Codigo, N'Producto', N'Inventario',
    s.PrecioVenta, s.PrecioVenta, s.PrecioVenta, 0, 0, s.PrecioCompra, 0,
    s.SeCompra, 0, s.SeVende, 1, 1, 0, 0,
    GETDATE(), @IdEmpresa, 0, 0, 0, 0, 0
);

SELECT @IdHarina = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-HARINA';
SELECT @IdAzucar = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-AZUCAR';
SELECT @IdHuevos = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-HUEVOS';
SELECT @IdLeche = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-LECHE';
SELECT @IdMantequilla = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-MANT';
SELECT @IdAceite = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-ACEITE';
SELECT @IdCarne = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-CARNE';
SELECT @IdBizcocho = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-BIZCOCHO';
SELECT @IdPastelito = IdProducto FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND CodigoBarra = N'QA-MFG-PASTELITO';

MERGE dbo.AlmacenExistencias AS t
USING (
    SELECT p.IdProducto, x.Stock AS Existencia
    FROM dbo.Productos p
    INNER JOIN @P x ON x.Codigo = p.CodigoBarra
    WHERE p.IdEmpresa = @IdEmpresa
) AS s
    ON t.IdEmpresa = @IdEmpresa AND t.IdAlmacen = @IdAlmacen AND t.IdProducto = s.IdProducto
WHEN MATCHED THEN UPDATE SET t.Existencia = s.Existencia, t.FechaUltimoMovimiento = GETDATE()
WHEN NOT MATCHED THEN INSERT (IdAlmacen, IdProducto, Existencia, IdEmpresa, FechaUltimoMovimiento)
VALUES (@IdAlmacen, s.IdProducto, s.Existencia, @IdEmpresa, GETDATE());

-- Receta bizcocho: rendimiento 1
SELECT @IdRecetaBiz = IdReceta FROM dbo.RecetaProduccion
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Bizcocho clásico' AND IdProductoTerminado = @IdBizcocho;

IF @IdRecetaBiz IS NULL
BEGIN
    INSERT INTO dbo.RecetaProduccion (IdEmpresa, IdProductoTerminado, Nombre, RendimientoBase, IdUnidadMedida, Activa, Observacion, FechaCreacion, IdUsuario)
    VALUES (@IdEmpresa, @IdBizcocho, N'Bizcocho clásico', 1, @UndUnidad, 1,
            N'Demo: 1 bizcocho. Al producir 10 se multiplican los insumos x10.', GETDATE(), @IdUsuario);
    SET @IdRecetaBiz = SCOPE_IDENTITY();
END
ELSE
    UPDATE dbo.RecetaProduccion SET Activa = 1, RendimientoBase = 1, Observacion = N'Demo: 1 bizcocho. Al producir 10 se multiplican los insumos x10.'
    WHERE IdReceta = @IdRecetaBiz;

DELETE FROM dbo.RecetaProduccionItem WHERE IdReceta = @IdRecetaBiz;
INSERT INTO dbo.RecetaProduccionItem (IdReceta, IdProducto, Cantidad, IdUnidadMedida, Orden, Activo) VALUES
(@IdRecetaBiz, @IdHarina, 5, @UndLibra, 1, 1),
(@IdRecetaBiz, @IdAzucar, 3, @UndLibra, 2, 1),
(@IdRecetaBiz, @IdHuevos, 12, @UndUnidad, 3, 1),
(@IdRecetaBiz, @IdLeche, 2, @UndUnidad, 4, 1),
(@IdRecetaBiz, @IdMantequilla, 1, @UndLibra, 5, 1);

-- Receta pastelito: rendimiento 10
SELECT @IdRecetaPas = IdReceta FROM dbo.RecetaProduccion
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Pastelito de carne' AND IdProductoTerminado = @IdPastelito;

IF @IdRecetaPas IS NULL
BEGIN
    INSERT INTO dbo.RecetaProduccion (IdEmpresa, IdProductoTerminado, Nombre, RendimientoBase, IdUnidadMedida, Activa, Observacion, FechaCreacion, IdUsuario)
    VALUES (@IdEmpresa, @IdPastelito, N'Pastelito de carne', 10, @UndUnidad, 1,
            N'Demo: receta para 10 unidades. Una orden de 50 usa factor 5.', GETDATE(), @IdUsuario);
    SET @IdRecetaPas = SCOPE_IDENTITY();
END
ELSE
    UPDATE dbo.RecetaProduccion SET Activa = 1, RendimientoBase = 10 WHERE IdReceta = @IdRecetaPas;

DELETE FROM dbo.RecetaProduccionItem WHERE IdReceta = @IdRecetaPas;
INSERT INTO dbo.RecetaProduccionItem (IdReceta, IdProducto, Cantidad, IdUnidadMedida, Orden, Activo) VALUES
(@IdRecetaPas, @IdCarne, 2, @UndLibra, 1, 1),
(@IdRecetaPas, @IdHarina, 1, @UndLibra, 2, 1),
(@IdRecetaPas, @IdAceite, 0.5, @UndUnidad, 3, 1);

-- Helper: explode materials into an order
DECLARE @InsMat TABLE (IdProducto INT, IdUnidad INT, Teorica DECIMAL(18,4), Disp DECIMAL(18,4), Precio DECIMAL(18,4));

-- OP-2026-0001: 10 bizcochos PLANIFICADA (faltan harina y leche)
IF NOT EXISTS (SELECT 1 FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0001')
BEGIN
    INSERT INTO dbo.OrdenProduccion (
        IdEmpresa, Numero, IdReceta, IdProductoTerminado, CantidadPlanificada, IdAlmacenOrigen, IdAlmacenDestino,
        Fecha, IdUsuarioResponsable, Observacion, Estado, FechaCreacion, IdUsuario)
    VALUES (
        @IdEmpresa, N'OP-2026-0001', @IdRecetaBiz, @IdBizcocho, 10, @IdAlmacen, @IdAlmacen,
        GETDATE(), @IdUsuario, N'Demo para ver faltantes: harina 35 vs 50 lb y leche 8 vs 20 litros.',
        N'PLANIFICADA', GETDATE(), @IdUsuario);
    SET @IdOp1 = SCOPE_IDENTITY();
END
ELSE
    SELECT @IdOp1 = IdOrdenProduccion FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0001';

DELETE FROM dbo.OrdenProduccionMaterial WHERE IdOrdenProduccion = @IdOp1;
INSERT INTO dbo.OrdenProduccionMaterial (IdOrdenProduccion, IdProducto, IdUnidadMedida, CantidadTeorica, Disponible, Faltante, PrecioCompra, CostoLinea)
SELECT @IdOp1, i.IdProducto, i.IdUnidadMedida, i.Cantidad * 10,
       ISNULL(e.Existencia, 0),
       CASE WHEN i.Cantidad * 10 > ISNULL(e.Existencia, 0) THEN i.Cantidad * 10 - ISNULL(e.Existencia, 0) ELSE 0 END,
       ISNULL(p.PrecioCompra, 0),
       ROUND(i.Cantidad * 10 * ISNULL(p.PrecioCompra, 0), 2)
FROM dbo.RecetaProduccionItem i
INNER JOIN dbo.Productos p ON p.IdProducto = i.IdProducto
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = i.IdProducto AND e.IdAlmacen = @IdAlmacen AND e.IdEmpresa = @IdEmpresa
WHERE i.IdReceta = @IdRecetaBiz AND i.Activo = 1;

UPDATE dbo.OrdenProduccion SET Estado = N'PLANIFICADA', Observacion = N'Demo para ver faltantes: harina 35 vs 50 lb y leche 8 vs 20 litros.'
WHERE IdOrdenProduccion = @IdOp1;

-- OP-2026-0002: 1 bizcocho EN_PROCESO (hay stock para completar)
IF NOT EXISTS (SELECT 1 FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0002')
BEGIN
    INSERT INTO dbo.OrdenProduccion (
        IdEmpresa, Numero, IdReceta, IdProductoTerminado, CantidadPlanificada, IdAlmacenOrigen, IdAlmacenDestino,
        Fecha, IdUsuarioResponsable, Observacion, Estado, FechaInicio, FechaCreacion, IdUsuario)
    VALUES (
        @IdEmpresa, N'OP-2026-0002', @IdRecetaBiz, @IdBizcocho, 1, @IdAlmacen, @IdAlmacen,
        GETDATE(), @IdUsuario, N'Demo lista para Completar: hay inventario para 1 bizcocho. Pruebe cantidad real 1 y revise movimientos.',
        N'EN_PROCESO', GETDATE(), GETDATE(), @IdUsuario);
    SET @IdOp2 = SCOPE_IDENTITY();
END
ELSE
    SELECT @IdOp2 = IdOrdenProduccion FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0002';

DELETE FROM dbo.OrdenProduccionMaterial WHERE IdOrdenProduccion = @IdOp2;
INSERT INTO dbo.OrdenProduccionMaterial (IdOrdenProduccion, IdProducto, IdUnidadMedida, CantidadTeorica, Disponible, Faltante, PrecioCompra, CostoLinea)
SELECT @IdOp2, i.IdProducto, i.IdUnidadMedida, i.Cantidad,
       ISNULL(e.Existencia, 0),
       CASE WHEN i.Cantidad > ISNULL(e.Existencia, 0) THEN i.Cantidad - ISNULL(e.Existencia, 0) ELSE 0 END,
       ISNULL(p.PrecioCompra, 0),
       ROUND(i.Cantidad * ISNULL(p.PrecioCompra, 0), 2)
FROM dbo.RecetaProduccionItem i
INNER JOIN dbo.Productos p ON p.IdProducto = i.IdProducto
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = i.IdProducto AND e.IdAlmacen = @IdAlmacen AND e.IdEmpresa = @IdEmpresa
WHERE i.IdReceta = @IdRecetaBiz AND i.Activo = 1;

UPDATE dbo.OrdenProduccion
SET Estado = N'EN_PROCESO', FechaInicio = ISNULL(FechaInicio, GETDATE()),
    Observacion = N'Demo lista para Completar: hay inventario para 1 bizcocho. Pruebe cantidad real 1 y revise movimientos.'
WHERE IdOrdenProduccion = @IdOp2 AND Estado <> N'COMPLETADA';

-- OP-2026-0003: 50 pastelitos BORRADOR
IF NOT EXISTS (SELECT 1 FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0003')
BEGIN
    INSERT INTO dbo.OrdenProduccion (
        IdEmpresa, Numero, IdReceta, IdProductoTerminado, CantidadPlanificada, IdAlmacenOrigen, IdAlmacenDestino,
        Fecha, IdUsuarioResponsable, Observacion, Estado, FechaCreacion, IdUsuario)
    VALUES (
        @IdEmpresa, N'OP-2026-0003', @IdRecetaPas, @IdPastelito, 50, @IdAlmacen, @IdAlmacen,
        GETDATE(), @IdUsuario, N'Demo borrador: receta rinde 10; 50 unidades ⇒ carne 10 lb, harina 5 lb, aceite 2.5.',
        N'BORRADOR', GETDATE(), @IdUsuario);
    SET @IdOp3 = SCOPE_IDENTITY();
END
ELSE
    SELECT @IdOp3 = IdOrdenProduccion FROM dbo.OrdenProduccion WHERE IdEmpresa = @IdEmpresa AND Numero = N'OP-2026-0003';

DELETE FROM dbo.OrdenProduccionMaterial WHERE IdOrdenProduccion = @IdOp3;
INSERT INTO dbo.OrdenProduccionMaterial (IdOrdenProduccion, IdProducto, IdUnidadMedida, CantidadTeorica, Disponible, Faltante, PrecioCompra, CostoLinea)
SELECT @IdOp3, i.IdProducto, i.IdUnidadMedida, i.Cantidad * 5,
       ISNULL(e.Existencia, 0),
       CASE WHEN i.Cantidad * 5 > ISNULL(e.Existencia, 0) THEN i.Cantidad * 5 - ISNULL(e.Existencia, 0) ELSE 0 END,
       ISNULL(p.PrecioCompra, 0),
       ROUND(i.Cantidad * 5 * ISNULL(p.PrecioCompra, 0), 2)
FROM dbo.RecetaProduccionItem i
INNER JOIN dbo.Productos p ON p.IdProducto = i.IdProducto
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = i.IdProducto AND e.IdAlmacen = @IdAlmacen AND e.IdEmpresa = @IdEmpresa
WHERE i.IdReceta = @IdRecetaPas AND i.Activo = 1;

UPDATE dbo.OrdenProduccion SET Estado = N'BORRADOR'
WHERE IdOrdenProduccion = @IdOp3 AND Estado NOT IN (N'COMPLETADA', N'CANCELADA');

SELECT N'Recetas' AS Tipo, IdReceta, Nombre, RendimientoBase, IdProductoTerminado FROM dbo.RecetaProduccion WHERE IdEmpresa = @IdEmpresa;
SELECT o.Numero, o.Estado, o.CantidadPlanificada, pr.Nombre AS Producto
FROM dbo.OrdenProduccion o
INNER JOIN dbo.Productos pr ON pr.IdProducto = o.IdProductoTerminado
WHERE o.IdEmpresa = @IdEmpresa
ORDER BY o.Numero;
SELECT o.Numero, p.Nombre, m.CantidadTeorica, m.Disponible, m.Faltante
FROM dbo.OrdenProduccionMaterial m
INNER JOIN dbo.OrdenProduccion o ON o.IdOrdenProduccion = m.IdOrdenProduccion
INNER JOIN dbo.Productos p ON p.IdProducto = m.IdProducto
WHERE o.IdEmpresa = @IdEmpresa
ORDER BY o.Numero, m.IdOrdenMaterial;
