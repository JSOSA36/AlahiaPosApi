-- ============================================================
-- Recepción diferida Compras → Almacén (idempotente)
-- ============================================================

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'EstadoRecepcion') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders
        ADD EstadoRecepcion NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.OrdenCompraHeaders', 'FechaUltimaRecepcion') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenCompraHeaders
        ADD FechaUltimaRecepcion DATETIME NULL;
END
GO

IF COL_LENGTH('dbo.OrdenCompraDetalles', 'CantidadRecibida') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenCompraDetalles
        ADD CantidadRecibida DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_OrdenCompraDetalles_CantidadRecibida DEFAULT (0);
END
GO

-- Facturas ya confirmadas con inventario aplicado: marcar recepción completa
UPDATE OrdenCompraHeaders
SET EstadoRecepcion = 'RECIBIDA'
WHERE EstadoRecepcion IS NULL
  AND AjustadaInventario = 1
  AND Estado NOT IN ('BORRADOR', 'ANULADA');
GO

-- Facturas confirmadas sin inventario aún: pendientes de recepción
UPDATE OrdenCompraHeaders
SET EstadoRecepcion = 'PENDIENTE_RECEPCION'
WHERE EstadoRecepcion IS NULL
  AND ISNULL(AjustadaInventario, 0) = 0
  AND Estado NOT IN ('BORRADOR', 'ANULADA');
GO

-- Borradores / anuladas
UPDATE OrdenCompraHeaders
SET EstadoRecepcion = 'NO_APLICA'
WHERE EstadoRecepcion IS NULL;
GO

-- Histórico: si ya se ajustó inventario, igualar cantidad recibida a cantidad pedida
UPDATE d
SET d.CantidadRecibida = d.Cantidad
FROM dbo.OrdenCompraDetalles d
INNER JOIN dbo.OrdenCompraHeaders h
    ON h.IdOrdenCompraHeader = d.IdOrdenCompraHeader
WHERE h.EstadoRecepcion = 'RECIBIDA'
  AND ISNULL(d.CantidadRecibida, 0) = 0
  AND d.Cantidad > 0;
GO

PRINT 'Recepción diferida: columnas aplicadas.';
GO
