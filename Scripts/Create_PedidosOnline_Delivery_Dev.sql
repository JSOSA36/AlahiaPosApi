-- Pedidos online PWA + delivery. Solo AlahiaPos_Dev.

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
    VALUES (N'PEDIDOS_ONLINE', N'Pedidos online y delivery', N'Pedidos desde PWA, KDS y asignación a repartidores.', 0, 1, GETDATE());
GO

DECLARE @IdModulo INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'PEDIDOS_ONLINE');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT e.IdEmpresa, @IdModulo, 1, GETDATE()
FROM dbo.Empresas e
WHERE ISNULL(e.EsEmpresaSistema, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Empresa_Modulos em
      WHERE em.EmpresaId = e.IdEmpresa AND em.ModuloId = @IdModulo
  );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
WHERE em.ModuloId = @IdModulo;
GO

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON m.Codigo = N'PEDIDOS_ONLINE'
WHERE EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = p.IdEmpresa AND em.ModuloId = m.Id AND em.Activo = 1
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PedidoOnlineCanal WHERE Slug = N'terraza27')
BEGIN
    INSERT INTO dbo.PedidoOnlineCanal (IdEmpresa, Slug, NombrePublico, WhatsApp, Activo, FechaCreacion)
    VALUES (55, N'terraza27', N'Terraza Prolongacion 27', NULL, 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PedidoOnlineCanal WHERE Slug = N'saborurbano')
BEGIN
    INSERT INTO dbo.PedidoOnlineCanal (IdEmpresa, Slug, NombrePublico, WhatsApp, LogoUrl, Activo, FechaCreacion)
    VALUES (62, N'saborurbano', N'Sabor Urbano', N'8095550180', N'https://alahiaupdate.alahiapos.com/demo-sabor-urbano-logo.jpg', 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Perfiles WHERE IdEmpresa = 55 AND Nombre = N'Repartidor')
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (55, N'Repartidor', N'Acceso mínimo a pedidos online y PWA de reparto.', 1);
END
GO

DECLARE @IdPerfilRep INT = (SELECT TOP 1 IdPerfil FROM dbo.Perfiles WHERE IdEmpresa = 55 AND Nombre = N'Repartidor');
DECLARE @IdModuloPed INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'PEDIDOS_ONLINE');

IF @IdPerfilRep IS NOT NULL AND @IdModuloPed IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles
    WHERE IdPerfil = @IdPerfilRep AND IdModulo = @IdModuloPed AND IdEmpresa = 55
)
BEGIN
    INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
    VALUES (@IdPerfilRep, @IdModuloPed, 1, GETDATE(), 55);
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Sabor Urbano (62): perfil + usuario de repartidor demo
IF NOT EXISTS (SELECT 1 FROM dbo.Perfiles WHERE IdEmpresa = 62 AND Nombre = N'Repartidor')
BEGIN
    INSERT INTO dbo.Perfiles (IdEmpresa, Nombre, Descripcion, Activo)
    VALUES (62, N'Repartidor', N'Acceso minimo a pedidos online y PWA de reparto.', 1);
END
GO

DECLARE @IdEmpresaSu INT = 62;
DECLARE @IdPerfilSu INT = (SELECT TOP 1 IdPerfil FROM dbo.Perfiles WHERE IdEmpresa = 62 AND Nombre = N'Repartidor');
DECLARE @IdModuloSu INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'PEDIDOS_ONLINE');
DECLARE @IdEmpleadoSu INT;
DECLARE @IdUsuarioSu INT;
DECLARE @UserReparto NVARCHAR(150) = N'repartidor@saborurbano.demo';
DECLARE @HashReparto NVARCHAR(128) = N'02d87bd6b67f1c51e5a0c51294c1df1a4e41701a0b5ec6fbbba401edb8f55af0'; -- DemoFood2026!

IF @IdPerfilSu IS NOT NULL AND @IdModuloSu IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles
    WHERE IdPerfil = @IdPerfilSu AND IdModulo = @IdModuloSu AND IdEmpresa = @IdEmpresaSu
)
BEGIN
    INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
    VALUES (@IdPerfilSu, @IdModuloSu, 1, GETDATE(), @IdEmpresaSu);
END

SELECT @IdEmpleadoSu = IdEmpleados FROM dbo.EmpleadosP WHERE IdEmpresa = @IdEmpresaSu AND Nombre = N'Luis Reparto';
IF @IdEmpleadoSu IS NULL
BEGIN
    INSERT INTO dbo.EmpleadosP (Nombre, Telefono, Celular, Ocupacion, ComisionServicio, ComisionProductos, Estado, IdEmpresa, Nota)
    VALUES (N'Luis Reparto', N'8095550181', N'8095550181', N'Repartidor', 0, 0, 1, @IdEmpresaSu, N'Demo Pedir/Reparto');
    SET @IdEmpleadoSu = SCOPE_IDENTITY();
END

SELECT @IdUsuarioSu = IdUsuario FROM dbo.Usuarios WHERE IdEmpresa = @IdEmpresaSu AND UserName = @UserReparto;
IF @IdUsuarioSu IS NULL AND @IdPerfilSu IS NOT NULL AND @IdEmpleadoSu IS NOT NULL
BEGIN
    INSERT INTO dbo.Usuarios
    (
        IdEmpresa, UserName, Correo, PasswordHash, Estado, IdPerfil, IdEmpleado,
        FechaCreacion,
        PuedeEliminarOrden, PuedeEliminarItemCarrito, PuedeDisminuirCantidadCarrito, PuedeEditarPrecioCarrito
    )
    VALUES
    (
        @IdEmpresaSu, @UserReparto, @UserReparto, @HashReparto, 1, @IdPerfilSu, @IdEmpleadoSu,
        GETDATE(),
        0, 0, 0, 0
    );
    SET @IdUsuarioSu = SCOPE_IDENTITY();
END

IF @IdUsuarioSu IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.DeliveryRepartidor WHERE IdEmpresa = @IdEmpresaSu AND IdUsuario = @IdUsuarioSu)
BEGIN
    INSERT INTO dbo.DeliveryRepartidor (IdEmpresa, IdUsuario, Activo, Disponible, FechaCreacion)
    VALUES (@IdEmpresaSu, @IdUsuarioSu, 1, 1, GETDATE());
END
GO
