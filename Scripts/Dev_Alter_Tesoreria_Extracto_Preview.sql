-- AlahiaPos_Dev only: preview editable de extractos bancarios.
-- NO ejecutar en Prod. Idempotente.
USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- Columnas de trazabilidad de parseo ---------- */
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'AdapterUsado') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD AdapterUsado NVARCHAR(60) NULL;
GO

IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'ParserWarnings') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD ParserWarnings NVARCHAR(MAX) NULL;
GO

/* ---------- Estados extracto: incluye PREVIEW ---------- */
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TesExtImp_Estado'
      AND parent_object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
    ALTER TABLE dbo.TesoreriaExtractoImport DROP CONSTRAINT CK_TesExtImp_Estado;
GO

ALTER TABLE dbo.TesoreriaExtractoImport WITH NOCHECK
ADD CONSTRAINT CK_TesExtImp_Estado CHECK (
    Estado IN ('PREVIEW', 'CARGADO', 'PROCESADO', 'CERRADO', 'ANULADO')
);
GO

/* ---------- Dedupe por hash: excluir PREVIEW y ANULADO ----------
   Permite reintentar/descartar previews sin debilitar definitivos.
   El índice único filtrado anterior incluía PREVIEW y bloqueaba reintentos. */
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesExtImp_EmpCuentaHash'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
    DROP INDEX UX_TesExtImp_EmpCuentaHash ON dbo.TesoreriaExtractoImport;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesExtImp_EmpCuentaHash_Definitivo'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    -- Filtered indexes no admiten NOT IN; listar estados definitivos.
    CREATE UNIQUE INDEX UX_TesExtImp_EmpCuentaHash_Definitivo
        ON dbo.TesoreriaExtractoImport (IdEmpresa, IdCuentaFinanciera, HashArchivo)
        WHERE HashArchivo IS NOT NULL
          AND Estado IN ('CARGADO', 'PROCESADO', 'CERRADO');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TesExtImp_Estado'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    CREATE INDEX IX_TesExtImp_Estado
        ON dbo.TesoreriaExtractoImport (IdEmpresa, IdCuentaFinanciera, Estado);
END
GO

PRINT 'Dev_Alter_Tesoreria_Extracto_Preview.sql OK';
GO
