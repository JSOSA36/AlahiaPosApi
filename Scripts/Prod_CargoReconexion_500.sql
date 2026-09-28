-- ============================================================
-- Cargo fijo de reconexión RD$ 500 para todos los clientes.
-- Solo AlahiaPos_Prod. Autorizado por el usuario.
-- ============================================================
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END
GO

-- Default de columna: 500
IF EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Empresas')
      AND COL_NAME(parent_object_id, parent_column_id) = N'CargoReconexionDop'
)
BEGIN
    DECLARE @df SYSNAME = (
        SELECT name
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Empresas')
          AND COL_NAME(parent_object_id, parent_column_id) = N'CargoReconexionDop'
    );
    EXEC(N'ALTER TABLE dbo.Empresas DROP CONSTRAINT [' + @df + N']');
END
GO

ALTER TABLE dbo.Empresas
    ADD CONSTRAINT DF_Empresas_CargoReconexionDop DEFAULT (500) FOR CargoReconexionDop;
GO

UPDATE dbo.Empresas
SET CargoReconexionDop = 500
WHERE ISNULL(EsEmpresaSistema, 0) = 0
  AND CargoReconexionDop <> 500;
GO

-- Recalcular línea RECONEXION en ciclos abiertos/vencidos: 500 / 60 = 8.33 USD
DECLARE @Tasa DECIMAL(18, 4) = 60;
DECLARE @CargoDop DECIMAL(18, 2) = 500;
DECLARE @CargoUsd DECIMAL(18, 2) = ROUND(@CargoDop / @Tasa, 2);

UPDATE d
SET d.Monto = @CargoUsd
FROM dbo.SuscripcionCicloDetalle d
INNER JOIN dbo.SuscripcionCiclo c ON c.IdCiclo = d.IdCiclo
INNER JOIN dbo.Empresas e ON e.IdEmpresa = c.IdEmpresa
WHERE d.TipoLinea = N'RECONEXION'
  AND c.Estado IN (N'ABIERTO', N'VENCIDO')
  AND ISNULL(e.EsEmpresaSistema, 0) = 0
  AND d.Monto <> @CargoUsd;

UPDATE c
SET c.Monto = ISNULL((
        SELECT SUM(d.Monto)
        FROM dbo.SuscripcionCicloDetalle d
        WHERE d.IdCiclo = c.IdCiclo
    ), c.Monto)
FROM dbo.SuscripcionCiclo c
INNER JOIN dbo.Empresas e ON e.IdEmpresa = c.IdEmpresa
WHERE c.Estado IN (N'ABIERTO', N'VENCIDO')
  AND ISNULL(e.EsEmpresaSistema, 0) = 0
  AND EXISTS (
        SELECT 1
        FROM dbo.SuscripcionCicloDetalle d
        WHERE d.IdCiclo = c.IdCiclo
          AND d.TipoLinea = N'RECONEXION'
  );
GO

PRINT N'Cargo reconexión RD$ 500 aplicado en AlahiaPos_Prod.';
GO
