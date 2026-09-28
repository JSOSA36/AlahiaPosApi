-- ============================================================
-- Inventario multisucursal: existencias únicas por almacén+producto
-- Base: AlahiaPos_Prod
-- Idempotente. Fusiona duplicados antes del índice único.
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

-- Fusionar filas duplicadas (IdAlmacen, IdProducto): conservar la de menor Id.
IF OBJECT_ID(N'dbo.AlmacenExistencias', N'U') IS NOT NULL
BEGIN
    ;WITH agrupado AS
    (
        SELECT
            IdAlmacen,
            IdProducto,
            MIN(IdAlmacenExistencia) AS IdKeep,
            SUM(Existencia) AS TotalExistencia
        FROM dbo.AlmacenExistencias
        GROUP BY IdAlmacen, IdProducto
        HAVING COUNT(*) > 1
    )
    UPDATE e
    SET e.Existencia = a.TotalExistencia
    FROM dbo.AlmacenExistencias e
    INNER JOIN agrupado a
        ON e.IdAlmacenExistencia = a.IdKeep;

    ;WITH dups AS
    (
        SELECT
            IdAlmacenExistencia,
            ROW_NUMBER() OVER (
                PARTITION BY IdAlmacen, IdProducto
                ORDER BY IdAlmacenExistencia
            ) AS rn
        FROM dbo.AlmacenExistencias
    )
    DELETE FROM dups WHERE rn > 1;
END
GO

IF OBJECT_ID(N'dbo.AlmacenExistencias', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_AlmacenExistencias_AlmacenProducto'
          AND object_id = OBJECT_ID(N'dbo.AlmacenExistencias')
   )
BEGIN
    CREATE UNIQUE INDEX UX_AlmacenExistencias_AlmacenProducto
        ON dbo.AlmacenExistencias (IdAlmacen, IdProducto);
END
GO

