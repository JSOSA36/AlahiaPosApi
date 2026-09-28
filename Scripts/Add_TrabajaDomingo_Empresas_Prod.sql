-- AlahiaPos_Prod — NO ejecutar sin autorización explícita.
-- Mismo campo que Dev: TrabajaDomingo BIT default 1; Sena = 0.

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script es solo para AlahiaPos_Prod. Abortado.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.Empresas', 'TrabajaDomingo') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD TrabajaDomingo BIT NOT NULL
        CONSTRAINT DF_Empresas_TrabajaDomingo DEFAULT (1);
END
GO

UPDATE dbo.Empresas
SET TrabajaDomingo = 1
WHERE TrabajaDomingo IS NULL;
GO

UPDATE dbo.Empresas
SET TrabajaDomingo = 0
WHERE EsEmpresaSistema = 0
  AND NombreComercial LIKE N'%Sena%'
  AND (NombreComercial LIKE N'%Dental%' OR IdEmpresa = 60);
GO
