# AlahiaPos.Payroll.Abstractions

Contratos del **Payroll Engine** según [`ALAHIA-PE-01`](../docs/payroll/Payroll-Engine-Specification-v1.0.md).

Constitución cerrada. Este proyecto solo contiene dominio: **sin SQL, EF, API, UI ni Rules.DO**.

## Interfaces

| Contrato | Rol |
|----------|-----|
| `IPayrollEngine` | Orquesta cálculo y aprobación |
| `IPayrollRulePack` | Pack instalable (metadata + calculators + concepts) |
| `IConceptCalculator` | Extensión por concepto (pura, sin I/O) |
| `IConceptCalculatorMetadata` | ConceptCode + DependsOn para el DAG |
| `IConceptCalculatorFactory` | Resolución de calculators por ConceptCode |
| `IFactProvider` / `IAttendanceFactProvider` | Carga de hechos **antes** del contexto |
| `IConceptAssignmentProvider` / `IEmployeeContractAttributeProvider` | Assignments y contrato hacia el contexto |
| `IEvaluationContextBuilder` | Ensambla `EvaluationContext` (sin cálculo) |
| `IPayrollRunStore` | Persistencia de runs / snapshot / ExecutionKey |

## Modelos

| Tipo | Rol |
|------|-----|
| `EvaluationContext` | Contexto inmutable (`IdEmpresa`, money, facts, assignments) |
| `PayrollRun` / `PayrollLine` | Resultado del run |
| `ConceptCalculationResult` | Salida 1..n líneas + traces |
| `ConceptDependency` | Arista del DAG (Hard/Soft) |
| `CalculationTrace` | Explicabilidad (snapshot) |
| `ExecutionKey` | Idempotencia |
| `RulePackMetadata` / `ConceptMetadata` | Catálogo / pack |
| `MoneyPolicy` | Currency + precisión + redondeo |
| `NominaAprobadaEvent` | Contrato de integración (inmutable) |

## Reglas

- Ninguna lógica legal ni conceptos dominicanos en este ensamblado.
- `ConceptCode` es contrato público estable.
- Multiempresa: `IdEmpresa` es frontera obligatoria.
