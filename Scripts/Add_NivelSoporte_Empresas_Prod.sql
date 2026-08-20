-- Producción: STANDARD | GOLD | PREMIUM
-- Clínica Dental Sena = PREMIUM (antes PREMIUM_PLUS).

IF COL_LENGTH('dbo.Empresas', 'NivelSoporte') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD NivelSoporte NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Empresas_NivelSoporte DEFAULT ('STANDARD');
END
GO

UPDATE dbo.Empresas
SET NivelSoporte = 'STANDARD'
WHERE EsEmpresaSistema = 0
  AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(NivelSoporte, ''))), ' ', '_'))
      NOT IN ('GOLD', 'PREMIUM', 'PREMIUM_PLUS', 'PLATINUM');
GO

UPDATE dbo.Empresas
SET NivelSoporte = 'PREMIUM'
WHERE EsEmpresaSistema = 0
  AND (
    (IdEmpresa = 60 AND NombreComercial LIKE '%SENA%')
    OR UPPER(REPLACE(LTRIM(RTRIM(NivelSoporte)), ' ', '_')) IN ('PREMIUM_PLUS', 'PLATINUM')
  );
GO
