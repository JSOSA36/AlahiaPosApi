/*
  Espejo operativo de Flawless Laundry (59) -> "Flawless Laundry sucursal" en AlahiaPos_Prod.
  Copia catálogo, módulos, perfiles, params, cuentas y usuarios.
  No copia facturas, cobros, ECF emitidos ni clientes de la sucursal origen.
  No replica NCF tradicionales B*.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @Origen INT = 59;
DECLARE @Destino INT;
DECLARE @NombreDestino NVARCHAR(200) = N'Flawless Laundry sucursal';
DECLARE @IdAlmacen INT;
DECLARE @IdAreaNegocio INT;
DECLARE @IdArea INT;
DECLARE @IdEstacion INT;
DECLARE @IdCatO INT, @IdCatN INT, @NomCat NVARCHAR(200), @DesCat NVARCHAR(MAX), @TipoCat NVARCHAR(100);
DECLARE @IsActiva BIT, @Prioridad INT, @Img NVARCHAR(500), @TipoOp NVARCHAR(50);
DECLARE @IdPerO INT, @IdPerN INT, @NomPer NVARCHAR(100), @DesPer NVARCHAR(255), @ActPer BIT;
DECLARE @IdEmpO INT, @IdEmpN INT;
DECLARE @IdCtaO INT, @IdCtaN INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @Origen AND NombreComercial LIKE N'Flawless%')
BEGIN
    RAISERROR(N'No existe Flawless Laundry IdEmpresa=59 en Prod.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.Empresas WHERE NombreComercial = @NombreDestino)
BEGIN
    RAISERROR(N'Ya existe Flawless Laundry sucursal en Prod.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

INSERT INTO dbo.Empresas (
    NombreComercial, RNC, Direccion, Telefono, Logo, CorreElectronico, Nota,
    FechaInseccion, IdPlan, Estado, FechaTerminacion,
    PrimaryColor, SecondaryColor, TertiaryColor, Latitude, Longitude,
    UrlCitas, UrlCatalogo, GuidPublico, TokenNotificacion, titleColor,
    CorreoSMTP, PasswordSMTP, ServidorSMTP, PuertoSMTP, UsaSSL, NombreRemitente,
    LimiteUsuario, InfoAgendar, EsEmisorElectronico, AmbienteFE, SecuenciaActualECF,
    FechaHabilitacionFE, EstadoFE, ApiPrint,
    FechaInicioPlan, FechaVencimientoPlan, EstadoPlan,
    PagadoServicio, FechaUltimoPago, FechaProximoPago, EstadoServicio,
    PoliticasAceptadas, Municipio, Provincia, Politicas, EsEmpresaSistema,
    PrecioPlanEspecialUsd, ProveedorFE, ProveedorFE_Nombre, ProveedorFE_BaseUrl,
    ProveedorFE_ApiKey, ProveedorFE_Usuario, ProveedorFE_Password,
    MontoServicio, LimiteFacturacion, CargoAdicional, CargoReconexionDop,
    ReconexionPendiente, NivelSoporte, TrabajaDomingo
)
SELECT
    @NombreDestino, RNC, Direccion, Telefono, Logo, CorreElectronico, Nota,
    CAST(GETDATE() AS date), IdPlan, 1, FechaTerminacion,
    PrimaryColor, SecondaryColor, TertiaryColor, Latitude, Longitude,
    UrlCitas, UrlCatalogo, NEWID(), NULL, titleColor,
    CorreoSMTP, PasswordSMTP, ServidorSMTP, PuertoSMTP, UsaSSL, NombreRemitente,
    LimiteUsuario, InfoAgendar,
    0, NULL, NULL, NULL, NULL, ApiPrint,
    GETDATE(), DATEADD(YEAR, 1, GETDATE()), N'ACTIVO',
    1, GETDATE(), CAST(N'2026-09-30' AS datetime), N'ACTIVA',
    PoliticasAceptadas, Municipio, Provincia, Politicas, 0,
    NULL, NULL, NULL, NULL, NULL, NULL, NULL,
    66.67, LimiteFacturacion, 0, CargoReconexionDop,
    0, NivelSoporte, TrabajaDomingo
FROM dbo.Empresas
WHERE IdEmpresa = @Origen;

SET @Destino = SCOPE_IDENTITY();

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion, FechaDesactivacion)
SELECT @Destino, em.ModuloId, em.Activo, GETDATE(), NULL
FROM dbo.Empresa_Modulos em
WHERE em.EmpresaId = @Origen;

INSERT INTO dbo.AreaNegocio (Nombre, Descripcion, Activo, IdEmpresa, FechaCreacion)
SELECT an.Nombre, an.Descripcion, an.Activo, @Destino, GETDATE()
FROM dbo.AreaNegocio an
WHERE an.IdEmpresa = @Origen;
SET @IdAreaNegocio = SCOPE_IDENTITY();

INSERT INTO dbo.Areas (Nombre, IsActivo, FechaInseccion, IdEmpresa, IdAreaNegocio)
SELECT a.Nombre, a.IsActivo, CAST(GETDATE() AS date), @Destino, @IdAreaNegocio
FROM dbo.Areas a
WHERE a.IdEmpresa = @Origen;
SET @IdArea = SCOPE_IDENTITY();

INSERT INTO dbo.Almacenes (Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion)
VALUES (N'Principal', N'Almacén principal Flawless Laundry sucursal', @Destino, 1, 1, GETDATE());
SET @IdAlmacen = SCOPE_IDENTITY();

IF NOT EXISTS (SELECT 1 FROM dbo.Almacens WHERE IdAlmacen = @IdAlmacen)
BEGIN
    SET IDENTITY_INSERT dbo.Almacens ON;
    INSERT INTO dbo.Almacens (IdAlmacen, Nombre, Descripcion, IsActivo, FechaInseccion, IdEmpresa)
    VALUES (@IdAlmacen, N'Principal', N'Almacen principal Flawless Laundry sucursal', 1, CAST(GETDATE() AS date), @Destino);
    SET IDENTITY_INSERT dbo.Almacens OFF;
END;

DECLARE @MapCat TABLE (IdOrigen INT PRIMARY KEY, IdNuevo INT NOT NULL);

DECLARE curCat CURSOR LOCAL FAST_FORWARD FOR
    SELECT IdCategoria, Nombre, Descripcion, Tipo, IsActiva, Prioridad, ImagenPath, TipoOperacion
    FROM dbo.Categorias
    WHERE IdEmpresa = @Origen
    ORDER BY IdCategoria;
OPEN curCat;
FETCH NEXT FROM curCat INTO @IdCatO, @NomCat, @DesCat, @TipoCat, @IsActiva, @Prioridad, @Img, @TipoOp;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, ImagenPath, TipoOperacion)
    VALUES (@NomCat, @DesCat, @TipoCat, ISNULL(@IsActiva, 1), CAST(GETDATE() AS date), @Prioridad, @Destino, @Img, @TipoOp);
    INSERT INTO @MapCat (IdOrigen, IdNuevo) VALUES (@IdCatO, SCOPE_IDENTITY());
    FETCH NEXT FROM curCat INTO @IdCatO, @NomCat, @DesCat, @TipoCat, @IsActiva, @Prioridad, @Img, @TipoOp;
END
CLOSE curCat;
DEALLOCATE curCat;

INSERT INTO dbo.Productos (
    Nombre, Descripcion, Rentado, Disponibles, IdProveedor, Cantidad, Stock, PrecioVenta,
    IdUnidadMedida, IdCategoria, IdAlmacen, CodigoBarra, Descuento, Precio1, Precio2, Precio3,
    PorcientoDescuento, PorcientoGanancia, PrecioCompra, Itbis, Nota, SeCompra, SeAlquila, SeVende,
    ControlarStock, IsActivo, Ganancia, FechaInseccion, TipoProducto, PrecioDolar, PrecioExterno,
    Imagen1, Imagen2, Imagen3, IdCocina, EsProductoBelleza, IdEmpresa, EsServicio, IdArea,
    DuracionServicio, DisponibleEnCitas, TipoOperacion, TipoComportamiento, TasaItbis,
    TipoIngresoDgiiDefault, CodigoExencionDgii
)
SELECT
    p.Nombre, p.Descripcion, ISNULL(p.Rentado, 0), ISNULL(p.Disponibles, 0), p.IdProveedor,
    ISNULL(p.Cantidad, 0), ISNULL(p.Stock, 0), ISNULL(p.PrecioVenta, 0),
    p.IdUnidadMedida, mapCat.IdNuevo, @IdAlmacen, p.CodigoBarra,
    ISNULL(p.Descuento, 0), ISNULL(p.Precio1, 0), ISNULL(p.Precio2, 0), ISNULL(p.Precio3, 0),
    ISNULL(p.PorcientoDescuento, 0), ISNULL(p.PorcientoGanancia, 0), ISNULL(p.PrecioCompra, 0),
    ISNULL(p.Itbis, 0), p.Nota, ISNULL(p.SeCompra, 0), ISNULL(p.SeAlquila, 0), ISNULL(p.SeVende, 1),
    ISNULL(p.ControlarStock, 0), ISNULL(p.IsActivo, 1), ISNULL(p.Ganancia, 0),
    CAST(GETDATE() AS date), p.TipoProducto, ISNULL(p.PrecioDolar, 0), ISNULL(p.PrecioExterno, 0),
    p.Imagen1, p.Imagen2, p.Imagen3, p.IdCocina, ISNULL(p.EsProductoBelleza, 0), @Destino,
    ISNULL(p.EsServicio, 0), CASE WHEN ISNULL(p.IdArea, 0) = 0 THEN NULL ELSE @IdArea END,
    ISNULL(p.DuracionServicio, 0), ISNULL(p.DisponibleEnCitas, 0),
    ISNULL(NULLIF(p.TipoOperacion, N''), N'VENTA'), p.TipoComportamiento, p.TasaItbis,
    p.TipoIngresoDgiiDefault, p.CodigoExencionDgii
FROM dbo.Productos p
LEFT JOIN @MapCat mapCat ON mapCat.IdOrigen = p.IdCategoria
WHERE p.IdEmpresa = @Origen;

INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    @Destino, p.Tipo, p.CodigoPOS, p.Clave,
    CASE WHEN p.Clave IN (N'FACTURACION_ELECTRONICA', N'PREVIEW_DGII') THEN N'false' ELSE p.Valor END,
    p.Descripcion, GETDATE(), ISNULL(p.Activo, 1)
FROM dbo.Parametros p
WHERE p.IdEmpresa = @Origen;

INSERT INTO dbo.SecuenciaDocumentos (IdEmpresa, IdTipoDocumento, Prefijo, SecuenciaInicial, SecuenciaActual, FechaInseccion, TipoDocumentos_IdTipoDocumentos)
SELECT @Destino, s.IdTipoDocumento, s.Prefijo, s.SecuenciaInicial, 0, GETDATE(), s.TipoDocumentos_IdTipoDocumentos
FROM dbo.SecuenciaDocumentos s
WHERE s.IdEmpresa = @Origen;

INSERT INTO dbo.SecuenciasECF (
    IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
    stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
    Ambiente, FechaAutorizacion, NumeroResolucion
)
SELECT
    @Destino, s.TipoNCF, s.Serie,
    CASE WHEN ISNULL(s.SecuenciaInicial, 1) > 0 THEN s.SecuenciaInicial - 1 ELSE 0 END,
    s.SecuenciaFinal, s.fechaVencimiento, s.stockMinimo, s.Activo, GETDATE(),
    s.Descripcion, s.TipoEcfDgii, s.SecuenciaInicial, N'testecf',
    s.FechaAutorizacion, s.NumeroResolucion
FROM dbo.SecuenciasECF s
WHERE s.IdEmpresa = @Origen
  AND (s.TipoNCF LIKE N'E%' OR s.TipoEcfDgii IS NOT NULL);

DECLARE @MapCuentas TABLE (IdOrigen INT PRIMARY KEY, IdNuevo INT NOT NULL);

DECLARE curCta CURSOR LOCAL FAST_FORWARD FOR
    SELECT IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Origen ORDER BY IdCuentaFinanciera;
OPEN curCta;
FETCH NEXT FROM curCta INTO @IdCtaO;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, FechaSaldoInicial,
        PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion
    )
    SELECT
        @Destino, cf.Nombre, cf.TipoCuenta, cf.Banco, cf.NumeroCuenta,
        LEFT(cf.TipoCuenta + N'-SUC-' + CAST(@Destino AS NVARCHAR(10)), 40),
        cf.EsPrincipal, cf.IdTesoreriaSubtipoCuenta, cf.Color, cf.Icono, ISNULL(cf.Moneda, N'DOP'),
        cf.Activa, 0, 0, CAST(GETDATE() AS date),
        ISNULL(cf.PermiteMovimientosManuales, 1), ISNULL(cf.PermiteSaldoNegativo, 0), GETDATE(),
        N'Espejo Flawless Laundry sucursal'
    FROM dbo.CuentaFinanciera cf
    WHERE cf.IdCuentaFinanciera = @IdCtaO;

    INSERT INTO @MapCuentas (IdOrigen, IdNuevo) VALUES (@IdCtaO, SCOPE_IDENTITY());
    FETCH NEXT FROM curCta INTO @IdCtaO;
END
CLOSE curCta;
DEALLOCATE curCta;

INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
SELECT @Destino, m.MetodoPago, map.IdNuevo, m.Activo
FROM dbo.MetodoPagoCuenta m
INNER JOIN @MapCuentas map ON map.IdOrigen = m.IdCuentaFinanciera
WHERE m.IdEmpresa = @Origen;

INSERT INTO dbo.TesoreriaConfiguracion (
    IdEmpresa, ModoSaldo, PermitirSaldoNegativo, RequiereConciliacionBanco,
    IdCuentaCajaGeneral, IdCuentaCajaChicaDefault, FechaCreacion
)
SELECT
    @Destino, t.ModoSaldo, t.PermitirSaldoNegativo, t.RequiereConciliacionBanco,
    caja.IdNuevo, chica.IdNuevo, GETDATE()
FROM dbo.TesoreriaConfiguracion t
LEFT JOIN @MapCuentas caja ON caja.IdOrigen = t.IdCuentaCajaGeneral
LEFT JOIN @MapCuentas chica ON chica.IdOrigen = t.IdCuentaCajaChicaDefault
WHERE t.IdEmpresa = @Origen;

INSERT INTO dbo.DgiiConfiguracionEmpresa (
    IdEmpresa, RegimenTributarioCodigo, EsConstructor, EsComisionista, ObligadoLibroVentasSF,
    RazonSocial, DeclaranteNombre, DeclaranteCalidad, VersionInstructivoPreferida,
    Activo, FechaCreacion, FiscalActivo, Generar606, Generar607, GenerarIt1, FacturacionElectronicaActiva
)
SELECT
    @Destino, d.RegimenTributarioCodigo, d.EsConstructor, d.EsComisionista, d.ObligadoLibroVentasSF,
    @NombreDestino, d.DeclaranteNombre, d.DeclaranteCalidad, d.VersionInstructivoPreferida,
    d.Activo, GETDATE(), d.FiscalActivo, d.Generar606, d.Generar607, d.GenerarIt1, 0
FROM dbo.DgiiConfiguracionEmpresa d
WHERE d.IdEmpresa = @Origen;

DECLARE @MapPerfil TABLE (IdOrigen INT PRIMARY KEY, IdNuevo INT NOT NULL);

DECLARE curPer CURSOR LOCAL FAST_FORWARD FOR
    SELECT IdPerfil, Nombre, Descripcion, Activo FROM dbo.Perfiles WHERE IdEmpresa = @Origen ORDER BY IdPerfil;
OPEN curPer;
FETCH NEXT FROM curPer INTO @IdPerO, @NomPer, @DesPer, @ActPer;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (@Destino, @NomPer, @DesPer, @ActPer);
    INSERT INTO @MapPerfil (IdOrigen, IdNuevo) VALUES (@IdPerO, SCOPE_IDENTITY());
    FETCH NEXT FROM curPer INTO @IdPerO, @NomPer, @DesPer, @ActPer;
END
CLOSE curPer;
DEALLOCATE curPer;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT mp.IdNuevo, pr.IdModulo, pr.Activo, GETDATE(), @Destino
FROM dbo.PerfilRoles pr
INNER JOIN @MapPerfil mp ON mp.IdOrigen = pr.IdPerfil
WHERE pr.IdEmpresa = @Origen;

DECLARE @MapEmp TABLE (IdOrigen INT PRIMARY KEY, IdNuevo INT NOT NULL);
DECLARE @Ced NVARCHAR(50), @NomE NVARCHAR(200), @Dir NVARCHAR(500), @Tel NVARCHAR(50), @Cel NVARCHAR(50);
DECLARE @ComS DECIMAL(18,2), @ComP DECIMAL(18,2), @NotaE NVARCHAR(MAX), @EstE BIT, @Ocu NVARCHAR(200);

DECLARE curEmp CURSOR LOCAL FAST_FORWARD FOR
    SELECT IdEmpleados, Cedula, Nombre, Direccion, Telefono, Celular,
           ComisionServicio, ComisionProductos, Nota, Estado, Ocupacion
    FROM dbo.EmpleadosP WHERE IdEmpresa = @Origen ORDER BY IdEmpleados;
OPEN curEmp;
FETCH NEXT FROM curEmp INTO @IdEmpO, @Ced, @NomE, @Dir, @Tel, @Cel, @ComS, @ComP, @NotaE, @EstE, @Ocu;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.EmpleadosP (
        IdEmpresa, Cedula, Nombre, Direccion, Telefono, Celular,
        ComisionServicio, ComisionProductos, Nota, Estado, Ocupacion
    )
    VALUES (@Destino, @Ced, @NomE, @Dir, @Tel, @Cel, @ComS, @ComP, @NotaE, @EstE, @Ocu);
    INSERT INTO @MapEmp (IdOrigen, IdNuevo) VALUES (@IdEmpO, SCOPE_IDENTITY());
    FETCH NEXT FROM curEmp INTO @IdEmpO, @Ced, @NomE, @Dir, @Tel, @Cel, @ComS, @ComP, @NotaE, @EstE, @Ocu;
END
CLOSE curEmp;
DEALLOCATE curEmp;

INSERT INTO dbo.Usuarios (
    IdEmpresa, UserName, Correo, PasswordHash, Dispositivo, Token, Estado, UltimoAcceso,
    TokenRecuperacion, TokenExpira, IdPerfil, IdEmpleado, FechaCreacion,
    PuedeEliminarOrden, PuedeEliminarItemCarrito, PuedeDisminuirCantidadCarrito, PuedeEditarPrecioCarrito
)
SELECT
    @Destino,
    CASE
        WHEN CHARINDEX(N'@', u.UserName) > 0
            THEN LEFT(u.UserName, CHARINDEX(N'@', u.UserName) - 1) + N'.sucursal' + SUBSTRING(u.UserName, CHARINDEX(N'@', u.UserName), 200)
        ELSE u.UserName + N'.sucursal'
    END,
    CASE
        WHEN u.Correo IS NULL OR LTRIM(RTRIM(u.Correo)) = N'' THEN NULL
        WHEN CHARINDEX(N'@', u.Correo) > 0
            THEN LEFT(u.Correo, CHARINDEX(N'@', u.Correo) - 1) + N'.sucursal' + SUBSTRING(u.Correo, CHARINDEX(N'@', u.Correo), 200)
        ELSE u.Correo + N'.sucursal'
    END,
    u.PasswordHash, NULL, NULL, u.Estado, NULL, NULL, NULL,
    mp.IdNuevo, me.IdNuevo, GETDATE(),
    ISNULL(u.PuedeEliminarOrden, 0), ISNULL(u.PuedeEliminarItemCarrito, 0),
    ISNULL(u.PuedeDisminuirCantidadCarrito, 0), ISNULL(u.PuedeEditarPrecioCarrito, 0)
FROM dbo.Usuarios u
INNER JOIN @MapPerfil mp ON mp.IdOrigen = u.IdPerfil
INNER JOIN @MapEmp me ON me.IdOrigen = u.IdEmpleado
WHERE u.IdEmpresa = @Origen;

INSERT INTO dbo.Clientes (
    NombreComercial, Estado, LimiteCredito, FechaInseccion, CreditoFavor, IdEmpresa, EsRegimenEspecial
)
VALUES (N'Al Portador', 1, 0, CAST(GETDATE() AS date), 0, @Destino, 0);

INSERT INTO dbo.DescuentoHeader (
    IdEmpresa, NombreEvento, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin,
    DiasSemana, HoraInicio, HoraFin, AplicaATodos, AplicaATodasAreas, Activo
)
SELECT
    @Destino, dh.NombreEvento, dh.Descripcion, dh.TipoDescuento, dh.Valor,
    dh.FechaInicio, dh.FechaFin, dh.DiasSemana, dh.HoraInicio, dh.HoraFin,
    dh.AplicaATodos, dh.AplicaATodasAreas, dh.Activo
FROM dbo.DescuentoHeader dh
WHERE dh.IdEmpresa = @Origen;

INSERT INTO dbo.ProduccionEstacion (IdEmpresa, Codigo, Nombre, EsDespacho, Activa, OrdenVisual, FechaCreacion)
SELECT @Destino, pe.Codigo, pe.Nombre, pe.EsDespacho, pe.Activa, pe.OrdenVisual, GETDATE()
FROM dbo.ProduccionEstacion pe
WHERE pe.IdEmpresa = @Origen;

SELECT TOP 1 @IdEstacion = IdEstacion
FROM dbo.ProduccionEstacion
WHERE IdEmpresa = @Destino
ORDER BY OrdenVisual, IdEstacion;

INSERT INTO dbo.ProduccionConfiguracionEmpresa (
    IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
    TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
    IdEstacionPredeterminada, ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion
)
SELECT
    @Destino, pc.Activo, pc.UsarEstaciones, pc.UsarEstadosPorItem, pc.SonidoActivo,
    pc.TiempoAdvertenciaSegDefault, pc.TiempoCriticoSegDefault, pc.PermitirCompletarDesdeEstacion,
    @IdEstacion, pc.ModoOscuroDefault, pc.MostrarNombreCliente, pc.MostrarUsuarioSolicita, GETDATE()
FROM dbo.ProduccionConfiguracionEmpresa pc
WHERE pc.IdEmpresa = @Origen;

INSERT INTO dbo.RrhhTipoAusencia (
    IdEmpresa, Codigo, Nombre, Categoria, UnidadDefault, ConGoceSueldo,
    RequiereAprobacion, AfectaAsistencia, Activo
)
SELECT @Destino, t.Codigo, t.Nombre, t.Categoria, t.UnidadDefault, t.ConGoceSueldo,
       t.RequiereAprobacion, t.AfectaAsistencia, t.Activo
FROM dbo.RrhhTipoAusencia t
WHERE t.IdEmpresa = @Origen;

INSERT INTO dbo.RrhhBeneficio (
    IdEmpresa, Codigo, Nombre, Descripcion, TipoCalculo, Monto, Periodicidad,
    AfectaNomina, EnEspecie, Activo, FechaCreacion, FormaDesembolso, DiaPagoMes, MetodoPago, DescontarConsumoNomina
)
SELECT
    @Destino, b.Codigo, b.Nombre, b.Descripcion, b.TipoCalculo, b.Monto, b.Periodicidad,
    b.AfectaNomina, b.EnEspecie, b.Activo, GETDATE(), b.FormaDesembolso, b.DiaPagoMes, b.MetodoPago, b.DescontarConsumoNomina
FROM dbo.RrhhBeneficio b
WHERE b.IdEmpresa = @Origen;

COMMIT;

SELECT @Destino AS IdEmpresaNueva, @NombreDestino AS Nombre;
SELECT UserName, Correo FROM dbo.Usuarios WHERE IdEmpresa = @Destino;
SELECT 'Productos' t, COUNT(*) c FROM dbo.Productos WHERE IdEmpresa = @Destino
UNION ALL SELECT 'Categorias', COUNT(*) FROM dbo.Categorias WHERE IdEmpresa = @Destino
UNION ALL SELECT 'Modulos', COUNT(*) FROM dbo.Empresa_Modulos WHERE EmpresaId = @Destino
UNION ALL SELECT 'Usuarios', COUNT(*) FROM dbo.Usuarios WHERE IdEmpresa = @Destino
UNION ALL SELECT 'Perfiles', COUNT(*) FROM dbo.Perfiles WHERE IdEmpresa = @Destino
UNION ALL SELECT 'Cuentas', COUNT(*) FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino
UNION ALL SELECT 'Parametros', COUNT(*) FROM dbo.Parametros WHERE IdEmpresa = @Destino
UNION ALL SELECT 'SecuenciasECF', COUNT(*) FROM dbo.SecuenciasECF WHERE IdEmpresa = @Destino
UNION ALL SELECT 'PerfilRoles', COUNT(*) FROM dbo.PerfilRoles WHERE IdEmpresa = @Destino;
