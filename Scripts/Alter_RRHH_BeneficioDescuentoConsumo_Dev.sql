-- Beneficio de descuento sobre consumo (colaborador) y cargo a nómina.
-- AlahiaPos_Dev
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.RrhhBeneficio', 'DescontarConsumoNomina') IS NULL
    ALTER TABLE dbo.RrhhBeneficio ADD DescontarConsumoNomina BIT NOT NULL
        CONSTRAINT DF_RrhhBen_DescNom DEFAULT (0);
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.RrhhBeneficio
    WHERE IdEmpresa = 62 AND Codigo = N'DESCUENTO_COLABORADOR'
)
BEGIN
    INSERT INTO dbo.RrhhBeneficio (
        IdEmpresa, Codigo, Nombre, Descripcion, TipoCalculo, Monto, Periodicidad,
        AfectaNomina, EnEspecie, FormaDesembolso, DiaPagoMes, MetodoPago,
        DescontarConsumoNomina, Activo, FechaCreacion
    )
    VALUES (
        62, N'DESCUENTO_COLABORADOR', N'Descuento colaborador',
        N'30% en lo que consuma en el restaurante. El resto se puede descontar en la quincena.',
        N'DESCUENTO_CONSUMO', 30, N'MENSUAL',
        0, 1, N'NINGUNO', NULL, NULL,
        1, 1, SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.RrhhBeneficio
    SET TipoCalculo = N'DESCUENTO_CONSUMO',
        Monto = 30,
        EnEspecie = 1,
        AfectaNomina = 0,
        FormaDesembolso = N'NINGUNO',
        DescontarConsumoNomina = 1,
        Activo = 1
    WHERE IdEmpresa = 62 AND Codigo = N'DESCUENTO_COLABORADOR';
END
GO
