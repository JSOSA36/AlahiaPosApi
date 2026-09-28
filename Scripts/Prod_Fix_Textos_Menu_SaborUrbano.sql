-- Corrige tildes rotas en el menú público de Sabor Urbano (IdEmpresa=73).
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END

UPDATE dbo.Categorias
SET Nombre = N'Producción propia'
WHERE IdEmpresa = 73 AND IdCategoria = 501;

UPDATE dbo.Productos
SET Nombre = N'Bizcocho clásico',
    Descripcion = N'Bizcocho casero'
WHERE IdEmpresa = 73 AND IdProducto = 2821;

UPDATE dbo.Productos
SET Nombre = N'Pastelito de carne',
    Descripcion = N'Pastelito de carne'
WHERE IdEmpresa = 73 AND IdProducto = 2820;

UPDATE dbo.Productos
SET Nombre = N'Hamburguesa Clásica'
WHERE IdEmpresa = 73 AND IdProducto = 2810;

UPDATE dbo.Productos
SET Descripcion = N'Porción mediana'
WHERE IdEmpresa = 73 AND IdProducto = 2808;

UPDATE dbo.Productos
SET Descripcion = N'Bebida fría 16 oz'
WHERE IdEmpresa = 73 AND IdProducto = 2809;

UPDATE dbo.Productos
SET Nombre = N'Azúcar blanca'
WHERE IdEmpresa = 73 AND IdProducto = 2814;

UPDATE dbo.Productos
SET Descripcion = REPLACE(REPLACE(ISNULL(Descripcion, N''), N'�?"', N'–'), N'producci��n', N'producción')
WHERE IdEmpresa = 73 AND Descripcion LIKE N'%terminado%';

UPDATE dbo.Productos
SET Nombre = N'Aceite vegetal',
    Descripcion = N'Materia prima – galones'
WHERE IdEmpresa = 73 AND IdProducto = 2813;

UPDATE dbo.Productos
SET Descripcion = N'Materia prima – libras'
WHERE IdEmpresa = 73 AND IdProducto IN (2814, 2815, 2816, 2819);

UPDATE dbo.Productos
SET Descripcion = N'Materia prima – unidades'
WHERE IdEmpresa = 73 AND IdProducto = 2817;

UPDATE dbo.Productos
SET Descripcion = N'Materia prima – litros'
WHERE IdEmpresa = 73 AND IdProducto = 2818;

SELECT IdCategoria, Nombre FROM dbo.Categorias WHERE IdEmpresa = 73 ORDER BY Nombre;
SELECT IdProducto, Nombre, Descripcion FROM dbo.Productos WHERE IdEmpresa = 73 AND IdProducto IN (2808,2809,2810,2820,2821) ORDER BY Nombre;
GO
