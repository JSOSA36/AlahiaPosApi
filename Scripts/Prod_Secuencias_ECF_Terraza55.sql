-- Secuencias e-CF oficiales DGII para Terraza prolongación 27 (MATBERT SRL).
-- Solicitud 6009906280 (04/08/2026, APROBADA). Autorizado por el usuario en producción.
-- Quita B01 tradicional y rangos de PRUEBAS; deja E31/E32/E33/E34 de producción.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @FechaAut DATETIME2 = '2026-08-04';
DECLARE @VencE31 DATETIME = '2027-12-31';
DECLARE @VencE33 DATETIME = '2027-12-31';
DECLARE @VencSinFecha DATETIME = '2099-12-31';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND (NombreComercial LIKE N'%Terraza%' OR NombreComercial LIKE N'%MATBERT%')
)
BEGIN
    RAISERROR(N'No se encontró Terraza 27 / MATBERT (empresa 55) en Prod.', 16, 1);
    RETURN;
END;

DECLARE @MaxE31 INT = 0;
SELECT @MaxE31 = ISNULL(MAX(Num), 0)
FROM (
    SELECT TRY_CONVERT(int, RIGHT(h.NCF, 10)) AS Num
    FROM dbo.FacturaHeaders h
    WHERE h.IdEmpresa = @IdEmpresa AND h.NCF LIKE N'E31%'
    UNION ALL
    SELECT TRY_CONVERT(int, RIGHT(e.ENCF, 10))
    FROM dbo.ECFEncabezado e
    WHERE e.IdEmpresa = @IdEmpresa AND e.ENCF LIKE N'E31%'
) x
WHERE Num IS NOT NULL;

DECLARE @ProximaE31 INT = CASE WHEN @MaxE31 >= 1 THEN @MaxE31 + 1 ELSE 1 END;

BEGIN TRAN;

-- Quitar secuencia tradicional B01 (ya inactiva y sin uso en facturas).
DELETE FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa
  AND Serie = N'B01'
  AND ISNULL(TipoEcfDgii, 0) = 0
  AND Activo = 0;

-- E31 Factura de Crédito Fiscal Electrónico: 1–250, auth 6005417624
IF EXISTS (SELECT 1 FROM dbo.SecuenciasECF WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 31)
    UPDATE dbo.SecuenciasECF
    SET TipoNCF = N'E31',
        Serie = N'E31',
        Descripcion = N'Factura de Crédito Fiscal Electrónica',
        SecuenciaInicial = 1,
        SecuenciaActual = @ProximaE31,
        SecuenciaFinal = 250,
        fechaVencimiento = @VencE31,
        stockMinimo = 20,
        Activo = 1,
        Ambiente = N'PRODUCCION',
        FechaAutorizacion = @FechaAut,
        NumeroResolucion = N'6005417624'
    WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 31;
ELSE
    INSERT INTO dbo.SecuenciasECF (
        IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
        stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
        Ambiente, FechaAutorizacion, NumeroResolucion
    )
    VALUES (
        @IdEmpresa, N'E31', N'E31', @ProximaE31, 250, @VencE31,
        20, 1, GETDATE(), N'Factura de Crédito Fiscal Electrónica', 31, 1,
        N'PRODUCCION', @FechaAut, N'6005417624'
    );

-- E32 Factura de Consumo Electrónica: 1–5000, auth 6005417625 (sin vencimiento DGII)
IF EXISTS (SELECT 1 FROM dbo.SecuenciasECF WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 32)
    UPDATE dbo.SecuenciasECF
    SET TipoNCF = N'E32',
        Serie = N'E32',
        Descripcion = N'Factura de Consumo Electrónica',
        SecuenciaInicial = 1,
        SecuenciaActual = 1,
        SecuenciaFinal = 5000,
        fechaVencimiento = @VencSinFecha,
        stockMinimo = 50,
        Activo = 1,
        Ambiente = N'PRODUCCION',
        FechaAutorizacion = @FechaAut,
        NumeroResolucion = N'6005417625'
    WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 32;
ELSE
    INSERT INTO dbo.SecuenciasECF (
        IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
        stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
        Ambiente, FechaAutorizacion, NumeroResolucion
    )
    VALUES (
        @IdEmpresa, N'E32', N'E32', 1, 5000, @VencSinFecha,
        50, 1, GETDATE(), N'Factura de Consumo Electrónica', 32, 1,
        N'PRODUCCION', @FechaAut, N'6005417625'
    );

-- E33 Nota de Débito Electrónica: 1–40, auth 6005417626
IF EXISTS (SELECT 1 FROM dbo.SecuenciasECF WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 33)
    UPDATE dbo.SecuenciasECF
    SET TipoNCF = N'E33',
        Serie = N'E33',
        Descripcion = N'Nota de Débito Electrónica',
        SecuenciaInicial = 1,
        SecuenciaActual = 1,
        SecuenciaFinal = 40,
        fechaVencimiento = @VencE33,
        stockMinimo = 5,
        Activo = 1,
        Ambiente = N'PRODUCCION',
        FechaAutorizacion = @FechaAut,
        NumeroResolucion = N'6005417626'
    WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 33;
ELSE
    INSERT INTO dbo.SecuenciasECF (
        IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
        stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
        Ambiente, FechaAutorizacion, NumeroResolucion
    )
    VALUES (
        @IdEmpresa, N'E33', N'E33', 1, 40, @VencE33,
        5, 1, GETDATE(), N'Nota de Débito Electrónica', 33, 1,
        N'PRODUCCION', @FechaAut, N'6005417626'
    );

-- E34 Nota de Crédito Electrónica: 1–50, auth 6005417627 (sin vencimiento DGII)
IF EXISTS (SELECT 1 FROM dbo.SecuenciasECF WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 34)
    UPDATE dbo.SecuenciasECF
    SET TipoNCF = N'E34',
        Serie = N'E34',
        Descripcion = N'Nota de Crédito Electrónica',
        SecuenciaInicial = 1,
        SecuenciaActual = 1,
        SecuenciaFinal = 50,
        fechaVencimiento = @VencSinFecha,
        stockMinimo = 5,
        Activo = 1,
        Ambiente = N'PRODUCCION',
        FechaAutorizacion = @FechaAut,
        NumeroResolucion = N'6005417627'
    WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 34;
ELSE
    INSERT INTO dbo.SecuenciasECF (
        IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
        stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
        Ambiente, FechaAutorizacion, NumeroResolucion
    )
    VALUES (
        @IdEmpresa, N'E34', N'E34', 1, 50, @VencSinFecha,
        5, 1, GETDATE(), N'Nota de Crédito Electrónica', 34, 1,
        N'PRODUCCION', @FechaAut, N'6005417627'
    );

-- Ambiente DGII de la empresa: producción (ecf), no testecf.
UPDATE dbo.Empresas
SET AmbienteFE = N'ecf'
WHERE IdEmpresa = @IdEmpresa;

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, AmbienteFE
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SELECT
    IdSecuencia, TipoNCF, TipoEcfDgii, Serie,
    SecuenciaInicial, SecuenciaActual, SecuenciaFinal,
    fechaVencimiento, Activo, Ambiente, NumeroResolucion, Descripcion
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa
ORDER BY TipoEcfDgii, IdSecuencia;
GO
