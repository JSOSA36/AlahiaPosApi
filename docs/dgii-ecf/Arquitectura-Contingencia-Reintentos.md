# Arquitectura de contingencia, reintentos y continuidad e-CF

**Proyecto:** Alahia.eCF.Api / canal DGII directo  
**Fecha:** 2026-07-19  
**Estado:** Propuesta (sin implementación)  
**Dueño de implementación:** Transmission Engine (genérico) + **DGII Provider** (perfil normativo de este doc). Ver [Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md).  
**Fuentes oficiales (repo local `docs/dgii-ecf/`):**
- `Instructivo-Contingencia-FE` (feb 2026)
- `Informe Técnico e-CF v1.0` §19 Operación en Contingencia (mar 2026) — Arts. 40–43 Reglamento 587-24 / Ley 32-23
- `Formato Comprobante Fiscal Electrónico (e-CF) V1.0` — `IndicadorEnvioDiferido`, `CodigoModificacion=4`
- `Descripcion Tecnica Servicios DGII` — recepción, trackId, consulta de resultado/estado

> Publicaciones DGII marcadas como informativas sin validez legal; la fuente vinculante es la ley/reglamento/norma. Este documento alinea el diseño Alahia a lo publicado y deja explícito qué es **normativa** vs **decisión de ingeniería**.
>
> **Actualización:** cola, reintentos, auditoría, alertas y recuperación son del **Transmission Engine genérico**.  
> Vocabulario y normativa **DGII** (trackId, 72 h, RFCE, OFV) viven en el **DGII Provider** y en este documento — no en el core del Engine.  
> Ver [Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md).

---

## 0. Relación con el Transmission Engine

Este documento es la fuente de verdad **normativa RD**.  
Se materializa como config/comportamiento del **DgiiProvider**, no como tipos del núcleo del Engine.

```
Normativa DGII (este doc)  →  TransmissionProviders:DGII (config)  →  DgiiProvider
                                                                      ↑
Transmission Engine (genérico: cola/retry/audit) ─────────────────────┘
```

## 1. Objetivo

Que la API opere de forma robusta cuando la DGII (o la red) no esté disponible: **sin perder e-CF**, con **reintentos automáticos** dentro del plazo normativo, **idempotencia**, **auditoría completa** y **escalamiento** claro cuando expire el tiempo o se requiera intervención humana / OFV.

---

## 2. Conclusiones de la documentación DGII

### 2.1 Tres escenarios distintos (no mezclar)

| # | Escenario DGII | Qué ocurre | Qué debe hacer el emisor | Plazo clave |
|---|----------------|------------|--------------------------|-------------|
| A | **Falta de conectividad / intermitencia** (puede generar e-CF, no puede remitir) | Offline de envío | Generar e-CF offline, RI con leyenda de contingencia, **remitir al restablecer** | **≤ 72 horas** desde la emisión/offline |
| B | **Imposibilidad de emitir e-CF** (falla técnica del emisor) | No hay XML e-CF | Emitir **NCF no electrónico (Serie B)**; declarar contingencia **Total/Parcial** en OFV | Contingencia **≤ 15 días calendario**; luego **30 días** para enviar e-CF de reemplazo |
| C | **Contingencia de la DGII** (sistemas DGII no disponibles) | Emisor puede generar | **Almacenar** e-CF y enviar al restablecer | Si DGII > **15 días hábiles** down → OFV habilita reportes y operación con no electrónicos |

La API Alahia.eCF debe cubrir principalmente **A** y **C** de forma automática (cola + worker).  
El escenario **B** es proceso de negocio/OFV + emisión papel + e-CF de reemplazo (`CodigoModificacion = 4`); la API lo soporta como tipo de operación, no como “reintento de POST”.

### 2.2 Plazo de reenvío (conectividad) — 72 horas

Texto oficial (Instructivo + Informe Técnico):

- Generar e-CF de manera **offline**.
- Remitir a la DGII al restablecer la conexión, **en un plazo no mayor de setenta y dos (72) horas**.
- Entregar al cliente representación impresa con leyenda del tipo:  
  *“e-CF emitido en modalidad de Contingencia, el cual podrá ser consultado para su validez fiscal, a partir de las setenta y dos (72) horas.”*

**Implicación para Alahia:** el reloj de 72 h es el **SLA normativo de remisión**. La cola automática debe priorizar y alertar **antes** de vencer; al vencer sin envío exitoso → estado de intervención / contingencia formal, no seguir reintentando en silencio indefinidamente.

### 2.3 Contingencia “imposibilidad de emitir” — 15 + 30 días

- Máximo **15 días calendario** en contingencia notificada (Total/Parcial) vía OFV.
- Al salir: **30 días calendario** para generar y remitir a la DGII los e-CF que **reemplazan** los no electrónicos (solo a DGII, no al receptor).
- `CodigoModificacion = 4` — *Reemplazo NCF emitido en contingencia*.
- Solo son válidos fiscalmente los no electrónicos emitidos en contingencia **debidamente notificada**.

### 2.4 Contingencia de la DGII

- Almacenar e-CF y enviar al restablecer (sin un “máximo 72 h” explícito en el mismo párrafo; el sentido práctico es remisión inmediata al recuperar servicio).
- Si la indisponibilidad DGII supera **15 días hábiles**: OFV habilita envío de reportes y operación temporal con comprobantes no electrónicos.

### 2.5 Qué **no** es contingencia de conectividad

**`IndicadorEnvioDiferido`** (Formato e-CF): aplica a contribuyentes **previamente autorizados** a ventas offline (handheld, etc.), código `1`.  
No sustituye el procedimiento de contingencia ni autoriza a cualquiera a diferir envíos.  
**No usar** este indicador como “flag de cola de reintento” salvo autorización DGII real del RNC.

### 2.6 Estados oficiales del **servicio** DGII (recepción / consulta)

De *Descripción Técnica Servicios DGII* (resumen operativo):

| Momento | Qué retorna DGII | Significado |
|---------|------------------|-------------|
| POST recepción e-CF | `trackId` (acuse) | Documento recibido para procesamiento; **aún no** es validez fiscal final |
| Consulta por `trackId` | `estado`, `codigo`, `mensajes`, `eNCF`, etc. | Resultado de validación |
| Estados de validez citados | **Aceptado (1)**, **Rechazado (2)**, **No encontrado (0)** | Validez / nulidad / aún no reportado |
| Adicional | **Aceptado condicional** | Temporal; RFCE / consumo &lt; RD$250,000 |
| RFCE (recepción FC) | Aceptado / Aceptado condicional / Rechazado | Respuesta más inmediata en canal resumen |

Estados **internos Alahia** (cola, reintento, vencido) **no** son estados DGII: hay que mapearlos y mostrarlos al ERP sin confundirlos con Aceptado/Rechazado oficiales.

### 2.7 Buenas prácticas que la normativa implica (aunque no detallen “retry cada N min”)

La DGII **no** publica un algoritmo de reintentos (intervalos, max attempts). Sí exige:

1. **Persistir** el e-CF cuando no se puede transmitir (A/C).
2. **Remitir** apenas haya comunicación, dentro de **72 h** en falta de conectividad.
3. **Representación impresa** correcta en contingencia de conectividad.
4. **No inventar** validez: la consulta fiscal aplica tras el plazo indicado en la leyenda.
5. Separar claramente falla de **emisión** (papel + OFV) vs falla de **transmisión** (cola).
6. Idempotencia práctica: el nombre de archivo `/ eNCF` identifica el comprobante; reenviar el **mismo** e-CF firmado es recuperación; generar **otro** eNCF para la misma venta es otro documento.

---

## 3. Mapa normativo → comportamiento Alahia

```
¿Se pudo generar y firmar el XML e-CF?
│
├─ NO  → Escenario B (imposibilidad emitir)
│         · ERP: NCF Serie B + declaración OFV
│         · Luego e-CF reemplazo (cod. 4) en ≤30 días post-salida
│
└─ SÍ  → Intento POST recepción / RFCE
          │
          ├─ HTTP OK + trackId (o RFCE Aceptado*)
          │     → Estado DGII en curso / final vía consulta
          │
          ├─ Timeout / 5xx / red / DGII caída
          │     → Escenario A o C: PERSISTIR + cola ≤72h (A)
          │
          └─ 4xx de negocio / Rechazado XSD
                → NO reintentar ciego; corregir y (si aplica) nuevo ciclo
```

\*RFCE: respuesta síncrona de aceptación/rechazo; si falla transporte, misma cola.

---

## 4. Propuesta de arquitectura

### 4.1 Principios

| Principio | Detalle |
|-----------|---------|
| **Dueño único** | Toda esta sección se implementa en el **Transmission Engine**, no en facades ni en el Motor XML. |
| **Outbox / cola durable** | Sí. El e-CF firmado (artefacto canónico) se guarda **antes** o **en el mismo commit** que marca “Pendiente de envío”. |
| **Worker en segundo plano** | Sí. Background service del Engine: envío + consulta `trackId`. |
| **Idempotencia** | Clave natural: `(RncEmisor, eNCF)` + hash del XML firmado. Un solo “trabajo activo” por eNCF. |
| **Normativa primero** | Ventana 72 h como deadline de cola automática para conectividad (valor en **config**, default 72). |
| **Separar transporte vs negocio** | 5xx/timeout → reintento; Rechazado DGII → no reintento automático de “más de lo mismo”. |
| **XML inmutable** | El Engine **nunca** modifica el XML; solo lo transmite. |
| **Políticas configurables** | Backoff, workers, timeouts, alertas, polls trackId → `TransmissionEngine` en appsettings. |
| **Trazabilidad** | Cada intento = fila de auditoría inmutable. |
| **Reenvío manual** | Sí, vía API del Engine, respetando idempotencia. |

### 4.2 Componentes

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  ERP / API  │────▶│  Emisión e-CF    │────▶│  Store canónico     │
│  POS        │     │  XML+Firma       │     │  XML firmado+meta   │
└─────────────┘     └────────┬─────────┘     └──────────┬──────────┘
                             │                          │
                             ▼                          ▼
                    ┌──────────────────┐     ┌─────────────────────┐
                    │ Intento envío    │     │ OutboxEnvioEcf      │
                    │ síncrono (corto) │     │ + IntentosAuditoria │
                    └────────┬─────────┘     └──────────┬──────────┘
                             │ fallo transporte         │
                             └──────────▶ encola ───────┘
                                                    │
                         ┌──────────────────────────▼──────────────┐
                         │  Background Worker                      │
                         │  · claim outbox (SKIP LOCKED)           │
                         │  · POST recepción / RFCE                │
                         │  · consulta trackId hasta terminal      │
                         │  · backoff + respeto deadline 72h       │
                         └──────────────────────────┬──────────────┘
                                                    │
                         ┌──────────────────────────▼──────────────┐
                         │  Notificación ERP (webhook/poll/estado) │
                         └─────────────────────────────────────────┘
```

Alineación con lo ya existente en AlahiaPosApi: patrón `EventosOutbox` + `EcfGatewayOutboxProcessor` (claim con `READPAST`/`UPDLOCK`, max intentos). La propuesta para **Alahia.eCF.Api** (DGII directo) debe ser el mismo estilo, especializado en **artefacto firmado + ventana 72 h + estados de contingencia**.

### 4.3 Estados internos propuestos (Alahia)

| Estado Alahia | Significado | ¿Es estado DGII? |
|---------------|-------------|------------------|
| `GeneradoFirmado` | XML firmado persistido | No |
| `Enviando` | Worker/claim en curso | No |
| `EnProcesoDgii` | Hay `trackId`; esperando consulta | Parcial (acuse DGII) |
| `Aceptado` / `AceptadoCondicional` / `Rechazado` | Resultado consulta/recepción | **Sí** (mapeo oficial) |
| `PendienteEnvio` | Transporte falló; en cola | No |
| `Reintentando` | Subestado de pendiente | No |
| `VencidoSinEnvio` | Superó 72 h sin acuse | No → **intervención** |
| `RequiereIntervencion` | Política/exhaustivo/ambiguo | No |
| `ContingenciaOfv` | Escenario B declarado | Proceso OFV |

### 4.4 Política de reintentos (ingeniería, alineada a 72 h)

La DGII no fija intervalos. Propuesta Alahia:

| Parámetro | Valor propuesto | Justificación |
|-----------|-----------------|---------------|
| Intento síncrono inicial | 1 (timeout corto, p.ej. 30–60 s) | UX: si DGII está OK, responde ya |
| Backoff | Exponencial con jitter: 1m → 2m → 5m → 15m → 30m → 1h | Evita martillar DGII en caída masiva |
| Tope de intentos automáticos | Por **tiempo**, no solo por conteo: mientras `now < FechaEmision/Firma + 72h` | Cumple plazo normativo |
| Máx. intentos soft | p.ej. 25–40 en 72 h (derivado del backoff) | Evita loops infinitos si el reloj falla |
| Tras 72 h sin `trackId` | `VencidoSinEnvio` → `RequiereIntervencion` | Obliga procedimiento contingencia / soporte |
| Errores **no** reintentables | 401 auth propia, XML mal formado local, Rechazado DGII definitivo | Corrección humana / nuevo documento |
| Errores reintentables | Timeout, 502/503/504, conexión reset, “DGII no disponible” | Cola |
| Ambiguo (timeout **después** de POST) | No re-POST ciego: primero **consulta** por eNCF/estado si hay API; si no, marcar `RequiereIntervencion` o política “reenviar mismo XML una vez con dedupe” | Evita doble procesamiento |

**RFCE:** misma cola; si ya hubo Aceptado síncrono, no reencolar.

### 4.5 Cómo evitar enviar dos veces el mismo e-CF

1. **Clave única** `(IdEmpresa/RncEmisor, eNCF)` en outbox y en store canónico.  
2. Persistir **hash SHA-256** del XML firmado; el worker solo envía ese blob.  
3. Si ya existe `trackId` asociado → **solo consultar**, no volver a recepcionar.  
4. Claim atómico (una sola instancia procesa el ítem).  
5. Nombre de archivo DGII estable: `{RNC}{eNCF}.xml` (como hoy).  
6. Reenvío manual = “poner Pendiente otra vez” **solo si** no hay `trackId` terminal Aceptado/Rechazado.

### 4.6 Registro de cada intento (auditoría)

Por cada intento guardar al menos:

| Campo | Uso |
|-------|-----|
| `IdIntento`, `IdOutbox`, `eNCF`, `RncEmisor` | Correlación |
| `FechaHoraUtc` | Orden temporal |
| `Canal` (ECF/RFCE), `UrlAmbiente` | Contexto |
| `HttpStatus`, `DuracionMs` | Diagnóstico |
| `RequestId` / correlation | Ops |
| `TrackId` (si hubo) | Acuse |
| `CuerpoRespuesta` (truncado/sanitizado) | Evidencia |
| `ClasificacionError` (Transporte / Negocio / Ambiguo) | Política |
| `HashXmlEnviado` | Idempotencia |
| `Origen` (AutoWorker / ManualErp / SyncApi) | Quién disparó |

Retención: ≥ plazo de fiscalización interno (definir con negocio; mínimo años fiscales aplicables).

### 4.7 Visualización en el ERP

Pantalla / API de monitoreo (extensión de FE actual):

- Lista: eNCF, tipo, estado Alahia, estado DGII, intentos, próximo reintento, deadline 72 h, trackId.  
- Detalle: timeline de intentos + XML (descarga) + mensajes DGII.  
- Badges: `PendienteEnvio`, `Por vencer (<6 h)`, `Vencido`, `RequiereIntervencion`.  
- Acciones: **Reenviar ahora**, **Marcar contingencia OFV**, **Consultar trackId**, **Descargar RI**.  
- Alertas: correo/SignalR cuando entre en `RequiereIntervencion` o queden &lt; 6 h.

### 4.8 Reenvío manual

**Sí.** Casos:

- Operador ante caída larga ya recuperada (acelerar cola).  
- Tras corrección de certificado/red local.  
- Nunca para “forzar” un Rechazado XSD sin cambiar el XML.

API sugerida: `POST /api/Receipt/{eNCF}/retry` con Api-Key + auditoría `Origen=Manual`.

---

## 5. Flujo completo propuesto

### 5.1 Camino feliz + contingencia de transmisión (A/C)

```
Factura / documento origen
        ↓
Construcción FiscalDocumentoElectronico + definición e-CF
        ↓
XML + firma electrónica  →  persistir canónico (obligatorio)
        ↓
Intento de envío DGII (síncrono corto)
        ↓
    ┌─── DGII disponible + acuse ───▶ trackId → consulta → Aceptado/Rechazado
    │                                      ↓
    │                                 Notificar ERP
    │
    └─── DGII no disponible / timeout ───▶ PendienteEnvio (outbox)
                                              ↓
                                    Background Worker (backoff)
                                              ↓
                                    ¿Dentro de 72 h?
                                    ├─ Sí → reintento envío
                                    │         ├─ OK → EnProcesoDgii / terminal → ERP
                                    │         └─ fallo transporte → nuevo intento
                                    └─ No → VencidoSinEnvio → RequiereIntervencion
                                              ↓
                                         Alerta + playbook (declarar contingencia /
                                         soporte / remisión excepcional)
```

### 5.2 Camino imposibilidad de emitir (B)

```
Fallo al generar/firmar (certificado, sistema abajo)
        ↓
ERP bloquea e-CF y guía a NCF Serie B + Declaración Entrada Contingencia OFV
        ↓
Operación en papel (≤15 días)
        ↓
Declaración Salida OFV
        ↓
Dentro de 30 días: emitir e-CF de reemplazo (InformacionReferencia,
CodigoModificacion=4) → cola normal de envío a DGII (solo DGII)
```

### 5.3 Ambigüedad post-timeout

```
POST enviado, timeout sin respuesta clara
        ↓
NO asumir fracaso ni éxito
        ↓
Si hubo trackId parcial en logs → solo ConsultaResultado
Si no → marcar Ambiguo + RequiereIntervencion
        ↓
Operador / regla: consulta estado e-CF por RNC+eNCF (servicio consultaestado)
o un único re-POST del MISMO XML firmado bajo supervisión
```

---

## 6. Respuestas directas a las preguntas de diseño

| Pregunta | Respuesta |
|----------|-----------|
| ¿Cola (Queue)? | **Sí** — outbox durable en BD (preferible a cola volátil sola). |
| ¿Background Worker? | **Sí** — envío + consulta trackId. |
| ¿Cada cuánto reintentar? | Backoff 1m…1h (configurable); mientras quede ventana 72 h. |
| ¿Cuántos intentos antes de intervención? | Priorizar **deadline 72 h**; soft-cap de intentos; luego `RequiereIntervencion`. |
| ¿Evitar doble envío? | Único trabajo por eNCF; hash XML; no recepcionar si ya hay trackId. |
| ¿Registrar intentos? | Tabla de auditoría por intento (ver §4.6). |
| ¿Auditoría qué guardar? | Meta + hash + HTTP + trackId + clasificación + origen. |
| ¿ERP? | Monitoreo FE con timeline, deadlines y acciones. |
| ¿Reenvío manual? | **Sí**, auditado, idempotente. |

---

## 7. Qué hacer si vence el plazo sin envío

1. Estado `VencidoSinEnvio` / `RequiereIntervencion`.  
2. Notificación inmediata a roles fiscales.  
3. Playbook:
   - Si el e-CF **sí** se generó: evaluar remisión excepcional + evidencia de intentos (auditoría) y contacto DGII / procedimiento interno.  
   - Valorar si el caso debió escalar a **contingencia OFV** (B) o si fue **contingencia DGII** prolongada (C → reportes OFV a los 15 días hábiles).  
4. **No** borrar el XML firmado ni el historial.  
5. La venta comercial puede haber entregado RI de contingencia; la regularización fiscal sigue siendo responsabilidad del contribuyente con soporte de la plataforma.

---

## 8. Alcance por fase (cuando se implemente)

| Fase | Entrega |
|------|---------|
| **P0** | Persistencia canónica XML firmado + outbox + worker + estados + auditoría intentos + deadline 72 h |
| **P1** | Consulta trackId automática, UI ERP monitoreo, reenvío manual, alertas |
| **P2** | Playbook escenario B (papel + reemplazo cod. 4), detector “DGII down prolongada”, métricas SLO |
| **P3** | RI automática con leyenda de contingencia; dashboards de cola |

---

## 9. Relación con el motor de definiciones

- Contingencia **no** cambia reglas XSD por tipo: las definiciones E31–E34 siguen siendo la fuente de estructura.  
- El worker solo transmite el XML ya construido/firmado.  
- Excepción documentada: e-CF de **reemplazo por contingencia papel** usa `InformacionReferencia` + `CodigoModificacion=4` (ya previsto en formato DGII) — documentar en el DoD del tipo cuando se implemente.

---

## 10. Referencias internas

- Definiciones / DoD: `docs/dgii-ecf/definiciones/`  
- Instructivo contingencia: `docs/dgii-ecf/txt/Instructivo-Contingencia-FE.txt`  
- Informe técnico §19: `docs/dgii-ecf/txt/Informe Técnico e-CF v1.0.txt`  
- Servicios DGII: `docs/dgii-ecf/txt/Descripcion Tecnica Servicios DGII.txt`  
- Outbox existente ERP: `EcfGatewayOutboxProcessor` (patrón a reutilizar/adaptar)

---

## 11. Decisión pendiente de negocio (no bloquea el diseño)

1. ¿El reloj de 72 h corre desde `FechaEmision`, `FechaHoraFirma` o primer fallo de red?  
   **Recomendación técnica:** desde `FechaHoraFirma` del e-CF offline (más alineado a “emitido offline”).  
2. ¿Quién declara contingencia OFV (escenario B): usuario ERP vs proceso automático sugerido?  
3. Retención legal de XML e intentos (años).

---

**Siguiente paso (cuando se autorice implementación):** diseñar modelo de datos (`OutboxEnvioEcf`, `EnvioEcfIntento`) y contratos API de monitoreo/retry sobre este documento, sin alterar el motor genérico de definiciones XML.
