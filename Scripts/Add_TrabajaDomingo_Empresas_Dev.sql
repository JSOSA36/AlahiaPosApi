-- AlahiaPos_Dev — si el 30 cae domingo y la empresa no abre, el modal de cobro pasa al día siguiente.
-- Default: trabaja domingo (mayoría de clientes actuales).

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

-- Clínica Dental Dra Sena: no trabaja domingo
UPDATE dbo.Empresas
SET TrabajaDomingo = 0
WHERE EsEmpresaSistema = 0
  AND NombreComercial LIKE N'%Sena%'
  AND (NombreComercial LIKE N'%Dental%' OR IdEmpresa = 60);
GO
