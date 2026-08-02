SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
USE AlahiaPos_Dev;

INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'CobroClienteRegistrado', 999001, 'CobroCliente',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T10:00:00","referenciaId":999001,"referenciaTipo":"CobroCliente","monto":100.00,"formaPago":"EFECTIVO","idFacturaHeader":9409}',
 'Pendiente', GETDATE());

INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'CompraConfirmada', 999002, 'Compra',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T10:00:00","referenciaId":999002,"referenciaTipo":"Compra","total":1180.00,"totalItbis":180.00,"montoInventario":1000.00,"montoGasto":0,"esContado":false,"formaPago":"Credito","numeroDocumento":"QA-FC-001"}',
 'Pendiente', GETDATE());

INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'PagoProveedorRegistrado', 999003, 'PagoProveedor',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T10:00:00","referenciaId":999003,"referenciaTipo":"PagoProveedor","monto":500.00,"formaPago":"EFECTIVO","idOrdenCompraHeader":1}',
 'Pendiente', GETDATE());

SELECT IdEventoOutbox, TipoEvento, ReferenciaId FROM EventosOutbox WHERE ReferenciaId IN (999001,999002,999003) ORDER BY IdEventoOutbox;
