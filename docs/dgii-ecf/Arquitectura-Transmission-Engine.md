# Transmission Engine — Motor de Transmisión

**Proyecto:** Alahia.eCF.Api / ecosistema Alahia  
**Fecha:** 2026-07-19  
**Estado:** Decisión de arquitectura (sin implementación)  
**Principio rector:** [ALAHIA-TE-01](./Principio-Transmission-Engine-Estrategico.md) — entrega confiable a cualquier destino externo  

**Complementa:**  
- [Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md)  
- [Arquitectura-Contingencia-Reintentos.md](./Arquitectura-Contingencia-Reintentos.md)  
- [definiciones/](./definiciones/)

---

## 1. Veredicto

**Sí: el Transmission Engine como dueño de cola/reintentos/auditoría mejora la arquitectura.**

**Visión obligatoria desde el diseño (aunque hoy solo exista DGII):** el Engine es un **motor de transmisión genérico**. No conoce la DGII. Cada autoridad (u otro destino) es un **Provider/Adaptador**.

Detalle de desacople, acoplamientos detectados y contratos:  
→ **[Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md)**

Hoy la orquestación (`ReceiptOrchestrator` + gateway) mezcla enrutamiento, HTTP y (futuro) resiliencia. Eso se corrige así:

| Pieza | Rol |
|-------|-----|
| Definiciones / `EcfXmlEngine` | Motor XML (específico e-CF RD) |
| `XmlSigner` | Motor de Firma |
| **Transmission Engine** | Cola, retry, idempotencia, auditoría, alertas, políticas **genéricas** |
| **DgiiProvider** (único hoy) | Auth, send, query, mapear respuestas DGII |
| Contingencia 72 h / OFV | Config/perfil **DGII**, no constantes del núcleo del Engine |

---

## 2. Pipeline canónico

```
ERP
 │  "Emitir e-CF" (operación de negocio RD)
 ▼
Facade / Orchestrator delgado
 ▼
Motor XML (definiciones e-CF)     — NO red
 ▼
Motor de Firma                    — NO red; no mutar XML post-firma
 ▼
Transmission Engine               — genérico; NUNCA modifica el payload
 ▼
ITransmissionProvider (DGII hoy)
```

**ERP:** disponibilidad del organismo **no** es un retry manual en el caso feliz → estados del Engine (`PendingRetry`, `Accepted`, …).

---

## 3. Responsabilidades

### 3.1 Transmission Engine — SÍ (genérico)

- Cola / outbox, workers, backoff configurable.
- Idempotencia `(ProviderCode, TaxpayerId, DocumentId)` + hash payload.
- Auditoría de intentos.
- Estados internos **canónicos**.
- Consulta de estado **si** el provider `SupportsStatusQuery` (referencia opaca `ProviderReceiptId`).
- Alertas y recuperación / reenvío manual.
- Aplicar **deadline del perfil del provider** (no “conocer” el artículo de ley).

### 3.2 Transmission Engine — NUNCA

- Modificar el payload firmado.
- Reglas XSD / firma / montos / ITBIS.
- Tipos o APIs con nombres `TrackId`, `eNCF`, `RFCE`, `OFV` en el núcleo.
- Importar clientes HTTP de un organismo concreto.

### 3.3 Provider (ej. DGII) — SÍ

- Autenticación, envío, consulta, interpretación de respuestas.
- Canales propios (`ECF`/`RFCE`), nombre de archivo, URLs, ambiente.
- Mapear a `ProviderSendResult` / `ProviderStatusResult` canónicos.
- Exponer opciones en `TransmissionProviders:DGII`.

### 3.4 Facade “Emitir e-CF”

1. Request → modelo fiscal.  
2. XML + firma.  
3. Armar `TransmissionPackage` (`ProviderCode=DGII`, …).  
4. `engine.SubmitAsync(package)`.  
5. Responder estado Alahia (+ `ProviderReceiptId` si ya hubo acuse).

---

## 4. Artefacto y canales

Ver package genérico en [Providers §4](./Arquitectura-Transmission-Providers.md).

**Canal ECF/RFCE:** lo decide la facade/dominio e-CF (umbral E32); viaja como `ProviderChannel` opaco. El Engine no calcula el umbral 250k.

---

## 5. Configuración

Dos niveles (ejemplo completo en doc Providers):

- `TransmissionEngine` — workers, retry genérico, alertas genéricas.  
- `TransmissionProviders:DGII` — `DeadlineHours: 72`, status query, HTTP, ambiente, hints OFV.

---

## 6. Integración con lo construido

```
…/Transmission/                 ← Engine genérico
…/Transmission/Providers/Dgii/  ← gateway actual vive aquí
…/DgiiDirecto/Definitions/      ← Motor XML (sin Transmission genérico dentro)
```

Outbox ERP (`EcfGatewayOutboxProcessor`) ≠ outbox del Engine: dos niveles (ERP “emitir” vs Engine “entregar payload al provider”).

---

## 7. API del Engine

Contratos desacoplados en el doc Providers.  
Monitoreo ERP lee store del Engine; “forzar query” llama al provider vía Engine, no con APIs DGII en el controller.

---

## 8. Checklist y resumen

Antes de P0: cumplir checklist de desacople en [Providers §10](./Arquitectura-Transmission-Providers.md).  
Implementación inicial: Engine + **FakeProvider** (tests) + **DgiiProvider** (prod/testecf).

| Pregunta | Respuesta |
|----------|-----------|
| ¿Engine dueño de resiliencia? | **Sí** |
| ¿Engine conoce DGII? | **No** — solo el Provider |
| ¿XML/payload inmutable? | **Sí** |
| ¿Políticas hardcodeadas? | **No** — config Engine + perfil Provider |
| ¿Listo multi-país mañana? | **Sí**, si se implementa con contratos Providers |

**No implementar aún** hasta validar este desacople en el diseño de interfaces.
