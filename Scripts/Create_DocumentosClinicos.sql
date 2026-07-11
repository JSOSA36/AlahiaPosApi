-- =============================================
-- Módulo: Documentos Clínicos (Etapa 1 - Base de Datos)
-- Descripción: Plantillas y documentos clínicos emitidos.
-- Nota: El paciente es Clientes (IdCliente -> Clientes.IDCliente).
-- =============================================

/* =========================================================
   1. PlantillasDocumentosClinicos
   ========================================================= */
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'PlantillasDocumentosClinicos'
      AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.PlantillasDocumentosClinicos
    (
        IdPlantilla         INT             IDENTITY(1,1) NOT NULL,
        IdEmpresa           INT             NOT NULL,
        Nombre              NVARCHAR(200)   NOT NULL,
        TipoDocumento       NVARCHAR(100)   NOT NULL,
        ContenidoHTML       NVARCHAR(MAX)   NOT NULL,
        EsPredeterminada    BIT             NOT NULL CONSTRAINT DF_PlantillasDocumentosClinicos_EsPredeterminada DEFAULT (0),
        Activa              BIT             NOT NULL CONSTRAINT DF_PlantillasDocumentosClinicos_Activa DEFAULT (1),
        FechaCreacion       DATETIME2(0)    NOT NULL CONSTRAINT DF_PlantillasDocumentosClinicos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        IdUsuarioCreacion   INT             NOT NULL,

        CONSTRAINT PK_PlantillasDocumentosClinicos
            PRIMARY KEY CLUSTERED (IdPlantilla)
    );
END
GO

/* FK: Empresa */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PlantillasDocumentosClinicos_Empresas'
)
BEGIN
    ALTER TABLE dbo.PlantillasDocumentosClinicos
    ADD CONSTRAINT FK_PlantillasDocumentosClinicos_Empresas
        FOREIGN KEY (IdEmpresa)
        REFERENCES dbo.Empresas (IdEmpresa);
END
GO

/* FK: Usuario creador */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PlantillasDocumentosClinicos_Usuarios'
)
BEGIN
    ALTER TABLE dbo.PlantillasDocumentosClinicos
    ADD CONSTRAINT FK_PlantillasDocumentosClinicos_Usuarios
        FOREIGN KEY (IdUsuarioCreacion)
        REFERENCES dbo.Usuarios (IdUsuario);
END
GO

/* Índices: PlantillasDocumentosClinicos */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PlantillasDocumentosClinicos_IdEmpresa'
      AND object_id = OBJECT_ID('dbo.PlantillasDocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlantillasDocumentosClinicos_IdEmpresa
        ON dbo.PlantillasDocumentosClinicos (IdEmpresa);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PlantillasDocumentosClinicos_IdEmpresa_TipoDocumento'
      AND object_id = OBJECT_ID('dbo.PlantillasDocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlantillasDocumentosClinicos_IdEmpresa_TipoDocumento
        ON dbo.PlantillasDocumentosClinicos (IdEmpresa, TipoDocumento)
        INCLUDE (Nombre, EsPredeterminada, Activa);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PlantillasDocumentosClinicos_IdEmpresa_Activa'
      AND object_id = OBJECT_ID('dbo.PlantillasDocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlantillasDocumentosClinicos_IdEmpresa_Activa
        ON dbo.PlantillasDocumentosClinicos (IdEmpresa, Activa)
        INCLUDE (Nombre, TipoDocumento, EsPredeterminada);
END
GO

/* Solo una plantilla predeterminada por empresa y tipo */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_PlantillasDocumentosClinicos_Empresa_Tipo_Predeterminada'
      AND object_id = OBJECT_ID('dbo.PlantillasDocumentosClinicos')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_PlantillasDocumentosClinicos_Empresa_Tipo_Predeterminada
        ON dbo.PlantillasDocumentosClinicos (IdEmpresa, TipoDocumento)
        WHERE EsPredeterminada = 1;
END
GO


/* =========================================================
   2. DocumentosClinicos
   ========================================================= */
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'DocumentosClinicos'
      AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.DocumentosClinicos
    (
        IdDocumentoClinico  INT             IDENTITY(1,1) NOT NULL,
        IdEmpresa           INT             NOT NULL,
        IdCliente           INT             NOT NULL,
        IdPlantilla         INT             NOT NULL,
        TipoDocumento       NVARCHAR(100)   NOT NULL,
        NumeroDocumento     NVARCHAR(50)    NOT NULL,
        FechaEmision        DATETIME2(0)    NOT NULL,
        NombreDoctor        NVARCHAR(200)   NULL,
        HorasReposo         INT             NULL,
        Procedimiento       NVARCHAR(500)   NULL,
        Observaciones       NVARCHAR(MAX)   NULL,
        ContenidoHTMLFinal  NVARCHAR(MAX)   NOT NULL,
        DatosJSON           NVARCHAR(MAX)   NULL,
        IdUsuarioCreacion   INT             NOT NULL,
        Estado              NVARCHAR(30)    NOT NULL CONSTRAINT DF_DocumentosClinicos_Estado DEFAULT ('EMITIDO'),
        FechaCreacion       DATETIME2(0)    NOT NULL CONSTRAINT DF_DocumentosClinicos_FechaCreacion DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_DocumentosClinicos
            PRIMARY KEY CLUSTERED (IdDocumentoClinico),

        CONSTRAINT CK_DocumentosClinicos_HorasReposo
            CHECK (HorasReposo IS NULL OR HorasReposo >= 0),

        CONSTRAINT CK_DocumentosClinicos_Estado
            CHECK (Estado IN ('BORRADOR', 'EMITIDO', 'ANULADO'))
    );
END
GO

/* FK: Empresa */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DocumentosClinicos_Empresas'
)
BEGIN
    ALTER TABLE dbo.DocumentosClinicos
    ADD CONSTRAINT FK_DocumentosClinicos_Empresas
        FOREIGN KEY (IdEmpresa)
        REFERENCES dbo.Empresas (IdEmpresa);
END
GO

/* FK: Cliente (paciente) */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DocumentosClinicos_Clientes'
)
BEGIN
    ALTER TABLE dbo.DocumentosClinicos
    ADD CONSTRAINT FK_DocumentosClinicos_Clientes
        FOREIGN KEY (IdCliente)
        REFERENCES dbo.Clientes (IDCliente);
END
GO

/* FK: Plantilla origen */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DocumentosClinicos_PlantillasDocumentosClinicos'
)
BEGIN
    ALTER TABLE dbo.DocumentosClinicos
    ADD CONSTRAINT FK_DocumentosClinicos_PlantillasDocumentosClinicos
        FOREIGN KEY (IdPlantilla)
        REFERENCES dbo.PlantillasDocumentosClinicos (IdPlantilla);
END
GO

/* FK: Usuario creador */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DocumentosClinicos_Usuarios'
)
BEGIN
    ALTER TABLE dbo.DocumentosClinicos
    ADD CONSTRAINT FK_DocumentosClinicos_Usuarios
        FOREIGN KEY (IdUsuarioCreacion)
        REFERENCES dbo.Usuarios (IdUsuario);
END
GO

/* Índices: DocumentosClinicos */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DocumentosClinicos_IdEmpresa'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentosClinicos_IdEmpresa
        ON dbo.DocumentosClinicos (IdEmpresa);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DocumentosClinicos_IdCliente'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentosClinicos_IdCliente
        ON dbo.DocumentosClinicos (IdCliente);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DocumentosClinicos_IdEmpresa_FechaEmision'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentosClinicos_IdEmpresa_FechaEmision
        ON dbo.DocumentosClinicos (IdEmpresa, FechaEmision DESC)
        INCLUDE (IdCliente, TipoDocumento, NumeroDocumento, Estado);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DocumentosClinicos_IdEmpresa_IdCliente'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentosClinicos_IdEmpresa_IdCliente
        ON dbo.DocumentosClinicos (IdEmpresa, IdCliente, FechaEmision DESC)
        INCLUDE (TipoDocumento, NumeroDocumento, Estado, NombreDoctor);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DocumentosClinicos_IdPlantilla'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentosClinicos_IdPlantilla
        ON dbo.DocumentosClinicos (IdPlantilla);
END
GO

/* Número de documento único por empresa */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_DocumentosClinicos_IdEmpresa_NumeroDocumento'
      AND object_id = OBJECT_ID('dbo.DocumentosClinicos')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_DocumentosClinicos_IdEmpresa_NumeroDocumento
        ON dbo.DocumentosClinicos (IdEmpresa, NumeroDocumento);
END
GO
