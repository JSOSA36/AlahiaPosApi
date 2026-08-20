# AlahiaPos.Payroll.Core

Implementación del motor según [`ALAHIA-PE-01`](../docs/payroll/Payroll-Engine-Specification-v1.0.md).

## Dependencias

- Solo `AlahiaPos.Payroll.Abstractions`
- Sin SQL, EF, HTTP, Rules.DO

## Piezas

| Área | Tipos |
|------|--------|
| Dag | `ConceptDependencyGraph`, `DagBuilder`, `CycleDetector`, `TopologicalSorter` |
| Engine | `PayrollEngine`, `PayrollRunner`, `CalculationPipeline`, `ConceptCalculatorFactory` |
| Validation | `PayrollValidationService` |

## Uso

```csharp
IPayrollEngine engine = new PayrollEngine();
var draft = engine.Calculate(request, rulePack);
var approved = engine.Approve(draft, approvalRequest);
```
