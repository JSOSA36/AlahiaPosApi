-- Pago de nómina: cuenta de tesorería y movimiento de salida.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.NominaProceso', N'IdCuentaFinanciera') IS NULL
    ALTER TABLE dbo.NominaProceso ADD IdCuentaFinanciera INT NULL;
GO

IF COL_LENGTH(N'dbo.NominaProceso', N'IdMovimientoFinanciero') IS NULL
    ALTER TABLE dbo.NominaProceso ADD IdMovimientoFinanciero INT NULL;
GO
