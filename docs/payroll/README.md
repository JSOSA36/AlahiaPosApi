# Nómina / Payroll Engine — Documentación

| Código | Documento | Estado |
|--------|-----------|--------|
| **ALAHIA-PE-01** | [Payroll Engine Specification v1.0](./Payroll-Engine-Specification-v1.0.md) | Constitución vigente |

La especificación es normativa: define principios, dependencias permitidas/prohibidas, pipeline DAG, RulePacks y anti-patrones. No describe tablas SQL ni APIs.

## Código

| Proyecto | Rol |
|----------|-----|
| [`AlahiaPos.Payroll.Abstractions`](../../AlahiaPos.Payroll.Abstractions/) | Contratos de dominio (sin Infrastructure) |
| [`AlahiaPos.Payroll.Core`](../../AlahiaPos.Payroll.Core/) | Motor DAG + runner (solo Abstractions) |
| [`AlahiaPos.Payroll.Rules.DO`](../../AlahiaPos.Payroll.Rules.DO/) | RulePack `DO-2026.01` (solo Abstractions) |
| [`AlahiaPos.Payroll.Infrastructure`](../../AlahiaPos.Payroll.Infrastructure/) | EF, contexto, facts, runs/snapshots (sin lógica fiscal) |

Expediente laboral (corte 1): tablas `EmpleadoLaboral`, `NominaConcepto*`, menú `RRHH_LABORAL`, API `api/rrhh`.

Las tasas y tablas del pack DO son parámetros versionados; los defaults son operativos de ejemplo hasta validación TSS/DGII.
