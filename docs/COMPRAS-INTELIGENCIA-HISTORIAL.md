# Compras — Inteligencia desde historial (sin ProductoProveedor)

## Decisión

**No** crear tabla `ProductoProveedor` por ahora.

La **fuente oficial** de indicadores de Compras es:

- `OrdenCompraHeaders` (FACTC, `IdTipoDocumentos = 11`)
- `OrdenCompraDetalles`

Cada línea confirmada ya aporta: producto, proveedor (vía header), fecha, cantidad, descuento, ITBIS, subtotal/total, empresa, almacén.

`ProductoProveedor` solo se evaluará si aparecen necesidades **maestras** (proveedor preferido, código del proveedor, contratos, precios pactados, lead time).

## Filtro canónico de consultas

```sql
h.IdTipoDocumentos = 11          -- solo Factura de Compra (no OC)
AND h.Estado NOT IN ('BORRADOR', 'ANULADA')
AND h.IdEmpresa = @IdEmpresa
```

No mezclar con órdenes (tipo 5): duplicaría el mismo pedido cuando ya existe FACTC.

## Precio unitario de línea

No hay columna `PrecioUnitario` persistida; se deriva:

```sql
PrecioUnitarioNeto = CASE WHEN d.Cantidad = 0 THEN 0
                          ELSE (d.SubTotal - d.Itbis) / d.Cantidad END
```

(Base sin ITBIS; si el reporte requiere costo con impuesto, usar `d.SubTotal / d.Cantidad`.)

## Preguntas de negocio → consultas

| Pregunta | Enfoque |
|----------|---------|
| ¿Más barato por proveedor? | `MIN(PrecioUnitarioNeto)` agrupado por `IdProveedor` para un `IdProducto` |
| ¿Último precio? | `TOP 1` ordenado por `h.FechaInseccion DESC, h.IdOrdenCompraHeader DESC` |
| ¿Promedio / min / max? | `AVG` / `MIN` / `MAX` de precio unitario |
| ¿Variación en el tiempo? | Serie `FechaInseccion`, precio unitario (por factura/línea) |
| ¿Quién vendió más cantidad? | `SUM(d.Cantidad)` por proveedor |
| ¿Mayor frecuencia? | `COUNT(DISTINCT h.IdOrdenCompraHeader)` por proveedor |
| Historial por producto | Join detalle+header filtrado por `IdProducto` |
| Historial por proveedor | Join filtrado por `IdProveedor` |

## Índices (optimización)

Script: `Scripts/Add_Indexes_Compras_Inteligencia.sql` (aplicado en **AlahiaPos_Dev**)

Ajuste previo: `Estado` / `CondicionFactura` / `NumeroDocumento` acotados (ya no `nvarchar(max)`) para poder indexar.

| Índice | Objetivo |
|--------|----------|
| `IX_OrdenCompraDetalles_IdOrdenCompraHeader` | Join detalle→header |
| `IX_OrdenCompraDetalles_Empresa_Producto` | KPIs e historial por producto |
| `IX_OrdenCompraHeaders_Empresa_Tipo_Estado_Fecha` | Timeline FACTC |
| `IX_OrdenCompraHeaders_Empresa_Proveedor_Tipo` | Historial / ranking proveedor |

Ejecutar el mismo script en Prod cuando se habiliten reportes en producción.

## Ejemplo: último precio y más barato

```sql
-- Último precio de compra de un producto
SELECT TOP 1
    h.NumeroDocumento,
    h.FechaInseccion,
    h.IdProveedor,
    (d.SubTotal - d.Itbis) / NULLIF(d.Cantidad, 0) AS PrecioUnitarioNeto
FROM OrdenCompraDetalles d
INNER JOIN OrdenCompraHeaders h ON h.IdOrdenCompraHeader = d.IdOrdenCompraHeader
WHERE d.IdEmpresa = @IdEmpresa
  AND d.IdProducto = @IdProducto
  AND h.IdTipoDocumentos = 11
  AND h.Estado NOT IN ('BORRADOR', 'ANULADA')
ORDER BY h.FechaInseccion DESC, h.IdOrdenCompraHeader DESC;

-- Proveedor más barato (precio unitario neto promedio o mínimo)
SELECT
    h.IdProveedor,
    MIN((d.SubTotal - d.Itbis) / NULLIF(d.Cantidad, 0)) AS PrecioMin,
    AVG((d.SubTotal - d.Itbis) / NULLIF(d.Cantidad, 0)) AS PrecioPromedio,
    SUM(d.Cantidad) AS CantidadTotal,
    COUNT(DISTINCT h.IdOrdenCompraHeader) AS VecesComprado
FROM OrdenCompraDetalles d
INNER JOIN OrdenCompraHeaders h ON h.IdOrdenCompraHeader = d.IdOrdenCompraHeader
WHERE d.IdEmpresa = @IdEmpresa
  AND d.IdProducto = @IdProducto
  AND h.IdTipoDocumentos = 11
  AND h.Estado NOT IN ('BORRADOR', 'ANULADA')
GROUP BY h.IdProveedor
ORDER BY PrecioMin;
```

## Conclusión

Con el modelo actual **sí** se pueden construir todos los indicadores pedidos de forma eficiente, siempre que:

1. Se consulte solo FACTC confirmadas.
2. Existan los índices de join e historial.
3. El precio unitario se calcule desde la línea (sin tabla espejo).

No hace falta `ProductoProveedor` para esta fase.
