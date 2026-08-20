-- Completar tesorería + asiento de la nómina 1 (Sabor Urbano) que quedó PAGADA
-- sin movimiento porque la API aún no tenía el código nuevo.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @IdEmpresa INT = 62;
DECLARE @IdNomina INT = 1;
DECLARE @IdCuenta INT;
DECLARE @IdUsuario INT;
DECLARE @Neto DECIMAL(18,2);
DECLARE @SaldoAnterior DECIMAL(18,2);
DECLARE @SaldoNuevo DECIMAL(18,2);
DECLARE @IdMov INT;
DECLARE @Periodo NVARCHAR(80);
DECLARE @IdGastoGL INT;
DECLARE @IdBancoGL INT;
DECLARE @IdAsiento INT;
DECLARE @Numero NVARCHAR(30);

SELECT
    @IdUsuario = p.IdUsuarioPaga,
    @Neto = CAST(ISNULL((SELECT SUM(e.Neto) FROM dbo.NominaProcesoEmpleado e WHERE e.IdNominaProceso = p.IdNominaProceso), 0) AS DECIMAL(18,2)),
    @Periodo = CONVERT(NVARCHAR(10), p.FechaInicio, 103) + N' – ' + CONVERT(NVARCHAR(10), p.FechaFin, 103)
FROM dbo.NominaProceso p
WHERE p.IdEmpresa = @IdEmpresa AND p.IdNominaProceso = @IdNomina;

SELECT TOP 1 @IdCuenta = IdCuentaFinanciera, @SaldoAnterior = SaldoDisponible
FROM dbo.CuentaFinanciera
WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Banco Popular' AND Activa = 1;

SELECT @IdGastoGL = IdCuentaContable FROM dbo.CuentasContables WHERE IdEmpresa = @IdEmpresa AND Codigo = N'5.3';
SELECT @IdBancoGL = IdCuentaContable FROM dbo.CuentasContables WHERE IdEmpresa = @IdEmpresa AND Codigo = N'1.1.2';

IF @IdCuenta IS NULL OR @Neto IS NULL OR @Neto <= 0
BEGIN
    RAISERROR(N'No hay cuenta Popular o neto para completar el pago.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.MovimientoFinanciero
    WHERE IdEmpresa = @IdEmpresa AND ClaveIdempotencia = N'NOMINA-1'
)
BEGIN
    SET @SaldoNuevo = @SaldoAnterior - @Neto;

    INSERT INTO dbo.MovimientoFinanciero (
        IdEmpresa, IdUsuario, IdCuentaOrigen, TipoMovimiento, Categoria,
        ReferenciaId, ReferenciaTipo, Monto, Motivo, Observacion,
        BalanceAnteriorOrigen, BalanceNuevoOrigen,
        FechaMovimiento, FechaRegistro, Estado, EstadoConciliacion, ClaveIdempotencia
    )
    VALUES (
        @IdEmpresa, ISNULL(@IdUsuario, 1), @IdCuenta, N'SALIDA', N'NOMINA',
        @IdNomina, N'NOMINA', @Neto,
        N'Nómina 20260816-20260831 (' + @Periodo + N')',
        N'Salida automática por pago de nómina. Neto ' + CONVERT(NVARCHAR(40), @Neto),
        @SaldoAnterior, @SaldoNuevo,
        GETDATE(), GETDATE(), N'CONFIRMADO', N'PENDIENTE', N'NOMINA-1'
    );

    SET @IdMov = SCOPE_IDENTITY();

    UPDATE dbo.CuentaFinanciera
    SET SaldoDisponible = @SaldoNuevo
    WHERE IdCuentaFinanciera = @IdCuenta;

    UPDATE dbo.NominaProceso
    SET IdCuentaFinanciera = @IdCuenta,
        IdMovimientoFinanciero = @IdMov
    WHERE IdNominaProceso = @IdNomina;
END
ELSE
BEGIN
    SELECT @IdMov = IdMovimientoFinanciero
    FROM dbo.MovimientoFinanciero
    WHERE IdEmpresa = @IdEmpresa AND ClaveIdempotencia = N'NOMINA-1';

    UPDATE dbo.NominaProceso
    SET IdCuentaFinanciera = @IdCuenta,
        IdMovimientoFinanciero = @IdMov
    WHERE IdNominaProceso = @IdNomina AND IdMovimientoFinanciero IS NULL;
END;

IF @IdGastoGL IS NOT NULL AND @IdBancoGL IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.AsientosContables
       WHERE IdEmpresa = @IdEmpresa AND OrigenModulo = N'Nomina' AND OrigenReferenciaId = @IdNomina
         AND Estado <> N'Anulado'
   )
BEGIN
    SET @Numero = N'AS-' + CONVERT(NVARCHAR(4), YEAR(GETDATE())) + N'-00001';

    INSERT INTO dbo.AsientosContables (
        IdEmpresa, Numero, Fecha, Concepto, Estado, IdUsuario,
        OrigenModulo, OrigenReferenciaId, EsAutomatico, FechaInseccion, TipoOperacion
    )
    VALUES (
        @IdEmpresa, @Numero, GETDATE(),
        N'Nómina 20260816-20260831 (' + @Periodo + N')',
        N'Confirmado', ISNULL(@IdUsuario, 1),
        N'Nomina', @IdNomina, 1, GETDATE(), N'ALTA'
    );

    SET @IdAsiento = SCOPE_IDENTITY();

    INSERT INTO dbo.AsientosContablesDetalle (IdAsientoContable, IdCuentaContable, Debito, Credito, Referencia)
    VALUES
        (@IdAsiento, @IdGastoGL, @Neto, 0, N'Gasto de nómina'),
        (@IdAsiento, @IdBancoGL, 0, @Neto, N'Pago de nómina');
END;

SELECT
    p.IdNominaProceso, p.Estado, p.IdCuentaFinanciera, p.IdMovimientoFinanciero
FROM dbo.NominaProceso p WHERE p.IdNominaProceso = @IdNomina;

SELECT mf.IdMovimientoFinanciero, mf.Categoria, mf.Monto, mf.Motivo, cf.Nombre, cf.SaldoDisponible
FROM dbo.MovimientoFinanciero mf
JOIN dbo.CuentaFinanciera cf ON cf.IdCuentaFinanciera = mf.IdCuentaOrigen
WHERE mf.ClaveIdempotencia = N'NOMINA-1';

SELECT a.Numero, a.Concepto, d.Debito, d.Credito, cc.Codigo, cc.Nombre
FROM dbo.AsientosContables a
JOIN dbo.AsientosContablesDetalle d ON d.IdAsientoContable = a.IdAsientoContable
JOIN dbo.CuentasContables cc ON cc.IdCuentaContable = d.IdCuentaContable
WHERE a.IdEmpresa = @IdEmpresa AND a.OrigenModulo = N'Nomina';
GO
