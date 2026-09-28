-- AlahiaPos_Dev. E43/E47 (y E46 sin comprador): quitar RncComprador= vacío
-- del UrlQR del lote. No cambia fechas, monto ni código de seguridad.
SET NOCOUNT ON;
BEGIN TRAN;

UPDATE c
SET
    UrlQR = REPLACE(c.UrlQR, N'&RncComprador=&', N'&'),
    RespuestaDgii = REPLACE(c.RespuestaDgii, N'&RncComprador=&', N'&')
FROM dbo.CertecfCaso c
INNER JOIN dbo.CertecfSesion s ON s.IdSesion = c.IdSesion
WHERE s.IdEmpresa = 60
  AND (
        (c.UrlQR LIKE N'%&RncComprador=&%')
     OR (c.RespuestaDgii LIKE N'%&RncComprador=&%')
  );

SELECT c.IdCaso, c.Encf, c.Estado, c.UrlQR
FROM dbo.CertecfCaso c
INNER JOIN dbo.CertecfSesion s ON s.IdSesion = c.IdSesion
WHERE s.IdEmpresa = 60
  AND c.TipoPrueba = N'SIMULACION'
  AND (c.Encf LIKE N'E43%' OR c.Encf LIKE N'E47%' OR c.Encf LIKE N'E46%')
ORDER BY c.Encf;

COMMIT;
