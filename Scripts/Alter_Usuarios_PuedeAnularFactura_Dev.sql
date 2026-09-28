-- Permiso por usuario: anular facturas
USE AlahiaPos_Dev;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Dev.', 16, 1);
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

PRINT 'Usuarios.PuedeAnularFactura listo en AlahiaPos_Dev.';
GO
