using System.Text.Json;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class NominaProcesoService : INominaProcesoService
    {
        private static readonly string[] IngresosMotor = { "SUELDO_BASE", "HORAS_EXTRA" };
        private static readonly string[] DeduccionesLegales = { "AFP_EMPLEADO", "SFS_EMPLEADO", "ISR_EMPLEADO" };

        private readonly AlahiaPosContext _db;
        private readonly PayrollDbContext _payroll;
        private readonly IRrhhAsistenciaService _asistencia;
        private readonly IEvaluationContextBuilder _contextBuilder;
        private readonly IPayrollEngine _engine;
        private readonly IPayrollRunStore _runStore;

        private readonly IPayrollRulePack _pack;
        private readonly IMovimientoFinancieroService _tesoreria;
        private readonly IContabilidadEventPublisher _contabilidad;

        public NominaProcesoService(
            AlahiaPosContext db,
            PayrollDbContext payroll,
            IRrhhAsistenciaService asistencia,
            IEvaluationContextBuilder contextBuilder,
            IPayrollEngine engine,
            IPayrollRunStore runStore,
            IPayrollRulePack pack,
            IMovimientoFinancieroService tesoreria,
            IContabilidadEventPublisher contabilidad)
        {
            _db = db;
            _payroll = payroll;
            _asistencia = asistencia;
            _contextBuilder = contextBuilder;
            _engine = engine;
            _runStore = runStore;
            _pack = pack;
            _tesoreria = tesoreria;
            _contabilidad = contabilidad;
        }

        public async Task<IReadOnlyList<NominaProcesoVistaDto>> ListarAsync(int idEmpresa)
        {
            var rows = await _db.NominaProceso.AsNoTracking()
                .Include(p => p.Empleados)
                .Where(p => p.IdEmpresa == idEmpresa)
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();
            return rows.Select(p => ToVista(p, incluirDetalle: false)).ToList();
        }

        public async Task<NominaProcesoVistaDto> GetAsync(int idEmpresa, int id)
        {
            var p = await Cargar(idEmpresa, id, tracking: false);
            return ToVista(p, incluirDetalle: true);
        }

        public async Task<NominaProcesoVistaDto> CrearAsync(NominaProcesoCrearDto dto, int idUsuario)
        {
            if (dto.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es obligatorio.");
            var inicio = dto.FechaInicio.Date;
            var fin = dto.FechaFin.Date;
            if (fin < inicio)
                throw new ArgumentException("El período es inválido.");

            var periodKey = $"{inicio:yyyyMMdd}-{fin:yyyyMMdd}";
            var intent = string.IsNullOrWhiteSpace(dto.Intent) ? "REGULAR" : dto.Intent.Trim().ToUpperInvariant();
            var exists = await _db.NominaProceso.AnyAsync(p =>
                p.IdEmpresa == dto.IdEmpresa && p.PeriodKey == periodKey && p.Intent == intent);
            if (exists)
                throw new InvalidOperationException("Ya existe un proceso de nómina para ese período.");

            var row = new NominaProceso
            {
                IdEmpresa = dto.IdEmpresa,
                PeriodKey = periodKey,
                Intent = intent,
                FechaInicio = inicio,
                FechaFin = fin,
                Frecuencia = string.IsNullOrWhiteSpace(dto.Frecuencia) ? "QUINCENAL" : dto.Frecuencia.Trim().ToUpperInvariant(),
                Estado = RrhhEstados.NominaBorrador,
                Observacion = dto.Observacion,
                IdUsuarioCrea = idUsuario
            };
            _db.NominaProceso.Add(row);
            row.Eventos.Add(Evento(null, RrhhEstados.NominaBorrador, idUsuario, "Proceso creado"));
            await _db.SaveChangesAsync();
            return ToVista(row, incluirDetalle: true);
        }

        public async Task<NominaProcesoVistaDto> GenerarPrenominaAsync(int idEmpresa, int id, int idUsuario)
        {
            var p = await Cargar(idEmpresa, id, tracking: true);
            AsegurarEditable(p);

            await _asistencia.CalcularAsync(new RrhhCalcularAsistenciaRequest
            {
                IdEmpresa = idEmpresa,
                Desde = p.FechaInicio,
                Hasta = p.FechaFin
            });

            var laborables = await _db.EmpleadoLaboral.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa && e.EstadoLaboral == "ACTIVO")
                .ToListAsync();
            var nombres = await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(e => e.IdEmpleados, e => e.Nombre ?? "");
            var cargos = await _db.RrhhCargo.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(c => c.IdCargo);

            var pack = _pack;
            var period = new PayrollPeriod(
                DateOnly.FromDateTime(p.FechaInicio),
                DateOnly.FromDateTime(p.FechaFin),
                p.PeriodKey);

            var diasLaborablesPeriodo = ContarDiasLaborables(p.FechaInicio, p.FechaFin);

            _db.NominaProcesoEmpleado.RemoveRange(p.Empleados);
            p.Empleados.Clear();

            foreach (var lab in laborables)
            {
                var asistencia = await _db.RrhhAsistenciaDia.AsNoTracking()
                    .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == lab.IdEmpleados
                                && a.Fecha >= p.FechaInicio && a.Fecha <= p.FechaFin)
                    .ToListAsync();

                var minutosExtra = asistencia.Sum(a => a.MinutosExtra);
                var solicitudes = await _db.RrhhSolicitudAusencia.AsNoTracking()
                    .Where(s => s.IdEmpresa == idEmpresa && s.IdEmpleados == lab.IdEmpleados
                                && s.Estado == RrhhEstados.Aprobada
                                && s.FechaInicio <= p.FechaFin && s.FechaFin >= p.FechaInicio)
                    .ToListAsync();
                var diasSinGoceReal = 0m;
                foreach (var s in solicitudes.Where(x => !x.ConGoceSueldo && x.Unidad != "HORAS"))
                    diasSinGoceReal += s.Cantidad;
                diasSinGoceReal += asistencia.Count(a => a.Estado == RrhhEstados.Ausente && !a.EsJustificado);

                var horasExtra = Math.Round(minutosExtra / 60m, 2, MidpointRounding.AwayFromZero);
                var cargo = lab.IdCargo is > 0 && cargos.TryGetValue(lab.IdCargo.Value, out var c) ? c : null;
                var salarioMensual = cargo is { SalarioBase: > 0 } ? cargo.SalarioBase : lab.SalarioBase;
                var horasMes = 190.67m;
                var tarifaHora = salarioMensual <= 0 ? 0 : Math.Round(salarioMensual / horasMes, 4, MidpointRounding.AwayFromZero);
                var montoHe = Math.Round(horasExtra * tarifaHora, 2, MidpointRounding.AwayFromZero);

                await ReemplazarHechosAsistenciaAsync(idEmpresa, lab.IdEmpleados, p.PeriodKey, horasExtra, montoHe);

                var salarioPeriodo = ToPeriodAmount(salarioMensual, p.Frecuencia);
                var descAsistencia = diasLaborablesPeriodo <= 0
                    ? 0
                    : Math.Round(salarioPeriodo * (diasSinGoceReal / diasLaborablesPeriodo), 2, MidpointRounding.AwayFromZero);

                var asignaciones = await _db.NominaConceptoAsignacion.AsNoTracking()
                    .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == lab.IdEmpleados && a.Activo)
                    .ToListAsync();
                var comisiones = SumAsignacion(asignaciones, "COMISION");
                var bonif = SumAsignacion(asignaciones, "BONIFICACION");
                var beneficiosCargo = asignaciones
                    .Where(a => a.Nota == "Desde cargo"
                                && a.ConceptCode != "SUELDO_BASE"
                                && a.ConceptCode != "COMISION"
                                && a.ConceptCode != "BONIFICACION")
                    .Sum(a => a.MontoFijo ?? 0);

                var prestamo = await _db.RrhhPrestamoCuota
                    .Where(c => c.Estado == "PENDIENTE"
                                && c.Prestamo != null
                                && c.Prestamo.IdEmpresa == idEmpresa
                                && c.Prestamo.IdEmpleados == lab.IdEmpleados
                                && c.Prestamo.Estado == "ACTIVO"
                                && c.FechaProgramada >= p.FechaInicio
                                && c.FechaProgramada <= p.FechaFin)
                    .SumAsync(c => (decimal?)c.Monto) ?? 0;
                var anticipo = await _db.RrhhAnticipo
                    .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == lab.IdEmpleados
                                && a.Estado == "PENDIENTE"
                                && a.Fecha >= p.FechaInicio && a.Fecha <= p.FechaFin)
                    .SumAsync(a => (decimal?)a.Monto) ?? 0;
                var consumo = await _db.FacturaHeaders
                    .Where(f => f.IdEmpresa == idEmpresa
                                && f.IdEmpleadoConsumo == lab.IdEmpleados
                                && f.CargarConsumoNomina
                                && !f.EstaCancelada
                                && f.Pendiente > 0
                                && f.IdNominaDescuento == null)
                    .SumAsync(f => (decimal?)f.Pendiente) ?? 0;

                IReadOnlyList<PayrollLine> lines = Array.Empty<PayrollLine>();
                try
                {
                    var ctx = await _contextBuilder.BuildAsync(new EvaluationContextBuildRequest(
                        idEmpresa,
                        lab.IdEmpleados,
                        period,
                        pack.Metadata.PackId,
                        pack.Metadata.Version,
                        pack.Metadata.DefaultMoneyPolicy,
                        pack.Metadata.Parameters));
                    var calc = _engine.Calculate(new PayrollCalculationRequest(
                        idEmpresa,
                        new ExecutionKey(idEmpresa, p.PeriodKey, p.Intent),
                        period,
                        pack.Metadata.PackId,
                        new[] { ctx }), pack);

                    lines = calc.Lines.Select(l => l with
                    {
                        Attributes = MergeAttr(l.Attributes, lab.IdEmpleados)
                    }).ToList();
                }
                catch (Exception ex)
                {
                    lines = Array.Empty<PayrollLine>();
                    p.Observacion = TrimObs(p.Observacion, $"{nombres.GetValueOrDefault(lab.IdEmpleados)}: {ex.Message}");
                }

                var sueldo = SumLines(lines, "SUELDO_BASE");
                var heMotor = SumLines(lines, "HORAS_EXTRA");
                var legales = DeduccionesLegales.Sum(c => SumLines(lines, c));
                var brutoMotor = IngresosMotor.Sum(c => SumLines(lines, c));
                var netoMotor = SumLines(lines, "NETO");
                var bruto = brutoMotor + comisiones + bonif + beneficiosCargo;
                var neto = (netoMotor == 0 && brutoMotor == 0 ? sueldo : netoMotor)
                           + comisiones + bonif + beneficiosCargo
                           - descAsistencia - prestamo - anticipo - consumo;

                p.Empleados.Add(new NominaProcesoEmpleado
                {
                    IdEmpleados = lab.IdEmpleados,
                    NombreEmpleado = nombres.GetValueOrDefault(lab.IdEmpleados),
                    SalarioBase = sueldo != 0 ? sueldo : salarioPeriodo,
                    HorasExtra = heMotor != 0 ? heMotor : montoHe,
                    Comisiones = comisiones,
                    Bonificaciones = bonif,
                    DescuentosAsistencia = descAsistencia,
                    Prestamos = prestamo,
                    Anticipos = anticipo,
                    OtrosDescuentos = consumo,
                    OtrosIngresos = beneficiosCargo,
                    DeduccionesLegales = legales,
                    Bruto = bruto,
                    Neto = neto,
                    DiasAusenteSinGoce = diasSinGoceReal,
                    MinutosExtra = minutosExtra,
                    LineasJson = JsonSerializer.Serialize(lines)
                });
            }

            p.Eventos.Add(Evento(p.Estado, p.Estado, idUsuario, "Pre-nómina generada"));
            await _db.SaveChangesAsync();
            return ToVista(p, incluirDetalle: true);
        }

        public async Task<NominaProcesoVistaDto> CambiarEstadoAsync(
            int idEmpresa, int id, string nuevoEstado, int idUsuario, string? comentario, int? idCuentaFinanciera = null)
        {
            var p = await Cargar(idEmpresa, id, tracking: true);
            var destino = (nuevoEstado ?? "").Trim().ToUpperInvariant();
            var origen = p.Estado;

            if (origen == destino)
                return ToVista(p, incluirDetalle: true);

            ValidarTransicion(origen, destino);

            if (destino is RrhhEstados.NominaAprobada)
                await AprobarMotorAsync(p);

            string? tipoCuenta = null;
            if (destino is RrhhEstados.NominaPagada)
            {
                tipoCuenta = await RegistrarPagoTesoreriaAsync(p, idUsuario, idCuentaFinanciera);
                await RegistrarGastoNominaAsync(p, idUsuario, tipoCuenta);
                await MarcarPagosAsync(p);
            }

            p.Estado = destino;
            switch (destino)
            {
                case RrhhEstados.NominaEnRevision:
                    p.IdUsuarioRevision = idUsuario;
                    p.FechaRevision = DateTime.UtcNow;
                    break;
                case RrhhEstados.NominaAprobada:
                    p.IdUsuarioAprueba = idUsuario;
                    p.FechaAprobacion = DateTime.UtcNow;
                    break;
                case RrhhEstados.NominaPagada:
                    p.IdUsuarioPaga = idUsuario;
                    p.FechaPago = DateTime.UtcNow;
                    break;
                case RrhhEstados.NominaCerrada:
                    p.IdUsuarioCierra = idUsuario;
                    p.FechaCierre = DateTime.UtcNow;
                    break;
            }

            p.Eventos.Add(Evento(origen, destino, idUsuario, comentario));
            await _db.SaveChangesAsync();

            var vista = ToVista(p, incluirDetalle: true);
            if (destino is RrhhEstados.NominaPagada)
                vista.AdvertenciaContabilidad = await PublicarContabilidadNominaAsync(p, idUsuario, tipoCuenta);
            return vista;
        }

        private async Task AprobarMotorAsync(NominaProceso p)
        {
            if (p.Empleados.Count == 0)
                throw new InvalidOperationException("Genere la pre-nómina antes de aprobar.");

            var pack = _pack;
            var period = new PayrollPeriod(
                DateOnly.FromDateTime(p.FechaInicio),
                DateOnly.FromDateTime(p.FechaFin),
                p.PeriodKey);
            var key = new ExecutionKey(p.IdEmpresa, p.PeriodKey, p.Intent);

            var contextos = new List<EvaluationContext>();
            foreach (var emp in p.Empleados)
            {
                try
                {
                    contextos.Add(await _contextBuilder.BuildAsync(new EvaluationContextBuildRequest(
                        p.IdEmpresa, emp.IdEmpleados, period,
                        pack.Metadata.PackId, pack.Metadata.Version,
                        pack.Metadata.DefaultMoneyPolicy, pack.Metadata.Parameters)));
                }
                catch
                {
                    /* expediente incompleto: se liquida solo en el host */
                }
            }

            if (contextos.Count == 0)
                throw new InvalidOperationException("Ningún empleado tiene expediente laboral listo para el motor de nómina.");

            var existing = await _runStore.GetByExecutionKeyAsync(key);
            if (existing != null && existing.Status == PayrollRunStatus.Approved)
            {
                p.PayrollRunId = existing.PayrollRunId;
                return;
            }

            if (existing is { Status: PayrollRunStatus.Draft })
            {
                var entity = await _payroll.Runs.FirstOrDefaultAsync(r => r.PayrollRunId == existing.PayrollRunId);
                if (entity != null)
                {
                    _payroll.Runs.Remove(entity);
                    await _payroll.SaveChangesAsync();
                }
            }

            var draft = _engine.Calculate(new PayrollCalculationRequest(
                p.IdEmpresa, key, period, pack.Metadata.PackId, contextos), pack);
            await _runStore.SaveDraftAsync(draft);
            var approved = _engine.Approve(draft, new PayrollApprovalRequest(p.IdEmpresa, draft.PayrollRunId, key));
            await _runStore.SaveApprovedAsync(approved);
            p.PayrollRunId = approved.PayrollRunId;
        }

        private async Task<string?> RegistrarPagoTesoreriaAsync(
            NominaProceso p, int idUsuario, int? idCuentaFinanciera)
        {
            if (p.Empleados.Count == 0)
                throw new InvalidOperationException("Genere la pre-nómina antes de marcarla pagada.");

            var totalNeto = Math.Round(p.Empleados.Sum(e => e.Neto), 2, MidpointRounding.AwayFromZero);
            if (totalNeto < 0)
                throw new InvalidOperationException("El neto de la nómina no puede ser negativo.");

            if (totalNeto == 0)
            {
                if (idCuentaFinanciera is > 0)
                    p.IdCuentaFinanciera = idCuentaFinanciera;
                return null;
            }

            if (idCuentaFinanciera is not > 0)
                throw new InvalidOperationException(
                    "Seleccione la cuenta de banco o caja de donde sale el pago de nómina.");

            var cuenta = await _db.CuentaFinanciera.AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdCuentaFinanciera == idCuentaFinanciera.Value
                    && c.IdEmpresa == p.IdEmpresa)
                ?? throw new InvalidOperationException("La cuenta financiera no pertenece a esta empresa.");

            if (!cuenta.Activa)
                throw new InvalidOperationException("La cuenta financiera no está activa.");

            var periodo = $"{p.FechaInicio:dd/MM/yyyy} - {p.FechaFin:dd/MM/yyyy}";
            var clave = $"NOMINA-{p.IdNominaProceso}";
            var motivo = $"Nómina {p.PeriodKey} ({periodo})";

            try
            {
                await _tesoreria.RegistrarSalidaAsync(
                    p.IdEmpresa,
                    idUsuario,
                    cuenta.IdCuentaFinanciera,
                    totalNeto,
                    motivo,
                    $"Salida automática por pago de nómina. Neto {totalNeto:N2}.",
                    categoria: "NOMINA",
                    referenciaId: p.IdNominaProceso,
                    referenciaTipo: "NOMINA",
                    claveIdempotencia: clave);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(ex.Message);
            }

            var movimiento = await _db.MovimientoFinanciero.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.IdEmpresa == p.IdEmpresa && m.ClaveIdempotencia == clave);

            p.IdCuentaFinanciera = cuenta.IdCuentaFinanciera;
            p.IdMovimientoFinanciero = movimiento?.IdMovimientoFinanciero;
            return cuenta.TipoCuenta;
        }

        private async Task RegistrarGastoNominaAsync(NominaProceso p, int idUsuario, string? tipoCuenta)
        {
            var totalNeto = Math.Round(p.Empleados.Sum(e => e.Neto), 2, MidpointRounding.AwayFromZero);
            if (totalNeto <= 0)
                return;

            var referencia = $"NOMINA-{p.IdNominaProceso}";
            var existe = await _db.Gastos.AnyAsync(g =>
                g.IdEmpresa == p.IdEmpresa
                && g.Referencia == referencia
                && !g.EstaAnulado);
            if (existe)
                return;

            var categoria = await AsegurarCategoriaNominaAsync(p.IdEmpresa);
            var idProveedor = await _db.Proveedores.AsNoTracking()
                .Where(x => x.IdEmpresa == p.IdEmpresa && x.IsActivo)
                .Select(x => (int?)x.IdProveedor)
                .FirstOrDefaultAsync() ?? 0;
            if (idProveedor <= 0)
                throw new InvalidOperationException(
                    "No hay un proveedor activo para registrar el gasto de nómina.");

            var periodo = $"{p.FechaInicio:dd/MM/yyyy} - {p.FechaFin:dd/MM/yyyy}";
            var formaPago = string.Equals(tipoCuenta, "CAJA", StringComparison.OrdinalIgnoreCase)
                ? "EFECTIVO"
                : "TRANSFERENCIA";

            _db.Gastos.Add(new Gastos
            {
                IdEmpresa = p.IdEmpresa,
                FechaInseccion = DateTime.Now,
                TipoGasto = "Nómina",
                CategoriaGasto = categoria,
                IdCategoriaGasto = categoria.IdCategoriaGasto > 0 ? categoria.IdCategoriaGasto : null,
                TipoComprobante = GastoComprobanteTipos.SinComprobante,
                IdProveedor = idProveedor,
                Monto = totalNeto,
                Detalle = $"Pago de nomina {p.PeriodKey} ({periodo})",
                IdUsuario = idUsuario > 0 ? idUsuario : null,
                FormaPago = formaPago,
                Orien = "NOMINA",
                IdCuentaFinanciera = p.IdCuentaFinanciera,
                Referencia = referencia,
                OrigenModulo = "NOMINA",
                EstaAnulado = false,
                EstaCerrada = false
            });
        }

        private async Task<CategoriaGasto> AsegurarCategoriaNominaAsync(int idEmpresa)
        {
            const string nombre = "Nómina";
            var actual = await _db.CategoriasGasto
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.Nombre == nombre);
            if (actual != null)
            {
                if (!actual.Activo)
                    actual.Activo = true;
                return actual;
            }

            var mapeo = await _db.ContabilidadCuentaMapeo.AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.Activo
                    && (m.CodigoConcepto == ContabilidadConceptosMapeo.GastoNomina
                        || m.CodigoConcepto == ContabilidadConceptosMapeo.GastoOperativo))
                .OrderBy(m => m.CodigoConcepto == ContabilidadConceptosMapeo.GastoNomina ? 0 : 1)
                .FirstOrDefaultAsync();

            var cat = new CategoriaGasto
            {
                IdEmpresa = idEmpresa,
                Nombre = nombre,
                Descripcion = "Pago de nomina a colaboradores",
                Activo = true,
                Orden = 13,
                IdCuentaContable = mapeo?.IdCuentaContable,
                FechaCreacion = DateTime.UtcNow
            };
            _db.CategoriasGasto.Add(cat);
            return cat;
        }

        private async Task<string?> PublicarContabilidadNominaAsync(
            NominaProceso p, int idUsuario, string? tipoCuenta)
        {
            var totalNeto = Math.Round(p.Empleados.Sum(e => e.Neto), 2, MidpointRounding.AwayFromZero);
            var totalBruto = Math.Round(p.Empleados.Sum(e => e.Bruto), 2, MidpointRounding.AwayFromZero);
            var totalPrestamos = Math.Round(p.Empleados.Sum(e => e.Prestamos), 2, MidpointRounding.AwayFromZero);
            var totalAnticipos = Math.Round(p.Empleados.Sum(e => e.Anticipos), 2, MidpointRounding.AwayFromZero);
            var totalAfp = Math.Round(p.Empleados.Sum(e => SumLegalJson(e.LineasJson, "AFP_EMPLEADO")), 2, MidpointRounding.AwayFromZero);
            var totalSfs = Math.Round(p.Empleados.Sum(e => SumLegalJson(e.LineasJson, "SFS_EMPLEADO")), 2, MidpointRounding.AwayFromZero);
            var totalIsr = Math.Round(p.Empleados.Sum(e => SumLegalJson(e.LineasJson, "ISR_EMPLEADO")), 2, MidpointRounding.AwayFromZero);

            if (totalNeto <= 0 && totalAfp <= 0 && totalSfs <= 0 && totalIsr <= 0
                && totalPrestamos <= 0 && totalAnticipos <= 0)
                return null;

            var periodo = $"{p.FechaInicio:dd/MM/yyyy} - {p.FechaFin:dd/MM/yyyy}";
            var contab = await _contabilidad.TryPublishAsync(new NominaPagadaEvent
            {
                IdEmpresa = p.IdEmpresa,
                IdUsuario = idUsuario,
                Fecha = p.FechaPago ?? DateTime.Now,
                ReferenciaId = p.IdNominaProceso,
                ReferenciaTipo = "Nomina",
                TotalNeto = totalNeto,
                TotalBruto = totalBruto,
                TotalAfp = totalAfp,
                TotalSfs = totalSfs,
                TotalIsr = totalIsr,
                TotalPrestamos = totalPrestamos,
                TotalAnticipos = totalAnticipos,
                IdCuentaFinanciera = p.IdCuentaFinanciera,
                TipoCuentaFinanciera = tipoCuenta,
                PeriodKey = p.PeriodKey,
                Detalle = $"Nómina {p.PeriodKey} ({periodo})"
            });

            return string.IsNullOrWhiteSpace(contab.Advertencia) ? null : contab.Advertencia;
        }

        private static decimal SumLegalJson(string? json, string code)
        {
            if (string.IsNullOrWhiteSpace(json))
                return 0;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return 0;

                decimal sum = 0;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;
                    var concepto = PropString(el, "ConceptCode") ?? PropString(el, "conceptCode");
                    if (!string.Equals(concepto, code, StringComparison.OrdinalIgnoreCase))
                        continue;
                    sum += PropDecimal(el, "Amount") ?? PropDecimal(el, "amount") ?? 0;
                }
                return sum;
            }
            catch
            {
                return 0;
            }
        }

        private static string? PropString(JsonElement el, string name) =>
            el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

        private static decimal? PropDecimal(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p))
                return null;
            return p.ValueKind switch
            {
                JsonValueKind.Number => p.GetDecimal(),
                JsonValueKind.String when decimal.TryParse(p.GetString(), out var n) => n,
                _ => null
            };
        }

        private async Task MarcarPagosAsync(NominaProceso p)
        {
            var ids = p.Empleados.Select(e => e.IdEmpleados).ToList();
            var cuotas = await _db.RrhhPrestamoCuota.AsTracking()
                .Include(c => c.Prestamo)
                .Where(c => c.Estado == "PENDIENTE"
                            && c.Prestamo != null
                            && c.Prestamo.IdEmpresa == p.IdEmpresa
                            && ids.Contains(c.Prestamo.IdEmpleados)
                            && c.FechaProgramada >= p.FechaInicio
                            && c.FechaProgramada <= p.FechaFin)
                .ToListAsync();
            foreach (var c in cuotas)
            {
                c.Estado = "APLICADA";
                c.PeriodKey = p.PeriodKey;
                if (c.Prestamo != null)
                {
                    c.Prestamo.Saldo = Math.Max(0, c.Prestamo.Saldo - c.Monto);
                    if (c.Prestamo.Saldo <= 0) c.Prestamo.Estado = "LIQUIDADO";
                }
            }

            var anticipos = await _db.RrhhAnticipo.AsTracking()
                .Where(a => a.IdEmpresa == p.IdEmpresa && ids.Contains(a.IdEmpleados)
                            && a.Estado == "PENDIENTE"
                            && a.Fecha >= p.FechaInicio && a.Fecha <= p.FechaFin)
                .ToListAsync();
            foreach (var a in anticipos)
            {
                a.Estado = "APLICADO";
                a.PeriodKey = p.PeriodKey;
            }

            var consumos = await _db.FacturaHeaders.AsTracking()
                .Where(f => f.IdEmpresa == p.IdEmpresa
                            && f.IdEmpleadoConsumo != null
                            && ids.Contains(f.IdEmpleadoConsumo.Value)
                            && f.CargarConsumoNomina
                            && !f.EstaCancelada
                            && f.Pendiente > 0
                            && f.IdNominaDescuento == null)
                .ToListAsync();
            foreach (var f in consumos)
            {
                f.Pagado = f.Total;
                f.Pendiente = 0;
                f.Estado = "Pagada";
                f.EstaCancelada = false;
                f.IdNominaDescuento = p.IdNominaProceso;
            }
        }

        private async Task ReemplazarHechosAsistenciaAsync(
            int idEmpresa, int idEmpleados, string periodKey, decimal horasExtra, decimal montoHe)
        {
            var viejos = await _payroll.PeriodFacts
                .Where(f => f.IdEmpresa == idEmpresa && f.IdEmpleados == idEmpleados
                            && f.PeriodKey == periodKey && f.Source == "ATTENDANCE")
                .ToListAsync();
            _payroll.PeriodFacts.RemoveRange(viejos);
            _payroll.PeriodFacts.Add(new PayrollPeriodFactEntity
            {
                IdEmpresa = idEmpresa,
                IdEmpleados = idEmpleados,
                PeriodKey = periodKey,
                FactType = "HORAS_EXTRA",
                ConceptCode = "HORAS_EXTRA",
                Quantity = horasExtra,
                Amount = montoHe,
                Source = "ATTENDANCE"
            });
            await _payroll.SaveChangesAsync();
        }

        private async Task<NominaProceso> Cargar(int idEmpresa, int id, bool tracking)
        {
            var q = tracking
                ? _db.NominaProceso.AsTracking().Include(p => p.Empleados).Include(p => p.Eventos)
                : _db.NominaProceso.AsNoTracking().Include(p => p.Empleados).Include(p => p.Eventos);
            return await q.FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.IdNominaProceso == id)
                ?? throw new InvalidOperationException("Proceso de nómina no encontrado.");
        }

        private static void AsegurarEditable(NominaProceso p)
        {
            if (RrhhEstados.NominaBloqueada(p.Estado))
                throw new InvalidOperationException("Una nómina aprobada, pagada o cerrada no se puede modificar.");
        }

        private static void ValidarTransicion(string origen, string destino)
        {
            var ok = (origen, destino) switch
            {
                (RrhhEstados.NominaBorrador, RrhhEstados.NominaEnRevision) => true,
                (RrhhEstados.NominaEnRevision, RrhhEstados.NominaBorrador) => true,
                (RrhhEstados.NominaEnRevision, RrhhEstados.NominaAprobada) => true,
                (RrhhEstados.NominaAprobada, RrhhEstados.NominaPagada) => true,
                (RrhhEstados.NominaPagada, RrhhEstados.NominaCerrada) => true,
                _ => false
            };
            if (!ok)
                throw new InvalidOperationException($"Transición no permitida: {origen} → {destino}.");
        }

        private static NominaProcesoVistaDto ToVista(NominaProceso p, bool incluirDetalle) => new()
        {
            IdNominaProceso = p.IdNominaProceso,
            IdEmpresa = p.IdEmpresa,
            PeriodKey = p.PeriodKey,
            Intent = p.Intent,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            Frecuencia = p.Frecuencia,
            Estado = p.Estado,
            PayrollRunId = p.PayrollRunId,
            Observacion = p.Observacion,
            FechaCreacion = p.FechaCreacion,
            Empleados = p.Empleados?.Count ?? 0,
            TotalBruto = p.Empleados?.Sum(e => e.Bruto) ?? 0,
            TotalNeto = p.Empleados?.Sum(e => e.Neto) ?? 0,
            IdCuentaFinanciera = p.IdCuentaFinanciera,
            IdMovimientoFinanciero = p.IdMovimientoFinanciero,
            Detalle = incluirDetalle ? p.Empleados : null,
            Eventos = incluirDetalle ? p.Eventos?.OrderBy(e => e.Fecha).ToList() : null
        };

        private static NominaProcesoEvento Evento(string? anterior, string nuevo, int usuario, string? comentario) => new()
        {
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            IdUsuario = usuario,
            Comentario = comentario
        };

        private static decimal ToPeriodAmount(decimal salarioBaseMensual, string frecuencia) =>
            frecuencia?.ToUpperInvariant() switch
            {
                "SEMANAL" => Math.Round(salarioBaseMensual / 4m, 2, MidpointRounding.AwayFromZero),
                "QUINCENAL" => Math.Round(salarioBaseMensual / 2m, 2, MidpointRounding.AwayFromZero),
                _ => salarioBaseMensual
            };

        private static decimal SumAsignacion(IEnumerable<NominaConceptoAsignacion> list, string code) =>
            list.Where(a => string.Equals(a.ConceptCode, code, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.MontoFijo ?? 0);

        private static decimal SumLines(IEnumerable<PayrollLine> lines, string code) =>
            lines.Where(l => string.Equals(l.ConceptCode, code, StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.Amount);

        private static IReadOnlyDictionary<string, string> MergeAttr(
            IReadOnlyDictionary<string, string>? current, int idEmpleados)
        {
            var d = current is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(current, StringComparer.Ordinal);
            d["IdEmpleados"] = idEmpleados.ToString();
            return d;
        }

        private static int ContarDiasLaborables(DateTime inicio, DateTime fin)
        {
            var n = 0;
            for (var d = inicio.Date; d <= fin.Date; d = d.AddDays(1))
            {
                var dow = d.DayOfWeek;
                if (dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday) n++;
            }
            return Math.Max(n, 1);
        }

        private static string TrimObs(string? actual, string extra)
        {
            var joined = string.IsNullOrWhiteSpace(actual) ? extra : actual + " | " + extra;
            return joined.Length <= 400 ? joined : joined[..400];
        }
    }
}
