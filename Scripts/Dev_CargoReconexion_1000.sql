-- AlahiaPos_Dev — cargo fijo de reconexión RD$ 1,000 para todos los clientes.

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.Empresas', 'CargoReconexionDop') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas ADD CargoReconexionDop DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Empresas_CargoReconexionDop DEFAULT (1000);
END
GO

-- Default de columna: 1000
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
    ADD CONSTRAINT DF_Empresas_CargoReconexionDop DEFAULT (1000) FOR CargoReconexionDop;
GO

UPDATE dbo.Empresas
SET CargoReconexionDop = 1000
WHERE ISNULL(EsEmpresaSistema, 0) = 0
  AND CargoReconexionDop <> 1000;
GO

DECLARE @Tasa DECIMAL(18, 4) = 60;
DECLARE @CargoDop DECIMAL(18, 2) = 1000;
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
