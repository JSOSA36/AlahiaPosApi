-- Correo laboral: destino del recibo de nómina.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.EmpleadoLaboral', N'Correo') IS NULL
    ALTER TABLE dbo.EmpleadoLaboral ADD Correo NVARCHAR(150) NULL;
GO
