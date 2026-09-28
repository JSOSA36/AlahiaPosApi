-- Terraza 27 (55) Prod: dejar un solo Four Loko — Gold (1846),
-- el que se ajustó ayer (10/09/2026) a 5 unidades.
-- Pedido del cliente: no dejar todos los sabores.
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

DECLARE @Keep INT = 1846; -- Four Loko Gold, stock 5

IF NOT EXISTS (
    SELECT 1 FROM dbo.Productos
    WHERE IdProducto = @Keep AND IdEmpresa = 55 AND Nombre = N'Four Loko Gold'
)
BEGIN
    RAISERROR(N'No está Four Loko Gold (1846) en Terraza 55.', 16, 1);
    RETURN;
END;

DECLARE @Off TABLE (IdProducto INT PRIMARY KEY);
INSERT INTO @Off (IdProducto)
SELECT IdProducto
FROM dbo.Productos
WHERE IdEmpresa = 55
  AND Nombre LIKE N'Four Loko%'
  AND IdProducto <> @Keep;

BEGIN TRAN;

UPDATE p
SET p.IsActivo = 0,
    p.Stock = 0
FROM dbo.Productos p
INNER JOIN @Off x ON x.IdProducto = p.IdProducto
WHERE p.IdEmpresa = 55;

UPDATE e
SET e.Existencia = 0
FROM dbo.AlmacenExistencias e
INNER JOIN @Off x ON x.IdProducto = e.IdProducto
WHERE e.IdEmpresa = 55;

COMMIT TRAN;

SELECT p.IdProducto, LEFT(p.Nombre, 40) Nombre, p.Stock, p.IsActivo, e.Existencia
FROM dbo.Productos p
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = p.IdProducto AND e.IdEmpresa = p.IdEmpresa
WHERE p.IdEmpresa = 55 AND p.Nombre LIKE N'Four Loko%'
ORDER BY p.IsActivo DESC, p.Nombre;
GO
