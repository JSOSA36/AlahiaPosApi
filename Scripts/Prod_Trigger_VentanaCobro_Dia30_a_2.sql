-- Impide suspensión automática durante la ventana de cobro: día 30 + 3 días (31, 1 y 2).
-- El 31 no es vencimiento. Suspender a partir del día 3. Sin publicar API.
-- Autorizado: producción.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

-- Flawless Laundry (59): restaurar servicio y quitar reconexión del ciclo agosto.
UPDATE dbo.Empresas
SET EstadoServicio = N'PENDIENTE_PAGO',
    ReconexionPendiente = 0
WHERE IdEmpresa = 59
  AND NombreComercial LIKE N'%Flawless%';

UPDATE c
SET c.Estado = N'ABIERTO',
    c.Monto = e.MontoServicio + e.CargoAdicional
FROM dbo.SuscripcionCiclo c
INNER JOIN dbo.Empresas e ON e.IdEmpresa = c.IdEmpresa
WHERE c.IdEmpresa = 59
  AND c.Anio = 2026 AND c.Mes = 8
  AND e.NombreComercial LIKE N'%Flawless%';

DELETE d
FROM dbo.SuscripcionCicloDetalle d
INNER JOIN dbo.SuscripcionCiclo c ON c.IdCiclo = d.IdCiclo
WHERE c.IdEmpresa = 59 AND c.Anio = 2026 AND c.Mes = 8
  AND d.TipoLinea = N'RECONEXION';

INSERT INTO dbo.SuscripcionEvento (IdEmpresa, IdCiclo, Tipo, Detalle, Canal, Fecha)
SELECT 59, c.IdCiclo, N'REVERSA_SUSPENSION_DIA31',
       N'Reversa: 3 días después del 30 (31, 1 y 2). Plazo hasta el día 2. Sin reconexión.',
       N'SISTEMA', GETDATE()
FROM dbo.SuscripcionCiclo c
WHERE c.IdEmpresa = 59 AND c.Anio = 2026 AND c.Mes = 8;

COMMIT TRAN;
GO

IF OBJECT_ID(N'dbo.trg_Empresas_NoSuspenderEnVentanaCobro', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Empresas_NoSuspenderEnVentanaCobro;
GO

CREATE TRIGGER dbo.trg_Empresas_NoSuspenderEnVentanaCobro
ON dbo.Empresas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @dia INT = DAY(CONVERT(date, GETDATE()));

    -- Ventana: 30 y los 3 días siguientes (31, 1, 2). El día 3 ya se puede suspender.
    IF @dia NOT IN (30, 31, 1, 2)
        RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE ISNULL(i.EsEmpresaSistema, 0) = 0
          AND ISNULL(i.EstadoServicio, N'') <> N'CANCELADA'
          AND (
                i.EstadoServicio IN (N'SUSPENDIDA', N'BLOQUEADO')
                OR ISNULL(i.ReconexionPendiente, 0) = 1
              )
    )
    BEGIN
        THROW 50031, N'Ventana de cobro activa (día 30 más 3 días: 31, 1 y 2). No se suspende hasta el día 3.', 1;
    END
END;
GO

SELECT IdEmpresa, NombreComercial, EstadoServicio, PagadoServicio, ReconexionPendiente
FROM dbo.Empresas
WHERE IdEmpresa = 59;

SELECT IdCiclo, Estado, Monto
FROM dbo.SuscripcionCiclo
WHERE IdEmpresa = 59 AND Anio = 2026 AND Mes = 8;

SELECT name, is_disabled
FROM sys.triggers
WHERE name = N'trg_Empresas_NoSuspenderEnVentanaCobro';
GO
