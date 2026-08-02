# PrinterApi — Mixed Content (HTTPS → HTTP)

Runbook de soporte cuando el POS en la nube **no envía facturas / encargos al PrinterApi local**.

Fecha de referencia: 2026-07-21  
Caso: De Laura Pastelería (`IdEmpresa = 56`), demo `cloudalahiaposdemo.alahiapos.com`.

---

## Síntoma

- El cliente vende o imprime desde el POS web (HTTPS) y **no sale ticket**.
- En Chrome DevTools → Console aparece algo como:

```text
Mixed Content: The page at 'https://...alahiapos.com/...' was loaded over HTTPS,
but requested an insecure XMLHttpRequest endpoint
'http://10.0.0.x:5045/api/Printer/ticket/...'.
This content should also be served over HTTPS.
```

La petición **ni siquiera llega** al PrinterApi (no hay log en la consola del servicio local).

---

## Causa

1. El frontend usa `Empresas.ApiPrint` (ej. `http://10.0.0.10:5045`).
2. El POS corre en **HTTPS**.
3. Chrome bloquea llamadas **HTTP a una IP de red** desde una página HTTPS (**Mixed Content**).
4. Excepción práctica: `http://localhost:5045` suele funcionar en la misma PC; **no sirve para tabletas** u otros equipos de la red (para ellos `localhost` es el dispositivo, no la PC del PrinterApi).

### Config típica en Prod (`Empresas.ApiPrint`)

| Uso | Valor | Notas |
|-----|--------|--------|
| Misma PC + tabletas en LAN | `http://10.0.0.x:5045` | Correcto para multi-dispositivo; dispara Mixed Content en Chrome |
| Solo esa PC | `http://localhost:5045` | Evita Mixed Content en esa máquina; rompe tabletas remotas |

No “arreglar” cambiando a `localhost` si el cliente usa tableta contra el mismo PrinterApi.

---

## Diagnóstico rápido

1. Confirmar `ApiPrint` en Prod (solo lectura salvo autorización explícita):

```sql
SELECT IdEmpresa, NombreComercial, ApiPrint
FROM Empresas
WHERE IdEmpresa = <IdEmpresa>;
```

2. En la PC del PrinterApi: servicio corriendo, escuchando en `0.0.0.0:5045` (o al menos accesible en la LAN).
3. Probar ping local: `http://localhost:5045/api/Printer/ping`
4. Probar por IP: `http://10.0.0.x:5045/api/Printer/ping` (misma IP que en `ApiPrint`).
5. Si el ping por IP falla: IP cambió (DHCP), firewall, o el API no escucha en todas las interfaces.
6. En DevTools del POS: ¿sigue saliendo Mixed Content o ya es `ERR_CONNECTION_*`?

---

## Solución inmediata (por equipo)

Mantener `ApiPrint` con la **IP de la PC del PrinterApi**. En cada Chrome que imprima vía API:

### PC (Chrome escritorio)

1. Abrir el POS (HTTPS).
2. Candado junto a la URL → **Configuración de sitios**.
3. **Contenido inseguro** → **Permitir**.
4. Recargar el POS y probar impresión.
5. Si llega al PrinterApi, la consola del servicio debe mostrar el GET (ej. ticket / BizcochoEncargo/print).

### Tableta (Chrome Android)

En Android el permiso por sitio suele no estar disponible:

1. Ir a `chrome://flags`
2. Buscar **Insecure origins treated as secure**
3. Agregar: `http://10.0.0.x:5045` (IP real del cliente)
4. **Enabled** → **Relaunch**
5. Abrir el POS y probar impresión

Repetir en **cada** equipo/navegador que necesite llamar al PrinterApi por HTTP.

---

## Si ya no hay Mixed Content pero no imprime

| Error / síntoma | Qué hacer |
|-----------------|-----------|
| `ERR_CONNECTION_REFUSED` / timeout | PrinterApi caído, IP incorrecta, o puerto 5045 bloqueado |
| Ping localhost OK, IP no | Firewall Windows / binding del API |
| Petición llega (log 200 al API nube) pero no papel | Impresora, driver ESC/POS, o error posterior en PrinterApi (revisar consola del servicio) |
| POS tableta/compacto | Algunos flujos usan vista previa del navegador y **no** llaman ApiPrint; otros (encargos, cierres) sí |

---

## Checklist instalación nuevo cliente con PrinterApi + POS nube

- [ ] PC dedicada (o estable) con PrinterApi en puerto **5045**, escuchando en red (`0.0.0.0`).
- [ ] IP **fija** o reserva DHCP en el router; documentar la IP en `Empresas.ApiPrint`.
- [ ] Firewall: permitir TCP **5045** entrante en esa PC.
- [ ] Tabletas y cajas en la **misma LAN** (o VPN) que puedan alcanzar esa IP.
- [ ] Chrome: permitir contenido inseguro (PC) / insecure origin flag (Android) para esa URL.
- [ ] Probar: venta/encargo → log en PrinterApi → ticket físico.
- [ ] No usar `localhost` en `ApiPrint` si hay tableta u otra caja.

---

## Mejora de producto (pendiente)

Servir PrinterApi por **HTTPS** (certificado local o hostname interno) para eliminar el ajuste manual de Mixed Content en todos los clientes.

---

## Referencia código

- Frontend: `PrintService` → `parametros.ApiPrint` + `/api/Printer/ticket/{id}/{idEmpresa}`
- Login carga: `empresa.apiPrint` → `ParametrosService.ApiPrint`
- Backend local: proyecto `PrinterApi` (ticket, lavador, cierre, encargos, etc.)
