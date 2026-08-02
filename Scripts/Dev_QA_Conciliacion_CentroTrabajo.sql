-- AlahiaPos_Dev — QA esquema e integridad Centro de Conciliación Bancaria
-- No toca Prod. Verifica DDL, índices, estados y reglas de una sola sesión abierta.
USE AlahiaPos_Dev;
GO

SET NOCOUNT ON;

DECLARE @Errores INT = 0;

PRINT '=== QA Conciliación Centro Trabajo ===';

IF COL_LENGTH('dbo.TesoreriaExtractoImport', 'IdTesoreriaConciliacion') IS NULL
BEGIN
    PRINT 'FAIL: falta IdTesoreriaConciliacion en TesoreriaExtractoImport';
    SET @Errores += 1;
END
ELSE PRINT 'OK: TesoreriaExtractoImport.IdTesoreriaConciliacion';

IF COL_LENGTH('dbo.TesoreriaConciliacion', 'SaldoBancoInicial') IS NULL
BEGIN
    PRINT 'FAIL: falta SaldoBancoInicial';
    SET @Errores += 1;
END
ELSE PRINT 'OK: TesoreriaConciliacion.SaldoBancoInicial';

IF COL_LENGTH('dbo.TesoreriaConciliacion', 'RowVersion') IS NULL
BEGIN
    PRINT 'FAIL: falta RowVersion en TesoreriaConciliacion';
    SET @Errores += 1;
END
ELSE PRINT 'OK: TesoreriaConciliacion.RowVersion';

IF OBJECT_ID('dbo.TesoreriaConciliacionAuditoria', 'U') IS NULL
BEGIN
    PRINT 'FAIL: falta tabla TesoreriaConciliacionAuditoria';
    SET @Errores += 1;
END
ELSE PRINT 'OK: TesoreriaConciliacionAuditoria';

IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE Codigo = 'CONCILIACION_BANCARIA' AND Activo = 1)
BEGIN
    PRINT 'FAIL: módulo CONCILIACION_BANCARIA no activo';
    SET @Errores += 1;
END
ELSE PRINT 'OK: módulo CONCILIACION_BANCARIA';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesConc_CuentaAbierta'
)
BEGIN
    PRINT 'WARN: índice UX_TesConc_CuentaAbierta no encontrado';
    SET @Errores += 1;
END
ELSE PRINT 'OK: índice UX_TesConc_CuentaAbierta';

;WITH Abiertas AS (
    SELECT IdCuentaFinanciera, COUNT(*) AS Cnt
    FROM dbo.TesoreriaConciliacion
    WHERE Estado IN ('BORRADOR', 'EN_PROCESO')
    GROUP BY IdCuentaFinanciera
    HAVING COUNT(*) > 1
)
SELECT @Errores = @Errores + COUNT(*) FROM Abiertas;

IF EXISTS (
    SELECT 1
    FROM dbo.TesoreriaConciliacion
    WHERE Estado IN ('BORRADOR', 'EN_PROCESO')
    GROUP BY IdCuentaFinanciera
    HAVING COUNT(*) > 1
)
    PRINT 'FAIL: hay más de una conciliación abierta por cuenta';
ELSE
    PRINT 'OK: sin solapes de conciliaciones abiertas';

-- Columnas de línea para matching explicable
IF COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ReglaMatch') IS NULL
    OR COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ExplicacionMatch') IS NULL
    OR COL_LENGTH('dbo.TesoreriaExtractoLinea', 'ClasificacionLinea') IS NULL
BEGIN
    PRINT 'FAIL: faltan columnas de matching en TesoreriaExtractoLinea';
    SET @Errores += 1;
END
ELSE PRINT 'OK: columnas ReglaMatch / ExplicacionMatch / ClasificacionLinea';

PRINT CONCAT('=== Resultado QA: ', CASE WHEN @Errores = 0 THEN 'PASS' ELSE CONCAT('FAIL (', @Errores, ')') END, ' ===');
GO
