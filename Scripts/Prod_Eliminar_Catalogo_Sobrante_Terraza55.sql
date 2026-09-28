/*
  Terraza 27 (MATBERT, 55): eliminar del catálogo los productos que no están
  en el listado fuente de verdad. Los que ya salieron en factura o movimiento
  de inventario se conservan inactivos y fuera del POS (integridad fiscal).
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 55 AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

DECLARE @Borrar TABLE (IdProducto INT PRIMARY KEY);
DECLARE @Conservar TABLE (IdProducto INT PRIMARY KEY);

INSERT INTO @Borrar (IdProducto)
SELECT p.IdProducto
FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND (p.IsActivo = 0 OR p.SeVende = 0)
  AND NOT EXISTS (SELECT 1 FROM dbo.FacturaDetalles d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.MovimientosInventarioDetalle d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.NotasCreditoDetalle d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.OrdenCompraDetalles d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.AjusteInventarioDetalles d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.ConduceDetalles d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.DevolucionesDetalles d WHERE d.IdProducto = p.IdProducto)
  AND NOT EXISTS (SELECT 1 FROM dbo.DevolucionesClienteDetalles d WHERE d.IdProducto = p.IdProducto);

INSERT INTO @Conservar (IdProducto)
SELECT p.IdProducto
FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND (p.IsActivo = 0 OR p.SeVende = 0)
  AND NOT EXISTS (SELECT 1 FROM @Borrar b WHERE b.IdProducto = p.IdProducto);

BEGIN TRAN;

DELETE e
FROM dbo.AlmacenExistencias e
INNER JOIN @Borrar b ON b.IdProducto = e.IdProducto;

DELETE v
FROM dbo.Variaciones v
INNER JOIN @Borrar b ON b.IdProducto = v.IdProducto;

DELETE c
FROM dbo.EmpleadoServicioComisions c
INNER JOIN @Borrar b ON b.IdProducto = c.IdProducto;

DELETE p
FROM dbo.Productos p
INNER JOIN @Borrar b ON b.IdProducto = p.IdProducto
WHERE p.IdEmpresa = 55;

UPDATE p
SET p.IsActivo = 0,
    p.SeVende = 0,
    p.EsServicio = 0,
    p.TipoOperacion = N'SIN_VENTA'
FROM dbo.Productos p
INNER JOIN @Conservar c ON c.IdProducto = p.IdProducto
WHERE p.IdEmpresa = 55;

COMMIT TRAN;

SELECT 'Eliminados' t, COUNT(*) c FROM @Borrar
UNION ALL SELECT 'ConservadosConHistorial', COUNT(*) FROM @Conservar
UNION ALL SELECT 'ActivosPOS', COUNT(*) FROM dbo.Productos
         WHERE IdEmpresa = 55 AND IsActivo = 1 AND SeVende = 1
UNION ALL SELECT 'InactivosRestantes', COUNT(*) FROM dbo.Productos
         WHERE IdEmpresa = 55 AND (IsActivo = 0 OR SeVende = 0);
GO
