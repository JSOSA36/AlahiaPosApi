-- =====================================================
-- Fase 3.0 — Infraestructura de integración contable
-- (Eventos desacoplados + configuración por empresa)
-- =====================================================

-- Campos adicionales en asientos automáticos
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AsientosContables') AND name = 'TipoOperacion')
BEGIN
    ALTER TABLE AsientosContables
        ADD TipoOperacion NVARCHAR(30) NOT NULL DEFAULT 'ALTA';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AsientosContables') AND name = 'IdAsientoContableOrigen')
BEGIN
    ALTER TABLE AsientosContables
        ADD IdAsientoContableOrigen INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Asientos_Origen_Automatico')
BEGIN
    CREATE UNIQUE INDEX UX_Asientos_Origen_Automatico
        ON AsientosContables (IdEmpresa, OrigenModulo, OrigenReferenciaId, TipoOperacion)
        WHERE EsAutomatico = 1 AND Estado <> 'Anulado' AND OrigenReferenciaId IS NOT NULL;
END
GO

-- Configuración por empresa
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContabilidadConfiguracion')
BEGIN
    CREATE TABLE ContabilidadConfiguracion (
        IdContabilidadConfiguracion INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        IntegracionAutomatica BIT NOT NULL DEFAULT 0,
        GenerarCOGSAutomatico BIT NOT NULL DEFAULT 1,
        SepararAsientoCOGS BIT NOT NULL DEFAULT 1,
        FechaInseccion DATETIME NOT NULL DEFAULT GETDATE(),
        FechaActualizacion DATETIME NULL,
        CONSTRAINT UX_ContabilidadConfiguracion_Empresa UNIQUE (IdEmpresa)
    );
END
GO

-- Outbox de eventos del ERP (capa neutra)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EventosOutbox')
BEGIN
    CREATE TABLE EventosOutbox (
        IdEventoOutbox INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        TipoEvento NVARCHAR(100) NOT NULL,
        ReferenciaId INT NULL,
        ReferenciaTipo NVARCHAR(100) NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente',
        Intentos INT NOT NULL DEFAULT 0,
        MensajeError NVARCHAR(1000) NULL,
        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
        FechaProcesado DATETIME NULL
    );

    CREATE INDEX IX_EventosOutbox_Estado ON EventosOutbox (Estado, FechaCreacion);
    CREATE INDEX IX_EventosOutbox_Empresa ON EventosOutbox (IdEmpresa, TipoEvento);
END
GO

-- Log de integración contable
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContabilidadIntegracionLog')
BEGIN
    CREATE TABLE ContabilidadIntegracionLog (
        IdContabilidadIntegracionLog INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        IdEventoOutbox INT NULL,
        OrigenModulo NVARCHAR(50) NULL,
        OrigenReferenciaId INT NULL,
        TipoOperacion NVARCHAR(30) NULL,
        Estado NVARCHAR(20) NOT NULL,
        IdAsientoContable INT NULL,
        Mensaje NVARCHAR(1000) NULL,
        Fecha DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_ContabilidadIntegracionLog_Empresa
        ON ContabilidadIntegracionLog (IdEmpresa, Fecha DESC);
END
GO

-- Mapeo concepto → cuenta contable (parametrización)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContabilidadCuentaMapeo')
BEGIN
    CREATE TABLE ContabilidadCuentaMapeo (
        IdContabilidadCuentaMapeo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        CodigoConcepto NVARCHAR(50) NOT NULL,
        IdCuentaContable INT NOT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        CONSTRAINT UX_ContabilidadCuentaMapeo_Empresa_Concepto
            UNIQUE (IdEmpresa, CodigoConcepto)
    );
END
GO
