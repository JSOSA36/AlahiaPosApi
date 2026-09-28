-- ============================================================
-- Parámetro ControlEfectivoPorDenominacion (Dev / AlahiaPos_Dev)
-- true  = Activado: el cierre pide billetes y monedas (default).
-- false = Desactivado: cierre simplificado (solo Clínica Dental Sena).
-- ============================================================
SET NOCOUNT ON;

DECLARE @Clave NVARCHAR(100) = N'ControlEfectivoPorDenominacion';
DECLARE @Descripcion NVARCHAR(300) =
    N'Control de efectivo por denominacion. Activado = pide billetes y monedas al cerrar. Desactivado = cierre simplificado (solo Clinica Dental Sena).';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @Clave,
    N'true',
    @Descripcion,
    GETDATE(),
    1
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Parametros p
    WHERE p.IdEmpresa = e.IdEmpresa
      AND LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

UPDATE p
SET p.Valor = N'true',
    p.Descripcion = @Descripcion,
    p.Activo = 1
FROM dbo.Parametros p
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');

UPDATE p
SET p.Valor = N'false'
FROM dbo.Parametros p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
  AND e.NombreComercial LIKE N'%Sena%'
  AND (
        e.NombreComercial LIKE N'%Dental%'
     OR e.NombreComercial LIKE N'%Clinica%'
     OR e.NombreComercial LIKE N'%Clínica%'
     OR e.NombreComercial LIKE N'%Odontolog%'
     OR e.NombreComercial LIKE N'%DRA.SENA%'
     OR e.NombreComercial LIKE N'%DRA SENA%'
  );

SELECT p.IdParametro, p.IdEmpresa, e.NombreComercial, p.Clave, p.Valor, p.Activo
FROM dbo.Parametros p
LEFT JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
ORDER BY p.IdEmpresa;
