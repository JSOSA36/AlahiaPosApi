-- Valores: STANDARD | GOLD | PREMIUM
-- PREMIUM_PLUS (legado) se convierte al nuevo Premium.

IF COL_LENGTH('dbo.Empresas', 'NivelSoporte') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD NivelSoporte NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Empresas_NivelSoporte DEFAULT ('STANDARD');
END
GO

UPDATE dbo.Empresas
SET NivelSoporte = 'PREMIUM'
WHERE UPPER(REPLACE(LTRIM(RTRIM(NivelSoporte)), ' ', '_')) IN ('PREMIUM_PLUS', 'PREMIUMPLUS', 'PLATINUM', 'PLATINO');
GO

UPDATE dbo.Empresas
SET NivelSoporte = 'STANDARD'
WHERE NivelSoporte IS NULL
   OR UPPER(REPLACE(LTRIM(RTRIM(NivelSoporte)), ' ', '_'))
      NOT IN ('STANDARD', 'GOLD', 'PREMIUM');
GO

UPDATE dbo.Empresas
SET NivelSoporte = 'PREMIUM'
WHERE EsEmpresaSistema = 0
  AND IdEmpresa = 60
  AND NombreComercial LIKE '%SENA%';
GO
