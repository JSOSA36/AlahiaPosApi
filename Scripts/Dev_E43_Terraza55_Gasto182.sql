-- AlahiaPos_Dev — Secuencia E43 (gastos menores) para Terraza 27 + e-NCF del gasto 182
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
  RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
  RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdGasto INT = 182;
DECLARE @Encf NVARCHAR(13);

IF NOT EXISTS (
  SELECT 1 FROM dbo.SecuenciasECF
  WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 43 AND Activo = 1
)
BEGIN
  INSERT INTO dbo.SecuenciasECF (
    IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
    stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
    Ambiente, FechaAutorizacion
  )
  VALUES (
    @IdEmpresa, N'E43', N'E43', 1, 999, '2028-12-31',
    20, 1, GETDATE(), N'Gastos Menores Electrónico', 43, 1,
    N'PRUEBAS', GETDATE()
  );
END;

IF EXISTS (
  SELECT 1 FROM dbo.Gastos
  WHERE IdGasto = @IdGasto AND IdEmpresa = @IdEmpresa
    AND TipoComprobante = N'Comprobante para Gastos Menores'
    AND (NumeroComprobante IS NULL OR LTRIM(RTRIM(NumeroComprobante)) = N'')
)
BEGIN
  UPDATE TOP (1) dbo.SecuenciasECF
  SET SecuenciaActual = SecuenciaActual + 1
  WHERE IdEmpresa = @IdEmpresa
    AND TipoEcfDgii = 43
    AND Activo = 1
    AND SecuenciaActual <= SecuenciaFinal
    AND fechaVencimiento > GETDATE();

  SELECT TOP (1) @Encf = Serie + RIGHT(REPLICATE('0', 10) + CAST(SecuenciaActual - 1 AS VARCHAR(10)), 10)
  FROM dbo.SecuenciasECF
  WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 43 AND Activo = 1
  ORDER BY IdSecuencia DESC;

  UPDATE g
  SET g.NumeroComprobante = @Encf,
      g.FechaComprobante = CAST(g.FechaInseccion AS DATE),
      g.RncEmisorComprobante = ISNULL(NULLIF(LTRIM(RTRIM(e.RNC)), N''), g.RncEmisorComprobante),
      g.NombreEmisorComprobante = ISNULL(e.NombreComercial, g.NombreEmisorComprobante)
  FROM dbo.Gastos g
  INNER JOIN dbo.Empresas e ON e.IdEmpresa = g.IdEmpresa
  WHERE g.IdGasto = @IdGasto AND g.IdEmpresa = @IdEmpresa;
END;

SELECT IdSecuencia, TipoEcfDgii, Serie, SecuenciaActual, SecuenciaFinal, Activo
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa AND TipoEcfDgii = 43;

SELECT IdGasto, TipoComprobante, NumeroComprobante, FechaComprobante, Monto, RncEmisorComprobante
FROM dbo.Gastos
WHERE IdGasto = @IdGasto;
GO
