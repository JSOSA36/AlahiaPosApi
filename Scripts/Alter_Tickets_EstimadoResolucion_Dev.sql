-- ============================================================
-- Tickets: estimado de resolución por cola (rango 2h–24h) — AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.Tickets', N'HorasEstimadasMin') IS NULL
    ALTER TABLE dbo.Tickets ADD HorasEstimadasMin INT NULL;
GO

IF COL_LENGTH(N'dbo.Tickets', N'HorasEstimadasMax') IS NULL
    ALTER TABLE dbo.Tickets ADD HorasEstimadasMax INT NULL;
GO

IF COL_LENGTH(N'dbo.Tickets', N'FechaEstimadaResolucion') IS NULL
    ALTER TABLE dbo.Tickets ADD FechaEstimadaResolucion DATETIME NULL;
GO

PRINT 'Tickets estimado rango 2h-24h listo en AlahiaPos_Dev.';
GO
