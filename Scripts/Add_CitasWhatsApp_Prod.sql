-- Pase prod: columnas WhatsApp Citas + consumo por mensaje.
-- Guard: solo corre si la base actual es AlahiaPos_Prod.
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR('Este script solo corre en AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.Empresas', 'NotificarCitasWhatsApp') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD NotificarCitasWhatsApp BIT NOT NULL
        CONSTRAINT DF_Empresas_NotificarCitasWhatsApp DEFAULT (1);
END
GO

IF COL_LENGTH('dbo.Citas', 'FechaRecordatorioWhatsApp') IS NULL
BEGIN
    ALTER TABLE dbo.Citas ADD FechaRecordatorioWhatsApp DATETIME NULL;
END
GO

IF OBJECT_ID('dbo.WhatsAppCitasConsumo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WhatsAppCitasConsumo
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppCitasConsumo PRIMARY KEY,
        IdEmpresa          INT NOT NULL,
        IdCita             INT NULL,
        Tipo               NVARCHAR(40) NOT NULL,
        Telefono           NVARCHAR(30) NULL,
        PrecioClienteDop   DECIMAL(12,2) NOT NULL CONSTRAINT DF_WACitasConsumo_Precio DEFAULT (5),
        CostoAlahiaDop     DECIMAL(12,2) NOT NULL CONSTRAINT DF_WACitasConsumo_Costo DEFAULT (3),
        TwilioSid          NVARCHAR(80) NULL,
        EsPrueba           BIT NOT NULL CONSTRAINT DF_WACitasConsumo_Prueba DEFAULT (0),
        Fecha              DATETIME NOT NULL CONSTRAINT DF_WACitasConsumo_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_WACitasConsumo_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );

    CREATE INDEX IX_WACitasConsumo_EmpresaFecha
        ON dbo.WhatsAppCitasConsumo (IdEmpresa, Fecha)
        INCLUDE (PrecioClienteDop, CostoAlahiaDop, EsPrueba);
END
GO
