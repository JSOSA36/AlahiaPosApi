# Compras — TipoComportamiento (arquitectura aprobada)

## Decisión

El ERP clasifica artículos por **comportamiento operativo**, no por categoría comercial.

| TipoComportamiento | Al confirmar compra | Al recibir en Almacén | Estado |
|--------------------|---------------------|------------------------|--------|
| `Inventario` | Solo documenta + CxP/pago | Entrada vía `MovimientoInventario` | Implementado (recepción diferida) |
| `Gasto` | Registro en tabla `Gastos` (sin duplicar tesorería) | N/A | Implementado |
| `Servicio` | Reservado — flujo futuro | N/A | Stub |
| `ActivoFijo` | Solo documenta + CxP/pago | Entrada física vía `MovimientoInventario`; alta de activo es fase futura | Recepción diferida |

Ver también: [COMPRAS-RECEPCION-DIFERIDA.md](./COMPRAS-RECEPCION-DIFERIDA.md).

## Maestro de artículos (`Productos`)

- **`EsServicio`** — Naturaleza comercial (Producto / Servicio). POS, Citas, Odontología. **No se modifica desde Comportamiento ERP.**
- **`TipoComportamiento`** — Comportamiento en compras/contabilidad. Campo independiente.

Migración automática (sin tocar `EsServicio`):

| Condición existente | TipoComportamiento asignado |
|---------------------|----------------------------|
| ControlarStock = 1 | Inventario |
| EsServicio = 1 | Gasto |
| Otros | Gasto |

Los servicios comerciales (EsServicio=1) siguen siendo servicios en POS; su comportamiento de compra por defecto es Gasto.

## Documento único de compra

- `OrdenCompraHeader` — un documento por factura proveedor
- `OrdenCompraDetalle.TipoComportamientoLinea` — **snapshot** del producto al guardar borrador
- `IdGastoGenerado` / `IdActivoFijoGenerado` — trazabilidad hacia módulos destino

## Orquestador

```
Confirmar factura (Compras)
  ├── CxP / pago contado (tesorería)
  ├── Gasto → Gastos (OrigenModulo=COMPRAS)
  └── Inventario / ActivoFijo → EstadoRecepcion = PENDIENTE_RECEPCION (sin stock)

Recepción física (Almacén → MovimientoInventario)
  └── ConfirmarRecepcion → GuardarMovimiento ENTRADA/COMPRA
        └── actualiza CantidadRecibida / EstadoRecepcion
```

Tesorería y CxP permanecen a **nivel documento** (un pago por factura).
Inventario físico permanece **solo** en `MovimientosInventarioService`.

## SQL

`Scripts/Add_Recepcion_Diferida_Compras.sql`

## Reutilización futura

`TipoComportamientoConstantes` debe usarse en:

- Contabilidad (mapeo de cuentas por tipo)
- Tesorería (clasificación de movimientos)
- Activos Fijos (alta desde recepción de compra)
- Reportes gerenciales

## Categorías

`Categorias` sigue siendo solo clasificación comercial/visual. **No** determina comportamiento de compra.
