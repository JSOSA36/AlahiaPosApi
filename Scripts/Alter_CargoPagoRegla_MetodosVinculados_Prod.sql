-- Métodos reales de la empresa en cargos de cobro.
-- Requiere autorización explícita para ejecutar en Prod.
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.CargoPagoRegla', N'MetodosVinculados') IS NULL
BEGIN
    ALTER TABLE dbo.CargoPagoRegla
        ADD MetodosVinculados NVARCHAR(2000) NOT NULL
            CONSTRAINT DF_CargoPagoRegla_MetodosVinculados DEFAULT (N'');
END
GO

PRINT 'CargoPagoRegla.MetodosVinculados listo en AlahiaPos_Prod.';
GO
