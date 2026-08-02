/*
  Migrate_DevSchema_To_Prod_20260730.sql
  ------------------------------------------------------------
  Objetivo: llevar a AlahiaPos_Prod el DDL que ya existe en AlahiaPos_Dev
  y que aún no está en producción (solo cambios aditivos / idempotentes).

  NO copia datos de negocio de Dev → Prod.
  NO elimina columnas ni tablas.

  Revisado contra diff Dev vs Prod del 2026-07-30:
  - Columnas faltantes en tablas existentes
  - Tablas nuevas: CategoriasGasto, Tesoreria extracto/auditoría,
    PagoReclasificacion, Cotizador*

  IMPORTANTE: ejecutar solo con autorización explícita sobre Prod.
*/
USE AlahiaPos_Prod;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
  RAISERROR('Este script solo puede ejecutarse en AlahiaPos_Prod.', 16, 1);
  RETURN;
END
GO

PRINT '=== INICIO migración schema Dev→Prod ===';
GO

/* =====================================================================
   1) CuentaFinanciera
   ===================================================================== */
IF COL_LENGTH('dbo.CuentaFinanciera', 'FechaSaldoInicial') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD FechaSaldoInicial DATE NULL;
GO

IF COL_LENGTH('dbo.CuentaFinanciera', 'PermiteMovimientosManuales') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD PermiteMovimientosManuales BIT NOT NULL
        CONSTRAINT DF_CuentaFin_PermiteManual DEFAULT (1);
GO

IF COL_LENGTH('dbo.CuentaFinanciera', 'RowVersion') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD RowVersion ROWVERSION NOT NULL;
GO

/* =====================================================================
   2) MovimientoFinanciero — conciliación
   ===================================================================== */
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
    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_MovFin_EstadoConciliacion'
)
BEGIN
    ALTER TABLE dbo.MovimientoFinanciero WITH NOCHECK
    ADD CONSTRAINT CK_MovFin_EstadoConciliacion
    CHECK (EstadoConciliacion IN ('PENDIENTE','CONCILIADO','EXCLUIDO','REVERSADO'));
END
GO

/* =====================================================================
   3) TesoreriaConciliacion — campos de centro de trabajo
   ===================================================================== */
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'ToleranciaDiferencia') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD ToleranciaDiferencia DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_TesoreriaConc_Tol DEFAULT (0);
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoConciliado') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD SaldoConciliado DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'Diferencia') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD Diferencia DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'IdUsuarioReapertura') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD IdUsuarioReapertura INT NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'FechaReapertura') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD FechaReapertura DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'MotivoReapertura') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD MotivoReapertura NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoBancoInicial') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD SaldoBancoInicial DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'IdExtractoPrincipal') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD IdExtractoPrincipal INT NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'RowVersion') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD RowVersion ROWVERSION NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesConc_CuentaAbierta'
      AND object_id = OBJECT_ID('dbo.TesoreriaConciliacion')
)
BEGIN
    CREATE UNIQUE INDEX UX_TesConc_CuentaAbierta
        ON dbo.TesoreriaConciliacion (IdEmpresa, IdCuentaFinanciera)
        WHERE Estado IN ('BORRADOR', 'EN_PROCESO');
END
GO

/* =====================================================================
   4) Gastos — categorías + comprobante
   ===================================================================== */
IF OBJECT_ID(N'dbo.CategoriasGasto', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CategoriasGasto (
    IdCategoriaGasto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CategoriasGasto PRIMARY KEY,
    IdEmpresa INT NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_CategoriasGasto_Activo DEFAULT (1),
    Orden INT NOT NULL CONSTRAINT DF_CategoriasGasto_Orden DEFAULT (0),
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CategoriasGasto_Fecha DEFAULT (SYSUTCDATETIME()),
    IdCuentaContable INT NULL,
    CONSTRAINT UQ_CategoriasGasto_Empresa_Nombre UNIQUE (IdEmpresa, Nombre)
  );

  CREATE INDEX IX_CategoriasGasto_Empresa_Activo
    ON dbo.CategoriasGasto (IdEmpresa, Activo, Orden);
END
GO

IF COL_LENGTH('dbo.CategoriasGasto', 'IdCuentaContable') IS NULL
    ALTER TABLE dbo.CategoriasGasto ADD IdCuentaContable INT NULL;
GO

IF COL_LENGTH('dbo.Gastos', 'IdCategoriaGasto') IS NULL
  ALTER TABLE dbo.Gastos ADD IdCategoriaGasto INT NULL;
GO
IF COL_LENGTH('dbo.Gastos', 'TipoComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD TipoComprobante NVARCHAR(80) NULL;
GO
IF COL_LENGTH('dbo.Gastos', 'NumeroComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD NumeroComprobante NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Gastos', 'FechaComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD FechaComprobante DATE NULL;
GO
IF COL_LENGTH('dbo.Gastos', 'RncEmisorComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD RncEmisorComprobante NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Gastos', 'NombreEmisorComprobante') IS NULL
  ALTER TABLE dbo.Gastos ADD NombreEmisorComprobante NVARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Gastos_CategoriasGasto')
AND COL_LENGTH('dbo.Gastos', 'IdCategoriaGasto') IS NOT NULL
BEGIN
  ALTER TABLE dbo.Gastos WITH NOCHECK
  ADD CONSTRAINT FK_Gastos_CategoriasGasto
    FOREIGN KEY (IdCategoriaGasto) REFERENCES dbo.CategoriasGasto (IdCategoriaGasto);
END
GO

/* =====================================================================
   5) Ingresos / Pagos — vínculo a movimiento
   ===================================================================== */
IF COL_LENGTH('dbo.Ingresos', 'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.Ingresos ADD IdMovimientoFinanciero INT NULL;
GO
IF COL_LENGTH('dbo.PagosFacturasClientes', 'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.PagosFacturasClientes ADD IdMovimientoFinanciero INT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Ingresos_IdMovimientoFinanciero'
      AND object_id = OBJECT_ID('dbo.Ingresos')
)
    CREATE INDEX IX_Ingresos_IdMovimientoFinanciero
        ON dbo.Ingresos (IdMovimientoFinanciero)
        WHERE IdMovimientoFinanciero IS NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PagosFacturasClientes_IdMovimientoFinanciero'
      AND object_id = OBJECT_ID('dbo.PagosFacturasClientes')
)
    CREATE INDEX IX_PagosFacturasClientes_IdMovimientoFinanciero
        ON dbo.PagosFacturasClientes (IdMovimientoFinanciero)
        WHERE IdMovimientoFinanciero IS NOT NULL;
GO

/* =====================================================================
   6) ECFEncabezado — Transmission Engine
   ===================================================================== */
IF COL_LENGTH('dbo.ECFEncabezado', 'TransmissionJobId') IS NULL
    ALTER TABLE dbo.ECFEncabezado ADD TransmissionJobId NVARCHAR(100) NULL;
GO

/* =====================================================================
   7) Extracto bancario + auditoría
   ===================================================================== */
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
        Banco NVARCHAR(150) NULL,
        NumeroCuentaBanco NVARCHAR(100) NULL,
        Moneda NVARCHAR(3) NULL,
        SaldoInicial DECIMAL(18,2) NULL,
        SaldoFinal DECIMAL(18,2) NULL,
        TotalDebitos DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtImp_TotalDeb DEFAULT (0),
        TotalCreditos DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtImp_TotalCred DEFAULT (0),
        HashArchivo NVARCHAR(64) NULL,
        IdTesoreriaConciliacion INT NULL,
        SaldoBancoInicialDeclarado DECIMAL(18,2) NULL,
        AdapterUsado NVARCHAR(60) NULL,
        ParserWarnings NVARCHAR(MAX) NULL,
        CONSTRAINT PK_TesoreriaExtractoImport PRIMARY KEY (IdTesoreriaExtractoImport),
        CONSTRAINT FK_TesExtImp_Cuenta FOREIGN KEY (IdCuentaFinanciera)
            REFERENCES dbo.CuentaFinanciera (IdCuentaFinanciera),
        CONSTRAINT FK_TesExtImp_Conciliacion FOREIGN KEY (IdTesoreriaConciliacion)
            REFERENCES dbo.TesoreriaConciliacion (IdTesoreriaConciliacion),
        CONSTRAINT CK_TesExtImp_Estado CHECK (
            Estado IN ('CARGADO','PROCESADO','CERRADO','ANULADO','PREVIEW')
        )
    );
END
GO

-- columnas por si la tabla existía parcial
IF OBJECT_ID('dbo.TesoreriaExtractoImport','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'Banco') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD Banco NVARCHAR(150) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'NumeroCuentaBanco') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD NumeroCuentaBanco NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'Moneda') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD Moneda NVARCHAR(3) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoInicial') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoInicial DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoFinal') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoFinal DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'TotalDebitos') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD TotalDebitos DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtImp_TotalDeb DEFAULT (0);
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'TotalCreditos') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD TotalCreditos DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtImp_TotalCred DEFAULT (0);
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'HashArchivo') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD HashArchivo NVARCHAR(64) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'IdTesoreriaConciliacion') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD IdTesoreriaConciliacion INT NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoBancoInicialDeclarado') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoBancoInicialDeclarado DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'AdapterUsado') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD AdapterUsado NVARCHAR(60) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'ParserWarnings') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoImport ADD ParserWarnings NVARCHAR(MAX) NULL;
END
GO

IF OBJECT_ID('dbo.TesoreriaExtractoLinea', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaExtractoLinea (
        IdTesoreriaExtractoLinea INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaExtractoImport INT NOT NULL,
        FechaMovimiento DATE NOT NULL,
        Descripcion NVARCHAR(500) NULL,
        Referencia NVARCHAR(200) NULL,
        Debito DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtLin_Deb DEFAULT (0),
        Credito DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesExtLin_Cred DEFAULT (0),
        Balance DECIMAL(18,2) NULL,
        EstadoMatch NVARCHAR(40) NOT NULL CONSTRAINT DF_TesExtLin_Match DEFAULT ('PENDIENTE'),
        IdMovimientoFinanciero INT NULL,
        ScoreSugerido DECIMAL(9,4) NULL,
        Observacion NVARCHAR(600) NULL,
        AccionTomada NVARCHAR(60) NULL,
        CategoriaSugerida NVARCHAR(100) NULL,
        EsAutoConciliado BIT NOT NULL CONSTRAINT DF_TesExtLin_Auto DEFAULT (0),
        FechaResolucion DATETIME2 NULL,
        IdUsuarioResolucion INT NULL,
        ReglaMatch NVARCHAR(160) NULL,
        ExplicacionMatch NVARCHAR(1000) NULL,
        ClasificacionLinea NVARCHAR(60) NULL,
        ModuloOrigenSugerido NVARCHAR(80) NULL,
        CONSTRAINT PK_TesoreriaExtractoLinea PRIMARY KEY (IdTesoreriaExtractoLinea),
        CONSTRAINT FK_TesExtLin_Imp FOREIGN KEY (IdTesoreriaExtractoImport)
            REFERENCES dbo.TesoreriaExtractoImport (IdTesoreriaExtractoImport),
        CONSTRAINT FK_TesExtLin_Mov FOREIGN KEY (IdMovimientoFinanciero)
            REFERENCES dbo.MovimientoFinanciero (IdMovimientoFinanciero),
        CONSTRAINT CK_TesExtLin_Match CHECK (
            EstadoMatch IN (
                'PENDIENTE', 'SUGERIDO', 'CONFIRMADO', 'AUTO_CONCILIADO',
                'DESCARTADO', 'IGNORADO', 'NUEVO_MOV', 'RESUELTO',
                'DIFERENCIA', 'AMBIGUO', 'DUPLICADO',
                'RECLASIFICACION_PENDIENTE_CONTABLE'
            )
        )
    );
END
GO

IF OBJECT_ID('dbo.TesoreriaExtractoLinea','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'AccionTomada') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD AccionTomada NVARCHAR(60) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'CategoriaSugerida') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD CategoriaSugerida NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'EsAutoConciliado') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD EsAutoConciliado BIT NOT NULL CONSTRAINT DF_TesExtLin_Auto DEFAULT (0);
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'FechaResolucion') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD FechaResolucion DATETIME2 NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'IdUsuarioResolucion') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD IdUsuarioResolucion INT NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ReglaMatch') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD ReglaMatch NVARCHAR(160) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ExplicacionMatch') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD ExplicacionMatch NVARCHAR(1000) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ClasificacionLinea') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD ClasificacionLinea NVARCHAR(60) NULL;
    IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ModuloOrigenSugerido') IS NULL
        ALTER TABLE dbo.TesoreriaExtractoLinea ADD ModuloOrigenSugerido NVARCHAR(80) NULL;
END
GO

IF OBJECT_ID('dbo.TesoreriaConciliacionAuditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaConciliacionAuditoria (
        IdTesoreriaConciliacionAuditoria INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaConciliacion INT NOT NULL,
        IdEmpresa INT NOT NULL,
        IdUsuario INT NULL,
        Accion NVARCHAR(40) NOT NULL,
        Detalle NVARCHAR(1000) NULL,
        IdTesoreriaExtractoLinea INT NULL,
        IdMovimientoFinanciero INT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_TesConcAud_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TesoreriaConciliacionAuditoria PRIMARY KEY (IdTesoreriaConciliacionAuditoria),
        CONSTRAINT FK_TesConcAud_Conc FOREIGN KEY (IdTesoreriaConciliacion)
            REFERENCES dbo.TesoreriaConciliacion (IdTesoreriaConciliacion)
    );
    CREATE INDEX IX_TesConcAud_Conc
        ON dbo.TesoreriaConciliacionAuditoria (IdTesoreriaConciliacion, Fecha DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesExtImp_EmpCuentaHash'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    CREATE UNIQUE INDEX UX_TesExtImp_EmpCuentaHash
        ON dbo.TesoreriaExtractoImport (IdEmpresa, IdCuentaFinanciera, HashArchivo)
        WHERE HashArchivo IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TesExtImp_Conciliacion'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    CREATE INDEX IX_TesExtImp_Conciliacion
        ON dbo.TesoreriaExtractoImport (IdTesoreriaConciliacion)
        WHERE IdTesoreriaConciliacion IS NOT NULL;
END
GO

/* =====================================================================
   8) PagoReclasificacion
   ===================================================================== */
IF OBJECT_ID('dbo.PagoReclasificacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoReclasificacion
    (
        IdPagoReclasificacion           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoReclasificacion PRIMARY KEY,
        IdEmpresa                       INT NOT NULL,
        IdTesoreriaConciliacion         INT NULL,
        IdTesoreriaExtractoLinea        INT NOT NULL,
        DocumentoTipo                   NVARCHAR(40) NOT NULL CONSTRAINT DF_PagoReclas_DocTipo DEFAULT ('FACTURA'),
        IdDocumento                     INT NULL,
        IdFacturaHeader                 INT NULL,
        IdPagoFacturaCliente            INT NULL,
        IdIngreso                       INT NULL,
        IdMovimientoOriginal            INT NOT NULL,
        IdMovimientoReclasificacion     INT NULL,
        IdCuentaOrigen                  INT NOT NULL,
        IdCuentaDestino                 INT NOT NULL,
        MetodoPagoOriginal              NVARCHAR(50) NOT NULL CONSTRAINT DF_PagoReclas_MetodoOrig DEFAULT (''),
        MetodoPagoEfectivo              NVARCHAR(50) NOT NULL CONSTRAINT DF_PagoReclas_MetodoEfect DEFAULT (''),
        Monto                           DECIMAL(18,2) NOT NULL,
        FechaMovimientoOriginal         DATETIME2 NOT NULL,
        FechaEfectiva                   DATETIME2 NOT NULL,
        FechaContable                   DATETIME2 NOT NULL,
        Tratamiento                     NVARCHAR(40) NOT NULL CONSTRAINT DF_PagoReclas_Trat DEFAULT ('POST_CIERRE'),
        IdCajaAperturaSnapshot          INT NULL,
        IdCajaCierreSnapshot            INT NULL,
        CajaEstabaCerrada               BIT NOT NULL CONSTRAINT DF_PagoReclas_CajaCerrada DEFAULT (0),
        IdPeriodoContableSnapshot       INT NULL,
        PeriodoOriginalCerrado          BIT NOT NULL CONSTRAINT DF_PagoReclas_PerCerrado DEFAULT (0),
        IdUsuario                       INT NOT NULL,
        Motivo                          NVARCHAR(500) NOT NULL,
        Estado                          NVARCHAR(30) NOT NULL CONSTRAINT DF_PagoReclas_Estado DEFAULT ('APLICADA'),
        IdPagoReclasificacionReversaDe  INT NULL,
        IdAsientoContable               INT NULL,
        ClaveIdempotencia               NVARCHAR(120) NULL,
        FechaCreacion                   DATETIME2 NOT NULL CONSTRAINT DF_PagoReclas_Fecha DEFAULT (SYSUTCDATETIME()),
        FechaReversion                  DATETIME2 NULL,
        RowVersion                      ROWVERSION NOT NULL
    );

    CREATE UNIQUE INDEX UX_PagoReclas_ClaveIdempotencia
        ON dbo.PagoReclasificacion (IdEmpresa, ClaveIdempotencia)
        WHERE ClaveIdempotencia IS NOT NULL;
END
GO

/* =====================================================================
   9) Cotizador inteligente (schema; sin seed de precios)
   ===================================================================== */
IF OBJECT_ID(N'dbo.TipoNegocio', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.TipoNegocio (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TipoNegocio PRIMARY KEY,
    Codigo NVARCHAR(40) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Orden INT NOT NULL CONSTRAINT DF_TipoNegocio_Orden DEFAULT(0),
    Activo BIT NOT NULL CONSTRAINT DF_TipoNegocio_Activo DEFAULT(1),
    CONSTRAINT UX_TipoNegocio_Codigo UNIQUE (Codigo)
  );
END
GO

IF OBJECT_ID(N'dbo.ModuloComercial', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloComercial (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloComercial PRIMARY KEY,
    ModuloId INT NOT NULL,
    CategoriaComercial NVARCHAR(80) NOT NULL,
    DescripcionComercial NVARCHAR(500) NULL,
    Nivel NVARCHAR(20) NOT NULL CONSTRAINT DF_ModuloComercial_Nivel DEFAULT(N'BASE'),
    VisibleCotizador BIT NOT NULL CONSTRAINT DF_ModuloComercial_Visible DEFAULT(0),
    ParticipaPrecio BIT NOT NULL CONSTRAINT DF_ModuloComercial_Participa DEFAULT(0),
    PrecioBaseUSD DECIMAL(18,2) NOT NULL CONSTRAINT DF_ModuloComercial_Precio DEFAULT(0),
    Orden INT NOT NULL CONSTRAINT DF_ModuloComercial_Orden DEFAULT(0),
    Icono NVARCHAR(60) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_ModuloComercial_Activo DEFAULT(1),
    CONSTRAINT FK_ModuloComercial_Modulos FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT UX_ModuloComercial_ModuloId UNIQUE (ModuloId)
  );
END
GO

IF OBJECT_ID(N'dbo.ModuloDependencia', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloDependencia (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloDependencia PRIMARY KEY,
    ModuloId INT NOT NULL,
    ModuloRequeridoId INT NOT NULL,
    Tipo NVARCHAR(20) NOT NULL CONSTRAINT DF_ModuloDependencia_Tipo DEFAULT(N'RECOMIENDA'),
    Mensaje NVARCHAR(400) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_ModuloDependencia_Activo DEFAULT(1),
    CONSTRAINT FK_ModuloDependencia_Modulo FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT FK_ModuloDependencia_Requerido FOREIGN KEY (ModuloRequeridoId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT UX_ModuloDependencia UNIQUE (ModuloId, ModuloRequeridoId, Tipo)
  );
END
GO

IF OBJECT_ID(N'dbo.ModuloTipoNegocio', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ModuloTipoNegocio (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ModuloTipoNegocio PRIMARY KEY,
    ModuloId INT NOT NULL,
    TipoNegocioId INT NOT NULL,
    Preseleccionado BIT NOT NULL CONSTRAINT DF_ModuloTipoNegocio_Pre DEFAULT(0),
    CONSTRAINT FK_ModuloTipoNegocio_Modulo FOREIGN KEY (ModuloId) REFERENCES dbo.Modulos(Id),
    CONSTRAINT FK_ModuloTipoNegocio_Tipo FOREIGN KEY (TipoNegocioId) REFERENCES dbo.TipoNegocio(Id),
    CONSTRAINT UX_ModuloTipoNegocio UNIQUE (ModuloId, TipoNegocioId)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizadorParametro', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizadorParametro (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizadorParametro PRIMARY KEY,
    Clave NVARCHAR(80) NOT NULL,
    Valor NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    VisibleCliente BIT NOT NULL CONSTRAINT DF_CotizadorParametro_Visible DEFAULT(0),
    CONSTRAINT UX_CotizadorParametro_Clave UNIQUE (Clave)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizadorTramoDocumento', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizadorTramoDocumento (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizadorTramoDocumento PRIMARY KEY,
    DesdeDocs INT NOT NULL,
    HastaDocs INT NULL,
    CargoUSD DECIMAL(18,2) NOT NULL,
    Etiqueta NVARCHAR(200) NULL,
    Orden INT NOT NULL CONSTRAINT DF_CotizadorTramo_Orden DEFAULT(0),
    Activo BIT NOT NULL CONSTRAINT DF_CotizadorTramo_Activo DEFAULT(1)
  );
END
GO

IF OBJECT_ID(N'dbo.Cotizacion', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Cotizacion (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cotizacion PRIMARY KEY,
    Folio NVARCHAR(40) NOT NULL,
    TipoNegocioCodigo NVARCHAR(40) NULL,
    Usuarios INT NOT NULL,
    Sucursales INT NOT NULL,
    UsaFacturacionElectronica BIT NOT NULL,
    DocumentosElectronicosMensuales INT NOT NULL,
    PrecioMensualUSD DECIMAL(18,2) NOT NULL,
    SnapshotJson NVARCHAR(MAX) NOT NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Cotizacion_Fecha DEFAULT (SYSUTCDATETIME())
  );
END
GO

IF OBJECT_ID(N'dbo.CotizacionDetalle', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizacionDetalle (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizacionDetalle PRIMARY KEY,
    CotizacionId INT NOT NULL,
    ModuloId INT NULL,
    CodigoModulo NVARCHAR(50) NULL,
    Concepto NVARCHAR(120) NOT NULL,
    MontoUSD DECIMAL(18,2) NOT NULL,
    TipoLinea NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_CotizacionDetalle_Cotizacion FOREIGN KEY (CotizacionId) REFERENCES dbo.Cotizacion(Id)
  );
END
GO

IF OBJECT_ID(N'dbo.CotizacionLead', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CotizacionLead (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CotizacionLead PRIMARY KEY,
    CotizacionId INT NULL,
    Tipo NVARCHAR(30) NOT NULL,
    Nombre NVARCHAR(150) NULL,
    Correo NVARCHAR(150) NULL,
    Telefono NVARCHAR(40) NULL,
    Mensaje NVARCHAR(1000) NULL,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CotizacionLead_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_CotizacionLead_Cotizacion FOREIGN KEY (CotizacionId) REFERENCES dbo.Cotizacion(Id)
  );
END
GO

/* =====================================================================
   10) Módulo catálogo (si falta)
   ===================================================================== */
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Modulos')
AND NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = 'CONCILIACION_BANCARIA')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo, PrecioUSD)
    VALUES (
        'CONCILIACION_BANCARIA',
        'Conciliación Bancaria',
        'Centro de trabajo de conciliación banco ↔ ERP',
        1,
        0
    );
END
GO

PRINT '=== FIN migración schema Dev→Prod ===';
GO
