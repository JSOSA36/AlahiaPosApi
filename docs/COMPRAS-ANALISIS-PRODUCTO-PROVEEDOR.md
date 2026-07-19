# Análisis de Productos por Proveedor

## Decisión

Sin tabla `ProductoProveedor`. Fuente única: **FACTC confirmadas** (`OrdenCompraHeaders` tipo 11 + `OrdenCompraDetalles`).

## API

```
GET /api/Compras/AnalisisProducto/{idEmpresa}
  ?idProducto=&idProveedor=&idAlmacen=&desde=&hasta=
```

Respuesta:
- `indicadores` (último, promedio, mejor, veces, proveedor más barato/frecuente…)
- `resumenProveedores` (tabla gerencial)
- `historial` (detalle de líneas)
- `evolucionPrecio` (serie temporal)

Precio unitario = `(SubTotal - Itbis) / Cantidad`.

## UI

- Menú Compras → **Analisis Producto-Proveedor** → `/compras/analisis-producto`
- Ficha producto → botón **Historial de Compras** (modal con el mismo reporte)

## Índices

Reutiliza `Add_Indexes_Compras_Inteligencia.sql` (Dev).
