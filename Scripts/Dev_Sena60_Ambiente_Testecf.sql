-- Clínica Dental Dra Sena (60): ambiente DGII de pruebas (testecf).
USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa
    FROM dbo.Empresas
    WHERE IdEmpresa = 60 AND NombreComercial LIKE N'%Sena%'
);

IF @IdEmpresa IS NULL
BEGIN
    RAISERROR(N'No se encontró Dra Sena (60) en Dev.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE dbo.Empresas
SET AmbienteFE = N'testecf',
    EsEmisorElectronico = 1,
    ProveedorFE = N'DGII_DIRECTO',
    ProveedorFE_Nombre = N'Alahia.eCF.Api',
    EstadoFE = N'Activo'
WHERE IdEmpresa = @IdEmpresa;

UPDATE dbo.SecuenciasECF
SET Ambiente = N'PRUEBAS'
WHERE IdEmpresa = @IdEmpresa AND Activo = 1;

UPDATE dbo.CertificadoDigital
SET Ambiente = N'PRUEBAS'
WHERE IdEmpresa = @IdEmpresa AND Activo = 1;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DgiiConfiguracionEmpresa')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.DgiiConfiguracionEmpresa WHERE IdEmpresa = @IdEmpresa)
        UPDATE dbo.DgiiConfiguracionEmpresa
        SET FiscalActivo = 1,
            FacturacionElectronicaActiva = 1
        WHERE IdEmpresa = @IdEmpresa;
END

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, ProveedorFE, AmbienteFE, EsEmisorElectronico
FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa;

SELECT IdCertificado, NombreArchivo, Activo, Ambiente, FechaExpiracion,
       DATALENGTH(ArchivoBytes) AS Bytes, LEN(PasswordEncriptado) AS PasswordLen
FROM dbo.CertificadoDigital
WHERE IdEmpresa = @IdEmpresa AND Activo = 1;

SELECT COUNT(*) AS SecuenciasPruebas
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa AND Activo = 1 AND Ambiente = N'PRUEBAS';
GO
