# Principio arquitectónico — El banco es la fuente de verdad (Conciliación Bancaria)

**Código:** `ALAHIA-CB-01`  
**Fecha:** 2026-07-21  
**Estado:** Principio vigente  
**Ámbito:** Módulo de Conciliación Bancaria (AlahiaPosApi, CLoudAlahiaPos) y flujos financieros derivados

---

## Enunciado

> En conciliación bancaria, la **realidad financiera la define el banco**, no el ERP.  
> Los movimientos financieros del ERP son nuestra **referencia** de lo que creemos que ocurrió;
> la entidad bancaria es quien **realmente administra el dinero**.

Si el estado de cuenta muestra que un débito o un crédito **fue ejecutado**, debemos
asumir que el movimiento **ocurrió**, aunque todavía no exista en nuestro sistema.
La conciliación existe precisamente para **registrar esos movimientos y sincronizar el
ERP con la realidad del banco**.

---

## Reglas derivadas

1. **El estado de cuenta representa la realidad.**
   Un movimiento presente en el extracto se considera un hecho consumado.

2. **La vista previa es el único punto de corrección.**
   Un PDF puede traer errores de parser, o el usuario puede detectar una inconsistencia.
   Esos errores se corrigen en la **vista previa editable antes de confirmar**, no durante
   la conciliación ni alterando el extracto ya confirmado.

3. **Tras confirmar, el ERP se alinea al banco, no al revés.**
   Al resolver una línea (crear cargo/interés/ajuste o asociar), el ERP se ajusta para
   reflejar lo que el banco ya hizo.

4. **No se valida saldo en libros para movimientos originados en el extracto.**
   Si el banco permitió una salida, es porque el dinero existía; el saldo bajo en libros
   solo indica que el ERP está desactualizado. Los movimientos creados desde la
   conciliación (`ReferenciaTipo = "EXTRACTO"`) **omiten la validación de "Fondos
   insuficientes"**. Los movimientos manuales **siguen validando saldo** con normalidad.

5. **Monto, fecha, cuenta y dirección se toman del extracto.**
   Al registrar desde una línea, el usuario solo edita **motivo** y **categoría**; el
   resto proviene de la línea confirmada. Corregir el importe se hace en la vista previa.

6. **Idempotencia y trazabilidad.**
   Cada movimiento creado desde una línea usa clave `EXT_<idImport>_<idLinea>`, de modo
   que reimportar el archivo o reintentar la acción **no duplica** movimientos. Toda
   resolución queda auditada (quién, cuándo, qué línea, qué movimiento).

---

## Dónde vive en el código

- **Backend — omisión de validación de fondos:**
  `AlahiaPos.DataAccess/Servicios/MovimientoFinancieroService.cs`
  (`RegistrarSalidaAsync`: `esRegistroDesdeExtracto` según `ReferenciaTipo == "EXTRACTO"`).
- **Backend — creación/idempotencia desde línea:**
  `AlahiaPos.DataAccess/Servicios/TesoreriaExtractoService.cs`
  (`CrearMovimientoBancarioAsync`, clave `EXT_<import>_<linea>`).
- **Backend — resolución dentro de la sesión:**
  `AlahiaPos.DataAccess/Servicios/TesoreriaConciliacionService.cs` (`ResolverLineaAsync`).
- **Frontend — corrección previa a confirmar:**
  `CLoudAlahiaPos/src/app/Components/conciliacion-bancaria/` (wizard de vista previa editable).

---

## Qué NO hacer

- No bloquear el registro de un movimiento del extracto por "fondos insuficientes".
- No editar montos del extracto fuera de la vista previa.
- No tratar el ERP como fuente de verdad frente a un estado de cuenta confirmado.
- No crear rutas que dupliquen movimientos ya conciliados desde una línea.
