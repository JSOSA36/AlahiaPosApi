-- AlahiaPos_Dev: TipoComportamiento (independiente de EsServicio)
-- Ejecutar en AlahiaPos_Dev. Idempotente.

USE AlahiaPos_Dev;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Productos') AND name = 'TipoComportamiento'
)
BEGIN
    ALTER TABLE Productos
        ADD TipoComportamiento NVARCHAR(30) NULL;
END
GO

-- Comportamiento de compra: NO modifica EsServicio (naturaleza comercial / POS).
UPDATE Productos
SET TipoComportamiento = CASE
    WHEN ControlarStock = 1 THEN 'Inventario'
    WHEN EsServicio = 1 THEN 'Gasto'
    ELSE 'Gasto'
END
WHERE TipoComportamiento IS NULL OR LTRIM(RTRIM(TipoComportamiento)) = '';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'TipoComportamientoLinea'
)
BEGIN
    ALTER TABLE OrdenCompraDetalles
        ADD TipoComportamientoLinea NVARCHAR(30) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'IdGastoGenerado'
)
BEGIN
    ALTER TABLE OrdenCompraDetalles
        ADD IdGastoGenerado INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('OrdenCompraDetalles') AND name = 'IdActivoFijoGenerado'
)
BEGIN
    ALTER TABLE OrdenCompraDetalles
        ADD IdActivoFijoGenerado INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Gastos') AND name = 'IdOrdenCompraDetalle'
)
BEGIN
    ALTER TABLE Gastos
        ADD IdOrdenCompraDetalle INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Gastos') AND name = 'OrigenModulo'
)
BEGIN
    ALTER TABLE Gastos
        ADD OrigenModulo NVARCHAR(30) NULL;
END
GO

-- Verificación empresa 60 (clínica dental)
SELECT
    COUNT(*) AS TotalActivos,
    SUM(CASE WHEN EsServicio = 1 THEN 1 ELSE 0 END) AS ServiciosComerciales,
    SUM(CASE WHEN TipoComportamiento = 'Inventario' THEN 1 ELSE 0 END) AS ComportamientoInventario,
    SUM(CASE WHEN TipoComportamiento = 'Gasto' THEN 1 ELSE 0 END) AS ComportamientoGasto
FROM Productos
WHERE IdEmpresa = 60 AND IsActivo = 1;
GO

PRINT 'TipoComportamiento aplicado. EsServicio no fue modificado.';
GO
