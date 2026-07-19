-- ============================================================
-- Suscripciones / Cobros SaaS — AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

IF COL_LENGTH('dbo.Empresas', 'EsEmpresaSistema') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD EsEmpresaSistema BIT NOT NULL
        CONSTRAINT DF_Empresas_EsEmpresaSistema DEFAULT(0);
END
GO

-- Migrar estados legacy
UPDATE dbo.Empresas SET EstadoServicio = N'ACTIVA' WHERE EstadoServicio IN (N'ACTIVO', N'ACTIVA');
UPDATE dbo.Empresas SET EstadoServicio = N'PENDIENTE_PAGO' WHERE EstadoServicio IN (N'VENCIDO', N'PENDIENTE', N'PENDIENTE_PAGO');
UPDATE dbo.Empresas SET EstadoServicio = N'SUSPENDIDA' WHERE EstadoServicio IN (N'BLOQUEADO', N'SUSPENDIDA');
GO

IF COL_LENGTH('dbo.PagosEmpresa', 'FechaPago') IS NULL
    ALTER TABLE dbo.PagosEmpresa ADD FechaPago DATETIME NULL;
IF COL_LENGTH('dbo.PagosEmpresa', 'Banco') IS NULL
    ALTER TABLE dbo.PagosEmpresa ADD Banco NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.PagosEmpresa', 'Referencia') IS NULL
    ALTER TABLE dbo.PagosEmpresa ADD Referencia NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.PagosEmpresa', 'IdCiclo') IS NULL
    ALTER TABLE dbo.PagosEmpresa ADD IdCiclo INT NULL;
IF COL_LENGTH('dbo.PagosEmpresa', 'IdUsuarioReporta') IS NULL
    ALTER TABLE dbo.PagosEmpresa ADD IdUsuarioReporta INT NULL;
GO

IF OBJECT_ID('dbo.SuscripcionCiclo') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionCiclo
    (
        IdCiclo           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuscripcionCiclo PRIMARY KEY,
        IdEmpresa         INT NOT NULL,
        Anio              INT NOT NULL,
        Mes               INT NOT NULL,
        FechaGeneracion   DATETIME NOT NULL CONSTRAINT DF_SC_FechaGen DEFAULT(GETDATE()),
        Monto             DECIMAL(18,2) NOT NULL CONSTRAINT DF_SC_Monto DEFAULT(0),
        IdPlan            INT NULL,
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_SC_Estado DEFAULT(N'ABIERTO'),
        -- ABIERTO | PAGADO | VENCIDO
        CONSTRAINT UQ_SuscripcionCiclo_EmpresaMes UNIQUE (IdEmpresa, Anio, Mes)
    );
    CREATE INDEX IX_SuscripcionCiclo_Estado ON dbo.SuscripcionCiclo (Estado, Anio, Mes);
END
GO

IF OBJECT_ID('dbo.SuscripcionEvento') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionEvento
    (
        IdEvento     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuscripcionEvento PRIMARY KEY,
        IdEmpresa    INT NOT NULL,
        IdCiclo      INT NULL,
        Tipo         NVARCHAR(60) NOT NULL,
        Detalle      NVARCHAR(MAX) NULL,
        Canal        NVARCHAR(40) NULL,
        IdUsuario    INT NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        Fecha        DATETIME NOT NULL CONSTRAINT DF_SE_Fecha DEFAULT(GETDATE())
    );
    CREATE INDEX IX_SuscripcionEvento_Empresa ON dbo.SuscripcionEvento (IdEmpresa, Fecha DESC);
END
GO

IF OBJECT_ID('dbo.SuscripcionAvisoLog') IS NULL
BEGIN
    CREATE TABLE dbo.SuscripcionAvisoLog
    (
        IdAvisoLog   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuscripcionAvisoLog PRIMARY KEY,
        IdEmpresa    INT NOT NULL,
        IdCiclo      INT NULL,
        TipoAviso    NVARCHAR(40) NOT NULL,
        Canal        NVARCHAR(40) NOT NULL,
        FechaEnvio   DATETIME NOT NULL CONSTRAINT DF_SAL_Fecha DEFAULT(GETDATE()),
        CONSTRAINT UQ_SuscripcionAvisoLog UNIQUE (IdEmpresa, IdCiclo, TipoAviso, Canal)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Codigo = 'SUSCRIPCIONES_COBROS')
    INSERT INTO Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES ('SUSCRIPCIONES_COBROS', 'Cobros y Suscripciones', 'Panel MacroBits de cobros SaaS', 0, 1, GETDATE());
GO

PRINT 'Create_SuscripcionesCobros_Dev OK';
GO
