-- ============================================================
-- EmpleadosP.IdSucursal — sucursal operativa del colaborador.
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

IF COL_LENGTH(N'dbo.EmpleadosP', N'IdSucursal') IS NULL
BEGIN
    ALTER TABLE dbo.EmpleadosP
        ADD IdSucursal INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_EmpleadosP_Sucursal'
      AND parent_object_id = OBJECT_ID(N'dbo.EmpleadosP')
)
BEGIN
    ALTER TABLE dbo.EmpleadosP
        ADD CONSTRAINT FK_EmpleadosP_Sucursal
            FOREIGN KEY (IdSucursal) REFERENCES dbo.Sucursal (IdSucursal);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EmpleadosP_IdSucursal'
      AND object_id = OBJECT_ID(N'dbo.EmpleadosP')
)
BEGIN
    CREATE INDEX IX_EmpleadosP_IdSucursal
        ON dbo.EmpleadosP (IdEmpresa, IdSucursal);
END
GO

PRINT 'EmpleadosP.IdSucursal listo en AlahiaPos_Prod.';
GO
