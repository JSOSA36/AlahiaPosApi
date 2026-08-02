-- AlahiaPos_Dev only: extracto bancario como fuente de conciliación.
USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'Banco') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD Banco NVARCHAR(150) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'NumeroCuentaBanco') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD NumeroCuentaBanco NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'Moneda') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD Moneda NVARCHAR(3) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoInicial') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoInicial DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoFinal') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoFinal DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'TotalDebitos') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD TotalDebitos DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_TesExtImp_TotalDeb DEFAULT (0);
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'TotalCreditos') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD TotalCreditos DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_TesExtImp_TotalCred DEFAULT (0);
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'HashArchivo') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD HashArchivo NVARCHAR(64) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'IdTesoreriaConciliacion') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD IdTesoreriaConciliacion INT NULL;
GO

IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'AccionTomada') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD AccionTomada NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'CategoriaSugerida') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD CategoriaSugerida NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'EsAutoConciliado') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD EsAutoConciliado BIT NOT NULL
        CONSTRAINT DF_TesExtLin_Auto DEFAULT (0);
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'FechaResolucion') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD FechaResolucion DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'IdUsuarioResolucion') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD IdUsuarioResolucion INT NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TesExtLin_Match'
      AND parent_object_id = OBJECT_ID('dbo.TesoreriaExtractoLinea')
)
    ALTER TABLE dbo.TesoreriaExtractoLinea DROP CONSTRAINT CK_TesExtLin_Match;
GO

ALTER TABLE dbo.TesoreriaExtractoLinea WITH CHECK
ADD CONSTRAINT CK_TesExtLin_Match CHECK (
    EstadoMatch IN (
        'PENDIENTE', 'SUGERIDO', 'CONFIRMADO', 'AUTO_CONCILIADO',
        'DESCARTADO', 'IGNORADO', 'NUEVO_MOV', 'RESUELTO'
    )
);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesExtImp_EmpCuentaHash'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    CREATE UNIQUE INDEX UX_TesExtImp_EmpCuentaHash
        ON dbo.TesoreriaExtractoImport (IdEmpresa, IdCuentaFinanciera, HashArchivo)
        WHERE HashArchivo IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TesExtLin_ImportEstado'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoLinea')
)
BEGIN
    CREATE INDEX IX_TesExtLin_ImportEstado
        ON dbo.TesoreriaExtractoLinea (IdTesoreriaExtractoImport, EstadoMatch);
END
GO

PRINT 'Dev_Alter_Tesoreria_Extracto_Real.sql OK';
GO
