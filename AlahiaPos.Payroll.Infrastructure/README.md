# AlahiaPos.Payroll.Infrastructure

Persistencia y carga de contexto del Payroll Engine (`ALAHIA-PE-01`).

## Qué hace

- `PayrollDbContext` + tablas de runs/líneas/traces/snapshot, assignments, facts, contrato
- `IEvaluationContextBuilder` — arma contexto (sin calcular)
- `IPayrollRunStore` — draft / approve / ExecutionKey
- `IAttendanceFactProvider` / `IFactProvider` — hechos del período

## Qué no hace

- AFP / ISR / SFS ni ninguna regla legal
- Cálculo de conceptos ni DAG
- API / controllers

## DI

```csharp
services.AddPayrollInfrastructure(o => o.UseSqlServer(cs));
// tests: o.UseInMemoryDatabase("payroll-tests")
```

Solo referencia `AlahiaPos.Payroll.Abstractions`.
