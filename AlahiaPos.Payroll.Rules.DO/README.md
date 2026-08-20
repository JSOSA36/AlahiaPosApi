# AlahiaPos.Payroll.Rules.DO

RulePack `DO-2026.01` (ALAHIA-PE-01 §11 / §13).

## Dependencias

- Solo `AlahiaPos.Payroll.Abstractions`
- **No** referencia Core, Infrastructure, SQL ni HTTP

## Conceptos

| ConceptCode | Origen |
|-------------|--------|
| SUELDO_BASE | Assignment vigente |
| HORAS_EXTRA | Fact del período |
| AFP_EMPLEADO / SFS_EMPLEADO / ISR_EMPLEADO | Deducciones empleado |
| AFP_PATRONAL / SFS_PATRONAL / INFOTEP_PATRONAL | Aportes patronales |
| NETO | Proyección (sin lógica fiscal) |

Las tasas viven en `PackParameters` del pack (versionables). Los valores default son **operativos de ejemplo**, no oficiales hasta validación TSS/DGII.
