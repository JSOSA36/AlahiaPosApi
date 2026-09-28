-- Laboratorio CerteCF: persistir QR / código / firma / XML del envío.
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR('Este script solo corre contra AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.CertecfCaso', 'UrlQR') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD UrlQR NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.CertecfCaso', 'CodigoSeguridad') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD CodigoSeguridad NVARCHAR(32) NULL;
GO

IF COL_LENGTH('dbo.CertecfCaso', 'FechaFirma') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD FechaFirma DATETIME NULL;
GO

IF COL_LENGTH('dbo.CertecfCaso', 'XmlFirmado') IS NULL
    ALTER TABLE dbo.CertecfCaso ADD XmlFirmado NVARCHAR(MAX) NULL;
GO

-- Recupera de RespuestaDgii lo que ya se había escrito en texto.
UPDATE c
SET
    UrlQR = CASE
        WHEN NULLIF(LTRIM(RTRIM(c.UrlQR)), N'') IS NULL
             AND CHARINDEX(N'http', LOWER(c.RespuestaDgii)) > 0
        THEN (
            SELECT TOP (1) LTRIM(RTRIM(SUBSTRING(ln.Linea, CHARINDEX(N'http', LOWER(ln.Linea)), 1000)))
            FROM (
                SELECT LTRIM(RTRIM(ss.value)) AS Linea
                FROM STRING_SPLIT(REPLACE(c.RespuestaDgii, CHAR(13), N''), CHAR(10)) ss
            ) ln
            WHERE ln.Linea LIKE N'QR%' AND ln.Linea LIKE N'%http%'
        )
        ELSE c.UrlQR
    END,
    CodigoSeguridad = CASE
        WHEN NULLIF(LTRIM(RTRIM(c.CodigoSeguridad)), N'') IS NULL
        THEN (
            SELECT TOP (1) LTRIM(RTRIM(SUBSTRING(ln.Linea, CHARINDEX(N':', ln.Linea) + 1, 32)))
            FROM (
                SELECT LTRIM(RTRIM(ss.value)) AS Linea
                FROM STRING_SPLIT(REPLACE(c.RespuestaDgii, CHAR(13), N''), CHAR(10)) ss
            ) ln
            WHERE ln.Linea LIKE N'CodigoSeguridad%'
        )
        ELSE c.CodigoSeguridad
    END
FROM dbo.CertecfCaso c
WHERE c.RespuestaDgii IS NOT NULL;
GO
