# EXX — [Nombre oficial del comprobante]

**TipoeCF:** XX  
**Clase:** `EcfXXDefinition`  
**XSD:** `e-CF XX v.1.0.xsd`  
**Estado DoD:** Incompleto | En certificación | **Completo**

> Un comprobante **no** se considera terminado solo porque la DGII lo acepte.  
> Ver checklist DoD en [README.md](./README.md).

## Propósito

[Para qué se usa según DGII / negocio Alahia. Canal de envío.]

## Campos obligatorios

| Sección | Campo | Notas |
|---------|-------|-------|
| | | |

## Campos opcionales

| Sección | Campo | Notas |
|---------|-------|-------|
| | | |

## Campos prohibidos

| Sección | Campo | Motivo |
|---------|-------|--------|
| | | XSD / rechazo DGII / regla de negocio |

## Restricciones del XSD

- Archivo XSD, orden de nodos, `minOccurs`, formatos (eNCF, RNC, fechas).

## Reglas de negocio de la DGII

- Canal, umbrales, dependencias entre campos, referencias a otros NCF.

## Restricciones y validaciones especiales

- Validaciones del motor / definición (`Validar`, Presence, dependencias).
- Quirks de ambiente (testecf / certecf / prod).

## Casos especiales

- Flujos del orquestador (RFCE, etc.) **sin** meter lógica en el XML Builder.

## XML de ejemplo (aceptado / estructura validada)

| Archivo | eNCF / trackId (si aplica) | Ambiente | Notas |
|---------|----------------------------|----------|-------|
| [ejemplos/EXX-….xml](./ejemplos/) | | testecf | Sin firma en el archivo de conocimiento; estructura = envío aceptado |

## Casos de prueba

| ID | Escenario | Entrada clave | Resultado esperado |
|----|-----------|---------------|--------------------|
| EXX-T01 | Happy path | … | Aceptado |
| EXX-T02 | Campo prohibido presente en DTO | … | Nodo omitido / rechazo local |
| EXX-T03 | Falta obligatorio | … | Error de validación |

## Errores de certificación y resolución

| Fecha | Error / síntoma | Causa raíz | Resolución | ¿En definición? | ¿En este doc? |
|-------|-----------------|------------|------------|-----------------|---------------|
| | | | | Sí/No | Sí/No |

## Historial de certificación

| Fecha | Evento | Ambiente | Resultado | Evidencia |
|-------|--------|----------|-----------|-----------|
| | Primera aceptación / regresión / cambio de regla | testecf | Aceptado/Rechazado | eNCF, trackId |

## Checklist DoD (marcar al cerrar el tipo)

- [ ] Definición C# + registry
- [ ] Este documento completo (todas las secciones)
- [ ] Reglas de negocio documentadas
- [ ] XML de ejemplo en `ejemplos/`
- [ ] Casos de prueba
- [ ] Historial de certificación
- [ ] Errores encontrados + resolución
- [ ] Restricciones / validaciones especiales

## Referencias

- Clase: `…/Definitions/EcfXXDefinition.cs`
- Índice: [README.md](./README.md)
