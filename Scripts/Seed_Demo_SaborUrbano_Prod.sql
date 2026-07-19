-- ============================================================
-- Seed Demo Comida Rápida — AlahiaPos_Prod
-- Empresa: Sabor Urbano
-- Usuario: admin@saborurbano.demo / DemoFood2026!
-- Autorizado: pasar demo fastfood a Prod (2026-07-17)
-- ============================================================
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END

DECLARE @IdEmpresa INT;
DECLARE @IdPerfil INT;
DECLARE @IdEmpleado INT;
DECLARE @IdUsuario INT;
DECLARE @IdAlmacen INT;
DECLARE @IdProveedor INT;
DECLARE @IdCatHamburguesas INT;
DECLARE @IdCatBurritos INT;
DECLARE @IdCatYaroas INT;
DECLARE @IdCatComplementos INT;
DECLARE @PasswordHash NVARCHAR(128) =
    N'02d87bd6b67f1c51e5a0c51294c1df1a4e41701a0b5ec6fbbba401edb8f55af0'; -- DemoFood2026!
DECLARE @UserName NVARCHAR(150) = N'admin@saborurbano.demo';
DECLARE @Correo NVARCHAR(150) = N'admin@saborurbano.demo';
DECLARE @Logo NVARCHAR(300) = N'https://alahiaupdate.alahiapos.com/demo-sabor-urbano-logo.jpg';
DECLARE @BaseImg NVARCHAR(100) = N'https://alahiaupdate.alahiapos.com/';
DECLARE @IdPlan INT = (SELECT TOP 1 IdPlan FROM dbo.PlanesCloud ORDER BY IdPlan DESC);

-- 1) Empresa (sin EsEmpresaSistema — no existe en Prod)
SELECT @IdEmpresa = IdEmpresa
FROM dbo.Empresas
WHERE NombreComercial = N'Sabor Urbano'
   OR CorreElectronico = @Correo;

IF @IdEmpresa IS NULL
BEGIN
    INSERT INTO dbo.Empresas
    (
        NombreComercial, RNC, Direccion, Telefono, CorreElectronico, Logo,
        Estado, FechaTerminacion, GuidPublico, UsaSSL, PoliticasAceptadas,
        PagadoServicio, EstadoServicio, IdPlan,
        FechaInicioPlan, FechaVencimientoPlan, EstadoPlan,
        LimiteUsuario, PrimaryColor, SecondaryColor, TertiaryColor, titleColor,
        FechaInseccion
    )
    VALUES
    (
        N'Sabor Urbano', N'132458796', N'Av. Winston Churchill, Santo Domingo, RD',
        N'809-555-0180', @Correo, @Logo,
        1, DATEADD(YEAR, 5, GETDATE()), NEWID(), 1, 1,
        1, N'ACTIVA', ISNULL(@IdPlan, 1),
        GETDATE(), DATEADD(YEAR, 5, GETDATE()), N'ACTIVO',
        10, N'#ea580c', N'#1c1917', N'#f97316', N'#ffffff',
        GETDATE()
    );
    SET @IdEmpresa = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa Sabor Urbano creada Id=', @IdEmpresa);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET NombreComercial = N'Sabor Urbano',
        Logo = @Logo,
        Estado = 1,
        PagadoServicio = 1,
        EstadoServicio = N'ACTIVA',
        PoliticasAceptadas = 1,
        FechaTerminacion = DATEADD(YEAR, 5, GETDATE()),
        IdPlan = ISNULL(IdPlan, @IdPlan),
        PrimaryColor = N'#ea580c',
        SecondaryColor = N'#1c1917',
        TertiaryColor = N'#f97316',
        titleColor = N'#ffffff',
        CorreElectronico = @Correo,
        Telefono = N'809-555-0180',
        Direccion = N'Av. Winston Churchill, Santo Domingo, RD'
    WHERE IdEmpresa = @IdEmpresa;
    PRINT CONCAT('Empresa Sabor Urbano actualizada Id=', @IdEmpresa);
END

-- 2) Perfil
SELECT @IdPerfil = IdPerfil FROM dbo.Perfiles WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador';
IF @IdPerfil IS NULL
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (@IdEmpresa, N'Administrador', N'Administrador Sabor Urbano', 1);
    SET @IdPerfil = SCOPE_IDENTITY();
END

-- 3) Empleado
SELECT TOP 1 @IdEmpleado = IdEmpleados
FROM dbo.Empleados
WHERE IdEmpresa = @IdEmpresa AND (Correo = @Correo OR Nombre = N'Admin Sabor Urbano');

IF @IdEmpleado IS NULL
BEGIN
    INSERT INTO dbo.Empleados
    (
        Nombre, Posicion, Estado, IdEmpresa,
        FechaInseccion, FechaIngreso, FechaNacimiento,
        ComisionServicio, ComisionProductos, Salario, Correo
    )
    VALUES
    (
        N'Admin Sabor Urbano', N'Administrador', 1, @IdEmpresa,
        CAST(GETDATE() AS date), GETDATE(), '1990-01-01',
        0, 0, 0, @Correo
    );
    SET @IdEmpleado = SCOPE_IDENTITY();
END

-- 4) Usuario
SELECT @IdUsuario = IdUsuario
FROM dbo.Usuarios
WHERE IdEmpresa = @IdEmpresa AND (UserName = @UserName OR Correo = @Correo);

IF @IdUsuario IS NULL
BEGIN
    INSERT INTO dbo.Usuarios
    (
        IdEmpresa, UserName, Correo, PasswordHash, Estado, IdPerfil, IdEmpleado,
        FechaCreacion,
        PuedeEliminarOrden, PuedeEliminarItemCarrito, PuedeDisminuirCantidadCarrito, PuedeEditarPrecioCarrito
    )
    VALUES
    (
        @IdEmpresa, @UserName, @Correo, @PasswordHash, 1, @IdPerfil, @IdEmpleado,
        GETDATE(), 1, 1, 1, 1
    );
    SET @IdUsuario = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET PasswordHash = @PasswordHash, Estado = 1, IdPerfil = @IdPerfil,
        IdEmpleado = ISNULL(IdEmpleado, @IdEmpleado), Correo = @Correo, UserName = @UserName
    WHERE IdUsuario = @IdUsuario;
END

-- 5) Modulos + PerfilRoles
;WITH Mods AS (
    SELECT Id AS ModuloId FROM dbo.Modulos
    WHERE Codigo IN (
        N'POS', N'ORDENES', N'PRODUCTOS', N'CATEGORIAS', N'CLIENTES',
        N'DASHBOARD', N'PARAMETROS', N'EMPRESA', N'USUARIOS', N'PERFILES',
        N'LISTADO_CAJA', N'MOVIMIENTO_CAJA', N'CIERRE_CAJA', N'FACTURAS',
        N'CENTRO_PRODUCCION', N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR',
        N'PRODUCCION_PRIORIDAD', N'PRODUCCION_CONFIG'
    )
)
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.ModuloId, 1, GETDATE()
FROM Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.ModuloId
);

UPDATE em SET Activo = 1, FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos mo ON mo.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND mo.Codigo IN (
        N'POS', N'ORDENES', N'PRODUCTOS', N'CATEGORIAS', N'CLIENTES',
        N'DASHBOARD', N'PARAMETROS', N'EMPRESA', N'USUARIOS', N'PERFILES',
        N'LISTADO_CAJA', N'MOVIMIENTO_CAJA', N'CIERRE_CAJA', N'FACTURAS',
        N'CENTRO_PRODUCCION', N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR',
        N'PRODUCCION_PRIORIDAD', N'PRODUCCION_CONFIG'
  );

;WITH Mods AS (
    SELECT Id AS ModuloId FROM dbo.Modulos
    WHERE Codigo IN (
        N'POS', N'ORDENES', N'PRODUCTOS', N'CATEGORIAS', N'CLIENTES',
        N'DASHBOARD', N'PARAMETROS', N'EMPRESA', N'USUARIOS', N'PERFILES',
        N'LISTADO_CAJA', N'MOVIMIENTO_CAJA', N'CIERRE_CAJA', N'FACTURAS',
        N'CENTRO_PRODUCCION', N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR',
        N'PRODUCCION_PRIORIDAD', N'PRODUCCION_CONFIG'
    )
)
INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfil, m.ModuloId, 1, GETDATE(), @IdEmpresa
FROM Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = @IdPerfil AND pr.IdModulo = m.ModuloId AND pr.IdEmpresa = @IdEmpresa
);

UPDATE pr SET Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Modulos mo ON mo.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa AND pr.IdPerfil = @IdPerfil;

-- 6) Parametros
DECLARE @Params TABLE (Clave NVARCHAR(100), Valor NVARCHAR(50), Descripcion NVARCHAR(200));
INSERT INTO @Params VALUES
(N'USAS_ORDENES', N'true', N'Habilita documento Orden en POS'),
(N'ESTATUS_ORDENES', N'true', N'Muestra estados de produccion en listado de ordenes'),
(N'FACTURAR_CON_ITBIS', N'true', N'Facturar con ITBIS'),
(N'PRECIO_INCLUYE_ITBIS', N'true', N'Precio incluye ITBIS'),
(N'IMPRIMIR_ORDEN', N'true', N'Imprimir orden'),
(N'CANTIDAD_COPIAS_ORDEN', N'1', N'Copias de orden'),
(N'COMISION_EMPLEADO', N'false', N'Sin comision en demo');

INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT @IdEmpresa, N'EMPRESA', NULL, p.Clave, p.Valor, p.Descripcion, GETDATE(), 1
FROM @Params p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Parametros x WHERE x.IdEmpresa = @IdEmpresa AND x.Clave = p.Clave);

UPDATE x SET Valor = p.Valor, Activo = 1, Descripcion = p.Descripcion
FROM dbo.Parametros x
INNER JOIN @Params p ON p.Clave = x.Clave
WHERE x.IdEmpresa = @IdEmpresa;

-- 7) Almacen + Proveedor
SELECT TOP 1 @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE IdEmpresa = @IdEmpresa AND EsPrincipal = 1;
IF @IdAlmacen IS NULL
BEGIN
    INSERT INTO dbo.Almacenes (Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion)
    VALUES (N'Principal', N'Almacen principal Sabor Urbano', @IdEmpresa, 1, 1, GETDATE());
    SET @IdAlmacen = SCOPE_IDENTITY();
END

SELECT TOP 1 @IdProveedor = IdProveedor FROM dbo.Proveedores WHERE IdEmpresa = @IdEmpresa;
IF @IdProveedor IS NULL
BEGIN
    INSERT INTO dbo.Proveedores
        (RNC, NombreComercial, Telefono, IsActivo, Direccion, Nota, Email, FechaInseccion, IdEmpresa)
    VALUES (N'000000000', N'Proveedor General', N'809-000-0000', 1, N'Santo Domingo', N'', N'', GETDATE(), @IdEmpresa);
    SET @IdProveedor = SCOPE_IDENTITY();
END

-- 8) Cliente
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE IdEmpresa = @IdEmpresa AND NombreComercial = N'Al Portador')
BEGIN
    INSERT INTO dbo.Clientes (NombreComercial, Estado, LimiteCredito, FechaInseccion, IdEmpresa)
    VALUES (N'Al Portador', 1, 0, GETDATE(), @IdEmpresa);
END

-- 9) Categorias
SELECT @IdCatHamburguesas = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Hamburguesas';
IF @IdCatHamburguesas IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
    VALUES (N'Hamburguesas', N'Hamburguesas artesanales', N'VENTA', 1, GETDATE(), 1, @IdEmpresa,
            @BaseImg + N'demo-hamburguesa-clasica.jpg', N'VENTA');
    SET @IdCatHamburguesas = SCOPE_IDENTITY();
END

SELECT @IdCatBurritos = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Burritos';
IF @IdCatBurritos IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
    VALUES (N'Burritos', N'Burritos rellenos', N'VENTA', 1, GETDATE(), 2, @IdEmpresa,
            @BaseImg + N'demo-burrito-carne.jpg', N'VENTA');
    SET @IdCatBurritos = SCOPE_IDENTITY();
END

SELECT @IdCatYaroas = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Yaroas';
IF @IdCatYaroas IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
    VALUES (N'Yaroas', N'Yaroas dominicanas', N'VENTA', 1, GETDATE(), 3, @IdEmpresa,
            @BaseImg + N'demo-yaroa-mixta.jpg', N'VENTA');
    SET @IdCatYaroas = SCOPE_IDENTITY();
END

SELECT @IdCatComplementos = IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Complementos';
IF @IdCatComplementos IS NULL
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
    VALUES (N'Complementos', N'Papas y bebidas', N'VENTA', 1, GETDATE(), 4, @IdEmpresa,
            @BaseImg + N'demo-papas-fritas.jpg', N'VENTA');
    SET @IdCatComplementos = SCOPE_IDENTITY();
END

-- 10) Productos
DECLARE @Prods TABLE (
    Nombre NVARCHAR(120), Descripcion NVARCHAR(200), IdCategoria INT,
    Precio DECIMAL(18,2), Imagen NVARCHAR(300), Codigo NVARCHAR(30)
);
INSERT INTO @Prods VALUES
(N'Hamburguesa Clasica', N'Carne, queso, lechuga y tomate', @IdCatHamburguesas, 295.00, @BaseImg + N'demo-hamburguesa-clasica.jpg', N'SU-H001'),
(N'Hamburguesa de Pollo', N'Pechuga crispy con mayo', @IdCatHamburguesas, 310.00, @BaseImg + N'demo-hamburguesa-pollo.jpg', N'SU-H002'),
(N'Hamburguesa Doble Bacon', N'Doble carne y bacon', @IdCatHamburguesas, 395.00, @BaseImg + N'demo-hamburguesa-doble.jpg', N'SU-H003'),
(N'Burrito de Res', N'Arroz, frijoles y carne de res', @IdCatBurritos, 280.00, @BaseImg + N'demo-burrito-carne.jpg', N'SU-B001'),
(N'Burrito de Pollo', N'Pollo, aguacate y salsa', @IdCatBurritos, 275.00, @BaseImg + N'demo-burrito-pollo.jpg', N'SU-B002'),
(N'Yaroa Mixta', N'Papas, carne y queso', @IdCatYaroas, 350.00, @BaseImg + N'demo-yaroa-mixta.jpg', N'SU-Y001'),
(N'Yaroa de Pollo', N'Papas, pollo y queso', @IdCatYaroas, 340.00, @BaseImg + N'demo-yaroa-pollo.jpg', N'SU-Y002'),
(N'Papas Fritas', N'Porcion mediana', @IdCatComplementos, 120.00, @BaseImg + N'demo-papas-fritas.jpg', N'SU-C001'),
(N'Refresco', N'Bebida fria 16 oz', @IdCatComplementos, 80.00, @BaseImg + N'demo-refresco.jpg', N'SU-C002');

INSERT INTO dbo.Productos
(
    EsServicio, Nombre, Descripcion, Rentado, Disponibles, IdProveedor, Cantidad, Stock, PrecioVenta,
    IdUnidadMedida, TipoOperacion, IdCategoria, IdAlmacen, CodigoBarra, TipoProducto,
    TipoComportamiento, Precio1, Precio2, Precio3, PrecioDolar, PrecioExterno, PrecioCompra,
    Imagen1, Itbis, SeCompra, SeAlquila, SeVende, ControlarStock, IsActivo,
    Ganancia, IdCocina, FechaInseccion, IdEmpresa, DisponibleEnCitas, DuracionServicio,
    Descuento, PorcientoDescuento, PorcientoGanancia
)
SELECT
    0, p.Nombre, p.Descripcion, 0, 100, @IdProveedor, 100, 100, p.Precio,
    1, N'VENTA', p.IdCategoria, @IdAlmacen, p.Codigo, N'Producto',
    N'Inventario', p.Precio, p.Precio, p.Precio, 0, 0, p.Precio * 0.55,
    p.Imagen, 1, 1, 0, 1, 0, 1,
    p.Precio * 0.45, 0, GETDATE(), @IdEmpresa, 0, 0,
    0, 0, 0
FROM @Prods p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos x WHERE x.IdEmpresa = @IdEmpresa AND x.Nombre = p.Nombre);

UPDATE x
SET PrecioVenta = p.Precio, Precio1 = p.Precio, Imagen1 = p.Imagen, IdCategoria = p.IdCategoria,
    TipoOperacion = N'VENTA', IsActivo = 1, SeVende = 1, Itbis = 1,
    CodigoBarra = p.Codigo, Descripcion = p.Descripcion
FROM dbo.Productos x
INNER JOIN @Prods p ON p.Nombre = x.Nombre
WHERE x.IdEmpresa = @IdEmpresa;

-- 11) Secuencias (desde Flawless 59)
INSERT INTO dbo.SecuenciaDocumentos
    (SecuenciaInicial, SecuenciaActual, Prefijo, IdTipoDocumento, FechaInseccion, IdEmpresa)
SELECT s.SecuenciaInicial, 0, s.Prefijo, s.IdTipoDocumento, GETDATE(), @IdEmpresa
FROM dbo.SecuenciaDocumentos s
WHERE s.IdEmpresa = 59
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SecuenciaDocumentos x
      WHERE x.IdEmpresa = @IdEmpresa AND x.IdTipoDocumento = s.IdTipoDocumento
  );

-- 12) Centro Produccion config
IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionConfiguracionEmpresa WHERE IdEmpresa = @IdEmpresa)
BEGIN
    INSERT INTO dbo.ProduccionConfiguracionEmpresa
        (IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
         TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
         ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion)
    VALUES (@IdEmpresa, 1, 0, 0, 1, 600, 900, 1, 1, 1, 1, GETDATE());
END
ELSE
    UPDATE dbo.ProduccionConfiguracionEmpresa SET Activo = 1 WHERE IdEmpresa = @IdEmpresa;

IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionEstacion WHERE IdEmpresa = @IdEmpresa AND Codigo = N'GENERAL')
BEGIN
    INSERT INTO dbo.ProduccionEstacion (IdEmpresa, Codigo, Nombre, EsDespacho, Activa, OrdenVisual)
    VALUES (@IdEmpresa, N'GENERAL', N'General', 0, 1, 0);
END

UPDATE c
SET IdEstacionPredeterminada = s.IdEstacion, FechaActualizacion = GETDATE()
FROM dbo.ProduccionConfiguracionEmpresa c
INNER JOIN dbo.ProduccionEstacion s ON s.IdEmpresa = c.IdEmpresa AND s.Codigo = N'GENERAL'
WHERE c.IdEmpresa = @IdEmpresa AND c.IdEstacionPredeterminada IS NULL;

SELECT @IdEmpresa AS IdEmpresa, @UserName AS Usuario, N'DemoFood2026!' AS Clave;
SELECT IdCategoria, Nombre FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa ORDER BY Prioridad;
SELECT IdProducto, Nombre, PrecioVenta FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa ORDER BY Nombre;
SELECT COUNT(*) AS Secuencias FROM dbo.SecuenciaDocumentos WHERE IdEmpresa = @IdEmpresa;
GO
