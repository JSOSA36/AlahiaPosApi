/*
  Migrate_DevSchema_To_Prod_20260828.sql
  ------------------------------------------------------------
  Lleva a AlahiaPos_Prod el DDL de AlahiaPos_Dev que aún no está.
  Solo cambios aditivos / idempotentes.

  NO copia datos de negocio Dev -> Prod.
  NO elimina columnas ni tablas.
  NO activa PEDIDOS_ONLINE a todos los clientes (queda en catálogo).

  Incluye:
  - Empresas.TrabajaDomingo (Sena = 0)
  - Tablas PedidoOnline / Delivery
  - Módulo PEDIDOS_ONLINE (catálogo)
  - Parámetros ControlEfectivoPorDenominacion y PREVIEW_DGII

  Autorización: usuario pidió pasar cambios de BD Dev a producción.
*/
USE AlahiaPos_Prod;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Este script solo puede ejecutarse en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

PRINT '=== INICIO migracion schema Dev->Prod 2026-08-28 ===';
GO

/* =====================================================================
   1) Empresas.TrabajaDomingo
   ===================================================================== */
IF COL_LENGTH(N'dbo.Empresas', N'TrabajaDomingo') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD TrabajaDomingo BIT NOT NULL
        CONSTRAINT DF_Empresas_TrabajaDomingo DEFAULT (1);
END
GO

UPDATE dbo.Empresas
SET TrabajaDomingo = 1
WHERE TrabajaDomingo IS NULL;
GO

UPDATE dbo.Empresas
SET TrabajaDomingo = 0
WHERE ISNULL(EsEmpresaSistema, 0) = 0
  AND NombreComercial LIKE N'%Sena%'
  AND (NombreComercial LIKE N'%Dental%' OR IdEmpresa = 60);
GO

/* =====================================================================
   2) Pedidos online + delivery
   ===================================================================== */
IF OBJECT_ID(N'dbo.PedidoOnlineCanal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PedidoOnlineCanal (
        IdCanal        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa      INT NOT NULL,
        Slug           NVARCHAR(80) NOT NULL,
        NombrePublico  NVARCHAR(160) NOT NULL,
        WhatsApp       NVARCHAR(40) NULL,
        LogoUrl        NVARCHAR(300) NULL,
        Activo         BIT NOT NULL CONSTRAINT DF_PedidoOnlineCanal_Activo DEFAULT (1),
        FechaCreacion  DATETIME NOT NULL CONSTRAINT DF_PedidoOnlineCanal_Fecha DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_PedidoOnlineCanal_Slug ON dbo.PedidoOnlineCanal (Slug);
    CREATE INDEX IX_PedidoOnlineCanal_Empresa ON dbo.PedidoOnlineCanal (IdEmpresa, Activo);
END
GO

IF OBJECT_ID(N'dbo.PedidoOnline', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PedidoOnline (
        IdPedidoOnline       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdCanal              INT NOT NULL,
        IdFacturaHeader      INT NOT NULL,
        IdCliente            INT NULL,
        NombreCliente        NVARCHAR(120) NOT NULL,
        Telefono             NVARCHAR(30) NOT NULL,
        TipoEntrega          NVARCHAR(20) NOT NULL,
        Direccion            NVARCHAR(300) NULL,
        ReferenciaDireccion  NVARCHAR(300) NULL,
        Latitud              DECIMAL(10,7) NULL,
        Longitud             DECIMAL(10,7) NULL,
        MetodoPago           NVARCHAR(40) NOT NULL,
        Observacion          NVARCHAR(500) NULL,
        EstadoLogistico      NVARCHAR(30) NOT NULL CONSTRAINT DF_PedidoOnline_Estado DEFAULT (N'Nuevo'),
        IdempotencyKey       NVARCHAR(80) NULL,
        FechaCreacion        DATETIME NOT NULL CONSTRAINT DF_PedidoOnline_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_PedidoOnline_Canal FOREIGN KEY (IdCanal) REFERENCES dbo.PedidoOnlineCanal (IdCanal)
    );
    CREATE INDEX IX_PedidoOnline_Empresa ON dbo.PedidoOnline (IdEmpresa, FechaCreacion DESC);
    CREATE UNIQUE INDEX UX_PedidoOnline_Idempotency ON dbo.PedidoOnline (IdEmpresa, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.DeliveryRepartidor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryRepartidor (
        IdRepartidor  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa     INT NOT NULL,
        IdUsuario     INT NOT NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_DeliveryRepartidor_Activo DEFAULT (1),
        Disponible    BIT NOT NULL CONSTRAINT DF_DeliveryRepartidor_Disp DEFAULT (1),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_DeliveryRepartidor_Fecha DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_DeliveryRepartidor_Usuario ON dbo.DeliveryRepartidor (IdEmpresa, IdUsuario);
END
GO

IF OBJECT_ID(N'dbo.DeliveryAsignacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryAsignacion (
        IdAsignacion         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa            INT NOT NULL,
        IdPedidoOnline       INT NOT NULL,
        IdUsuarioRepartidor  INT NOT NULL,
        IdUsuarioAsigna      INT NULL,
        Estado               NVARCHAR(20) NOT NULL CONSTRAINT DF_DeliveryAsignacion_Estado DEFAULT (N'Asignado'),
        Activa               BIT NOT NULL CONSTRAINT DF_DeliveryAsignacion_Activa DEFAULT (1),
        FechaAsignacion      DATETIME NOT NULL CONSTRAINT DF_DeliveryAsignacion_Fecha DEFAULT (GETDATE()),
        FechaRecogido        DATETIME NULL,
        FechaEnCamino        DATETIME NULL,
        FechaEntregado       DATETIME NULL,
        CONSTRAINT FK_DeliveryAsignacion_Pedido FOREIGN KEY (IdPedidoOnline) REFERENCES dbo.PedidoOnline (IdPedidoOnline)
    );
    CREATE INDEX IX_DeliveryAsignacion_Pedido ON dbo.DeliveryAsignacion (IdPedidoOnline, Activa);
    CREATE INDEX IX_DeliveryAsignacion_Repartidor ON dbo.DeliveryAsignacion (IdEmpresa, IdUsuarioRepartidor, Activa);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = N'PEDIDOS_ONLINE')
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, PrecioUSD, Activo, FechaCreacion)
    VALUES (N'PEDIDOS_ONLINE', N'Pedidos online y delivery', N'Pedidos desde PWA, KDS y asignacion a repartidores.', 0, 1, GETDATE());
ELSE
    UPDATE dbo.Modulos
    SET Nombre = N'Pedidos online y delivery',
        Descripcion = N'Pedidos desde PWA, KDS y asignacion a repartidores.',
        Activo = 1
    WHERE Codigo = N'PEDIDOS_ONLINE';
GO

/* =====================================================================
   3) Parámetros de empresa (catálogo, sin pisar valores existentes)
   ===================================================================== */
DECLARE @ClaveEfectivo NVARCHAR(100) = N'ControlEfectivoPorDenominacion';
DECLARE @DescEfectivo NVARCHAR(300) =
    N'Control de efectivo por denominacion. Activado = pide billetes y monedas al cerrar. Desactivado = cierre simplificado (solo Clinica Dental Sena).';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @ClaveEfectivo,
    N'true',
    @DescEfectivo,
    GETDATE(),
    1
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Parametros p
    WHERE p.IdEmpresa = e.IdEmpresa
      AND LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@ClaveEfectivo)
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

UPDATE p
SET p.Descripcion = @DescEfectivo,
    p.Activo = 1
FROM dbo.Parametros p
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@ClaveEfectivo)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');
GO

DECLARE @ClavePreview NVARCHAR(100) = N'PREVIEW_DGII';
DECLARE @DescPreview NVARCHAR(200) =
    N'Si esta activo, el POS muestra vista previa del comprobante (cualquier NCF) al facturar. NC siempre la muestra.';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @ClavePreview,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Parametros fe
            WHERE fe.IdEmpresa = e.IdEmpresa
              AND fe.Clave = N'FACTURACION_ELECTRONICA'
              AND LOWER(LTRIM(RTRIM(ISNULL(fe.Valor, N'')))) IN (N'true', N'1')
        ) THEN N'true'
        ELSE N'false'
    END,
    @DescPreview,
    GETDATE(),
    1
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Parametros p
    WHERE p.IdEmpresa = e.IdEmpresa
      AND UPPER(REPLACE(REPLACE(p.Clave, N'_', N''), N'-', N'')) = N'PREVIEWDGII'
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

UPDATE p
SET p.Descripcion = @DescPreview,
    p.Activo = 1
FROM dbo.Parametros p
WHERE UPPER(REPLACE(REPLACE(p.Clave, N'_', N''), N'-', N'')) = N'PREVIEWDGII'
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');
GO

PRINT '=== FIN migracion schema Dev->Prod 2026-08-28 ===';

SELECT
    CASE WHEN COL_LENGTH(N'dbo.Empresas', N'TrabajaDomingo') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS Empresas_TrabajaDomingo,
    CASE WHEN OBJECT_ID(N'dbo.PedidoOnlineCanal', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS PedidoOnlineCanal,
    CASE WHEN OBJECT_ID(N'dbo.PedidoOnline', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS PedidoOnline,
    CASE WHEN OBJECT_ID(N'dbo.DeliveryRepartidor', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS DeliveryRepartidor,
    CASE WHEN OBJECT_ID(N'dbo.DeliveryAsignacion', N'U') IS NOT NULL THEN 'OK' ELSE 'FALTA' END AS DeliveryAsignacion;

SELECT m.Codigo, m.Nombre, m.Activo,
       (SELECT COUNT(*) FROM dbo.Empresa_Modulos em WHERE em.ModuloId = m.Id AND em.Activo = 1) AS EmpresasActivas
FROM dbo.Modulos m
WHERE m.Codigo = N'PEDIDOS_ONLINE';

SELECT p.Clave, COUNT(*) AS Empresas
FROM dbo.Parametros p
WHERE p.Clave IN (N'ControlEfectivoPorDenominacion', N'PREVIEW_DGII')
GROUP BY p.Clave;
GO
