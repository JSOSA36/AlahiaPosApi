# Arquitectura — Recepción diferida (Compras ≠ Almacén)

## Decisión

Al confirmar una factura de compra **NO** se genera entrada de inventario ni alta de activo fijo.

| Responsabilidad | Módulo | Momento |
|-----------------|--------|---------|
| Documento compra + CxP/pago | **Compras** | Confirmar factura |
| Gasto no inventariable | **Compras** | Confirmar (líneas `Gasto`) |
| Entrada física inventario / ActivoFijo | **MovimientoInventario** | Recepción desde compra |

## Dos dimensiones de estado

| Campo | Significado | Valores |
|-------|-------------|---------|
| `Estado` | Ciclo financiero / documento | `BORRADOR`, `CONFIRMADA`, `PARCIALMENTE_PAGADA`, `PAGADA`, `ANULADA` |
| `EstadoRecepcion` | Ciclo físico almacén | `NO_APLICA`, `PENDIENTE_RECEPCION`, `PARCIALMENTE_RECIBIDA`, `RECIBIDA` |

## Flujo

```
1. Borrador compra
2. Confirmar
     ├── Secuencia FACTC
     ├── CxP o pago contado (tesorería)
     ├── Líneas Gasto → tabla Gastos (si aplica)
     ├── Inventario / ActivoFijo → NO se mueven
     └── EstadoRecepcion = PENDIENTE_RECEPCION (si hay líneas recibibles)
                            o NO_APLICA (solo Gasto/Servicio)
3. Almacén → Movimiento inventario → "Recepción desde Compra"
     ├── Busca FACTC / NCF proveedor / proveedor / pendientes
     ├── Carga líneas con CantidadPendienteRecepcion
     ├── Confirma recepción (parcial o total)
     └── Único punto que llama MovimientosInventarioService.GuardarMovimiento
4. Cuando CantidadRecibida >= Cantidad en todas las líneas recibibles
     └── EstadoRecepcion = RECIBIDA, AjustadaInventario = true
```

## Campos

**Header:** `EstadoRecepcion`, `FechaUltimaRecepcion`, `IdAlmacen` (sugerido), `AjustadaInventario` (completitud recepción)

**Detalle:** `CantidadRecibida` (pendiente = Cantidad − CantidadRecibida)

## API

| Endpoint | Uso |
|----------|-----|
| `GET Compras/PendientesRecepcion/{idEmpresa}` | Búsqueda para almacén |
| `GET Compras/Recepcion/{id}/{idEmpresa}` | Documento listo para recibir |
| `POST Compras/{id}/Recepcion` | Confirma recepción → MovimientoInventario |

## Trazabilidad

- Movimiento: `Motivo=COMPRA`, `Referencia=OC-{id}-REC-{timestamp}`
- Activo fijo: la recepción deja la cantidad recibida; el módulo Activos tomará esas líneas después (`IdActivoFijoGenerado`)

## Script

`Scripts/Add_Recepcion_Diferida_Compras.sql`
