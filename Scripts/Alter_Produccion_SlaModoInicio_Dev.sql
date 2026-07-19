-- ============================================================
-- Centro Producción: SLA multi-reloj + SlaModoInicio
-- SOLO AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: este script solo puede ejecutarse en AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

-- Flujo: desde qué evento arranca el reloj SLA
IF COL_LENGTH(N'dbo.ProduccionFlujo', N'SlaModoInicio') IS NULL
BEGIN
    ALTER TABLE dbo.ProduccionFlujo
        ADD SlaModoInicio NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionFlujo_SlaModo DEFAULT (N'CREACION');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProduccionFlujo_SlaModoInicio'
)
BEGIN
    ALTER TABLE dbo.ProduccionFlujo
        ADD CONSTRAINT CK_ProduccionFlujo_SlaModoInicio
        CHECK (SlaModoInicio IN (N'CREACION', N'INICIO_PREPARACION'));
END
GO

-- Trabajo: snapshot del modo (histórico)
IF COL_LENGTH(N'dbo.ProduccionTrabajo', N'SlaModoInicioSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ADD SlaModoInicioSnapshot NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ProduccionTrabajo_SlaModo DEFAULT (N'CREACION');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProduccionTrabajo_SlaModoInicio'
)
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ADD CONSTRAINT CK_ProduccionTrabajo_SlaModoInicio
        CHECK (SlaModoInicioSnapshot IN (N'CREACION', N'INICIO_PREPARACION'));
END
GO

-- FechaLimiteObjetivo nullable hasta que arranca el reloj (modo INICIO_PREPARACION)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ProduccionTrabajo')
      AND name = N'FechaLimiteObjetivo'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.ProduccionTrabajo
        ALTER COLUMN FechaLimiteObjetivo DATETIME NULL;
END
GO

-- POS_ORDEN: SLA mide preparación real (desde Iniciar)
UPDATE dbo.ProduccionFlujo
SET SlaModoInicio = N'INICIO_PREPARACION'
WHERE TipoTrabajoCodigo = N'POS_ORDEN';
GO

-- Trabajos activos POS aún sin iniciar: alinear snapshot y limpiar límite
UPDATE t
SET
    t.SlaModoInicioSnapshot = N'INICIO_PREPARACION',
    t.FechaLimiteObjetivo = CASE
        WHEN t.FechaInicio IS NULL THEN NULL
        ELSE DATEADD(SECOND, t.SlaObjetivoSegundosSnapshot, t.FechaInicio)
    END
FROM dbo.ProduccionTrabajo t
WHERE t.TipoTrabajoCodigo = N'POS_ORDEN'
  AND t.ActivoEnTablero = 1;
GO

SELECT
    f.IdFlujo, f.TipoTrabajoCodigo, f.Nombre, f.SlaObjetivoSegundos, f.SlaAdvertenciaSegundos, f.SlaModoInicio
FROM dbo.ProduccionFlujo f
WHERE f.TipoTrabajoCodigo = N'POS_ORDEN';
GO
