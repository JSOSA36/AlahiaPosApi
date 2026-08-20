/* Demo carwash IdEmpresa=70 alineado a TotalClean (58). */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Origen INT = 58;
DECLARE @Destino INT = 70;
DECLARE @IdProveedorDest INT;
DECLARE @IdLineaWash INT, @IdLineaCafe INT;
DECLARE @IdAreaWash INT, @IdAreaCafe INT;
DECLARE @IdCaja INT, @IdBhd INT, @IdPop INT, @IdRes INT, @IdTar INT;

SELECT TOP 1 @IdProveedorDest = IdProveedor
FROM dbo.Productos
WHERE IdEmpresa = @Destino AND IdProveedor IS NOT NULL;

BEGIN TRAN;

/* 1) Parámetros faltantes (FE apagada; comisión ON para demo) */
INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT @Destino, p.Tipo, p.CodigoPOS, p.Clave,
       CASE WHEN p.Clave = N'FACTURACION_ELECTRONICA' THEN N'false' ELSE p.Valor END,
       p.Descripcion, GETDATE(), 1
FROM dbo.Parametros p
WHERE p.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Parametros x
      WHERE x.IdEmpresa = @Destino AND x.Clave = p.Clave
  );

UPDATE dbo.Parametros SET Valor = N'true'
WHERE IdEmpresa = @Destino AND Clave = N'COMISION_EMPLEADO';

UPDATE dbo.Parametros SET Valor = N'false'
WHERE IdEmpresa = @Destino AND Clave = N'FACTURACION_ELECTRONICA';

/* 2) Líneas de negocio */
IF NOT EXISTS (SELECT 1 FROM dbo.AreaNegocio WHERE IdEmpresa = @Destino AND Nombre = N'Carwash')
    INSERT INTO dbo.AreaNegocio (Nombre, Descripcion, Activo, IdEmpresa, FechaCreacion)
    VALUES (N'Carwash', N'Servicios de lavado y detailing', 1, @Destino, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.AreaNegocio WHERE IdEmpresa = @Destino AND Nombre = N'Cafeteria')
    INSERT INTO dbo.AreaNegocio (Nombre, Descripcion, Activo, IdEmpresa, FechaCreacion)
    VALUES (N'Cafeteria', N'Venta de alimentos y bebidas', 1, @Destino, GETDATE());

SELECT @IdLineaWash = IdAreaNegocio FROM dbo.AreaNegocio WHERE IdEmpresa = @Destino AND Nombre = N'Carwash';
SELECT @IdLineaCafe = IdAreaNegocio FROM dbo.AreaNegocio WHERE IdEmpresa = @Destino AND Nombre = N'Cafeteria';

/* 3) Áreas */
IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'CARS WASH')
    INSERT INTO dbo.Areas (IdEmpresa, Nombre, IdAreaNegocio, IsActivo, FechaInseccion)
    VALUES (@Destino, N'CARS WASH', @IdLineaWash, 1, CAST(GETDATE() AS DATE));

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'CAFETERIA')
    INSERT INTO dbo.Areas (IdEmpresa, Nombre, IdAreaNegocio, IsActivo, FechaInseccion)
    VALUES (@Destino, N'CAFETERIA', @IdLineaCafe, 1, CAST(GETDATE() AS DATE));

SELECT @IdAreaWash = IdArea FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'CARS WASH';
SELECT @IdAreaCafe = IdArea FROM dbo.Areas WHERE IdEmpresa = @Destino AND Nombre = N'CAFETERIA';

/* 4) Categorías con foto de TotalClean */
IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE IdEmpresa = @Destino AND Nombre = N'CarWash')
    INSERT INTO dbo.Categorias (IdEmpresa, Nombre, Tipo, TipoOperacion, Prioridad, IsActiva, FechaInseccion, ImagenPath)
    SELECT @Destino, N'CarWash', N'Servicio', N'VENTA', 1, 1, CAST(GETDATE() AS DATE), ImagenPath
    FROM dbo.Categorias WHERE IdEmpresa = @Origen AND IdCategoria = 274;

IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE IdEmpresa = @Destino AND Nombre = N'Cafeteria')
    INSERT INTO dbo.Categorias (IdEmpresa, Nombre, Tipo, TipoOperacion, Prioridad, IsActiva, FechaInseccion, ImagenPath)
    SELECT @Destino, N'Cafeteria', N'Producto', N'VENTA', 2, 1, CAST(GETDATE() AS DATE), ImagenPath
    FROM dbo.Categorias WHERE IdEmpresa = @Origen AND IdCategoria = 83;

/* 5) Reubicar los 6 ítems que ya tenía el demo */
UPDATE p SET
    p.IdArea = @IdAreaWash,
    p.IdCategoria = c.IdCategoria,
    p.EsServicio = 1,
    p.TipoComportamiento = N'Servicio',
    p.TipoProducto = N'Servicio',
    p.DisponibleEnCitas = 1
FROM dbo.Productos p
INNER JOIN dbo.Categorias c ON c.IdEmpresa = @Destino AND c.Nombre = N'CarWash'
WHERE p.IdEmpresa = @Destino
  AND p.Nombre IN (N'Lavado básico', N'Lavado full detail', N'Encerado', N'Motor', N'Aspirado interior');

UPDATE p SET
    p.IdArea = @IdAreaCafe,
    p.IdCategoria = c.IdCategoria,
    p.EsServicio = 0,
    p.TipoComportamiento = N'Inventario',
    p.TipoProducto = N'Producto'
FROM dbo.Productos p
INNER JOIN dbo.Categorias c ON c.IdEmpresa = @Destino AND c.Nombre = N'Cafeteria'
WHERE p.IdEmpresa = @Destino
  AND p.Nombre = N'Aromatizante';

/* 6) Copiar catálogo CarWash + Cafeteria de TotalClean (sin préstamos ni basura) */
INSERT INTO dbo.Productos (
    Nombre, Descripcion, Rentado, Cantidad, Stock, PrecioVenta, Descuento,
    Precio1, Precio2, Precio3, PorcientoDescuento, PorcientoGanancia, PrecioCompra,
    Itbis, SeCompra, SeAlquila, SeVende, ControlarStock, IsActivo, Ganancia,
    FechaInseccion, PrecioDolar, PrecioExterno, IdEmpresa, IdAlmacen, IdProveedor,
    IdCategoria, IdArea, EsServicio, DuracionServicio, DisponibleEnCitas,
    TipoOperacion, TipoComportamiento, TipoProducto, Imagen1, Imagen2, Imagen3
)
SELECT
    p.Nombre, p.Descripcion,
    CAST(0 AS BIT), CAST(0 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.PrecioVenta, 0), CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.Precio1, 0), ISNULL(p.Precio2, 0), ISNULL(p.Precio3, 0),
    CAST(0 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)),
    ISNULL(p.Itbis, 0), CAST(0 AS BIT), CAST(0 AS BIT), CAST(1 AS BIT),
    CAST(0 AS BIT), CAST(1 AS BIT), CAST(0 AS DECIMAL(18,2)),
    CAST(GETDATE() AS DATE), CAST(0 AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2)),
    @Destino, NULL, @IdProveedorDest,
    catD.IdCategoria,
    CASE WHEN catO.Nombre = N'CarWash' THEN @IdAreaWash ELSE @IdAreaCafe END,
    ISNULL(p.EsServicio, 0),
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN ISNULL(NULLIF(p.DuracionServicio, 0), 30) ELSE 0 END,
    CAST(0 AS BIT),
    N'VENTA',
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN N'Servicio' ELSE N'Inventario' END,
    CASE WHEN ISNULL(p.EsServicio, 0) = 1 THEN N'Servicio' ELSE N'Producto' END,
    p.Imagen1, p.Imagen2, p.Imagen3
FROM dbo.Productos p
INNER JOIN dbo.Categorias catO ON catO.IdCategoria = p.IdCategoria AND catO.IdEmpresa = @Origen
INNER JOIN dbo.Categorias catD ON catD.IdEmpresa = @Destino
    AND (
        (catO.IdCategoria = 274 AND catD.Nombre = N'CarWash')
        OR (catO.IdCategoria = 83 AND catD.Nombre = N'Cafeteria')
    )
WHERE p.IdEmpresa = @Origen
  AND ISNULL(p.IsActivo, 1) = 1
  AND p.IdCategoria IN (274, 83)
  AND p.Nombre NOT LIKE N'PRESTAMO%'
  AND p.Nombre NOT LIKE N'MONEDAS%'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Productos d
      WHERE d.IdEmpresa = @Destino
        AND LTRIM(RTRIM(LOWER(d.Nombre))) = LTRIM(RTRIM(LOWER(p.Nombre)))
  );

/* 7) Quitar categorías genéricas */
DELETE FROM dbo.Categorias
WHERE IdEmpresa = @Destino
  AND Nombre IN (N'Productos', N'Servicios')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Productos p
      WHERE p.IdEmpresa = @Destino AND p.IdCategoria = Categorias.IdCategoria
  );

/* 8) Empleados demo + comisión 24% en CARS WASH (mismo esquema TotalClean) */
IF NOT EXISTS (SELECT 1 FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino)
BEGIN
    INSERT INTO dbo.EmpleadosP (Nombre, Telefono, Celular, Ocupacion, ComisionServicio, ComisionProductos, Estado, IdEmpresa)
    VALUES
        (N'Luis Pérez',    N'809-555-3101', N'809-555-3101', N'Lavador', 24, 0, 1, @Destino),
        (N'Miguel Santos', N'809-555-3102', N'809-555-3102', N'Lavador', 24, 0, 1, @Destino),
        (N'José Ramírez',  N'809-555-3103', N'809-555-3103', N'Lavador', 24, 0, 1, @Destino),
        (N'Pedro Núñez',   N'809-555-3104', N'809-555-3104', N'Lavador', 24, 0, 1, @Destino),
        (N'Ana Gómez',     N'809-555-3105', N'809-555-3105', N'Caja',     0, 0, 1, @Destino);
END

INSERT INTO dbo.EmpleadoAreaComision (IdEmpleado, IdArea, TipoComision, PorcientoComision, MontoComision, IdEmpresa)
SELECT e.IdEmpleados, @IdAreaWash, N'PORCIENTO', 24, 0, @Destino
FROM dbo.EmpleadosP e
WHERE e.IdEmpresa = @Destino
  AND e.Estado = 1
  AND ISNULL(e.ComisionServicio, 0) > 0
  AND @IdAreaWash IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.EmpleadoAreaComision x
      WHERE x.IdEmpleado = e.IdEmpleados AND x.IdArea = @IdAreaWash AND x.IdEmpresa = @Destino
  );

/* 9) Cuentas y métodos de pago */
IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Efectivo')
    INSERT INTO dbo.CuentaFinanciera (IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo, EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda, Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion)
    VALUES (@Destino, N'Efectivo', N'CAJA', NULL, NULL, N'CAJA-70', 1, 3, N'#16a34a', N'cash-outline', N'DOP', 1, 0, 0, 1, 0, GETDATE(), N'Caja carwash (demo)');

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'BHD')
    INSERT INTO dbo.CuentaFinanciera (IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo, EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda, Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion)
    VALUES (@Destino, N'BHD', N'BANCO', N'BHD', N'12345678902', N'BHD-70', 0, 4, N'#f59e0b', N'wallet-outline', N'DOP', 1, 0, 0, 1, 0, GETDATE(), N'Transferencia BHD (demo)');

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Popular')
    INSERT INTO dbo.CuentaFinanciera (IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo, EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda, Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion)
    VALUES (@Destino, N'Popular', N'BANCO', N'Popular', N'730123457', N'POPULAR-70', 0, 4, N'#dc2626', N'business-outline', N'DOP', 1, 0, 0, 1, 0, GETDATE(), N'Transferencia Popular (demo)');

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Reservas')
    INSERT INTO dbo.CuentaFinanciera (IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo, EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda, Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion)
    VALUES (@Destino, N'Reservas', N'BANCO', N'Reservas', N'960123457', N'RESERVAS-70', 0, 4, N'#059669', N'business-outline', N'DOP', 1, 0, 0, 1, 0, GETDATE(), N'Transferencia Banreservas (demo)');

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Tarjeta')
    INSERT INTO dbo.CuentaFinanciera (IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo, EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda, Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion)
    VALUES (@Destino, N'Tarjeta', N'TARJETA', NULL, NULL, N'TARJETA-70', 0, 6, N'#2563eb', N'card-outline', N'DOP', 1, 0, 0, 1, 0, GETDATE(), N'Pago con tarjetas (demo)');

SELECT @IdCaja = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Efectivo';
SELECT @IdBhd  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'BHD';
SELECT @IdPop  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Popular';
SELECT @IdRes  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Reservas';
SELECT @IdTar  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Tarjeta';

IF @IdCaja IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'EFECTIVO')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo) VALUES (@Destino, N'EFECTIVO', @IdCaja, 1);
IF @IdBhd IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'BHD')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo) VALUES (@Destino, N'BHD', @IdBhd, 1);
IF @IdPop IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'POPULAR')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo) VALUES (@Destino, N'POPULAR', @IdPop, 1);
IF @IdRes IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'BANRESERVAS')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo) VALUES (@Destino, N'BANRESERVAS', @IdRes, 1);
IF @IdTar IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'TARJETA')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo) VALUES (@Destino, N'TARJETA', @IdTar, 1);

/* 10) Al Portador + secuencia Factura en espera */
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE IdEmpresa = @Destino AND NombreComercial = N'Al Portador')
    INSERT INTO dbo.Clientes (NombreComercial, Estado, LimiteCredito, CreditoFavor, IdEmpresa, FechaInseccion, EsRegimenEspecial)
    VALUES (N'Al Portador', 1, 0, 0, @Destino, GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.SecuenciaDocumentos WHERE IdEmpresa = @Destino AND IdTipoDocumento = 13)
    INSERT INTO dbo.SecuenciaDocumentos (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
    VALUES (0, 0, N'ESP-0000', 13, CAST(GETDATE() AS DATE), @Destino);

COMMIT TRAN;

SELECT N'Params' T, COUNT(*) C FROM dbo.Parametros WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Lineas', COUNT(*) FROM dbo.AreaNegocio WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Areas', COUNT(*) FROM dbo.Areas WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Categorias', COUNT(*) FROM dbo.Categorias WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Productos', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Empleados', COUNT(*) FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Comisiones', COUNT(*) FROM dbo.EmpleadoAreaComision WHERE IdEmpresa = @Destino
UNION ALL SELECT N'MetodosPago', COUNT(*) FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino;

SELECT Nombre, Ocupacion, ComisionServicio FROM dbo.EmpleadosP WHERE IdEmpresa = @Destino;
SELECT m.MetodoPago, c.Nombre Cuenta FROM dbo.MetodoPagoCuenta m JOIN dbo.CuentaFinanciera c ON c.IdCuentaFinanciera = m.IdCuentaFinanciera WHERE m.IdEmpresa = @Destino;
SELECT Clave, Valor FROM dbo.Parametros WHERE IdEmpresa = @Destino AND Clave IN (N'COMISION_EMPLEADO', N'FACTURACION_ELECTRONICA', N'MODALIDAD_POS', N'USAS_ORDENES');
