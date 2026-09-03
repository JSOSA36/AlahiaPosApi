-- ============================================================
-- Cargos de cobro (CARGOS_PAGO) — AlahiaPos_Prod
-- Requiere autorización explícita para ejecutar en Prod.
-- ============================================================
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CargoPagoRegla', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CargoPagoRegla
    (
        IdCargoPagoRegla INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_CargoPagoRegla PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL,
        Valor DECIMAL(18, 4) NOT NULL,
        GrupoMetodo NVARCHAR(30) NOT NULL,
        Activo BIT NOT NULL
            CONSTRAINT DF_CargoPagoRegla_Activo DEFAULT (1),
        Orden INT NOT NULL
            CONSTRAINT DF_CargoPagoRegla_Orden DEFAULT (1),
        FechaInseccion DATETIME NOT NULL
            CONSTRAINT DF_CargoPagoRegla_Fecha DEFAULT (GETDATE())
    );

    CREATE INDEX IX_CargoPagoRegla_Empresa
        ON dbo.CargoPagoRegla (IdEmpresa, Activo, Orden);
END
GO

IF OBJECT_ID(N'dbo.FacturaCargo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FacturaCargo
    (
        IdFacturaCargo INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_FacturaCargo PRIMARY KEY,
        IdFacturaHeader INT NOT NULL,
        IdCargoPagoRegla INT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL,
        Valor DECIMAL(18, 4) NOT NULL,
        BaseCalculo DECIMAL(18, 2) NOT NULL,
        Monto DECIMAL(18, 2) NOT NULL,
        MetodoPago NVARCHAR(100) NULL,
        IdEmpresa INT NOT NULL,
        FechaInseccion DATETIME NOT NULL
            CONSTRAINT DF_FacturaCargo_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_FacturaCargo_Header
            FOREIGN KEY (IdFacturaHeader)
            REFERENCES dbo.FacturaHeaders (IdFacturaHeader)
    );

    CREATE INDEX IX_FacturaCargo_Header
        ON dbo.FacturaCargo (IdFacturaHeader);
END
GO

IF COL_LENGTH(N'dbo.FacturaHeaders', N'MontoCargo') IS NULL
BEGIN
    ALTER TABLE dbo.FacturaHeaders
        ADD MontoCargo DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_FacturaHeaders_MontoCargo DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'CARGOS_PAGO')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (
        N'CARGOS_PAGO',
        N'Cargos de cobro',
        N'Cargo extra en facturas según método de pago (porcentaje o monto fijo). Ejemplo: 6% en pagos con tarjeta.',
        0,
        1,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Nombre = N'Cargos de cobro',
        Descripcion = N'Cargo extra en facturas según método de pago (porcentaje o monto fijo). Ejemplo: 6% en pagos con tarjeta.',
        Activo = 1
    WHERE Codigo = N'CARGOS_PAGO';
END
GO

DECLARE @IdMod INT = (SELECT TOP 1 Id FROM dbo.Modulos WHERE Codigo = N'CARGOS_PAGO');

IF @IdMod IS NULL
BEGIN
    RAISERROR('No se pudo resolver Id del módulo CARGOS_PAGO', 16, 1);
    RETURN;
END

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdMod, 1, GETDATE()
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdMod
);

UPDATE dbo.Empresa_Modulos
SET Activo = 1, FechaDesactivacion = NULL
WHERE ModuloId = @IdMod;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, @IdMod, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = @IdMod AND pr.IdEmpresa = p.IdEmpresa
);

UPDATE dbo.PerfilRoles SET Activo = 1 WHERE IdModulo = @IdMod;

PRINT 'CARGOS_PAGO listo en AlahiaPos_Prod.';
GO
