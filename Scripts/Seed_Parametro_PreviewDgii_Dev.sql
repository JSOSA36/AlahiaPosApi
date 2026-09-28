-- ============================================================
-- Parámetro PREVIEW_DGII (Dev / AlahiaPos_Dev)
-- Si Valor = 'true', el POS muestra la vista previa del
-- comprobante al facturar (cualquier tipo de NCF).
-- Las notas de crédito siempre muestran vista previa.
-- Default: true si FACTURACION_ELECTRONICA ya está activa.
-- ============================================================
SET NOCOUNT ON;

DECLARE @Clave NVARCHAR(100) = N'PREVIEW_DGII';
DECLARE @Descripcion NVARCHAR(200) =
    N'Si está activo, el POS muestra vista previa del comprobante (cualquier NCF) al facturar. NC siempre la muestra.';

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
            FROM dbo.Parametros fe
            WHERE fe.IdEmpresa = e.IdEmpresa
              AND fe.Clave = N'FACTURACION_ELECTRONICA'
              AND LOWER(LTRIM(RTRIM(ISNULL(fe.Valor, N'')))) IN (N'true', N'1')
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
      AND UPPER(REPLACE(REPLACE(p.Clave, N'_', N''), N'-', N'')) = N'PREVIEWDGII'
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

UPDATE p
SET p.Descripcion = @Descripcion,
    p.Activo = 1
FROM dbo.Parametros p
WHERE UPPER(REPLACE(REPLACE(p.Clave, N'_', N''), N'-', N'')) = N'PREVIEWDGII'
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');

SELECT p.IdParametro, p.IdEmpresa, e.NombreComercial, p.Clave, p.Valor, p.Activo
FROM dbo.Parametros p
LEFT JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE UPPER(REPLACE(REPLACE(p.Clave, N'_', N''), N'-', N'')) = N'PREVIEWDGII'
ORDER BY p.IdEmpresa;
