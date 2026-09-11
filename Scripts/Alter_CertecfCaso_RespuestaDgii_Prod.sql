-- Laboratorio CerteCF: guardar el cuerpo de respuesta DGII por caso.
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.CertecfCaso', 'RespuestaDgii') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD RespuestaDgii NVARCHAR(MAX) NULL;
GO

