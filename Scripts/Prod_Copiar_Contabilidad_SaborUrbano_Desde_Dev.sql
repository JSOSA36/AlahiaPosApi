-- Modelo de cuentas Sabor Urbano (Prod 73) guiado por Centro Odontológico DRA.SENA (Prod 60).
-- Catálogo = clínica en Prod (sin subcuentas QA). Mapeo/config = clínica en Dev (integración lista).
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

DECLARE @OrigenClinica INT = 60;
DECLARE @Destino INT = 73;
DECLARE @Nivel INT;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @Destino AND NombreComercial = N'Sabor Urbano'
)
BEGIN
    RAISERROR(N'No existe Sabor Urbano IdEmpresa=73 en Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CuentasContables WHERE IdEmpresa = @OrigenClinica)
BEGIN
    RAISERROR(N'DRA.SENA no tiene catálogo de cuentas en Prod.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM dbo.ContabilidadConfiguracion WHERE IdEmpresa = @Destino)
    INSERT INTO dbo.ContabilidadConfiguracion (
        IdEmpresa, IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS, FechaInseccion
    )
    SELECT TOP 1
        @Destino,
        ISNULL(c.IntegracionAutomatica, 1),
        ISNULL(c.GenerarCOGSAutomatico, 1),
        ISNULL(c.SepararAsientoCOGS, 1),
        GETDATE()
    FROM AlahiaPos_Dev.dbo.ContabilidadConfiguracion c
    WHERE c.IdEmpresa = @OrigenClinica;
ELSE
    UPDATE dest
    SET dest.IntegracionAutomatica = ISNULL(src.IntegracionAutomatica, 1),
        dest.GenerarCOGSAutomatico = ISNULL(src.GenerarCOGSAutomatico, 1),
        dest.SepararAsientoCOGS = ISNULL(src.SepararAsientoCOGS, 1),
        dest.FechaActualizacion = GETDATE()
    FROM dbo.ContabilidadConfiguracion dest
    INNER JOIN AlahiaPos_Dev.dbo.ContabilidadConfiguracion src
        ON src.IdEmpresa = @OrigenClinica
    WHERE dest.IdEmpresa = @Destino;

IF NOT EXISTS (SELECT 1 FROM dbo.ContabilidadConfiguracion WHERE IdEmpresa = @Destino)
    INSERT INTO dbo.ContabilidadConfiguracion (
        IdEmpresa, IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS, FechaInseccion
    )
    VALUES (@Destino, 1, 1, 1, GETDATE());

SET @Nivel = 1;
WHILE @Nivel <= 6
BEGIN
    INSERT INTO dbo.CuentasContables (
        IdEmpresa, Codigo, Nombre, TipoCuenta, IdCuentaPadre, Nivel, PermiteMovimiento, Activa, FechaInseccion
    )
    SELECT
        @Destino,
        src.Codigo,
        src.Nombre,
        src.TipoCuenta,
        padreDest.IdCuentaContable,
        src.Nivel,
        src.PermiteMovimiento,
        ISNULL(src.Activa, 1),
        GETDATE()
    FROM dbo.CuentasContables src
    LEFT JOIN dbo.CuentasContables padreSrc
        ON padreSrc.IdCuentaContable = src.IdCuentaPadre
    LEFT JOIN dbo.CuentasContables padreDest
        ON padreDest.IdEmpresa = @Destino AND padreDest.Codigo = padreSrc.Codigo
    WHERE src.IdEmpresa = @OrigenClinica
      AND src.Nivel = @Nivel
      AND NOT EXISTS (
          SELECT 1 FROM dbo.CuentasContables x
          WHERE x.IdEmpresa = @Destino AND x.Codigo = src.Codigo
      );

    SET @Nivel += 1;
END;

INSERT INTO dbo.ContabilidadCuentaMapeo (IdEmpresa, CodigoConcepto, IdCuentaContable, Activo)
SELECT
    @Destino,
    mp.CodigoConcepto,
    dest.IdCuentaContable,
    ISNULL(mp.Activo, 1)
FROM AlahiaPos_Dev.dbo.ContabilidadCuentaMapeo mp
INNER JOIN AlahiaPos_Dev.dbo.CuentasContables src
    ON src.IdCuentaContable = mp.IdCuentaContable
INNER JOIN dbo.CuentasContables dest
    ON dest.IdEmpresa = @Destino AND dest.Codigo = src.Codigo
WHERE mp.IdEmpresa = @OrigenClinica
  AND NOT EXISTS (
      SELECT 1 FROM dbo.ContabilidadCuentaMapeo x
      WHERE x.IdEmpresa = @Destino AND x.CodigoConcepto = mp.CodigoConcepto
  );

UPDATE destMap
SET destMap.IdCuentaContable = dest.IdCuentaContable,
    destMap.Activo = ISNULL(mp.Activo, 1)
FROM dbo.ContabilidadCuentaMapeo destMap
INNER JOIN AlahiaPos_Dev.dbo.ContabilidadCuentaMapeo mp
    ON mp.IdEmpresa = @OrigenClinica AND mp.CodigoConcepto = destMap.CodigoConcepto
INNER JOIN AlahiaPos_Dev.dbo.CuentasContables src
    ON src.IdCuentaContable = mp.IdCuentaContable
INNER JOIN dbo.CuentasContables dest
    ON dest.IdEmpresa = @Destino AND dest.Codigo = src.Codigo
WHERE destMap.IdEmpresa = @Destino;

UPDATE cf
SET cf.IdCuentaContable = cc.IdCuentaContable
FROM dbo.CuentaFinanciera cf
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = cf.IdEmpresa AND cc.Codigo = N'1.1.1'
WHERE cf.IdEmpresa = @Destino
  AND cf.TipoCuenta = N'CAJA'
  AND cf.IdCuentaContable IS NULL;

UPDATE cf
SET cf.IdCuentaContable = cc.IdCuentaContable
FROM dbo.CuentaFinanciera cf
INNER JOIN dbo.CuentasContables cc
    ON cc.IdEmpresa = cf.IdEmpresa AND cc.Codigo = N'1.1.2'
WHERE cf.IdEmpresa = @Destino
  AND cf.TipoCuenta = N'BANCO'
  AND cf.IdCuentaContable IS NULL;

COMMIT TRAN;

SELECT COUNT(*) AS CuentasGL FROM dbo.CuentasContables WHERE IdEmpresa = @Destino;

SELECT Codigo, Nombre, TipoCuenta, Nivel, PermiteMovimiento
FROM dbo.CuentasContables
WHERE IdEmpresa = @Destino
ORDER BY Codigo;

SELECT mp.CodigoConcepto, cc.Codigo, cc.Nombre
FROM dbo.ContabilidadCuentaMapeo mp
JOIN dbo.CuentasContables cc ON cc.IdCuentaContable = mp.IdCuentaContable
WHERE mp.IdEmpresa = @Destino
ORDER BY mp.CodigoConcepto;

SELECT cf.Nombre, cf.TipoCuenta, cc.Codigo AS CuentaGL, cc.Nombre AS NombreGL
FROM dbo.CuentaFinanciera cf
LEFT JOIN dbo.CuentasContables cc ON cc.IdCuentaContable = cf.IdCuentaContable
WHERE cf.IdEmpresa = @Destino;

SELECT IntegracionAutomatica, GenerarCOGSAutomatico, SepararAsientoCOGS
FROM dbo.ContabilidadConfiguracion
WHERE IdEmpresa = @Destino;
GO
