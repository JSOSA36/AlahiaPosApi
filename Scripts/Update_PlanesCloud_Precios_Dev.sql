-- Actualiza precios oficiales de planes SaaS (Dev)
-- Fuente: tabla comercial MacroBits (USD / mes + tope facturación RD$)
-- BÁSICO 55 · STANDARD 80 · GOLD 110 · PLATINUM 150 · ELITE 250

SET NOCOUNT ON;

UPDATE dbo.PlanesCloud SET PrecioUSD = 55,  LimiteFacturacion = 150000,  CantEquipos = 1 WHERE IdPlan = 3 AND Nombre LIKE 'Basico%';
UPDATE dbo.PlanesCloud SET PrecioUSD = 80,  LimiteFacturacion = 400000,  CantEquipos = 2 WHERE IdPlan = 2 AND Nombre LIKE 'Standard%';
UPDATE dbo.PlanesCloud SET PrecioUSD = 110, LimiteFacturacion = 1250000, CantEquipos = 3 WHERE IdPlan = 4 AND Nombre LIKE 'Gold%';
UPDATE dbo.PlanesCloud SET PrecioUSD = 150, LimiteFacturacion = 2000000, CantEquipos = 5 WHERE IdPlan = 5 AND Nombre LIKE 'Platinum%';
UPDATE dbo.PlanesCloud SET PrecioUSD = 250, LimiteFacturacion = 5000000, CantEquipos = 8 WHERE IdPlan = 6 AND Nombre LIKE 'Elite%';

SELECT IdPlan, Nombre, CantEquipos, LimiteFacturacion, PrecioUSD, Activo
FROM dbo.PlanesCloud
WHERE IdPlan BETWEEN 2 AND 6
ORDER BY PrecioUSD;
