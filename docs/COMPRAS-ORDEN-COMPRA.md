# Órdenes de Compra (OC)

## Flujo enterprise

```
OC borrador → Emitir (OC-0001) → Enviar al proveedor → Crear FACTC desde OC → Confirmar FACTC (CxP)
                                                                        ↓
                                                              Recepción en Almacén
```

## Reglas

| Acción | CxP / pago | Inventario | Secuencia |
|--------|------------|------------|-----------|
| Emitir OC | No | No | `OC-xxxx` (tipo doc 5) |
| Enviar OC | No | No | Marca `ENVIADA` + `FechaEnvioProveedor` (SMTP fase 2) |
| Generar FACTC | No (borrador) | No | Copia líneas; `IdDocumentoOrigen` = OC |
| Confirmar FACTC | Sí | No | `FACTC-xxxx` + recepción diferida |

## API

- `GET Compras/Ordenes/{idEmpresa}`
- `POST Compras/Ordenes/Borrador`
- `POST Compras/Ordenes/{id}/Emitir`
- `POST Compras/Ordenes/{id}/Enviar`
- `POST Compras/Ordenes/{id}/GenerarFactura/{idEmpresa}`

## SQL

`Scripts/Add_OrdenCompra_Documento.sql`
