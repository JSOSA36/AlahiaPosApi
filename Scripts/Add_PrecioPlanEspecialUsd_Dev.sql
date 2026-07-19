-- Precio especial de plan por empresa (solo ese cliente).
-- NULL = usa el precio del catálogo PlanesCloud.
-- Ejemplo: Standard vale 80, Flawless paga 70 → PrecioPlanEspecialUsd = 70

IF COL_LENGTH('dbo.Empresas', 'PrecioPlanEspecialUsd') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD PrecioPlanEspecialUsd DECIMAL(18, 2) NULL;
END
GO
