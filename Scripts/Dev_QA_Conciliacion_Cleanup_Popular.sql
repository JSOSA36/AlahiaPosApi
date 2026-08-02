-- Dev only: inventario + limpieza acotada de pruebas Popular / extractos QA.
-- NO DDL. NO Prod. Filtrado por IdEmpresa + criterios de prueba.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @IdEmpresa INT = 60;

PRINT '=== INVENTARIO PREVIO ===';

SELECT 'ExtractoPopular' AS Tipo, i.IdTesoreriaExtractoImport, i.IdCuentaFinanciera, i.NombreArchivo, i.Banco, i.NumeroCuentaBanco, i.Estado, i.IdTesoreriaConciliacion
FROM dbo.TesoreriaExtractoImport i
WHERE i.IdEmpresa = @IdEmpresa
  AND (
        i.NumeroCuentaBanco = '1234567890'
     OR i.NombreArchivo LIKE '%Estado de Cuenta Bancaria%'
     OR i.NombreArchivo LIKE '%Popular%'
     OR i.Banco LIKE '%Popular%'
     OR i.NombreArchivo LIKE 'QA-CB-%'
  );

SELECT 'ConciliacionCuentaPopular' AS Tipo, c.IdTesoreriaConciliacion, c.IdCuentaFinanciera, c.Estado, c.IdExtractoPrincipal, c.PeriodoDesde, c.PeriodoHasta
FROM dbo.TesoreriaConciliacion c
WHERE c.IdEmpresa = @IdEmpresa
  AND (
        c.IdCuentaFinanciera IN (
            SELECT IdCuentaFinanciera FROM dbo.CuentaFinanciera
            WHERE IdEmpresa = @IdEmpresa AND (Nombre LIKE '%BANCO POPULAR%' OR Nombre LIKE 'QA Conciliación Popular%')
        )
     OR c.IdExtractoPrincipal IN (
            SELECT IdTesoreriaExtractoImport FROM dbo.TesoreriaExtractoImport
            WHERE IdEmpresa = @IdEmpresa
              AND (NumeroCuentaBanco = '1234567890' OR NombreArchivo LIKE '%Estado de Cuenta Bancaria%' OR NombreArchivo LIKE 'QA-CB-%')
        )
  );

BEGIN TRAN;

DECLARE @Imports TABLE (Id INT PRIMARY KEY);
INSERT INTO @Imports (Id)
SELECT IdTesoreriaExtractoImport
FROM dbo.TesoreriaExtractoImport
WHERE IdEmpresa = @IdEmpresa
  AND (
        NumeroCuentaBanco = '1234567890'
     OR NombreArchivo LIKE '%Estado de Cuenta Bancaria%'
     OR NombreArchivo LIKE '%Popular%'
     OR Banco LIKE '%Popular%'
     OR NombreArchivo LIKE 'QA-CB-%'
  );

DECLARE @Concs TABLE (Id INT PRIMARY KEY);
INSERT INTO @Concs (Id)
SELECT DISTINCT c.IdTesoreriaConciliacion
FROM dbo.TesoreriaConciliacion c
WHERE c.IdEmpresa = @IdEmpresa
  AND (
        c.IdExtractoPrincipal IN (SELECT Id FROM @Imports)
     OR EXISTS (
            SELECT 1 FROM dbo.TesoreriaExtractoImport i
            WHERE i.IdTesoreriaConciliacion = c.IdTesoreriaConciliacion
              AND i.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports)
        )
     OR c.IdCuentaFinanciera IN (
            SELECT IdCuentaFinanciera FROM dbo.CuentaFinanciera
            WHERE IdEmpresa = @IdEmpresa AND Nombre LIKE 'QA Conciliación Popular%'
        )
  );

PRINT '=== BORRANDO ===';

-- PagoReclasificacion ligado a líneas/movimientos de estas sesiones
IF OBJECT_ID('dbo.PagoReclasificacion', 'U') IS NOT NULL
BEGIN
    DELETE pr
    FROM dbo.PagoReclasificacion pr
    WHERE pr.IdEmpresa = @IdEmpresa
      AND (
            pr.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs)
         OR pr.IdTesoreriaExtractoLinea IN (
                SELECT l.IdTesoreriaExtractoLinea
                FROM dbo.TesoreriaExtractoLinea l
                WHERE l.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports)
            )
      );
END

IF OBJECT_ID('dbo.TesoreriaConciliacionAuditoria', 'U') IS NOT NULL
BEGIN
    DELETE a
    FROM dbo.TesoreriaConciliacionAuditoria a
    WHERE a.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs);
END

IF OBJECT_ID('dbo.TesoreriaConciliacionLinea', 'U') IS NOT NULL
BEGIN
    DELETE cl
    FROM dbo.TesoreriaConciliacionLinea cl
    WHERE cl.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs);
END

-- Desvincular movimientos de esas conciliaciones (no borrar cobros/pagos reales ajenos)
UPDATE m
SET m.IdTesoreriaConciliacion = NULL,
    m.EstadoConciliacion = CASE WHEN m.ReferenciaTipo IN ('EXTRACTO', 'RECLASIFICAR_PAGO') THEN m.EstadoConciliacion ELSE 'PENDIENTE' END,
    m.FechaConciliacion = NULL,
    m.IdUsuarioConciliacion = NULL
FROM dbo.MovimientoFinanciero m
WHERE m.IdEmpresa = @IdEmpresa
  AND m.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs);

-- Romper FK línea → movimiento antes de borrar movimientos EXTRACTO
UPDATE l
SET l.IdMovimientoFinanciero = NULL
FROM dbo.TesoreriaExtractoLinea l
WHERE l.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports);

-- Borrar SOLO movimientos creados desde extracto / reclasificación / seed QA de este escenario
DELETE m
FROM dbo.MovimientoFinanciero m
WHERE m.IdEmpresa = @IdEmpresa
  AND (
        (m.ReferenciaTipo = 'EXTRACTO' AND m.ReferenciaId IN (
            SELECT l.IdTesoreriaExtractoLinea FROM dbo.TesoreriaExtractoLinea l
            WHERE l.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports)
        ))
     OR (m.ClaveIdempotencia LIKE 'EXT_%' AND EXISTS (
            SELECT 1 FROM @Imports i
            WHERE m.ClaveIdempotencia LIKE 'EXT_' + CAST(i.Id AS VARCHAR(20)) + '_%'
        ))
     OR (m.ReferenciaTipo = 'RECLASIFICAR_PAGO' AND m.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs))
     OR (m.ClaveIdempotencia LIKE 'QA-CB-%')
     OR (m.Motivo LIKE 'QA-CB-%')
  );

DELETE l
FROM dbo.TesoreriaExtractoLinea l
WHERE l.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports);

UPDATE c SET c.IdExtractoPrincipal = NULL
FROM dbo.TesoreriaConciliacion c
WHERE c.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs);

DELETE i
FROM dbo.TesoreriaExtractoImport i
WHERE i.IdTesoreriaExtractoImport IN (SELECT Id FROM @Imports);

DELETE c
FROM dbo.TesoreriaConciliacion c
WHERE c.IdTesoreriaConciliacion IN (SELECT Id FROM @Concs);

-- Desactivar cuentas QA previas de este escenario (no borrar historial ajeno)
UPDATE cf
SET cf.Activa = 0
FROM dbo.CuentaFinanciera cf
WHERE cf.IdEmpresa = @IdEmpresa
  AND cf.Nombre LIKE 'QA Conciliación Popular%';

COMMIT;

PRINT '=== POST-LIMPIEZA ===';
SELECT COUNT(*) AS ImportsRestantes
FROM dbo.TesoreriaExtractoImport
WHERE IdEmpresa = @IdEmpresa
  AND (NumeroCuentaBanco = '1234567890' OR NombreArchivo LIKE '%Estado de Cuenta Bancaria%' OR NombreArchivo LIKE 'QA-CB-%');

SELECT COUNT(*) AS ConcsQARestantes
FROM dbo.TesoreriaConciliacion c
INNER JOIN dbo.CuentaFinanciera cf ON cf.IdCuentaFinanciera = c.IdCuentaFinanciera
WHERE c.IdEmpresa = @IdEmpresa AND cf.Nombre LIKE 'QA Conciliación Popular%';
