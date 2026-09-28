-- Cargo por reconexión tras suspensión (solo AlahiaPos_Dev).
-- Se marca ReconexionPendiente al suspender; se suma al ciclo; se limpia al aprobar pago.

IF COL_LENGTH('dbo.Empresas', 'CargoReconexionDop') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD CargoReconexionDop DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Empresas_CargoReconexionDop DEFAULT (1000);
END
GO

IF COL_LENGTH('dbo.Empresas', 'ReconexionPendiente') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD ReconexionPendiente BIT NOT NULL
        CONSTRAINT DF_Empresas_ReconexionPendiente DEFAULT (0);
END
GO

-- Asegurar default 1000 en clientes existentes sin valor raro
UPDATE dbo.Empresas
SET CargoReconexionDop = 1000
WHERE CargoReconexionDop IS NULL OR CargoReconexionDop < 0;
GO
