using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagoReclasificacionService : IPagoReclasificacionService
    {
        private readonly AlahiaPosContext _context;
        private readonly IMovimientoFinancieroService _movimientoService;
        private readonly IContabilidadCierreService _cierreService;

        public PagoReclasificacionService(
            AlahiaPosContext context,
            IMovimientoFinancieroService movimientoService,
            IContabilidadCierreService cierreService)
        {
            _context = context;
            _movimientoService = movimientoService;
            _cierreService = cierreService;
        }

        public async Task<ReclasificarPagoResultadoDto> ReclasificarDesdeConciliacionAsync(
            ReclasificarPagoConciliacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length < 5)
                throw new InvalidOperationException("El motivo de reclasificación es obligatorio (mín. 5 caracteres).");

            if (dto.IdMovimientoFinanciero <= 0)
                throw new InvalidOperationException("Debe indicar el movimiento a reclasificar.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conc = await _context.TesoreriaConciliacion.AsTracking().FirstOrDefaultAsync(x =>
                    x.IdTesoreriaConciliacion == dto.IdTesoreriaConciliacion
                    && x.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Conciliación no encontrada.");

                if (conc.Estado is "CERRADA" or "ANULADA")
                    throw new InvalidOperationException("La conciliación no está editable.");

                var linea = await _context.TesoreriaExtractoLinea.AsTracking()
                    .Include(x => x.ExtractoImport)
                    .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea)
                    ?? throw new InvalidOperationException("Línea de extracto no encontrada.");

                if (linea.ExtractoImport == null || linea.ExtractoImport.IdEmpresa != dto.IdEmpresa)
                    throw new InvalidOperationException("La línea no pertenece a la empresa.");

                if (linea.ExtractoImport.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion
                    && linea.ExtractoImport.IdTesoreriaExtractoImport != conc.IdExtractoPrincipal)
                    throw new InvalidOperationException("La línea no pertenece a esta conciliación.");

                if (linea.EstadoMatch is "CONFIRMADO" or "AUTO_CONCILIADO" or "NUEVO_MOV" or "RESUELTO")
                {
                    var ya = await _context.PagoReclasificacion.AsNoTracking().FirstOrDefaultAsync(r =>
                        r.IdEmpresa == dto.IdEmpresa
                        && r.IdTesoreriaExtractoLinea == linea.IdTesoreriaExtractoLinea
                        && (r.Estado == PagoReclasificacionEstados.Aplicada
                            || r.Estado == PagoReclasificacionEstados.PendienteContable)
                        && r.IdPagoReclasificacionReversaDe == null);

                    if (ya != null)
                    {
                        await tx.CommitAsync();
                        return new ReclasificarPagoResultadoDto
                        {
                            IdPagoReclasificacion = ya.IdPagoReclasificacion,
                            IdMovimientoReclasificacion = ya.IdMovimientoReclasificacion ?? 0,
                            IdAsientoContable = ya.IdAsientoContable,
                            Tratamiento = ya.Tratamiento,
                            Estado = ya.Estado,
                            MetodoPagoOriginal = ya.MetodoPagoOriginal,
                            MetodoPagoEfectivo = ya.MetodoPagoEfectivo,
                            FechaContable = ya.FechaContable,
                            YaAplicada = true
                        };
                    }

                    throw new InvalidOperationException("La línea ya está resuelta.");
                }

                if (linea.Credito <= 0)
                    throw new InvalidOperationException(
                        "RECLASIFICAR_PAGO aplica a créditos bancarios (depósitos/transferencias recibidas).");

                var existenteActiva = await _context.PagoReclasificacion.AsNoTracking().AnyAsync(r =>
                    r.IdEmpresa == dto.IdEmpresa
                    && r.IdTesoreriaExtractoLinea == linea.IdTesoreriaExtractoLinea
                    && (r.Estado == PagoReclasificacionEstados.Aplicada
                        || r.Estado == PagoReclasificacionEstados.PendienteContable)
                    && r.IdPagoReclasificacionReversaDe == null);

                if (existenteActiva)
                    throw new InvalidOperationException("Ya existe una reclasificación activa para esta línea.");

                var movOrigen = await _context.MovimientoFinanciero.AsTracking().FirstOrDefaultAsync(m =>
                    m.IdMovimientoFinanciero == dto.IdMovimientoFinanciero
                    && m.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Movimiento origen no encontrado.");

                if (movOrigen.Estado != "CONFIRMADO")
                    throw new InvalidOperationException("El movimiento origen no está confirmado.");

                if (movOrigen.EstadoConciliacion == "CONCILIADO")
                    throw new InvalidOperationException("El movimiento origen ya está conciliado.");

                if (movOrigen.Monto != linea.Credito)
                    throw new InvalidOperationException(
                        $"El monto del movimiento ({movOrigen.Monto:N2}) no coincide con el crédito del extracto ({linea.Credito:N2}).");

                var idCuentaOrigen = movOrigen.IdCuentaDestino
                    ?? (string.Equals(movOrigen.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase)
                        ? movOrigen.IdCuentaDestino
                        : movOrigen.IdCuentaOrigen);

                // ENTRADA suele usar IdCuentaDestino; fallback a origen si es nulo.
                idCuentaOrigen ??= movOrigen.IdCuentaOrigen;

                if (idCuentaOrigen is not > 0)
                    throw new InvalidOperationException("No se pudo determinar la cuenta origen del cobro.");

                if (idCuentaOrigen == conc.IdCuentaFinanciera)
                    throw new InvalidOperationException(
                        "El movimiento ya está en la cuenta bancaria de la conciliación. Use ASOCIAR.");

                var cuentaOrigen = await _context.CuentaFinanciera.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCuentaFinanciera == idCuentaOrigen)
                    ?? throw new InvalidOperationException("Cuenta origen no encontrada.");

                var clave = $"RECLAS_PAGO_{dto.IdEmpresa}_{linea.IdTesoreriaExtractoLinea}";

                // Contexto de caja / período
                FacturaHeaders? factura = null;
                if (string.Equals(movOrigen.ReferenciaTipo, "FACTURA", StringComparison.OrdinalIgnoreCase)
                    && movOrigen.ReferenciaId is > 0)
                {
                    factura = await _context.FacturaHeaders.AsNoTracking()
                        .FirstOrDefaultAsync(f => f.IdFacturaHeader == movOrigen.ReferenciaId);
                }

                var pago = await ResolverPagoAsync(dto.IdEmpresa, movOrigen, factura);
                var ingreso = await ResolverIngresoAsync(dto.IdEmpresa, movOrigen, factura, pago);

                if (pago != null && pago.IdMovimientoFinanciero is not > 0)
                {
                    var pagoTracked = await _context.PagosFacturasClientes.AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == pago.Id);
                    if (pagoTracked != null)
                        pagoTracked.IdMovimientoFinanciero = movOrigen.IdMovimientoFinanciero;
                }

                if (ingreso != null && ingreso.IdMovimientoFinanciero is not > 0)
                {
                    var ingresoTracked = await _context.Ingresos.AsTracking()
                        .FirstOrDefaultAsync(i => i.IdIngreso == ingreso.IdIngreso);
                    if (ingresoTracked != null)
                        ingresoTracked.IdMovimientoFinanciero = movOrigen.IdMovimientoFinanciero;
                }

                var cajaCerrada = factura?.EstaCerrada == true || factura?.IdCajaCierre is > 0;
                if (!cajaCerrada && factura?.IdUsuario is > 0)
                {
                    var apertura = await _context.CajaApertura.AsNoTracking()
                        .Where(a => a.IdEmpresa == dto.IdEmpresa && a.IdUsuario == factura.IdUsuario)
                        .OrderByDescending(a => a.FechaApertura)
                        .FirstOrDefaultAsync();
                    if (apertura != null
                        && string.Equals(apertura.Estado, "CERRADA", StringComparison.OrdinalIgnoreCase))
                        cajaCerrada = true;
                }

                var periodoOriginalCerrado = await _cierreService.EstaPeriodoBloqueadoAsync(
                    dto.IdEmpresa, movOrigen.FechaMovimiento);

                var fechaPreferida = DateTime.Now;
                var fechaContable = await _cierreService.ResolverFechaContableAbiertaAsync(
                    dto.IdEmpresa, fechaPreferida)
                    ?? throw new InvalidOperationException(
                        "No hay un período contable abierto para registrar la reclasificación. Abra un período e intente de nuevo.");

                var tratamiento = cajaCerrada
                    ? PagoReclasificacionTratamientos.PostCierre
                    : PagoReclasificacionTratamientos.AntesCierre;

                var metodoOriginal = pago?.FormaPago
                    ?? ingreso?.FormaPago
                    ?? factura?.FormaPago
                    ?? "Efectivo";

                var metodoEfectivo = string.IsNullOrWhiteSpace(dto.MetodoPagoEfectivo)
                    ? "Transferencia"
                    : dto.MetodoPagoEfectivo.Trim();

                var idMovReclas = await _movimientoService.RegistrarTransferenciaReclasificacionAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    idCuentaOrigen.Value,
                    conc.IdCuentaFinanciera,
                    linea.Credito,
                    $"Reclasificar pago: {dto.Motivo.Trim()}",
                    $"Línea extracto #{linea.IdTesoreriaExtractoLinea}; mov origen #{movOrigen.IdMovimientoFinanciero}",
                    clave,
                    linea.IdTesoreriaExtractoLinea,
                    fechaContable);

                var movReclas = await _context.MovimientoFinanciero.AsTracking()
                    .FirstAsync(m => m.IdMovimientoFinanciero == idMovReclas);

                movReclas.EstadoConciliacion = "CONCILIADO";
                movReclas.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                movReclas.FechaConciliacion = DateTime.UtcNow;
                movReclas.IdUsuarioConciliacion = dto.IdUsuario;

                // El movimiento original permanece como evidencia histórica; ya no representa el dinero en Caja.
                movOrigen.Observacion = string.IsNullOrWhiteSpace(movOrigen.Observacion)
                    ? $"Reclasificado a cuenta #{conc.IdCuentaFinanciera} vía {clave}"
                    : $"{movOrigen.Observacion} | Reclasificado a cuenta #{conc.IdCuentaFinanciera} vía {clave}";

                linea.IdMovimientoFinanciero = idMovReclas;
                linea.EstadoMatch = "CONFIRMADO";
                linea.AccionTomada = AccionExtractoPendiente.RECLASIFICAR_PAGO.ToString();
                linea.EsAutoConciliado = false;
                linea.FechaResolucion = DateTime.UtcNow;
                linea.IdUsuarioResolucion = dto.IdUsuario;
                linea.ReglaMatch = "RECLASIFICAR_PAGO";
                linea.ExplicacionMatch =
                    $"Reclasificado de {cuentaOrigen.Nombre} ({metodoOriginal}) → banco ({metodoEfectivo}).";

                var reclas = new PagoReclasificacion
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                    IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                    DocumentoTipo = "FACTURA",
                    IdDocumento = factura?.IdFacturaHeader,
                    IdFacturaHeader = factura?.IdFacturaHeader,
                    IdPagoFacturaCliente = pago?.Id,
                    IdIngreso = ingreso?.IdIngreso,
                    IdMovimientoOriginal = movOrigen.IdMovimientoFinanciero,
                    IdMovimientoReclasificacion = idMovReclas,
                    IdCuentaOrigen = idCuentaOrigen.Value,
                    IdCuentaDestino = conc.IdCuentaFinanciera,
                    MetodoPagoOriginal = metodoOriginal,
                    MetodoPagoEfectivo = metodoEfectivo,
                    Monto = linea.Credito,
                    FechaMovimientoOriginal = movOrigen.FechaMovimiento,
                    FechaEfectiva = DateTime.Now,
                    FechaContable = fechaContable,
                    Tratamiento = tratamiento,
                    IdCajaCierreSnapshot = factura?.IdCajaCierre,
                    CajaEstabaCerrada = cajaCerrada,
                    PeriodoOriginalCerrado = periodoOriginalCerrado,
                    IdUsuario = dto.IdUsuario,
                    Motivo = dto.Motivo.Trim(),
                    Estado = PagoReclasificacionEstados.Aplicada,
                    ClaveIdempotencia = clave,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.PagoReclasificacion.Add(reclas);

                _context.TesoreriaConciliacionAuditoria.Add(new TesoreriaConciliacionAuditoria
                {
                    IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    Accion = "RECLASIFICAR_PAGO",
                    Detalle =
                        $"{metodoOriginal}→{metodoEfectivo} · {linea.Credito:N2} · {tratamiento} · mov #{idMovReclas}",
                    IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                    IdMovimientoFinanciero = idMovReclas,
                    Fecha = DateTime.UtcNow
                });

                // Recalcular diferencia de conciliación
                var estado = await _movimientoService.GetEstadoCuentaAsync(
                    conc.IdCuentaFinanciera, conc.PeriodoDesde, conc.PeriodoHasta);
                conc.SaldoLibrosFinal = estado.SaldoFinal;
                conc.Diferencia = (conc.SaldoBancoFinal ?? 0) - conc.SaldoLibrosFinal;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ReclasificarPagoResultadoDto
                {
                    IdPagoReclasificacion = reclas.IdPagoReclasificacion,
                    IdMovimientoReclasificacion = idMovReclas,
                    IdAsientoContable = reclas.IdAsientoContable,
                    Tratamiento = tratamiento,
                    Estado = reclas.Estado,
                    MetodoPagoOriginal = metodoOriginal,
                    MetodoPagoEfectivo = metodoEfectivo,
                    FechaContable = fechaContable,
                    YaAplicada = false
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ReclasificarPagoResultadoDto> ReversarAsync(ReversarReclasificacionPagoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length < 5)
                throw new InvalidOperationException("El motivo de reversión es obligatorio.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var original = await _context.PagoReclasificacion.AsTracking().FirstOrDefaultAsync(r =>
                    r.IdPagoReclasificacion == dto.IdPagoReclasificacion
                    && r.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Reclasificación no encontrada.");

                if (original.Estado == PagoReclasificacionEstados.Reversada)
                    throw new InvalidOperationException("La reclasificación ya está reversada.");

                if (original.IdMovimientoReclasificacion is not > 0)
                    throw new InvalidOperationException("La reclasificación no tiene movimiento asociado.");

                var fechaContable = await _cierreService.ResolverFechaContableAbiertaAsync(
                    dto.IdEmpresa, DateTime.Now)
                    ?? throw new InvalidOperationException(
                        "No hay período contable abierto para registrar la reversión.");

                var clave = $"RECLAS_REV_{dto.IdEmpresa}_{original.IdPagoReclasificacion}";

                // Inversa: Banco → Caja
                var idMovRev = await _movimientoService.RegistrarTransferenciaReclasificacionAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    original.IdCuentaDestino,
                    original.IdCuentaOrigen,
                    original.Monto,
                    $"Reverso reclasificación pago: {dto.Motivo.Trim()}",
                    $"Reversa de PagoReclasificacion #{original.IdPagoReclasificacion}",
                    clave,
                    original.IdTesoreriaExtractoLinea,
                    fechaContable);

                original.Estado = PagoReclasificacionEstados.Reversada;
                original.FechaReversion = DateTime.UtcNow;

                var reverso = new PagoReclasificacion
                {
                    IdEmpresa = original.IdEmpresa,
                    IdTesoreriaConciliacion = original.IdTesoreriaConciliacion,
                    IdTesoreriaExtractoLinea = original.IdTesoreriaExtractoLinea,
                    DocumentoTipo = original.DocumentoTipo,
                    IdDocumento = original.IdDocumento,
                    IdFacturaHeader = original.IdFacturaHeader,
                    IdPagoFacturaCliente = original.IdPagoFacturaCliente,
                    IdIngreso = original.IdIngreso,
                    IdMovimientoOriginal = original.IdMovimientoReclasificacion!.Value,
                    IdMovimientoReclasificacion = idMovRev,
                    IdCuentaOrigen = original.IdCuentaDestino,
                    IdCuentaDestino = original.IdCuentaOrigen,
                    MetodoPagoOriginal = original.MetodoPagoEfectivo,
                    MetodoPagoEfectivo = original.MetodoPagoOriginal,
                    Monto = original.Monto,
                    FechaMovimientoOriginal = original.FechaEfectiva,
                    FechaEfectiva = DateTime.Now,
                    FechaContable = fechaContable,
                    Tratamiento = original.Tratamiento,
                    CajaEstabaCerrada = original.CajaEstabaCerrada,
                    PeriodoOriginalCerrado = original.PeriodoOriginalCerrado,
                    IdUsuario = dto.IdUsuario,
                    Motivo = dto.Motivo.Trim(),
                    Estado = PagoReclasificacionEstados.Aplicada,
                    IdPagoReclasificacionReversaDe = original.IdPagoReclasificacion,
                    ClaveIdempotencia = clave,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.PagoReclasificacion.Add(reverso);

                var linea = await _context.TesoreriaExtractoLinea.AsTracking()
                    .FirstOrDefaultAsync(l => l.IdTesoreriaExtractoLinea == original.IdTesoreriaExtractoLinea);
                if (linea != null
                    && linea.IdMovimientoFinanciero == original.IdMovimientoReclasificacion)
                {
                    linea.EstadoMatch = "PENDIENTE";
                    linea.IdMovimientoFinanciero = null;
                    linea.AccionTomada = null;
                    linea.FechaResolucion = null;
                    linea.IdUsuarioResolucion = null;
                    linea.ReglaMatch = null;
                    linea.ExplicacionMatch = $"Reclasificación #{original.IdPagoReclasificacion} reversada.";
                    linea.EsAutoConciliado = false;
                }

                if (original.IdTesoreriaConciliacion is > 0)
                {
                    _context.TesoreriaConciliacionAuditoria.Add(new TesoreriaConciliacionAuditoria
                    {
                        IdTesoreriaConciliacion = original.IdTesoreriaConciliacion.Value,
                        IdEmpresa = dto.IdEmpresa,
                        IdUsuario = dto.IdUsuario,
                        Accion = "REVERSAR_RECLASIFICACION_PAGO",
                        Detalle = dto.Motivo.Trim(),
                        IdTesoreriaExtractoLinea = original.IdTesoreriaExtractoLinea,
                        IdMovimientoFinanciero = idMovRev,
                        Fecha = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ReclasificarPagoResultadoDto
                {
                    IdPagoReclasificacion = reverso.IdPagoReclasificacion,
                    IdMovimientoReclasificacion = idMovRev,
                    Tratamiento = reverso.Tratamiento,
                    Estado = reverso.Estado,
                    MetodoPagoOriginal = reverso.MetodoPagoOriginal,
                    MetodoPagoEfectivo = reverso.MetodoPagoEfectivo,
                    FechaContable = fechaContable,
                    YaAplicada = false
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<Dictionary<int, string>> ObtenerMetodosEfectivosAntesCierreAsync(
            int idEmpresa,
            IEnumerable<int> idIngresos)
        {
            var ids = idIngresos.Distinct().Where(x => x > 0).ToList();
            if (ids.Count == 0)
                return new Dictionary<int, string>();

            var list = await _context.PagoReclasificacion.AsNoTracking()
                .Where(r =>
                    r.IdEmpresa == idEmpresa
                    && r.IdIngreso != null
                    && ids.Contains(r.IdIngreso.Value)
                    && r.Estado == PagoReclasificacionEstados.Aplicada
                    && r.Tratamiento == PagoReclasificacionTratamientos.AntesCierre
                    && r.IdPagoReclasificacionReversaDe == null)
                .ToListAsync();

            return list
                .GroupBy(r => r.IdIngreso!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IdPagoReclasificacion).First().MetodoPagoEfectivo);
        }

        public async Task<Dictionary<int, string>> ObtenerMetodosEfectivosPorFacturaAntesCierreAsync(
            int idEmpresa,
            IEnumerable<int> idFacturas)
        {
            var ids = idFacturas.Distinct().Where(x => x > 0).ToList();
            if (ids.Count == 0)
                return new Dictionary<int, string>();

            var list = await _context.PagoReclasificacion.AsNoTracking()
                .Where(r =>
                    r.IdEmpresa == idEmpresa
                    && r.IdFacturaHeader != null
                    && ids.Contains(r.IdFacturaHeader.Value)
                    && r.Estado == PagoReclasificacionEstados.Aplicada
                    && r.Tratamiento == PagoReclasificacionTratamientos.AntesCierre
                    && r.IdPagoReclasificacionReversaDe == null)
                .ToListAsync();

            return list
                .GroupBy(r => r.IdFacturaHeader!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IdPagoReclasificacion).First().MetodoPagoEfectivo);
        }

        public async Task<IReadOnlyList<PagoReclasificacion>> ListarActivasPorEmpresaAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var q = _context.PagoReclasificacion.AsNoTracking()
                .Where(r =>
                    r.IdEmpresa == idEmpresa
                    && r.Estado == PagoReclasificacionEstados.Aplicada
                    && r.IdPagoReclasificacionReversaDe == null);

            if (desde.HasValue)
                q = q.Where(r => r.FechaEfectiva >= desde.Value.Date);
            if (hasta.HasValue)
                q = q.Where(r => r.FechaEfectiva <= hasta.Value.Date.AddDays(1).AddTicks(-1));

            return await q.OrderByDescending(r => r.FechaEfectiva).ToListAsync();
        }

        private async Task<PagosFacturasClientes?> ResolverPagoAsync(
            int idEmpresa,
            MovimientoFinanciero mov,
            FacturaHeaders? factura)
        {
            var porVinculo = await _context.PagosFacturasClientes.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdMovimientoFinanciero == mov.IdMovimientoFinanciero);
            if (porVinculo != null)
                return porVinculo;

            if (factura == null)
                return null;

            var candidatos = await _context.PagosFacturasClientes.AsNoTracking()
                .Where(p => p.IdFacturaHeader == factura.IdFacturaHeader && p.Monto == mov.Monto)
                .ToListAsync();

            return candidatos.Count == 1 ? candidatos[0] : null;
        }

        private async Task<Ingresos?> ResolverIngresoAsync(
            int idEmpresa,
            MovimientoFinanciero mov,
            FacturaHeaders? factura,
            PagosFacturasClientes? pago)
        {
            var porVinculo = await _context.Ingresos.AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.IdEmpresa == idEmpresa
                    && i.IdMovimientoFinanciero == mov.IdMovimientoFinanciero
                    && !i.EstaAnulado);
            if (porVinculo != null)
                return porVinculo;

            if (factura == null)
                return null;

            var q = _context.Ingresos.AsNoTracking()
                .Where(i =>
                    i.IdEmpresa == idEmpresa
                    && i.IdFacturaHeader == factura.IdFacturaHeader
                    && i.Monto == mov.Monto
                    && !i.EstaAnulado);

            if (pago != null && !string.IsNullOrWhiteSpace(pago.FormaPago))
                q = q.Where(i => i.FormaPago == pago.FormaPago);

            var list = await q.ToListAsync();
            return list.Count == 1 ? list[0] : null;
        }
    }
}
