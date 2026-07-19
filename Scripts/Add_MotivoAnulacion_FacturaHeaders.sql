IF COL_LENGTH('FacturaHeaders', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE FacturaHeaders
    ADD MotivoAnulacion NVARCHAR(500) NULL;
END
GO
