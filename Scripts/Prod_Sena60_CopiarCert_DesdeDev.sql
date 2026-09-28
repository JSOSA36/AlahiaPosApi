-- Copia el .p12 + clave de Clínica Dental Dra Sena (60) de Dev hacia Prod.
-- Solo CertificadoDigital. No toca RNC, módulos, perfiles ni ProveedorFE.
-- Necesario porque Alahia.eCF.Api (ecf.alahiapos.com) lee AlahiaPos_Prod.
USE AlahiaPos_Prod;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: este script es solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 60 AND NombreComercial LIKE N'%Sena%'
)
BEGIN
    RAISERROR(N'Prod empresa 60 no es Dra Sena. Abortado.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM AlahiaPos_Dev.dbo.Empresas
    WHERE IdEmpresa = 60 AND NombreComercial LIKE N'%Sena%'
)
BEGIN
    RAISERROR(N'Dev empresa 60 no es Dra Sena. Abortado.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM AlahiaPos_Dev.dbo.CertificadoDigital
    WHERE IdEmpresa = 60 AND Activo = 1
      AND ArchivoBytes IS NOT NULL AND DATALENGTH(ArchivoBytes) > 0
      AND NULLIF(PasswordEncriptado, N'') IS NOT NULL
)
BEGIN
    RAISERROR(N'Dev no tiene .p12 activo de Sena (60). Abortado.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE dbo.CertificadoDigital
SET Activo = 0
WHERE IdEmpresa = 60 AND Activo = 1;

INSERT INTO dbo.CertificadoDigital (
    IdEmpresa, NombreArchivo, RutaArchivo, ArchivoBytes, PasswordEncriptado,
    FechaExpiracion, Activo, Ambiente, FechaCreacion
)
SELECT TOP 1
    60,
    d.NombreArchivo,
    NULL,
    d.ArchivoBytes,
    d.PasswordEncriptado,
    d.FechaExpiracion,
    1,
    d.Ambiente,
    GETDATE()
FROM AlahiaPos_Dev.dbo.CertificadoDigital d
WHERE d.IdEmpresa = 60 AND d.Activo = 1
  AND d.ArchivoBytes IS NOT NULL AND DATALENGTH(d.ArchivoBytes) > 0
ORDER BY d.FechaCreacion DESC;

COMMIT TRAN;

SELECT TOP 1 IdCertificado, NombreArchivo, Activo, Ambiente, FechaExpiracion,
       DATALENGTH(ArchivoBytes) AS Bytes, LEN(PasswordEncriptado) AS PasswordLen
FROM dbo.CertificadoDigital
WHERE IdEmpresa = 60 AND Activo = 1
ORDER BY FechaCreacion DESC;
GO
