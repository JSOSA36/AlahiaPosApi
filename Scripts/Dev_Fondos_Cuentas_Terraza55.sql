-- AlahiaPos_Dev — Cargar saldo en cuentas de Terraza 27 (empresa 55)
-- para poder emitir gastos. Idempotente.
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
  RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
  RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdUsuario INT = 21;
DECLARE @Monto DECIMAL(18,2) = 100000.00;
DECLARE @Done TABLE (
  IdCuenta INT NOT NULL,
  Nombre NVARCHAR(120) NULL,
  SaldoAnterior DECIMAL(18,2) NOT NULL
);

INSERT INTO @Done (IdCuenta, Nombre, SaldoAnterior)
SELECT cf.IdCuentaFinanciera, cf.Nombre, cf.SaldoDisponible
FROM dbo.CuentaFinanciera cf
WHERE cf.IdEmpresa = @IdEmpresa
  AND cf.Activa = 1
  AND NOT EXISTS (
    SELECT 1
    FROM dbo.MovimientoFinanciero m
    WHERE m.IdEmpresa = @IdEmpresa
      AND m.ClaveIdempotencia = N'QA-FONDOS-GASTO-' + CAST(cf.IdCuentaFinanciera AS NVARCHAR(20))
  );

INSERT INTO dbo.MovimientoFinanciero (
  IdEmpresa, IdUsuario, IdCuentaDestino, TipoMovimiento, Categoria,
  Monto, Motivo, Observacion,
  BalanceAnteriorDestino, BalanceNuevoDestino,
  FechaMovimiento, FechaRegistro, Estado, EstadoConciliacion, ClaveIdempotencia
)
SELECT
  @IdEmpresa,
  @IdUsuario,
  d.IdCuenta,
  N'ENTRADA',
  N'AJUSTE',
  @Monto,
  N'Ajuste QA: fondos para emitir gastos',
  N'Carga de saldo en cuenta ' + ISNULL(d.Nombre, N''),
  d.SaldoAnterior,
  d.SaldoAnterior + @Monto,
  GETDATE(),
  SYSUTCDATETIME(),
  N'CONFIRMADO',
  N'PENDIENTE',
  N'QA-FONDOS-GASTO-' + CAST(d.IdCuenta AS NVARCHAR(20))
FROM @Done d;

UPDATE cf
SET cf.SaldoDisponible = cf.SaldoDisponible + @Monto
FROM dbo.CuentaFinanciera cf
INNER JOIN @Done d ON d.IdCuenta = cf.IdCuentaFinanciera;

SELECT
  cf.IdCuentaFinanciera,
  cf.Nombre,
  cf.TipoCuenta,
  cf.SaldoDisponible,
  mpc.MetodoPago
FROM dbo.CuentaFinanciera cf
LEFT JOIN dbo.MetodoPagoCuenta mpc
  ON mpc.IdCuentaFinanciera = cf.IdCuentaFinanciera AND mpc.IdEmpresa = cf.IdEmpresa AND mpc.Activo = 1
WHERE cf.IdEmpresa = @IdEmpresa AND cf.Activa = 1
ORDER BY cf.Nombre;
GO
