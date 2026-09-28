-- Terraza 27 (55) Prod: quitar servicios de carwash del catálogo.
-- María: eliminar todos los servicios del carwash.
-- Combos/tragos/picaderas que estaban mal en CARWASH se recategorizan (no se borran).
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

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 55 AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @CatCervezas INT = (SELECT TOP 1 IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'CERVEZAS');
DECLARE @CatLicores INT = (SELECT TOP 1 IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'LICORES');
DECLARE @CatSnacks INT = (SELECT TOP 1 IdCategoria FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'SNACKS');

DECLARE @Serv TABLE (IdProducto INT PRIMARY KEY);

INSERT INTO @Serv (IdProducto)
SELECT p.IdProducto
FROM dbo.Productos p
WHERE p.IdEmpresa = @IdEmpresa
  AND (
        p.Nombre LIKE N'Lavado%'
     OR p.Nombre LIKE N'LAVADO%'
     OR p.Nombre LIKE N'Desarme%'
     OR p.Nombre LIKE N'Encerado%'
     OR p.Nombre LIKE N'Interior Sin%'
     OR p.Nombre = N'Tratamiento De Ozono'
  );

DECLARE @Borrar TABLE (IdProducto INT PRIMARY KEY);
DECLARE @Ocultar TABLE (IdProducto INT PRIMARY KEY);

INSERT INTO @Borrar (IdProducto)
SELECT s.IdProducto
FROM @Serv s
WHERE NOT EXISTS (SELECT 1 FROM dbo.FacturaDetalles d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.MovimientosInventarioDetalle d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.NotasCreditoDetalle d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.OrdenCompraDetalles d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.AjusteInventarioDetalles d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.ConduceDetalles d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.DevolucionesDetalles d WHERE d.IdProducto = s.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.DevolucionesClienteDetalles d WHERE d.IdProducto = s.IdProducto);

INSERT INTO @Ocultar (IdProducto)
SELECT s.IdProducto
FROM @Serv s
WHERE NOT EXISTS (SELECT 1 FROM @Borrar b WHERE b.IdProducto = s.IdProducto);

BEGIN TRAN;

DELETE e FROM dbo.AlmacenExistencias e INNER JOIN @Borrar b ON b.IdProducto = e.IdProducto;
IF OBJECT_ID(N'dbo.Variaciones', N'U') IS NOT NULL
    DELETE v FROM dbo.Variaciones v INNER JOIN @Borrar b ON b.IdProducto = v.IdProducto;
IF OBJECT_ID(N'dbo.EmpleadoServicioComisions', N'U') IS NOT NULL
    DELETE c FROM dbo.EmpleadoServicioComisions c INNER JOIN @Borrar b ON b.IdProducto = c.IdProducto;

DELETE p
FROM dbo.Productos p
INNER JOIN @Borrar b ON b.IdProducto = p.IdProducto
WHERE p.IdEmpresa = @IdEmpresa;

UPDATE p
SET p.IsActivo = 0,
    p.SeVende = 0,
    p.Stock = 0
FROM dbo.Productos p
INNER JOIN @Ocultar o ON o.IdProducto = p.IdProducto
WHERE p.IdEmpresa = @IdEmpresa;

UPDATE e
SET e.Existencia = 0
FROM dbo.AlmacenExistencias e
INNER JOIN @Ocultar o ON o.IdProducto = e.IdProducto
WHERE e.IdEmpresa = @IdEmpresa;

-- Lo que quedó mal puesto en CARWASH (bar/comida) pasa a categoría de Terraza.
UPDATE p
SET p.IdCategoria = CASE
    WHEN p.Nombre LIKE N'%Cubetazo%' OR p.Nombre LIKE N'%Heineken%' OR p.Nombre LIKE N'%Miller%'
         OR p.Nombre LIKE N'%Matine%' OR p.Nombre LIKE N'%Cerveza%' THEN @CatCervezas
    WHEN p.Nombre LIKE N'%Trago%' OR p.Nombre LIKE N'%Shot%' OR p.Nombre LIKE N'%Mojito%'
         OR p.Nombre LIKE N'%Cuba Libre%' OR p.Nombre LIKE N'%Margarita%'
         OR p.Nombre LIKE N'%Combo De%' THEN @CatLicores
    ELSE @CatSnacks
END
FROM dbo.Productos p
INNER JOIN dbo.Categorias c ON c.IdCategoria = p.IdCategoria
WHERE p.IdEmpresa = @IdEmpresa
  AND c.Nombre = N'CARWASH'
  AND p.IsActivo = 1;

COMMIT TRAN;

DECLARE @NBorrar INT = (SELECT COUNT(*) FROM @Borrar);
DECLARE @NOcultar INT = (SELECT COUNT(*) FROM @Ocultar);
PRINT N'Borrados: ' + CAST(@NBorrar AS varchar(12));
PRINT N'Ocultos (ya facturados): ' + CAST(@NOcultar AS varchar(12));

SELECT p.IdProducto, LEFT(p.Nombre, 50) Nombre, p.IsActivo, p.SeVende
FROM dbo.Productos p
INNER JOIN @Ocultar o ON o.IdProducto = p.IdProducto;

SELECT LEFT(c.Nombre, 20) Categoria, COUNT(*) Items
FROM dbo.Productos p
INNER JOIN dbo.Categorias c ON c.IdCategoria = p.IdCategoria
WHERE p.IdEmpresa = 55 AND p.IsActivo = 1
GROUP BY c.Nombre
ORDER BY c.Nombre;
GO
