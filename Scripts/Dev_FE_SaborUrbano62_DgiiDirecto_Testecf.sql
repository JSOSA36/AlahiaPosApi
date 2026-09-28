-- Facturación electrónica de prueba para Sabor Urbano (empresa 62) en Dev.
-- DGII_DIRECTO + testecf (mismo camino POS que ya funciona). No toca CerteCF ni otras empresas.
-- PerfilRoles solo Administrador. No activa FE_CERTIFICACION.
USE AlahiaPos_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Dev'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Dev.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 62;
DECLARE @Venc DATETIME = '2099-12-31';
DECLARE @Ahora DATETIME = GETDATE();

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND NombreComercial = N'Sabor Urbano'
)
BEGIN
    RAISERROR(N'No se encontró Sabor Urbano (empresa 62) en Dev.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE dbo.Empresas
SET EsEmisorElectronico = 1,
    AmbienteFE = N'testecf',
    ProveedorFE = N'DGII_DIRECTO',
    ProveedorFE_Nombre = N'Alahia.eCF.Api / DGII Directo',
    EstadoFE = N'Activo',
    FechaHabilitacionFE = ISNULL(FechaHabilitacionFE, @Ahora)
WHERE IdEmpresa = @IdEmpresa
  AND NombreComercial = N'Sabor Urbano';

IF EXISTS (SELECT 1 FROM dbo.DgiiConfiguracionEmpresa WHERE IdEmpresa = @IdEmpresa)
BEGIN
    UPDATE dbo.DgiiConfiguracionEmpresa
    SET FiscalActivo = 1,
        FacturacionElectronicaActiva = 1,
        RazonSocial = COALESCE(NULLIF(RazonSocial, N''), N'Sabor Urbano'),
        Activo = 1
    WHERE IdEmpresa = @IdEmpresa;
END
ELSE
BEGIN
    INSERT INTO dbo.DgiiConfiguracionEmpresa
    (
        IdEmpresa, RegimenTributarioCodigo, EsConstructor, EsComisionista, ObligadoLibroVentasSF,
        RazonSocial, VersionInstructivoPreferida, Activo, FechaCreacion,
        FiscalActivo, Generar606, Generar607, GenerarIt1, FacturacionElectronicaActiva
    )
    VALUES
    (
        @IdEmpresa, N'ORDINARIO', 0, 0, 0,
        N'Sabor Urbano', N'1.0', 1, @Ahora,
        1, 1, 1, 1, 1
    );
END;

IF EXISTS (
    SELECT 1 FROM dbo.Parametros
    WHERE IdEmpresa = @IdEmpresa AND Clave = N'FACTURACION_ELECTRONICA'
)
    UPDATE dbo.Parametros
    SET Valor = N'true', Activo = 1
    WHERE IdEmpresa = @IdEmpresa AND Clave = N'FACTURACION_ELECTRONICA';
ELSE
    INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
    VALUES (@IdEmpresa, N'EMPRESA', NULL, N'FACTURACION_ELECTRONICA', N'true',
            N'Emisión de comprobantes fiscales electrónicos', @Ahora, 1);

IF EXISTS (
    SELECT 1 FROM dbo.Parametros
    WHERE IdEmpresa = @IdEmpresa AND Clave = N'PREVIEW_DGII'
)
    UPDATE dbo.Parametros
    SET Valor = N'true', Activo = 1
    WHERE IdEmpresa = @IdEmpresa AND Clave = N'PREVIEW_DGII';
ELSE
    INSERT INTO dbo.Parametros (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
    VALUES (@IdEmpresa, N'EMPRESA', NULL, N'PREVIEW_DGII', N'true',
            N'Si esta activo, el POS muestra vista previa del comprobante al facturar.', @Ahora, 1);

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, @Ahora
FROM dbo.Modulos m
WHERE m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR'
    )
  AND m.Activo = 1
  AND NOT EXISTS (
        SELECT 1 FROM dbo.Empresa_Modulos em
        WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
    );

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR'
    );

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, @Ahora, @IdEmpresa
FROM dbo.Perfiles p
CROSS JOIN dbo.Modulos m
WHERE p.IdEmpresa = @IdEmpresa
  AND p.Activo = 1
  AND p.Nombre = N'Administrador'
  AND m.Activo = 1
  AND m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR'
    )
  AND NOT EXISTS (
        SELECT 1 FROM dbo.PerfilRoles pr
        WHERE pr.IdPerfil = p.IdPerfil
          AND pr.IdModulo = m.Id
          AND pr.IdEmpresa = @IdEmpresa
    );

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
  AND p.IdEmpresa = @IdEmpresa
  AND p.Nombre = N'Administrador'
  AND m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR'
    );

;WITH tipos AS (
    SELECT 31 AS Tipo, N'E31' AS Serie, N'Factura de Crédito Fiscal Electrónica' AS Descripcion, 20 AS Stock
    UNION ALL SELECT 32, N'E32', N'Factura de Consumo Electrónica', 50
    UNION ALL SELECT 33, N'E33', N'Nota de Débito Electrónica', 5
    UNION ALL SELECT 34, N'E34', N'Nota de Crédito Electrónica', 5
)
UPDATE s
SET s.TipoNCF = t.Serie,
    s.Serie = t.Serie,
    s.Descripcion = t.Descripcion,
    s.SecuenciaInicial = 1,
    s.SecuenciaActual = CASE WHEN s.SecuenciaActual BETWEEN 1 AND 999 THEN s.SecuenciaActual ELSE 1 END,
    s.SecuenciaFinal = 999,
    s.fechaVencimiento = @Venc,
    s.stockMinimo = t.Stock,
    s.Activo = 1,
    s.Ambiente = N'PRUEBAS',
    s.NumeroResolucion = N'PRUEBAS'
FROM dbo.SecuenciasECF s
INNER JOIN tipos t ON t.Tipo = s.TipoEcfDgii
WHERE s.IdEmpresa = @IdEmpresa;

INSERT INTO dbo.SecuenciasECF (
    IdEmpresa, TipoNCF, Serie, SecuenciaActual, SecuenciaFinal, fechaVencimiento,
    stockMinimo, Activo, FechaCreacion, Descripcion, TipoEcfDgii, SecuenciaInicial,
    Ambiente, FechaAutorizacion, NumeroResolucion
)
SELECT
    @IdEmpresa, t.Serie, t.Serie, 1, 999, @Venc,
    t.Stock, 1, @Ahora, t.Descripcion, t.Tipo, 1,
    N'PRUEBAS', @Ahora, N'PRUEBAS'
FROM (
    SELECT 31 AS Tipo, N'E31' AS Serie, N'Factura de Crédito Fiscal Electrónica' AS Descripcion, 20 AS Stock
    UNION ALL SELECT 32, N'E32', N'Factura de Consumo Electrónica', 50
    UNION ALL SELECT 33, N'E33', N'Nota de Débito Electrónica', 5
    UNION ALL SELECT 34, N'E34', N'Nota de Crédito Electrónica', 5
) t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SecuenciasECF s
    WHERE s.IdEmpresa = @IdEmpresa AND s.TipoEcfDgii = t.Tipo
);

-- Con 2 sucursales el POS no usa la autorización de empresa: hay que asignar tramos.
-- Principal (29) 1-500; Naco (41) 501-999. Los e-NCF no se repiten entre sucursales.
INSERT INTO dbo.SecuenciaECFAsignacion (
    IdSecuencia, IdEmpresa, TipoEcfDgii, IdSucursal,
    SecuenciaInicial, SecuenciaFinal, SecuenciaActual, Activo, FechaCreacion
)
SELECT
    s.IdSecuencia, @IdEmpresa, s.TipoEcfDgii, suc.IdSucursal,
    suc.Ini, suc.Fin, suc.Ini, 1, @Ahora
FROM dbo.SecuenciasECF s
CROSS JOIN (
    SELECT 29 AS IdSucursal, 1 AS Ini, 500 AS Fin
    UNION ALL SELECT 41, 501, 999
) suc
WHERE s.IdEmpresa = @IdEmpresa
  AND s.TipoEcfDgii IN (31, 32, 33, 34)
  AND EXISTS (
        SELECT 1 FROM dbo.Sucursal x
        WHERE x.IdSucursal = suc.IdSucursal AND x.IdEmpresa = @IdEmpresa AND x.Activa = 1
    )
  AND NOT EXISTS (
        SELECT 1 FROM dbo.SecuenciaECFAsignacion a
        WHERE a.IdEmpresa = @IdEmpresa
          AND a.TipoEcfDgii = s.TipoEcfDgii
          AND a.IdSucursal = suc.IdSucursal
          AND a.Activo = 1
    );

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, RNC, EsEmisorElectronico, AmbienteFE, ProveedorFE,
       ProveedorFE_Nombre, EstadoFE
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SELECT FiscalActivo, FacturacionElectronicaActiva, RazonSocial
FROM dbo.DgiiConfiguracionEmpresa
WHERE IdEmpresa = @IdEmpresa;

SELECT Clave, Valor, Activo
FROM dbo.Parametros
WHERE IdEmpresa = @IdEmpresa AND Clave IN (N'FACTURACION_ELECTRONICA', N'PREVIEW_DGII');

SELECT m.Codigo, em.Activo
FROM dbo.Empresa_Modulos em
JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo LIKE N'FE_%'
ORDER BY m.Codigo;

SELECT p.Nombre AS Perfil, m.Codigo, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
  AND m.Codigo LIKE N'FE_%'
ORDER BY p.Nombre, m.Codigo;

SELECT TipoNCF, Serie, TipoEcfDgii, SecuenciaInicial, SecuenciaActual, SecuenciaFinal,
       Activo, Ambiente, NumeroResolucion, Descripcion
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa
ORDER BY ISNULL(TipoEcfDgii, 0), IdSecuencia;

SELECT a.TipoEcfDgii, suc.Nombre AS Sucursal, a.SecuenciaInicial, a.SecuenciaActual, a.SecuenciaFinal, a.Activo
FROM dbo.SecuenciaECFAsignacion a
JOIN dbo.Sucursal suc ON suc.IdSucursal = a.IdSucursal
WHERE a.IdEmpresa = @IdEmpresa
ORDER BY a.TipoEcfDgii, a.IdSucursal;
GO
