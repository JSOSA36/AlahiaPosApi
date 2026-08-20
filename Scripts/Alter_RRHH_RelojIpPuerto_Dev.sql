-- IP/puerto del reloj en LAN (SDK). AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'DireccionIp') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD DireccionIp NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'Puerto') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD Puerto INT NULL;
IF COL_LENGTH(N'dbo.RrhhPonchadorDispositivo', N'ClaveComunicacion') IS NULL
    ALTER TABLE dbo.RrhhPonchadorDispositivo ADD ClaveComunicacion NVARCHAR(40) NULL;
GO
