-- Delivery por distancia. Solo AlahiaPos_Dev.
-- Extiende PedidoOnline, el canal y agrega configuración, caché y direcciones.

IF COL_LENGTH(N'dbo.PedidoOnlineCanal', N'IdSucursal') IS NULL
    ALTER TABLE dbo.PedidoOnlineCanal ADD IdSucursal INT NULL;
GO

IF COL_LENGTH(N'dbo.PedidoOnline', N'CostoDelivery') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD CostoDelivery DECIMAL(12,2) NOT NULL CONSTRAINT DF_PedidoOnline_CostoDelivery DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'DistanciaDeliveryKm') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD DistanciaDeliveryKm DECIMAL(10,3) NOT NULL CONSTRAINT DF_PedidoOnline_DistanciaDelivery DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'TarifaBaseAplicada') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD TarifaBaseAplicada DECIMAL(12,2) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'PrecioKmAplicado') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD PrecioKmAplicado DECIMAL(12,2) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'RedondeoAplicado') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD RedondeoAplicado DECIMAL(12,2) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'MinimoAplicado') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD MinimoAplicado DECIMAL(12,2) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'ProveedorDistancia') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD ProveedorDistancia NVARCHAR(40) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'OrigenLatitud') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD OrigenLatitud DECIMAL(10,7) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'OrigenLongitud') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD OrigenLongitud DECIMAL(10,7) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'DestinoLatitud') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD DestinoLatitud DECIMAL(10,7) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'DestinoLongitud') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD DestinoLongitud DECIMAL(10,7) NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'FechaCalculoDelivery') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD FechaCalculoDelivery DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.PedidoOnline', N'DeliveryCobrarAlEntregar') IS NULL
    ALTER TABLE dbo.PedidoOnline ADD DeliveryCobrarAlEntregar BIT NOT NULL CONSTRAINT DF_PedidoOnline_DeliveryCobrarAlEntregar DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.DeliveryConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryConfig (
        IdDeliveryConfig      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa             INT NOT NULL,
        IdSucursal            INT NULL,
        DeliveryHabilitado    BIT NOT NULL CONSTRAINT DF_DeliveryConfig_Hab DEFAULT (0),
        CalculoPorDistancia   BIT NOT NULL CONSTRAINT DF_DeliveryConfig_Calc DEFAULT (0),
        TarifaBase            DECIMAL(12,2) NOT NULL,
        PrecioPorKilometro    DECIMAL(12,2) NOT NULL,
        CostoMinimoDelivery   DECIMAL(12,2) NOT NULL,
        RedondeoDelivery      DECIMAL(12,2) NOT NULL,
        DistanciaMaximaKm     DECIMAL(10,2) NULL,
        AccionFueraDeZona     NVARCHAR(30) NOT NULL,
        Proveedor             NVARCHAR(40) NOT NULL,
        MinutosCache          INT NOT NULL CONSTRAINT DF_DeliveryConfig_Cache DEFAULT (30),
        ExigirGps             BIT NOT NULL CONSTRAINT DF_DeliveryConfig_Gps DEFAULT (1),
        FechaCreacion         DATETIME NOT NULL CONSTRAINT DF_DeliveryConfig_Fecha DEFAULT (GETDATE()),
        FechaActualizacion    DATETIME NOT NULL CONSTRAINT DF_DeliveryConfig_Upd DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DeliveryConfig_Sucursal' AND object_id = OBJECT_ID(N'dbo.DeliveryConfig'))
    CREATE UNIQUE INDEX UX_DeliveryConfig_Sucursal
        ON dbo.DeliveryConfig (IdEmpresa, IdSucursal)
        WHERE IdSucursal IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DeliveryConfig_Empresa' AND object_id = OBJECT_ID(N'dbo.DeliveryConfig'))
    CREATE UNIQUE INDEX UX_DeliveryConfig_Empresa
        ON dbo.DeliveryConfig (IdEmpresa)
        WHERE IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.DeliveryConfigAuditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryConfigAuditoria (
        IdAuditoria        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdSucursal         INT NULL,
        IdUsuario          INT NOT NULL,
        Fecha              DATETIME NOT NULL CONSTRAINT DF_DeliveryConfigAud_Fecha DEFAULT (GETDATE()),
        Accion             NVARCHAR(40) NOT NULL,
        ValorAnteriorJson  NVARCHAR(MAX) NULL,
        ValorNuevoJson     NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_DeliveryConfigAuditoria_Empresa
        ON dbo.DeliveryConfigAuditoria (IdEmpresa, Fecha DESC);
END
GO

IF OBJECT_ID(N'dbo.DeliveryDistanciaCache', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryDistanciaCache (
        IdCache              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdSucursal           INT NOT NULL,
        Proveedor            NVARCHAR(40) NOT NULL,
        LatitudDestino       DECIMAL(10,7) NOT NULL,
        LongitudDestino      DECIMAL(10,7) NOT NULL,
        LatitudClave         DECIMAL(7,3) NOT NULL,
        LongitudClave        DECIMAL(7,3) NOT NULL,
        OrigenLatitudClave   DECIMAL(7,3) NOT NULL,
        OrigenLongitudClave  DECIMAL(7,3) NOT NULL,
        DistanciaKm          DECIMAL(10,3) NOT NULL,
        DuracionSegundos     INT NULL,
        FechaCreacion        DATETIME NOT NULL CONSTRAINT DF_DeliveryCache_Fecha DEFAULT (GETDATE()),
        FechaExpiracion      DATETIME NOT NULL
    );
    CREATE INDEX IX_DeliveryDistanciaCache_Busqueda
        ON dbo.DeliveryDistanciaCache (IdEmpresa, IdSucursal, Proveedor, LatitudClave, LongitudClave, OrigenLatitudClave, OrigenLongitudClave, FechaExpiracion);
END
GO

IF OBJECT_ID(N'dbo.ClienteDireccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClienteDireccion (
        IdClienteDireccion   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdCliente            INT NOT NULL,
        Direccion            NVARCHAR(300) NOT NULL,
        Referencia           NVARCHAR(300) NULL,
        Latitud              DECIMAL(10,7) NULL,
        Longitud             DECIMAL(10,7) NULL,
        NombreDestinatario   NVARCHAR(120) NULL,
        Telefono             NVARCHAR(30) NULL,
        Activa               BIT NOT NULL CONSTRAINT DF_ClienteDireccion_Activa DEFAULT (1),
        FechaCreacion        DATETIME NOT NULL CONSTRAINT DF_ClienteDireccion_Fecha DEFAULT (GETDATE())
    );
    CREATE INDEX IX_ClienteDireccion_Cliente
        ON dbo.ClienteDireccion (IdEmpresa, IdCliente, Activa);
END
GO

PRINT 'Delivery por distancia listo en AlahiaPos_Dev.';
GO
