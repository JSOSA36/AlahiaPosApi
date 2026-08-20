-- ============================================================
-- Parámetro NotificacionVehiculo (Dev / AlahiaPos_Dev)
-- Si Valor = 'true', al pulsar "Marcar lista" en Centro de
-- Producción se anuncia por voz: "{cliente} Su vehículo está listo".
-- Default: false (no pisa un valor ya existente).
-- ============================================================
SET NOCOUNT ON;

DECLARE @Clave NVARCHAR(100) = N'NotificacionVehiculo';
DECLARE @Descripcion NVARCHAR(200) = N'Al marcar lista en Centro de Producción, anuncia por voz que el vehículo está listo';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @Clave,
    N'false',
    @Descripcion,
    GETDATE(),
    1
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Parametros p
    WHERE p.IdEmpresa = e.IdEmpresa
      AND p.Clave = @Clave
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

-- Si la empresa ya usa ticket de lavador, activar el anuncio (lavaderos).
UPDATE p
SET p.Valor = N'true',
    p.Activo = 1,
    p.Descripcion = @Descripcion
FROM dbo.Parametros p
WHERE p.Clave = @Clave
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
  AND EXISTS (
      SELECT 1
      FROM dbo.Parametros lav
      WHERE lav.IdEmpresa = p.IdEmpresa
        AND lav.Clave = N'PrintTicketLavador'
        AND LOWER(LTRIM(RTRIM(ISNULL(lav.Valor, N'')))) IN (N'true', N'1')
  );

SELECT p.IdParametro, p.IdEmpresa, e.NombreComercial, p.Clave, p.Valor, p.Activo
FROM dbo.Parametros p
LEFT JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE p.Clave = @Clave
ORDER BY p.IdEmpresa;
