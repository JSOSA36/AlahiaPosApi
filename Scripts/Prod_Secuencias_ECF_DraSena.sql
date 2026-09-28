-- Secuencias e-CF de producción para Centro Odontológico Dra Sena (empresa 60).
-- Solicitud DGII 6009988267 (22/09/2026, APROBADA). Autorizado por el usuario.
-- No modifica Empresas.RNC. El e-CF sale con RNC 133659115 por código.
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

DECLARE @IdEmpresa INT = 60;
DECLARE @IdSucursal INT;
DECLARE @FechaAut DATETIME2 = '2026-09-22';
DECLARE @VencE31 DATETIME = '2027-12-31';
DECLARE @VencE43 DATETIME = '2027-12-31';
DECLARE @VencSinFecha DATETIME = '2099-12-31';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND NombreComercial LIKE N'%Sena%'
)
BEGIN
    RAISERROR(N'No se encontró Dra Sena (empresa 60) en Prod.', 16, 1);
    RETURN;
END;

SELECT @IdSucursal = IdSucursal
FROM dbo.Sucursal
WHERE IdEmpresa = @IdEmpresa AND Activa = 1 AND EsPrincipal = 1;

IF @IdSucursal IS NULL
BEGIN
    RAISERROR(N'Dra Sena no tiene sucursal principal activa.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

-- Tradicionales B quedan inactivas. El historial no se borra.
UPDATE dbo.SecuenciasECF
SET Activo = 0
WHERE IdEmpresa = @IdEmpresa
  AND Serie IN (N'B01', N'B04')
  AND ISNULL(TipoEcfDgii, 0) = 0;

DECLARE @Tipos TABLE (
    TipoEcf INT NOT NULL PRIMARY KEY,
    TipoNCF NVARCHAR(10) NOT NULL,
    Descripcion NVARCHAR(100) NOT NULL,
    Final INT NOT NULL,
    Vence DATETIME NOT NULL,
    Stock INT NOT NULL,
    Resolucion NVARCHAR(50) NOT NULL
);

INSERT INTO @Tipos (TipoEcf, TipoNCF, Descripcion, Final, Vence, Stock, Resolucion)
VALUES
    (31, N'E31', N'Factura de Crédito Fiscal Electrónica', 20, @VencE31, 5, N'6005508835'),
    (32, N'E32', N'Factura de Consumo Electrónica', 100, @VencSinFecha, 10, N'6005508836'),
    (34, N'E34', N'Nota de Crédito Electrónica', 20, @VencSinFecha, 5, N'6005508837'),
    (43, N'E43', N'Gastos Menores Electrónico', 100, @VencE43, 10, N'6005508838');

UPDATE s
SET s.TipoNCF = t.TipoNCF,
    s.Serie = t.TipoNCF,
    s.Descripcion = t.Descripcion,
    s.SecuenciaInicial = 1,
    s.SecuenciaActual = 1,
    s.SecuenciaFinal = t.Final,
    s.fechaVencimiento = t.Vence,
    s.stockMinimo = t.Stock,
    s.Activo = 1,
    s.Ambiente = N'PRODUCCION',
    s.FechaAutorizacion = @FechaAut,
    s.NumeroResolucion = t.Resolucion,
    s.IdSucursal = NULL
FROM dbo.SecuenciasECF s
INNER JOIN @Tipos t ON t.TipoEcf = s.TipoEcfDgii
WHERE s.IdEmpresa = @IdEmpresa;

INSERT INTO dbo.SecuenciasECF (
    IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
    stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
    Ambiente, FechaAutorizacion, NumeroResolucion
)
SELECT
    @IdEmpresa, t.TipoNCF, t.TipoNCF, 1, t.Final, t.Vence,
    t.Stock, 1, GETDATE(), t.Descripcion, t.TipoEcf, 1,
    N'PRODUCCION', @FechaAut, t.Resolucion
FROM @Tipos t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SecuenciasECF s
    WHERE s.IdEmpresa = @IdEmpresa AND s.TipoEcfDgii = t.TipoEcf
);

UPDATE a
SET a.SecuenciaInicial = 1,
    a.SecuenciaActual = 1,
    a.SecuenciaFinal = t.Final,
    a.Activo = 1
FROM dbo.SecuenciaECFAsignacion a
INNER JOIN dbo.SecuenciasECF s ON s.IdSecuencia = a.IdSecuencia
INNER JOIN @Tipos t ON t.TipoEcf = s.TipoEcfDgii
WHERE a.IdEmpresa = @IdEmpresa
  AND a.IdSucursal = @IdSucursal;

INSERT INTO dbo.SecuenciaECFAsignacion (
    IdSecuencia, IdEmpresa, TipoEcfDgii, IdSucursal,
    SecuenciaInicial, SecuenciaFinal, SecuenciaActual, Activo, FechaCreacion
)
SELECT s.IdSecuencia, @IdEmpresa, s.TipoEcfDgii, @IdSucursal,
       1, s.SecuenciaFinal, 1, 1, GETDATE()
FROM dbo.SecuenciasECF s
INNER JOIN @Tipos t ON t.TipoEcf = s.TipoEcfDgii
WHERE s.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SecuenciaECFAsignacion a
      WHERE a.IdSecuencia = s.IdSecuencia AND a.IdSucursal = @IdSucursal
  );

UPDATE dbo.Empresas
SET AmbienteFE = N'ecf',
    EsEmisorElectronico = 1,
    EstadoFE = N'Activo',
    FechaHabilitacionFE = ISNULL(FechaHabilitacionFE, GETDATE())
WHERE IdEmpresa = @IdEmpresa;

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, RNC, AmbienteFE, EsEmisorElectronico, EstadoFE
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SELECT TipoNCF, TipoEcfDgii, SecuenciaInicial, SecuenciaActual, SecuenciaFinal,
       CONVERT(varchar(10), fechaVencimiento, 23) AS Vence,
       Activo, Ambiente, NumeroResolucion
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa
ORDER BY ISNULL(TipoEcfDgii, 0), IdSecuencia;

SELECT a.TipoEcfDgii, a.IdSucursal, a.SecuenciaInicial, a.SecuenciaActual, a.SecuenciaFinal, a.Activo
FROM dbo.SecuenciaECFAsignacion a
WHERE a.IdEmpresa = @IdEmpresa
ORDER BY a.TipoEcfDgii;
GO
