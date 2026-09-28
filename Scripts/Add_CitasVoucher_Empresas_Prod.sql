-- AlahiaPos_Prod — parámetros de reserva en la app pública de citas.
-- PedirVoucherCitas / MontoReservaCitas (ya existen en Dev).
USE AlahiaPos_Prod;
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.Empresas', 'PedirVoucherCitas') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD PedirVoucherCitas BIT NOT NULL
        CONSTRAINT DF_Empresas_PedirVoucherCitas DEFAULT (0);
    PRINT 'OK: PedirVoucherCitas agregado.';
END
ELSE
    PRINT 'OK: PedirVoucherCitas ya existía.';
GO

IF COL_LENGTH('dbo.Empresas', 'MontoReservaCitas') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD MontoReservaCitas DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Empresas_MontoReservaCitas DEFAULT (0);
    PRINT 'OK: MontoReservaCitas agregado.';
END
ELSE
    PRINT 'OK: MontoReservaCitas ya existía.';
GO
