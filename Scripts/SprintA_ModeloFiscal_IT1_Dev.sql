-- =============================================================================
-- Sprint A — Modelo fiscal mínimo IT-1 / Anexo A
-- Base: AlahiaPos_Dev ÚNICAMENTE
-- NO tocar Prod. NO UI. NO motores Anexo A / IT-1.
-- Ajustes: forma venta mixta sin código único; TipoIngreso sin backfill=1;
--          Productos.TasaItbis solo default futuro.
-- =============================================================================
USE AlahiaPos_Dev;
GO

SET NOCOUNT ON;
GO

PRINT '=== Sprint A: inicio ===';
GO

-- -----------------------------------------------------------------------------
-- 1) DgiiCatalogo
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DgiiCatalogo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DgiiCatalogo (
        IdDgiiCatalogo        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DgiiCatalogo PRIMARY KEY,
        TipoCatalogo          NVARCHAR(40)  NOT NULL,
        CodigoDGII            NVARCHAR(20)  NOT NULL,
        Descripcion           NVARCHAR(200) NOT NULL,
        FechaInicioVigencia   DATE          NOT NULL CONSTRAINT DF_DgiiCatalogo_Ini DEFAULT ('2020-01-01'),
        FechaFinVigencia      DATE          NULL,
        Activo                BIT           NOT NULL CONSTRAINT DF_DgiiCatalogo_Activo DEFAULT (1),
        VersionInstructivo    NVARCHAR(20)  NOT NULL CONSTRAINT DF_DgiiCatalogo_Ver DEFAULT (N'IT-1-2020'),
        Orden                 INT           NOT NULL CONSTRAINT DF_DgiiCatalogo_Orden DEFAULT (0),
        MetaJson              NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX UX_DgiiCatalogo_TipoCodigoIni
        ON dbo.DgiiCatalogo (TipoCatalogo, CodigoDGII, FechaInicioVigencia);
    PRINT 'Creada DgiiCatalogo';
END
ELSE
    PRINT 'DgiiCatalogo ya existe';
GO

-- -----------------------------------------------------------------------------
-- 2) DgiiConfiguracionEmpresa
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DgiiConfiguracionEmpresa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DgiiConfiguracionEmpresa (
        IdEmpresa                     INT           NOT NULL CONSTRAINT PK_DgiiConfiguracionEmpresa PRIMARY KEY,
        RegimenTributarioCodigo       NVARCHAR(20)  NOT NULL CONSTRAINT DF_DgiiCfg_Regimen DEFAULT (N'ORDINARIO'),
        EsConstructor                 BIT           NOT NULL CONSTRAINT DF_DgiiCfg_Const DEFAULT (0),
        EsComisionista                BIT           NOT NULL CONSTRAINT DF_DgiiCfg_Comis DEFAULT (0),
        ObligadoLibroVentasSF         BIT           NOT NULL CONSTRAINT DF_DgiiCfg_SF DEFAULT (0),
        RazonSocial                   NVARCHAR(200) NULL,
        DeclaranteNombre              NVARCHAR(150) NULL,
        DeclaranteCalidad             NVARCHAR(80)  NULL,
        VersionInstructivoPreferida   NVARCHAR(20)  NOT NULL CONSTRAINT DF_DgiiCfg_Ver DEFAULT (N'IT-1-2020'),
        Activo                        BIT           NOT NULL CONSTRAINT DF_DgiiCfg_Activo DEFAULT (1),
        FechaCreacion                 DATETIME      NOT NULL CONSTRAINT DF_DgiiCfg_Fec DEFAULT (GETDATE()),
        CONSTRAINT FK_DgiiCfg_Empresa FOREIGN KEY (IdEmpresa) REFERENCES dbo.Empresas (IdEmpresa)
    );
    PRINT 'Creada DgiiConfiguracionEmpresa';
END
ELSE
    PRINT 'DgiiConfiguracionEmpresa ya existe';
GO

-- -----------------------------------------------------------------------------
-- Helper: ADD COLUMN if missing
-- -----------------------------------------------------------------------------
-- (inline IF COL_LENGTH checks below)

-- -----------------------------------------------------------------------------
-- 3) FacturaHeaders — fotografía fiscal
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.FacturaHeaders', 'CodigoTipoComprobanteDgii') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD CodigoTipoComprobanteDgii NVARCHAR(2) NULL;
IF COL_LENGTH('dbo.FacturaHeaders', 'TipoIngresoDgii') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD TipoIngresoDgii TINYINT NULL; -- histórico NULL; nuevos ops default app=1
IF COL_LENGTH('dbo.FacturaHeaders', 'IndicadorFacturacion') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD IndicadorFacturacion TINYINT NULL;
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoGravado') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoGravado DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoGravado DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoExento') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoExento DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoExento DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoGravadoI1') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoGravadoI1 DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoGravadoI1 DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoGravadoI2') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoGravadoI2 DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoGravadoI2 DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoGravadoI3') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoGravadoI3 DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoGravadoI3 DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoGravadoI4') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoGravadoI4 DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_MontoGravadoI4 DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'DescuentoAfectaBase') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD DescuentoAfectaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_DescAfectaBase DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'MontoPropinaLegal') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD MontoPropinaLegal DECIMAL(18,2) NOT NULL CONSTRAINT DF_FH_PropinaLegal DEFAULT (0);
-- Solo certeza (crédito=15). Contado / mixto = NULL; motor usa montos por medio.
IF COL_LENGTH('dbo.FacturaHeaders', 'FormaVentaFiscalDgii') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD FormaVentaFiscalDgii TINYINT NULL;
IF COL_LENGTH('dbo.FacturaHeaders', 'RegimenFiscalClienteCodigo') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD RegimenFiscalClienteCodigo NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.FacturaHeaders', 'TasaItbisPrincipal') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD TasaItbisPrincipal DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.FacturaHeaders', 'FotografiaFiscalVersion') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD FotografiaFiscalVersion INT NOT NULL CONSTRAINT DF_FH_FotoVer DEFAULT (0);
IF COL_LENGTH('dbo.FacturaHeaders', 'FechaFotografiaFiscal') IS NULL
    ALTER TABLE dbo.FacturaHeaders ADD FechaFotografiaFiscal DATETIME NULL;
PRINT 'FacturaHeaders: columnas fiscales OK';
GO

-- -----------------------------------------------------------------------------
-- 4) FacturaDetalles
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.FacturaDetalles', 'TasaItbis') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD TasaItbis DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.FacturaDetalles', 'IndicadorFacturacion') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD IndicadorFacturacion TINYINT NULL;
IF COL_LENGTH('dbo.FacturaDetalles', 'MontoGravadoLinea') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD MontoGravadoLinea DECIMAL(18,2) NOT NULL CONSTRAINT DF_FD_MontoGravado DEFAULT (0);
IF COL_LENGTH('dbo.FacturaDetalles', 'MontoExentoLinea') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD MontoExentoLinea DECIMAL(18,2) NOT NULL CONSTRAINT DF_FD_MontoExento DEFAULT (0);
IF COL_LENGTH('dbo.FacturaDetalles', 'DescuentoAfectaBase') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD DescuentoAfectaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_FD_Desc DEFAULT (0);
IF COL_LENGTH('dbo.FacturaDetalles', 'ItbisCalculado') IS NULL
    ALTER TABLE dbo.FacturaDetalles ADD ItbisCalculado DECIMAL(18,2) NOT NULL CONSTRAINT DF_FD_ItbisCalc DEFAULT (0);
PRINT 'FacturaDetalles: columnas fiscales OK';
GO

-- -----------------------------------------------------------------------------
-- 5) NotasCredito
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.NotasCredito', 'CodigoTipoComprobanteDgii') IS NULL
    ALTER TABLE dbo.NotasCredito ADD CodigoTipoComprobanteDgii NVARCHAR(2) NULL;
IF COL_LENGTH('dbo.NotasCredito', 'FechaFacturaOrigen') IS NULL
    ALTER TABLE dbo.NotasCredito ADD FechaFacturaOrigen DATETIME NULL;
IF COL_LENGTH('dbo.NotasCredito', 'MontoGravado') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoGravado DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_MontoGravado DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'MontoExento') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoExento DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_MontoExento DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'MontoGravadoI1') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoGravadoI1 DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_I1 DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'MontoGravadoI2') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoGravadoI2 DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_I2 DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'MontoGravadoI3') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoGravadoI3 DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_I3 DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'MontoGravadoI4') IS NULL
    ALTER TABLE dbo.NotasCredito ADD MontoGravadoI4 DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_I4 DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'DescuentoAfectaBase') IS NULL
    ALTER TABLE dbo.NotasCredito ADD DescuentoAfectaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_NC_Desc DEFAULT (0);
IF COL_LENGTH('dbo.NotasCredito', 'TasaItbisPrincipal') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TasaItbisPrincipal DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.NotasCredito', 'TipoIngresoDgii') IS NULL
    ALTER TABLE dbo.NotasCredito ADD TipoIngresoDgii TINYINT NULL; -- sin backfill a 1
IF COL_LENGTH('dbo.NotasCredito', 'FotografiaFiscalVersion') IS NULL
    ALTER TABLE dbo.NotasCredito ADD FotografiaFiscalVersion INT NOT NULL CONSTRAINT DF_NC_FotoVer DEFAULT (0);
PRINT 'NotasCredito: columnas fiscales OK';
GO

-- -----------------------------------------------------------------------------
-- 6) NotasCreditoDetalle
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.NotasCreditoDetalle', 'TasaItbis') IS NULL
    ALTER TABLE dbo.NotasCreditoDetalle ADD TasaItbis DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.NotasCreditoDetalle', 'MontoGravadoLinea') IS NULL
    ALTER TABLE dbo.NotasCreditoDetalle ADD MontoGravadoLinea DECIMAL(18,2) NOT NULL CONSTRAINT DF_NCD_Grav DEFAULT (0);
IF COL_LENGTH('dbo.NotasCreditoDetalle', 'MontoExentoLinea') IS NULL
    ALTER TABLE dbo.NotasCreditoDetalle ADD MontoExentoLinea DECIMAL(18,2) NOT NULL CONSTRAINT DF_NCD_Exe DEFAULT (0);
IF COL_LENGTH('dbo.NotasCreditoDetalle', 'DescuentoAfectaBase') IS NULL
    ALTER TABLE dbo.NotasCreditoDetalle ADD DescuentoAfectaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_NCD_Desc DEFAULT (0);
IF COL_LENGTH('dbo.NotasCreditoDetalle', 'ItbisCalculado') IS NULL
    ALTER TABLE dbo.NotasCreditoDetalle ADD ItbisCalculado DECIMAL(18,2) NOT NULL CONSTRAINT DF_NCD_Itbis DEFAULT (0);
PRINT 'NotasCreditoDetalle: columnas fiscales OK';
GO

-- -----------------------------------------------------------------------------
-- 7) OrdenCompraHeaders — destino pendiente + foto (606 intacto)
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'DestinoItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD DestinoItbis TINYINT NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'DestinoItbisSugerido') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD DestinoItbisSugerido TINYINT NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'EstadoClasificacionItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD EstadoClasificacionItbis NVARCHAR(30) NOT NULL CONSTRAINT DF_OCH_EstClas DEFAULT (N'NO_APLICA');
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ClasificacionConfirmada') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ClasificacionConfirmada BIT NOT NULL CONSTRAINT DF_OCH_ClasConf DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FechaClasificacion') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD FechaClasificacion DATETIME NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'IdUsuarioClasificacion') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD IdUsuarioClasificacion INT NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisComprasLocales') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisComprasLocales DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisLoc DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisServicios') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisServicios DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisServ DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisImportaciones') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisImportaciones DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisImp DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'TasaItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD TasaItbis DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'CodigoNormaRetencionItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD CodigoNormaRetencionItbis NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'BaseRetencionItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD BaseRetencionItbis DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_BaseRet DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'RegimenFiscalProveedorCodigo') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD RegimenFiscalProveedorCodigo NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'EsImportacion') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD EsImportacion BIT NOT NULL CONSTRAINT DF_OCH_EsImp DEFAULT (0);
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FotografiaFiscalVersion') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD FotografiaFiscalVersion INT NOT NULL CONSTRAINT DF_OCH_FotoVer DEFAULT (0);
PRINT 'OrdenCompraHeaders: columnas fiscales OK (606 existente intacto)';
GO

-- -----------------------------------------------------------------------------
-- 8) OrdenCompraDetalles
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.OrdenCompraDetalles', 'TasaItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraDetalles ADD TasaItbis DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.OrdenCompraDetalles', 'DestinoItbis') IS NULL
    ALTER TABLE dbo.OrdenCompraDetalles ADD DestinoItbis TINYINT NULL;
IF COL_LENGTH('dbo.OrdenCompraDetalles', 'ItbisCalculado') IS NULL
    ALTER TABLE dbo.OrdenCompraDetalles ADD ItbisCalculado DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCD_ItbisCalc DEFAULT (0);
PRINT 'OrdenCompraDetalles: columnas fiscales OK';
GO

-- -----------------------------------------------------------------------------
-- 9) Productos — SOLO defaults futuros (nunca fuente motor / histórico)
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.Productos', 'TasaItbis') IS NULL
    ALTER TABLE dbo.Productos ADD TasaItbis DECIMAL(5,2) NULL; -- null = usar ParametrosConfigs
IF COL_LENGTH('dbo.Productos', 'TipoIngresoDgiiDefault') IS NULL
    ALTER TABLE dbo.Productos ADD TipoIngresoDgiiDefault TINYINT NULL;
IF COL_LENGTH('dbo.Productos', 'CodigoExencionDgii') IS NULL
    ALTER TABLE dbo.Productos ADD CodigoExencionDgii NVARCHAR(20) NULL;
PRINT 'Productos: defaults fiscales OK (no recalcula histórico)';
GO

-- -----------------------------------------------------------------------------
-- 10) Proveedores / Clientes — defaults futuros
-- -----------------------------------------------------------------------------
IF COL_LENGTH('dbo.Proveedores', 'RegimenDgii') IS NULL
    ALTER TABLE dbo.Proveedores ADD RegimenDgii NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Proveedores', 'TipoIdentificacionDgii') IS NULL
    ALTER TABLE dbo.Proveedores ADD TipoIdentificacionDgii TINYINT NULL;
IF COL_LENGTH('dbo.Proveedores', 'ClasificacionRetencionItbisDefault') IS NULL
    ALTER TABLE dbo.Proveedores ADD ClasificacionRetencionItbisDefault NVARCHAR(20) NULL;

IF COL_LENGTH('dbo.Clientes', 'RegimenDgii') IS NULL
    ALTER TABLE dbo.Clientes ADD RegimenDgii NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Clientes', 'EsRegimenEspecial') IS NULL
    ALTER TABLE dbo.Clientes ADD EsRegimenEspecial BIT NOT NULL CONSTRAINT DF_Cli_RegEsp DEFAULT (0);
IF COL_LENGTH('dbo.Clientes', 'TipoIdentificacionDgii') IS NULL
    ALTER TABLE dbo.Clientes ADD TipoIdentificacionDgii TINYINT NULL;
PRINT 'Proveedores/Clientes: defaults OK';
GO

-- =============================================================================
-- SEEDS catálogo
-- =============================================================================
;WITH S(TipoCatalogo, CodigoDGII, Descripcion, Orden) AS (
    SELECT * FROM (VALUES
    -- Tipo comprobante
    (N'TIPO_COMPROBANTE', N'01', N'Crédito fiscal', 1),
    (N'TIPO_COMPROBANTE', N'02', N'Consumo', 2),
    (N'TIPO_COMPROBANTE', N'03', N'Nota de débito', 3),
    (N'TIPO_COMPROBANTE', N'04', N'Nota de crédito', 4),
    (N'TIPO_COMPROBANTE', N'12', N'Registro único de ingresos', 5),
    (N'TIPO_COMPROBANTE', N'14', N'Regímenes especiales', 6),
    (N'TIPO_COMPROBANTE', N'15', N'Gubernamental', 7),
    (N'TIPO_COMPROBANTE', N'16', N'Exportación', 8),
    (N'TIPO_COMPROBANTE', N'31', N'e-CF Crédito fiscal', 31),
    (N'TIPO_COMPROBANTE', N'32', N'e-CF Consumo', 32),
    (N'TIPO_COMPROBANTE', N'33', N'e-CF Nota de débito', 33),
    (N'TIPO_COMPROBANTE', N'34', N'e-CF Nota de crédito', 34),
    (N'TIPO_COMPROBANTE', N'44', N'e-CF Regímenes especiales', 44),
    (N'TIPO_COMPROBANTE', N'45', N'e-CF Gubernamental', 45),
    (N'TIPO_COMPROBANTE', N'46', N'e-CF Exportación', 46),
    -- Destino ITBIS Anexo A
    (N'DESTINO_ITBIS', N'1', N'No deducible - productores bienes/servicios exentos', 1),
    (N'DESTINO_ITBIS', N'2', N'No deducible - activo categoría I', 2),
    (N'DESTINO_ITBIS', N'3', N'No deducible - otros', 3),
    (N'DESTINO_ITBIS', N'4', N'Deducible - producción/venta exportación', 4),
    (N'DESTINO_ITBIS', N'5', N'Deducible - bienes gravados', 5),
    (N'DESTINO_ITBIS', N'6', N'Deducible - servicios gravados', 6),
    (N'DESTINO_ITBIS', N'7', N'Sujeto a proporcionalidad Art. 349', 7),
    -- Estado clasificación
    (N'ESTADO_CLASIFICACION_ITBIS', N'NO_APLICA', N'Sin ITBIS', 1),
    (N'ESTADO_CLASIFICACION_ITBIS', N'PENDIENTE_VALIDAR', N'Sugerido / pendiente confirmación', 2),
    (N'ESTADO_CLASIFICACION_ITBIS', N'CONFIRMADO', N'Confirmado fiscalmente', 3),
    -- Tipo ingreso Anexo A
    (N'TIPO_INGRESO', N'1', N'Operaciones (no financieros)', 1),
    (N'TIPO_INGRESO', N'2', N'Financieros', 2),
    (N'TIPO_INGRESO', N'3', N'Extraordinarios', 3),
    (N'TIPO_INGRESO', N'4', N'Arrendamientos', 4),
    (N'TIPO_INGRESO', N'5', N'Venta activos depreciables', 5),
    (N'TIPO_INGRESO', N'6', N'Otros ingresos', 6),
    -- Forma venta (códigos casilla; uso solo si certeza — no mixto)
    (N'FORMA_VENTA_FISCAL', N'12', N'Efectivo', 12),
    (N'FORMA_VENTA_FISCAL', N'13', N'Cheque / Transferencia', 13),
    (N'FORMA_VENTA_FISCAL', N'14', N'Tarjeta', 14),
    (N'FORMA_VENTA_FISCAL', N'15', N'A crédito', 15),
    (N'FORMA_VENTA_FISCAL', N'16', N'Bonos / certificado regalo', 16),
    (N'FORMA_VENTA_FISCAL', N'17', N'Permutas', 17),
    (N'FORMA_VENTA_FISCAL', N'18', N'Otras formas', 18),
    -- Régimen
    (N'REGIMEN', N'ORDINARIO', N'Régimen ordinario', 1),
    (N'REGIMEN', N'RST', N'Régimen simplificado', 2),
    (N'REGIMEN', N'ESPECIAL', N'Régimen especial', 3),
    -- Normas retención ITBIS
    (N'NORMA_RET_ITBIS', N'08-04', N'Retención tarjeta Norma 08-04', 1),
    (N'NORMA_RET_ITBIS', N'02-05', N'Norma 02-05', 2),
    (N'NORMA_RET_ITBIS', N'07-09', N'Norma 07-09 sociedades', 3),
    (N'NORMA_RET_ITBIS', N'01-11', N'Norma 01-11 ISFL', 4),
    (N'NORMA_RET_ITBIS', N'07-07', N'Norma 07-07 constructores', 5),
    (N'NORMA_RET_ITBIS', N'RST', N'Retención RST', 6),
    (N'NORMA_RET_ITBIS', N'COMP_COMPRAS', N'Comprobante de compras', 7),
    -- Tasas
    (N'TASA_ITBIS', N'18', N'Tasa 18%', 18),
    (N'TASA_ITBIS', N'16', N'Tasa 16%', 16),
    (N'TASA_ITBIS', N'9', N'Tasa 9% Ley 690-16', 9),
    (N'TASA_ITBIS', N'8', N'Tasa 8% Ley 690-16', 8),
    (N'TASA_ITBIS', N'0', N'Exento / 0%', 0)
    ) v(TipoCatalogo, CodigoDGII, Descripcion, Orden)
)
INSERT INTO dbo.DgiiCatalogo (TipoCatalogo, CodigoDGII, Descripcion, FechaInicioVigencia, Activo, VersionInstructivo, Orden)
SELECT s.TipoCatalogo, s.CodigoDGII, s.Descripcion, '2020-01-01', 1, N'IT-1-2020', s.Orden
FROM S s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DgiiCatalogo c
    WHERE c.TipoCatalogo = s.TipoCatalogo
      AND c.CodigoDGII = s.CodigoDGII
      AND c.FechaInicioVigencia = '2020-01-01'
);
PRINT 'Seed DgiiCatalogo OK';
GO

-- Config por empresa
INSERT INTO dbo.DgiiConfiguracionEmpresa (IdEmpresa, RegimenTributarioCodigo, RazonSocial)
SELECT e.IdEmpresa, N'ORDINARIO', e.NombreComercial
FROM dbo.Empresas e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DgiiConfiguracionEmpresa c WHERE c.IdEmpresa = e.IdEmpresa
);
PRINT 'Seed DgiiConfiguracionEmpresa OK';
GO

-- =============================================================================
-- BACKFILL (estimado version=0; sin TipoIngreso=1; destino NO confirmado)
-- =============================================================================

-- FacturaDetalles: foto línea estimada
UPDATE d
SET
    d.ItbisCalculado = CASE WHEN d.ItbisCalculado = 0 THEN d.Itbis ELSE d.ItbisCalculado END,
    d.MontoGravadoLinea = CASE
        WHEN d.MontoGravadoLinea = 0 AND d.Itbis <> 0 THEN d.SubTotal
        WHEN d.MontoGravadoLinea = 0 AND d.Itbis = 0 THEN 0
        ELSE d.MontoGravadoLinea END,
    d.MontoExentoLinea = CASE
        WHEN d.MontoExentoLinea = 0 AND d.Itbis = 0 AND d.SubTotal <> 0 THEN d.SubTotal
        ELSE d.MontoExentoLinea END,
    d.DescuentoAfectaBase = CASE WHEN d.DescuentoAfectaBase = 0 THEN ISNULL(d.Descuento, 0) ELSE d.DescuentoAfectaBase END
FROM dbo.FacturaDetalles d;
PRINT 'Backfill FacturaDetalles OK';
GO

-- FacturaHeaders
UPDATE h
SET
    h.MontoPropinaLegal = CASE WHEN h.MontoPropinaLegal = 0 THEN ISNULL(h.MontoPropina, 0) ELSE h.MontoPropinaLegal END,
    h.DescuentoAfectaBase = CASE WHEN h.DescuentoAfectaBase = 0 THEN ISNULL(h.TotalDescuento, 0) ELSE h.DescuentoAfectaBase END,
    h.MontoGravado = CASE
        WHEN h.MontoGravado = 0 AND h.TotalItbis <> 0 THEN ISNULL(h.SubTotal, 0)
        ELSE h.MontoGravado END,
    h.MontoExento = CASE
        WHEN h.MontoExento = 0 AND h.TotalItbis = 0 THEN ISNULL(h.SubTotal, 0)
        ELSE h.MontoExento END,
    h.MontoGravadoI1 = CASE
        WHEN h.MontoGravadoI1 = 0 AND h.TotalItbis <> 0 THEN ISNULL(h.SubTotal, 0)
        ELSE h.MontoGravadoI1 END,
    -- Tipo NCF desde prefijo (refinado en UPDATE siguiente para e-CF)
    h.CodigoTipoComprobanteDgii = COALESCE(
        h.CodigoTipoComprobanteDgii,
        CASE
            WHEN h.NCF LIKE N'B01%' THEN N'01'
            WHEN h.NCF LIKE N'B02%' THEN N'02'
            WHEN h.NCF LIKE N'B03%' THEN N'03'
            WHEN h.NCF LIKE N'B04%' THEN N'04'
            WHEN h.NCF LIKE N'B12%' THEN N'12'
            WHEN h.NCF LIKE N'B14%' THEN N'14'
            WHEN h.NCF LIKE N'B15%' THEN N'15'
            WHEN h.NCF LIKE N'B16%' THEN N'16'
            ELSE NULL
        END
    ),
    -- Forma venta: SOLO crédito con certeza. Contado/mixto = NULL (montos son la verdad).
    h.FormaVentaFiscalDgii = CASE
        WHEN h.FormaVentaFiscalDgii IS NOT NULL THEN h.FormaVentaFiscalDgii
        WHEN UPPER(REPLACE(ISNULL(h.TipoFactura, N''), N'É', N'E')) LIKE N'%CREDITO%' THEN 15
        ELSE NULL
    END
    -- TipoIngresoDgii: NO asignar en histórico (queda NULL)
FROM dbo.FacturaHeaders h;
PRINT 'Backfill FacturaHeaders OK (TipoIngreso NULL; FormaVenta solo crédito)';
GO

-- Corrección B01 con e-CF: ya aplicado arriba en segundo SET — consolidar en un solo update fue duplicado.
-- Re-aplicar e-CF codes limpios:
UPDATE dbo.FacturaHeaders
SET CodigoTipoComprobanteDgii = CASE
    WHEN NCF LIKE N'E31%' THEN N'31'
    WHEN NCF LIKE N'E32%' THEN N'32'
    WHEN NCF LIKE N'E33%' THEN N'33'
    WHEN NCF LIKE N'E34%' THEN N'34'
    WHEN NCF LIKE N'E44%' THEN N'44'
    WHEN NCF LIKE N'E45%' THEN N'45'
    WHEN NCF LIKE N'E46%' THEN N'46'
    WHEN NCF LIKE N'B01%' THEN N'01'
    WHEN NCF LIKE N'B02%' THEN N'02'
    WHEN NCF LIKE N'B03%' THEN N'03'
    WHEN NCF LIKE N'B04%' THEN N'04'
    WHEN NCF LIKE N'B12%' THEN N'12'
    WHEN NCF LIKE N'B14%' THEN N'14'
    WHEN NCF LIKE N'B15%' THEN N'15'
    WHEN NCF LIKE N'B16%' THEN N'16'
    ELSE CodigoTipoComprobanteDgii
END;
GO

-- NotasCredito
UPDATE nc
SET
    nc.FechaFacturaOrigen = COALESCE(nc.FechaFacturaOrigen, fh.FechaInseccion),
    nc.CodigoTipoComprobanteDgii = COALESCE(
        nc.CodigoTipoComprobanteDgii,
        CASE
            WHEN nc.NCF LIKE N'E34%' THEN N'34'
            WHEN nc.NCF LIKE N'B04%' THEN N'04'
            WHEN nc.NCF LIKE N'34%' THEN N'34'
            ELSE N'04'
        END
    ),
    nc.MontoGravado = CASE WHEN nc.MontoGravado = 0 AND nc.TotalItbis <> 0 THEN ISNULL(nc.SubTotal, 0) ELSE nc.MontoGravado END,
    nc.MontoExento = CASE WHEN nc.MontoExento = 0 AND nc.TotalItbis = 0 THEN ISNULL(nc.SubTotal, 0) ELSE nc.MontoExento END,
    nc.MontoGravadoI1 = CASE WHEN nc.MontoGravadoI1 = 0 AND nc.TotalItbis <> 0 THEN ISNULL(nc.SubTotal, 0) ELSE nc.MontoGravadoI1 END,
    nc.FotografiaFiscalVersion = 0
FROM dbo.NotasCredito nc
LEFT JOIN dbo.FacturaHeaders fh ON fh.IdFacturaHeader = nc.IdFacturaHeader;
PRINT 'Backfill NotasCredito OK';
GO

UPDATE ncd
SET
    ncd.ItbisCalculado = CASE WHEN ncd.ItbisCalculado = 0 THEN ncd.Itbis ELSE ncd.ItbisCalculado END,
    ncd.MontoGravadoLinea = CASE WHEN ncd.MontoGravadoLinea = 0 AND ncd.Itbis <> 0 THEN ncd.SubTotal ELSE ncd.MontoGravadoLinea END,
    ncd.MontoExentoLinea = CASE WHEN ncd.MontoExentoLinea = 0 AND ncd.Itbis = 0 THEN ncd.SubTotal ELSE ncd.MontoExentoLinea END
FROM dbo.NotasCreditoDetalle ncd;
PRINT 'Backfill NotasCreditoDetalle OK';
GO

-- Compras: sugerencia 5/6 sin confirmar
UPDATE o
SET
    o.DestinoItbisSugerido = CASE
        WHEN o.TotalItbis = 0 THEN NULL
        WHEN o.ItbisProporcionalidad > 0 THEN 7
        WHEN o.ItbisLlevadoAlCosto > 0 AND o.ItbisLlevadoAlCosto >= o.TotalItbis THEN 3
        WHEN ISNULL(o.MontoFacturadoServicios, 0) >= ISNULL(o.MontoFacturadoBienes, 0)
             AND ISNULL(o.MontoFacturadoServicios, 0) > 0 THEN 6
        WHEN ISNULL(o.MontoFacturadoBienes, 0) > 0 THEN 5
        WHEN ISNULL(o.MontoFacturadoServicios, 0) > 0 THEN 6
        ELSE 5
    END,
    o.DestinoItbis = NULL, -- nunca auto-confirmado
    o.EstadoClasificacionItbis = CASE
        WHEN o.TotalItbis = 0 THEN N'NO_APLICA'
        ELSE N'PENDIENTE_VALIDAR'
    END,
    o.ClasificacionConfirmada = 0,
    o.ItbisComprasLocales = CASE
        WHEN o.TotalItbis = 0 THEN 0
        WHEN ISNULL(o.MontoFacturadoBienes, 0) >= ISNULL(o.MontoFacturadoServicios, 0)
            THEN (o.TotalItbis - ISNULL(o.ItbisLlevadoAlCosto, 0))
        ELSE o.ItbisComprasLocales
    END,
    o.ItbisServicios = CASE
        WHEN o.TotalItbis = 0 THEN 0
        WHEN ISNULL(o.MontoFacturadoServicios, 0) > ISNULL(o.MontoFacturadoBienes, 0)
            THEN (o.TotalItbis - ISNULL(o.ItbisLlevadoAlCosto, 0))
        ELSE o.ItbisServicios
    END,
    o.FotografiaFiscalVersion = 0
FROM dbo.OrdenCompraHeaders o;
PRINT 'Backfill OrdenCompraHeaders OK (PENDIENTE_VALIDAR; DestinoItbis NULL)';
GO

UPDATE od
SET od.ItbisCalculado = CASE WHEN od.ItbisCalculado = 0 THEN od.Itbis ELSE od.ItbisCalculado END
FROM dbo.OrdenCompraDetalles od;
PRINT 'Backfill OrdenCompraDetalles OK';
GO

-- Maestros defaults (no fiscal photo)
UPDATE dbo.Proveedores SET RegimenDgii = N'ORDINARIO' WHERE RegimenDgii IS NULL;
UPDATE dbo.Clientes SET RegimenDgii = N'ORDINARIO' WHERE RegimenDgii IS NULL;
-- Productos.TasaItbis queda NULL (default param). TipoIngresoDgiiDefault NULL (app usará 1 en nuevos).
PRINT 'Backfill maestros OK';
GO

-- =============================================================================
-- Verificación
-- =============================================================================
SELECT 'DgiiCatalogo' AS Tabla, COUNT(*) AS Filas FROM dbo.DgiiCatalogo
UNION ALL SELECT 'DgiiConfiguracionEmpresa', COUNT(*) FROM dbo.DgiiConfiguracionEmpresa
UNION ALL SELECT 'FACTC_PendienteValidar', COUNT(*) FROM dbo.OrdenCompraHeaders
    WHERE EstadoClasificacionItbis = N'PENDIENTE_VALIDAR'
UNION ALL SELECT 'FACTC_Confirmados', COUNT(*) FROM dbo.OrdenCompraHeaders
    WHERE ClasificacionConfirmada = 1
UNION ALL SELECT 'FH_FormaVentaCredito15', COUNT(*) FROM dbo.FacturaHeaders WHERE FormaVentaFiscalDgii = 15
UNION ALL SELECT 'FH_TipoIngresoNotNull', COUNT(*) FROM dbo.FacturaHeaders WHERE TipoIngresoDgii IS NOT NULL;

PRINT '=== Sprint A completado en AlahiaPos_Dev ===';
PRINT 'TXT 606 / columnas 606 existentes: no modificados.';
GO
