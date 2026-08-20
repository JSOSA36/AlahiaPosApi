# Payroll Engine Specification v1.0

**Código:** `ALAHIA-PE-01`  
**Documento:** Payroll Engine Specification  
**Versión:** 1.0  
**Fecha:** 2026-08-02  
**Estado:** Constitución vigente — normativa  
**Ámbito:** Motor de nómina de Alahia ERP (`AlahiaPos.Payroll.*`)

---

## 0. Naturaleza de este documento

Este documento es la **constitución** del Payroll Engine.

No es un tutorial. No es una guía de implementación de UI. No describe tablas SQL ni endpoints HTTP.

Define:

- Qué está **permitido**.
- Qué está **prohibido**.
- Qué principios **nunca** deben romperse.
- Cómo **extender** el motor correctamente.

Cualquier diseño de persistencia, API, RulePack o UI que contradiga esta especificación es **inválido**, aunque “funcione”.

Cambios a esta constitución requieren versión explícita del documento (`v1.1`, `v2.0`, …) y justificación de producto.

---

## 1. Propósito y visión

### 1.1 Propósito

Construir un **motor de cálculo de nómina** reutilizable, extensible y desacoplado, estable a largo plazo (horizonte 10 años).

No se construye “una nómina para República Dominicana”.  
Se construye un **núcleo agnóstico** donde cada jurisdicción es un **RulePack instalable**.

### 1.2 Criterio de éxito

Dentro de cinco o diez años, un desarrollador debe poder:

- agregar un país,
- agregar una regla legal,
- agregar un concepto,

**sin modificar el núcleo (`Core`)**.

Si el Core debe cambiar para soportar un país nuevo, la arquitectura ha fallado.

---

## 2. Decisiones congeladas (v1.0)

Las siguientes decisiones son **definitivas** para la v1.0 del motor:

| # | Decisión |
|---|----------|
| D1 | Core completamente agnóstico al país / jurisdicción |
| D2 | Legislación solo mediante RulePacks desacoplados |
| D3 | `EvaluationContext` inmutable durante el cálculo |
| D4 | Calculadoras sin acceso a base de datos |
| D5 | Calculadoras sin llamadas HTTP ni I/O externo |
| D6 | Snapshot completo e inmutable del `PayrollRun` al aprobar |
| D7 | Contabilidad **fuera** del motor |
| D8 | Integración con el resto del ERP mediante **eventos de dominio** |
| D9 | Ejecución oficial mediante **grafo de dependencias (DAG)** |
| D10 | Unidad de extensión = **concepto** (`IConceptCalculator`) |
| D11 | Obligaciones distintas = **conceptos independientes** en el grafo |
| D12 | Un calculator puede emitir **múltiples líneas** vía `ConceptCalculationResult` |
| D13 | Maestro de personas = `EmpleadosP` existente; no existe `RH_Empleado` paralelo |
| D14 | **Multiempresa:** todo run/contexto/asignación/pack activo/config contable lleva `IdEmpresa`; aislamiento estricto entre empresas |
| D15 | **ConceptCode** es contrato público estable: no se reutiliza ni cambia de significado en producción |
| D16 | **Moneda y precisión** son obligatorias en el contexto (CurrencyCode, precisión, política de redondeo); el RulePack define el manejo |
| D17 | **CalculationTrace** forma parte del snapshot aprobado (explicabilidad del cálculo) |
| D18 | **Idempotencia** de ejecución: una misma solicitud no produce múltiples runs aprobados |
| D19 | Evento **`NominaAprobada`** inmutable con payload mínimo de integración; el detalle vive en el snapshot |

---

## 3. Regla de Oro (`ALAHIA-PE-GOLDEN`)

> El núcleo del motor **nunca** conocerá conceptos legales específicos ni jurisdicciones.

### 3.1 Prohibido en Core

No existirán clases, métodos, servicios o condiciones del tipo:

- `CalcularAFP()`
- `CalcularISR()`
- `CalcularSFS()`
- `CalcularRegalia()`
- `CalcularSocialSecurity()`
- cualquier nombre que incruste legislación de un país

No existirá lógica del tipo:

```csharp
if (Pais == "DO") { ... }
if (Jurisdiction == "PA") { ... }
if (Concepto == "AFP") { ... }
if (Codigo.StartsWith("ISR")) { ... }
```

### 3.2 Único lugar de la legislación

Toda legislación vive **exclusivamente** en RulePacks (`AlahiaPos.Payroll.Rules.*`).

El Core solo:

1. Carga un contexto.  
2. Obtiene conceptos activos.  
3. Resuelve la calculadora del concepto.  
4. Ejecuta la calculadora.  
5. Genera líneas.  
6. Continúa según el DAG.

El motor **no sabe qué está calculando**. Recibe una calculadora y produce un resultado.

---

## 4. Glosario normativo

| Término | Definición |
|---------|------------|
| **Concept** | Identidad estable de un elemento liquidable (`ConceptCode`). Ej.: `SALARIO_BASE`, `AFP_EMPLEADO`. |
| **Concept version** | Configuración vigente de un concepto (motor, parámetros, vigencia). El run resuelve la versión por fecha del período. |
| **Assignment** | Vinculación concepto ↔ empleado (o grupo) con vigencia y override opcional. |
| **Fact** | Hecho ya interpretado del período (horas, tardanza, HE, cuota de préstamo, comisión). No es una marcación cruda. |
| **EvaluationContext** | Snapshot inmutable de entradas para un empleado en un run. |
| **IConceptCalculator** | Strategy que, dado un contexto (+ líneas ya resueltas de dependencias), produce un `ConceptCalculationResult`. |
| **ConceptCalculationResult** | Resultado de un calculator: una o más `PayrollLine`. |
| **PayrollLine** | Línea de liquidación (código, tipo, monto, origen, metadata de auditoría). |
| **PayrollRun** | Ejecución de nómina para un período (borrador o aprobado). |
| **RulePack** | Paquete versionado que registra conceptos legales y calculators de una jurisdicción (o variante). |
| **DAG** | Grafo dirigido acíclico de dependencias entre conceptos. |
| **Snapshot** | Congelamiento reproducible de un run aprobado (inputs, pack, líneas, traces, hash). |
| **CalculationTrace** | Explicación auditable de cómo se obtuvo una línea (base, regla, resultado, calculator, pack). |
| **ExecutionKey** | Identidad idempotente de una solicitud de cálculo/aprobación (empresa + período + intención). |

---

## 4A. Multiempresa (`IdEmpresa`) — regla de dominio

Todo el motor opera **dentro de un contexto de empresa**.

**Obligatorio** asociar a `IdEmpresa`:

- `PayrollRun`
- `EvaluationContext`
- Assignments
- RulePack activo (selección por empresa / período)
- Configuración contable Concepto → Cuenta (fuera del Core, pero siempre por empresa)

**Prohibido:**

- Mezclar empleados, hechos, líneas o totales de distintas empresas en un mismo run.
- Resolver un RulePack o parámetros de la empresa A al calcular la empresa B.

En escenario SaaS, cada tenant permanece **completamente aislado**. El Core debe tratar `IdEmpresa` como frontera de seguridad de datos, no como un campo ornamental.

---

## 4B. Estabilidad de `ConceptCode` (contrato público)

`ConceptCode` es un **contrato público** del sistema.

Una vez que un `ConceptCode` ha sido utilizado en producción (runs aprobados o integraciones):

1. **No** cambia de significado.  
2. **No** se reutiliza para otro cálculo.  
3. **No** se elimina físicamente (puede desactivarse / dejar de asignarse).

**Incorrecto**

```text
BONO   → en 2026 bono mensual; en 2028 “el mismo código” pasa a bono anual
```

**Correcto**

```text
BONO_MENSUAL
BONO_ANUAL
```

Los conceptos históricos deben permanecer auditables y reconstruibles desde snapshots.

---

## 5. Separación de proyectos (dependencias de ensamblado)

Estructura objetivo:

```text
AlahiaPos.Payroll.Abstractions
AlahiaPos.Payroll.Core
AlahiaPos.Payroll.Rules.DO
AlahiaPos.Payroll.Rules.*     (futuros países / variantes)
AlahiaPos.Payroll.Infrastructure
AlahiaPos.Payroll.API         (o host del ERP)
```

### 5.1 Responsabilidades

| Proyecto | Responsabilidad | Puede referenciar |
|----------|-----------------|-------------------|
| **Abstractions** | Contratos, records de dominio, metadata de dependencias | Nada de Payroll (solo BCL / contratos compartidos mínimos) |
| **Core** | Motor: DAG, runner, validaciones, agregación | Solo **Abstractions** |
| **Rules.\*** | Seeds de conceptos + `IConceptCalculator` de una jurisdicción | Solo **Abstractions** |
| **Infrastructure** | SQL, repositorios, carga de hechos/asignaciones hacia el contexto | Abstractions (+ stacks de datos del ERP) |
| **API / Host** | Orquestación HTTP, DI, composición de packs | Core + Rules.* + Infrastructure |

### 5.2 Reglas de dependencia (normativas)

**Permitido**

- Core → Abstractions  
- Rules.* → Abstractions  
- Infrastructure → Abstractions  
- Host → Core, Rules.*, Infrastructure  

**Prohibido**

- Core → Infrastructure  
- Core → Rules.*  
- Rules.* → Core  
- Rules.* → Infrastructure  
- Abstractions → Core / Rules / Infrastructure  

El descubrimiento de RulePacks es por **registro** (DI / composition root), nunca por referencia del Core a un país.

---

## 6. Bounded contexts alrededor del motor

```text
HR Core / EmpleadosP
        │
        ▼
Attendance (Marcacion → DiaLaboral / Facts)
        │
        ▼
Payroll Engine (esta especificación)
        │  evento NominaAprobada
        ▼
Contabilidad (mapeo Concepto → Cuenta)
```

| Contexto | Posee | No hace |
|----------|-------|---------|
| HR / Empleados | Maestro `EmpleadosP`, contrato, asignaciones | Calcular neto ni impuestos |
| Attendance | Marcaciones y hechos de asistencia | Conocer ISR/AFP ni generar asientos |
| **Payroll Engine** | Runs, líneas, DAG, snapshots | Biometría, SQL en calculators, asientos |
| Contabilidad | Asientos tras evento | Reglas laborales |
| Biometrics (adapter) | Verify result + provider ids | Plantillas o imágenes faciales |

---

## 7. Contratos conceptuales del dominio

Los nombres siguientes son **normativos** para la v1.0. La firma exacta en C# vivirá en `AlahiaPos.Payroll.Abstractions` y no podrá contradecir esta sección.

### 7.1 `IPayrollEngine`

Orquesta un run:

1. Recibe un pedido de liquidación (período, empleados, RulePack activo).  
2. Solicita construcción de contextos (vía puertos / providers **fuera** de las calculadoras).  
3. Ejecuta el pipeline DAG.  
4. Produce un `PayrollRun` (borrador).  
5. En aprobación: congela snapshot y emite evento de dominio.

### 7.2 `IPayrollRulePack`

Contrato de un pack instalable. Debe exponer al menos:

- `PackId` (ej. `DO-2026.01`)  
- `JurisdictionCode` (informativo; el Core no ramifica por él)  
- `Version`  
- Registro de `IConceptCalculator` (y metadata de conceptos que aporta)

### 7.3 `IConceptCalculator`

Unidad de extensión.

- Está ligado a **un** `ConceptCode` primario (el que “posee” en el grafo).  
- Declara `DependsOn: ConceptCode[]`.  
- Recibe contexto inmutable + vista de líneas ya calculadas de sus dependencias.  
- Devuelve `ConceptCalculationResult` (1..n `PayrollLine`).  
- **No** accede a BD, HTTP, repositorios, `DbContext`, ni servicios de infraestructura.

### 7.4 `ConceptCalculationResult`

Contiene `Lines: PayrollLine[]`.

Si un calculator emite líneas con `ConceptCode` distinto al primario (efecto colateral), esas líneas deben estar justificadas en el diseño del pack; la preferencia normativa v1.0 es:

> Obligaciones distintas ⇒ **conceptos independientes** y calculators separados en el grafo.

### 7.5 `EvaluationContext`

Record / objeto **inmutable** por empleado y run. Incluye, como mínimo conceptual:

- `IdEmpresa` (obligatorio; frontera multiempresa)  
- Identidad del empleado (`IdEmpleados`)  
- Datos de contrato relevantes al cálculo  
- Asignaciones vigentes  
- Hechos del período  
- Parámetros del RulePack **ya congelados** para el run  
- **Moneda y precisión (obligatorias):**  
  - `CurrencyCode`  
  - Precisión monetaria (escala decimal)  
  - Política de redondeo aplicable al cálculo  

Una vez iniciado el cálculo del empleado, el contexto no muta.

#### Moneda, precisión y redondeo (obligatorio)

Cada RulePack **debe** definir cómo manejar:

- Decimales / escala  
- Redondeo **por línea**  
- Redondeo **acumulado** (si aplica)

El Core aplica la política expuesta en el contexto/pack; no inventa redondeos ad hoc ni asume una moneda fija (p. ej. DOP) en código del núcleo.

### 7.6 Providers de hechos (fuera del calculator)

Puertos tales como:

- `IAttendanceFactProvider`  
- `IFactProvider` (préstamos, comisiones POS, etc.)

**Permitido:** leer infraestructura **antes** de armar el `EvaluationContext`.  
**Prohibido:** ser invocados desde `IConceptCalculator.Execute`.

### 7.7 `PayrollLine` / `PayrollRun` / `CalculationTrace`

- `PayrollLine`: resultado auditable (código, tipo, monto, origen, referencias).  
- `PayrollRun`: contenedor del período + `IdEmpresa` + estado + líneas + RulePack + `ExecutionKey`.  
- `CalculationTrace`: explicación del cálculo de un concepto/línea, **parte del snapshot aprobado**.

Orígenes de línea (normativos):

| Origen | Significado |
|--------|-------------|
| `ASSIGNMENT` | Derivado de asignación vigente |
| `FACT` | Derivado de un hecho del período |
| `RULE` | Emitido por calculator de RulePack / concepto |

#### CalculationTrace (explicabilidad)

Además del resultado (`AFP_EMPLEADO = 1,200`), el sistema debe poder explicar el cálculo sin intervención del desarrollador.

Un `CalculationTrace` incluye, como mínimo conceptual:

- `ConceptCode`  
- Base utilizada  
- Regla / tasa / fórmula aplicada (descripción o identificador estable)  
- Resultado  
- Nombre/identificador del calculator  
- `RulePackId` + versión  

Forma parte del **snapshot** del run aprobado.

Pregunta normativa que el producto debe poder responder:

> ¿Por qué este empleado recibió este descuento / ingreso?

---

## 7A. Idempotencia del PayrollRun

Una misma solicitud de negocio **no** puede producir múltiples `PayrollRun` aprobados.

Debe existir una **identidad de ejecución** (`ExecutionKey`) que represente la operación (como mínimo: `IdEmpresa` + período + tipo de corrida / intención).

Si el usuario genera nómina dos veces para la misma clave:

- Se reutiliza el run borrador existente, o  
- Se rechaza / se retorna el existente,  

según política del host — pero **nunca** se aprueban dos runs distintos para la misma `ExecutionKey`.

La aprobación también es idempotente respecto a esa clave.

---

## 8. Pipeline de ejecución (DAG)

### 8.1 Flujo oficial

```text
Load contexts (facts + assignments + pack params)
        │
        ▼
Resolve active concepts
        │
        ▼
Build dependency DAG
        │
        ▼
Detect cycles → fail run if any
        │
        ▼
Topological order (levels)
        │
        ▼
For each concept in order:
    resolve IConceptCalculator
    execute → ConceptCalculationResult
    append PayrollLine(s)
        │
        ▼
Aggregate (bruto, descuentos, neto, aportes, …) as projections over lines
        │
        ▼
PayrollRun (Draft)
```

No existe un orden fijo en código del tipo `CalcularSalario → CalcularAFP → CalcularISR → CalcularNeto` dentro de Core.

### 8.2 Dependencias

Cada calculator declara dependencias explícitas por `ConceptCode`.

El Core:

1. Construye el DAG.  
2. Detecta ciclos → **falla el run** (no “mejor esfuerzo”).  
3. Calcula orden topológico.  
4. Ejecuta.

El Core **no** contiene dependencias hardcodeadas entre conceptos.

### 8.3 Hard vs soft (normativa v1.0)

| Tipo | Comportamiento |
|------|----------------|
| **Hard** | Si la dependencia no produjo líneas / monto resoluble, el run **falla** para ese empleado (o el run completo, según política del host; default: falla empleado y marca error en run). |
| **Soft** | Si falta, se trata como cero / vacío y el calculator continúa. |

Toda dependencia debe declarar si es hard o soft. Si se omite, el default es **hard**.

### 8.4 Paralelización (diseño futuro, no requisito de implementación v1.0)

Conceptos en el mismo **nivel** topológico sin dependencias cruzadas podrán ejecutarse en paralelo en el futuro.

Por ello, desde v1.0:

- Calculators son **stateless** respecto a estado compartido mutable.  
- Solo leen contexto + líneas ya cerradas de dependencias.  
- No escriben estado global.

La implementación v1.0 puede ser secuencial; el diseño no debe impedir paralelismo.

---

## 9. Ciclo de vida del PayrollRun

```text
Draft
  │  recalcular (nuevo intento / reemplazo de borrador)
  ▼
Draft (reemplazado)
  │  aprobar
  ▼
Approved (bloqueado)
  │
  ├─ Snapshot congelado
  ├─ RulePackId + Version persistidos
  └─ Domain event: NominaAprobada
```

| Estado | Permitido | Prohibido |
|--------|-----------|-----------|
| **Draft** | Recalcular, descartar, inspeccionar | Emitir asiento contable definitivo como hecho cerrado |
| **Approved** | Consultar, generar recibo desde snapshot, auditar | Mutar líneas, tasas, fórmulas, o “recalcular in-place” |

Recalcular un período aprobado implica un **nuevo** proceso de negocio (reversión / run correctivo), no editar el snapshot histórico.

---

## 10. Snapshot

Al aprobar un `PayrollRun`, el sistema **debe** congelar un snapshot que permita reconstruir el resultado sin reinterpretar reglas actuales.

El snapshot incluye, como mínimo:

- Identificación del run, `IdEmpresa` y período  
- `ExecutionKey`  
- `RulePackId` + versión exacta  
- Parámetros del pack usados  
- Identidad de conceptos/versiones resueltas  
- Inputs relevantes del contexto (o hash + payload)  
- Todas las `PayrollLine`  
- Todos los `CalculationTrace` asociados  
- Totales proyectados  
- Política monetaria (currency, precisión, redondeo) usada  
- Hash de integridad del snapshot  

Aunque el empleado, las tasas legales o las fórmulas cambien después, el run aprobado **no cambia**.

---

## 11. RulePacks

### 11.1 Identificación

Formato recomendado:

```text
{JURISDICTION}-{YYYY.MM}[ -VARIANT ]
```

Ejemplos: `DO-2026.01`, `DO-2027.01`, `PA-2026.01`, `US-LITE-2026.01`, `CUSTOM-EMPRESA-001-2026.01`.

### 11.2 Asociación

Cada empresa (o período) opera con **un** RulePack activo versionado.

El `PayrollRun` aprobado almacena exactamente qué pack usó.

### 11.3 Cómo registrar un RulePack (extensión correcta)

1. Crear ensamblado `AlahiaPos.Payroll.Rules.{Code}`.  
2. Implementar `IPayrollRulePack`.  
3. Registrar calculators y metadata de conceptos.  
4. Registrar el pack en el composition root del host.  
5. **No** modificar `Core`.

### 11.4 Variantes / custom

No se permiten “reglas personalizadas” sueltas sin pack.

Lo permitido:

- Override de parámetros versionado dentro del contrato del pack, o  
- Pack derivado versionado (`CUSTOM-…`) con el mismo contrato `IPayrollRulePack`.

---

## 12. Cómo agregar un nuevo concepto

1. Definir `ConceptCode` estable.  
2. Definir versión (vigencia, tipo, parámetros).  
3. Implementar `IConceptCalculator` (en el pack o en un módulo de conceptos compartidos **sin** legislación de país, si aplica).  
4. Declarar `DependsOn` (hard/soft).  
5. Registrar en el RulePack o catálogo que el host resuelve.  
6. Si es contable: mapear Concepto → Cuenta en configuración de empresa (**fuera** del Core).  
7. Cubrir con pruebas unitarias del calculator (contexto + dependencias mockeadas).

**Prohibido:** agregar un `switch` en Core por el nuevo código.

---

## 13. Cómo agregar un nuevo país

1. Crear `AlahiaPos.Payroll.Rules.{PAIS}`.  
2. Implementar pack + calculators legales.  
3. Versionar (`PA-2026.01`, …).  
4. Registrar en el host.  
5. Pruebas del pack.  
6. Verificar que **cero** archivos de `Core` cambian.

Si Core cambia, se rechaza el PR del país hasta corregir el diseño.

---

## 14. Integración contable y evento `NominaAprobada`

- Contabilidad **no** calcula impuestos laborales.  
- Tras `Approved`, el host publica el evento de dominio **`NominaAprobada`**.  
- El evento es **inmutable** una vez emitido.  
- Contabilidad genera asientos usando mapeo **Concepto → Cuenta contable** por empresa.  
- El motor no conoce el plan de cuentas.

### Payload mínimo del evento (normativo)

El evento transporta información suficiente para integración, **sin** reemplazar el snapshot:

- `PayrollRunId`  
- `IdEmpresa`  
- Período  
- `RulePackId`  
- `RulePackVersion`  
- Fecha/hora de aprobación  
- Totales principales (bruto, descuentos, neto, aportes patronales — según proyección del run)

El **detalle completo** (líneas, traces, parámetros) permanece en el snapshot del run.

---

## 15. Biometría y asistencia (límites del motor)

Fuera del alcance de cálculo del motor, pero con reglas de frontera:

- El motor **no** almacena ni procesa biometría.  
- Asistencia entrega **hechos** (`DiaLaboral`, etc.), nunca marcaciones crudas, al contexto.  
- Verify logs biométricos (resultado, confianza, dispositivo, provider id) son infraestructura de asistencia/identidad, no del Payroll Core.

---

## 16. Pruebas (normativa)

| Capa | Requisito |
|------|-----------|
| Core | Tests de DAG (orden, ciclos, hard/soft) sin SQL |
| Calculator | Tests unitarios con contexto inmutable fabricado |
| RulePack | Tests de escenarios legales del país |
| Infrastructure | Tests de mapeo a/desde persistencia (separados) |

Está **prohibido** que un test de calculator requiera base de datos.

---

## 17. Anti-patrones (nunca aceptar)

### AP-01 — Legislación en el Core

```csharp
// PROHIBIDO
public decimal CalcularISR(EvaluationContext ctx) { ... }
```

### AP-02 — Branch por país

```csharp
// PROHIBIDO en Core
if (ctx.Pais == "DO") return CalcularDo(ctx);
```

### AP-03 — Branch por código de concepto en Core

```csharp
// PROHIBIDO en Core
switch (conceptCode) {
  case "AFP": ...
  case "ISR": ...
}
```

### AP-04 — I/O dentro del calculator

```csharp
// PROHIBIDO
public ConceptCalculationResult Execute(...) {
    var tasas = _db.Tasas.ToList(); // BD
    var api = _http.GetAsync(...);  // HTTP
}
```

### AP-05 — Orden fijo disfrazado

```csharp
// PROHIBIDO en Core
Run("SALARIO");
Run("AFP");
Run("ISR");
Run("NETO");
```

El único orden legítimo es el topológico del DAG declarado por los packs.

### AP-06 — Maestro de empleados paralelo

```text
PROHIBIDO: tabla/entidad RH_Empleado como segunda fuente de verdad.
```

### AP-07 — Contabilidad dentro del calculator

```csharp
// PROHIBIDO
_asientoService.Crear(...);
```

### AP-08 — Mutar un run aprobado

```text
PROHIBIDO: UPDATE de líneas/tasas de un PayrollRun en estado Approved.
```

### AP-09 — Pack que referencia Infrastructure

```text
PROHIBIDO: Rules.DO → repositorios SQL / DbContext.
```

### AP-10 — “Custom rules” sin versión ni pack

```text
PROHIBIDO: scripts sueltos o ifs de empresa dentro del Core o del runner.
```

### AP-11 — Mezcla multiempresa

```text
PROHIBIDO: un PayrollRun o EvaluationContext sin IdEmpresa,
o que agregue empleados/hechos de otra empresa.
```

### AP-12 — Reutilizar ConceptCode con otro significado

```text
PROHIBIDO: cambiar el significado de un ConceptCode ya usado en producción.
```

### AP-13 — Cálculo sin política monetaria

```text
PROHIBIDO: producir líneas sin CurrencyCode / precisión / redondeo definidos en el contexto.
```

### AP-14 — Aprobar dos veces la misma ExecutionKey

```text
PROHIBIDO: múltiples PayrollRun Approved para la misma ExecutionKey.
```

---

## 18. Fuera de alcance de esta especificación v1.0

Explícitamente **no** define (se definirán en documentos posteriores sin romper esta constitución):

- Modelo físico SQL / migraciones  
- Entidades EF  
- Contratos HTTP/API  
- Pantallas UI  
- Detalle de fórmulas DO (eso es `Rules.DO`)  
- Diseño de recibo PDF  
- Algoritmo de paralelismo concreto  

Esos entregables **deben** cumplir ALAHIA-PE-01.

---

## 19. Orden de implementación derivado de esta constitución

1. Este documento (hecho).  
2. `AlahiaPos.Payroll.Abstractions` — contratos.  
3. `AlahiaPos.Payroll.Core` — DAG + runner + tests.  
4. `AlahiaPos.Payroll.Rules.DO` — primer pack (sin contaminar Core).  
5. Infrastructure + persistencia.  
6. API / host + evento `NominaAprobada`.  
7. UI.

Ningún paso 5–7 puede comenzar alterando los principios de los pasos 1–3.

---

## 20. Enmiendas

| Versión | Fecha | Cambio |
|---------|-------|--------|
| 1.0 | 2026-08-02 | Constitución inicial del Payroll Engine |
| 1.0 | 2026-08-02 | Fortalecimiento normativo: multiempresa, ConceptCode estable, moneda/redondeo, CalculationTrace, idempotencia, payload de `NominaAprobada` (D14–D19, AP-11–AP-14) |

Toda enmienda material incrementa la versión del documento y enumera qué decisión congelada se altera. Los refuerzos anteriores se adoptan **dentro de v1.0** como cierre de la constitución antes de Abstractions.

---

**Fin de Payroll Engine Specification v1.0 (`ALAHIA-PE-01`)**
