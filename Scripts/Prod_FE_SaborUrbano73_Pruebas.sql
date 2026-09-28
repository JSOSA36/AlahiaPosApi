-- Facturación electrónica de prueba para Sabor Urbano (empresa 73).
-- Mismo suplidor que Terraza 55 (PG.eInvoicing sandbox). Secuencias E31-E34 PRUEBAS.
-- PerfilRoles solo Administrador. Autorizado por el usuario en producción.
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

DECLARE @IdEmpresa INT = 73;
DECLARE @IdFuente INT = 55;
DECLARE @Venc DATETIME = '2099-12-31';
DECLARE @Ahora DATETIME = GETDATE();

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa
      AND NombreComercial = N'Sabor Urbano'
)
BEGIN
    RAISERROR(N'No se encontró Sabor Urbano (empresa 73) en Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdFuente
      AND ProveedorFE = N'PROVEEDOR_EXTERNO'
      AND ISNULL(ProveedorFE_ApiKey, N'') <> N''
)
BEGIN
    RAISERROR(N'Terraza 55 no tiene suplidor externo configurado.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

-- 1) Empresa: mismo proveedor que Terraza, ambiente de pruebas.
UPDATE dest
SET dest.EsEmisorElectronico = 1,
    dest.AmbienteFE = N'testecf',
    dest.ProveedorFE = src.ProveedorFE,
    dest.ProveedorFE_Nombre = src.ProveedorFE_Nombre,
    dest.ProveedorFE_BaseUrl = src.ProveedorFE_BaseUrl,
    dest.ProveedorFE_ApiKey = src.ProveedorFE_ApiKey,
    dest.ProveedorFE_Usuario = src.ProveedorFE_Usuario,
    dest.ProveedorFE_Password = src.ProveedorFE_Password,
    dest.EstadoFE = N'Activo',
    dest.FechaHabilitacionFE = ISNULL(dest.FechaHabilitacionFE, @Ahora)
FROM dbo.Empresas dest
INNER JOIN dbo.Empresas src ON src.IdEmpresa = @IdFuente
WHERE dest.IdEmpresa = @IdEmpresa;

-- 2) Flags fiscales.
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

-- 3) Parámetros POS.
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

-- 4) Licencia módulos FE (empresa).
INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, @Ahora
FROM dbo.Modulos m
WHERE m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
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
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
    );

-- 5) PerfilRoles solo Administrador (no cajeros).
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
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
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
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
    );

-- 6) Secuencias e-CF de prueba (1-999).
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

COMMIT TRAN;

SELECT IdEmpresa, NombreComercial, RNC, EsEmisorElectronico, AmbienteFE, ProveedorFE,
       ProveedorFE_Nombre, ProveedorFE_BaseUrl,
       CASE WHEN LEN(ISNULL(ProveedorFE_ApiKey, N'')) > 0 THEN N'SI' ELSE N'NO' END AS ApiKey,
       EstadoFE
FROM dbo.Empresas
WHERE IdEmpresa = @IdEmpresa;

SELECT FiscalActivo, FacturacionElectronicaActiva, RazonSocial
FROM dbo.DgiiConfiguracionEmpresa
WHERE IdEmpresa = @IdEmpresa;

SELECT m.Codigo, em.Activo
FROM dbo.Empresa_Modulos em
JOIN dbo.Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa
  AND m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
    )
ORDER BY m.Codigo;

SELECT p.Nombre AS Perfil, m.Codigo, pr.Activo
FROM dbo.PerfilRoles pr
JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
JOIN dbo.Modulos m ON m.Id = pr.IdModulo
WHERE pr.IdEmpresa = @IdEmpresa
  AND m.Codigo IN (
        N'FE_CONFIGURACION', N'FE_SECUENCIAS', N'FE_CERTIFICADO',
        N'FE_ESTADO_DGII', N'FE_HISTORIAL', N'FE_MONITOREO', N'FE_REPROCESAR',
        N'NCF_SECUENCIAS'
    )
ORDER BY m.Codigo;

SELECT TipoNCF, Serie, TipoEcfDgii, SecuenciaInicial, SecuenciaActual, SecuenciaFinal,
       Activo, Ambiente, NumeroResolucion, Descripcion
FROM dbo.SecuenciasECF
WHERE IdEmpresa = @IdEmpresa
ORDER BY ISNULL(TipoEcfDgii, 0), IdSecuencia;
GO
