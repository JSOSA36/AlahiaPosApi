using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class TesoreriaConciliacionService : ITesoreriaConciliacionService
    {
        private static readonly HashSet<string> CategoriasBancariasPuras = new(StringComparer.OrdinalIgnoreCase)
        {
            "COMISION_BANCARIA", "CARGO_BANCARIO", "IMPUESTO_BANCARIO",
            "INTERES_BANCARIO", "DEBITO_AUTOMATICO", "CREDITO_AUTOMATICO",
            "AJUSTE_BANCARIO", "REVERSO_BANCARIO", "OTRO_BANCARIO", "COMISION"
        };

        private static readonly HashSet<string> CategoriasOperativas = new(StringComparer.OrdinalIgnoreCase)
        {
            "VENTA", "COBRO", "COBRO_CXC", "PAGO", "PAGO_PROVEEDOR", "PAGO_CLIENTE",
            "GASTO", "COMPRA", "INGRESO", "INGRESO_EXTRA", "TRANSFERENCIA", "CIERRE_CAJA"
        };

        private readonly AlahiaPosContext _context;
        private readonly IMovimientoFinancieroService _movimientoService;
        private readonly ICuentaFinancieraService _cuentaService;
        private readonly ITesoreriaExtractoService _extractoService;
        private readonly IPagoReclasificacionService _reclasificacionService;

        public TesoreriaConciliacionService(
            AlahiaPosContext context,
            IMovimientoFinancieroService movimientoService,
            ICuentaFinancieraService cuentaService,
            ITesoreriaExtractoService extractoService,
            IPagoReclasificacionService reclasificacionService)
        {
            _context = context;
            _movimientoService = movimientoService;
            _cuentaService = cuentaService;
            _extractoService = extractoService;
            _reclasificacionService = reclasificacionService;
        }

        public async Task<TesoreriaConciliacion> CrearConciliacionAsync(CrearConciliacionDto dto)
        {
            var cuenta = await _cuentaService.GetByIdAsync(dto.IdCuentaFinanciera)
                ?? throw new InvalidOperationException("Cuenta financiera no encontrada.");

            if (cuenta.IdEmpresa != dto.IdEmpresa)
                throw new InvalidOperationException("La cuenta no pertenece a la empresa.");

            var abierta = await _context.TesoreriaConciliacion.AsNoTracking().FirstOrDefaultAsync(x =>
                x.IdEmpresa == dto.IdEmpresa
                && x.IdCuentaFinanciera == dto.IdCuentaFinanciera
                && (x.Estado == "BORRADOR" || x.Estado == "EN_PROCESO"));

            if (abierta != null)
                throw new InvalidOperationException(
                    $"Ya existe una conciliación abierta (#{abierta.IdTesoreriaConciliacion}) para esta cuenta.");

            var saldoInicial = await _movimientoService.GetEstadoCuentaAsync(
                dto.IdCuentaFinanciera,
                null,
                dto.PeriodoDesde.AddDays(-1));

            var saldoFinal = await _movimientoService.GetEstadoCuentaAsync(
                dto.IdCuentaFinanciera,
                dto.PeriodoDesde,
                dto.PeriodoHasta);

            var conciliacion = new TesoreriaConciliacion
            {
                IdEmpresa = dto.IdEmpresa,
                IdCuentaFinanciera = dto.IdCuentaFinanciera,
                PeriodoDesde = dto.PeriodoDesde.Date,
                PeriodoHasta = dto.PeriodoHasta.Date,
                SaldoLibrosInicial = saldoInicial.SaldoFinal,
                SaldoLibrosFinal = saldoFinal.SaldoFinal,
                SaldoBancoInicial = dto.SaldoBancoInicial,
                SaldoBancoFinal = dto.SaldoBancoFinal,
                ToleranciaDiferencia = dto.ToleranciaDiferencia,
                Diferencia = dto.SaldoBancoFinal - saldoFinal.SaldoFinal,
                Estado = "EN_PROCESO",
                IdUsuario = dto.IdUsuario,
                Observacion = dto.Observacion,
                FechaCreacion = DateTime.UtcNow
            };

            _context.TesoreriaConciliacion.Add(conciliacion);
            await _context.SaveChangesAsync();

            await AuditarAsync(
                conciliacion.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "CREAR",
                $"Periodo {conciliacion.PeriodoDesde:yyyy-MM-dd}..{conciliacion.PeriodoHasta:yyyy-MM-dd}");

            if (dto.IdTesoreriaExtractoImport is > 0)
            {
                await AdjuntarExtractoAsync(new AdjuntarExtractoAConciliacionDto
                {
                    IdTesoreriaConciliacion = conciliacion.IdTesoreriaConciliacion,
                    IdTesoreriaExtractoImport = dto.IdTesoreriaExtractoImport.Value,
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    EjecutarMatching = true
                });
            }

            return conciliacion;
        }

        public async Task<TesoreriaConciliacion?> GetByIdAsync(int idTesoreriaConciliacion, int idEmpresa)
        {
            return await _context.TesoreriaConciliacion
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IdTesoreriaConciliacion == idTesoreriaConciliacion
                    && x.IdEmpresa == idEmpresa);
        }

        public async Task<ConciliacionWorkspaceDto> GetWorkspaceAsync(int idTesoreriaConciliacion, int idEmpresa)
        {
            var conc = await GetByIdAsync(idTesoreriaConciliacion, idEmpresa)
                ?? throw new InvalidOperationException("Conciliación no encontrada.");

            var cuenta = await _cuentaService.GetByIdAsync(conc.IdCuentaFinanciera);
            var extracto = await _context.TesoreriaExtractoImport.AsNoTracking()
                .Where(x =>
                    x.IdEmpresa == idEmpresa
                    && (x.IdTesoreriaConciliacion == idTesoreriaConciliacion
                        || x.IdTesoreriaExtractoImport == conc.IdExtractoPrincipal))
                .OrderByDescending(x => x.IdTesoreriaExtractoImport)
                .FirstOrDefaultAsync();

            var lineasBanco = new List<ConciliacionLineaBancoDto>();
            if (extracto != null)
            {
                lineasBanco = await ConstruirLineasBancoAsync(extracto.IdTesoreriaExtractoImport);
            }

            var libroPendientes = (await ListarMovimientosPendientesAsync(idTesoreriaConciliacion, idEmpresa)).ToList();
            var stats = CalcularEstadisticas(lineasBanco, libroPendientes.Count);
            var saldos = await CalcularSaldosAsync(conc, extracto, lineasBanco, libroPendientes);
            var (puedeCerrar, motivo, bloqueos) = EvaluarCierre(conc, extracto, lineasBanco, saldos, libroPendientes.Count);

            var auditoria = await _context.TesoreriaConciliacionAuditoria.AsNoTracking()
                .Where(x => x.IdTesoreriaConciliacion == idTesoreriaConciliacion)
                .OrderByDescending(x => x.Fecha)
                .Take(40)
                .ToListAsync();

            return new ConciliacionWorkspaceDto
            {
                Conciliacion = conc,
                NombreCuenta = cuenta?.Nombre,
                Extracto = extracto,
                Saldos = saldos,
                Estadisticas = stats,
                PuedeCerrar = puedeCerrar,
                MotivoNoCerrar = motivo,
                Bloqueos = bloqueos,
                LineasBanco = lineasBanco,
                MovimientosLibroPendientes = libroPendientes,
                AuditoriaReciente = auditoria
            };
        }

        public async Task<TesoreriaExtractoImport> AdjuntarExtractoAsync(AdjuntarExtractoAConciliacionDto dto)
        {
            var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
            var import = await _context.TesoreriaExtractoImport.AsTracking().FirstOrDefaultAsync(x =>
                x.IdTesoreriaExtractoImport == dto.IdTesoreriaExtractoImport
                && x.IdEmpresa == dto.IdEmpresa)
                ?? throw new InvalidOperationException("Extracto no encontrado.");

            if (import.IdCuentaFinanciera != conc.IdCuentaFinanciera)
                throw new InvalidOperationException("El extracto pertenece a otra cuenta financiera.");

            if (string.Equals(import.Estado, "PREVIEW", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "No se puede adjuntar un extracto en PREVIEW. Confírmelo primero.");

            if (string.Equals(import.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("No se puede adjuntar un extracto anulado.");

            if (import.IdTesoreriaConciliacion is > 0
                && import.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion)
                throw new InvalidOperationException("El extracto ya está vinculado a otra conciliación.");

            import.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
            conc.IdExtractoPrincipal = import.IdTesoreriaExtractoImport;

            if (import.SaldoFinal.HasValue)
                conc.SaldoBancoFinal = import.SaldoFinal;
            if (import.SaldoInicial.HasValue)
                conc.SaldoBancoInicial = import.SaldoInicial;
            if (import.PeriodoDesde.HasValue)
                conc.PeriodoDesde = import.PeriodoDesde.Value.Date;
            if (import.PeriodoHasta.HasValue)
                conc.PeriodoHasta = import.PeriodoHasta.Value.Date;

            await _context.SaveChangesAsync();
            await AuditarAsync(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "ADJUNTAR_EXTRACTO",
                $"Extracto #{import.IdTesoreriaExtractoImport}");

            if (dto.EjecutarMatching)
            {
                await EjecutarMatchingAsync(new EjecutarMatchingConciliacionDto
                {
                    IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario
                });
            }

            return import;
        }

        public async Task<TesoreriaExtractoImport> ImportarYAdjuntarAsync(
            int idTesoreriaConciliacion,
            ImportarExtractoDto dto)
        {
            var conc = await GetConciliacionEditableAsync(idTesoreriaConciliacion, dto.IdEmpresa);
            dto.IdCuentaFinanciera = conc.IdCuentaFinanciera;
            dto.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;

            var import = await _extractoService.ImportarAsync(dto);

            var tracked = await _context.TesoreriaExtractoImport.FirstAsync(x =>
                x.IdTesoreriaExtractoImport == import.IdTesoreriaExtractoImport);
            tracked.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
            conc.IdExtractoPrincipal = tracked.IdTesoreriaExtractoImport;
            if (tracked.SaldoFinal.HasValue)
                conc.SaldoBancoFinal = tracked.SaldoFinal;
            if (tracked.SaldoInicial.HasValue)
                conc.SaldoBancoInicial = tracked.SaldoInicial;
            if (tracked.PeriodoDesde.HasValue)
                conc.PeriodoDesde = tracked.PeriodoDesde.Value.Date;
            if (tracked.PeriodoHasta.HasValue)
                conc.PeriodoHasta = tracked.PeriodoHasta.Value.Date;

            await ClasificarLineasExtractoAsync(tracked.IdTesoreriaExtractoImport);
            await _context.SaveChangesAsync();

            await AuditarAsync(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "IMPORTAR_EXTRACTO",
                $"Extracto #{tracked.IdTesoreriaExtractoImport} · {tracked.NombreArchivo}");

            await EjecutarMatchingAsync(new EjecutarMatchingConciliacionDto
            {
                IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                IdEmpresa = dto.IdEmpresa,
                IdUsuario = dto.IdUsuario,
                ToleranciaDias = dto.ToleranciaDiasMatch <= 0 ? 3 : dto.ToleranciaDiasMatch
            });

            return tracked;
        }

        public async Task<ConciliacionWorkspaceDto> EjecutarMatchingAsync(EjecutarMatchingConciliacionDto dto)
        {
            var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
            var extractoId = conc.IdExtractoPrincipal
                ?? (await _context.TesoreriaExtractoImport.AsNoTracking()
                    .Where(x => x.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion)
                    .OrderByDescending(x => x.IdTesoreriaExtractoImport)
                    .Select(x => (int?)x.IdTesoreriaExtractoImport)
                    .FirstOrDefaultAsync())
                ?? throw new InvalidOperationException("La conciliación no tiene extracto adjunto.");

            await ClasificarLineasExtractoAsync(extractoId);
            await _extractoService.SugerirMatchesAsync(extractoId, dto.IdEmpresa);

            // Vincular matches confirmados/auto a la sesión
            var lineas = await _context.TesoreriaExtractoLinea
                .AsTracking()
                .Where(x =>
                    x.IdTesoreriaExtractoImport == extractoId
                    && x.IdMovimientoFinanciero != null
                    && (x.EstadoMatch == "CONFIRMADO"
                        || x.EstadoMatch == "AUTO_CONCILIADO"
                        || x.EstadoMatch == "NUEVO_MOV"))
                .ToListAsync();

            foreach (var linea in lineas)
            {
                var mov = await _context.MovimientoFinanciero.AsTracking().FirstOrDefaultAsync(m =>
                    m.IdMovimientoFinanciero == linea.IdMovimientoFinanciero);
                if (mov == null) continue;

                mov.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                mov.EstadoConciliacion = "CONCILIADO";
                mov.FechaConciliacion ??= DateTime.UtcNow;
                mov.IdUsuarioConciliacion ??= dto.IdUsuario;

                linea.ReglaMatch ??= linea.EsAutoConciliado ? "AUTO_MONTO_FECHA_REF" : "MANUAL";
                if (string.IsNullOrWhiteSpace(linea.ExplicacionMatch) && linea.ScoreSugerido.HasValue)
                    linea.ExplicacionMatch = $"Score {linea.ScoreSugerido:0.00}";
            }

            await RecalcularDiferenciaAsync(conc);
            await _context.SaveChangesAsync();
            await AuditarAsync(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "MATCHING",
                $"Extracto #{extractoId}");

            return await GetWorkspaceAsync(conc.IdTesoreriaConciliacion, dto.IdEmpresa);
        }

        public async Task<ResolverExtractoLineaResultadoDto> ResolverLineaAsync(ResolverLineaConciliacionDto dto)
        {
            if (dto.Accion == AccionExtractoPendiente.RECLASIFICAR_PAGO)
            {
                if (dto.IdMovimientoFinanciero is not > 0)
                    throw new InvalidOperationException(
                        "RECLASIFICAR_PAGO requiere el IdMovimientoFinanciero del cobro en otra cuenta.");

                if (string.IsNullOrWhiteSpace(dto.Motivo))
                    throw new InvalidOperationException("RECLASIFICAR_PAGO requiere un motivo.");

                // Orquestación propia con su transacción (evitar anidar).
                var reclas = await _reclasificacionService.ReclasificarDesdeConciliacionAsync(
                    new ReclasificarPagoConciliacionDto
                    {
                        IdTesoreriaConciliacion = dto.IdTesoreriaConciliacion,
                        IdTesoreriaExtractoLinea = dto.IdTesoreriaExtractoLinea,
                        IdEmpresa = dto.IdEmpresa,
                        IdUsuario = dto.IdUsuario,
                        IdMovimientoFinanciero = dto.IdMovimientoFinanciero.Value,
                        Motivo = dto.Motivo
                    });

                return new ResolverExtractoLineaResultadoDto
                {
                    IdTesoreriaExtractoLinea = dto.IdTesoreriaExtractoLinea,
                    Accion = AccionExtractoPendiente.RECLASIFICAR_PAGO,
                    IdMovimientoFinanciero = reclas.IdMovimientoReclasificacion,
                    EntidadCreada = "RECLASIFICACION_PAGO",
                    YaResuelta = reclas.YaAplicada
                };
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
                var linea = await _context.TesoreriaExtractoLinea
                    .AsTracking()
                    .Include(x => x.ExtractoImport)
                    .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea)
                    ?? throw new InvalidOperationException("Línea de extracto no encontrada.");

                if (linea.ExtractoImport == null || linea.ExtractoImport.IdEmpresa != dto.IdEmpresa)
                    throw new InvalidOperationException("La línea no pertenece a la empresa.");

                if (linea.ExtractoImport.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion
                    && linea.ExtractoImport.IdTesoreriaExtractoImport != conc.IdExtractoPrincipal)
                    throw new InvalidOperationException("La línea no pertenece a esta conciliación.");

                ClasificarLinea(linea);

                // CREAR_GASTO / CREAR_INGRESO: reutilizan módulos ERP (permitido también en líneas OPERATIVO).
                // CREAR_AJUSTE: solo movimiento bancario puro.
                if (dto.Accion is AccionExtractoPendiente.CREAR_GASTO or AccionExtractoPendiente.CREAR_INGRESO
                    or AccionExtractoPendiente.CREAR_AJUSTE)
                {
                    var esBancarioPuro = string.Equals(
                        linea.ClasificacionLinea, "BANCARIO_PURO", StringComparison.OrdinalIgnoreCase);

                    if (!esBancarioPuro
                        && (string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO_AMBIGUO", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            "Hay un cobro candidato en otra cuenta. Use RECLASIFICAR_PAGO para no duplicar el ingreso.");
                    }

                    if (dto.Accion == AccionExtractoPendiente.CREAR_AJUSTE
                        && string.Equals(linea.ClasificacionLinea, "OPERATIVO", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Esta línea parece operativa ({linea.ModuloOrigenSugerido}). " +
                            "Use CREAR_GASTO / CREAR_INGRESO o asocie un movimiento del módulo origen.");
                    }

                    if (string.IsNullOrWhiteSpace(dto.Categoria))
                    {
                        dto.Categoria = dto.Accion == AccionExtractoPendiente.CREAR_INGRESO
                            ? (linea.CategoriaSugerida ?? "INTERES_BANCARIO")
                            : (linea.CategoriaSugerida ?? "COMISION_BANCARIA");
                    }

                    if (dto.Accion == AccionExtractoPendiente.CREAR_AJUSTE)
                        dto.Categoria = "AJUSTE_BANCARIO";
                }

                var result = await _extractoService.ResolverLineaAsync(new ResolverExtractoLineaDto
                {
                    IdTesoreriaExtractoLinea = dto.IdTesoreriaExtractoLinea,
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    Accion = dto.Accion,
                    IdMovimientoFinanciero = dto.IdMovimientoFinanciero,
                    Categoria = dto.Categoria,
                    Motivo = dto.Motivo
                });

                if (result.IdMovimientoFinanciero is > 0)
                {
                    var mov = await _context.MovimientoFinanciero.AsTracking().FirstOrDefaultAsync(m =>
                        m.IdMovimientoFinanciero == result.IdMovimientoFinanciero);
                    if (mov != null)
                    {
                        mov.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                        mov.EstadoConciliacion = "CONCILIADO";
                        mov.FechaConciliacion = DateTime.UtcNow;
                        mov.IdUsuarioConciliacion = dto.IdUsuario;
                    }
                }

                await RecalcularDiferenciaAsync(conc);
                AgregarAuditoria(
                    conc.IdTesoreriaConciliacion,
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    dto.Accion.ToString(),
                    $"Línea #{dto.IdTesoreriaExtractoLinea}",
                    dto.IdTesoreriaExtractoLinea,
                    result.IdMovimientoFinanciero);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return result;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task DeshacerMatchAsync(DeshacerMatchConciliacionDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
                var linea = await _context.TesoreriaExtractoLinea
                    .Include(x => x.ExtractoImport)
                    .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea)
                    ?? throw new InvalidOperationException("Línea no encontrada.");

                if (linea.ExtractoImport == null
                    || (linea.ExtractoImport.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion
                        && linea.ExtractoImport.IdTesoreriaExtractoImport != conc.IdExtractoPrincipal))
                    throw new InvalidOperationException("La línea no pertenece a esta conciliación.");

                if (linea.EstadoMatch == "NUEVO_MOV")
                {
                    throw new InvalidOperationException(
                        "No se puede deshacer un movimiento creado desde el extracto. " +
                        "Anule el movimiento financiero desde el Libro Banco si corresponde.");
                }

                var idMov = linea.IdMovimientoFinanciero;
                if (idMov is > 0)
                {
                    var mov = await _context.MovimientoFinanciero.FirstOrDefaultAsync(m =>
                        m.IdMovimientoFinanciero == idMov
                        && m.IdEmpresa == dto.IdEmpresa);
                    if (mov != null
                        && mov.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion)
                    {
                        mov.EstadoConciliacion = "PENDIENTE";
                        mov.IdTesoreriaConciliacion = null;
                        mov.FechaConciliacion = null;
                        mov.IdUsuarioConciliacion = null;
                    }
                }

                linea.EstadoMatch = "PENDIENTE";
                linea.IdMovimientoFinanciero = null;
                linea.ScoreSugerido = null;
                linea.EsAutoConciliado = false;
                linea.AccionTomada = null;
                linea.FechaResolucion = null;
                linea.IdUsuarioResolucion = null;
                linea.ReglaMatch = null;
                linea.ExplicacionMatch = dto.Motivo;

                var espejos = await _context.TesoreriaConciliacionLinea
                    .Where(l =>
                        l.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion
                        && l.IdMovimientoFinanciero == idMov)
                    .ToListAsync();
                _context.TesoreriaConciliacionLinea.RemoveRange(espejos);

                await RecalcularDiferenciaAsync(conc);
                AgregarAuditoria(
                    conc.IdTesoreriaConciliacion,
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    "DESHACER_MATCH",
                    dto.Motivo ?? $"Línea #{dto.IdTesoreriaExtractoLinea}",
                    dto.IdTesoreriaExtractoLinea,
                    idMov);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<MovimientoFinancieroListadoDto>> BuscarCandidatosAsync(
            BuscarCandidatosMatchDto dto)
        {
            var conc = await GetByIdAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa)
                ?? throw new InvalidOperationException("Conciliación no encontrada.");

            var linea = await _context.TesoreriaExtractoLinea.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea);

            var monto = dto.Monto
                ?? (linea == null ? null : (decimal?)(linea.Debito > 0 ? linea.Debito : linea.Credito));

            var desde = dto.Desde ?? conc.PeriodoDesde.AddDays(-7);
            var hasta = dto.Hasta ?? conc.PeriodoHasta.AddDays(7);
            var top = dto.Top <= 0 ? 50 : Math.Min(dto.Top, 100);

            var query = _context.MovimientoFinanciero.AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == dto.IdEmpresa
                    && m.Estado == "CONFIRMADO"
                    && m.EstadoConciliacion == "PENDIENTE"
                    && m.FechaMovimiento.Date >= desde.Date
                    && m.FechaMovimiento.Date <= hasta.Date);

            if (dto.IncluirOtrasCuentas)
            {
                // Candidatos cruzados: ENTRADA en otras cuentas (reclasificación).
                query = query.Where(m =>
                    m.IdCuentaOrigen != conc.IdCuentaFinanciera
                    && m.IdCuentaDestino != conc.IdCuentaFinanciera
                    && (m.TipoMovimiento == "ENTRADA"
                        || m.Categoria == "VENTA"
                        || m.Categoria == "COBRO_CXC"));
            }
            else
            {
                query = query.Where(m =>
                    m.IdCuentaOrigen == conc.IdCuentaFinanciera
                    || m.IdCuentaDestino == conc.IdCuentaFinanciera);
            }

            if (monto is > 0)
                query = query.Where(m => m.Monto == monto.Value);

            if (!string.IsNullOrWhiteSpace(dto.Search))
            {
                var s = dto.Search.Trim();
                query = query.Where(m =>
                    (m.Motivo != null && m.Motivo.Contains(s))
                    || (m.Observacion != null && m.Observacion.Contains(s))
                    || (m.NumeroComprobante != null && m.NumeroComprobante.Contains(s))
                    || (m.ReferenciaTipo != null && m.ReferenciaTipo.Contains(s)));
            }

            var list = await query
                .OrderByDescending(m => m.FechaMovimiento)
                .ThenByDescending(m => m.IdMovimientoFinanciero)
                .Take(top)
                .ToListAsync();

            return list.Select(m => new MovimientoFinancieroListadoDto
            {
                IdMovimientoFinanciero = m.IdMovimientoFinanciero,
                IdEmpresa = m.IdEmpresa,
                IdUsuario = m.IdUsuario,
                IdCuentaOrigen = m.IdCuentaOrigen,
                IdCuentaDestino = m.IdCuentaDestino,
                TipoMovimiento = m.TipoMovimiento,
                Categoria = m.Categoria,
                ReferenciaId = m.ReferenciaId,
                ReferenciaTipo = m.ReferenciaTipo,
                Monto = m.Monto,
                Motivo = m.Motivo,
                Observacion = m.Observacion,
                FechaMovimiento = m.FechaMovimiento,
                FechaRegistro = m.FechaRegistro,
                Estado = m.Estado,
                EstadoConciliacion = m.EstadoConciliacion,
                IdTesoreriaConciliacion = m.IdTesoreriaConciliacion,
                NumeroComprobante = m.NumeroComprobante
            });
        }

        public async Task<IEnumerable<MovimientoFinancieroListadoDto>> ListarMovimientosPendientesAsync(
            int idTesoreriaConciliacion,
            int idEmpresa)
        {
            var conc = await GetByIdAsync(idTesoreriaConciliacion, idEmpresa)
                ?? throw new InvalidOperationException("Conciliación no encontrada.");

            return await _movimientoService.ConsultarAsync(new MovimientoFinancieroFiltroDto
            {
                IdEmpresa = idEmpresa,
                IdCuentaFinanciera = conc.IdCuentaFinanciera,
                Desde = conc.PeriodoDesde,
                Hasta = conc.PeriodoHasta,
                Estado = "CONFIRMADO",
                EstadoConciliacion = "PENDIENTE"
            });
        }

        public async Task MarcarConciliadosAsync(MarcarConciliacionMovimientosDto dto)
        {
            var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
            var ahora = DateTime.UtcNow;

            foreach (var idMov in dto.IdMovimientosFinancieros.Distinct())
            {
                var mov = await _context.MovimientoFinanciero.FirstOrDefaultAsync(x =>
                    x.IdMovimientoFinanciero == idMov
                    && x.IdEmpresa == dto.IdEmpresa
                    && x.Estado == "CONFIRMADO");

                if (mov == null)
                    continue;

                if (mov.IdCuentaOrigen != conc.IdCuentaFinanciera
                    && mov.IdCuentaDestino != conc.IdCuentaFinanciera)
                    throw new InvalidOperationException(
                        $"El movimiento {idMov} no pertenece a la cuenta de la conciliación.");

                mov.EstadoConciliacion = "CONCILIADO";
                mov.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                mov.FechaConciliacion = ahora;
                mov.IdUsuarioConciliacion = dto.IdUsuario;

                var lineaExistente = await _context.TesoreriaConciliacionLinea.FirstOrDefaultAsync(l =>
                    l.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion
                    && l.IdMovimientoFinanciero == idMov);

                if (lineaExistente == null)
                {
                    var esSalida = mov.IdCuentaOrigen == conc.IdCuentaFinanciera;
                    _context.TesoreriaConciliacionLinea.Add(new TesoreriaConciliacionLinea
                    {
                        IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                        FechaMovimiento = mov.FechaMovimiento,
                        Descripcion = mov.Motivo,
                        ReferenciaBanco = mov.NumeroComprobante ?? mov.ReferenciaTipo,
                        Monto = mov.Monto,
                        TipoLinea = esSalida ? "DEBITO" : "CREDITO",
                        Conciliado = true,
                        IdMovimientoFinanciero = mov.IdMovimientoFinanciero
                    });
                }
                else
                {
                    lineaExistente.Conciliado = true;
                    lineaExistente.IdMovimientoFinanciero = idMov;
                }
            }

            await RecalcularDiferenciaAsync(conc);
            await _context.SaveChangesAsync();
            await AuditarAsync(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "MARCAR_LIBRO",
                $"{dto.IdMovimientosFinancieros.Count} movimiento(s)");
        }

        public async Task DesmarcarConciliadosAsync(MarcarConciliacionMovimientosDto dto)
        {
            var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);

            foreach (var idMov in dto.IdMovimientosFinancieros.Distinct())
            {
                var mov = await _context.MovimientoFinanciero.FirstOrDefaultAsync(x =>
                    x.IdMovimientoFinanciero == idMov
                    && x.IdEmpresa == dto.IdEmpresa
                    && x.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion);

                if (mov == null)
                    continue;

                mov.EstadoConciliacion = "PENDIENTE";
                mov.IdTesoreriaConciliacion = null;
                mov.FechaConciliacion = null;
                mov.IdUsuarioConciliacion = null;

                var lineas = await _context.TesoreriaConciliacionLinea
                    .Where(l =>
                        l.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion
                        && l.IdMovimientoFinanciero == idMov)
                    .ToListAsync();

                _context.TesoreriaConciliacionLinea.RemoveRange(lineas);
            }

            await RecalcularDiferenciaAsync(conc);
            await _context.SaveChangesAsync();
            await AuditarAsync(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "DESMARCAR_LIBRO",
                $"{dto.IdMovimientosFinancieros.Count} movimiento(s)");
        }

        public async Task<int> RegistrarCargoInteresAsync(RegistrarCargoInteresDto dto)
        {
            var conc = await GetConciliacionEditableAsync(dto.IdTesoreriaConciliacion, dto.IdEmpresa);
            var tipo = (dto.TipoMovimiento ?? "SALIDA").Trim().ToUpperInvariant();

            if (tipo is not ("ENTRADA" or "SALIDA"))
                throw new InvalidOperationException("TipoMovimiento debe ser ENTRADA o SALIDA.");

            var categoria = tipo == "ENTRADA" ? "INTERES_BANCARIO" : "COMISION_BANCARIA";
            var motivoNorm = (dto.Motivo ?? string.Empty).Trim().ToUpperInvariant();
            var clave =
                $"CONC_{conc.IdTesoreriaConciliacion}_CARGO_{tipo}_{dto.Monto:0.00}_{motivoNorm.GetHashCode():X}";

            if (tipo == "ENTRADA")
            {
                await _movimientoService.RegistrarEntradaAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    conc.IdCuentaFinanciera,
                    dto.Monto,
                    dto.Motivo,
                    dto.Observacion,
                    categoria: categoria,
                    claveIdempotencia: clave);
            }
            else
            {
                await _movimientoService.RegistrarSalidaAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    conc.IdCuentaFinanciera,
                    dto.Monto,
                    dto.Motivo,
                    dto.Observacion,
                    categoria: categoria,
                    claveIdempotencia: clave);
            }

            var mov = await _context.MovimientoFinanciero
                .FirstOrDefaultAsync(x => x.ClaveIdempotencia == clave);

            if (mov != null)
            {
                mov.EstadoConciliacion = "CONCILIADO";
                mov.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                mov.FechaConciliacion = DateTime.UtcNow;
                mov.IdUsuarioConciliacion = dto.IdUsuario;
            }

            await RecalcularDiferenciaAsync(conc);
            AgregarAuditoria(
                conc.IdTesoreriaConciliacion,
                dto.IdEmpresa,
                dto.IdUsuario,
                "CARGO_INTERES",
                $"{tipo} {dto.Monto:N2} · {dto.Motivo}",
                idMovimiento: mov?.IdMovimientoFinanciero);
            await _context.SaveChangesAsync();

            return mov?.IdMovimientoFinanciero ?? 0;
        }

        public async Task<TesoreriaConciliacion> CerrarConciliacionAsync(
            int idTesoreriaConciliacion,
            int idEmpresa,
            int idUsuario)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conc = await GetConciliacionEditableAsync(idTesoreriaConciliacion, idEmpresa);
                var workspace = await GetWorkspaceAsync(idTesoreriaConciliacion, idEmpresa);

                if (!workspace.PuedeCerrar)
                    throw new InvalidOperationException(
                        workspace.MotivoNoCerrar.Length > 0
                            ? workspace.MotivoNoCerrar
                            : "La conciliación aún no puede cerrarse.");

                await RecalcularDiferenciaAsync(conc);

                conc.Estado = "CERRADA";
                conc.SaldoConciliado = conc.SaldoBancoFinal;
                conc.FechaCierre = DateTime.UtcNow;

                if (conc.IdExtractoPrincipal is > 0)
                {
                    var import = await _context.TesoreriaExtractoImport.FirstOrDefaultAsync(x =>
                        x.IdTesoreriaExtractoImport == conc.IdExtractoPrincipal);
                    if (import != null && import.Estado != "ANULADO")
                    {
                        import.Estado = "CERRADO";
                        import.IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion;
                    }
                }

                var cuenta = await _context.CuentaFinanciera.FirstOrDefaultAsync(c =>
                    c.IdCuentaFinanciera == conc.IdCuentaFinanciera
                    && c.IdEmpresa == idEmpresa);

                if (cuenta != null)
                {
                    cuenta.FechaUltimaConciliacion = conc.PeriodoHasta;
                    cuenta.UltimoSaldoConciliado = conc.SaldoConciliado;
                }

                AgregarAuditoria(
                    conc.IdTesoreriaConciliacion,
                    idEmpresa,
                    idUsuario,
                    "CERRAR",
                    $"Saldo conciliado {conc.SaldoConciliado:N2}");
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return conc;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public Task<ReclasificarPagoResultadoDto> ReversarReclasificacionAsync(ReversarReclasificacionPagoDto dto)
            => _reclasificacionService.ReversarAsync(dto);

        public async Task<TesoreriaConciliacion> ReabrirConciliacionAsync(ReabrirConciliacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo))
                throw new InvalidOperationException("El motivo de reapertura es obligatorio.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conc = await _context.TesoreriaConciliacion.FirstOrDefaultAsync(x =>
                    x.IdTesoreriaConciliacion == dto.IdTesoreriaConciliacion
                    && x.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Conciliación no encontrada.");

                if (conc.Estado != "CERRADA")
                    throw new InvalidOperationException("Solo se pueden reabrir conciliaciones cerradas.");

                var otraAbierta = await _context.TesoreriaConciliacion.AsNoTracking().AnyAsync(x =>
                    x.IdEmpresa == dto.IdEmpresa
                    && x.IdCuentaFinanciera == conc.IdCuentaFinanciera
                    && x.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion
                    && (x.Estado == "BORRADOR" || x.Estado == "EN_PROCESO"));

                if (otraAbierta)
                    throw new InvalidOperationException(
                        "Ya existe otra conciliación abierta para esta cuenta.");

                conc.Estado = "EN_PROCESO";
                conc.IdUsuarioReapertura = dto.IdUsuario;
                conc.FechaReapertura = DateTime.UtcNow;
                conc.MotivoReapertura = dto.Motivo.Trim();
                conc.FechaCierre = null;
                conc.SaldoConciliado = null;

                if (conc.IdExtractoPrincipal is > 0)
                {
                    var import = await _context.TesoreriaExtractoImport.FirstOrDefaultAsync(x =>
                        x.IdTesoreriaExtractoImport == conc.IdExtractoPrincipal);
                    if (import != null && import.Estado == "CERRADO")
                        import.Estado = "PROCESADO";
                }

                // Restaurar marcadores de cuenta desde la última cierre previa (si existe).
                var cuenta = await _context.CuentaFinanciera.FirstOrDefaultAsync(c =>
                    c.IdCuentaFinanciera == conc.IdCuentaFinanciera
                    && c.IdEmpresa == dto.IdEmpresa);

                if (cuenta != null)
                {
                    var previa = await _context.TesoreriaConciliacion.AsNoTracking()
                        .Where(x =>
                            x.IdEmpresa == dto.IdEmpresa
                            && x.IdCuentaFinanciera == conc.IdCuentaFinanciera
                            && x.IdTesoreriaConciliacion != conc.IdTesoreriaConciliacion
                            && x.Estado == "CERRADA"
                            && x.SaldoConciliado != null)
                        .OrderByDescending(x => x.PeriodoHasta)
                        .ThenByDescending(x => x.IdTesoreriaConciliacion)
                        .FirstOrDefaultAsync();

                    if (previa != null)
                    {
                        cuenta.FechaUltimaConciliacion = previa.PeriodoHasta;
                        cuenta.UltimoSaldoConciliado = previa.SaldoConciliado;
                    }
                    else
                    {
                        cuenta.FechaUltimaConciliacion = null;
                        cuenta.UltimoSaldoConciliado = null;
                    }
                }

                AgregarAuditoria(
                    conc.IdTesoreriaConciliacion,
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    "REABRIR",
                    dto.Motivo.Trim());
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return conc;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ConciliacionResumenDto>> GetHistorialAsync(
            int idEmpresa,
            int? idCuentaFinanciera = null)
        {
            var query = _context.TesoreriaConciliacion
                .AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa);

            if (idCuentaFinanciera is > 0)
                query = query.Where(x => x.IdCuentaFinanciera == idCuentaFinanciera);

            var conciliaciones = await query
                .OrderByDescending(x => x.PeriodoHasta)
                .ThenByDescending(x => x.IdTesoreriaConciliacion)
                .ToListAsync();

            var cuentas = await _context.CuentaFinanciera
                .AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(c => c.IdCuentaFinanciera, c => c.Nombre);

            var resultado = new List<ConciliacionResumenDto>();
            foreach (var conc in conciliaciones)
            {
                var extractoId = conc.IdExtractoPrincipal
                    ?? await _context.TesoreriaExtractoImport.AsNoTracking()
                        .Where(x => x.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion)
                        .OrderByDescending(x => x.IdTesoreriaExtractoImport)
                        .Select(x => (int?)x.IdTesoreriaExtractoImport)
                        .FirstOrDefaultAsync();

                int conciliados;
                int pendientes;
                if (extractoId is > 0)
                {
                    var estados = await _context.TesoreriaExtractoLinea.AsNoTracking()
                        .Where(x => x.IdTesoreriaExtractoImport == extractoId)
                        .Select(x => x.EstadoMatch)
                        .ToListAsync();
                    conciliados = estados.Count(TesoreriaConciliacionEstados.EsConciliadaBanco);
                    pendientes = estados.Count(TesoreriaConciliacionEstados.EsPendienteBanco);
                }
                else
                {
                    // Sin extracto: fallback a movimientos del libro (sesión recién creada).
                    pendientes = await _context.MovimientoFinanciero.CountAsync(m =>
                        m.IdEmpresa == idEmpresa
                        && m.Estado == "CONFIRMADO"
                        && m.EstadoConciliacion == "PENDIENTE"
                        && (m.IdCuentaOrigen == conc.IdCuentaFinanciera || m.IdCuentaDestino == conc.IdCuentaFinanciera)
                        && m.FechaMovimiento.Date >= conc.PeriodoDesde.Date
                        && m.FechaMovimiento.Date <= conc.PeriodoHasta.Date);

                    conciliados = await _context.MovimientoFinanciero.CountAsync(m =>
                        m.IdTesoreriaConciliacion == conc.IdTesoreriaConciliacion
                        && m.EstadoConciliacion == "CONCILIADO");
                }

                resultado.Add(new ConciliacionResumenDto
                {
                    IdTesoreriaConciliacion = conc.IdTesoreriaConciliacion,
                    IdCuentaFinanciera = conc.IdCuentaFinanciera,
                    NombreCuenta = cuentas.GetValueOrDefault(conc.IdCuentaFinanciera),
                    PeriodoDesde = conc.PeriodoDesde,
                    PeriodoHasta = conc.PeriodoHasta,
                    SaldoLibrosInicial = conc.SaldoLibrosInicial,
                    SaldoLibrosFinal = conc.SaldoLibrosFinal,
                    SaldoBancoFinal = conc.SaldoBancoFinal,
                    SaldoConciliado = conc.SaldoConciliado,
                    Diferencia = conc.Diferencia,
                    ToleranciaDiferencia = conc.ToleranciaDiferencia,
                    Estado = conc.Estado,
                    FechaCreacion = conc.FechaCreacion,
                    FechaCierre = conc.FechaCierre,
                    MovimientosConciliados = conciliados,
                    MovimientosPendientes = pendientes
                });
            }

            return resultado;
        }

        public async Task<IEnumerable<MovimientoFinancieroListadoDto>> GetPendientesConciliacionAsync(
            int idEmpresa,
            int idCuentaFinanciera,
            DateTime? hasta = null)
        {
            return await _movimientoService.ConsultarAsync(new MovimientoFinancieroFiltroDto
            {
                IdEmpresa = idEmpresa,
                IdCuentaFinanciera = idCuentaFinanciera,
                Hasta = hasta ?? DateTime.Now,
                Estado = "CONFIRMADO",
                EstadoConciliacion = "PENDIENTE"
            });
        }

        private async Task ClasificarLineasExtractoAsync(int idImport)
        {
            var lineas = await _context.TesoreriaExtractoLinea
                .AsTracking()
                .Where(x => x.IdTesoreriaExtractoImport == idImport)
                .ToListAsync();

            foreach (var linea in lineas)
                ClasificarLinea(linea);

            await _context.SaveChangesAsync();
        }

        private static void ClasificarLinea(TesoreriaExtractoLinea linea)
        {
            var cat = TesoreriaExtractoService.ClasificarCategoria(
                linea.Descripcion,
                linea.Referencia,
                linea.Debito > 0);

            // Mapear categoría legacy
            if (string.Equals(cat, "GASTO_BANCARIO", StringComparison.OrdinalIgnoreCase))
                cat = "CARGO_BANCARIO";

            linea.CategoriaSugerida ??= cat;

            var texto = $"{linea.Referencia} {linea.Descripcion}".ToUpperInvariant();
            if (CategoriasBancariasPuras.Contains(cat)
                || texto.Contains("COMISION") || texto.Contains("INTERES")
                || texto.Contains("CARGO") || texto.Contains("IMPUESTO")
                || texto.Contains("COM-"))
            {
                linea.ClasificacionLinea = "BANCARIO_PURO";
                linea.ModuloOrigenSugerido = "BANCO";
                return;
            }

            if (texto.Contains("TRF") || texto.Contains("TRANSFER"))
            {
                linea.ClasificacionLinea = "OPERATIVO";
                linea.ModuloOrigenSugerido = "TRANSFERENCIA";
                return;
            }

            if (texto.Contains("DEP-") || texto.Contains("DEPOSITO") || texto.Contains("DEPÓSITO")
                || texto.Contains("COBRO") || texto.Contains("PAGO CLIENTE"))
            {
                linea.ClasificacionLinea = "OPERATIVO";
                linea.ModuloOrigenSugerido = "CUENTAS_COBRAR";
                return;
            }

            if (texto.Contains("PAGO") || texto.Contains("PROVEEDOR") || texto.Contains("CH-"))
            {
                linea.ClasificacionLinea = "OPERATIVO";
                linea.ModuloOrigenSugerido = "CUENTAS_PAGAR_PROVEEDOR";
                return;
            }

            linea.ClasificacionLinea = "DESCONOCIDO";
            linea.ModuloOrigenSugerido = null;
        }

        private async Task<List<ConciliacionLineaBancoDto>> ConstruirLineasBancoAsync(int idImport)
        {
            var lineas = await _context.TesoreriaExtractoLinea.AsNoTracking()
                .Where(x => x.IdTesoreriaExtractoImport == idImport)
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdTesoreriaExtractoLinea)
                .ToListAsync();

            var resultado = new List<ConciliacionLineaBancoDto>();
            foreach (var linea in lineas)
            {
                MovimientoFinancieroListadoDto? sugerido = null;
                if (linea.IdMovimientoFinanciero is > 0)
                {
                    var mov = await _context.MovimientoFinanciero.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.IdMovimientoFinanciero == linea.IdMovimientoFinanciero);
                    if (mov != null)
                    {
                        sugerido = new MovimientoFinancieroListadoDto
                        {
                            IdMovimientoFinanciero = mov.IdMovimientoFinanciero,
                            TipoMovimiento = mov.TipoMovimiento,
                            Categoria = mov.Categoria,
                            Monto = mov.Monto,
                            Motivo = mov.Motivo,
                            FechaMovimiento = mov.FechaMovimiento,
                            Estado = mov.Estado,
                            EstadoConciliacion = mov.EstadoConciliacion,
                            ReferenciaTipo = mov.ReferenciaTipo,
                            ReferenciaId = mov.ReferenciaId
                        };
                    }
                }

                var accion = RecomendarAccion(linea);
                var dtoLinea = new ConciliacionLineaBancoDto
                {
                    IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                    FechaMovimiento = linea.FechaMovimiento,
                    Descripcion = linea.Descripcion,
                    Referencia = linea.Referencia,
                    Debito = linea.Debito,
                    Credito = linea.Credito,
                    MontoNeto = linea.Debito > 0 ? -linea.Debito : linea.Credito,
                    Balance = linea.Balance,
                    EstadoMatch = linea.EstadoMatch,
                    IdMovimientoFinanciero = linea.IdMovimientoFinanciero,
                    ScoreSugerido = linea.ScoreSugerido,
                    CategoriaSugerida = linea.CategoriaSugerida,
                    AccionTomada = linea.AccionTomada,
                    EsAutoConciliado = linea.EsAutoConciliado,
                    AccionRecomendada = accion,
                    ClasificacionLinea = linea.ClasificacionLinea,
                    ModuloOrigenSugerido = linea.ModuloOrigenSugerido,
                    ReglaMatch = linea.ReglaMatch,
                    ExplicacionMatch = linea.ExplicacionMatch,
                    InstruccionUsuario = ConstruirInstruccion(linea, accion),
                    MovimientoSugerido = sugerido
                };

                if (string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO_AMBIGUO", StringComparison.OrdinalIgnoreCase))
                {
                    await EnriquecerSugerenciaReclasificacionAsync(dtoLinea, linea, sugerido);
                }

                if (string.Equals(linea.AccionTomada, "RECLASIFICAR_PAGO", StringComparison.OrdinalIgnoreCase))
                {
                    var reclas = await _context.PagoReclasificacion.AsNoTracking()
                        .Where(r =>
                            r.IdTesoreriaExtractoLinea == linea.IdTesoreriaExtractoLinea
                            && (r.Estado == PagoReclasificacionEstados.Aplicada
                                || r.Estado == PagoReclasificacionEstados.PendienteContable)
                            && r.IdPagoReclasificacionReversaDe == null)
                        .OrderByDescending(r => r.IdPagoReclasificacion)
                        .FirstOrDefaultAsync();
                    if (reclas != null)
                    {
                        dtoLinea.IdPagoReclasificacion = reclas.IdPagoReclasificacion;
                        dtoLinea.MetodoPagoOriginalSugerido = reclas.MetodoPagoOriginal;
                        dtoLinea.MetodoPagoEfectivoSugerido = reclas.MetodoPagoEfectivo;
                        dtoLinea.TratamientoPrevisto = reclas.Tratamiento;
                        dtoLinea.CajaOrigenCerrada = reclas.CajaEstabaCerrada;
                        dtoLinea.PeriodoOriginalCerrado = reclas.PeriodoOriginalCerrado;
                    }
                }

                resultado.Add(dtoLinea);
            }

            return resultado;
        }

        private async Task EnriquecerSugerenciaReclasificacionAsync(
            ConciliacionLineaBancoDto dto,
            TesoreriaExtractoLinea linea,
            MovimientoFinancieroListadoDto? sugerido)
        {
            dto.EsCandidatoReclasificacion = true;
            dto.MetodoPagoEfectivoSugerido = "Transferencia";
            dto.EvidenciasReclasificacion = new List<string>
            {
                $"Monto idéntico: {linea.Credito:N2}",
                $"Score: {linea.ScoreSugerido:0.00}"
            };

            MovimientoFinanciero? movOrigen = null;
            if (linea.IdMovimientoFinanciero is > 0)
            {
                movOrigen = await _context.MovimientoFinanciero.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IdMovimientoFinanciero == linea.IdMovimientoFinanciero);
            }

            var evidencia = movOrigen != null
                && TesoreriaMatchingEngine.TieneEvidenciaIdentificadora(linea, movOrigen);
            var candidatos = string.Equals(linea.EstadoMatch, "AMBIGUO", StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;
            dto.ConfianzaReclasificacion = TesoreriaMatchingEngine.NivelConfianzaCruzado(
                linea.ScoreSugerido ?? 0, evidencia, candidatos);

            if (movOrigen == null)
                return;

            var idCuenta = movOrigen.IdCuentaDestino ?? movOrigen.IdCuentaOrigen;
            dto.IdCuentaOrigenSugerida = idCuenta;
            if (idCuenta is > 0)
            {
                var cuenta = await _context.CuentaFinanciera.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCuentaFinanciera == idCuenta);
                dto.NombreCuentaOrigenSugerida = cuenta?.Nombre;
            }

            dto.EvidenciasReclasificacion.Add(
                $"Fecha ERP: {movOrigen.FechaMovimiento:yyyy-MM-dd} · Banco: {linea.FechaMovimiento:yyyy-MM-dd}");

            if (!string.IsNullOrWhiteSpace(movOrigen.Motivo))
                dto.EvidenciasReclasificacion.Add($"Motivo: {movOrigen.Motivo}");

            if (string.Equals(movOrigen.ReferenciaTipo, "FACTURA", StringComparison.OrdinalIgnoreCase)
                && movOrigen.ReferenciaId is > 0)
            {
                dto.IdFacturaHeaderSugerida = movOrigen.ReferenciaId;
                var factura = await _context.FacturaHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.IdFacturaHeader == movOrigen.ReferenciaId);
                if (factura != null)
                {
                    dto.NumeroDocumentoSugerido = factura.NumeroDocumento ?? factura.NCF;
                    dto.CajaOrigenCerrada = factura.EstaCerrada || factura.IdCajaCierre is > 0;
                    dto.MetodoPagoOriginalSugerido = factura.FormaPago ?? "Efectivo";
                    if (factura.IDCliente is > 0)
                    {
                        var cliente = await _context.Clientes.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.IDCliente == factura.IDCliente);
                        dto.ClienteSugerido = cliente?.NombreComercial;
                    }

                    dto.EvidenciasReclasificacion.Add(
                        $"Factura {(dto.NumeroDocumentoSugerido ?? $"#{factura.IdFacturaHeader}")} · forma {dto.MetodoPagoOriginalSugerido}");
                }
            }

            dto.TratamientoPrevisto = dto.CajaOrigenCerrada
                ? PagoReclasificacionTratamientos.PostCierre
                : PagoReclasificacionTratamientos.AntesCierre;
            dto.PeriodoOriginalCerrado = await _context.PeriodosContables.AsNoTracking()
                .AnyAsync(p =>
                    p.IdEmpresa == movOrigen.IdEmpresa
                    && p.Anio == movOrigen.FechaMovimiento.Year
                    && p.Mes == movOrigen.FechaMovimiento.Month
                    && p.Estado == ContabilidadConstantes.PeriodoCerrado);
        }

        private static string RecomendarAccion(TesoreriaExtractoLinea linea)
        {
            if (linea.EstadoMatch is "SUGERIDO" or "AUTO_CONCILIADO" or "CONFIRMADO" or "AMBIGUO")
            {
                if (string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO_AMBIGUO", StringComparison.OrdinalIgnoreCase))
                    return AccionExtractoPendiente.RECLASIFICAR_PAGO.ToString();

                return AccionExtractoPendiente.ASOCIAR.ToString();
            }

            if (string.Equals(linea.ClasificacionLinea, "OPERATIVO", StringComparison.OrdinalIgnoreCase))
                return AccionExtractoPendiente.ASOCIAR.ToString();

            var cat = (linea.CategoriaSugerida ?? string.Empty).ToUpperInvariant();
            if (cat.Contains("INTERES") || cat.Contains("CREDITO_AUTOMATICO"))
                return AccionExtractoPendiente.CREAR_INGRESO.ToString();
            if (cat.Contains("AJUSTE") || cat.Contains("REVERSO"))
                return AccionExtractoPendiente.CREAR_AJUSTE.ToString();
            if (linea.Debito > 0)
                return AccionExtractoPendiente.CREAR_GASTO.ToString();
            return AccionExtractoPendiente.CREAR_INGRESO.ToString();
        }

        private static string ConstruirInstruccion(TesoreriaExtractoLinea linea, string accion)
        {
            if (TesoreriaConciliacionEstados.EsConciliadaBanco(linea.EstadoMatch))
                return "Línea resuelta.";

            if (TesoreriaConciliacionEstados.EsExcluidaBanco(linea.EstadoMatch))
                return "Línea excluida (no cuenta como conciliada).";

            if (string.Equals(linea.ClasificacionLinea, "OPERATIVO", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO_AMBIGUO", StringComparison.OrdinalIgnoreCase))
                {
                    return "Cobro probablemente registrado en otra cuenta (p. ej. Caja/Efectivo). " +
                           "Reclasifique el pago hacia el banco del extracto; no cree un ingreso nuevo.";
                }

                return $"Movimiento operativo ({linea.ModuloOrigenSugerido}). " +
                       "Asocie el documento/movimiento existente del módulo origen. " +
                       "No cree un ajuste bancario genérico.";
            }

            if (string.Equals(linea.ClasificacionLinea, "BANCARIO_PURO", StringComparison.OrdinalIgnoreCase))
            {
                return accion switch
                {
                    nameof(AccionExtractoPendiente.CREAR_INGRESO) =>
                        "Movimiento bancario puro. Puede registrar interés/crédito y conciliar en un clic.",
                    nameof(AccionExtractoPendiente.CREAR_AJUSTE) =>
                        "Movimiento bancario puro. Registre el ajuste y concilie.",
                    _ => "Movimiento bancario puro. Puede registrar comisión/cargo y conciliar en un clic."
                };
            }

            if (linea.EstadoMatch == "SUGERIDO")
                return linea.ExplicacionMatch ?? "Hay una sugerencia. Confirme o busque otro movimiento.";

            if (linea.EstadoMatch is "AMBIGUO" or "DUPLICADO" or "DIFERENCIA")
                return "Requiere revisión manual: coincidencia ambigua o diferencia de monto.";

            return "Sin coincidencia clara. Busque en libro o cree el movimiento bancario si aplica.";
        }

        private async Task<ConciliacionSaldosDto> CalcularSaldosAsync(
            TesoreriaConciliacion conc,
            TesoreriaExtractoImport? extracto,
            List<ConciliacionLineaBancoDto> lineas,
            List<MovimientoFinancieroListadoDto> libroPendientes)
        {
            var estado = await _movimientoService.GetEstadoCuentaAsync(
                conc.IdCuentaFinanciera,
                conc.PeriodoDesde,
                conc.PeriodoHasta);

            var conciliadas = lineas.Where(x => TesoreriaConciliacionEstados.EsConciliadaBanco(x.EstadoMatch)).ToList();
            var pendientesBanco = lineas.Where(x => TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch)).ToList();

            var saldoBancoFinal = conc.SaldoBancoFinal
                ?? extracto?.SaldoFinal
                ?? lineas.Where(x => x.Balance.HasValue).Select(x => x.Balance).LastOrDefault();

            var marcoExtractoDesde = extracto?.PeriodoDesde?.Date
                ?? (lineas.Count > 0
                    ? lineas.Min(x => x.FechaMovimiento.Date)
                    : (DateTime?)null)
                ?? conc.PeriodoDesde.Date;

            // SaldoInicial del estado = libros al cierre del día anterior al marco del extracto.
            var estadoInicioExtracto = await _movimientoService.GetEstadoCuentaAsync(
                conc.IdCuentaFinanciera,
                marcoExtractoDesde,
                marcoExtractoDesde);

            var saldoLibrosInicioExtracto = estadoInicioExtracto.SaldoInicial;
            var saldoBancoInicioExtracto = conc.SaldoBancoInicial ?? extracto?.SaldoInicial;
            var variacionHistorica = saldoLibrosInicioExtracto - (saldoBancoInicioExtracto ?? 0m);

            var brechaBalanceCuenta = (saldoBancoFinal ?? 0) - estado.SaldoFinal;
            var extractoCompletamenteResuelto = pendientesBanco.Count == 0;
            // Preferencia UX: 0 si no hay pendientes de banco ni libro del período.
            // Si hay pendientes, residual tras restar la variación histórica del gap de saldos.
            var diferenciaPeriodo = extractoCompletamenteResuelto && libroPendientes.Count == 0
                ? 0m
                : brechaBalanceCuenta + variacionHistorica;

            return new ConciliacionSaldosDto
            {
                SaldoLibrosInicial = conc.SaldoLibrosInicial,
                SaldoLibrosFinal = estado.SaldoFinal,
                SaldoBancoInicial = saldoBancoInicioExtracto,
                SaldoBancoFinal = saldoBancoFinal,
                MontoConciliadoBanco = conciliadas.Sum(x => Math.Abs(x.MontoNeto)),
                MontoPendienteBanco = pendientesBanco.Sum(x => Math.Abs(x.MontoNeto)),
                MontoPendienteLibro = libroPendientes.Sum(x => x.Monto),
                Diferencia = brechaBalanceCuenta,
                BrechaBalanceCuenta = brechaBalanceCuenta,
                SaldoLibrosInicioExtracto = saldoLibrosInicioExtracto,
                SaldoBancoInicioExtracto = saldoBancoInicioExtracto,
                VariacionHistorica = variacionHistorica,
                DiferenciaPeriodo = diferenciaPeriodo,
                ToleranciaDiferencia = conc.ToleranciaDiferencia,
                ExtractoCompletamenteResuelto = extractoCompletamenteResuelto
            };
        }

        private static ConciliacionEstadisticasDto CalcularEstadisticas(
            List<ConciliacionLineaBancoDto> lineas,
            int libroPendientes)
        {
            return new ConciliacionEstadisticasDto
            {
                LineasBancoTotal = lineas.Count,
                LineasConciliadas = lineas.Count(x => TesoreriaConciliacionEstados.EsConciliadaBanco(x.EstadoMatch)),
                LineasSugeridas = lineas.Count(x => x.EstadoMatch == "SUGERIDO"),
                // Misma definición que filtro Pendientes / saldos / cierre.
                LineasPendientes = lineas.Count(x => TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch)),
                LineasAmbiguas = lineas.Count(x => x.EstadoMatch is "AMBIGUO" or "DUPLICADO" or "DIFERENCIA"),
                LineasBancariasPurasPendientes = lineas.Count(x =>
                    x.ClasificacionLinea == "BANCARIO_PURO"
                    && TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch)),
                LineasOperativasPendientes = lineas.Count(x =>
                    x.ClasificacionLinea == "OPERATIVO"
                    && TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch)),
                MovimientosLibroPendientes = libroPendientes,
                AutoMatches = lineas.Count(x => x.EsAutoConciliado || x.EstadoMatch == "AUTO_CONCILIADO"),
                MatchesManuales = lineas.Count(x => x.EstadoMatch == "CONFIRMADO" && !x.EsAutoConciliado),
                MovimientosCreados = lineas.Count(x => x.EstadoMatch == "NUEVO_MOV"),
                Ignorados = lineas.Count(x => TesoreriaConciliacionEstados.EsExcluidaBanco(x.EstadoMatch))
            };
        }

        /// <summary>
        /// Política default (cuadrar extracto): bloquea por pendientes de banco,
        /// pendientes de libro del período y diferencia del período.
        /// No bloquea por brecha de saldo de cuenta (historia/apertura).
        /// </summary>
        private static (bool PuedeCerrar, string Motivo, List<string> Bloqueos) EvaluarCierre(
            TesoreriaConciliacion conc,
            TesoreriaExtractoImport? extracto,
            List<ConciliacionLineaBancoDto> lineas,
            ConciliacionSaldosDto saldos,
            int movimientosLibroPendientes)
        {
            var bloqueos = new List<string>();

            if (extracto == null && lineas.Count == 0)
                bloqueos.Add("Adjunte o importe un estado de cuenta bancario.");

            var pendientes = lineas.Count(x => TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch));
            if (pendientes > 0)
                bloqueos.Add($"Hay {pendientes} línea(s) del banco sin resolver.");

            if (movimientosLibroPendientes > 0)
                bloqueos.Add(
                    $"Hay {movimientosLibroPendientes} movimiento(s) de libro del período sin conciliar.");

            if (Math.Abs(saldos.DiferenciaPeriodo) > saldos.ToleranciaDiferencia)
                bloqueos.Add(
                    $"La diferencia de conciliación del período {saldos.DiferenciaPeriodo:N2} excede la tolerancia {saldos.ToleranciaDiferencia:N2}.");

            // Reclasificaciones pendientes de asiento contable bloquean el cierre.
            if (lineas.Any(x =>
                    string.Equals(x.AccionTomada, "RECLASIFICAR_PAGO", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.EstadoMatch, "RECLASIFICACION_PENDIENTE_CONTABLE", StringComparison.OrdinalIgnoreCase)))
            {
                bloqueos.Add("Hay reclasificaciones de pago pendientes de contabilizar.");
            }

            var motivo = bloqueos.Count == 0
                ? string.Empty
                : string.Join(" ", bloqueos);

            return (bloqueos.Count == 0, motivo, bloqueos);
        }

        private async Task<TesoreriaConciliacion> GetConciliacionEditableAsync(
            int idTesoreriaConciliacion,
            int idEmpresa)
        {
            // AsTracking explícito: el contexto usa NoTracking global y estas
            // conciliaciones se mutan (adjuntar, matching, cierre).
            var conc = await _context.TesoreriaConciliacion.AsTracking().FirstOrDefaultAsync(x =>
                x.IdTesoreriaConciliacion == idTesoreriaConciliacion
                && x.IdEmpresa == idEmpresa)
                ?? throw new InvalidOperationException("Conciliación no encontrada.");

            if (conc.Estado is "CERRADA" or "ANULADA")
                throw new InvalidOperationException("La conciliación no está editable.");

            return conc;
        }

        private async Task RecalcularDiferenciaAsync(TesoreriaConciliacion conc)
        {
            var estado = await _movimientoService.GetEstadoCuentaAsync(
                conc.IdCuentaFinanciera,
                conc.PeriodoDesde,
                conc.PeriodoHasta);

            conc.SaldoLibrosFinal = estado.SaldoFinal;
            conc.Diferencia = (conc.SaldoBancoFinal ?? 0) - conc.SaldoLibrosFinal;
            _context.TesoreriaConciliacion.Update(conc);
        }

        private void AgregarAuditoria(
            int idConciliacion,
            int idEmpresa,
            int? idUsuario,
            string accion,
            string? detalle,
            int? idLinea = null,
            int? idMovimiento = null)
        {
            _context.TesoreriaConciliacionAuditoria.Add(new TesoreriaConciliacionAuditoria
            {
                IdTesoreriaConciliacion = idConciliacion,
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Accion = accion,
                Detalle = detalle,
                IdTesoreriaExtractoLinea = idLinea,
                IdMovimientoFinanciero = idMovimiento,
                Fecha = DateTime.UtcNow
            });
        }

        private async Task AuditarAsync(
            int idConciliacion,
            int idEmpresa,
            int? idUsuario,
            string accion,
            string? detalle,
            int? idLinea = null,
            int? idMovimiento = null)
        {
            AgregarAuditoria(idConciliacion, idEmpresa, idUsuario, accion, detalle, idLinea, idMovimiento);
            await _context.SaveChangesAsync();
        }
    }
}
