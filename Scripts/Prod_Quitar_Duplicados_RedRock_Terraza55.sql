/*
  Terraza 55: quitar duplicados Red Rock 400/450 del mismo sabor (todos a RD$40).
  Se deja el que tiene existencia (inventario que María contó).
  Sin historial de factura ni movimiento: se eliminan.
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
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

DECLARE @Del TABLE (IdProducto INT PRIMARY KEY);
INSERT INTO @Del (IdProducto) VALUES (1778), (1815), (1826);

IF EXISTS (
    SELECT 1
    FROM dbo.FacturaDetalles d
    INNER JOIN @Del x ON x.IdProducto = d.IdProducto
)
BEGIN
    RAISERROR(N'Abortado: hay facturas sobre un duplicado.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

DELETE e FROM dbo.AlmacenExistencias e INNER JOIN @Del x ON x.IdProducto = e.IdProducto;
DELETE p FROM dbo.Productos p INNER JOIN @Del x ON x.IdProducto = p.IdProducto WHERE p.IdEmpresa = 55;

COMMIT TRAN;

SELECT IdProducto, LEFT(Nombre, 45) Nombre, Precio1, Stock, CodigoBarra
FROM dbo.Productos
WHERE IdEmpresa = 55 AND IsActivo = 1 AND Nombre LIKE N'Red Rock%'
ORDER BY Nombre;
GO
