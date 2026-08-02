SET NOCOUNT ON;
DECLARE @e INT=60, @fin DATETIME='2026-07-21';

SELECT c.TipoCuenta, SUM(d.Debito-d.Credito) SaldoRaw
FROM AsientosContablesDetalle d
JOIN AsientosContables a ON a.IdAsientoContable=d.IdAsientoContable
JOIN CuentasContables c ON c.IdCuentaContable=d.IdCuentaContable
WHERE a.IdEmpresa=@e AND a.Estado<>'Anulado' AND a.Fecha<@fin AND c.PermiteMovimiento=1 AND c.Activa=1
GROUP BY c.TipoCuenta
ORDER BY c.TipoCuenta;

SELECT ISNULL(SUM(d.Debito),0) TotDeb, ISNULL(SUM(d.Credito),0) TotCred
FROM AsientosContablesDetalle d
JOIN AsientosContables a ON a.IdAsientoContable=d.IdAsientoContable
WHERE a.IdEmpresa=@e AND a.Estado<>'Anulado' AND a.Fecha<@fin;
