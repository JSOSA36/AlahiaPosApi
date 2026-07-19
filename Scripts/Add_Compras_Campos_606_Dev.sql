-- ============================================================
-- Compras FACTC: campos fiscales Formato 606 (DGII)
-- Solo Dev: AlahiaPos_Dev
-- ============================================================
USE AlahiaPos_Dev;
GO

-- 1) Catálogo Tipo Bienes y Servicios = códigos oficiales DGII 1-11
IF OBJECT_ID('dbo.TipoBienesServices') IS NOT NULL
BEGIN
    DELETE FROM dbo.TipoBienesServices;

    SET IDENTITY_INSERT dbo.TipoBienesServices ON;

    INSERT INTO dbo.TipoBienesServices (IdTipoBienesServicios, Descripcion, FechaInseccion, IdEmpresa) VALUES
    (1,  N'01 - Gastos de personal',                                                      GETDATE(), 0),
    (2,  N'02 - Gastos por trabajos, suministros y servicios',                              GETDATE(), 0),
    (3,  N'03 - Arrendamientos',                                                           GETDATE(), 0),
    (4,  N'04 - Gastos de activos fijos',                                                  GETDATE(), 0),
    (5,  N'05 - Gastos de representación',                                                 GETDATE(), 0),
    (6,  N'06 - Otras deducciones admitidas',                                              GETDATE(), 0),
    (7,  N'07 - Gastos financieros',                                                       GETDATE(), 0),
    (8,  N'08 - Gastos extraordinarios',                                                   GETDATE(), 0),
    (9,  N'09 - Compras y gastos que formarán parte del costo de venta',                   GETDATE(), 0),
    (10, N'10 - Adquisiciones de activos',                                                 GETDATE(), 0),
    (11, N'11 - Gastos de seguros',                                                        GETDATE(), 0);

    SET IDENTITY_INSERT dbo.TipoBienesServices OFF;
END
GO

-- 2) Campos fiscales en OrdenCompraHeaders
IF COL_LENGTH('dbo.OrdenCompraHeaders', 'NcfModificado') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD NcfModificado NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FormaPagoDgii') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD FormaPagoDgii INT NULL;
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'MontoFacturadoServicios') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD MontoFacturadoServicios DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_MontoServicios DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'MontoFacturadoBienes') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD MontoFacturadoBienes DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_MontoBienes DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisRetenido') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisRetenido DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisRet DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisProporcionalidad') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisProporcionalidad DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisProp DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'ItbisLlevadoAlCosto') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD ItbisLlevadoAlCosto DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_ItbisCosto DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'TipoRetencionIsr') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD TipoRetencionIsr INT NULL;
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'MontoRetencionRenta') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD MontoRetencionRenta DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCH_RetRenta DEFAULT(0);
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FechaPagoFiscal') IS NULL
    ALTER TABLE dbo.OrdenCompraHeaders ADD FechaPagoFiscal DATE NULL;
GO

PRINT 'Add_Compras_Campos_606_Dev OK';
GO
