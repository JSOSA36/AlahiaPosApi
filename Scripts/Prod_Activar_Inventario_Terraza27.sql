-- Inventario completo Terraza prolongación 27 (MATBERT SRL) — AlahiaPos_Prod empresa 55.
-- Autorizado por el usuario: activar en producción.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdAlmacen INT;
DECLARE @IdAlmacenes INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'ALMACENES');
DECLARE @IdMovInv INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'MOVIMIENTO_INVENTARIO');
DECLARE @IdPerdidas INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'REPORTE_PERDIDAS');
DECLARE @IdConduces INT = (SELECT Id FROM dbo.Modulos WHERE Codigo = N'CONDUCES');

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'No se encontró Terraza 27 de febrero (empresa 55).', 16, 1);
    RETURN;
END;

SELECT TOP 1 @IdAlmacen = IdAlmacen
FROM dbo.Almacenes
WHERE IdEmpresa = @IdEmpresa AND Activo = 1
ORDER BY EsPrincipal DESC, IdAlmacen;

BEGIN TRAN;

IF @IdAlmacen IS NULL
BEGIN
    INSERT INTO dbo.Almacenes (Nombre, Descripcion, IdEmpresa, EsPrincipal, Activo, FechaCreacion)
    VALUES (N'Principal', N'Almacén principal Terraza 27', @IdEmpresa, 1, 1, GETDATE());
    SET @IdAlmacen = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Almacens WHERE IdAlmacen = @IdAlmacen)
BEGIN
    SET IDENTITY_INSERT dbo.Almacens ON;
    INSERT INTO dbo.Almacens (IdAlmacen, Nombre, Descripcion, IsActivo, FechaInseccion, IdEmpresa)
    VALUES (@IdAlmacen, N'Principal', N'Almacén principal Terraza 27', 1, CAST(GETDATE() AS date), @IdEmpresa);
    SET IDENTITY_INSERT dbo.Almacens OFF;
END;

DECLARE @Mods TABLE (ModuloId INT PRIMARY KEY);
INSERT INTO @Mods (ModuloId)
SELECT x.Id FROM (VALUES (@IdAlmacenes), (@IdMovInv), (@IdPerdidas), (@IdConduces)) v(Id)
JOIN dbo.Modulos x ON x.Id = v.Id
WHERE v.Id IS NOT NULL;

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.ModuloId, 1, GETDATE()
FROM @Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.ModuloId
);

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN @Mods m ON m.ModuloId = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa;

DECLARE @Perfiles TABLE (IdPerfil INT PRIMARY KEY);
INSERT INTO @Perfiles (IdPerfil)
SELECT IdPerfil FROM dbo.Perfiles
WHERE IdEmpresa = @IdEmpresa
  AND Activo = 1
  AND (
      Nombre LIKE N'%Principal%'
      OR Nombre LIKE N'%Recepci%'
  );

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.ModuloId, 1, GETDATE(), @IdEmpresa
FROM @Perfiles p
CROSS JOIN @Mods m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PerfilRoles pr
    WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.ModuloId AND pr.IdEmpresa = @IdEmpresa
);

UPDATE pr SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN @Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN @Mods m ON m.ModuloId = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa;

UPDATE dbo.Productos
SET IdAlmacen = @IdAlmacen
WHERE IdEmpresa = @IdEmpresa
  AND ISNULL(IdAlmacen, 0) <> @IdAlmacen;

INSERT INTO dbo.AlmacenExistencias (
    IdAlmacen, IdProducto, Existencia, CostoPromedio, StockMinimo, IdEmpresa, FechaUltimoMovimiento
)
SELECT
    @IdAlmacen,
    p.IdProducto,
    ISNULL(p.Stock, 0),
    ISNULL(p.PrecioCompra, 0),
    0,
    @IdEmpresa,
    GETDATE()
FROM dbo.Productos p
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AlmacenExistencias x
      WHERE x.IdAlmacen = @IdAlmacen AND x.IdProducto = p.IdProducto
  );

UPDATE dbo.MovimientosInventario
SET IdAlmacen = @IdAlmacen
WHERE IdEmpresa = @IdEmpresa
  AND IdAlmacen IS NULL;

COMMIT TRAN;

SELECT m.Codigo, em.Activo Licencia
FROM dbo.Modulos m
JOIN dbo.Empresa_Modulos em ON em.ModuloId = m.Id AND em.EmpresaId = @IdEmpresa
WHERE m.Codigo IN (N'ALMACENES', N'MOVIMIENTO_INVENTARIO', N'REPORTE_PERDIDAS', N'CONDUCES', N'PRODUCTOS', N'CATEGORIAS')
ORDER BY m.Codigo;

SELECT p.Nombre Perfil, m.Codigo, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
  AND m.Codigo IN (N'ALMACENES', N'MOVIMIENTO_INVENTARIO', N'REPORTE_PERDIDAS', N'CONDUCES')
ORDER BY p.Nombre, m.Codigo;

SELECT IdAlmacen, Nombre, EsPrincipal, Activo FROM dbo.Almacenes WHERE IdEmpresa = @IdEmpresa;
SELECT COUNT(*) ProductosAlmacen FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND IdAlmacen = @IdAlmacen;
SELECT COUNT(*) Existencias, SUM(Existencia) StockTotal
FROM dbo.AlmacenExistencias WHERE IdEmpresa = @IdEmpresa AND IdAlmacen = @IdAlmacen;
SELECT COUNT(*) Movimientos FROM dbo.MovimientosInventario WHERE IdEmpresa = @IdEmpresa;
GO
