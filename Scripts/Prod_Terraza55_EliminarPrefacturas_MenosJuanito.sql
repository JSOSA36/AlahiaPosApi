-- PRODUCCIÓN — Terraza prolongación 27 (MATBERT SRL), IdEmpresa = 55.
-- Elimina prefacturas abiertas (IdTipoDocumentos = 10) excepto la cuenta "juanito".
-- No toca facturas tipo 1 ni maestros / perfiles / módulos.
USE AlahiaPos_Prod;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @Nombre NVARCHAR(200);

SELECT @Nombre = NombreComercial FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa;
IF @Nombre IS NULL OR (@Nombre NOT LIKE N'%Terraza%' AND @Nombre NOT LIKE N'%MATBERT%')
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza / MATBERT. Abortado.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#PrefasBorrar') IS NOT NULL DROP TABLE #PrefasBorrar;
SELECT h.IdFacturaHeader, h.NumeroDocumento, h.NombreCuenta, h.Total
INTO #PrefasBorrar
FROM dbo.FacturaHeaders h
WHERE h.IdEmpresa = @IdEmpresa
  AND h.IdTipoDocumentos = 10
  AND LTRIM(RTRIM(LOWER(ISNULL(h.NombreCuenta, N'')))) <> N'juanito';

IF NOT EXISTS (SELECT 1 FROM dbo.FacturaHeaders h
               WHERE h.IdEmpresa = @IdEmpresa
                 AND h.IdTipoDocumentos = 10
                 AND LTRIM(RTRIM(LOWER(ISNULL(h.NombreCuenta, N'')))) = N'juanito')
BEGIN
    RAISERROR(N'No se encontró la prefactura de juanito. Abortado para no borrar todo.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

DELETE d
FROM dbo.FacturaDetalles d
INNER JOIN #PrefasBorrar t ON t.IdFacturaHeader = d.IdFacturaHeader;

DELETE c
FROM dbo.FacturaCargo c
INNER JOIN #PrefasBorrar t ON t.IdFacturaHeader = c.IdFacturaHeader;

DELETE h
FROM dbo.FacturaHeaders h
INNER JOIN #PrefasBorrar t ON t.IdFacturaHeader = h.IdFacturaHeader;

COMMIT TRAN;

SELECT N'Eliminadas' Accion, IdFacturaHeader, NumeroDocumento, NombreCuenta, Total
FROM #PrefasBorrar;

SELECT h.IdFacturaHeader, h.NumeroDocumento, h.NombreCuenta, h.Total
FROM dbo.FacturaHeaders h
WHERE h.IdEmpresa = @IdEmpresa AND h.IdTipoDocumentos = 10;
GO
