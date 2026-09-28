-- Sabor Urbano (73): DGII_DIRECTO en testecf (sin PG Invoice).
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END

UPDATE dbo.Empresas
SET ProveedorFE = N'DGII_DIRECTO',
    AmbienteFE = N'testecf',
    ProveedorFE_Nombre = N'Alahia.eCF.Api / DGII Directo',
    EstadoFE = N'Activo',
    EsEmisorElectronico = 1
WHERE IdEmpresa = 73
  AND NombreComercial = N'Sabor Urbano';

UPDATE dbo.SecuenciasECF
SET Ambiente = N'testecf'
WHERE IdEmpresa = 73
  AND Activo = 1;

SELECT IdEmpresa, AmbienteFE, ProveedorFE, ProveedorFE_Nombre, EstadoFE
FROM dbo.Empresas WHERE IdEmpresa = 73;

SELECT TipoNCF, Ambiente, Activo
FROM dbo.SecuenciasECF WHERE IdEmpresa = 73
ORDER BY TipoEcfDgii;
GO
