/* Demo salón IdEmpresa=69: catálogo tipo Dismerling (1) + empleados demo + comisiones.
   Solo AlahiaPos_Prod. No copia nombres reales ni cédulas de Dismerling. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Origen INT = 1;
DECLARE @Destino INT = 69;
DECLARE @IdAlmacenDest INT;
DECLARE @IdProveedorDest INT;
DECLARE @IdAreaNegocio INT = 10;

SELECT TOP 1 @IdAlmacenDest = IdAlmacen
FROM dbo.Almacenes
WHERE IdEmpresa = @Destino
ORDER BY IdAlmacen;

SELECT TOP 1 @IdProveedorDest = IdProveedor
FROM dbo.Productos
WHERE IdEmpresa = @Destino AND IdProveedor IS NOT NULL;

BEGIN TRAN;

/* 1) Áreas activas de Dismerling (sin "no usar") */
INSERT INTO dbo.Areas (IdEmpresa, Nombre, IdAreaNegocio, IsActivo, FechaInseccion)
SELECT @Destino, a.Nombre, ISNULL(a.IdAreaNegocio, @IdAreaNegocio), CAST(1 AS BIT), CAST(GETDATE() AS DATE)
FROM dbo.Areas a
WHERE a.IdEmpresa = @Origen
  AND ISNULL(a.IsActivo, 1) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Areas d
      WHERE d.IdEmpresa = @Destino AND LTRIM(RTRIM(d.Nombre)) = LTRIM(RTRIM(a.Nombre))
  );

/* 2) Categorías de Dismerling por nombre */
INSERT INTO dbo.Categorias (IdEmpresa, Nombre, Tipo, TipoOperacion, Prioridad, IsActiva, FechaInseccion)
SELECT @Destino, c.Nombre, ISNULL(NULLIF(LTRIM(RTRIM(c.Tipo)), N''), N'Servicio'), N'VENTA', ISNULL(c.Prioridad, 0), CAST(1 AS BIT), CAST(GETDATE() AS DATE)
FROM dbo.Categorias c
WHERE c.IdEmpresa = @Origen
  AND ISNULL(c.IsActiva, 1) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Categorias d
      WHERE d.IdEmpresa = @Destino AND LTRIM(RTRIM(d.Nombre)) = LTRIM(RTRIM(c.Nombre))
  );

/* 3) Servicios/productos de Dismerling que el demo aún no tiene (por nombre) */
INSERT INTO dbo.Productos (
    Nombre, Descripcion, Rentado, Cantidad, Stock, PrecioVenta, Descuento,
    Precio1, Precio2, Precio3, PorcientoDescuento, PorcientoGanancia, PrecioCompra,
    Itbis, SeCompra, SeAlquila, SeVende, ControlarStock, IsActivo, Ganancia,
    FechaInseccion, PrecioDolar, PrecioExterno, IdEmpresa, IdAlmacen, IdProveedor,
    IdCategoria, IdArea, EsServicio, DuracionServicio, DisponibleEnCitas,
    TipoOperacion, TipoComportamiento, TipoProducto
)
SELECT
    p.Nombre,
    p.Descripcion,
    CAST(0 AS BIT),
    CAST(0 AS DECIMAL(18,2)),
    CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.PrecioVenta, 0),
    CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.Precio1, 0),
    ISNULL(p.Precio2, 0),
    ISNULL(p.Precio3, 0),
    CAST(0 AS DECIMAL(18,2)),
    CAST(0 AS DECIMAL(18,2)),
    CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.Itbis, 0),
    CAST(0 AS BIT),
    CAST(0 AS BIT),
    CAST(1 AS BIT),
    CAST(CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN 0 ELSE 0 END AS BIT),
    CAST(1 AS BIT),
    CAST(0 AS DECIMAL(18,2)),
    CAST(GETDATE() AS DATE),
    CAST(0 AS DECIMAL(18,2)),
    CAST(0 AS DECIMAL(18,2)),
    @Destino,
    NULL,
    @IdProveedorDest,
    catD.IdCategoria,
    areaD.IdArea,
    ISNULL(p.EsServicio, 0),
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN ISNULL(NULLIF(p.DuracionServicio, 0), 60) ELSE 0 END,
    CAST(CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN 1 ELSE 0 END AS BIT),
    N'VENTA',
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN N'Servicio' ELSE N'Inventario' END,
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN N'Servicio' ELSE N'Producto' END
FROM dbo.Productos p
LEFT JOIN dbo.Categorias catO ON catO.IdCategoria = p.IdCategoria AND catO.IdEmpresa = @Origen
LEFT JOIN dbo.Categorias catD ON catD.IdEmpresa = @Destino AND LTRIM(RTRIM(catD.Nombre)) = LTRIM(RTRIM(ISNULL(catO.Nombre, N'')))
LEFT JOIN dbo.Areas areaO ON areaO.IdArea = p.IdArea AND areaO.IdEmpresa = @Origen
LEFT JOIN dbo.Areas areaD ON areaD.IdEmpresa = @Destino AND LTRIM(RTRIM(areaD.Nombre)) = LTRIM(RTRIM(ISNULL(areaO.Nombre, N'')))
WHERE p.IdEmpresa = @Origen
  AND ISNULL(p.IsActivo, 1) = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Productos d
      WHERE d.IdEmpresa = @Destino
        AND LTRIM(RTRIM(LOWER(d.Nombre))) = LTRIM(RTRIM(LOWER(p.Nombre)))
  );

/* Fallback: servicios copiados sin categoría → Servicios del demo */
UPDATE p
SET p.IdCategoria = c.IdCategoria
FROM dbo.Productos p
INNER JOIN dbo.Categorias c ON c.IdEmpresa = @Destino AND c.Nombre = N'Servicios'
WHERE p.IdEmpresa = @Destino
  AND p.IdCategoria IS NULL
  AND ISNULL(p.EsServicio, 0) = 1;

UPDATE p
SET p.IdArea = a.IdArea
FROM dbo.Productos p
INNER JOIN dbo.Areas a ON a.IdEmpresa = @Destino AND a.Nombre = N'Servicios'
WHERE p.IdEmpresa = @Destino
  AND (p.IdArea IS NULL OR p.IdArea = 0)
  AND ISNULL(p.EsServicio, 0) = 1;

/* 4) Empleados demo (nombres ficticios; no copiar staff real de Dismerling) */
IF NOT EXISTS (SELECT 1 FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino)
BEGIN
    INSERT INTO dbo.EmpleadosP (Nombre, Telefono, Celular, Ocupacion, ComisionServicio, ComisionProductos, Estado, IdEmpresa)
    VALUES
        (N'Laura Méndez',   N'809-555-2101', N'809-555-2101', N'Estilista',     30, 10, 1, @Destino),
        (N'Camila Rojas',   N'809-555-2102', N'809-555-2102', N'Estilista',     30, 10, 1, @Destino),
        (N'Valeria Soto',   N'809-555-2103', N'809-555-2103', N'Colorista',     30, 10, 1, @Destino),
        (N'Sofía Peralta',  N'809-555-2104', N'809-555-2104', N'Manicurista',   30, 10, 1, @Destino),
        (N'Andrea Cruz',    N'809-555-2105', N'809-555-2105', N'Recepcionista',  0,  0, 1, @Destino);
END

/* 5) Comisiones por área: 30% servicios, 10% productos (mismo esquema Dismerling) */
DECLARE @IdAreaServ INT, @IdAreaProd INT;
SELECT @IdAreaServ = IdArea FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'Servicios';
SELECT @IdAreaProd = IdArea FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'Productos / Belleza';

INSERT INTO dbo.EmpleadoAreaComision (IdEmpleado, IdArea, TipoComision, PorcientoComision, MontoComision, IdEmpresa)
SELECT e.IdEmpleados, @IdAreaServ, N'PORCIENTO', 30, 0, @Destino
FROM dbo.EmpleadosP e
WHERE e.IdEmpresa = @Destino
  AND e.Estado = 1
  AND ISNULL(e.ComisionServicio, 0) > 0
  AND @IdAreaServ IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.EmpleadoAreaComision x
      WHERE x.IdEmpleado = e.IdEmpleados AND x.IdArea = @IdAreaServ AND x.IdEmpresa = @Destino
  );

INSERT INTO dbo.EmpleadoAreaComision (IdEmpleado, IdArea, TipoComision, PorcientoComision, MontoComision, IdEmpresa)
SELECT e.IdEmpleados, @IdAreaProd, N'PORCIENTO', 10, 0, @Destino
FROM dbo.EmpleadosP e
WHERE e.IdEmpresa = @Destino
  AND e.Estado = 1
  AND ISNULL(e.ComisionProductos, 0) > 0
  AND @IdAreaProd IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.EmpleadoAreaComision x
      WHERE x.IdEmpleado = e.IdEmpleados AND x.IdArea = @IdAreaProd AND x.IdEmpresa = @Destino
  );

/* 6) Activar comisión en POS del demo */
UPDATE dbo.Parametros
SET Valor = N'true', Descripcion = N'Comisión por empleado activa (demo salón)'
WHERE IdEmpresa = @Destino AND Clave = N'COMISION_EMPLEADO';

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE IdEmpresa = @Destino AND Clave = N'COMISION_EMPLEADO')
    INSERT INTO dbo.Parametros (Tipo, Clave, Valor, IdEmpresa, Descripcion, Activo)
    VALUES (N'EMPRESA', N'COMISION_EMPLEADO', N'true', @Destino, N'Comisión por empleado activa (demo salón)', 1);

COMMIT TRAN;

SELECT N'Areas' AS Tabla, COUNT(*) AS Total FROM dbo.Areas WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Categorias', COUNT(*) FROM dbo.Categorias WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Productos', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Servicios', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @Destino AND EsServicio = 1
UNION ALL SELECT N'Empleados', COUNT(*) FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Comisiones', COUNT(*) FROM dbo.EmpleadoAreaComision WHERE IdEmpresa = @Destino;

SELECT IdEmpleados, Nombre, Ocupacion, ComisionServicio, ComisionProductos
FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino;

SELECT e.Nombre, a.Nombre AS Area, c.TipoComision, c.PorcientoComision AS Valor
FROM dbo.EmpleadoAreaComision c
INNER JOIN dbo.EmpleadosP e ON e.IdEmpleados = c.IdEmpleado
INNER JOIN dbo.Areas a ON a.IdArea = c.IdArea
WHERE c.IdEmpresa = @Destino
ORDER BY e.Nombre, a.Nombre;
