-- Dev: marcar formas de pago que se usan al cotejar cobros de ARS.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.MetodoPagoCuenta', N'EsCobroArs') IS NULL
BEGIN
    ALTER TABLE dbo.MetodoPagoCuenta
        ADD EsCobroArs BIT NOT NULL
            CONSTRAINT DF_MetodoPagoCuenta_EsCobroArs DEFAULT (0);
END;
GO

-- Clínica Dental Sena: bancos ya vinculados a cuenta financiera.
UPDATE m
SET m.EsCobroArs = 1
FROM dbo.MetodoPagoCuenta m
WHERE m.IdEmpresa = 60
  AND m.Activo = 1
  AND m.IdCuentaFinanciera > 0
  AND UPPER(LTRIM(RTRIM(m.MetodoPago))) NOT IN (N'EFECTIVO', N'ARS');

SELECT IdEmpresa, MetodoPago, IdCuentaFinanciera, EsCobroArs
FROM dbo.MetodoPagoCuenta
WHERE IdEmpresa = 60
ORDER BY MetodoPago;
