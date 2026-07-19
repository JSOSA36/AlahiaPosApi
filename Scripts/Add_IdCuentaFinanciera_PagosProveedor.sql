-- Idempotente: cuenta origen del pago a proveedor
IF COL_LENGTH('dbo.PagosProveedor', 'IdCuentaFinanciera') IS NULL
BEGIN
    ALTER TABLE dbo.PagosProveedor
        ADD IdCuentaFinanciera INT NULL;
END
GO
