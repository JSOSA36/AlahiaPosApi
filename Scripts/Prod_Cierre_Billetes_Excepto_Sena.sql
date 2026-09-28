-- AlahiaPos_Prod (autorizado: Laura Pastelería debe contar billetes).
-- Política: cierre por denominación (billetes) para todos.
-- Solo Clínica Dental Sena usa el cierre simplificado (ControlEfectivoPorDenominacion = false).
SET NOCOUNT ON;

DECLARE @Clave NVARCHAR(100) = N'ControlEfectivoPorDenominacion';
DECLARE @Desc NVARCHAR(300) =
    N'Control de efectivo por denominacion. Activado = pide billetes y monedas al cerrar. Desactivado = cierre simplificado (solo Clinica Dental Sena).';

-- Insertar el parámetro si falta
INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @Clave,
    N'true',
    @Desc,
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

-- Todos: conteo de billetes
UPDATE p
SET p.Valor = N'true',
    p.Descripcion = @Desc,
    p.Activo = 1
FROM dbo.Parametros p
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');

-- Solo Clínica Dental Sena: cierre simplificado
UPDATE p
SET p.Valor = N'false'
FROM dbo.Parametros p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
  AND (
        e.NombreComercial LIKE N'%Sena%'
        AND (
              e.NombreComercial LIKE N'%Dental%'
           OR e.NombreComercial LIKE N'%Clinica%'
           OR e.NombreComercial LIKE N'%Clínica%'
           OR e.NombreComercial LIKE N'%Odontolog%'
           OR e.NombreComercial LIKE N'%DRA.SENA%'
           OR e.NombreComercial LIKE N'%DRA SENA%'
        )
      );

SELECT
    e.IdEmpresa,
    e.NombreComercial,
    p.Valor AS ControlEfectivoPorDenominacion,
    CASE WHEN p.Valor IN (N'true', N'1') THEN N'Billetes (cierre anterior)' ELSE N'Simplificado (nuevo)' END AS ModoCierre
FROM dbo.Parametros p
INNER JOIN dbo.Empresas e ON e.IdEmpresa = p.IdEmpresa
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
  AND (
        e.NombreComercial LIKE N'%Laura%'
        OR e.NombreComercial LIKE N'%Pastel%'
        OR e.NombreComercial LIKE N'%Sena%'
      )
ORDER BY e.IdEmpresa;
