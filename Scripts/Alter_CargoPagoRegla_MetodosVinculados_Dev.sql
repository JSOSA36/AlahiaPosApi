-- Métodos reales de la empresa en cargos de cobro (Billet BHD, Azul, etc.)
USE AlahiaPos_Dev;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Dev.', 16, 1);
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

PRINT 'CargoPagoRegla.MetodosVinculados listo en AlahiaPos_Dev.';
GO
