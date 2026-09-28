-- Dev only. WhatsApp Business (Alahia Citas): flag por salón + idempotencia de recordatorio.
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
