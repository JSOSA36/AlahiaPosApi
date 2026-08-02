-- Cobro dinámico por empresa (sin depender de PlanesCloud para montos).
-- Solo AlahiaPos_Dev.

IF COL_LENGTH('dbo.Empresas', 'MontoServicio') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD MontoServicio DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Empresas_MontoServicio DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Empresas', 'LimiteFacturacion') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD LimiteFacturacion INT NOT NULL CONSTRAINT DF_Empresas_LimiteFacturacion DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Empresas', 'CargoAdicional') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD CargoAdicional DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Empresas_CargoAdicional DEFAULT (0);
END
GO

-- Migración inicial desde catálogo / precio especial (si aún no se cargó MontoServicio)
UPDATE e
SET
    e.MontoServicio = COALESCE(
        NULLIF(e.MontoServicio, 0),
        e.PrecioPlanEspecialUsd,
        p.PrecioUSD,
        0
    ),
    e.LimiteFacturacion = CASE
        WHEN e.LimiteFacturacion > 0 THEN e.LimiteFacturacion
        WHEN p.LimiteFacturacion IS NULL THEN 0
        WHEN p.LimiteFacturacion > 2147483647 THEN 2147483647
        ELSE CAST(p.LimiteFacturacion AS INT)
    END
FROM dbo.Empresas e
LEFT JOIN dbo.PlanesCloud p ON p.IdPlan = e.IdPlan
WHERE e.EsEmpresaSistema = 0
  AND (e.MontoServicio = 0 OR e.LimiteFacturacion = 0);
GO
