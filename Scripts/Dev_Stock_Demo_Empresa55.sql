-- AlahiaPos_Dev — Stock de demo para empresa 55 (POS: AlmacenExistencias).
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdAlmacen INT;

SELECT @IdAlmacen = IdAlmacen
FROM dbo.Almacenes
WHERE IdEmpresa = @IdEmpresa AND EsPrincipal = 1 AND Activo = 1;

IF @IdAlmacen IS NULL
    SELECT TOP 1 @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE IdEmpresa = @IdEmpresa AND Activo = 1;

IF @IdAlmacen IS NULL
BEGIN
    RAISERROR(N'No hay almacén activo para empresa 55.', 16, 1);
    RETURN;
END;

UPDATE dbo.Productos
SET IdAlmacen = @IdAlmacen
WHERE IdEmpresa = @IdEmpresa
  AND ISNULL(EsServicio, 0) = 0
  AND ISNULL(IdAlmacen, 0) <> @IdAlmacen;

;WITH Src AS (
    SELECT
        p.IdProducto,
        CAST(
            CASE
                WHEN ISNULL(p.Cantidad, 0) > 0 THEN p.Cantidad
                ELSE 48
            END AS DECIMAL(18,2)
        ) AS Existencia,
        CAST(ISNULL(p.PrecioCompra, 0) AS DECIMAL(18,2)) AS Costo
    FROM dbo.Productos p
    WHERE p.IdEmpresa = @IdEmpresa
      AND ISNULL(p.EsServicio, 0) = 0
      AND ISNULL(p.ControlarStock, 0) = 1
)
MERGE dbo.AlmacenExistencias AS t
USING Src AS s
    ON t.IdEmpresa = @IdEmpresa
   AND t.IdAlmacen = @IdAlmacen
   AND t.IdProducto = s.IdProducto
WHEN MATCHED THEN
    UPDATE SET
        t.Existencia = CASE WHEN ISNULL(t.Existencia, 0) > 0 THEN t.Existencia ELSE s.Existencia END,
        t.FechaUltimoMovimiento = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (IdAlmacen, IdProducto, Existencia, CostoPromedio, StockMinimo, IdEmpresa, FechaUltimoMovimiento)
    VALUES (@IdAlmacen, s.IdProducto, s.Existencia, s.Costo, 0, @IdEmpresa, GETDATE());

SELECT
    COUNT(*) AS ProductosConStock,
    SUM(e.Existencia) AS Unidades
FROM dbo.AlmacenExistencias e
WHERE e.IdEmpresa = @IdEmpresa AND e.IdAlmacen = @IdAlmacen AND e.Existencia > 0;

SELECT p.Nombre, e.Existencia
FROM dbo.Productos p
JOIN dbo.AlmacenExistencias e ON e.IdProducto = p.IdProducto AND e.IdEmpresa = @IdEmpresa
WHERE p.IdEmpresa = @IdEmpresa AND p.IdCategoria = 357
ORDER BY p.Nombre;
