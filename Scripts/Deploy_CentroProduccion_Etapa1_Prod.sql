-- ============================================================
-- Centro de Producción — Etapa 1 — Deploy AlahiaPos_Prod
-- Idempotente. Sin datos demo.
--
-- Orden de ejecución:
--   1) Este script (DDL + seeds catálogo/flujo/módulos + config segura)
--   2) Deploy API / FE por separado (no incluido aquí)
--   3) Opcional: Deploy_Parametro_EstatusOrdenes_Prod.sql por empresa
--   4) Licenciar módulos y activar motor por empresa según negocio
--
-- Seguridad:
--   - @CONFIRMO_PROD debe ser 1 y DB_NAME() = AlahiaPos_Prod
--   - Por defecto NO licencia módulos ni activa el motor en todas las empresas
-- ============================================================
USE AlahiaPos_Prod;
GO

SET NOCOUNT ON;
GO

-- >>> Poner en 1 solo con autorización explícita para ejecutar en Prod <<<
DECLARE @CONFIRMO_PROD BIT = 0;

IF DB_NAME() <> N'AlahiaPos_Prod' OR ISNULL(@CONFIRMO_PROD, 0) <> 1
BEGIN
    RAISERROR(
        N'Abortado: este script solo corre en AlahiaPos_Prod con @CONFIRMO_PROD = 1. API/FE se despliegan aparte.',
        16, 1);
    SET NOEXEC ON;
END
GO

-- ------------------------------------------------------------
-- 1) Catálogo TipoTrabajo
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionTipoTrabajo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionTipoTrabajo
    (
        IdTipoTrabajo   INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionTipoTrabajo PRIMARY KEY,
        Codigo          NVARCHAR(40)  NOT NULL,
        Nombre          NVARCHAR(120) NOT NULL,
        Descripcion     NVARCHAR(400) NULL,
        Activo          BIT NOT NULL
            CONSTRAINT DF_ProduccionTipoTrabajo_Activo DEFAULT (1),
        FechaCreacion   DATETIME NOT NULL
            CONSTRAINT DF_ProduccionTipoTrabajo_Fecha DEFAULT (GETDATE()),
        CONSTRAINT UQ_ProduccionTipoTrabajo_Codigo UNIQUE (Codigo)
    );
END
GO

-- ------------------------------------------------------------
-- 2) Configuración por empresa
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionConfiguracionEmpresa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionConfiguracionEmpresa
    (
        IdEmpresa                       INT NOT NULL
            CONSTRAINT PK_ProduccionConfiguracionEmpresa PRIMARY KEY,
        Activo                          BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Activo DEFAULT (0),
        UsarEstaciones                  BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_UsarEstaciones DEFAULT (0),
        UsarEstadosPorItem              BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_UsarEstadosPorItem DEFAULT (0),
        SonidoActivo                    BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Sonido DEFAULT (1),
        TiempoAdvertenciaSegDefault     INT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Adv DEFAULT (600),
        TiempoCriticoSegDefault         INT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Crit DEFAULT (900),
        PermitirCompletarDesdeEstacion  BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_CompletarEst DEFAULT (1),
        IdEstacionPredeterminada        INT NULL,
        ModoOscuroDefault               BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Oscuro DEFAULT (1),
        MostrarNombreCliente            BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Nombre DEFAULT (1),
        MostrarUsuarioSolicita          BIT NOT NULL
            CONSTRAINT DF_ProduccionCfg_Usuario DEFAULT (1),
        FechaActualizacion              DATETIME NOT NULL
            CONSTRAINT DF_ProduccionCfg_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_ProduccionCfg_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );
END
GO

-- ------------------------------------------------------------
-- 3) Flujo (incluye SlaModoInicio desde el inicio)
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionFlujo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionFlujo
    (
        IdFlujo                     INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionFlujo PRIMARY KEY,
        IdEmpresa                   INT NULL, -- NULL = plantilla sistema
        TipoTrabajoCodigo           NVARCHAR(40) NOT NULL,
        Nombre                      NVARCHAR(120) NOT NULL,
        Activo                      BIT NOT NULL
            CONSTRAINT DF_ProduccionFlujo_Activo DEFAULT (1),
        Version                     INT NOT NULL
            CONSTRAINT DF_ProduccionFlujo_Version DEFAULT (1),
        SlaObjetivoSegundos         INT NOT NULL,
        SlaAdvertenciaSegundos      INT NOT NULL,
        SlaModoInicio               NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionFlujo_SlaModo DEFAULT (N'CREACION'),
        FechaCreacion               DATETIME NOT NULL
            CONSTRAINT DF_ProduccionFlujo_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_ProduccionFlujo_Tipo
            FOREIGN KEY (TipoTrabajoCodigo) REFERENCES dbo.ProduccionTipoTrabajo (Codigo),
        CONSTRAINT FK_ProduccionFlujo_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa),
        CONSTRAINT CK_ProduccionFlujo_Sla
            CHECK (SlaObjetivoSegundos > 0 AND SlaAdvertenciaSegundos > 0
                   AND SlaAdvertenciaSegundos <= SlaObjetivoSegundos),
        CONSTRAINT CK_ProduccionFlujo_SlaModoInicio
            CHECK (SlaModoInicio IN (N'CREACION', N'INICIO_PREPARACION'))
    );

    CREATE INDEX IX_ProduccionFlujo_Empresa_Tipo
        ON dbo.ProduccionFlujo (IdEmpresa, TipoTrabajoCodigo, Activo);
END
GO

-- Upgrade: columna SlaModoInicio si la tabla ya existía sin ella
IF OBJECT_ID(N'dbo.ProduccionFlujo', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ProduccionFlujo', N'SlaModoInicio') IS NULL
BEGIN
    ALTER TABLE dbo.ProduccionFlujo
        ADD SlaModoInicio NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionFlujo_SlaModo DEFAULT (N'CREACION');
END
GO

IF OBJECT_ID(N'dbo.ProduccionFlujo', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.check_constraints
       WHERE name = N'CK_ProduccionFlujo_SlaModoInicio'
   )
BEGIN
    ALTER TABLE dbo.ProduccionFlujo
        ADD CONSTRAINT CK_ProduccionFlujo_SlaModoInicio
        CHECK (SlaModoInicio IN (N'CREACION', N'INICIO_PREPARACION'));
END
GO

-- ------------------------------------------------------------
-- 4) Estados del flujo
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionFlujoEstado', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionFlujoEstado
    (
        IdFlujoEstado           INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionFlujoEstado PRIMARY KEY,
        IdFlujo                 INT NOT NULL,
        Codigo                  NVARCHAR(40) NOT NULL,
        NombreVisible           NVARCHAR(80) NOT NULL,
        Orden                   INT NOT NULL,
        EsInicial               BIT NOT NULL
            CONSTRAINT DF_ProduccionFlujoEstado_Inicial DEFAULT (0),
        EsTerminal              BIT NOT NULL
            CONSTRAINT DF_ProduccionFlujoEstado_Terminal DEFAULT (0),
        CuentaParaCompletar     BIT NOT NULL
            CONSTRAINT DF_ProduccionFlujoEstado_Completar DEFAULT (1),
        Icono                   NVARCHAR(40) NULL,
        ColorHint               NVARCHAR(20) NULL,
        CONSTRAINT FK_ProduccionFlujoEstado_Flujo
            FOREIGN KEY (IdFlujo) REFERENCES dbo.ProduccionFlujo (IdFlujo),
        CONSTRAINT UQ_ProduccionFlujoEstado_FlujoCodigo UNIQUE (IdFlujo, Codigo)
    );

    CREATE INDEX IX_ProduccionFlujoEstado_FlujoOrden
        ON dbo.ProduccionFlujoEstado (IdFlujo, Orden);
END
GO

-- ------------------------------------------------------------
-- 5) Transiciones
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionFlujoTransicion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionFlujoTransicion
    (
        IdFlujoTransicion   INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionFlujoTransicion PRIMARY KEY,
        IdFlujo             INT NOT NULL,
        CodigoDesde         NVARCHAR(40) NOT NULL,
        CodigoHasta         NVARCHAR(40) NOT NULL,
        RequiereMotivo      BIT NOT NULL
            CONSTRAINT DF_ProduccionFlujoTransicion_Motivo DEFAULT (0),
        RequierePermiso     NVARCHAR(40) NULL,
        CONSTRAINT FK_ProduccionFlujoTransicion_Flujo
            FOREIGN KEY (IdFlujo) REFERENCES dbo.ProduccionFlujo (IdFlujo),
        CONSTRAINT UQ_ProduccionFlujoTransicion UNIQUE (IdFlujo, CodigoDesde, CodigoHasta)
    );

    CREATE INDEX IX_ProduccionFlujoTransicion_Flujo
        ON dbo.ProduccionFlujoTransicion (IdFlujo, CodigoDesde);
END
GO

-- ------------------------------------------------------------
-- 6) Estaciones
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionEstacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionEstacion
    (
        IdEstacion      INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionEstacion PRIMARY KEY,
        IdEmpresa       INT NOT NULL,
        Codigo          NVARCHAR(40) NOT NULL,
        Nombre          NVARCHAR(120) NOT NULL,
        EsDespacho      BIT NOT NULL
            CONSTRAINT DF_ProduccionEstacion_Despacho DEFAULT (0),
        Activa          BIT NOT NULL
            CONSTRAINT DF_ProduccionEstacion_Activa DEFAULT (1),
        OrdenVisual     INT NOT NULL
            CONSTRAINT DF_ProduccionEstacion_Orden DEFAULT (0),
        FechaCreacion   DATETIME NOT NULL
            CONSTRAINT DF_ProduccionEstacion_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_ProduccionEstacion_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa),
        CONSTRAINT UQ_ProduccionEstacion_EmpresaCodigo UNIQUE (IdEmpresa, Codigo)
    );

    CREATE INDEX IX_ProduccionEstacion_EmpresaActiva
        ON dbo.ProduccionEstacion (IdEmpresa, Activa, OrdenVisual);
END
GO

-- FK diferida: IdEstacionPredeterminada → ProduccionEstacion
IF OBJECT_ID(N'dbo.ProduccionConfiguracionEmpresa', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ProduccionEstacion', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProduccionCfg_EstacionPred'
   )
BEGIN
    ALTER TABLE dbo.ProduccionConfiguracionEmpresa
        ADD CONSTRAINT FK_ProduccionCfg_EstacionPred
            FOREIGN KEY (IdEstacionPredeterminada)
            REFERENCES dbo.ProduccionEstacion (IdEstacion);
END
GO

-- ------------------------------------------------------------
-- 7) Responsables por estación
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionEstacionResponsable', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionEstacionResponsable
    (
        IdEstacionResponsable   INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionEstacionResponsable PRIMARY KEY,
        IdEstacion              INT NOT NULL,
        IdUsuario               INT NULL,
        IdEmpleado              INT NULL,
        EsPrincipal             BIT NOT NULL
            CONSTRAINT DF_ProduccionEstResp_Principal DEFAULT (1),
        VigenteDesde            DATETIME NULL,
        VigenteHasta            DATETIME NULL,
        Activo                  BIT NOT NULL
            CONSTRAINT DF_ProduccionEstResp_Activo DEFAULT (1),
        FechaCreacion           DATETIME NOT NULL
            CONSTRAINT DF_ProduccionEstResp_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_ProduccionEstResp_Estacion
            FOREIGN KEY (IdEstacion) REFERENCES dbo.ProduccionEstacion (IdEstacion),
        CONSTRAINT CK_ProduccionEstResp_UsuarioOEmpleado
            CHECK (IdUsuario IS NOT NULL OR IdEmpleado IS NOT NULL)
    );

    CREATE INDEX IX_ProduccionEstResp_Estacion
        ON dbo.ProduccionEstacionResponsable (IdEstacion, Activo);
END
GO

-- ------------------------------------------------------------
-- 8) Trabajo (SlaModoInicioSnapshot + FechaLimiteObjetivo nullable)
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionTrabajo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionTrabajo
    (
        IdTrabajo                       INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionTrabajo PRIMARY KEY,
        IdEmpresa                       INT NOT NULL,
        TipoTrabajoCodigo               NVARCHAR(40) NOT NULL,
        IdFlujo                         INT NOT NULL,
        CodigoEstadoActual              NVARCHAR(40) NOT NULL,
        OrigenModulo                    NVARCHAR(40) NOT NULL,
        OrigenTipo                      NVARCHAR(80) NOT NULL,
        OrigenId                        INT NOT NULL,
        IdempotencyKey                  NVARCHAR(120) NOT NULL,
        NumeroVisible                   NVARCHAR(60) NOT NULL,
        NombreVisible                   NVARCHAR(200) NOT NULL,
        Referencia                      NVARCHAR(200) NULL,
        EtiquetaContexto                NVARCHAR(40) NULL,
        Observacion                     NVARCHAR(1000) NULL,
        IdUsuarioSolicita               INT NULL,
        Prioridad                       NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_Prioridad DEFAULT (N'Normal'),
        SlaObjetivoSegundosSnapshot     INT NOT NULL,
        SlaAdvertenciaSegundosSnapshot  INT NOT NULL,
        SlaModoInicioSnapshot           NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_SlaModo DEFAULT (N'CREACION'),
        FechaCreacion                   DATETIME NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_FechaCreacion DEFAULT (GETDATE()),
        FechaLimiteObjetivo             DATETIME NULL, -- nullable hasta que arranca el reloj (INICIO_PREPARACION)
        FechaInicio                     DATETIME NULL,
        FechaCompletado                 DATETIME NULL,
        FechaCancelacion                DATETIME NULL,
        IdUsuarioCancelacion            INT NULL,
        MotivoCancelacion               NVARCHAR(500) NULL,
        ActivoEnTablero                 BIT NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_ActivoTablero DEFAULT (1),
        PlantillaCodigo                 NVARCHAR(40) NULL,
        RowVersion                      ROWVERSION NOT NULL,
        CONSTRAINT FK_ProduccionTrabajo_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa),
        CONSTRAINT FK_ProduccionTrabajo_Tipo
            FOREIGN KEY (TipoTrabajoCodigo) REFERENCES dbo.ProduccionTipoTrabajo (Codigo),
        CONSTRAINT FK_ProduccionTrabajo_Flujo
            FOREIGN KEY (IdFlujo) REFERENCES dbo.ProduccionFlujo (IdFlujo),
        CONSTRAINT UQ_ProduccionTrabajo_Idempotency UNIQUE (IdEmpresa, IdempotencyKey),
        CONSTRAINT UQ_ProduccionTrabajo_Origen UNIQUE (IdEmpresa, OrigenTipo, OrigenId),
        CONSTRAINT CK_ProduccionTrabajo_Prioridad
            CHECK (Prioridad IN (N'Normal', N'Alta', N'Urgente')),
        CONSTRAINT CK_ProduccionTrabajo_SlaModoInicio
            CHECK (SlaModoInicioSnapshot IN (N'CREACION', N'INICIO_PREPARACION'))
    );

    CREATE INDEX IX_ProduccionTrabajo_Tablero
        ON dbo.ProduccionTrabajo (IdEmpresa, ActivoEnTablero, CodigoEstadoActual, FechaCreacion);

    CREATE INDEX IX_ProduccionTrabajo_Tipo
        ON dbo.ProduccionTrabajo (IdEmpresa, TipoTrabajoCodigo, ActivoEnTablero);

    CREATE INDEX IX_ProduccionTrabajo_Limite
        ON dbo.ProduccionTrabajo (IdEmpresa, ActivoEnTablero, FechaLimiteObjetivo);
END
GO

-- Upgrade: SlaModoInicioSnapshot si la tabla ya existía sin ella
IF OBJECT_ID(N'dbo.ProduccionTrabajo', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ProduccionTrabajo', N'SlaModoInicioSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ADD SlaModoInicioSnapshot NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_SlaModo DEFAULT (N'CREACION');
END
GO

IF OBJECT_ID(N'dbo.ProduccionTrabajo', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.check_constraints
       WHERE name = N'CK_ProduccionTrabajo_SlaModoInicio'
   )
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ADD CONSTRAINT CK_ProduccionTrabajo_SlaModoInicio
        CHECK (SlaModoInicioSnapshot IN (N'CREACION', N'INICIO_PREPARACION'));
END
GO

-- FechaLimiteObjetivo debe ser nullable (modo INICIO_PREPARACION)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ProduccionTrabajo')
      AND name = N'FechaLimiteObjetivo'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ALTER COLUMN FechaLimiteObjetivo DATETIME NULL;
END
GO

-- ------------------------------------------------------------
-- 9) Ítems del trabajo
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionTrabajoItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionTrabajoItem
    (
        IdTrabajoItem           INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionTrabajoItem PRIMARY KEY,
        IdTrabajo               INT NOT NULL,
        OrigenDetalleId         INT NULL,
        IdEstacion              INT NULL,
        CodigoItem              NVARCHAR(40) NULL,
        NombreItem              NVARCHAR(200) NOT NULL,
        Cantidad                DECIMAL(18,4) NOT NULL
            CONSTRAINT DF_ProduccionTrabajoItem_Cant DEFAULT (1),
        Observacion             NVARCHAR(500) NULL,
        VariacionesTexto        NVARCHAR(1000) NULL,
        CodigoEstado            NVARCHAR(40) NOT NULL,
        Orden                   INT NOT NULL
            CONSTRAINT DF_ProduccionTrabajoItem_Orden DEFAULT (0),
        FechaInicio             DATETIME NULL,
        FechaListo              DATETIME NULL,
        IdUsuarioUltimoCambio   INT NULL,
        IdResponsableAsignado   INT NULL,
        RowVersion              ROWVERSION NOT NULL,
        CONSTRAINT FK_ProduccionTrabajoItem_Trabajo
            FOREIGN KEY (IdTrabajo) REFERENCES dbo.ProduccionTrabajo (IdTrabajo),
        CONSTRAINT FK_ProduccionTrabajoItem_Estacion
            FOREIGN KEY (IdEstacion) REFERENCES dbo.ProduccionEstacion (IdEstacion)
    );

    CREATE INDEX IX_ProduccionTrabajoItem_Trabajo
        ON dbo.ProduccionTrabajoItem (IdTrabajo, Orden);

    CREATE INDEX IX_ProduccionTrabajoItem_Estacion
        ON dbo.ProduccionTrabajoItem (IdEstacion, CodigoEstado);
END
GO

-- ------------------------------------------------------------
-- 10) Historial
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProduccionHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProduccionHistorial
    (
        IdHistorial             INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ProduccionHistorial PRIMARY KEY,
        IdTrabajo               INT NOT NULL,
        IdTrabajoItem           INT NULL,
        CodigoEstadoAnterior    NVARCHAR(40) NULL,
        CodigoEstadoNuevo       NVARCHAR(40) NOT NULL,
        IdUsuario               INT NULL,
        IdEstacion              INT NULL,
        Fecha                   DATETIME NOT NULL
            CONSTRAINT DF_ProduccionHistorial_Fecha DEFAULT (GETDATE()),
        Motivo                  NVARCHAR(500) NULL,
        Origen                  NVARCHAR(40) NOT NULL
            CONSTRAINT DF_ProduccionHistorial_Origen DEFAULT (N'Sistema'),
        CONSTRAINT FK_ProduccionHistorial_Trabajo
            FOREIGN KEY (IdTrabajo) REFERENCES dbo.ProduccionTrabajo (IdTrabajo),
        CONSTRAINT FK_ProduccionHistorial_Item
            FOREIGN KEY (IdTrabajoItem) REFERENCES dbo.ProduccionTrabajoItem (IdTrabajoItem),
        CONSTRAINT FK_ProduccionHistorial_Estacion
            FOREIGN KEY (IdEstacion) REFERENCES dbo.ProduccionEstacion (IdEstacion)
    );

    CREATE INDEX IX_ProduccionHistorial_Trabajo
        ON dbo.ProduccionHistorial (IdTrabajo, Fecha DESC);
END
GO

PRINT 'DDL Centro de Producción Etapa 1 — tablas OK (Prod)';
GO

-- ============================================================
-- SEEDS (catálogo / flujo / módulos — sin demo)
-- ============================================================

-- Tipo POS_ORDEN
IF NOT EXISTS (SELECT 1 FROM dbo.ProduccionTipoTrabajo WHERE Codigo = N'POS_ORDEN')
BEGIN
    INSERT INTO dbo.ProduccionTipoTrabajo (Codigo, Nombre, Descripcion, Activo)
    VALUES (
        N'POS_ORDEN',
        N'Orden POS',
        N'Trabajos generados al confirmar una orden desde el POS',
        1
    );
END
GO

-- Flujo sistema POS_ORDEN (IdEmpresa NULL) con SlaModoInicio = INICIO_PREPARACION
DECLARE @IdFlujo INT;

IF NOT EXISTS (
    SELECT 1 FROM dbo.ProduccionFlujo
    WHERE IdEmpresa IS NULL AND TipoTrabajoCodigo = N'POS_ORDEN' AND Activo = 1
)
BEGIN
    INSERT INTO dbo.ProduccionFlujo
        (IdEmpresa, TipoTrabajoCodigo, Nombre, Activo, Version,
         SlaObjetivoSegundos, SlaAdvertenciaSegundos, SlaModoInicio)
    VALUES
        (NULL, N'POS_ORDEN', N'Órdenes POS', 1, 1, 900, 600, N'INICIO_PREPARACION');
END

SELECT @IdFlujo = IdFlujo
FROM dbo.ProduccionFlujo
WHERE IdEmpresa IS NULL AND TipoTrabajoCodigo = N'POS_ORDEN' AND Activo = 1;

-- Alinear flujos POS_ORDEN existentes al modo de preparación real
UPDATE dbo.ProduccionFlujo
SET SlaModoInicio = N'INICIO_PREPARACION'
WHERE TipoTrabajoCodigo = N'POS_ORDEN'
  AND (SlaModoInicio IS NULL OR SlaModoInicio <> N'INICIO_PREPARACION');

IF @IdFlujo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ProduccionFlujoEstado WHERE IdFlujo = @IdFlujo)
BEGIN
    INSERT INTO dbo.ProduccionFlujoEstado
        (IdFlujo, Codigo, NombreVisible, Orden, EsInicial, EsTerminal, CuentaParaCompletar, ColorHint)
    VALUES
        (@IdFlujo, N'PENDIENTE',       N'Pendiente',        1, 1, 0, 1, N'neutral'),
        (@IdFlujo, N'EN_PREPARACION',  N'En preparación',   2, 0, 0, 1, N'info'),
        (@IdFlujo, N'LISTA',           N'Lista',            3, 0, 0, 1, N'success'),
        (@IdFlujo, N'ENTREGADA',       N'Entregada',        4, 0, 1, 1, N'success'),
        (@IdFlujo, N'CANCELADA',       N'Cancelada',        99, 0, 1, 0, N'danger');
END

IF @IdFlujo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ProduccionFlujoTransicion WHERE IdFlujo = @IdFlujo)
BEGIN
    INSERT INTO dbo.ProduccionFlujoTransicion
        (IdFlujo, CodigoDesde, CodigoHasta, RequiereMotivo, RequierePermiso)
    VALUES
        (@IdFlujo, N'PENDIENTE',      N'EN_PREPARACION', 0, N'PRODUCCION_GESTIONAR'),
        (@IdFlujo, N'EN_PREPARACION', N'LISTA',          0, N'PRODUCCION_GESTIONAR'),
        (@IdFlujo, N'LISTA',          N'ENTREGADA',      0, N'PRODUCCION_GESTIONAR'),
        (@IdFlujo, N'PENDIENTE',      N'CANCELADA',      1, N'PRODUCCION_CANCELAR'),
        (@IdFlujo, N'EN_PREPARACION', N'CANCELADA',      1, N'PRODUCCION_CANCELAR'),
        (@IdFlujo, N'LISTA',          N'CANCELADA',      1, N'PRODUCCION_CANCELAR');
END
GO

-- Trabajos activos POS (si hubiera): alinear snapshot y límite con modo INICIO_PREPARACION
IF OBJECT_ID(N'dbo.ProduccionTrabajo', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ProduccionTrabajo', N'SlaModoInicioSnapshot') IS NOT NULL
BEGIN
    UPDATE t
    SET
        t.SlaModoInicioSnapshot = N'INICIO_PREPARACION',
        t.FechaLimiteObjetivo = CASE
            WHEN t.FechaInicio IS NULL THEN NULL
            ELSE DATEADD(SECOND, t.SlaObjetivoSegundosSnapshot, t.FechaInicio)
        END
    FROM dbo.ProduccionTrabajo t
    WHERE t.TipoTrabajoCodigo = N'POS_ORDEN'
      AND t.ActivoEnTablero = 1;
END
GO

-- Módulos (catálogo global; no implica licencia por empresa)
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'CENTRO_PRODUCCION')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'CENTRO_PRODUCCION', N'Centro de Producción',
            N'Tablero operativo de trabajos y flujos de producción', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PRODUCCION_GESTIONAR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'PRODUCCION_GESTIONAR', N'Producción — Gestionar',
            N'Cambiar estados de trabajos en el Centro de Producción', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PRODUCCION_CANCELAR')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'PRODUCCION_CANCELAR', N'Producción — Cancelar',
            N'Cancelar trabajos en el Centro de Producción', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PRODUCCION_PRIORIDAD')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'PRODUCCION_PRIORIDAD', N'Producción — Prioridad',
            N'Cambiar prioridad de trabajos', 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PRODUCCION_CONFIG')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'PRODUCCION_CONFIG', N'Producción — Configuración',
            N'Configurar Centro de Producción por empresa', 0, 1, GETDATE());
GO

-- ============================================================
-- Config + estación GENERAL (siempre, opt-in de Activo)
-- Licencia módulos solo si @LicenciarTodasEmpresas = 1
-- Ajustar estos flags ANTES de ejecutar (default seguro = 0)
-- ============================================================
DECLARE @LicenciarTodasEmpresas BIT = 1;      -- 1 = Empresa_Modulos + PerfilRoles Admin
DECLARE @ActivarMotorTodasEmpresas BIT = 1;   -- 1 = ProduccionConfiguracionEmpresa.Activo = 1

-- Empresar filtro: Prod puede no tener EsEmpresaSistema
-- Config por empresa (Activo según flag)
INSERT INTO dbo.ProduccionConfiguracionEmpresa
    (IdEmpresa, Activo, UsarEstaciones, UsarEstadosPorItem, SonidoActivo,
     TiempoAdvertenciaSegDefault, TiempoCriticoSegDefault, PermitirCompletarDesdeEstacion,
     ModoOscuroDefault, MostrarNombreCliente, MostrarUsuarioSolicita, FechaActualizacion)
SELECT
    e.IdEmpresa,
    CASE WHEN @ActivarMotorTodasEmpresas = 1 THEN 1 ELSE 0 END,
    0, 0, 1, 600, 900, 1, 1, 1, 1, GETDATE()
FROM dbo.Empresas e
WHERE NOT EXISTS (
      SELECT 1 FROM dbo.ProduccionConfiguracionEmpresa c WHERE c.IdEmpresa = e.IdEmpresa
  );

-- Estación GENERAL por empresa
INSERT INTO dbo.ProduccionEstacion (IdEmpresa, Codigo, Nombre, EsDespacho, Activa, OrdenVisual)
SELECT e.IdEmpresa, N'GENERAL', N'General', 0, 1, 0
FROM dbo.Empresas e
WHERE NOT EXISTS (
      SELECT 1 FROM dbo.ProduccionEstacion s
      WHERE s.IdEmpresa = e.IdEmpresa AND s.Codigo = N'GENERAL'
  );

UPDATE c
SET IdEstacionPredeterminada = s.IdEstacion,
    FechaActualizacion = GETDATE()
FROM dbo.ProduccionConfiguracionEmpresa c
INNER JOIN dbo.ProduccionEstacion s
    ON s.IdEmpresa = c.IdEmpresa AND s.Codigo = N'GENERAL'
WHERE c.IdEstacionPredeterminada IS NULL;

-- Licencia + roles Admin solo con flag explícito
IF @LicenciarTodasEmpresas = 1
BEGIN
    ;WITH Mods AS (
        SELECT Id FROM dbo.Modulos
        WHERE Codigo IN (
            N'CENTRO_PRODUCCION', N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR',
            N'PRODUCCION_PRIORIDAD', N'PRODUCCION_CONFIG'
        )
    )
    INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
    SELECT e.IdEmpresa, m.Id, 1, GETDATE()
    FROM dbo.Empresas e
    CROSS JOIN Mods m
    WHERE NOT EXISTS (
          SELECT 1 FROM dbo.Empresa_Modulos em
          WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = m.Id
      );

    ;WITH Mods AS (
        SELECT Id FROM dbo.Modulos
        WHERE Codigo IN (
            N'CENTRO_PRODUCCION', N'PRODUCCION_GESTIONAR', N'PRODUCCION_CANCELAR',
            N'PRODUCCION_PRIORIDAD', N'PRODUCCION_CONFIG'
        )
    )
    INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
    SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
    FROM dbo.Perfiles p
    INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
    CROSS JOIN Mods m
    WHERE (
          p.Nombre LIKE N'%Admin%'
          OR p.Nombre LIKE N'%Administrador%'
      )
      AND NOT EXISTS (
          SELECT 1 FROM dbo.PerfilRoles pr
          WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
      );
END
ELSE
BEGIN
    PRINT 'Licencia omitida: @LicenciarTodasEmpresas = 0 (licenciar por empresa después del deploy).';
END
GO

-- ============================================================
-- Verificación
-- ============================================================
SELECT 'Tablas' AS Seccion, t.name AS Nombre
FROM sys.tables t
WHERE t.name LIKE N'Produccion%'
ORDER BY t.name;

SELECT Codigo, Nombre, Activo FROM dbo.ProduccionTipoTrabajo;

SELECT IdFlujo, TipoTrabajoCodigo, Nombre, SlaObjetivoSegundos, SlaAdvertenciaSegundos, SlaModoInicio
FROM dbo.ProduccionFlujo
WHERE TipoTrabajoCodigo = N'POS_ORDEN';

SELECT Codigo, NombreVisible, Orden, EsInicial, EsTerminal
FROM dbo.ProduccionFlujoEstado
WHERE IdFlujo = (
    SELECT TOP 1 IdFlujo
    FROM dbo.ProduccionFlujo
    WHERE TipoTrabajoCodigo = N'POS_ORDEN' AND IdEmpresa IS NULL
);

SELECT Codigo, Nombre FROM dbo.Modulos
WHERE Codigo LIKE N'CENTRO_PRODUCCION' OR Codigo LIKE N'PRODUCCION_%';

SELECT
    COUNT(*) AS EmpresasConConfig,
    SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) AS ConfigActivas,
    SUM(CASE WHEN Activo = 0 THEN 1 ELSE 0 END) AS ConfigInactivas
FROM dbo.ProduccionConfiguracionEmpresa;

SELECT COUNT(*) AS EstacionesGeneral
FROM dbo.ProduccionEstacion
WHERE Codigo = N'GENERAL';

SELECT COUNT(*) AS EmpresasConModuloCentro
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE m.Codigo = N'CENTRO_PRODUCCION' AND em.Activo = 1;

PRINT 'Centro de Producción Etapa 1 — DDL + Seeds OK en AlahiaPos_Prod (sin demo). Deploy API/FE aparte.';
GO

SET NOEXEC OFF;
GO
