# Transmission Engine — Providers multi-destino (visión largo plazo)

**Fecha:** 2026-07-19  
**Estado:** Decisión de arquitectura (sin implementación)  
**Principio rector:** [ALAHIA-TE-01](./Principio-Transmission-Engine-Estrategico.md)  
**Padre:** [Arquitectura-Transmission-Engine.md](./Arquitectura-Transmission-Engine.md)  
**Normativa RD (solo Provider DGII):** [Arquitectura-Contingencia-Reintentos.md](./Arquitectura-Contingencia-Reintentos.md)

---

## 1. Veredicto

**La idea es correcta y debe nacer así desde el día 1.**  
El diseño actual del Engine **aún no está lo suficientemente desacoplado**: varias interfaces y políticas están conceptualmente atadas a la DGII. Hay que **corregir el contrato público antes de implementar**, sin construir aún SAT/SUNAT/SII.

```
Transmission Engine          ← genérico (cola, retry, auditoría, políticas)
        │
        ├── ITransmissionProvider  "DGII"
        ├── ITransmissionProvider  "SAT"     (futuro)
        ├── ITransmissionProvider  "SUNAT"   (futuro)
        ├── ITransmissionProvider  "SII"     (futuro)
        └── …
```

Hoy solo existirá el **DGII Provider**. El Engine no debe importar tipos, URLs, trackId, RFCE ni “72 horas” como constantes de dominio DGII.

---

## 2. Acoplamientos DGII detectados en el diseño actual

| # | Acoplamiento en docs/contratos actuales | Por qué rompe multi-provider | Corrección antes de implementar |
|---|------------------------------------------|------------------------------|----------------------------------|
| 1 | API habla de `trackId` como concepto del Engine | Es jerga DGII; otros usan UUID, folio, `numeroTicket`, etc. | Engine: `ExternalReference` / `ProviderReceiptId` (opaco) |
| 2 | Idempotencia `(RncEmisor, eNCF)` | RNC/eNCF son RD; México/Perú/Chile usan otros IDs | Clave: `(ProviderCode, TaxpayerId, DocumentId)` |
| 3 | `CanalTransporte: ECF \| RFCE` en el package del Engine | Canales DGII; SAT tiene CFDI/cancelación, etc. | `ProviderChannel` string/opaque decidido por facade o por el Provider |
| 4 | `NombreArchivoDgii` | Nombre de archivo DGII | `DeliveryHints` / `ProviderPayloadMetadata` opaco |
| 5 | Engine “aplica normativa 72 h / OFV / escenarios A-B-C” | Normativa **dominicana**; no pertenece al núcleo genérico | **ProviderPolicyProfile** o config **por provider** (`Providers:DGII:…`) |
| 6 | `TrackIdPolling` en config raíz del Engine | Asume modelo async DGII | Capacidad del provider: `SupportsAsyncStatusQuery` + perfil de polling **por provider** |
| 7 | “Mapeo a estados DGII” dentro del Engine | Estados oficiales varían | Provider traduce → **estados canónicos del Engine** |
| 8 | Folder plan: Engine bajo `DgiiDirecto/Transmission` | Sugiere ownership DGII | `Transmission/` genérico + `Providers/Dgii/` |
| 9 | `GetStatusAsync(rnc, encf)` | Nombres RD | `GetStatusAsync(providerCode, taxpayerId, documentId)` o `jobId` |
| 10 | Contingencia doc → “dueño Engine” sin matiz | OK operacionalmente, pero la **semántica** 72 h es del perfil DGII | Engine ejecuta `DeadlineHours` del perfil del provider; el doc DGII alimenta solo `Providers:DGII` |

Ninguno impide el pipeline XML→Firma→Submit; todos se arreglan **renombrando contratos y moviendo políticas al perfil del provider**.

---

## 3. Modelo objetivo

### 3.1 Qué es genérico (Engine)

- Outbox / claim / workers  
- Backoff, max attempts, sync attempt  
- Idempotencia por clave canónica  
- Auditoría de intentos  
- Estados **internos** canónicos  
- Alertas genéricas (`ApproachingDeadline`, `RequiresIntervention`)  
- Recuperación y reenvío manual  
- Selección del provider por `ProviderCode`  
- **No** conoce HTTP, auth, XSD del organismo, ni nombres locales de campos

### 3.2 Qué es específico (Provider)

| Capacidad | DGII (hoy) | Otros (mañana) |
|-----------|------------|----------------|
| Auth | Semilla + token | OAuth, certificados, etc. |
| Send | `recepcion` / `recepcionfc` | Timbrado, SOAP, REST… |
| Query status | consulta `trackId` | Según organismo |
| Interpretar respuesta | Aceptado / Rechazado / En proceso | Mapear a resultado canónico |
| Naming archivo / multipart | `{RNC}{eNCF}.xml` | Lo que exija el organismo |
| Perfil normativo | Deadline 72 h, hints OFV | Plazos locales en su config |

### 3.3 Diagrama

```
┌──────────────────────────────────────────────┐
│         Transmission Engine (genérico)       │
│  Submit / Retry / Outbox / Audit / Alerts    │
└─────────────────────┬────────────────────────┘
                      │ ITransmissionProvider
        ┌─────────────┼─────────────┬──────────────┐
        ▼             ▼             ▼              ▼
   DgiiProvider   SatProvider  SunatProvider   SiiProvider
   (auth/send/    (futuro)     (futuro)        (futuro)
    query/map)
```

---

## 4. Contratos públicos recomendados (pre-implementación)

### 4.1 Package genérico

```text
TransmissionPackage
├── ProviderCode              // "DGII" | "SAT" | …
├── TaxpayerId                // RNC, RFC, RUC, …
├── DocumentId                // eNCF, UUID fiscal, …
├── DocumentTypeHint          // opcional, informativo (E32, Factura, …)
├── ProviderChannel           // opaco: "ECF" | "RFCE" | "CFDI" | …
├── Payload                   // bytes inmutables (XML firmado u otro)
├── PayloadContentType        // "application/xml", …
├── PayloadSha256
├── IssuedAtUtc               // ancla de deadline (antes FechaHoraFirma)
├── CorrelationId, TenantId, SourceDocumentId
└── ProviderMetadata          // diccionario opaco (nombre archivo, ambiente, …)
```

### 4.2 Resultado canónico del Provider → Engine

```text
ProviderSendResult
├── Outcome: AcceptedImmediate | AcceptedPending | RejectedBusiness | TransientFailure | Ambiguous
├── ProviderReceiptId?        // trackId u otro (opaco)
├── ProviderStatusCode?       // string/int opaco
├── ProviderStatusLabel?      // "Aceptado", …
├── Messages[]                // texto para auditoría/ERP
└── RawResponseRef?           // id/blob sanitizado
```

```text
ProviderStatusResult
├── Outcome: Pending | Accepted | AcceptedConditional | Rejected | NotFound | Unknown
├── … (mismos campos de referencia/mensajes)
```

El Engine solo ramifica por `Outcome` canónico:

| Outcome | Acción del Engine |
|---------|-------------------|
| `TransientFailure` | Reintento según política genérica + deadline del **perfil del provider** |
| `AcceptedPending` | Guardar `ProviderReceiptId`; programa consultas si `SupportsStatusQuery` |
| `AcceptedImmediate` / `Accepted` | Terminal éxito |
| `RejectedBusiness` | Terminal; **no** reintentar el mismo payload |
| `Ambiguous` | `RequiresIntervention` |
| `NotFound` (en query) | Seguir poll o intervenir según perfil |

### 4.3 Interface del Provider

```csharp
// Pseudocontrato — no implementar aún
interface ITransmissionProvider
{
    string ProviderCode { get; }

    Task<ProviderSendResult> SendAsync(
        TransmissionPackage package, CancellationToken ct);

    Task<ProviderStatusResult> QueryStatusAsync(
        TransmissionPackage package,
        string providerReceiptId,
        CancellationToken ct);

    // Opcional: si el organismo no tiene query async, retornar false
    bool SupportsStatusQuery { get; }
}
```

Registro: `ITransmissionProviderResolver.Get("DGII")`.

### 4.4 API del Engine (desacoplada)

```csharp
interface ITransmissionEngine
{
    Task<TransmissionAcceptResult> SubmitAsync(TransmissionPackage package, CancellationToken ct);

    Task<TransmissionJobStatus> GetStatusAsync(Guid jobId, CancellationToken ct);
    // overload: (providerCode, taxpayerId, documentId)

    Task<TransmissionAcceptResult> RetryManualAsync(Guid jobId, string requestedBy, CancellationToken ct);
}
```

Eliminar de la API pública del Engine: `trackId`, `rnc`, `encf`, `RFCE`, `OFV` como tipos de primer nivel.

---

## 5. Configuración en dos niveles

```json
{
  "TransmissionEngine": {
    "MaxWorkers": 2,
    "BatchSize": 10,
    "SyncAttemptEnabled": true,
    "SyncTimeoutSeconds": 45,
    "Retry": {
      "InitialDelaySeconds": 60,
      "BackoffMultiplier": 2.0,
      "MaxDelaySeconds": 3600,
      "JitterPercent": 20,
      "MaxAttempts": 40
    },
    "Alerts": {
      "OnRequiresIntervention": true,
      "OnApproachingDeadline": true
    }
  },
  "TransmissionProviders": {
    "DGII": {
      "Enabled": true,
      "DeadlineHours": 72,
      "AlertBeforeDeadlineHours": 6,
      "StatusQuery": {
        "Enabled": true,
        "InitialDelaySeconds": 5,
        "IntervalSeconds": 15,
        "MaxPolls": 40,
        "GiveUpAfterMinutes": 30
      },
      "HttpTimeoutSeconds": 60,
      "Ambiente": "testecf",
      "Hints": {
        "OfvAfterProviderOutageBusinessDays": 15
      }
    }
  }
}
```

- **Engine:** mecánicas universales.  
- **TransmissionProviders:DGII:** plazos y particularidades de República Dominicana (contenido del doc de contingencia).  
- Futuro: `TransmissionProviders:SAT`, etc., sin tocar código del Engine.

---

## 6. Estados internos canónicos (Engine)

Independientes del organismo:

`Queued` · `Sending` · `PendingProvider` · `Accepted` · `AcceptedConditional` · `Rejected` · `TransientPendingRetry` · `DeadlineExceeded` · `RequiresIntervention`

El Provider DGII mapea `Aceptado`/`Rechazado`/`EnProceso` → estos valores.  
Otros providers mapean los suyos.

---

## 7. Dónde vive la contingencia DGII

| Capa | Rol |
|------|-----|
| Doc `Arquitectura-Contingencia-Reintentos.md` | Fuente de verdad **normativa RD** |
| `TransmissionProviders:DGII` | Plazos/hints operativos |
| `DgiiProvider` | Auth, send ECF/RFCE, query trackId, mapear respuestas |
| **Engine** | Ejecuta deadline/alert/retry **sin saber que son “artículos 40–43”** |

Escenario B (papel + OFV) sigue siendo **proceso de negocio/ERP + Provider DGII (documentos de reemplazo)**, no lógica hardcodeada del núcleo del Engine.

---

## 8. Estructura de carpetas (corregida)

```
…/Transmission/                    ← genérico
    Engine/
    Outbox/
    Workers/
    Audit/
    Abstractions/                  ← ITransmissionProvider, Package, Outcomes
    Options/                       ← TransmissionEngineOptions

…/Transmission/Providers/
    Dgii/                          ← único provider hoy
      DgiiTransmissionProvider.cs
      DgiiAuth / Recepcion / Consulta (gateway actual)
      DgiiProviderOptions.cs
```

No anidar el Engine genérico dentro de `DgiiDirecto` como dueño; `DgiiDirecto` puede alimentar el provider o moverse bajo `Providers/Dgii`.

---

## 9. Impacto en el pipeline Alahia (RD)

Sin cambio de experiencia ERP:

```
ERP "Emitir e-CF"
  → Motor XML (definiciones DGII/e-CF)
  → Motor Firma
  → Package { ProviderCode=DGII, TaxpayerId=RNC, DocumentId=eNCF, Channel=ECF|RFCE, … }
  → Transmission Engine
  → DgiiProvider
```

El Motor XML sigue siendo específico e-CF/DGII (correcto: es documento fiscal RD).  
El Engine no lo es.

---

## 10. Checklist pre-implementación (obligatorio)

- [ ] Contratos del Engine **sin** nombres `TrackId` / `eNCF` / `Rnc` / `RFCE` como API pública  
- [ ] `ITransmissionProvider` + `ProviderSendResult` canónico  
- [ ] Config partida: `TransmissionEngine` vs `TransmissionProviders:DGII`  
- [ ] Deadline/polling leídos del perfil del provider  
- [ ] Carpetas `Transmission/` + `Providers/Dgii/`  
- [ ] Doc contingencia etiquetado como **perfil DGII**, no como núcleo del Engine  
- [ ] Primer provider: solo DGII; tests del Engine con **fake provider** (sin red)

---

## 11. Resumen

| Pregunta | Respuesta |
|----------|-----------|
| ¿El Engine actual ya está desacoplado? | **No del todo** — ver tabla §2 |
| ¿Hay que esperar a SAT para arreglarlo? | **No** — corregir contratos **antes** del P0 |
| ¿Quién conoce la DGII? | Solo **DgiiProvider** (+ Motor XML/Firma e-CF) |
| ¿Dónde van las 72 h? | `TransmissionProviders:DGII:DeadlineHours` |
| ¿Se implementa multi-país ahora? | **No** — solo la abstracción y el provider DGII |

Con esto, el Transmission Engine nace reutilizable; agregar un organismo nuevo = nuevo Provider + bloque de config, **sin modificar el Engine**.
