-- Cuentas de tesorería demo para Sabor Urbano (IdEmpresa 62).
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa
    FROM dbo.Empresas
    WHERE NombreComercial = N'Sabor Urbano'
);

IF @IdEmpresa IS NULL
BEGIN
    RAISERROR(N'No existe la empresa Sabor Urbano en AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @SubCaja INT = (SELECT TOP 1 IdTesoreriaSubtipoCuenta FROM dbo.TesoreriaSubtipoCuenta WHERE Codigo = N'CAJA_GENERAL');
DECLARE @SubBanco INT = (SELECT TOP 1 IdTesoreriaSubtipoCuenta FROM dbo.TesoreriaSubtipoCuenta WHERE Codigo = N'CTA_CORRIENTE');

IF NOT EXISTS (
    SELECT 1 FROM dbo.CuentaFinanciera
    WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Caja General'
)
BEGIN
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, FechaSaldoInicial,
        PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion
    )
    VALUES (
        @IdEmpresa, N'Caja General', N'CAJA', NULL, NULL, N'CAJA-62',
        0, @SubCaja, N'#16a34a', N'cash-outline', N'DOP',
        1, 25000.00, 25000.00, CAST(GETDATE() AS DATE),
        1, 0, GETDATE(), N'Caja demo Sabor Urbano'
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.CuentaFinanciera
    WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Banco Popular'
)
BEGIN
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, FechaSaldoInicial,
        PermiteMovimientosManuales, PermiteSaldoNegativo, FechaCreacion, Descripcion
    )
    VALUES (
        @IdEmpresa, N'Banco Popular', N'BANCO', N'Popular', N'730000062', N'POPULAR-62',
        1, @SubBanco, N'#dc2626', N'business-outline', N'DOP',
        1, 200000.00, 200000.00, CAST(GETDATE() AS DATE),
        1, 0, GETDATE(), N'Cuenta corriente demo para pago de nómina'
    );
END;

SELECT
    IdCuentaFinanciera,
    Nombre,
    TipoCuenta,
    Banco,
    EsPrincipal,
    SaldoDisponible,
    Activa
FROM dbo.CuentaFinanciera
WHERE IdEmpresa = @IdEmpresa
ORDER BY EsPrincipal DESC, Nombre;
GO
