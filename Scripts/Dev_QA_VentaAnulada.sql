SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
USE AlahiaPos_Dev;

INSERT INTO EventosOutbox (IdEmpresa, TipoEvento, ReferenciaId, ReferenciaTipo, Payload, Estado, FechaCreacion)
VALUES
(60, 'VentaAnulada', 991102, 'VentaAnulada',
 '{"idEmpresa":60,"idUsuario":28,"fecha":"2026-07-20T12:30:00","referenciaId":991102,"referenciaTipo":"VentaAnulada","motivo":"QA anular venta COGS","numeroFactura":"QA-COGS-01"}',
 'Pendiente', GETDATE());

SELECT TOP 1 IdEventoOutbox, TipoEvento, Estado
FROM EventosOutbox
WHERE ReferenciaId = 991102 AND TipoEvento = 'VentaAnulada'
ORDER BY IdEventoOutbox DESC;

SELECT a.Numero, LEFT(a.Concepto,60) Concepto, a.TipoOperacion, a.OrigenModulo, a.OrigenReferenciaId,
  (SELECT SUM(Debito) FROM AsientosContablesDetalle d WHERE d.IdAsientoContable=a.IdAsientoContable) Debito,
  (SELECT SUM(Credito) FROM AsientosContablesDetalle d WHERE d.IdAsientoContable=a.IdAsientoContable) Credito
FROM AsientosContables a
WHERE a.IdEmpresa=60 AND a.OrigenReferenciaId IN (991101,991102,991104,991105,991106)
ORDER BY a.IdAsientoContable;
