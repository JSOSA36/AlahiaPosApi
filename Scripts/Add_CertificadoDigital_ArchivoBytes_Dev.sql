-- Certificado digital: almacenar P12 en BD (modo DGII directo).
-- Solo AlahiaPos_Dev.

IF COL_LENGTH('dbo.CertificadoDigital', 'ArchivoBytes') IS NULL
    ALTER TABLE dbo.CertificadoDigital ADD ArchivoBytes VARBINARY(MAX) NULL;
GO

IF COL_LENGTH('dbo.CertificadoDigital', 'Ambiente') IS NULL
    ALTER TABLE dbo.CertificadoDigital ADD Ambiente NVARCHAR(20) NULL;
GO
