/*
================================================================================
  TESORERÍA — Arquitectura ERP (compatible con CuentaFinanciera existente)
  Base objetivo: AlahiaPos_Dev
  Fecha: 2026-07-13

  Principios:
  - No elimina tablas ni columnas existentes
  - Extiende CuentaFinanciera / MovimientoFinanciero / MetodoPagoCuenta
  - Catálogos nuevos para Caja General, Caja Chica, Bancos, etc.
  - Preparado para: CxP, conciliación, integración contable
================================================================================
*/

/* ────────────────────────────────────────────────────────────────────────────
   1. CATÁLOGOS
   ──────────────────────────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.TesoreriaTipoCuenta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaTipoCuenta (
        IdTesoreriaTipoCuenta   INT IDENTITY(1,1) NOT NULL,
        Codigo                  NVARCHAR(30)  NOT NULL,
        Nombre                  NVARCHAR(100) NOT NULL,
        Descripcion             NVARCHAR(300) NULL,
        Orden                   INT NOT NULL CONSTRAINT DF_TesoreriaTipoCuenta_Orden DEFAULT (0),
        Activo                  BIT NOT NULL CONSTRAINT DF_TesoreriaTipoCuenta_Activo DEFAULT (1),
        CONSTRAINT PK_TesoreriaTipoCuenta PRIMARY KEY (IdTesoreriaTipoCuenta),
        CONSTRAINT UQ_TesoreriaTipoCuenta_Codigo UNIQUE (Codigo)
    );
END
GO

IF OBJECT_ID('dbo.TesoreriaSubtipoCuenta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaSubtipoCuenta (
        IdTesoreriaSubtipoCuenta INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaTipoCuenta    INT NOT NULL,
        Codigo                   NVARCHAR(30)  NOT NULL,
        Nombre                   NVARCHAR(100) NOT NULL,
        Descripcion              NVARCHAR(300) NULL,
        Orden                    INT NOT NULL CONSTRAINT DF_TesoreriaSubtipo_Orden DEFAULT (0),
        Activo                   BIT NOT NULL CONSTRAINT DF_TesoreriaSubtipo_Activo DEFAULT (1),
        CONSTRAINT PK_TesoreriaSubtipoCuenta PRIMARY KEY (IdTesoreriaSubtipoCuenta),
        CONSTRAINT UQ_TesoreriaSubtipo_Codigo UNIQUE (Codigo),
        CONSTRAINT FK_TesoreriaSubtipo_Tipo FOREIGN KEY (IdTesoreriaTipoCuenta)
            REFERENCES dbo.TesoreriaTipoCuenta (IdTesoreriaTipoCuenta)
    );
END
GO

IF OBJECT_ID('dbo.TesoreriaTipoDocumento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaTipoDocumento (
        IdTesoreriaTipoDocumento INT IDENTITY(1,1) NOT NULL,
        Codigo                   NVARCHAR(30)  NOT NULL,
        Nombre                   NVARCHAR(100) NOT NULL,
        Naturaleza               NVARCHAR(20)  NOT NULL, -- COBRO | PAGO | TRANSFERENCIA | AJUSTE
        Activo                   BIT NOT NULL CONSTRAINT DF_TesoreriaTipoDoc_Activo DEFAULT (1),
        CONSTRAINT PK_TesoreriaTipoDocumento PRIMARY KEY (IdTesoreriaTipoDocumento),
        CONSTRAINT UQ_TesoreriaTipoDocumento_Codigo UNIQUE (Codigo),
        CONSTRAINT CK_TesoreriaTipoDocumento_Naturaleza CHECK (Naturaleza IN ('COBRO','PAGO','TRANSFERENCIA','AJUSTE'))
    );
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   2. CONFIGURACIÓN POR EMPRESA
   ──────────────────────────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.TesoreriaConfiguracion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaConfiguracion (
        IdTesoreriaConfiguracion INT IDENTITY(1,1) NOT NULL,
        IdEmpresa                INT NOT NULL,
        ModoSaldo                NVARCHAR(20) NOT NULL CONSTRAINT DF_TesoreriaConfig_ModoSaldo DEFAULT ('SALDO_DISPONIBLE'),
        -- SALDO_DISPONIBLE = campo vivo (actual POS) | CALCULADO = solo movimientos
        PermitirSaldoNegativo    BIT NOT NULL CONSTRAINT DF_TesoreriaConfig_SaldoNeg DEFAULT (0),
        RequiereConciliacionBanco BIT NOT NULL CONSTRAINT DF_TesoreriaConfig_Conciliacion DEFAULT (0),
        IdCuentaCajaGeneral      INT NULL,
        IdCuentaCajaChicaDefault INT NULL,
        FechaCreacion            DATETIME2 NOT NULL CONSTRAINT DF_TesoreriaConfig_Fecha DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion       DATETIME2 NULL,
        CONSTRAINT PK_TesoreriaConfiguracion PRIMARY KEY (IdTesoreriaConfiguracion),
        CONSTRAINT UQ_TesoreriaConfiguracion_Empresa UNIQUE (IdEmpresa),
        CONSTRAINT CK_TesoreriaConfig_ModoSaldo CHECK (ModoSaldo IN ('SALDO_DISPONIBLE','CALCULADO'))
    );
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   3. MAPEO TESORERÍA → CONTABILIDAD
   ──────────────────────────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.TesoreriaCuentaContableMapeo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaCuentaContableMapeo (
        IdTesoreriaCuentaContableMapeo INT IDENTITY(1,1) NOT NULL,
        IdEmpresa                      INT NOT NULL,
        IdCuentaFinanciera             INT NOT NULL,
        IdCuentaContable               INT NOT NULL,
        TipoMapeo                      NVARCHAR(30) NOT NULL CONSTRAINT DF_TesoreriaMap_Tipo DEFAULT ('PRINCIPAL'),
        Activo                         BIT NOT NULL CONSTRAINT DF_TesoreriaMap_Activo DEFAULT (1),
        FechaCreacion                  DATETIME2 NOT NULL CONSTRAINT DF_TesoreriaMap_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TesoreriaCuentaContableMapeo PRIMARY KEY (IdTesoreriaCuentaContableMapeo),
        CONSTRAINT UQ_TesoreriaMap_Cuenta UNIQUE (IdEmpresa, IdCuentaFinanciera, TipoMapeo),
        CONSTRAINT FK_TesoreriaMap_CuentaFin FOREIGN KEY (IdCuentaFinanciera)
            REFERENCES dbo.CuentaFinanciera (IdCuentaFinanciera),
        CONSTRAINT FK_TesoreriaMap_CuentaCont FOREIGN KEY (IdCuentaContable)
            REFERENCES dbo.CuentasContables (IdCuentaContable)
    );
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   4. CONCILIACIÓN BANCARIA (estructura base)
   ──────────────────────────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.TesoreriaConciliacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaConciliacion (
        IdTesoreriaConciliacion INT IDENTITY(1,1) NOT NULL,
        IdEmpresa               INT NOT NULL,
        IdCuentaFinanciera      INT NOT NULL,
        PeriodoDesde            DATE NOT NULL,
        PeriodoHasta            DATE NOT NULL,
        SaldoLibrosInicial      DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesoreriaConc_SaldoIni DEFAULT (0),
        SaldoLibrosFinal        DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesoreriaConc_SaldoFin DEFAULT (0),
        SaldoBancoFinal         DECIMAL(18,2) NULL,
        Estado                  NVARCHAR(20) NOT NULL CONSTRAINT DF_TesoreriaConc_Estado DEFAULT ('BORRADOR'),
        IdUsuario               INT NULL,
        FechaCreacion           DATETIME2 NOT NULL CONSTRAINT DF_TesoreriaConc_Fecha DEFAULT (SYSUTCDATETIME()),
        FechaCierre             DATETIME2 NULL,
        Observacion             NVARCHAR(500) NULL,
        CONSTRAINT PK_TesoreriaConciliacion PRIMARY KEY (IdTesoreriaConciliacion),
        CONSTRAINT FK_TesoreriaConc_Cuenta FOREIGN KEY (IdCuentaFinanciera)
            REFERENCES dbo.CuentaFinanciera (IdCuentaFinanciera),
        CONSTRAINT CK_TesoreriaConc_Estado CHECK (Estado IN ('BORRADOR','EN_PROCESO','CERRADA','ANULADA'))
    );
END
GO

IF OBJECT_ID('dbo.TesoreriaConciliacionLinea', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaConciliacionLinea (
        IdTesoreriaConciliacionLinea INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaConciliacion      INT NOT NULL,
        FechaMovimiento              DATE NOT NULL,
        Descripcion                  NVARCHAR(250) NULL,
        ReferenciaBanco              NVARCHAR(100) NULL,
        Monto                        DECIMAL(18,2) NOT NULL,
        TipoLinea                    NVARCHAR(10) NOT NULL, -- DEBITO | CREDITO
        Conciliado                   BIT NOT NULL CONSTRAINT DF_TesoreriaConcLinea_Conc DEFAULT (0),
        IdMovimientoFinanciero       INT NULL,
        CONSTRAINT PK_TesoreriaConciliacionLinea PRIMARY KEY (IdTesoreriaConciliacionLinea),
        CONSTRAINT FK_TesoreriaConcLinea_Conc FOREIGN KEY (IdTesoreriaConciliacion)
            REFERENCES dbo.TesoreriaConciliacion (IdTesoreriaConciliacion) ON DELETE CASCADE,
        CONSTRAINT FK_TesoreriaConcLinea_Mov FOREIGN KEY (IdMovimientoFinanciero)
            REFERENCES dbo.MovimientoFinanciero (IdMovimientoFinanciero),
        CONSTRAINT CK_TesoreriaConcLinea_Tipo CHECK (TipoLinea IN ('DEBITO','CREDITO'))
    );
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   5. EXTENSIÓN CuentaFinanciera (backward compatible)
   ──────────────────────────────────────────────────────────────────────────── */

IF COL_LENGTH('dbo.CuentaFinanciera', 'Codigo') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD Codigo NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'IdTesoreriaSubtipoCuenta') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD IdTesoreriaSubtipoCuenta INT NULL;
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'Moneda') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD Moneda NVARCHAR(3) NOT NULL CONSTRAINT DF_CuentaFinanciera_Moneda DEFAULT ('DOP');
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'IdCuentaContable') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD IdCuentaContable INT NULL;
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'EsPrincipal') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD EsPrincipal BIT NOT NULL CONSTRAINT DF_CuentaFinanciera_EsPrincipal DEFAULT (0);
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'PermiteSaldoNegativo') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD PermiteSaldoNegativo BIT NOT NULL CONSTRAINT DF_CuentaFinanciera_SaldoNeg DEFAULT (0);
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'Descripcion') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD Descripcion NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'FechaUltimaConciliacion') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD FechaUltimaConciliacion DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.CuentaFinanciera', 'UltimoSaldoConciliado') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD UltimoSaldoConciliado DECIMAL(18,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CuentaFinanciera_TesoreriaSubtipo')
BEGIN
    ALTER TABLE dbo.CuentaFinanciera
        ADD CONSTRAINT FK_CuentaFinanciera_TesoreriaSubtipo
        FOREIGN KEY (IdTesoreriaSubtipoCuenta) REFERENCES dbo.TesoreriaSubtipoCuenta (IdTesoreriaSubtipoCuenta);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CuentaFinanciera_CuentaContable')
BEGIN
    ALTER TABLE dbo.CuentaFinanciera
        ADD CONSTRAINT FK_CuentaFinanciera_CuentaContable
        FOREIGN KEY (IdCuentaContable) REFERENCES dbo.CuentasContables (IdCuentaContable);
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   6. EXTENSIÓN MovimientoFinanciero
   ──────────────────────────────────────────────────────────────────────────── */

IF COL_LENGTH('dbo.MovimientoFinanciero', 'IdMovimientoPar') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD IdMovimientoPar INT NULL;
GO
IF COL_LENGTH('dbo.MovimientoFinanciero', 'NumeroComprobante') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD NumeroComprobante NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.MovimientoFinanciero', 'Estado') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_MovimientoFinanciero_Estado DEFAULT ('CONFIRMADO');
GO
IF COL_LENGTH('dbo.MovimientoFinanciero', 'ClaveIdempotencia') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD ClaveIdempotencia NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.MovimientoFinanciero', 'FechaRegistro') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_MovimientoFinanciero_FechaReg DEFAULT (SYSUTCDATETIME());
GO
IF COL_LENGTH('dbo.MovimientoFinanciero', 'IdTesoreriaTipoDocumento') IS NULL
    ALTER TABLE dbo.MovimientoFinanciero ADD IdTesoreriaTipoDocumento INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MovimientoFinanciero_Par')
BEGIN
    ALTER TABLE dbo.MovimientoFinanciero
        ADD CONSTRAINT FK_MovimientoFinanciero_Par
        FOREIGN KEY (IdMovimientoPar) REFERENCES dbo.MovimientoFinanciero (IdMovimientoFinanciero);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MovimientoFinanciero_TipoDoc')
BEGIN
    ALTER TABLE dbo.MovimientoFinanciero
        ADD CONSTRAINT FK_MovimientoFinanciero_TipoDoc
        FOREIGN KEY (IdTesoreriaTipoDocumento) REFERENCES dbo.TesoreriaTipoDocumento (IdTesoreriaTipoDocumento);
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   7. ÍNDICES
   ──────────────────────────────────────────────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimientoFinanciero_Referencia')
    CREATE NONCLUSTERED INDEX IX_MovimientoFinanciero_Referencia
        ON dbo.MovimientoFinanciero (ReferenciaTipo, ReferenciaId)
        INCLUDE (Monto, TipoMovimiento, IdEmpresa);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimientoFinanciero_Empresa_Fecha')
    CREATE NONCLUSTERED INDEX IX_MovimientoFinanciero_Empresa_Fecha
        ON dbo.MovimientoFinanciero (IdEmpresa, FechaMovimiento DESC)
        INCLUDE (TipoMovimiento, Monto, IdCuentaOrigen, IdCuentaDestino);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimientoFinanciero_Idempotencia')
BEGIN
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;
    CREATE UNIQUE NONCLUSTERED INDEX IX_MovimientoFinanciero_Idempotencia
        ON dbo.MovimientoFinanciero (IdEmpresa, ClaveIdempotencia)
        WHERE ClaveIdempotencia IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MetodoPagoCuenta_Empresa_Metodo')
    CREATE NONCLUSTERED INDEX IX_MetodoPagoCuenta_Empresa_Metodo
        ON dbo.MetodoPagoCuenta (IdEmpresa, MetodoPago)
        INCLUDE (IdCuentaFinanciera, Activo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CuentaFinanciera_Empresa_Activa')
    CREATE NONCLUSTERED INDEX IX_CuentaFinanciera_Empresa_Activa
        ON dbo.CuentaFinanciera (IdEmpresa, Activa)
        INCLUDE (TipoCuenta, Nombre, SaldoDisponible);
GO

/* ────────────────────────────────────────────────────────────────────────────
   8. FUNCIÓN: calcular saldo desde movimientos
   ──────────────────────────────────────────────────────────────────────────── */

CREATE OR ALTER FUNCTION dbo.fn_Tesoreria_CalcularSaldo (@IdCuentaFinanciera INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @BalanceInicial DECIMAL(18,2);
    DECLARE @Entradas DECIMAL(18,2);
    DECLARE @Salidas DECIMAL(18,2);

    SELECT @BalanceInicial = ISNULL(BalanceInicial, 0)
    FROM dbo.CuentaFinanciera
    WHERE IdCuentaFinanciera = @IdCuentaFinanciera;

    SELECT @Entradas = ISNULL(SUM(Monto), 0)
    FROM dbo.MovimientoFinanciero
    WHERE IdCuentaDestino = @IdCuentaFinanciera
      AND Estado = 'CONFIRMADO';

    SELECT @Salidas = ISNULL(SUM(Monto), 0)
    FROM dbo.MovimientoFinanciero
    WHERE IdCuentaOrigen = @IdCuentaFinanciera
      AND Estado = 'CONFIRMADO';

    RETURN ISNULL(@BalanceInicial, 0) + ISNULL(@Entradas, 0) - ISNULL(@Salidas, 0);
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   9. VISTA: saldos unificados
   ──────────────────────────────────────────────────────────────────────────── */

CREATE OR ALTER VIEW dbo.vw_TesoreriaSaldos
AS
SELECT
    cf.IdCuentaFinanciera,
    cf.IdEmpresa,
    cf.Nombre,
    cf.Codigo,
    cf.TipoCuenta,
    st.Codigo AS SubtipoCodigo,
    st.Nombre AS SubtipoNombre,
    cf.Moneda,
    cf.BalanceInicial,
    cf.SaldoDisponible,
    dbo.fn_Tesoreria_CalcularSaldo(cf.IdCuentaFinanciera) AS SaldoCalculado,
    cf.SaldoDisponible - dbo.fn_Tesoreria_CalcularSaldo(cf.IdCuentaFinanciera) AS DiferenciaSaldo,
    cf.Activa,
    cf.EsPrincipal,
    cf.IdCuentaContable,
    cf.FechaUltimaConciliacion
FROM dbo.CuentaFinanciera cf
LEFT JOIN dbo.TesoreriaSubtipoCuenta st ON st.IdTesoreriaSubtipoCuenta = cf.IdTesoreriaSubtipoCuenta;
GO

/* ────────────────────────────────────────────────────────────────────────────
   10. SP: sincronizar SaldoDisponible desde movimientos
   ──────────────────────────────────────────────────────────────────────────── */

CREATE OR ALTER PROCEDURE dbo.sp_Tesoreria_SincronizarSaldos
    @IdEmpresa INT = NULL,
    @IdCuentaFinanciera INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE cf
    SET cf.SaldoDisponible = dbo.fn_Tesoreria_CalcularSaldo(cf.IdCuentaFinanciera)
    FROM dbo.CuentaFinanciera cf
    WHERE (@IdEmpresa IS NULL OR cf.IdEmpresa = @IdEmpresa)
      AND (@IdCuentaFinanciera IS NULL OR cf.IdCuentaFinanciera = @IdCuentaFinanciera);
END
GO

/* ────────────────────────────────────────────────────────────────────────────
   11. SEED CATÁLOGOS
   ──────────────────────────────────────────────────────────────────────────── */

MERGE dbo.TesoreriaTipoCuenta AS t
USING (VALUES
    ('CAJA',    'Caja',    'Cuentas de efectivo operativo', 1),
    ('BANCO',   'Banco',   'Cuentas bancarias', 2),
    ('TARJETA', 'Tarjeta', 'Cuentas de liquidez por tarjeta / pasarela', 3)
) AS s (Codigo, Nombre, Descripcion, Orden)
ON t.Codigo = s.Codigo
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Descripcion, Orden) VALUES (s.Codigo, s.Nombre, s.Descripcion, s.Orden);
GO

INSERT INTO dbo.TesoreriaSubtipoCuenta (IdTesoreriaTipoCuenta, Codigo, Nombre, Descripcion, Orden)
SELECT tc.IdTesoreriaTipoCuenta, s.Codigo, s.Nombre, s.Descripcion, s.Orden
FROM (VALUES
    ('CAJA_GENERAL', 'Caja General', 'Caja principal de la empresa', 'CAJA', 1),
    ('CAJA_CHICA',   'Caja Chica',  'Fondo fijo / caja chica',      'CAJA', 2),
    ('CAJA_POS',     'Caja POS',    'Caja vinculada a punto de venta', 'CAJA', 3),
    ('CTA_CORRIENTE','Cuenta Corriente', 'Cuenta bancaria corriente', 'BANCO', 1),
    ('CTA_AHORRO',   'Cuenta de Ahorro', 'Cuenta de ahorro',         'BANCO', 2),
    ('TARJETA_LIQUIDEZ','Tarjeta Liquidez', 'Liquidación tarjetas',  'TARJETA', 1),
    ('PASARELA',     'Pasarela Digital', 'Billeteras / pasarelas',   'TARJETA', 2)
) AS s (Codigo, Nombre, Descripcion, TipoCodigo, Orden)
INNER JOIN dbo.TesoreriaTipoCuenta tc ON tc.Codigo = s.TipoCodigo
WHERE NOT EXISTS (SELECT 1 FROM dbo.TesoreriaSubtipoCuenta x WHERE x.Codigo = s.Codigo);
GO

MERGE dbo.TesoreriaTipoDocumento AS t
USING (VALUES
    ('COBRO_VENTA',      'Cobro por venta',           'COBRO'),
    ('COBRO_CXC',        'Cobro cuentas por cobrar',  'COBRO'),
    ('INGRESO_MANUAL',   'Ingreso manual',            'COBRO'),
    ('PAGO_GASTO',       'Pago de gasto',             'PAGO'),
    ('PAGO_PROVEEDOR',   'Pago a proveedor',          'PAGO'),
    ('PAGO_CXP',         'Pago cuentas por pagar',    'PAGO'),
    ('TRANSFERENCIA',    'Transferencia interna',     'TRANSFERENCIA'),
    ('AJUSTE',           'Ajuste manual',             'AJUSTE')
) AS s (Codigo, Nombre, Naturaleza)
ON t.Codigo = s.Codigo
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Naturaleza) VALUES (s.Codigo, s.Nombre, s.Naturaleza);
GO

/* Mapear subtipos a cuentas existentes según TipoCuenta + nombre */
UPDATE cf
SET cf.IdTesoreriaSubtipoCuenta = st.IdTesoreriaSubtipoCuenta
FROM dbo.CuentaFinanciera cf
INNER JOIN dbo.TesoreriaSubtipoCuenta st ON st.Codigo = CASE
    WHEN cf.TipoCuenta = 'CAJA' AND (cf.Nombre LIKE '%CHICA%' OR cf.Nombre LIKE '%FIJO%') THEN 'CAJA_CHICA'
    WHEN cf.TipoCuenta = 'CAJA' THEN 'CAJA_GENERAL'
    WHEN cf.TipoCuenta = 'BANCO' THEN 'CTA_CORRIENTE'
    WHEN cf.TipoCuenta = 'TARJETA' THEN 'TARJETA_LIQUIDEZ'
    ELSE 'CAJA_GENERAL'
END
WHERE cf.IdTesoreriaSubtipoCuenta IS NULL;
GO

/* Códigos automáticos para cuentas sin código */
UPDATE cf
SET cf.Codigo = CONCAT(cf.TipoCuenta, '-', cf.IdCuentaFinanciera)
FROM dbo.CuentaFinanciera cf
WHERE cf.Codigo IS NULL OR LTRIM(RTRIM(cf.Codigo)) = '';
GO

/* Configuración por empresa existente */
INSERT INTO dbo.TesoreriaConfiguracion (IdEmpresa, ModoSaldo, PermitirSaldoNegativo)
SELECT e.IdEmpresa, 'SALDO_DISPONIBLE', 0
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TesoreriaConfiguracion tc WHERE tc.IdEmpresa = e.IdEmpresa
);
GO

/* Sincronizar saldos en dev */
EXEC dbo.sp_Tesoreria_SincronizarSaldos;
GO

PRINT 'Tesorería: arquitectura aplicada correctamente.';
GO
