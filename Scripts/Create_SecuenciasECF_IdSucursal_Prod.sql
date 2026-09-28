-- ============================================================
-- SecuenciasECF.IdSucursal — sucursal que USA el rango.
-- La autorización DGII sigue siendo de la empresa.
-- Base: AlahiaPos_Prod
-- NO ejecutar sin autorización explícita.
-- ============================================================
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script es solo para AlahiaPos_Prod. Abortado.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SecuenciasECF', N'U') IS NULL
BEGIN
    RAISERROR('No existe dbo.SecuenciasECF. Abortado.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.SecuenciasECF', N'IdSucursal') IS NULL
BEGIN
    ALTER TABLE dbo.SecuenciasECF
        ADD IdSucursal INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_SecuenciasECF_Sucursal'
      AND parent_object_id = OBJECT_ID(N'dbo.SecuenciasECF')
)
BEGIN
    ALTER TABLE dbo.SecuenciasECF
        ADD CONSTRAINT FK_SecuenciasECF_Sucursal
            FOREIGN KEY (IdSucursal) REFERENCES dbo.Sucursal (IdSucursal);
END
GO

DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.SecuenciasECF;'
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.SecuenciasECF')
  AND i.is_unique = 1
  AND i.is_primary_key = 0
  AND i.name IS NOT NULL
  AND EXISTS (
        SELECT 1
        FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND c.name IN (N'IdEmpresa', N'TipoNCF', N'TipoEcfDgii')
        GROUP BY ic.index_id
        HAVING COUNT(*) >= 2
  )
  AND NOT EXISTS (
        SELECT 1
        FROM sys.index_columns ic2
        JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id
        WHERE ic2.object_id = i.object_id
          AND ic2.index_id = i.index_id
          AND c2.name = N'IdSucursal'
  );

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_SecuenciasECF_Empresa_Tipo_Sucursal_Activa'
      AND object_id = OBJECT_ID(N'dbo.SecuenciasECF')
)
BEGIN
    CREATE UNIQUE INDEX UX_SecuenciasECF_Empresa_Tipo_Sucursal_Activa
        ON dbo.SecuenciasECF (IdEmpresa, TipoEcfDgii, IdSucursal)
        WHERE Activo = 1 AND TipoEcfDgii IS NOT NULL AND IdSucursal IS NOT NULL;
END
GO

PRINT 'SecuenciasECF.IdSucursal listo en AlahiaPos_Prod.';
GO
