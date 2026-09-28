-- AlahiaPos_Dev — Nombres oficiales DGII en secuencias e-CF (acentos correctos).
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

UPDATE dbo.SecuenciasECF SET Descripcion = N'Factura de Crédito Fiscal Electrónica' WHERE TipoEcfDgii = 31;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Factura de Consumo Electrónica' WHERE TipoEcfDgii = 32;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Nota de Débito Electrónica' WHERE TipoEcfDgii = 33;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Nota de Crédito Electrónica' WHERE TipoEcfDgii = 34;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Compras Electrónica' WHERE TipoEcfDgii = 41;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Gastos Menores Electrónica' WHERE TipoEcfDgii = 43;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Regímenes Especiales Electrónica' WHERE TipoEcfDgii = 44;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Gubernamental Electrónica' WHERE TipoEcfDgii = 45;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Exportaciones Electrónica' WHERE TipoEcfDgii = 46;
UPDATE dbo.SecuenciasECF SET Descripcion = N'Pagos al Exterior Electrónica' WHERE TipoEcfDgii = 47;

SELECT IdEmpresa, TipoNCF, TipoEcfDgii, Descripcion
FROM dbo.SecuenciasECF
WHERE IdEmpresa = 55 AND TipoEcfDgii > 0
ORDER BY TipoEcfDgii;
