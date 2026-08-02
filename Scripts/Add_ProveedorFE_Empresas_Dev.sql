-- Proveedor fiscal por empresa: DGII directo (Alahia.eCF.Api) vs proveedor externo (Receipt/ApiKey).
-- Solo AlahiaPos_Dev.

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE NVARCHAR(40) NULL;
GO

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE_Nombre') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE_Nombre NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE_BaseUrl') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE_BaseUrl NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE_ApiKey') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE_ApiKey NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE_Usuario') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE_Usuario NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Empresas', 'ProveedorFE_Password') IS NULL
    ALTER TABLE dbo.Empresas ADD ProveedorFE_Password NVARCHAR(500) NULL;
GO
