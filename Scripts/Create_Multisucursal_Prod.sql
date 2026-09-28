-- ============================================================
-- Multisucursal v1: Sucursal, UsuarioSucursal, backfill Principal
-- Base: AlahiaPos_Prod
-- Idempotente. No fusiona empresas. No toca e-CF.
-- ============================================================
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo puede ejecutarse en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

-- ------------------------------------------------------------
-- 1. Tabla Sucursal
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Sucursal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sucursal
    (
        IdSucursal          INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Sucursal PRIMARY KEY,
        IdEmpresa           INT NOT NULL,
        Codigo              NVARCHAR(20) NOT NULL,
        Nombre              NVARCHAR(150) NOT NULL,
        EsPrincipal         BIT NOT NULL
            CONSTRAINT DF_Sucursal_EsPrincipal DEFAULT (0),
        Activa              BIT NOT NULL
            CONSTRAINT DF_Sucursal_Activa DEFAULT (1),
        Direccion           NVARCHAR(250) NULL,
        Telefono            NVARCHAR(40) NULL,
        Municipio           NVARCHAR(80) NULL,
        Provincia           NVARCHAR(80) NULL,
        Latitude            NVARCHAR(40) NULL,
        Longitude           NVARCHAR(40) NULL,
        ApiPrint            NVARCHAR(300) NULL,
        IdAlmacenPrincipal  INT NULL,
        FechaCreacion       DATETIME NOT NULL
            CONSTRAINT DF_Sucursal_FechaCreacion DEFAULT (GETDATE()),
        IdUsuarioCreacion   INT NULL,
        CONSTRAINT FK_Sucursal_Empresa
            FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Sucursal_EmpresaCodigo'
      AND object_id = OBJECT_ID(N'dbo.Sucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_Sucursal_EmpresaCodigo
        ON dbo.Sucursal (IdEmpresa, Codigo);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Sucursal_EmpresaPrincipal'
      AND object_id = OBJECT_ID(N'dbo.Sucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_Sucursal_EmpresaPrincipal
        ON dbo.Sucursal (IdEmpresa)
        WHERE EsPrincipal = 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Sucursal_EmpresaActiva'
      AND object_id = OBJECT_ID(N'dbo.Sucursal')
)
BEGIN
    CREATE INDEX IX_Sucursal_EmpresaActiva
        ON dbo.Sucursal (IdEmpresa, Activa);
END
GO

-- ------------------------------------------------------------
-- 2. UsuarioSucursal
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.UsuarioSucursal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioSucursal
    (
        IdUsuarioSucursal INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UsuarioSucursal PRIMARY KEY,
        IdUsuario         INT NOT NULL,
        IdSucursal        INT NOT NULL,
        EsDefault         BIT NOT NULL
            CONSTRAINT DF_UsuarioSucursal_EsDefault DEFAULT (0),
        Activo            BIT NOT NULL
            CONSTRAINT DF_UsuarioSucursal_Activo DEFAULT (1),
        FechaCreacion     DATETIME NOT NULL
            CONSTRAINT DF_UsuarioSucursal_FechaCreacion DEFAULT (GETDATE()),
        CONSTRAINT FK_UsuarioSucursal_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios (IdUsuario),
        CONSTRAINT FK_UsuarioSucursal_Sucursal
            FOREIGN KEY (IdSucursal) REFERENCES dbo.Sucursal (IdSucursal)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_UsuarioSucursal_UsuarioSucursal'
      AND object_id = OBJECT_ID(N'dbo.UsuarioSucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_UsuarioSucursal_UsuarioSucursal
        ON dbo.UsuarioSucursal (IdUsuario, IdSucursal);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_UsuarioSucursal_Default'
      AND object_id = OBJECT_ID(N'dbo.UsuarioSucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_UsuarioSucursal_Default
        ON dbo.UsuarioSucursal (IdUsuario)
        WHERE EsDefault = 1 AND Activo = 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_UsuarioSucursal_Usuario'
      AND object_id = OBJECT_ID(N'dbo.UsuarioSucursal')
)
BEGIN
    CREATE INDEX IX_UsuarioSucursal_Usuario
        ON dbo.UsuarioSucursal (IdUsuario)
        INCLUDE (IdSucursal, EsDefault, Activo);
END
GO

-- ------------------------------------------------------------
-- 3. Auditoría de cambio de sucursal
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.SucursalCambioLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SucursalCambioLog
    (
        IdSucursalCambioLog INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SucursalCambioLog PRIMARY KEY,
        IdUsuario           INT NOT NULL,
        IdEmpresa           INT NOT NULL,
        IdSucursalOrigen    INT NULL,
        IdSucursalDestino   INT NOT NULL,
        Fecha               DATETIME NOT NULL
            CONSTRAINT DF_SucursalCambioLog_Fecha DEFAULT (GETDATE()),
        Dispositivo         NVARCHAR(150) NULL,
        Ip                  NVARCHAR(64) NULL,
        CONSTRAINT FK_SucursalCambioLog_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios (IdUsuario),
        CONSTRAINT FK_SucursalCambioLog_Destino
            FOREIGN KEY (IdSucursalDestino) REFERENCES dbo.Sucursal (IdSucursal)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SucursalCambioLog_UsuarioFecha'
      AND object_id = OBJECT_ID(N'dbo.SucursalCambioLog')
)
BEGIN
    CREATE INDEX IX_SucursalCambioLog_UsuarioFecha
        ON dbo.SucursalCambioLog (IdUsuario, Fecha DESC);
END
GO

-- ------------------------------------------------------------
-- 4. Columnas nuevas (nullable para no romper escrituras v1)
-- ------------------------------------------------------------
IF COL_LENGTH(N'dbo.Usuarios', N'IdSucursalActiva') IS NULL
    ALTER TABLE dbo.Usuarios ADD IdSucursalActiva INT NULL;
GO

IF OBJECT_ID(N'dbo.Almacenes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Almacenes', N'IdSucursal') IS NULL
    ALTER TABLE dbo.Almacenes ADD IdSucursal INT NULL;
GO

IF COL_LENGTH(N'dbo.MovimientosInventario', N'IdSucursal') IS NULL
    ALTER TABLE dbo.MovimientosInventario ADD IdSucursal INT NULL;
GO

IF COL_LENGTH(N'dbo.MovimientosInventario', N'IdSucursalDestino') IS NULL
    ALTER TABLE dbo.MovimientosInventario ADD IdSucursalDestino INT NULL;
GO

DECLARE @Tablas TABLE (Nombre SYSNAME NOT NULL);
INSERT INTO @Tablas (Nombre) VALUES
    (N'FacturaHeaders'),
    (N'CajaApertura'),
    (N'CajaCierre'),
    (N'CajaMovimiento'),
    (N'OrdenCompraHeaders'),
    (N'Gastos'),
    (N'Ingresos'),
    (N'PosTerminal'),
    (N'Cocinas'),
    (N'PedidoOnline'),
    (N'Parametros'),
    (N'ConduceHeader'),
    (N'NotasCredito');

DECLARE @nombre SYSNAME;
DECLARE @sql NVARCHAR(400);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @Tablas;
OPEN cur;
FETCH NEXT FROM cur INTO @nombre;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @nombre, N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.' + @nombre, N'IdSucursal') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@nombre) + N' ADD IdSucursal INT NULL;';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM cur INTO @nombre;
END
CLOSE cur;
DEALLOCATE cur;
GO

-- ------------------------------------------------------------
-- 5. Sucursal Principal por empresa existente
-- ------------------------------------------------------------
INSERT INTO dbo.Sucursal
(
    IdEmpresa, Codigo, Nombre, EsPrincipal, Activa,
    Direccion, Telefono, Municipio, Provincia, Latitude, Longitude,
    ApiPrint, FechaCreacion
)
SELECT
    e.IdEmpresa,
    N'PRINC',
    N'Sucursal Principal',
    1,
    1,
    e.Direccion,
    e.Telefono,
    e.Municipio,
    e.Provincia,
    e.Latitude,
    e.Longitude,
    e.ApiPrint,
    GETDATE()
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Sucursal s
    WHERE s.IdEmpresa = e.IdEmpresa AND s.EsPrincipal = 1
);
GO

-- ------------------------------------------------------------
-- 6. Almacén principal por sucursal (crear si falta)
-- ------------------------------------------------------------
INSERT INTO dbo.Almacenes
(
    Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion, IdSucursal
)
SELECT
    N'Principal',
    N'Almacén principal',
    s.IdEmpresa,
    1,
    1,
    GETDATE(),
    s.IdSucursal
FROM dbo.Sucursal s
WHERE s.EsPrincipal = 1
  AND NOT EXISTS (
        SELECT 1 FROM dbo.Almacenes a WHERE a.IdEmpresa = s.IdEmpresa
  );
GO

UPDATE a
SET a.IdSucursal = s.IdSucursal
FROM dbo.Almacenes a
INNER JOIN dbo.Sucursal s
    ON s.IdEmpresa = a.IdEmpresa AND s.EsPrincipal = 1
WHERE a.IdSucursal IS NULL;
GO

UPDATE s
SET s.IdAlmacenPrincipal = a.IdAlmacen
FROM dbo.Sucursal s
INNER JOIN dbo.Almacenes a
    ON a.IdEmpresa = s.IdEmpresa
   AND a.IdSucursal = s.IdSucursal
   AND a.EsPrincipal = 1
WHERE s.IdAlmacenPrincipal IS NULL;
GO

UPDATE s
SET s.IdAlmacenPrincipal = a.IdAlmacen
FROM dbo.Sucursal s
INNER JOIN dbo.Almacenes a
    ON a.IdEmpresa = s.IdEmpresa
   AND a.IdSucursal = s.IdSucursal
WHERE s.IdAlmacenPrincipal IS NULL;
GO

-- ------------------------------------------------------------
-- 7. Backfill operativo = sucursal principal de la empresa
-- ------------------------------------------------------------
UPDATE h SET h.IdSucursal = s.IdSucursal
FROM dbo.FacturaHeaders h
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = h.IdEmpresa AND s.EsPrincipal = 1
WHERE h.IdSucursal IS NULL;
GO

UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.CajaApertura x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.CajaCierre x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.CajaMovimiento x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.MovimientosInventario x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.OrdenCompraHeaders', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.OrdenCompraHeaders x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.Gastos', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.Gastos x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.Ingresos', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.Ingresos x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.PosTerminal', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.PosTerminal x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.Cocinas', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.Cocinas x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.PedidoOnline', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.PedidoOnline x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.Parametros', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.Parametros x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL AND x.Tipo = N'POS';
GO

IF OBJECT_ID(N'dbo.ConduceHeader', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.ConduceHeader x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

IF OBJECT_ID(N'dbo.NotasCredito', N'U') IS NOT NULL
UPDATE x SET x.IdSucursal = s.IdSucursal
FROM dbo.NotasCredito x
INNER JOIN dbo.Sucursal s ON s.IdEmpresa = x.IdEmpresa AND s.EsPrincipal = 1
WHERE x.IdSucursal IS NULL;
GO

-- ------------------------------------------------------------
-- 8. Usuarios: acceso a Principal + sucursal activa
-- ------------------------------------------------------------
INSERT INTO dbo.UsuarioSucursal (IdUsuario, IdSucursal, EsDefault, Activo, FechaCreacion)
SELECT
    u.IdUsuario,
    s.IdSucursal,
    1,
    1,
    GETDATE()
FROM dbo.Usuarios u
INNER JOIN dbo.Sucursal s
    ON s.IdEmpresa = u.IdEmpresa AND s.EsPrincipal = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.UsuarioSucursal us
    WHERE us.IdUsuario = u.IdUsuario AND us.IdSucursal = s.IdSucursal
);
GO

UPDATE u
SET u.IdSucursalActiva = s.IdSucursal
FROM dbo.Usuarios u
INNER JOIN dbo.Sucursal s
    ON s.IdEmpresa = u.IdEmpresa AND s.EsPrincipal = 1
WHERE u.IdSucursalActiva IS NULL;
GO

-- ------------------------------------------------------------
-- 9. FKs e índices de consulta (después del backfill)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Usuarios_SucursalActiva')
    ALTER TABLE dbo.Usuarios
        ADD CONSTRAINT FK_Usuarios_SucursalActiva
        FOREIGN KEY (IdSucursalActiva) REFERENCES dbo.Sucursal (IdSucursal);
GO

IF OBJECT_ID(N'dbo.Almacenes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Almacenes_Sucursal')
    ALTER TABLE dbo.Almacenes
        ADD CONSTRAINT FK_Almacenes_Sucursal
        FOREIGN KEY (IdSucursal) REFERENCES dbo.Sucursal (IdSucursal);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FacturaHeaders_EmpresaSucursalFecha'
      AND object_id = OBJECT_ID(N'dbo.FacturaHeaders')
)
BEGIN
    CREATE INDEX IX_FacturaHeaders_EmpresaSucursalFecha
        ON dbo.FacturaHeaders (IdEmpresa, IdSucursal, FechaInseccion);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CajaApertura_EmpresaSucursalEstado'
      AND object_id = OBJECT_ID(N'dbo.CajaApertura')
)
BEGIN
    CREATE INDEX IX_CajaApertura_EmpresaSucursalEstado
        ON dbo.CajaApertura (IdEmpresa, IdSucursal, Estado);
END
GO

IF OBJECT_ID(N'dbo.Almacenes', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Almacenes_EmpresaSucursal'
      AND object_id = OBJECT_ID(N'dbo.Almacenes')
)
BEGIN
    CREATE INDEX IX_Almacenes_EmpresaSucursal
        ON dbo.Almacenes (IdEmpresa, IdSucursal);
END
GO

PRINT 'Multisucursal v1 listo en AlahiaPos_Prod.';
GO

