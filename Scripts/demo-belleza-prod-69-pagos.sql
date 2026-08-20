/* Demo 69: cuentas financieras + métodos de pago (efectivo, BHD, Popular, Reservas, tarjeta). */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Destino INT = 69;
DECLARE @IdCaja INT, @IdBhd INT, @IdPop INT, @IdRes INT, @IdTar INT;

BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Efectivo')
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo,
        FechaCreacion, Descripcion
    )
    VALUES (
        @Destino, N'Efectivo', N'CAJA', NULL, NULL, N'CAJA-69',
        1, 3, N'#16a34a', N'cash-outline', N'DOP',
        1, 0, 0, 1, 0, GETDATE(), N'Caja del salón (demo)'
    );

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'BHD')
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo,
        FechaCreacion, Descripcion
    )
    VALUES (
        @Destino, N'BHD', N'BANCO', N'BHD', N'12345678901', N'BHD-69',
        0, 4, N'#f59e0b', N'wallet-outline', N'DOP',
        1, 0, 0, 1, 0, GETDATE(), N'Transferencia BHD (demo)'
    );

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Popular')
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo,
        FechaCreacion, Descripcion
    )
    VALUES (
        @Destino, N'Popular', N'BANCO', N'Popular', N'730123456', N'POPULAR-69',
        0, 4, N'#dc2626', N'business-outline', N'DOP',
        1, 0, 0, 1, 0, GETDATE(), N'Transferencia Popular (demo)'
    );

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Reservas')
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo,
        FechaCreacion, Descripcion
    )
    VALUES (
        @Destino, N'Reservas', N'BANCO', N'Reservas', N'960123456', N'RESERVAS-69',
        0, 4, N'#059669', N'business-outline', N'DOP',
        1, 0, 0, 1, 0, GETDATE(), N'Transferencia Banreservas (demo)'
    );

IF NOT EXISTS (SELECT 1 FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Tarjeta')
    INSERT INTO dbo.CuentaFinanciera (
        IdEmpresa, Nombre, TipoCuenta, Banco, NumeroCuenta, Codigo,
        EsPrincipal, IdTesoreriaSubtipoCuenta, Color, Icono, Moneda,
        Activa, BalanceInicial, SaldoDisponible, PermiteMovimientosManuales, PermiteSaldoNegativo,
        FechaCreacion, Descripcion
    )
    VALUES (
        @Destino, N'Tarjeta', N'TARJETA', NULL, NULL, N'TARJETA-69',
        0, 6, N'#2563eb', N'card-outline', N'DOP',
        1, 0, 0, 1, 0, GETDATE(), N'Pago con tarjetas (demo)'
    );

SELECT @IdCaja = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Efectivo';
SELECT @IdBhd  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'BHD';
SELECT @IdPop  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Popular';
SELECT @IdRes  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Reservas';
SELECT @IdTar  = IdCuentaFinanciera FROM dbo.CuentaFinanciera WHERE IdEmpresa = @Destino AND Nombre = N'Tarjeta';

IF @IdCaja IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'EFECTIVO')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'EFECTIVO', @IdCaja, 1);

IF @IdBhd IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'BHD')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'BHD', @IdBhd, 1);

IF @IdPop IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'POPULAR')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'POPULAR', @IdPop, 1);

IF @IdRes IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'BANRESERVAS')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'BANRESERVAS', @IdRes, 1);

IF @IdTar IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MetodoPagoCuenta WHERE IdEmpresa = @Destino AND MetodoPago = N'TARJETA')
    INSERT INTO dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago, IdCuentaFinanciera, Activo)
    VALUES (@Destino, N'TARJETA', @IdTar, 1);

COMMIT TRAN;

SELECT Nombre, TipoCuenta, Banco, NumeroCuenta, EsPrincipal, Activa
FROM dbo.CuentaFinanciera
WHERE IdEmpresa = @Destino
ORDER BY EsPrincipal DESC, Nombre;

SELECT m.MetodoPago, c.Nombre AS Cuenta, m.Activo
FROM dbo.MetodoPagoCuenta m
INNER JOIN dbo.CuentaFinanciera c ON c.IdCuentaFinanciera = m.IdCuentaFinanciera
WHERE m.IdEmpresa = @Destino
ORDER BY m.MetodoPago;
