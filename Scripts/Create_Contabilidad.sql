-- =====================================================
-- Módulo Contabilidad - Fase 1
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CuentasContables')
BEGIN
    CREATE TABLE CuentasContables (
        IdCuentaContable INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        Codigo NVARCHAR(30) NOT NULL,
        Nombre NVARCHAR(200) NOT NULL,
        TipoCuenta NVARCHAR(30) NOT NULL,
        IdCuentaPadre INT NULL,
        Nivel INT NOT NULL DEFAULT 1,
        PermiteMovimiento BIT NOT NULL DEFAULT 0,
        Activa BIT NOT NULL DEFAULT 1,
        FechaInseccion DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE UNIQUE INDEX UX_CuentasContables_Empresa_Codigo
        ON CuentasContables (IdEmpresa, Codigo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AsientosContables')
BEGIN
    CREATE TABLE AsientosContables (
        IdAsientoContable INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        Numero NVARCHAR(30) NOT NULL,
        Fecha DATETIME NOT NULL,
        Concepto NVARCHAR(500) NOT NULL,
        Estado NVARCHAR(20) NOT NULL DEFAULT 'Confirmado',
        IdUsuario INT NULL,
        OrigenModulo NVARCHAR(50) NOT NULL DEFAULT 'Manual',
        OrigenReferenciaId INT NULL,
        EsAutomatico BIT NOT NULL DEFAULT 0,
        FechaInseccion DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_AsientosContables_Empresa_Fecha
        ON AsientosContables (IdEmpresa, Fecha);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AsientosContablesDetalle')
BEGIN
    CREATE TABLE AsientosContablesDetalle (
        IdAsientoContableDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdAsientoContable INT NOT NULL,
        IdCuentaContable INT NOT NULL,
        Debito DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credito DECIMAL(18,2) NOT NULL DEFAULT 0,
        Referencia NVARCHAR(200) NULL,
        CONSTRAINT FK_AsientoDetalle_Asiento
            FOREIGN KEY (IdAsientoContable)
            REFERENCES AsientosContables(IdAsientoContable)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_AsientoDetalle_Asiento
        ON AsientosContablesDetalle (IdAsientoContable);
END
GO
