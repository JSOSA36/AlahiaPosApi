-- AlahiaPos_Dev. Clínica Dental Dra Sena (60) → Alahia.eCF.Api (DGII_DIRECTO).
-- No cambia RNC ni certificado; solo proveedor FE.
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

DECLARE @IdEmpresa INT = (
    SELECT TOP 1 IdEmpresa
    FROM dbo.Empresas
    WHERE IdEmpresa = 60
      AND (NombreComercial LIKE N'%Sena%' OR NombreComercial LIKE N'%Dental%')
);

IF @IdEmpresa IS NULL
BEGIN
    RAISERROR(N'No se encontró Clínica Dental Dra Sena (IdEmpresa 60) en Dev.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.CertificadoDigital
    WHERE IdEmpresa = @IdEmpresa AND Activo = 1
      AND ArchivoBytes IS NOT NULL AND DATALENGTH(ArchivoBytes) > 0
)
BEGIN
    RAISERROR(N'Sena (60) no tiene CertificadoDigital activo con .p12 en Dev.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE dbo.Empresas
SET EsEmisorElectronico = 1,
    ProveedorFE = N'DGII_DIRECTO',
    ProveedorFE_Nombre = N'Alahia.eCF.Api',
    EstadoFE = COALESCE(NULLIF(EstadoFE, N''), N'Activo'),
    FechaHabilitacionFE = ISNULL(FechaHabilitacionFE, GETDATE())
WHERE IdEmpresa = @IdEmpresa;

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
            N'Emisión de comprobantes fiscales electrónicos', GETDATE(), 1);

-- Licencia pedida: movimiento financiero + método de pago (solo Admin, solo esta empresa).
DECLARE @Codigos TABLE (Codigo NVARCHAR(80) PRIMARY KEY);
INSERT INTO @Codigos (Codigo) VALUES
    (N'MOVIMIENTO_FINANCIERO'),
    (N'METODO_PAGO_CUENTAS');

INSERT INTO dbo.Empresa_Modulos (EmpresaId, ModuloId, Activo, FechaActivacion)
SELECT @IdEmpresa, m.Id, 1, GETDATE()
FROM dbo.Modulos m
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Empresa_Modulos em
    WHERE em.EmpresaId = @IdEmpresa AND em.ModuloId = m.Id
);

UPDATE em
SET em.Activo = 1, em.FechaDesactivacion = NULL
FROM dbo.Empresa_Modulos em
INNER JOIN dbo.Modulos m ON m.Id = em.ModuloId
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE em.EmpresaId = @IdEmpresa;

INSERT INTO dbo.PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT p.IdPerfil, m.Id, 1, GETDATE(), p.IdEmpresa
FROM dbo.Perfiles p
INNER JOIN dbo.Modulos m ON 1 = 1
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE p.IdEmpresa = @IdEmpresa
  AND p.Activo = 1
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PerfilRoles pr
      WHERE pr.IdPerfil = p.IdPerfil AND pr.IdModulo = m.Id AND pr.IdEmpresa = p.IdEmpresa
  );

UPDATE pr
SET pr.Activo = 1
FROM dbo.PerfilRoles pr
INNER JOIN dbo.Perfiles p ON p.IdPerfil = pr.IdPerfil
INNER JOIN dbo.Modulos m ON m.Id = pr.IdModulo
INNER JOIN @Codigos c ON c.Codigo = m.Codigo
WHERE p.IdEmpresa = @IdEmpresa
  AND (p.Nombre = N'Administrador' OR p.Nombre LIKE N'%Administrador%');

COMMIT TRAN;

SELECT e.IdEmpresa, e.NombreComercial, e.ProveedorFE, e.ProveedorFE_Nombre,
       e.AmbienteFE, e.EsEmisorElectronico, e.EstadoFE
FROM dbo.Empresas e
WHERE e.IdEmpresa = @IdEmpresa;

SELECT TOP 1 c.IdCertificado, c.NombreArchivo, c.Activo, c.FechaExpiracion, c.Ambiente,
       DATALENGTH(c.ArchivoBytes) AS Bytes, LEN(c.PasswordEncriptado) AS PasswordLen
FROM dbo.CertificadoDigital c
WHERE c.IdEmpresa = @IdEmpresa AND c.Activo = 1
ORDER BY c.FechaCreacion DESC;
GO
