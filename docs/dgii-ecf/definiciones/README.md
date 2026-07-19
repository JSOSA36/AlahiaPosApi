# Definiciones e-CF — biblioteca de conocimiento

El XML Builder es un **motor genérico**. La lógica por tipo vive en definiciones C# + **esta documentación**.

El conocimiento del proyecto **no** debe depender de la memoria del equipo: evoluciona con la API.

## Principios

1. Un tipo = `EcfXXDefinition` + `EXX.md` + ejemplo(s) XML + historial.
2. **Aceptación DGII ≠ terminado.** Falta el DoD completo (abajo).
3. Nunca resolver un caso con `if (TipoeCF == …)` en el Builder/motor.
4. Toda regla nueva de certificación → **`Validar` en la definición + documentación `EXX.md` el mismo día**.
5. El Motor de Definiciones es la **única fuente de verdad** de validación fiscal (`IFiscalDocumentoValidator` → `def.Validar`). ERP y Alahia.eCF.Api lo invocan antes de enviar a DGII.
6. En el futuro, entender un comprobante = leer solo su carpeta de conocimiento.

## Definition of Done (obligatorio)

Un comprobante queda **Completo** solo si tiene:

| # | Entregable | Ubicación |
|---|------------|-----------|
| 1 | Definición del comprobante | `Definitions/EcfXXDefinition.cs` + registry |
| 2 | Documentación técnica actualizada | `EXX.md` (plantilla) |
| 3 | Reglas de negocio documentadas | sección en `EXX.md` |
| 4 | XML de ejemplo (estructura de envío aceptado) | `ejemplos/EXX-*.xml` |
| 5 | Casos de prueba | sección en `EXX.md` |
| 6 | Historial de certificación | sección en `EXX.md` |
| 7 | Errores de certificación y resolución | sección en `EXX.md` |
| 8 | Restricciones y validaciones especiales | sección en `EXX.md` |

Plantilla: [TEMPLATE.md](./TEMPLATE.md)

## Flujo del motor

1. TipoeCF → 2. Registry → 3. Validar → 4. Emitir nodos permitidos → 5. PostProcesar → 6. XML UTF-8 sin BOM

## Cómo agregar / cerrar un tipo

1. Copiar `TEMPLATE.md` → `EXX.md`.
2. Implementar y registrar `EcfXXDefinition`.
3. Probar (smoke + testecf).
4. Guardar XML de ejemplo en `ejemplos/` y enlazarlo.
5. Documentar casos de prueba, errores y historial.
6. Marcar checklist DoD en el `.md` y estado **Completo**.

Código: `AlahiaPos.DataAccess/Servicios/FiscalGateway/DgiiDirecto/Definitions/`

## Tipos

| Tipo | Doc | Clase | Estado DoD |
|------|-----|-------|------------|
| 31 | [E31.md](./E31.md) | `Ecf31Definition` | Completo |
| 32 | [E32.md](./E32.md) | `Ecf32Definition` | Completo |
| 33 | [E33.md](./E33.md) | `Ecf33Definition` | Completo |
| 34 | [E34.md](./E34.md) | `Ecf34Definition` | Completo |
| 41 | [E41.md](./E41.md) | `Ecf41Definition` | Completo |
| 43 | [E43.md](./E43.md) | `Ecf43Definition` | Completo |
| 44 | [E44.md](./E44.md) | `Ecf44Definition` | Completo |
| 45 | [E45.md](./E45.md) | `Ecf45Definition` | Completo |
| 46 | [E46.md](./E46.md) | `Ecf46Definition` | Completo |
| 47 | [E47.md](./E47.md) | `Ecf47Definition` | Completo |

Pendientes: ninguno en el set XSD del repo (31–34, 41, 43–47). Todos certificados.

## Ejemplos XML

Ver [ejemplos/](./ejemplos/). Son estructura generada por el motor alineada a eNCF aceptados en testecf (sin bloque `Signature`; la firma se agrega en runtime).
