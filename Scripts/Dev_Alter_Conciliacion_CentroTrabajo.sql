-- AlahiaPos_Dev only: Centro de Trabajo de Conciliación Bancaria
-- DO NOT run against Prod without explicit authorization
USE AlahiaPos_Dev;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- Extracto ↔ Conciliación ---------- */
IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'IdTesoreriaConciliacion') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD IdTesoreriaConciliacion INT NULL;
GO

IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'SaldoBancoInicialDeclarado') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoImport ADD SaldoBancoInicialDeclarado DECIMAL(18,2) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_TesExtImp_Conciliacion'
)
BEGIN
    ALTER TABLE dbo.TesoreriaExtractoImport
    ADD CONSTRAINT FK_TesExtImp_Conciliacion
        FOREIGN KEY (IdTesoreriaConciliacion)
        REFERENCES dbo.TesoreriaConciliacion (IdTesoreriaConciliacion);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TesExtImp_Conciliacion'
      AND object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
BEGIN
    CREATE INDEX IX_TesExtImp_Conciliacion
        ON dbo.TesoreriaExtractoImport (IdTesoreriaConciliacion)
        WHERE IdTesoreriaConciliacion IS NOT NULL;
END
GO

/* ---------- Estados extracto (incluye CERRADO) ---------- */
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TesExtImp_Estado'
      AND parent_object_id = OBJECT_ID('dbo.TesoreriaExtractoImport')
)
    ALTER TABLE dbo.TesoreriaExtractoImport DROP CONSTRAINT CK_TesExtImp_Estado;
GO

ALTER TABLE dbo.TesoreriaExtractoImport WITH NOCHECK
ADD CONSTRAINT CK_TesExtImp_Estado CHECK (
    Estado IN ('CARGADO', 'PROCESADO', 'CERRADO', 'ANULADO')
);
GO

/* ---------- Estados línea extracto ---------- */
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TesExtLin_Match'
      AND parent_object_id = OBJECT_ID('dbo.TesoreriaExtractoLinea')
)
    ALTER TABLE dbo.TesoreriaExtractoLinea DROP CONSTRAINT CK_TesExtLin_Match;
GO

ALTER TABLE dbo.TesoreriaExtractoLinea WITH NOCHECK
ADD CONSTRAINT CK_TesExtLin_Match CHECK (
    EstadoMatch IN (
        'PENDIENTE', 'SUGERIDO', 'CONFIRMADO', 'AUTO_CONCILIADO',
        'DESCARTADO', 'IGNORADO', 'NUEVO_MOV', 'RESUELTO',
        'DIFERENCIA', 'AMBIGUO', 'DUPLICADO'
    )
);
GO

IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ReglaMatch') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD ReglaMatch NVARCHAR(80) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ExplicacionMatch') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD ExplicacionMatch NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ClasificacionLinea') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD ClasificacionLinea NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ModuloOrigenSugerido') IS NULL
    ALTER TABLE dbo.TesoreriaExtractoLinea ADD ModuloOrigenSugerido NVARCHAR(40) NULL;
GO

/* ---------- Conciliación: saldos banco + rowversion ---------- */
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoBancoInicial') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD SaldoBancoInicial DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'IdExtractoPrincipal') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD IdExtractoPrincipal INT NULL;
GO
IF COL_LENGTH('dbo.TesoreriaConciliacion', 'RowVersion') IS NULL
    ALTER TABLE dbo.TesoreriaConciliacion ADD RowVersion ROWVERSION NOT NULL;
GO

IF COL_LENGTH('dbo.CuentaFinanciera', 'RowVersion') IS NULL
    ALTER TABLE dbo.CuentaFinanciera ADD RowVersion ROWVERSION NOT NULL;
GO

/* Una sola conciliación abierta por cuenta: cerrar solapes antiguos en Dev */
;WITH Ranked AS (
    SELECT IdTesoreriaConciliacion,
           ROW_NUMBER() OVER (
               PARTITION BY IdEmpresa, IdCuentaFinanciera
               ORDER BY FechaCreacion DESC, IdTesoreriaConciliacion DESC
           ) AS Rn
    FROM dbo.TesoreriaConciliacion
    WHERE Estado IN ('BORRADOR', 'EN_PROCESO')
)
UPDATE c
SET c.Estado = 'ANULADA',
    c.Observacion = LEFT(CONCAT(ISNULL(c.Observacion, ''), ' | Anulada por migración Centro Trabajo (solape).'), 500)
FROM dbo.TesoreriaConciliacion c
INNER JOIN Ranked r ON r.IdTesoreriaConciliacion = c.IdTesoreriaConciliacion
WHERE r.Rn > 1;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesConc_CuentaAbierta'
      AND object_id = OBJECT_ID('dbo.TesoreriaConciliacion')
)
BEGIN
    CREATE UNIQUE INDEX UX_TesConc_CuentaAbierta
        ON dbo.TesoreriaConciliacion (IdEmpresa, IdCuentaFinanciera)
        WHERE Estado IN ('BORRADOR', 'EN_PROCESO');
END
GO

/* ---------- Auditoría de acciones ---------- */
IF OBJECT_ID('dbo.TesoreriaConciliacionAuditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesoreriaConciliacionAuditoria (
        IdTesoreriaConciliacionAuditoria INT IDENTITY(1,1) NOT NULL,
        IdTesoreriaConciliacion INT NOT NULL,
        IdEmpresa INT NOT NULL,
        IdUsuario INT NULL,
        Accion NVARCHAR(40) NOT NULL,
        Detalle NVARCHAR(1000) NULL,
        IdTesoreriaExtractoLinea INT NULL,
        IdMovimientoFinanciero INT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_TesConcAud_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TesoreriaConciliacionAuditoria PRIMARY KEY (IdTesoreriaConciliacionAuditoria),
        CONSTRAINT FK_TesConcAud_Conc FOREIGN KEY (IdTesoreriaConciliacion)
            REFERENCES dbo.TesoreriaConciliacion (IdTesoreriaConciliacion)
    );

    CREATE INDEX IX_TesConcAud_Conc
        ON dbo.TesoreriaConciliacionAuditoria (IdTesoreriaConciliacion, Fecha DESC);
END
GO

/* ---------- Módulo CONCILIACION_BANCARIA ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = 'CONCILIACION_BANCARIA')
BEGIN
    INSERT INTO dbo.Modulos (Codigo, Nombre, Descripcion, Activo, PrecioUSD, FechaCreacion)
    VALUES (
        'CONCILIACION_BANCARIA',
        N'Conciliación Bancaria',
        N'Centro de trabajo: extracto vs libro banco, matching y cierre auditable.',
        1,
        0,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.Modulos
    SET Activo = 1,
        Nombre = N'Conciliación Bancaria',
        Descripcion = N'Centro de trabajo: extracto vs libro banco, matching y cierre auditable.'
    WHERE Codigo = 'CONCILIACION_BANCARIA';
END
GO

PRINT 'Dev_Alter_Conciliacion_CentroTrabajo.sql OK';
GO
