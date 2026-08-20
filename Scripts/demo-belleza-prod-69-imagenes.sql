/* Demo 69: quitar categorías genéricas Productos/Servicios, copiar imágenes de Dismerling (1). */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Origen INT = 1;
DECLARE @Destino INT = 69;
DECLARE @IdApl INT, @IdUnas INT, @IdProdBel INT;

SELECT @IdApl = d.IdCategoria
FROM dbo.Categorias d
INNER JOIN dbo.Categorias o ON o.IdEmpresa = @Origen AND o.Nombre = d.Nombre
WHERE d.IdEmpresa = @Destino AND o.Nombre = N'Aplicacion de Servicios';

SELECT @IdProdBel = d.IdCategoria
FROM dbo.Categorias d
INNER JOIN dbo.Categorias o ON o.IdEmpresa = @Origen AND o.Nombre = d.Nombre
WHERE d.IdEmpresa = @Destino AND o.Nombre = N'Productos de Belleza';

SELECT @IdUnas = d.IdCategoria
FROM dbo.Categorias d
INNER JOIN dbo.Categorias o ON o.IdEmpresa = @Origen AND o.Nombre = d.Nombre
WHERE d.IdEmpresa = @Destino AND o.IdCategoria = 454;

BEGIN TRAN;

/* 1) Reasignar ítems que quedaron en Productos / Servicios */
UPDATE dbo.Productos
SET IdCategoria = @IdApl
WHERE IdEmpresa = @Destino
  AND Nombre IN (N'Corte dama', N'Tinte completo', N'Keratina');

UPDATE dbo.Productos
SET IdCategoria = @IdUnas
WHERE IdEmpresa = @Destino
  AND Nombre IN (N'Manicure', N'Pedicure');

UPDATE dbo.Productos
SET IdCategoria = @IdProdBel
WHERE IdEmpresa = @Destino
  AND Nombre = N'Shampoo tratamiento';

/* 2) Imágenes de categorías = mismas URLs de Dismerling */
UPDATE d
SET d.ImagenPath = o.ImagenPath
FROM dbo.Categorias d
INNER JOIN dbo.Categorias o
    ON o.IdEmpresa = @Origen
   AND LTRIM(RTRIM(d.Nombre)) = LTRIM(RTRIM(o.Nombre))
WHERE d.IdEmpresa = @Destino
  AND ISNULL(o.ImagenPath, N'') <> N'';

/* 3) Imágenes de productos/servicios = Dismerling (si hay varias filas del mismo nombre, preferir la que tiene foto) */
;WITH origen AS (
    SELECT
        LTRIM(RTRIM(LOWER(p.Nombre))) AS NomKey,
        p.Imagen1,
        p.Imagen2,
        p.Imagen3,
        ROW_NUMBER() OVER (
            PARTITION BY LTRIM(RTRIM(LOWER(p.Nombre)))
            ORDER BY CASE WHEN ISNULL(p.Imagen1, N'') = N'' THEN 1 ELSE 0 END, p.IdProducto
        ) AS rn
    FROM dbo.Productos p
    WHERE p.IdEmpresa = @Origen
)
UPDATE d
SET
    d.Imagen1 = o.Imagen1,
    d.Imagen2 = o.Imagen2,
    d.Imagen3 = o.Imagen3
FROM dbo.Productos d
INNER JOIN origen o ON o.NomKey = LTRIM(RTRIM(LOWER(d.Nombre))) AND o.rn = 1
WHERE d.IdEmpresa = @Destino
  AND ISNULL(o.Imagen1, N'') <> N'';

/* 4) Quitar categorías genéricas si ya no tienen productos */
DELETE FROM dbo.Categorias
WHERE IdEmpresa = @Destino
  AND Nombre IN (N'Productos', N'Servicios')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Productos p
      WHERE p.IdEmpresa = @Destino AND p.IdCategoria = Categorias.IdCategoria
  );

COMMIT TRAN;

SELECT IdCategoria, Nombre, CASE WHEN ISNULL(ImagenPath, N'') = N'' THEN N'SIN FOTO' ELSE N'OK' END AS Img
FROM dbo.Categorias
WHERE IdEmpresa = @Destino
ORDER BY Nombre;

SELECT
    SUM(CASE WHEN ISNULL(Imagen1, N'') <> N'' THEN 1 ELSE 0 END) AS ConImagen,
    COUNT(*) AS Total
FROM dbo.Productos
WHERE IdEmpresa = @Destino;

SELECT Nombre
FROM dbo.Productos
WHERE IdEmpresa = @Destino AND ISNULL(Imagen1, N'') = N''
ORDER BY Nombre;
