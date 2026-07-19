# MVP Compras — Verificación técnica (AlahiaPos_Dev)

## 1. Mapeo EF

| Entidad C# | Tabla BD | Estado |
|------------|----------|--------|
| `OrdenCompraHeader` | `OrdenCompraHeaders` | Requiere `[Table("OrdenCompraHeaders")]` + `DbSet` |
| `OrdenCompraDetalle` | `OrdenCompraDetalles` | Requiere `[Table("OrdenCompraDetalles")]` + `DbSet` |
| `PagosProveedor` | `PagosProveedor` | Tabla nueva (script SQL) |
| `Proveedores` | `Proveedores` | Sin `DbSet` explícito; `Set<Proveedores>()` funciona |

## 2. FKs existentes

**OrdenCompraHeaders:**
- `IdProveedor` → `Proveedores`
- `IdTipoDocumentos` → `TipoDocumentos`
- `IdTipoBienesServicios` → `TipoBienesServices`

**OrdenCompraDetalles:**
- `IdProducto` → `Productos`
- (Sin FK a header en BD; relación lógica por `IdOrdenCompraHeader`)

**Proveedores referenciado por:** Gastos, Productos, OrdenCompraHeaders, DevelocionesHeaders

## 3. Campos reutilizados (sin nuevas columnas salvo IdAlmacen)

| Requerimiento MVP | Campo existente |
|-------------------|-----------------|
| Proveedor | `IdProveedor` |
| Fecha documento | `FechaInseccion` |
| Nº comprobante proveedor | `NCF` |
| Condición contado/crédito | `CondicionFactura` ("Contado" / "Credito") |
| Vencimiento | `FechaBencimiento` |
| Líneas producto | `OrdenCompraDetalles` |
| ITBIS, descuento, subtotal | `Itbis`, `Descuento`, `SubTotal` |
| Totales | `Total`, `TotalItbis`, `TotalDescuento` |
| CxP | `Pagado`, `Pendiente` |
| Estado | `Estado` |
| Nota/referencia | `Comentario` |
| Nº interno FACTC | `NumeroDocumento` + `SecuenciaDocumentoService` (tipo 11) |
| Tipo factura compra | `IdTipoDocumentos = 11` |
| Flag inventario aplicado | `AjustadaInventario` |
| Almacén recepción | **`IdAlmacen`** (columna nueva indispensable) |

## 4. Campo adicional indispensable

- **`IdAlmacen`** en `OrdenCompraHeaders`: no existía; necesario para `MovimientosInventario` sin duplicar lógica.

## 5. Idempotencia

| Operación | Mecanismo |
|-----------|-----------|
| Inventario / ActivoFijo | **No** al confirmar. Recepción vía `POST Compras/{id}/Recepcion` → `MovimientosInventario`. Ver `COMPRAS-RECEPCION-DIFERIDA.md`. |
| Pago contado al confirmar | `ClaveIdempotencia = "COMPRA-CONTADO-{id}"` en `MovimientoFinanciero` |
| Pago CxP parcial/total | `ClaveIdempotencia = "PAGO-CXP-{idOrden}-{idPago}"` |
| Recepción física | `EstadoRecepcion` + `CantidadRecibida`; `AjustadaInventario` = recepción completa |
| Reconfirmar factura | Rechazar si `Estado` no es `BORRADOR` |

Transacciones: confirmación (header + gastos + CxP/tesorería); recepción (detalle + movimiento inventario) en transacciones separadas.
