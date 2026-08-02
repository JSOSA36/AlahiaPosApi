-- AlahiaPos_Dev — Seed ContabilidadCuentaMapeo empresa 60
USE AlahiaPos_Dev;
GO

DECLARE @IdEmpresa INT = 60;

-- Asegura mapeo default por código de cuenta del catálogo
;WITH Conceptos AS (
  SELECT * FROM (VALUES
    ('CAJA', '1.1.1'),
    ('BANCO', '1.1.2'),
    ('INVENTARIO', '1.1.3'),
    ('CXC', '1.1.4'),
    ('ITBIS_COBRAR', '1.1.5'),
    ('CXP', '2.1.1'),
    ('ITBIS_PAGAR', '2.1.2'),
    ('VENTAS', '4.1'),
    ('OTROS_INGRESOS', '4.3'),
    ('GASTO_OPERATIVO', '5.1'),
    ('COSTO_VENTAS', '6.1')
  ) v(CodigoConcepto, CodigoCuenta)
)
INSERT INTO ContabilidadCuentaMapeo (IdEmpresa, CodigoConcepto, IdCuentaContable, Activo)
SELECT @IdEmpresa, c.CodigoConcepto, cc.IdCuentaContable, 1
FROM Conceptos c
INNER JOIN CuentasContables cc
  ON cc.IdEmpresa = @IdEmpresa AND cc.Codigo = c.CodigoCuenta AND cc.Activa = 1
WHERE NOT EXISTS (
  SELECT 1 FROM ContabilidadCuentaMapeo m
  WHERE m.IdEmpresa = @IdEmpresa AND m.CodigoConcepto = c.CodigoConcepto
);

UPDATE ContabilidadConfiguracion
SET IntegracionAutomatica = 1,
    GenerarCOGSAutomatico = 1,
    SepararAsientoCOGS = 1,
    FechaActualizacion = GETDATE()
WHERE IdEmpresa = @IdEmpresa;

SELECT m.CodigoConcepto, cc.Codigo, cc.Nombre, m.Activo
FROM ContabilidadCuentaMapeo m
JOIN CuentasContables cc ON cc.IdCuentaContable = m.IdCuentaContable
WHERE m.IdEmpresa = @IdEmpresa
ORDER BY m.CodigoConcepto;
GO
