-- ============================================================
-- Facturación Electrónica - Fase 1: DDL
-- Solo ejecutar en AlahiaPos_Dev
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

USE AlahiaPos_Dev;
GO

-- ============================================================
-- 1. SecuenciasECF: nuevas columnas para e-CF
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'Descripcion')
BEGIN
    ALTER TABLE SecuenciasECF ADD Descripcion NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'TipoEcfDgii')
BEGIN
    ALTER TABLE SecuenciasECF ADD TipoEcfDgii INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'SecuenciaInicial')
BEGIN
    ALTER TABLE SecuenciasECF ADD SecuenciaInicial INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'Ambiente')
BEGIN
    ALTER TABLE SecuenciasECF ADD Ambiente NVARCHAR(20) NOT NULL DEFAULT 'PRUEBAS';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'FechaAutorizacion')
BEGIN
    ALTER TABLE SecuenciasECF ADD FechaAutorizacion DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SecuenciasECF') AND name = 'NumeroResolucion')
BEGIN
    ALTER TABLE SecuenciasECF ADD NumeroResolucion NVARCHAR(50) NULL;
END
GO

-- ============================================================
-- 2. ECFEncabezado: extensión para facturación electrónica
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'OrigenDocumento')
BEGIN
    ALTER TABLE ECFEncabezado ADD OrigenDocumento INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'IdOrigen')
BEGIN
    ALTER TABLE ECFEncabezado ADD IdOrigen INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'SecurityCode')
BEGIN
    ALTER TABLE ECFEncabezado ADD SecurityCode NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'UrlQR')
BEGIN
    ALTER TABLE ECFEncabezado ADD UrlQR NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'FechaFirma')
BEGIN
    ALTER TABLE ECFEncabezado ADD FechaFirma DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'NumeroFacturaInterna')
BEGIN
    ALTER TABLE ECFEncabezado ADD NumeroFacturaInterna NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ECFEncabezado') AND name = 'EstadoDocumento')
BEGIN
    ALTER TABLE ECFEncabezado ADD EstadoDocumento NVARCHAR(30) NOT NULL DEFAULT 'BORRADOR';
END
GO

-- ============================================================
-- 3. Índice para búsqueda por origen
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ECFEncabezado_Origen' AND object_id = OBJECT_ID('ECFEncabezado'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ECFEncabezado_Origen
    ON ECFEncabezado (OrigenDocumento, IdOrigen)
    INCLUDE (IdEmpresa, ENCF, EstadoDocumento, EstadoDGII);
END
GO

-- ============================================================
-- 4. Índice para SecuenciasECF por empresa y tipo e-CF
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SecuenciasECF_EmpresaTipoEcf' AND object_id = OBJECT_ID('SecuenciasECF'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SecuenciasECF_EmpresaTipoEcf
    ON SecuenciasECF (IdEmpresa, TipoEcfDgii, Activo)
    INCLUDE (SecuenciaActual, SecuenciaFinal, fechaVencimiento, Ambiente);
END
GO

PRINT '=== Facturación Electrónica Fase 1 DDL completado ===';
GO
