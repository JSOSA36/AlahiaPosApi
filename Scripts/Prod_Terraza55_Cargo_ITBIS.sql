-- Terraza 27 de febrero (empresa 55, AlahiaPos_Prod):
-- el cargo de cobro deja de decir "tarjeta" y pasa a decir "ITBIS".
-- Pedido explícito del usuario (empresa + qué cambiar).
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

PRINT N'--- CargoPagoRegla antes ---';
SELECT IdCargoPagoRegla, Nombre, Tipo, Valor, GrupoMetodo, Activo
FROM dbo.CargoPagoRegla
WHERE IdEmpresa = @IdEmpresa;

BEGIN TRAN;

UPDATE dbo.CargoPagoRegla
SET Nombre = N'ITBIS'
WHERE IdEmpresa = @IdEmpresa
  AND Nombre LIKE N'%tarjeta%';

PRINT N'Reglas actualizadas: ' + CAST(@@ROWCOUNT AS varchar(12));

IF COL_LENGTH(N'dbo.FacturaCargo', N'Nombre') IS NOT NULL
BEGIN
    UPDATE dbo.FacturaCargo
    SET Nombre = N'ITBIS'
    WHERE IdEmpresa = @IdEmpresa
      AND Nombre LIKE N'%tarjeta%';
    PRINT N'FacturaCargo actualizados: ' + CAST(@@ROWCOUNT AS varchar(12));
END;

COMMIT TRAN;

PRINT N'--- CargoPagoRegla después ---';
SELECT IdCargoPagoRegla, Nombre, Tipo, Valor, GrupoMetodo, Activo
FROM dbo.CargoPagoRegla
WHERE IdEmpresa = @IdEmpresa;
GO
