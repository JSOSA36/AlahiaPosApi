-- ============================================================
-- Parámetro NotificacionVehiculo — Deploy AlahiaPos_Prod
-- Idempotente: inserta solo si no existe (no pisa valores).
--
-- Si Valor = 'true', al pulsar "Marcar lista" en Centro de
-- Producción se anuncia por voz: "{cliente} Su vehículo está listo".
-- Default: false. true solo si PrintTicketLavador ya está activo.
-- ============================================================
USE AlahiaPos_Prod;
GO

SET NOCOUNT ON;
GO

-- >>> CAMBIAR A 1 solo con autorización explícita para ejecutar en Prod <<<
DECLARE @CONFIRMO_PROD BIT = 0;

IF DB_NAME() <> N'AlahiaPos_Prod' OR ISNULL(@CONFIRMO_PROD, 0) <> 1
BEGIN
    RAISERROR(
        N'Abortado: este script solo corre en AlahiaPos_Prod con @CONFIRMO_PROD = 1.',
        16, 1);
    SET NOEXEC ON;
END
GO

DECLARE @Clave NVARCHAR(100) = N'NotificacionVehiculo';
DECLARE @Descripcion NVARCHAR(200) = N'Al marcar lista en Centro de Producción, anuncia por voz que el vehículo está listo';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @Clave,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Parametros lav
            WHERE lav.IdEmpresa = e.IdEmpresa
              AND lav.Clave = N'PrintTicketLavador'
              AND LOWER(LTRIM(RTRIM(ISNULL(lav.Valor, N'')))) IN (N'true', N'1')
        ) THEN N'true'
        ELSE N'false'
    END,
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

PRINT N'Filas insertadas: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT p.IdParametro, p.IdEmpresa, e.NombreComercial, p.Clave, p.Valor, p.Activo
FROM dbo.Parametros p
LEFT JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE p.Clave = @Clave
  AND p.Valor = N'true'
ORDER BY p.IdEmpresa;
GO

SET NOEXEC OFF;
GO
