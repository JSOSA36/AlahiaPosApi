-- Dev only: Sabor Urbano (62) usa RNC + certificado de Clínica Dental Sena (60)
-- para probar RFCE E32 < 250k por EncApi.
USE AlahiaPos_Dev;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdSena INT = 60;
DECLARE @IdSabor INT = 62;
DECLARE @RncSena VARCHAR(20);
DECLARE @RncSaborAntes VARCHAR(20);

SELECT @RncSena = NULLIF(LTRIM(RTRIM(RNC)), N'')
FROM dbo.Empresas
WHERE IdEmpresa = @IdSena
  AND (NombreComercial LIKE N'%Sena%' OR NombreComercial LIKE N'%Dental%');

SELECT @RncSaborAntes = RNC
FROM dbo.Empresas
WHERE IdEmpresa = @IdSabor AND NombreComercial = N'Sabor Urbano';

IF @RncSena IS NULL OR @RncSaborAntes IS NULL
BEGIN
    RAISERROR(N'No se encontró Sena (60) o Sabor Urbano (62) en Dev.', 16, 1);
    RETURN;
END;

PRINT N'Sena RNC=' + @RncSena;
PRINT N'Sabor Urbano RNC antes=' + ISNULL(@RncSaborAntes, N'(null)');

UPDATE dbo.Empresas
SET RNC = @RncSena
WHERE IdEmpresa = @IdSabor AND NombreComercial = N'Sabor Urbano';

UPDATE dbo.CertificadoDigital
SET Activo = 0
WHERE IdEmpresa = @IdSabor AND Activo = 1;

INSERT INTO dbo.CertificadoDigital
(
    IdEmpresa, NombreArchivo, RutaArchivo, ArchivoBytes, PasswordEncriptado,
    FechaExpiracion, Activo, Ambiente, FechaCreacion
)
SELECT TOP 1
    @IdSabor, NombreArchivo, RutaArchivo, ArchivoBytes, PasswordEncriptado,
    FechaExpiracion, 1, Ambiente, GETDATE()
FROM dbo.CertificadoDigital
WHERE IdEmpresa = @IdSena AND Activo = 1
ORDER BY FechaCreacion DESC;

IF @@ROWCOUNT = 0
    RAISERROR(N'Sena (60) no tiene CertificadoDigital activo.', 16, 1);

SELECT e.IdEmpresa, e.NombreComercial, e.RNC,
       c.IdCertificado, c.NombreArchivo, c.Activo, c.FechaExpiracion,
       CASE WHEN c.ArchivoBytes IS NULL THEN 0 ELSE DATALENGTH(c.ArchivoBytes) END AS Bytes
FROM dbo.Empresas e
LEFT JOIN dbo.CertificadoDigital c ON c.IdEmpresa = e.IdEmpresa AND c.Activo = 1
WHERE e.IdEmpresa IN (@IdSena, @IdSabor);
GO
