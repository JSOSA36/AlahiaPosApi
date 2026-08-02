-- Cuentas bancarias MacroBits para cobro de suscripción (solo AlahiaPos_Dev).

IF OBJECT_ID('dbo.SuscripcionCuentaCobro', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionCuentaCobro
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Banco NVARCHAR(120) NOT NULL,
        NumeroCuenta NVARCHAR(80) NOT NULL,
        Titular NVARCHAR(200) NOT NULL,
        Cedula NVARCHAR(40) NOT NULL,
        Correo NVARCHAR(200) NULL,
        CuentaEstandar NVARCHAR(80) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Activo DEFAULT (1),
        Orden INT NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Orden DEFAULT (0),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_SuscripcionCuentaCobro_Fecha DEFAULT (GETDATE()),
        FechaModificacion DATETIME NULL
    );
END
GO

-- Seed BHD (Ana de Oleo) si no existe
IF NOT EXISTS (
    SELECT 1 FROM dbo.SuscripcionCuentaCobro
    WHERE NumeroCuenta = N'32754360014'
)
BEGIN
    INSERT INTO dbo.SuscripcionCuentaCobro
        (Banco, NumeroCuenta, Titular, Cedula, Correo, CuentaEstandar, Activo, Orden)
    VALUES
        (N'Banco BHD', N'32754360014', N'ANA DE OLEO', N'00119101053',
         N'nadeysideoleo199130@gmail.com', N'DO06BCBH00000000032754360014', 1, 1);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.SuscripcionCuentaCobro
    WHERE NumeroCuenta = N'0856204060'
)
BEGIN
    INSERT INTO dbo.SuscripcionCuentaCobro
        (Banco, NumeroCuenta, Titular, Cedula, Correo, CuentaEstandar, Activo, Orden)
    VALUES
        (N'Banco Popular', N'0856204060', N'ANA DE OLEO', N'00119101053',
         NULL, NULL, 1, 2);
END
GO
