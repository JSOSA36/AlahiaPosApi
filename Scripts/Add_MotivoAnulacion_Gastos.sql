-- MotivoAnulacion en Gastos (idempotente)
IF COL_LENGTH('dbo.Gastos', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.Gastos
        ADD MotivoAnulacion NVARCHAR(500) NULL;
END
GO
