-- ============================================================
-- Fixture QA Dev: cruce stock vs ventas (Sabor Urbano = 62)
-- Solo AlahiaPos_Dev.
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 62;
DECLARE @IdUsuario INT = (
    SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdEmpresa = @IdEmpresa AND Estado = 1 ORDER BY IdUsuario
);
DECLARE @IdProducto INT = (
    SELECT TOP 1 p.IdProducto
    FROM dbo.Productos p
    WHERE p.IdEmpresa = @IdEmpresa AND ISNULL(p.EsServicio, 0) = 0
    ORDER BY p.IdProducto
);
DECLARE @IdSucursal INT = (
    SELECT TOP 1 IdSucursal FROM dbo.Sucursal WHERE IdEmpresa = @IdEmpresa AND Activa = 1 ORDER BY EsPrincipal DESC, IdSucursal
);
DECLARE @IdAlmacen INT = (
    SELECT TOP 1 IdAlmacen FROM dbo.Almacenes WHERE IdEmpresa = @IdEmpresa ORDER BY IdAlmacen
);
DECLARE @Tpl INT = (
    SELECT TOP 1 IdFacturaHeader FROM dbo.FacturaHeaders WHERE IdEmpresa = @IdEmpresa AND IdTipoDocumentos = 1 ORDER BY IdFacturaHeader DESC
);

IF @IdUsuario IS NULL OR @IdProducto IS NULL OR @Tpl IS NULL
BEGIN
    RAISERROR(N'Faltan usuario/producto/factura plantilla en empresa 62.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1 FROM dbo.CajaCierre
    WHERE IdEmpresa = @IdEmpresa AND Observacion = N'FIXTURE_CRUCE_STOCK_DEV'
)
BEGIN
    PRINT 'Fixture ya existe (FIXTURE_CRUCE_STOCK_DEV). OK.';
    SELECT TOP 1 IdCajaCierre, FechaCierre FROM dbo.CajaCierre
    WHERE IdEmpresa = @IdEmpresa AND Observacion = N'FIXTURE_CRUCE_STOCK_DEV';
    RETURN;
END;

DECLARE @Apertura DATETIME = '2026-09-08T09:00:00';
DECLARE @Cierre DATETIME = '2026-09-08T18:00:00';
DECLARE @Venta1 DATETIME = '2026-09-08T11:00:00';
DECLARE @Venta2 DATETIME = '2026-09-08T15:30:00';
DECLARE @StockAbrir DECIMAL(18,2) = 50;

DECLARE @IdApertura INT;
INSERT INTO dbo.CajaApertura (IdEmpresa, IdUsuario, FechaApertura, MontoInicial, Estado, Observacion, IdSucursal)
VALUES (@IdEmpresa, @IdUsuario, @Apertura, 100, 'CERRADA', N'FIXTURE_CRUCE_STOCK_DEV', @IdSucursal);
SET @IdApertura = SCOPE_IDENTITY();

DECLARE @IdCajaCierre INT;
INSERT INTO dbo.CajaCierre (
    IdCajaApertura, IdEmpresa, IdUsuario, FechaCierre,
    TotalEfectivo, TotalTarjeta, TotalTransferencia, TotalCredito, TotalGeneral,
    MontoRealCaja, Diferencia, Observacion,
    VentasBrutas, TotalDescuento, TotalIngresosExtra, TotalGastos, TotalIngresosNetos,
    DebeHaber, TotalBillet, IdSucursal
)
VALUES (
    @IdApertura, @IdEmpresa, @IdUsuario, @Cierre,
    300, 0, 0, 0, 300,
    100, 0, N'FIXTURE_CRUCE_STOCK_DEV',
    300, 0, 0, 0, 300,
    0, 0, @IdSucursal
);
SET @IdCajaCierre = SCOPE_IDENTITY();

-- Stock referencia al abrir
DECLARE @IdMovAbrir INT;
INSERT INTO dbo.MovimientosInventario (TipoMovimiento, Motivo, Referencia, Observacion, Fecha, IdUsuario, IdEmpresa, Activo, IdAlmacen, IdSucursal)
VALUES (N'ENTRADA', N'AJUSTE', N'FIXTURE_CRUCE', N'FIXTURE_CRUCE_STOCK_DEV abrir', DATEADD(MINUTE, -5, @Apertura), @IdUsuario, @IdEmpresa, 1, @IdAlmacen, @IdSucursal);
SET @IdMovAbrir = SCOPE_IDENTITY();
INSERT INTO dbo.MovimientosInventarioDetalle (IdMovimientoInventario, IdProducto, Cantidad, StockAnterior, StockNuevo, Fecha)
VALUES (@IdMovAbrir, @IdProducto, @StockAbrir, 0, @StockAbrir, DATEADD(MINUTE, -5, @Apertura));

-- Factura 1 (clone plantilla)
DECLARE @IdFact1 INT;
INSERT INTO dbo.FacturaHeaders (
    Plazo, TipoFactura, MontoPropina, IdEmpleados, IdMoso, IdMesa, IdTipoDocumentos, NCF, FormaPago, IDCliente,
    Efectivo, SubTotal, Cambio, Total, TotalItbis, TotalDescuento, EstaCancelada, EstaCerrada, Nota, FechaBencimiento,
    Estado, Pagado, Pendiente, Hora, AjustadoInventario, NombreCuenta, TipoOrden, FechaInseccion, NumeroDocumento,
    IdEmpleadoComision, IdDelivery, Moneda, PrintPending, PrintAcount, PrintAcountAll,
    MontoTarjetaVisa, MontoTarjetaMasterCard, MontoTransferencia, MontoCheques, MontoEfectivo, MontoDolar, MontoNotaCredito,
    Estado_Orden, IdEmpresa, MotivoAnulacion, PrintLavador, FechaEntrega, HoraEntrega, Abono, Balance, RNC, NombreEmpresa,
    IdUsuario, IdCajaCierre, IdSucursal, MontoGravado, MontoExento, MontoGravadoI1, MontoGravadoI2, MontoGravadoI3, MontoGravadoI4,
    DescuentoAfectaBase, MontoPropinaLegal, FotografiaFiscalVersion, CargarConsumoNomina, MontoCargo
)
SELECT
    Plazo, TipoFactura, 0, ISNULL(IdEmpleados, 0), IdMoso, IdMesa, 1, NCF, N'EFECTIVO', IDCliente,
    200, 200, 0, 200, 0, 0, 0, 1, N'FIXTURE_CRUCE_STOCK_DEV', GETDATE(),
    N'PAGADA', 200, 0, Hora, 1, N'Fixture cruce 1', TipoOrden, @Venta1, NumeroDocumento,
    IdEmpleadoComision, IdDelivery, Moneda, 0, 0, 0,
    0, 0, 0, 0, 200, 0, 0,
    Estado_Orden, IdEmpresa, NULL, 0, FechaEntrega, HoraEntrega, 0, 0, RNC, NombreEmpresa,
    @IdUsuario, @IdCajaCierre, @IdSucursal, 200, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0
FROM dbo.FacturaHeaders WHERE IdFacturaHeader = @Tpl;
SET @IdFact1 = SCOPE_IDENTITY();

INSERT INTO dbo.FacturaDetalles (
    IdFacturaHeader, IdProducto, Dias, Cantidad, Itbis, SubTotal, Descuento, PrecioOferta,
    EnviadoCocina, FechaInseccion, StatuItem, IdEmpresa, PrintLavador, CantidadDevuelta,
    MontoGravadoLinea, MontoExentoLinea, DescuentoAfectaBase, ItbisCalculado
)
VALUES (
    @IdFact1, @IdProducto, 0, 2, 0, 200, 0, 100,
    0, @Venta1, 0, @IdEmpresa, 0, 0,
    200, 0, 0, 0
);

DECLARE @IdMovV1 INT;
INSERT INTO dbo.MovimientosInventario (TipoMovimiento, Motivo, Referencia, Observacion, Fecha, IdUsuario, IdEmpresa, Activo, IdAlmacen, IdSucursal)
VALUES (N'SALIDA', N'VENTA', CONCAT(N'Factura #', @IdFact1), N'FIXTURE_CRUCE_STOCK_DEV', @Venta1, @IdUsuario, @IdEmpresa, 1, @IdAlmacen, @IdSucursal);
SET @IdMovV1 = SCOPE_IDENTITY();
INSERT INTO dbo.MovimientosInventarioDetalle (IdMovimientoInventario, IdProducto, Cantidad, StockAnterior, StockNuevo, Fecha)
VALUES (@IdMovV1, @IdProducto, 2, @StockAbrir, @StockAbrir - 2, @Venta1);

-- Factura 2
DECLARE @IdFact2 INT;
INSERT INTO dbo.FacturaHeaders (
    Plazo, TipoFactura, MontoPropina, IdEmpleados, IdMoso, IdMesa, IdTipoDocumentos, NCF, FormaPago, IDCliente,
    Efectivo, SubTotal, Cambio, Total, TotalItbis, TotalDescuento, EstaCancelada, EstaCerrada, Nota, FechaBencimiento,
    Estado, Pagado, Pendiente, Hora, AjustadoInventario, NombreCuenta, TipoOrden, FechaInseccion, NumeroDocumento,
    IdEmpleadoComision, IdDelivery, Moneda, PrintPending, PrintAcount, PrintAcountAll,
    MontoTarjetaVisa, MontoTarjetaMasterCard, MontoTransferencia, MontoCheques, MontoEfectivo, MontoDolar, MontoNotaCredito,
    Estado_Orden, IdEmpresa, MotivoAnulacion, PrintLavador, FechaEntrega, HoraEntrega, Abono, Balance, RNC, NombreEmpresa,
    IdUsuario, IdCajaCierre, IdSucursal, MontoGravado, MontoExento, MontoGravadoI1, MontoGravadoI2, MontoGravadoI3, MontoGravadoI4,
    DescuentoAfectaBase, MontoPropinaLegal, FotografiaFiscalVersion, CargarConsumoNomina, MontoCargo
)
SELECT
    Plazo, TipoFactura, 0, ISNULL(IdEmpleados, 0), IdMoso, IdMesa, 1, NCF, N'EFECTIVO', IDCliente,
    100, 100, 0, 100, 0, 0, 0, 1, N'FIXTURE_CRUCE_STOCK_DEV', GETDATE(),
    N'PAGADA', 100, 0, Hora, 1, N'Fixture cruce 2', TipoOrden, @Venta2, NumeroDocumento,
    IdEmpleadoComision, IdDelivery, Moneda, 0, 0, 0,
    0, 0, 0, 0, 100, 0, 0,
    Estado_Orden, IdEmpresa, NULL, 0, FechaEntrega, HoraEntrega, 0, 0, RNC, NombreEmpresa,
    @IdUsuario, @IdCajaCierre, @IdSucursal, 100, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0
FROM dbo.FacturaHeaders WHERE IdFacturaHeader = @Tpl;
SET @IdFact2 = SCOPE_IDENTITY();

INSERT INTO dbo.FacturaDetalles (
    IdFacturaHeader, IdProducto, Dias, Cantidad, Itbis, SubTotal, Descuento, PrecioOferta,
    EnviadoCocina, FechaInseccion, StatuItem, IdEmpresa, PrintLavador, CantidadDevuelta,
    MontoGravadoLinea, MontoExentoLinea, DescuentoAfectaBase, ItbisCalculado
)
VALUES (
    @IdFact2, @IdProducto, 0, 1, 0, 100, 0, 100,
    0, @Venta2, 0, @IdEmpresa, 0, 0,
    100, 0, 0, 0
);

DECLARE @IdMovV2 INT;
INSERT INTO dbo.MovimientosInventario (TipoMovimiento, Motivo, Referencia, Observacion, Fecha, IdUsuario, IdEmpresa, Activo, IdAlmacen, IdSucursal)
VALUES (N'SALIDA', N'VENTA', CONCAT(N'Factura #', @IdFact2), N'FIXTURE_CRUCE_STOCK_DEV', @Venta2, @IdUsuario, @IdEmpresa, 1, @IdAlmacen, @IdSucursal);
SET @IdMovV2 = SCOPE_IDENTITY();
INSERT INTO dbo.MovimientosInventarioDetalle (IdMovimientoInventario, IdProducto, Cantidad, StockAnterior, StockNuevo, Fecha)
VALUES (@IdMovV2, @IdProducto, 1, @StockAbrir - 2, @StockAbrir - 3, @Venta2);

SELECT
    @IdCajaCierre AS IdCajaCierre,
    @IdProducto AS IdProducto,
    @StockAbrir AS StockAlAbrir,
    CAST(3 AS DECIMAL(18,2)) AS Vendido,
    @StockAbrir - 3 AS EsperadoAlCerrar;

PRINT 'OK fixture cruce Dev listo.';
GO
