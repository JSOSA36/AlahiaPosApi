-- Permiso por usuario: anular facturas.
-- Requiere autorización explícita para ejecutar en Prod.
USE AlahiaPos_Prod;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'PuedeAnularFactura') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios
        ADD PuedeAnularFactura BIT NOT NULL
            CONSTRAINT DF_Usuarios_PuedeAnularFactura DEFAULT (0);
END
GO

PRINT 'Usuarios.PuedeAnularFactura listo en AlahiaPos_Prod.';
GO
