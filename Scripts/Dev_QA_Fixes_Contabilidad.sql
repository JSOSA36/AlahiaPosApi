SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
USE AlahiaPos_Dev;

-- Nota crédito (contado / reembolso caja) + venta con COGS + anulación + ajuste caja + inventario salida
DECLARE @refNc INT = 991101;
DECLARE @refVentaCogs INT = 991102;
DECLARE @refAnular INT = 991103;
DECLARE @refEntrada INT = 991104;
DECLARE @refSalidaInv INT = 991105;
DECLARE @refCompraContado INT = 991106;

INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'NotaCreditoCreada', @refNc, 'NotaCredito',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:00:00","referenciaId":991101,"referenciaTipo":"NotaCredito","idFacturaHeader":9409,"numeroDocumento":"QA-NC-001","subtotal":1000.00,"itbis":180.00,"total":1180.00,"montoCxc":0,"montoTesoreria":1180.00,"costoInventario":200.00}',
 'Pendiente', GETDATE());

-- Venta con COGS (producto con costo)
INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'VentaConfirmada', @refVentaCogs, 'Venta',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:00:00","referenciaId":991102,"referenciaTipo":"Venta","numeroFactura":"QA-COGS-01","tipoFactura":"Contado","subtotal":500.00,"itbis":90.00,"total":590.00,"montoCobrado":590.00,"montoCredito":0,"costoInventario":120.00,"metodoPago":"EFECTIVO"}',
 'Pendiente', GETDATE());

-- Entrada ajuste tesorería
INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'MovimientoBancarioRegistrado', @refEntrada, 'Entrada',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:00:00","referenciaId":991104,"referenciaTipo":"Entrada","tipoMovimiento":"ENTRADA","categoria":"AJUSTE","monto":50.00,"tipoCuentaDestino":"CAJA","motivo":"QA ajuste entrada"}',
 'Pendiente', GETDATE());

-- Inventario salida (ajuste / merma)
INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'InventarioMovimientoRegistrado', @refSalidaInv, 'Inventario',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:00:00","referenciaId":991105,"referenciaTipo":"Inventario","tipoMovimiento":"SALIDA","motivo":"AJUSTE","monto":75.00}',
 'Pendiente', GETDATE());

-- Compra contado
INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'CompraConfirmada', @refCompraContado, 'Compra',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:00:00","referenciaId":991106,"referenciaTipo":"Compra","total":590.00,"totalItbis":90.00,"montoInventario":500.00,"montoGasto":0,"esContado":true,"formaPago":"EFECTIVO","numeroDocumento":"QA-FC-CONTADO"}',
 'Pendiente', GETDATE());

SELECT IdEventoOutbox, TipoEvento, ReferenciaId, Estado
FROM EventosOutbox
WHERE ReferenciaId IN (991101,991102,991104,991105,991106)
ORDER BY IdEventoOutbox;
