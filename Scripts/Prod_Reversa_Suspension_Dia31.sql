-- Revierte suspensiones automáticas del 31/ago/2026 (día 31 mal tratado como vencido).
-- La ventana de cobro es del 30 al 3; no hay reconexión todavía.
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

BEGIN TRAN;

DECLARE @Desde DATETIME = '2026-08-31T00:00:00';
DECLARE @Hasta DATETIME = '2026-09-01T00:00:00';

;WITH afectadas AS (
    SELECT DISTINCT e.IdEmpresa
    FROM dbo.SuscripcionEvento e
    WHERE e.Tipo = N'SUSPENSION'
      AND e.Fecha >= @Desde AND e.Fecha < @Hasta
)
UPDATE emp
SET emp.ReconexionPendiente = 0,
    emp.EstadoServicio = CASE
        WHEN emp.EstadoServicio = N'PAGO_REPORTADO' THEN N'PAGO_REPORTADO'
        WHEN emp.EstadoServicio = N'CANCELADA' THEN N'CANCELADA'
        ELSE N'PENDIENTE_PAGO'
    END
FROM dbo.Empresas emp
INNER JOIN afectadas a ON a.IdEmpresa = emp.IdEmpresa
WHERE emp.EstadoServicio IN (N'SUSPENDIDA', N'BLOQUEADO', N'PAGO_REPORTADO', N'PENDIENTE_PAGO');

UPDATE c
SET c.Estado = N'ABIERTO',
    c.Monto = e.MontoServicio + e.CargoAdicional
FROM dbo.SuscripcionCiclo c
INNER JOIN dbo.Empresas e ON e.IdEmpresa = c.IdEmpresa
INNER JOIN (
    SELECT DISTINCT IdEmpresa
    FROM dbo.SuscripcionEvento
    WHERE Tipo = N'SUSPENSION' AND Fecha >= @Desde AND Fecha < @Hasta
) a ON a.IdEmpresa = c.IdEmpresa
WHERE c.Anio = 2026 AND c.Mes = 8
  AND c.Estado = N'VENCIDO';

DELETE d
FROM dbo.SuscripcionCicloDetalle d
INNER JOIN dbo.SuscripcionCiclo c ON c.IdCiclo = d.IdCiclo
INNER JOIN (
    SELECT DISTINCT IdEmpresa
    FROM dbo.SuscripcionEvento
    WHERE Tipo = N'SUSPENSION' AND Fecha >= @Desde AND Fecha < @Hasta
) a ON a.IdEmpresa = c.IdEmpresa
WHERE c.Anio = 2026 AND c.Mes = 8
  AND d.TipoLinea = N'RECONEXION';

INSERT INTO dbo.SuscripcionEvento (IdEmpresa, IdCiclo, Tipo, Detalle, Canal, Fecha)
SELECT e.IdEmpresa, c.IdCiclo, N'REVERSA_SUSPENSION_DIA31',
       N'Reversa: el 31 sigue en ventana de pago (plazo hasta el día 3). Sin cargo de reconexión.',
       N'SISTEMA', GETDATE()
FROM dbo.Empresas e
INNER JOIN (
    SELECT DISTINCT IdEmpresa
    FROM dbo.SuscripcionEvento
    WHERE Tipo = N'SUSPENSION' AND Fecha >= @Desde AND Fecha < @Hasta
) a ON a.IdEmpresa = e.IdEmpresa
LEFT JOIN dbo.SuscripcionCiclo c
    ON c.IdEmpresa = e.IdEmpresa AND c.Anio = 2026 AND c.Mes = 8;

COMMIT TRAN;

SELECT e.IdEmpresa, e.NombreComercial, e.EstadoServicio, e.PagadoServicio, e.ReconexionPendiente,
       c.Estado AS Ciclo, c.Monto
FROM dbo.Empresas e
LEFT JOIN dbo.SuscripcionCiclo c
    ON c.IdEmpresa = e.IdEmpresa AND c.Anio = 2026 AND c.Mes = 8
WHERE e.IdEmpresa IN (
    SELECT DISTINCT IdEmpresa FROM dbo.SuscripcionEvento
    WHERE Tipo = N'SUSPENSION' AND Fecha >= @Desde AND Fecha < @Hasta
)
ORDER BY e.IdEmpresa;
GO
