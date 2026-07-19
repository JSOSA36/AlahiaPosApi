# Estado de Cuenta del Proveedor (CxP)

## Ubicación

Módulo **Cuentas por Pagar** — auxiliar de proveedor. No es una consulta suelta sin contexto.

## Fuente (sin tablas nuevas)

| Movimiento | Origen | Débito | Crédito |
|------------|--------|--------|---------|
| Factura FACTC confirmada | `OrdenCompraHeaders` tipo 11 | `Total` | — |
| Pago a proveedor | `PagosProveedor` | — | `Monto` |

Excluir: `BORRADOR`, `ANULADA`. No incluir Órdenes de Compra (tipo 5).

## Convenio del auxiliar

Vista del **saldo por pagar al proveedor**:

- Factura → Débito (aumenta lo adeudado)
- Pago → Crédito (disminuye)
- `Balance` = running total

```
SaldoInicial = Σ facturas < desde − Σ pagos < desde
```

## API

```
GET /api/Compras/EstadoCuenta/{idEmpresa}/{idProveedor}?desde=&hasta=
```

Respuesta: encabezado, movimientos cronológicos, totales del período, balance pendiente actual, facturas abiertas con días de vencimiento.

## UI

`/compras/cxp/estado-cuenta` — filtros proveedor + fechas, tabla auxiliar, pendientes, imprimir (`window.print`). PDF/Excel: fase siguiente.

## Índice

`Scripts/Add_Index_PagosProveedor_EstadoCuenta.sql` — `(IdEmpresa, IdProveedor, FechaInseccion)`
