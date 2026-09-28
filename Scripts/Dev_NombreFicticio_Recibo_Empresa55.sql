-- AlahiaPos_Dev — Nombre y dirección ficticios en el recibo (empresa 55).
-- Solo para video/demo. No tocar Prod. No cambia RNC, teléfono ni clientes.

SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @NombreActual NVARCHAR(200);

SELECT @NombreActual = NombreComercial
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

IF @NombreActual IS NULL
BEGIN
    RAISERROR(N'No existe IdEmpresa 55 en Dev.', 16, 1);
    RETURN;
END;

IF @NombreActual NOT LIKE N'%MATBERT%'
   AND @NombreActual NOT LIKE N'%Terraza%'
   AND @NombreActual NOT LIKE N'Sabores del Caribe'
BEGIN
    RAISERROR(N'IdEmpresa 55 no es MATBERT / Terraza / demo. Abortado. Actual: %s', 16, 1, @NombreActual);
    RETURN;
END;

UPDATE dbo.Empresas
SET NombreComercial = N'Sabores del Caribe',
    Direccion = N'Av. Abraham Lincoln 205, Piantini, Santo Domingo, D.N.'
WHERE IdEmpresa = @IdEmpresa;

IF OBJECT_ID(N'dbo.DgiiConfiguracionEmpresa', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.DgiiConfiguracionEmpresa
    SET RazonSocial = N'Sabores del Caribe'
    WHERE IdEmpresa = @IdEmpresa;
END;

SELECT
    e.IdEmpresa,
    e.NombreComercial,
    e.Direccion,
    e.RNC,
    e.Telefono,
    d.RazonSocial AS DgiiRazonSocial
FROM dbo.Empresas e
LEFT JOIN dbo.DgiiConfiguracionEmpresa d ON d.IdEmpresa = e.IdEmpresa
WHERE e.IdEmpresa = @IdEmpresa;
