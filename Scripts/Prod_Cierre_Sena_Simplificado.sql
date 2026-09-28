-- Corrección: solo DRA.SENA / Clínica Dental Sena = cierre simplificado.
SET NOCOUNT ON;

DECLARE @Clave NVARCHAR(100) = N'ControlEfectivoPorDenominacion';

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
        OR p.Valor IN (N'false', N'0')
      )
ORDER BY e.IdEmpresa;
