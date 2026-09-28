/*
  De Laura Pastelería (56) — preparar cobro del ciclo agosto 2026.
  Hoy (día 2) aún es plazo: pendiente de pago, sin suspensión.
  Mañana (día 3) el worker suspende y suma reconexión RD$ 1,000.

  Servicio: USD 83.33
  Reconexión (desde día 3): RD$ 1,000 = USD 16.67 (tasa 60)
  Total a cobrar mañana: USD 100.00

  El 31/08 se marcó pagado a mano sin voucher. Se revierte.
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 56;
DECLARE @IdCiclo INT;
DECLARE @MontoServicio DECIMAL(18, 2) = 83.33;
DECLARE @TasaUsdDop DECIMAL(18, 4) = 60;
DECLARE @CargoReconexDop DECIMAL(18, 2) = 1000;
DECLARE @ReconexUsd DECIMAL(18, 2) = ROUND(@CargoReconexDop / @TasaUsdDop, 2);
DECLARE @Dia INT = DAY(CONVERT(date, GETDATE()));
DECLARE @ConReconexion BIT = CASE WHEN @Dia >= 3 AND @Dia < 30 THEN 1 ELSE 0 END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa AND NombreComercial LIKE N'%Laura%'
)
BEGIN
    RAISERROR(N'No se encontró De Laura Pastelería (empresa 56).', 16, 1);
    RETURN;
END;

SELECT @IdCiclo = IdCiclo
FROM dbo.SuscripcionCiclo
WHERE IdEmpresa = @IdEmpresa AND Anio = 2026 AND Mes = 8;

IF @IdCiclo IS NULL
BEGIN
    RAISERROR(N'No existe ciclo agosto 2026 de Laura.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE dbo.Empresas
SET PagadoServicio = 0,
    MontoServicio = @MontoServicio,
    CargoAdicional = 0,
    CargoReconexionDop = @CargoReconexDop,
    FechaUltimoPago = '2026-08-05T10:39:09.890',
    FechaProximoPago = '2026-09-30',
    EstadoServicio = CASE WHEN @ConReconexion = 1 THEN N'SUSPENDIDA' ELSE N'PENDIENTE_PAGO' END,
    ReconexionPendiente = @ConReconexion
WHERE IdEmpresa = @IdEmpresa;

UPDATE dbo.SuscripcionCiclo
SET Estado = CASE WHEN @ConReconexion = 1 THEN N'VENCIDO' ELSE N'ABIERTO' END,
    Monto = @MontoServicio + CASE WHEN @ConReconexion = 1 THEN @ReconexUsd ELSE 0 END
WHERE IdCiclo = @IdCiclo;

DELETE FROM dbo.SuscripcionCicloDetalle
WHERE IdCiclo = @IdCiclo AND TipoLinea <> N'PLAN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.SuscripcionCicloDetalle
    WHERE IdCiclo = @IdCiclo AND TipoLinea = N'PLAN'
)
BEGIN
    INSERT INTO dbo.SuscripcionCicloDetalle (IdCiclo, TipoLinea, Codigo, Nombre, Monto)
    VALUES (@IdCiclo, N'PLAN', N'PLAN_EMP_56', N'Plan De Laura Pasteleria', @MontoServicio);
END
ELSE
BEGIN
    UPDATE dbo.SuscripcionCicloDetalle
    SET Monto = @MontoServicio,
        Nombre = N'Plan De Laura Pasteleria',
        Codigo = N'PLAN_EMP_56'
    WHERE IdCiclo = @IdCiclo AND TipoLinea = N'PLAN';
END;

IF @ConReconexion = 1
BEGIN
    INSERT INTO dbo.SuscripcionCicloDetalle (IdCiclo, TipoLinea, Codigo, Nombre, Monto)
    VALUES (@IdCiclo, N'RECONEXION', N'RECONEX_56', N'Cargo por reconexión', @ReconexUsd);
END;

UPDATE dbo.PagosEmpresa
SET Estado = N'DESCARTADO',
    Observacion = N'Descartado: ciclo agosto de Laura no tiene voucher aprobado.',
    FechaValidacion = GETDATE(),
    UsuarioValida = N'ADMIN'
WHERE IdEmpresa = @IdEmpresa
  AND Estado IN (N'PENDIENTE', N'PAGO_REPORTADO');

INSERT INTO dbo.SuscripcionEvento (IdEmpresa, IdCiclo, Tipo, Detalle, Canal, Fecha)
VALUES (
    @IdEmpresa,
    @IdCiclo,
    CASE WHEN @ConReconexion = 1 THEN N'SUSPENSION' ELSE N'ESTADO_PENDIENTE_PAGO' END,
    CASE WHEN @ConReconexion = 1
        THEN N'Ciclo agosto vencido. Servicio USD 83.33 + reconexión RD$ 1000 (USD 16.67). Total USD 100.00.'
        ELSE N'Pago agosto revertido (marcado el 31/08 sin voucher). Pendiente USD 83.33. Mañana día 3: suspensión + reconexión RD$ 1000.'
    END,
    N'SISTEMA',
    GETDATE()
);

COMMIT TRAN;

SELECT IdEmpresa, LEFT(NombreComercial, 40) Nombre, EstadoServicio, PagadoServicio,
       ReconexionPendiente, MontoServicio, CargoReconexionDop, FechaUltimoPago, FechaProximoPago
FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa;

SELECT IdCiclo, Anio, Mes, Estado, Monto FROM dbo.SuscripcionCiclo WHERE IdCiclo = @IdCiclo;

SELECT TipoLinea, Codigo, Nombre, Monto
FROM dbo.SuscripcionCicloDetalle WHERE IdCiclo = @IdCiclo
ORDER BY Id;
GO
