-- Laboratorio CerteCF: guardar el cuerpo de respuesta DGII por caso.
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.CertecfCaso', 'RespuestaDgii') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD RespuestaDgii NVARCHAR(MAX) NULL;
GO
