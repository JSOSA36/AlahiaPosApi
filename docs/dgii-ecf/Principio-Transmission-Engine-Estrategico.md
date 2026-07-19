# Principio arquitectónico — Transmission Engine como componente estratégico Alahia

**Código:** `ALAHIA-TE-01`  
**Fecha:** 2026-07-19  
**Estado:** Principio vigente (visión de largo plazo; implementación por fases)  
**Ámbito:** Ecosistema Alahia (Alahia.eCF.Api, AlahiaPosApi y futuros servicios)

---

## Enunciado

> El **Transmission Engine** no existe para “enviar XML a la DGII”.  
> Existe para **garantizar la entrega confiable de documentos críticos hacia cualquier proveedor externo**.

Es un **componente estratégico reutilizable** del ecosistema Alahia.  
La DGII es solo el **primer Provider**. El Engine no nace ni evoluciona atado a un organismo.

---

## Separación inviolable

| Transmission Engine (genérico) | Providers (específicos) |
|--------------------------------|-------------------------|
| Cola / outbox | Autenticación |
| Reintentos y backoff | Protocolos y endpoints |
| Idempotencia | Formatos de payload |
| Auditoría de intentos | Interpretación de respuestas |
| Monitoreo y estados canónicos | Estados propios del destino |
| Recuperación y reenvío manual | Particularidades normativas |
| Alertas y workers | |
| Políticas genéricas (config) | Perfil/config del provider |

**Regla de oro:** agregar un destino nuevo = **nuevo Provider + configuración**.  
**El Engine no se modifica** para soportar ese destino.

---

## Destinos conceptuales (no limitativos)

Hoy: **DGII Provider** (e-CF / RFCE).

Mañana, sin cambiar el Engine, por ejemplo:

- Otras administraciones tributarias (SAT, SUNAT, SII, …)
- Bancos u operadores de pago
- Integradores / hubs fiscales
- Cualquier API externa donde la **entrega confiable** importe

El payload puede ser XML, JSON u otro; el Engine solo ve **bytes inmutables + metadatos canónicos**.

---

## Consecuencias de diseño

1. APIs e interfaces del Engine **sin** jerga de un organismo (`TrackId`, `eNCF`, `RNC`, etc. como tipos de primer nivel).  
2. Identidad del trabajo: `(ProviderCode, TaxpayerId | PartyId, DocumentId)` + hash del payload.  
3. Config en dos niveles: `TransmissionEngine` (universal) vs `TransmissionProviders:{Code}` (particular).  
4. Normativa local (p. ej. 72 h DGII) → **perfil del Provider**, documentada aparte.  
5. Motores de documento (XML e-CF, firma, definiciones) viven **aguas arriba**; el Engine no construye ni altera el documento.

---

## Relación con otros principios Alahia

| Principio / área | Relación |
|------------------|----------|
| Motor de definiciones e-CF | Construye el documento RD; no transmite |
| DoD de comprobantes | Calidad del documento ≠ entrega; ambos deben cumplirse |
| Contingencia DGII | Alimenta solo el Provider DGII |
| Gateway / adaptadores | Son Providers o parte de un Provider |

---

## Documentos de detalle

| Documento | Rol |
|-----------|-----|
| [Arquitectura-Transmission-Engine.md](./Arquitectura-Transmission-Engine.md) | Pipeline y responsabilidades del Engine |
| [Arquitectura-Transmission-Providers.md](./Arquitectura-Transmission-Providers.md) | Contratos Provider, desacople, checklist pre-implementación |
| [Arquitectura-Contingencia-Reintentos.md](./Arquitectura-Contingencia-Reintentos.md) | Normativa RD → perfil DGII |

---

## Compromiso de evolución

Durante muchos años, la plataforma puede crecer en **documentos** y en **destinos**.  
El Transmission Engine debe seguir siendo el mismo núcleo de resiliencia.

Cualquier PR o diseño que meta lógica de un organismo concreto en el core del Engine **viola este principio** y debe rechazarse o moverse a un Provider.
