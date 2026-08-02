# Contrato workspace — métricas de saldos (Fase 1)

**Ámbito:** `ConciliacionSaldosDto` / `ConciliacionSaldos` (workspace de conciliación)  
**Política de cierre default:** cuadrar el extracto (no exigir cuadre patrimonial de cuenta).

## Semántica (3 capas)

| Campo | Capa | Pregunta | ¿Bloquea cerrar? |
|-------|------|----------|------------------|
| `diferenciaPeriodo` | A. Conciliación del período | ¿Quedó resuelto el extracto/período? | **Sí** si `|valor| > tolerancia` |
| `variacionHistorica` | B. Variación histórica | ¿Cuánto del gap viene de fuera del extracto? | **No** (contexto) |
| `brechaBalanceCuenta` | C. Balance de cuenta | Cierre extracto vs libros acumulados | **No** como fallo de matching |

Alias: `diferencia` (= `brechaBalanceCuenta`) se conserva por compatibilidad; la UI **no** debe etiquetarlo como “Diferencia” de fallo.

## Campos Fase 1

| Campo JSON | Tipo | Definición |
|------------|------|------------|
| `saldoLibrosInicial` | decimal | Snapshot al crear la sesión |
| `saldoLibrosFinal` | decimal | Libros acumulados hasta `PeriodoHasta` |
| `saldoBancoInicial` / `saldoBancoInicioExtracto` | decimal? | Inicial del extracto / sesión |
| `saldoBancoFinal` | decimal? | Cierre del extracto |
| `saldoLibrosInicioExtracto` | decimal | ERP al inicio del marco del extracto (`BalanceInicial` + movs con fecha &lt; inicio extracto) |
| `variacionHistorica` | decimal | `saldoLibrosInicioExtracto − (saldoBancoInicioExtracto ?? 0)` |
| `brechaBalanceCuenta` | decimal | `saldoBancoFinal − saldoLibrosFinal` |
| `diferencia` | decimal | Igual a `brechaBalanceCuenta` (compat) |
| `diferenciaPeriodo` | decimal | `0` si no hay pendientes banco ni libro del período; si no, `brechaBalanceCuenta + variacionHistorica` |
| `extractoCompletamenteResuelto` | bool | Sin líneas de banco pendientes |
| `montoPendienteBanco` / `montoPendienteLibro` | decimal | Montos operativos |
| `toleranciaDiferencia` | decimal | Umbral de cierre sobre `diferenciaPeriodo` |

## Reglas de cierre (default)

Bloquea si:

1. No hay extracto y no hay líneas.
2. Hay pendientes de banco.
3. Hay movimientos de libro del período pendientes.
4. `|diferenciaPeriodo| > tolerancia` (mensaje: “La diferencia de conciliación del período …”).
5. Hay reclasificaciones pendientes de contabilizar.

**No** bloquea por `|brechaBalanceCuenta| > tolerancia` cuando el extracto está resuelto y la brecha es historia.
**No** usar el mensaje legacy `La diferencia {brecha} excede la tolerancia` (ese texto se refiere a `Diferencia`/`brechaBalanceCuenta`).

Opt-in futuro (no implementado): política “cuadrar saldo de cuenta”.
