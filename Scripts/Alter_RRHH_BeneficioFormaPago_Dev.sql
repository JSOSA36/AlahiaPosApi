-- Forma de pago del beneficio: con nómina o en fecha propia.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.RrhhBeneficio', 'FormaDesembolso') IS NULL
    ALTER TABLE dbo.RrhhBeneficio ADD FormaDesembolso NVARCHAR(20) NOT NULL
        CONSTRAINT DF_RrhhBen_Forma DEFAULT (N'NOMINA');
GO

IF COL_LENGTH('dbo.RrhhBeneficio', 'DiaPagoMes') IS NULL
    ALTER TABLE dbo.RrhhBeneficio ADD DiaPagoMes TINYINT NULL;
GO

IF COL_LENGTH('dbo.RrhhBeneficio', 'MetodoPago') IS NULL
    ALTER TABLE dbo.RrhhBeneficio ADD MetodoPago NVARCHAR(30) NULL;
GO

UPDATE dbo.RrhhBeneficio
SET FormaDesembolso = CASE WHEN AfectaNomina = 1 THEN N'NOMINA' ELSE N'NINGUNO' END
WHERE ISNULL(FormaDesembolso, N'') IN (N'', N'NOMINA') AND AfectaNomina = 0;

UPDATE dbo.RrhhBeneficio
SET FormaDesembolso = N'NOMINA', AfectaNomina = 1
WHERE ISNULL(FormaDesembolso, N'') = N'' AND AfectaNomina = 1;
GO

-- Demo Sabor Urbano: gasolina el día 5, aparte de la quincena
UPDATE b
SET
    b.FormaDesembolso = N'PAGO_APARTE',
    b.AfectaNomina = 0,
    b.DiaPagoMes = 5,
    b.MetodoPago = N'TRANSFERENCIA',
    b.Periodicidad = N'MENSUAL'
FROM dbo.RrhhBeneficio b
WHERE b.IdEmpresa = 62 AND b.Codigo = N'GASOLINA';
GO
