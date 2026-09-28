-- AlahiaPos_Dev — parámetros de reserva en la app pública de citas.
-- PedirVoucherCitas: si el cliente debe subir voucher al agendar.
-- MontoReservaCitas: abono que el salón exige (RD$).

IF COL_LENGTH('dbo.Empresas', 'PedirVoucherCitas') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD PedirVoucherCitas BIT NOT NULL
        CONSTRAINT DF_Empresas_PedirVoucherCitas DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Empresas', 'MontoReservaCitas') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD MontoReservaCitas DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Empresas_MontoReservaCitas DEFAULT (0);
END
GO
