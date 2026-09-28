-- ============================================================
-- ARS (Administradoras de Riesgos de Salud) — AlahiaPos_Dev
-- Maestro + cobertura en factura + cobros posteriores.
-- UTILIZAR_ARS queda en false para no cambiar empresas actuales.
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.ArsAseguradoras', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArsAseguradoras
    (
        IdArs INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArsAseguradoras PRIMARY KEY,
        IdEmpresa INT NOT NULL,
        Nombre NVARCHAR(150) NOT NULL,
        RNC NVARCHAR(20) NULL,
        Telefono NVARCHAR(30) NULL,
        Direccion NVARCHAR(250) NULL,
        Email NVARCHAR(120) NULL,
        Contacto NVARCHAR(120) NULL,
        Observaciones NVARCHAR(500) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ArsAseguradoras_Activo DEFAULT (1),
        FechaInseccion DATETIME NOT NULL CONSTRAINT DF_ArsAseguradoras_Fecha DEFAULT (GETDATE())
    );

    CREATE INDEX IX_ArsAseguradoras_Empresa
        ON dbo.ArsAseguradoras (IdEmpresa, Activo);
END;

IF COL_LENGTH('dbo.FacturaHeaders', 'IdArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IdArs INT NULL;

IF COL_LENGTH('dbo.FacturaHeaders', 'MontoCubiertoArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoCubiertoArs DECIMAL(18,2) NOT NULL CONSTRAINT DF_FacturaHeaders_MontoCubiertoArs DEFAULT (0);

IF COL_LENGTH('dbo.FacturaHeaders', 'PagadoArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD PagadoArs DECIMAL(18,2) NOT NULL CONSTRAINT DF_FacturaHeaders_PagadoArs DEFAULT (0);

IF COL_LENGTH('dbo.FacturaHeaders', 'PendienteArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD PendienteArs DECIMAL(18,2) NOT NULL CONSTRAINT DF_FacturaHeaders_PendienteArs DEFAULT (0);

IF COL_LENGTH('dbo.FacturaHeaders', 'EstadoArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD EstadoArs NVARCHAR(20) NULL;

IF COL_LENGTH('dbo.FacturaHeaders', 'FechaVencimientoArs') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD FechaVencimientoArs DATETIME NULL;

IF COL_LENGTH('dbo.PagosFacturasClientes', 'IdArs') IS NULL
    ALTER TABLE dbo.PagosFacturasClientes ADD IdArs INT NULL;

IF COL_LENGTH('dbo.PagosFacturasClientes', 'EsCoberturaArs') IS NULL
    ALTER TABLE dbo.PagosFacturasClientes ADD EsCoberturaArs BIT NOT NULL CONSTRAINT DF_PagosFacturasClientes_EsCoberturaArs DEFAULT (0);

DECLARE @Clave NVARCHAR(100) = N'UTILIZAR_ARS';
DECLARE @Descripcion NVARCHAR(300) =
    N'Utilizar ARS. Activado = permite cobertura de aseguradora y CxC a nombre de la ARS. Desactivado = el sistema funciona igual que ahora.';

INSERT INTO dbo.Parametros
    (IdEmpresa, Tipo, CodigoPOS, Clave, Valor, Descripcion, FechaCreacion, Activo)
SELECT
    e.IdEmpresa,
    N'EMPRESA',
    NULL,
    @Clave,
    N'false',
    @Descripcion,
    GETDATE(),
    1
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Parametros p
    WHERE p.IdEmpresa = e.IdEmpresa
      AND LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
      AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'')
);

UPDATE p
SET p.Descripcion = @Descripcion,
    p.Activo = 1
FROM dbo.Parametros p
WHERE LOWER(LTRIM(RTRIM(ISNULL(p.Clave, N'')))) = LOWER(@Clave)
  AND (p.CodigoPOS IS NULL OR p.CodigoPOS = N'');
