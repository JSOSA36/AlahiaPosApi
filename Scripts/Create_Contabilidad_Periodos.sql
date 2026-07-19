-- Fase 2 Contabilidad - Períodos contables (cierre)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PeriodosContables')
BEGIN
    CREATE TABLE PeriodosContables (
        IdPeriodoContable INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        Anio INT NOT NULL,
        Mes INT NOT NULL,
        Estado NVARCHAR(20) NOT NULL DEFAULT 'Abierto',
        FechaCierre DATETIME NULL,
        IdUsuarioCierre INT NULL,
        Observacion NVARCHAR(500) NULL,
        FechaInseccion DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UX_PeriodosContables_Empresa_Anio_Mes UNIQUE (IdEmpresa, Anio, Mes)
    );
END
GO
