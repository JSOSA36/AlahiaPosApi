-- Copia operativa Sabor Urbano: AlahiaPos_Dev (62) -> AlahiaPos_Prod (73).
-- No toca módulos ni usuarios. Sin NCF tipo B.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @Origen INT = 62;
DECLARE @Destino INT = 73;
DECLARE @IdAlmacen INT, @IdProveedor INT, @IdUsuario INT;
DECLARE @IdCaja INT, @IdBanco INT;
DECLARE @SubCaja INT, @SubBanco INT;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @Destino AND NombreComercial = N'Sabor Urbano'
)
BEGIN
    RAISERROR(N'No existe Sabor Urbano IdEmpresa=73 en Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM AlahiaPos_Dev.dbo.Empresas
    WHERE IdEmpresa = @Origen AND NombreComercial = N'Sabor Urbano'
)
BEGIN
    RAISERROR(N'No existe Sabor Urbano IdEmpresa=62 en Dev.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

/* 1) Parámetros */
INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT @Destino, p.Tipo, p.CodigoPOS, p.Clave, p.Valor, p.Descripcion, GETDATE(), ISNULL(p.Activo, 1)
FROM AlahiaPos_Dev.dbo.Parametros p
WHERE p.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Parametros x
      WHERE x.IdEmpresa = @Destino AND x.Clave = p.Clave
  );

UPDATE dest
SET dest.Valor = src.Valor,
    dest.Activo = ISNULL(src.Activo, 1),
    dest.Descripcion = src.Descripcion
FROM dbo.Parametros dest
INNER JOIN AlahiaPos_Dev.dbo.Parametros src
    ON src.IdEmpresa = @Origen AND src.Clave = dest.Clave
WHERE dest.IdEmpresa = @Destino;

/* 2) Almacén */
SELECT TOP 1 @IdAlmacen = IdAlmacen
FROM dbo.Almacenes
WHERE IdEmpresa = @Destino AND Activo = 1
ORDER BY EsPrincipal DESC, IdAlmacen;

IF @IdAlmacen IS NULL
BEGIN
    INSERT INTO dbo.Almacenes (Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion)
    SELECT TOP 1 a.Nombre, a.Descripcion, @Destino, a.EsPrincipal, a.Activo, GETDATE()
    FROM AlahiaPos_Dev.dbo.Almacenes a
    WHERE a.IdEmpresa = @Origen
    ORDER BY a.EsPrincipal DESC, a.IdAlmacen;

    SET @IdAlmacen = SCOPE_IDENTITY();
END;

/* Productos.IdAlmacen todavía apunta a dbo.Almacens (tabla vieja EF). */
IF @IdAlmacen IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Almacens WHERE IdAlmacen = @IdAlmacen)
BEGIN
    SET IDENTITY_INSERT dbo.Almacens ON;
    INSERT INTO dbo.Almacens (IdAlmacen, Nombre, Descripcion, IsActivo, FechaInseccion, IdEmpresa)
    VALUES (@IdAlmacen, N'Principal', N'Almacen principal Sabor Urbano', 1, CAST(GETDATE() AS date), @Destino);
    SET IDENTITY_INSERT dbo.Almacens OFF;
END;

/* 3) Proveedor */
SELECT TOP 1 @IdProveedor = IdProveedor
FROM dbo.Proveedores
WHERE IdEmpresa = @Destino
ORDER BY IdProveedor;

IF @IdProveedor IS NULL
BEGIN
    INSERT INTO dbo.Proveedores
        (RNC, NombreComercial, Telefono, IsActivo, Direccion, Nota, Email, FechaInseccion, IdEmpresa)
    SELECT TOP 1
        pr.RNC, pr.NombreComercial, pr.Telefono, ISNULL(pr.IsActivo, 1),
        pr.Direccion, pr.Nota, pr.Email, GETDATE(), @Destino
    FROM AlahiaPos_Dev.dbo.Proveedores pr
    WHERE pr.IdEmpresa = @Origen
    ORDER BY pr.IdProveedor;

    SET @IdProveedor = SCOPE_IDENTITY();
END;

/* 4) Clientes */
INSERT INTO dbo.Clientes
    (CedulaRNC, NombreComercial, Telefono, Celular, Estado, Email, Direccion, Nota,
     LimiteCredito, FechaInseccion, TipoDocumento, CreditoFavor, IdEmpresa, FechaNacimiento,
     RegimenDgii, EsRegimenEspecial, TipoIdentificacionDgii)
SELECT
    c.CedulaRNC, c.NombreComercial, c.Telefono, c.Celular, ISNULL(c.Estado, 1),
    c.Email, c.Direccion, c.Nota, ISNULL(c.LimiteCredito, 0), GETDATE(),
    c.TipoDocumento, ISNULL(c.CreditoFavor, 0), @Destino, c.FechaNacimiento,
    c.RegimenDgii, ISNULL(c.EsRegimenEspecial, 0), c.TipoIdentificacionDgii
FROM AlahiaPos_Dev.dbo.Clientes c
WHERE c.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Clientes x
      WHERE x.IdEmpresa = @Destino
        AND LTRIM(RTRIM(x.NombreComercial)) = LTRIM(RTRIM(c.NombreComercial))
  );

/* 5) Categorías */
INSERT INTO dbo.Categorias
    (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
SELECT
    cat.Nombre, cat.Descripcion, cat.Tipo, ISNULL(cat.IsActiva, 1), GETDATE(),
    cat.Prioridad, @Destino, cat.ImagenPath, cat.TipoOperacion
FROM AlahiaPos_Dev.dbo.Categorias cat
WHERE cat.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Categorias x
      WHERE x.IdEmpresa = @Destino AND x.Nombre = cat.Nombre
  );

/* 6) Productos (map categoría por nombre) */
INSERT INTO dbo.Productos
(
    EsServicio, Nombre, Descripcion, Rentado, Disponibles, IdProveedor, Cantidad, Stock, PrecioVenta,
    IdUnidadMedida, TipoOperacion, IdCategoria, IdAlmacen, CodigoBarra, TipoProducto,
    TipoComportamiento, Precio1, Precio2, Precio3, PrecioDolar, PrecioExterno, PrecioCompra,
    Imagen1, Imagen2, Imagen3, Itbis, SeCompra, SeAlquila, SeVende, ControlarStock, IsActivo,
    Ganancia, IdCocina, FechaInseccion, IdEmpresa, DisponibleEnCitas, DuracionServicio,
    Descuento, PorcientoDescuento, PorcientoGanancia, IdArea, Nota, TasaItbis
)
SELECT
    ISNULL(p.EsServicio, 0), p.Nombre, p.Descripcion, ISNULL(p.Rentado, 0),
    ISNULL(p.Disponibles, 0), @IdProveedor, ISNULL(p.Cantidad, 0), ISNULL(p.Stock, 0),
    ISNULL(p.PrecioVenta, 0), p.IdUnidadMedida, ISNULL(NULLIF(p.TipoOperacion, N''), N'VENTA'),
    cD.IdCategoria, @IdAlmacen, p.CodigoBarra, p.TipoProducto, p.TipoComportamiento,
    ISNULL(p.Precio1, 0), ISNULL(p.Precio2, 0), ISNULL(p.Precio3, 0),
    ISNULL(p.PrecioDolar, 0), ISNULL(p.PrecioExterno, 0), ISNULL(p.PrecioCompra, 0),
    p.Imagen1, p.Imagen2, p.Imagen3, ISNULL(p.Itbis, 0), ISNULL(p.SeCompra, 0),
    ISNULL(p.SeAlquila, 0), ISNULL(p.SeVende, 1), ISNULL(p.ControlarStock, 0),
    ISNULL(p.IsActivo, 1), ISNULL(p.Ganancia, 0), 0, CAST(GETDATE() AS date), @Destino,
    ISNULL(p.DisponibleEnCitas, 0), ISNULL(p.DuracionServicio, 0),
    ISNULL(p.Descuento, 0), ISNULL(p.PorcientoDescuento, 0), ISNULL(p.PorcientoGanancia, 0),
    NULL, p.Nota, p.TasaItbis
FROM AlahiaPos_Dev.dbo.Productos p
LEFT JOIN AlahiaPos_Dev.dbo.Categorias cO
    ON cO.IdCategoria = p.IdCategoria AND cO.IdEmpresa = @Origen
LEFT JOIN dbo.Categorias cD
    ON cD.IdEmpresa = @Destino AND cD.Nombre = cO.Nombre
WHERE p.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Productos x
      WHERE x.IdEmpresa = @Destino
        AND (
            (NULLIF(LTRIM(RTRIM(x.CodigoBarra)), N'') IS NOT NULL
             AND LTRIM(RTRIM(x.CodigoBarra)) = LTRIM(RTRIM(p.CodigoBarra)))
            OR (ISNULL(x.CodigoBarra, N'') = N'' AND LTRIM(RTRIM(x.Nombre)) = LTRIM(RTRIM(p.Nombre)))
        )
  );

/* 7) Cuentas financieras */
SELECT @SubCaja  = IdTesoreriaSubtipoCuenta FROM dbo.TesoreriaSubtipoCuenta WHERE Codigo = N'CAJA_GENERAL';
SELECT @SubBanco = IdTesoreriaSubtipoCuenta FROM dbo.TesoreriaSubtipoCuenta WHERE Codigo = N'CTA_CORRIENTE';

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Caja General')
BEGIN
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, FechaSaldoInicial,
        PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion
    )
    SELECT
        @Destino, N'Caja General', N'CAJA', NULL, NULL, N'CAJA-73',
        0, ISNULL(cf.IdTesoreriaSubtipoCuenta, @SubCaja), cf.Color, cf.Icono, ISNULL(cf.Moneda, N'DOP'),
        1, ISNULL(cf.BalanceInicial, 25000), ISNULL(cf.BalanceInicial, 25000), CAST(GETDATE() AS DATE),
        ISNULL(cf.PermiteMovimientosManuales, 1), ISNULL(cf.PermiteSaldoNegativo, 0), GETDATE(),
        N'Caja demo Sabor Urbano (copia Dev)'
    FROM AlahiaPos_Dev.dbo.CuentaFinanciera cf
    WHERE cf.IdEmpresa = @Origen AND cf.Nombre = N'Caja General';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Banco Popular')
BEGIN
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, FechaSaldoInicial,
        PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion
    )
    SELECT
        @Destino, N'Banco Popular', N'BANCO', N'Popular', N'730000073', N'POPULAR-73',
        1, ISNULL(cf.IdTesoreriaSubtipoCuenta, @SubBanco), cf.Color, cf.Icono, ISNULL(cf.Moneda, N'DOP'),
        1, ISNULL(cf.BalanceInicial, 200000), ISNULL(cf.BalanceInicial, 200000), CAST(GETDATE() AS DATE),
        ISNULL(cf.PermiteMovimientosManuales, 1), ISNULL(cf.PermiteSaldoNegativo, 0), GETDATE(),
        N'Cuenta corriente demo Sabor Urbano (copia Dev)'
    FROM AlahiaPos_Dev.dbo.CuentaFinanciera cf
    WHERE cf.IdEmpresa = @Origen AND cf.Nombre = N'Banco Popular';
END;

SELECT @IdCaja  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Caja General';
SELECT @IdBanco = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Banco Popular';

/* 8) Métodos de pago (mismos de Dev: EFECTIVO + POPULAR) */
IF @IdCaja IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'EFECTIVO')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'EFECTIVO', @IdCaja, 1);

IF @IdBanco IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'POPULAR')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'POPULAR', @IdBanco, 1);

/* 9) Secuencias operativas (sin NCF tipo B) */
INSERT INTO dbo.SecuenciaDocumentos
    (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
SELECT
    s.SecuenciaInicial, 0, s.Prefijo, s.IdTipoDocumento, CAST(GETDATE() AS date), @Destino
FROM AlahiaPos_Dev.dbo.SecuenciaDocumentos s
WHERE s.IdEmpresa = @Origen
  AND ISNULL(s.Prefijo, N'') NOT LIKE N'B0%'
  AND ISNULL(s.Prefijo, N'') NOT LIKE N'B1%'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SecuenciaDocumentos x
      WHERE x.IdEmpresa = @Destino AND x.IdTipoDocumento = s.IdTipoDocumento
  );

/* 10) Centro de producción (KDS) */
IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionEstacion WHERE IdEmpresa = @Destino AND Codigo = N'GENERAL')
BEGIN
    INSERT INTO dbo.ProduccionEstacion (IdEmpresa, Codigo, Nombre, EsDespacho, Activa, OrdenVisual)
    VALUES (@Destino, N'GENERAL', N'General', 0, 1, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionConfiguracionEmpresa WHERE IdEmpresa = @Destino)
BEGIN
    INSERT INTO dbo.ProduccionConfiguracionEmpresa
        (IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
         TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
         ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion)
    SELECT TOP 1
        @Destino, c.Activo, c.UsarEstaciones, c.UsarEstadosPorItem, c.SonidoActivo,
        c.TiempoAdvertenciaSegDefault, c.TiempoCriticoSegDefault, c.PermitirCompletarDesdeEstacion,
        c.ModoOscuroDefault, c.MostrarNombreCliente, c.MostrarUsuarioSolicita, GETDATE()
    FROM AlahiaPos_Dev.dbo.ProduccionConfiguracionEmpresa c
    WHERE c.IdEmpresa = @Origen;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionConfiguracionEmpresa WHERE IdEmpresa = @Destino)
BEGIN
    INSERT INTO dbo.ProduccionConfiguracionEmpresa
        (IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
         TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
         ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion)
    VALUES (@Destino, 1, 0, 0, 1, 600, 900, 1, 1, 1, 1, GETDATE());
END;

UPDATE c
SET IdEstacionPredeterminada = s.IdEstacion, FechaActualizacion = GETDATE()
FROM dbo.ProduccionConfiguracionEmpresa c
INNER JOIN dbo.ProduccionEstacion s ON s.IdEmpresa = c.IdEmpresa AND s.Codigo = N'GENERAL'
WHERE c.IdEmpresa = @Destino AND c.IdEstacionPredeterminada IS NULL;

/* 11) Recetas de manufactura (Bizcocho / Pastelito) */
SELECT TOP 1 @IdUsuario = IdUsuario FROM dbo.Usuarios WHERE IdEmpresa = @Destino ORDER BY IdUsuario;

INSERT INTO dbo.RecetaProduccion
    (IdEmpresa, IdProductoTerminado, Nombre, RendimientoBase, IdUnidadMedida, Activa, Observacion, FechaCreacion, IdUsuario)
SELECT
    @Destino, pt.IdProducto, r.Nombre, r.RendimientoBase, r.IdUnidadMedida, r.Activa, r.Observacion, GETDATE(), @IdUsuario
FROM AlahiaPos_Dev.dbo.RecetaProduccion r
INNER JOIN AlahiaPos_Dev.dbo.Productos po ON po.IdProducto = r.IdProductoTerminado
INNER JOIN dbo.Productos pt ON pt.IdEmpresa = @Destino AND pt.CodigoBarra = po.CodigoBarra
WHERE r.IdEmpresa = @Origen
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RecetaProduccion x
      WHERE x.IdEmpresa = @Destino AND x.Nombre = r.Nombre
  );

INSERT INTO dbo.RecetaProduccionItem (IdReceta, IdProducto, Cantidad, IdUnidadMedida, Orden, Activo)
SELECT
    rd.IdReceta, pin.IdProducto, i.Cantidad, i.IdUnidadMedida, i.Orden, ISNULL(i.Activo, 1)
FROM AlahiaPos_Dev.dbo.RecetaProduccionItem i
INNER JOIN AlahiaPos_Dev.dbo.RecetaProduccion r ON r.IdReceta = i.IdReceta AND r.IdEmpresa = @Origen
INNER JOIN dbo.RecetaProduccion rd ON rd.IdEmpresa = @Destino AND rd.Nombre = r.Nombre
INNER JOIN AlahiaPos_Dev.dbo.Productos po ON po.IdProducto = i.IdProducto
INNER JOIN dbo.Productos pin ON pin.IdEmpresa = @Destino AND pin.CodigoBarra = po.CodigoBarra
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RecetaProduccionItem x
    WHERE x.IdReceta = rd.IdReceta AND x.IdProducto = pin.IdProducto
);

COMMIT TRAN;

SELECT N'Categorias' T, COUNT(*) C FROM dbo.Categorias WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Productos', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Clientes', COUNT(*) FROM dbo.Clientes WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Almacenes', COUNT(*) FROM dbo.Almacenes WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Proveedores', COUNT(*) FROM dbo.Proveedores WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Cuentas', COUNT(*) FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino
UNION ALL SELECT N'MetodosPago', COUNT(*) FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Secuencias', COUNT(*) FROM dbo.SecuenciaDocumentos WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Recetas', COUNT(*) FROM dbo.RecetaProduccion WHERE IdEmpresa = @Destino
UNION ALL SELECT N'Areas', COUNT(*) FROM dbo.Areas WHERE IdEmpresa = @Destino;

SELECT Nombre, PrecioVenta, CodigoBarra FROM dbo.Productos WHERE IdEmpresa = @Destino ORDER BY Nombre;
SELECT Nombre, TipoCuenta, Banco, SaldoDisponible FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino;
SELECT MetodoPago FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino;
GO
