-- Dev only: Bancos / Conciliacion extensions for AlahiaPos_Dev
-- DO NOT run against Prod without explicit authorization

USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* MovimientoFinanciero – conciliación + anulación */
IF COL_LENGTH('dbo.MovimientoFinanciero', 'EstadoConciliacion') IS NULL
BEGIN
    ALTER TABLE dbo.MovimientoFinanciero ADD
        EstadoConciliacion NVARCHAR(20) NOT NULL
            CONSTRAINT DF_MovFin_EstadoConc DEFAULT ('PENDIENTE'),
        IdTesoreriaConciliacion INT NULL,
        FechaConciliacion DATETIME2 NULL,
        IdUsuarioConciliacion INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_MovFin_EstadoConciliacion'
)
BEGIN
    ALTER TABLE dbo.MovimientoFinanciero WITH NOCHECK
    ADD CONSTRAINT CK_MovFin_EstadoConciliacion
    CHECK (EstadoConciliacion IN ('PENDIENTE','CONCILIADO','EXCLUIDO','REVERSADO'));
END
GO

/* CuentaFinanciera – campos de mantenimiento */
IF COL_LENGTH('dbo.CuentaFinanciera', 'FechaSaldoInicial') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD FechaSaldoInicial DATE NULL;
GO

IF COL_LENGTH('dbo.CuentaFinanciera', 'PermiteMovimientosManuales') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD PermiteMovimientosManuales BIT NOT NULL
        CONSTRAINT DF_CuentaFin_PermiteManual DEFAULT (1);
GO

/* TesoreriaConciliacion – cierre / reapertura */
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'ToleranciaDiferencia') IS NULL
BEGIN
    ALTER TABLE dbo.TesoreriaConciliacion ADD
        ToleranciaDiferencia DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_TesoreriaConc_Tol DEFAULT (0),
        SaldoConciliado DECIMAL(18,2) NULL,
        Diferencia DECIMAL(18,2) NULL,
        IdUsuarioReapertura INT NULL,
        FechaReapertura DATETIME2 NULL,
        MotivoReapertura NVARCHAR(500) NULL;
END
GO

/* Import extracto bancario */
IF OBJECT_ID('dbo.TesoreriaExtractoImport', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaExtractoImport (
        IdTesoreriaExtractoImport INT IDENTITY(1,1) NOT NULL,
        IdEmpresa INT NOT NULL,
        IdCuentaFinanciera INT NOT NULL,
        NombreArchivo NVARCHAR(260) NOT NULL,
        Formato NVARCHAR(20) NOT NULL CONSTRAINT DF_TesExtImp_Fmt DEFAULT ('CSV'),
        PeriodoDesde DATE NULL,
        PeriodoHasta DATE NULL,
        IdUsuario INT NULL,
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_TesExtImp_Est DEFAULT ('CARGADO'),
        FechaCarga DATETIME2 NOT NULL CONSTRAINT DF_TesExtImp_Fecha DEFAULT (SYSUTCDATETIME()),
        Observacion NVARCHAR(500) NULL,
        CONSTRAINT PK_TesoreriaExtractoImport PRIMARY KEY (IdTesoreriaExtractoImport),
        CONSTRAINT FK_TesExtImp_Cuenta FOREIGN KEY (IdCuentaFinanciera)
            REFERENCES dbo.CuentaFinanciera (IdCuentaFinanciera),
        CONSTRAINT CK_TesExtImp_Estado CHECK (Estado IN ('CARGADO','PROCESADO','ANULADO'))
    );
END
GO

IF OBJECT_ID('dbo.TesoreriaExtractoLinea', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaExtractoLinea (
        IdTesoreriaExtractoLinea INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaExtractoImport INT NOT NULL,
        FechaMovimiento DATE NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Referencia NVARCHAR(100) NULL,
        Debito DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtLin_Deb DEFAULT (0),
        Credito DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtLin_Cred DEFAULT (0),
        Balance DECIMAL(18,2) NULL,
        EstadoMatch NVARCHAR(20) NOT NULL CONSTRAINT DF_TesExtLin_Match DEFAULT ('PENDIENTE'),
        IdMovimientoFinanciero INT NULL,
        ScoreSugerido DECIMAL(9,4) NULL,
        Observacion NVARCHAR(300) NULL,
        CONSTRAINT PK_TesoreriaExtractoLinea PRIMARY KEY (IdTesoreriaExtractoLinea),
        CONSTRAINT FK_TesExtLin_Imp FOREIGN KEY (IdTesoreriaExtractoImport)
            REFERENCES dbo.TesoreriaExtractoImport (IdTesoreriaExtractoImport) ON DELETE CASCADE,
        CONSTRAINT FK_TesExtLin_Mov FOREIGN KEY (IdMovimientoFinanciero)
            REFERENCES dbo.MovimientoFinanciero (IdMovimientoFinanciero),
        CONSTRAINT CK_TesExtLin_Match CHECK (EstadoMatch IN ('PENDIENTE','SUGERIDO','CONFIRMADO','DESCARTADO','NUEVO_MOV'))
    );
END
GO

PRINT 'Dev_Alter_Bancos_Conciliacion.sql OK';
GO
