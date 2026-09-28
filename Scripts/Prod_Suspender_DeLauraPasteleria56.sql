-- De Laura Pastelería (Rossana), empresa 56.
-- Revierte el pago marcado a mano el 31/08/2026 y deja el servicio SUSPENDIDO
-- para que al entrar vea la pantalla de servicio suspendido y pueda reportar el pago.
--
-- Uso: ejecútalo en AlahiaPos_Prod cuando quieras cobrarle / cortarle.
-- @ConReconexion = 1 incluye el cargo de reconexión (RD$ 1000). Pon 0 si aún está en plazo (hasta el día 2).
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

DECLARE @IdEmpresa INT = 56;
DECLARE @Anio INT = 2026;
DECLARE @Mes INT = 8;
DECLARE @ConReconexion BIT = 1;
DECLARE @TasaUsdDop DECIMAL(18, 4) = 60;
DECLARE @FechaUltimoPagoReal DATETIME = '2026-08-05T10:39:09.890';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND NombreComercial LIKE N'%Laura%'
)
BEGIN
    RAISERROR(N'No se encontró De Laura Pastelería (empresa 56).', 16, 1);
    RETURN;
END;

DECLARE @MontoPlan DECIMAL(18, 2);
DECLARE @CargoAdicional DECIMAL(18, 2);
DECLARE @CargoReconexDop DECIMAL(18, 2);
DECLARE @ReconexUsd DECIMAL(18, 2);
DECLARE @IdCiclo INT;

SELECT
    @MontoPlan = MontoServicio,
    @CargoAdicional = CargoAdicional,
    @CargoReconexDop = CargoReconexionDop
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SET @ReconexUsd = CASE
    WHEN @ConReconexion = 1 AND @CargoReconexDop > 0
        THEN ROUND(@CargoReconexDop / @TasaUsdDop, 2)
    ELSE 0
END;

SELECT @IdCiclo = IdCiclo
FROM dbo.SuscripcionCiclo
WHERE IdEmpresa = @IdEmpresa AND Anio = @Anio AND Mes = @Mes;

BEGIN TRAN;

UPDATE dbo.Empresas
SET PagadoServicio = 0,
    EstadoServicio = N'SUSPENDIDA',
    ReconexionPendiente = @ConReconexion,
    FechaUltimoPago = @FechaUltimoPagoReal,
    FechaProximoPago = '2026-09-30'
WHERE IdEmpresa = @IdEmpresa;

IF @IdCiclo IS NULL
BEGIN
    INSERT INTO dbo.SuscripcionCiclo (IdEmpresa, Anio, Mes, FechaGeneracion, Monto, IdPlan, Estado)
    VALUES (@IdEmpresa, @Anio, @Mes, GETDATE(), @MontoPlan + @CargoAdicional + @ReconexUsd, NULL, N'VENCIDO');
    SET @IdCiclo = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.SuscripcionCiclo
    SET Estado = N'VENCIDO',
        Monto = @MontoPlan + @CargoAdicional + @ReconexUsd
    WHERE IdCiclo = @IdCiclo;
END;

DELETE FROM dbo.SuscripcionCicloDetalle WHERE IdCiclo = @IdCiclo AND TipoLinea = N'RECONEXION';

IF @ConReconexion = 1 AND @ReconexUsd > 0
    AND NOT EXISTS (
        SELECT 1 FROM dbo.SuscripcionCicloDetalle
        WHERE IdCiclo = @IdCiclo AND TipoLinea = N'RECONEXION'
    )
BEGIN
    INSERT INTO dbo.SuscripcionCicloDetalle (IdCiclo, TipoLinea, Codigo, Nombre, Monto)
    VALUES (
        @IdCiclo,
        N'RECONEXION',
        N'RECONEX_' + CAST(@IdEmpresa AS NVARCHAR(20)),
        N'Cargo por reconexión',
        @ReconexUsd
    );
END;

UPDATE dbo.PagosEmpresa
SET Estado = N'DESCARTADO',
    Observacion = N'Descartado al reactivar cobro / suspensión de De Laura Pastelería.',
    FechaValidacion = GETDATE(),
    UsuarioValida = N'ADMIN'
WHERE IdEmpresa = @IdEmpresa
  AND Estado IN (N'PENDIENTE', N'PAGO_REPORTADO');

INSERT INTO dbo.SuscripcionEvento (IdEmpresa, IdCiclo, Tipo, Detalle, Canal, Fecha)
VALUES (
    @IdEmpresa,
    @IdCiclo,
    N'SUSPENSION',
    N'Suspensión manual para cobrar ciclo agosto. Pago marcado el 31/08 revertido.',
    N'SISTEMA',
    GETDATE()
);

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, EstadoServicio, PagadoServicio, ReconexionPendiente,
       MontoServicio, FechaUltimoPago, FechaProximoPago
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SELECT IdCiclo, Anio, Mes, Estado, Monto
FROM dbo.SuscripcionCiclo
WHERE IdCiclo = @IdCiclo;

SELECT TipoLinea, Codigo, Nombre, Monto
FROM dbo.SuscripcionCicloDetalle
WHERE IdCiclo = @IdCiclo
ORDER BY Id;
GO
