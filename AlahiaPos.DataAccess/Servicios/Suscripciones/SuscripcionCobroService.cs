using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Suscripciones
{
    public class SuscripcionCobroService : ISuscripcionCobroService
    {
        private const string CanalCentro = "NOTIF_CENTRO";

        private readonly AlahiaPosContext _ctx;
        private readonly IEnumerable<INotificacionSuscripcionCanal> _canales;
        private readonly INotificacionCentro _notificaciones;
        private readonly ILogger<SuscripcionCobroService> _logger;
        private readonly IConfiguration _config;

        public SuscripcionCobroService(
            AlahiaPosContext ctx,
            IEnumerable<INotificacionSuscripcionCanal> canales,
            INotificacionCentro notificaciones,
            ILogger<SuscripcionCobroService> logger,
            IConfiguration config)
        {
            _ctx = ctx;
            _canales = canales;
            _notificaciones = notificaciones;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Día del ciclo de cobro. En Development puede forzarse con
        /// Suscripcion:ForzarDia (+ opcional ForzarDiaEmpresaId) para pruebas.
        /// </summary>
        private int ObtenerDiaCobro(int? idEmpresa = null)
        {
            var forzarRaw = _config["Suscripcion:ForzarDia"];
            if (int.TryParse(forzarRaw, out var forzar) && forzar >= 1 && forzar <= 31)
            {
                var soloRaw = _config["Suscripcion:ForzarDiaEmpresaId"];
                int? soloEmpresa = int.TryParse(soloRaw, out var idFiltro) ? idFiltro : null;
                if (!soloEmpresa.HasValue || soloEmpresa == idEmpresa)
                {
                    _logger.LogDebug(
                        "Suscripcion ForzarDia={Dia} activo (empresa filtro={Filtro}, empresa={Empresa})",
                        forzar, soloEmpresa, idEmpresa);
                    return forzar;
                }
            }

            return DateTime.Now.Day;
        }

        public bool PuedeOperar(Empresas empresa)
        {
            if (empresa == null) return false;
            if (empresa.EsEmpresaSistema) return true;
            if (EsDemoVigente(empresa)) return true;

            var estado = NormalizarEstado(empresa.EstadoServicio);
            if (estado == SuscripcionEstados.Activa
                || estado == SuscripcionEstados.PendientePago)
                return true;

            // Voucher enviado en ventana de pago (día 30–3): puede operar mientras se valida.
            // Si ya estaba suspendido (día ≥ 4), no opera hasta aprobación admin.
            if (estado == SuscripcionEstados.PagoReportado)
                return EstaEnVentanaPago(ObtenerDiaCobro(empresa.IdEmpresa));

            return false;
        }

        public bool EstaBloqueada(Empresas empresa)
        {
            if (empresa == null) return true;
            if (empresa.EsEmpresaSistema) return false;
            if (EsDemoVigente(empresa)) return false;

            var estado = NormalizarEstado(empresa.EstadoServicio);
            if (estado == SuscripcionEstados.Suspendida
                || estado == SuscripcionEstados.Cancelada)
                return true;

            // Reportó pago ya suspendido (fuera de ventana): bloqueado hasta validar.
            if (estado == SuscripcionEstados.PagoReportado
                && !EstaEnVentanaPago(ObtenerDiaCobro(empresa.IdEmpresa)))
                return true;

            return false;
        }

        /// <summary>Día 30 o días 1–3 del ciclo: periodo de gracia de pago.</summary>
        private static bool EstaEnVentanaPago(int diaCobro) =>
            diaCobro == 30 || (diaCobro >= 1 && diaCobro <= 3);

        public AlertaPagoDto? ObtenerAlertaPago(Empresas empresa)
        {
            if (empresa == null || empresa.EsEmpresaSistema) return null;
            if (EsDemoVigente(empresa)) return null;

            var dia = ObtenerDiaCobro(empresa.IdEmpresa);
            var estado = NormalizarEstado(empresa.EstadoServicio);

            // Pantalla de bloqueo (no banner): suspendida / cancelada
            if (estado == SuscripcionEstados.Suspendida)
            {
                return new AlertaPagoDto
                {
                    Tipo = "critico",
                    Mensaje = "Su servicio está suspendido por falta de pago. Reporte el pago adjuntando su voucher para reactivarlo."
                };
            }

            if (estado == SuscripcionEstados.Cancelada)
            {
                return new AlertaPagoDto
                {
                    Tipo = "critico",
                    Mensaje = "Su servicio fue cancelado. Contacte a MacroBits."
                };
            }

            // Invariante: tras aprobar voucher (ACTIVA / pagado) o con voucher en validación → nunca re-pedir cobro.
            if (empresa.PagadoServicio
                || estado == SuscripcionEstados.Activa
                || estado == SuscripcionEstados.PagoReportado)
                return null;

            var pendienteCobro = estado == SuscripcionEstados.PendientePago;

            // Solo dos avisos UI/correo del ciclo: día 30 (inicio) y día 3 (último)
            if (!pendienteCobro || (dia != 30 && dia != 3))
                return null;

            var (titulo, mensaje) = MensajeAviso(TipoAvisoPorDia(dia));
            return new AlertaPagoDto
            {
                Tipo = dia == 3 ? "critico" : "advertencia",
                Mensaje = $"{titulo}. {mensaje}",
                DiaCobro = dia,
                DiasRestantes = DiasHastaLimitePago(dia)
            };
        }

        /// <summary>Días restantes hasta el día 3 (límite de pago).</summary>
        private static int DiasHastaLimitePago(int diaCobro) => diaCobro switch
        {
            30 => 3,
            1 => 2,
            2 => 1,
            3 => 0,
            _ => Math.Max(0, 3 - diaCobro)
        };

        public async Task ActualizarEstadoEmpresaAsync(int idEmpresa)
        {
            var emp = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (emp == null || emp.EsEmpresaSistema) return;

            await AplicarReglasEmpresaAsync(emp, enviarAvisos: false);
            await _ctx.SaveChangesAsync();
        }

        public async Task ProcesarCicloDiarioAsync(CancellationToken ct = default)
        {
            var empresas = await _ctx.Empresas.AsTracking()
                .Where(e => !e.EsEmpresaSistema && e.Estado)
                .ToListAsync(ct);

            foreach (var emp in empresas)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    await AplicarReglasEmpresaAsync(emp, enviarAvisos: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando suscripción empresa {Id}", emp.IdEmpresa);
                }
            }

            await _ctx.SaveChangesAsync(ct);
        }

        private async Task AplicarReglasEmpresaAsync(Empresas emp, bool enviarAvisos)
        {
            if (NormalizarEstado(emp.EstadoServicio) == SuscripcionEstados.Cancelada)
                return;

            // Plan Demo / prueba gratis: MontoServicio = 0 mientras FechaTerminacion esté vigente.
            if (await EsPlanDemoAsync(emp))
            {
                if (emp.FechaTerminacion.Date >= DateTime.Now.Date)
                {
                    emp.PagadoServicio = true;
                    emp.EstadoServicio = SuscripcionEstados.Activa;
                }
                return;
            }

            var hoy = DateTime.Now;
            var dia = ObtenerDiaCobro(emp.IdEmpresa);
            var tienePendiente = await TienePagoPendienteAsync(emp.IdEmpresa);
            var ciclo = await ObtenerOCrearCicloActualAsync(emp);

            // ═══════════════════════════════════════════════════════════════
            // INVARIANTE (admin aprobó voucher): ciclo PAGADO ⇒ no pedir cobro
            // de nuevo hasta que exista un ciclo NUEVO sin pagar (próximo día 30).
            // ═══════════════════════════════════════════════════════════════
            if (ciclo != null && ciclo.Estado == SuscripcionEstados.CicloPagado)
            {
                if (tienePendiente)
                {
                    await CerrarPagosPendientesAsync(
                        emp.IdEmpresa,
                        exceptoIdPago: null,
                        motivo: "Descartado: el ciclo vigente ya está pagado (aprobado por admin).",
                        usuarioValida: "SISTEMA");
                    tienePendiente = false;
                }

                emp.PagadoServicio = true;
                emp.EstadoServicio = SuscripcionEstados.Activa;
                emp.FechaProximoPago = ProximoDia30(hoy);
                return;
            }

            // Día 30: abre ciclo del mes actual (ABIERTO). Solo entonces se puede
            // volver a pedir pago — nunca si el ciclo vigente ya fue aprobado.
            if (dia == 30)
            {
                // Solo resetear flag si el último pago no cubre ESTE ciclo nuevo
                var pagoCubreCicloNuevo = emp.FechaUltimoPago.HasValue
                    && emp.FechaUltimoPago.Value.Date == hoy.Date;
                if (emp.PagadoServicio && !pagoCubreCicloNuevo)
                    emp.PagadoServicio = false;
            }

            // Pagado (y sin voucher huérfano): activo — sella el ciclo
            if (emp.PagadoServicio)
            {
                if (tienePendiente)
                {
                    await CerrarPagosPendientesAsync(
                        emp.IdEmpresa,
                        exceptoIdPago: null,
                        motivo: "Descartado automáticamente: la empresa ya figura como pagada.",
                        usuarioValida: "SISTEMA");
                    tienePendiente = false;
                }

                emp.EstadoServicio = SuscripcionEstados.Activa;
                emp.FechaProximoPago = ProximoDia30(hoy);
                if (ciclo != null && ciclo.Estado != SuscripcionEstados.CicloPagado)
                    ciclo.Estado = SuscripcionEstados.CicloPagado;
                return;
            }

            if (tienePendiente)
            {
                if (emp.EstadoServicio != SuscripcionEstados.PagoReportado)
                {
                    emp.EstadoServicio = SuscripcionEstados.PagoReportado;
                    await RegistrarEventoAsync(emp.IdEmpresa, "ESTADO_PAGO_REPORTADO",
                        "Empresa con pago en validación", idCiclo: ciclo?.IdCiclo);
                }
                // Sin reenviar avisos de cobro: ya subió voucher.
                return;
            }

            // Ventana de pago: día 30 y días 1–3
            if (dia == 30 || dia <= 3)
            {
                var prev = emp.EstadoServicio;
                emp.EstadoServicio = SuscripcionEstados.PendientePago;
                emp.FechaProximoPago = ProximoDia30(hoy);

                if (prev != SuscripcionEstados.PendientePago)
                {
                    await RegistrarEventoAsync(emp.IdEmpresa, "ESTADO_PENDIENTE_PAGO",
                        "Periodo de pago del ciclo", idCiclo: ciclo?.IdCiclo);
                }

                // Correo + notificación solo día 30 (inicio) y día 3 (último aviso)
                if (enviarAvisos && (dia == 30 || dia == 3))
                    await EnviarAvisoSiCorrespondeAsync(emp, ciclo, TipoAvisoPorDia(dia));

                return;
            }

            // Día ≥ 4 → suspensión + cargo de reconexión pendiente
            if (dia >= 4)
            {
                if (ciclo != null && ciclo.Estado == SuscripcionEstados.CicloAbierto)
                    ciclo.Estado = SuscripcionEstados.CicloVencido;

                if (emp.EstadoServicio != SuscripcionEstados.Suspendida)
                {
                    emp.EstadoServicio = SuscripcionEstados.Suspendida;
                    emp.ReconexionPendiente = true;

                    await RegistrarEventoAsync(emp.IdEmpresa, "SUSPENSION",
                        "Suspensión automática por falta de pago validado. Cargo de reconexión pendiente.",
                        idCiclo: ciclo?.IdCiclo);

                    if (enviarAvisos)
                        await EnviarAvisoSiCorrespondeAsync(emp, ciclo, SuscripcionEstados.AvisoSuspension);

                    // Recalcular ciclo para incluir cargo de reconexión en el total a pagar
                    if (ciclo != null && ciclo.Estado != SuscripcionEstados.CicloPagado)
                    {
                        var calc = CalcularFacturaDesdeEmpresa(emp);
                        ciclo.Monto = calc.Total;
                        await GuardarDetalleCicloAsync(ciclo.IdCiclo, calc);
                    }
                }
                else if (!emp.ReconexionPendiente)
                {
                    // Ya estaba suspendida sin flag (datos viejos): asegurar el cargo
                    emp.ReconexionPendiente = true;
                    if (ciclo != null && ciclo.Estado != SuscripcionEstados.CicloPagado)
                    {
                        var calc = CalcularFacturaDesdeEmpresa(emp);
                        ciclo.Monto = calc.Total;
                        await GuardarDetalleCicloAsync(ciclo.IdCiclo, calc);
                    }
                }
            }
        }

        private static string TipoAvisoPorDia(int dia) => dia switch
        {
            30 => SuscripcionEstados.AvisoDia30,
            3 => SuscripcionEstados.AvisoDia3,
            _ => SuscripcionEstados.AvisoDia30
        };

        private async Task EnviarAvisoSiCorrespondeAsync(Empresas emp, SuscripcionCiclo? ciclo, string tipoAviso)
        {
            if (ciclo == null) return;
            // Solo día 30 y día 3 (último). Día 2 y otros no envían.
            if (tipoAviso != SuscripcionEstados.AvisoDia30
                && tipoAviso != SuscripcionEstados.AvisoDia3
                && tipoAviso != SuscripcionEstados.AvisoSuspension)
                return;

            var yaEnviadoCentro = await _ctx.SuscripcionAvisoLog.AnyAsync(a =>
                a.IdEmpresa == emp.IdEmpresa
                && a.IdCiclo == ciclo.IdCiclo
                && a.TipoAviso == tipoAviso
                && a.Canal == CanalCentro);

            if (yaEnviadoCentro) return;

            var (titulo, mensaje) = MensajeAviso(tipoAviso);
            var esSuspension = tipoAviso == SuscripcionEstados.AvisoSuspension;

            await _notificaciones.PublicarAsync(new NotificacionEvento
            {
                Tipo = esSuspension ? NotificacionTipos.ServicioSuspendido : NotificacionTipos.PagoPendiente,
                IdEmpresa = emp.IdEmpresa,
                DestinoTipo = NotificacionDestinos.Empresa,
                Prioridad = esSuspension ? NotificacionPrioridades.Error : NotificacionPrioridades.Advertencia,
                Titulo = titulo,
                Mensaje = mensaje,
                Ruta = esSuspension ? "/servicio-suspendido" : "/pago-suscripcion",
                ReferenciaTipo = "SuscripcionCiclo",
                ReferenciaId = ciclo.IdCiclo,
                CorreoDestino = emp.CorreElectronico,
                NombreEmpresa = emp.NombreComercial
            });

            _ctx.SuscripcionAvisoLog.Add(new SuscripcionAvisoLog
            {
                IdEmpresa = emp.IdEmpresa,
                IdCiclo = ciclo.IdCiclo,
                TipoAviso = tipoAviso,
                Canal = CanalCentro,
                FechaEnvio = DateTime.Now
            });

            await RegistrarEventoAsync(emp.IdEmpresa, "AVISO_ENVIADO", titulo, CanalCentro, idCiclo: ciclo.IdCiclo);
            // El correo lo envía NotificacionCentro (canal EMAIL) con el mismo mensaje/instrucciones.
        }

        private static (string titulo, string mensaje) MensajeAviso(string tipo)
        {
            const string pasosVoucher =
                "Cómo proceder:\n" +
                "1) Inicie sesión en Alahia ERP.\n" +
                "2) Vaya a Pago de Suscripción (o use Reportar pago si el servicio está suspendido).\n" +
                "3) Adjunte la foto o PDF del voucher de transferencia.\n" +
                "4) Envíe el comprobante.\n" +
                "Si aún está en el periodo de pago (día 30 al 3), podrá seguir operando mientras se valida. " +
                "Si el servicio ya fue suspendido, el acceso se restaura solo cuando MacroBits apruebe el pago.\n" +
                "No es necesario subir el voucher más de una vez.";

            return tipo switch
            {
                SuscripcionEstados.AvisoDia30 => (
                    "Renovación de suscripción Alahia ERP",
                    "Su ciclo de suscripción inicia hoy (día 30). Tiene hasta el día 3 para reportar el pago; de lo contrario el acceso se suspenderá el día 4.\n\n" + pasosVoucher
                ),
                SuscripcionEstados.AvisoDia3 => (
                    "Último aviso de pago — Alahia ERP",
                    "Hoy es el último día del periodo de gracia. Si no reporta su voucher hoy, el servicio se suspenderá a partir de mañana (día 4).\n\n" + pasosVoucher
                ),
                SuscripcionEstados.AvisoSuspension => (
                    "Servicio suspendido — Alahia ERP",
                    "Su servicio está suspendido por falta de pago validado. " +
                    "Al regularizar deberá pagar también el cargo por reconexión (se muestra en el total a cobrar).\n\n" + pasosVoucher
                ),
                _ => ("Aviso de suscripción", "Tiene un pendiente relacionado con su suscripción.\n\n" + pasosVoucher)
            };
        }

        public async Task<SuscripcionCiclo?> ObtenerOCrearCicloActualAsync(Empresas empresa)
        {
            var hoy = DateTime.Now;
            // Del 1 al 29 el ciclo vigente es el del mes anterior iniciado el 30;
            // el día 30 abre el ciclo del mes actual.
            int anio = hoy.Year;
            int mes = hoy.Month;
            if (hoy.Day < 30)
            {
                var prev = hoy.AddMonths(-1);
                // Ciclo de cobro: mes de facturación = mes del día 30 que lo abrió
                // Si hoy es abril 2, el ciclo abierto el 30 de marzo es Anio=marzo, Mes=marzo
                anio = prev.Year;
                mes = prev.Month;
            }

            // Día 30: ciclo del mes actual
            if (hoy.Day == 30)
            {
                anio = hoy.Year;
                mes = hoy.Month;
            }

            var ciclo = await _ctx.SuscripcionCiclo.AsTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == empresa.IdEmpresa && c.Anio == anio && c.Mes == mes);

            if (ciclo != null) return ciclo;

            var calc = await CalcularFacturaAsync(empresa.IdEmpresa, hoy);

            ciclo = new SuscripcionCiclo
            {
                IdEmpresa = empresa.IdEmpresa,
                Anio = anio,
                Mes = mes,
                FechaGeneracion = DateTime.Now,
                Monto = calc.Total,
                IdPlan = empresa.IdPlan,
                Estado = SuscripcionEstados.CicloAbierto
            };
            _ctx.SuscripcionCiclo.Add(ciclo);
            await _ctx.SaveChangesAsync();

            await GuardarDetalleCicloAsync(ciclo.IdCiclo, calc);

            await RegistrarEventoAsync(empresa.IdEmpresa, "CICLO_GENERADO",
                $"Ciclo {mes}/{anio} generado. Total USD {calc.Total:0.00} (plan {calc.MontoPlan:0.00} + cargos {calc.MontoCargos:0.00})",
                idCiclo: ciclo.IdCiclo);

            return ciclo;
        }

        public async Task<SuscripcionCalculoFacturaDto> CalcularFacturaAsync(int idEmpresa, DateTime? fechaReferencia = null)
        {
            _ = fechaReferencia;
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            return CalcularFacturaDesdeEmpresa(empresa);
        }

        private SuscripcionCalculoFacturaDto CalcularFacturaDesdeEmpresa(Empresas empresa)
        {
            var idEmpresa = empresa.IdEmpresa;
            var nombreEmpresa = string.IsNullOrWhiteSpace(empresa.NombreComercial)
                ? $"Empresa {idEmpresa}"
                : empresa.NombreComercial.Trim();
            var nombrePlan = $"Plan {nombreEmpresa}";

            var montoServicio = empresa.MontoServicio < 0 ? 0m : empresa.MontoServicio;
            var cargoAdicional = empresa.CargoAdicional < 0 ? 0m : empresa.CargoAdicional;
            var tasa = ObtenerTasaUsdDop();
            var cargoReconexDop = ObtenerCargoReconexionDop(empresa);

            var calc = new SuscripcionCalculoFacturaDto
            {
                IdEmpresa = idEmpresa,
                IdPlan = empresa.IdPlan,
                NombrePlan = nombrePlan,
                MontoServicio = montoServicio,
                CargoAdicional = cargoAdicional,
                LimiteFacturacion = empresa.LimiteFacturacion < 0 ? 0 : empresa.LimiteFacturacion,
                CargoReconexionDop = cargoReconexDop,
                ReconexionPendiente = empresa.ReconexionPendiente,
                MontoPlan = montoServicio,
                MontoCargos = cargoAdicional,
                Total = montoServicio + cargoAdicional,
                TasaUsdDop = tasa
            };

            calc.Lineas.Add(new SuscripcionLineaFacturaDto
            {
                TipoLinea = TipoCargoRecurrente.Plan,
                Codigo = $"PLAN_EMP_{idEmpresa}",
                Nombre = nombrePlan,
                Monto = montoServicio
            });

            if (cargoAdicional > 0)
            {
                calc.Lineas.Add(new SuscripcionLineaFacturaDto
                {
                    TipoLinea = TipoCargoRecurrente.Servicio,
                    Codigo = $"CARGO_ADIC_{idEmpresa}",
                    Nombre = "Cargo adicional",
                    Monto = cargoAdicional
                });
            }

            if (empresa.ReconexionPendiente && cargoReconexDop > 0)
            {
                var reconexUsd = RedondearUsd(cargoReconexDop / tasa);
                calc.MontoReconexion = reconexUsd;
                calc.MontoReconexionDop = RedondearDop(cargoReconexDop);
                calc.MontoCargos += reconexUsd;
                calc.Total += reconexUsd;

                calc.Lineas.Add(new SuscripcionLineaFacturaDto
                {
                    TipoLinea = TipoCargoRecurrente.Reconexion,
                    Codigo = $"RECONEX_{idEmpresa}",
                    Nombre = "Cargo por reconexión",
                    Monto = reconexUsd,
                    MontoDop = calc.MontoReconexionDop
                });
            }

            calc.MontoPlanDop = RedondearDop(calc.MontoPlan * tasa);
            calc.MontoCargosDop = RedondearDop(calc.CargoAdicional * tasa) + calc.MontoReconexionDop;
            calc.TotalDop = calc.MontoPlanDop + calc.MontoCargosDop;
            foreach (var linea in calc.Lineas)
            {
                if (linea.MontoDop <= 0)
                    linea.MontoDop = RedondearDop(linea.Monto * tasa);
            }

            return calc;
        }

        private decimal ObtenerCargoReconexionDop(Empresas empresa)
        {
            // 0 = desactivado para ese cliente. Si por algún motivo viniera negativo, usa config/500.
            if (empresa.CargoReconexionDop >= 0)
                return empresa.CargoReconexionDop;

            var raw = _config["Suscripcion:CargoReconexionDop"];
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var cfg)
                && cfg >= 0)
                return cfg;

            return 500m;
        }

        private decimal ObtenerTasaUsdDop()
        {
            var raw = _config["Suscripcion:TasaUsdDop"];
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var tasa)
                && tasa > 0)
                return tasa;
            return 60m;
        }

        private static decimal RedondearDop(decimal valor) =>
            Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        private static decimal RedondearUsd(decimal valor) =>
            Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        public async Task RecalcularCicloAbiertoAsync(int idEmpresa)
        {
            var empresa = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null || empresa.EsEmpresaSistema) return;

            var ciclo = await ObtenerOCrearCicloActualAsync(empresa);
            if (ciclo == null || ciclo.Estado == SuscripcionEstados.CicloPagado)
                return;

            var tracked = await _ctx.SuscripcionCiclo.AsTracking()
                .FirstOrDefaultAsync(c => c.IdCiclo == ciclo.IdCiclo) ?? ciclo;

            var calc = await CalcularFacturaAsync(idEmpresa, DateTime.Now);
            tracked.Monto = calc.Total;
            tracked.IdPlan = calc.IdPlan;
            _ctx.SuscripcionCiclo.Update(tracked);
            await GuardarDetalleCicloAsync(tracked.IdCiclo, calc);
            await _ctx.SaveChangesAsync();

            await RegistrarEventoAsync(idEmpresa, "CICLO_RECALCULADO",
                $"Ciclo #{tracked.IdCiclo} recalculado. Total USD {calc.Total:0.00}",
                idCiclo: tracked.IdCiclo);
        }

        public async Task<SuscripcionCalculoFacturaDto> ActualizarTarifaEmpresaAsync(ActualizarTarifaEmpresaDto dto)
        {
            var emp = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            if (emp.EsEmpresaSistema)
                throw new Exception("No aplica a la empresa de sistema.");

            if (dto.MontoServicio < 0 || dto.CargoAdicional < 0)
                throw new Exception("Los montos no pueden ser negativos.");
            if (dto.LimiteFacturacion < 0)
                throw new Exception("El límite de facturación no puede ser negativo.");

            var antes =
                $"servicio={emp.MontoServicio:0.00}; cargo={emp.CargoAdicional:0.00}; limite={emp.LimiteFacturacion}";

            emp.MontoServicio = dto.MontoServicio;
            emp.CargoAdicional = dto.CargoAdicional;
            emp.LimiteFacturacion = dto.LimiteFacturacion;
            if (dto.CargoReconexionDop.HasValue)
            {
                if (dto.CargoReconexionDop.Value < 0)
                    throw new Exception("El cargo de reconexión no puede ser negativo.");
                emp.CargoReconexionDop = dto.CargoReconexionDop.Value;
            }
            // Mantener legado alineado para reportes antiguos
            emp.PrecioPlanEspecialUsd = dto.MontoServicio;

            await _ctx.SaveChangesAsync();

            await RegistrarEventoAsync(dto.IdEmpresa, "TARIFA_EMPRESA",
                $"{antes} → servicio={emp.MontoServicio:0.00}; cargo={emp.CargoAdicional:0.00}; limite={emp.LimiteFacturacion}",
                idUsuario: dto.IdUsuario);

            await RecalcularCicloAbiertoAsync(dto.IdEmpresa);
            return await CalcularFacturaAsync(dto.IdEmpresa);
        }

        /// <summary>Legado: mapea a MontoServicio.</summary>
        public async Task<SuscripcionCalculoFacturaDto> ActualizarPrecioPlanEspecialAsync(ActualizarPrecioPlanEspecialDto dto)
        {
            var emp = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            return await ActualizarTarifaEmpresaAsync(new ActualizarTarifaEmpresaDto
            {
                IdEmpresa = dto.IdEmpresa,
                MontoServicio = dto.PrecioPlanEspecialUsd ?? emp.MontoServicio,
                CargoAdicional = emp.CargoAdicional,
                LimiteFacturacion = emp.LimiteFacturacion,
                IdUsuario = dto.IdUsuario
            });
        }

        private async Task GuardarDetalleCicloAsync(int idCiclo, SuscripcionCalculoFacturaDto calc)
        {
            var existentes = await _ctx.SuscripcionCicloDetalle
                .Where(d => d.IdCiclo == idCiclo)
                .ToListAsync();
            if (existentes.Count > 0)
                _ctx.SuscripcionCicloDetalle.RemoveRange(existentes);

            foreach (var linea in calc.Lineas)
            {
                _ctx.SuscripcionCicloDetalle.Add(new SuscripcionCicloDetalle
                {
                    IdCiclo = idCiclo,
                    TipoLinea = linea.TipoLinea,
                    IdCargo = linea.IdCargo,
                    IdModulo = linea.IdModulo,
                    Codigo = linea.Codigo,
                    Nombre = linea.Nombre,
                    Monto = linea.Monto
                });
            }

            await _ctx.SaveChangesAsync();
        }

        public async Task<List<SuscripcionLineaFacturaDto>> ListarDetalleCicloAsync(int idCiclo)
        {
            return await _ctx.SuscripcionCicloDetalle.AsNoTracking()
                .Where(d => d.IdCiclo == idCiclo)
                .OrderBy(d => d.Id)
                .Select(d => new SuscripcionLineaFacturaDto
                {
                    TipoLinea = d.TipoLinea,
                    IdCargo = d.IdCargo,
                    IdModulo = d.IdModulo,
                    Codigo = d.Codigo,
                    Nombre = d.Nombre,
                    Monto = d.Monto
                }).ToListAsync();
        }

        private async Task<bool> TienePagoPendienteAsync(int idEmpresa)
        {
            return await _ctx.PagosEmpresa.AnyAsync(p =>
                p.IdEmpresa == idEmpresa && p.Estado == "PENDIENTE");
        }

        public async Task RegistrarEventoAsync(
            int idEmpresa, string tipo, string? detalle,
            string? canal = null, int? idUsuario = null, int? idCiclo = null, string? metadataJson = null)
        {
            _ctx.SuscripcionEvento.Add(new SuscripcionEvento
            {
                IdEmpresa = idEmpresa,
                IdCiclo = idCiclo,
                Tipo = tipo,
                Detalle = detalle,
                Canal = canal,
                IdUsuario = idUsuario,
                MetadataJson = metadataJson,
                Fecha = DateTime.Now
            });
            // Save deferred to caller when in batch; for isolated calls save:
            await _ctx.SaveChangesAsync();
        }

        public async Task OnPagoReportadoAsync(int idEmpresa, int idPago, int? idCiclo)
        {
            var emp = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (emp == null || emp.EsEmpresaSistema) return;

            emp.EstadoServicio = SuscripcionEstados.PagoReportado;
            _ctx.Empresas.Update(emp);
            await RegistrarEventoAsync(idEmpresa, "PAGO_REPORTADO",
                $"Pago #{idPago} reportado por el cliente", idCiclo: idCiclo);
            await _ctx.SaveChangesAsync();
        }

        public async Task OnPagoAprobadoAsync(int idEmpresa, int idPago, int? idCiclo, string? usuarioValida)
        {
            var emp = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (emp == null) return;

            // Evita que un voucher huérfano vuelva a poner PAGO_REPORTADO tras aprobar
            await CerrarPagosPendientesAsync(
                idEmpresa,
                exceptoIdPago: idPago,
                motivo: $"Descartado al aprobar pago #{idPago}.",
                usuarioValida: usuarioValida ?? "ADMIN");

            emp.PagadoServicio = true;
            emp.EstadoServicio = SuscripcionEstados.Activa;
            emp.ReconexionPendiente = false;
            emp.FechaUltimoPago = DateTime.Now;
            emp.FechaProximoPago = ProximoDia30(DateTime.Now);
            _ctx.Empresas.Update(emp);

            // Sella el ciclo del voucher Y el ciclo vigente: tras aprobar, no se vuelve a pedir pago.
            var idsCicloSellar = new HashSet<int>();
            if (idCiclo.HasValue) idsCicloSellar.Add(idCiclo.Value);

            var cicloActual = await ObtenerOCrearCicloActualAsync(emp);
            if (cicloActual != null) idsCicloSellar.Add(cicloActual.IdCiclo);

            foreach (var id in idsCicloSellar)
            {
                var ciclo = await _ctx.SuscripcionCiclo.AsTracking()
                    .FirstOrDefaultAsync(c => c.IdCiclo == id);
                if (ciclo != null)
                {
                    ciclo.Estado = SuscripcionEstados.CicloPagado;
                    _ctx.SuscripcionCiclo.Update(ciclo);
                }
            }

            await RegistrarEventoAsync(idEmpresa, "REACTIVACION",
                $"Pago #{idPago} aprobado por {usuarioValida}. Servicio reactivado. Ciclo sellado PAGADO.",
                idCiclo: idCiclo ?? cicloActual?.IdCiclo);
            await _ctx.SaveChangesAsync();
        }

        /// <summary>Admin marca pagado sin voucher (o tras validación externa).</summary>
        public async Task OnMarcarPagoManualAsync(int idEmpresa, string? usuario = null)
        {
            var emp = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (emp == null) return;

            await CerrarPagosPendientesAsync(
                idEmpresa,
                exceptoIdPago: null,
                motivo: "Descartado al marcar pago manualmente.",
                usuarioValida: usuario ?? "ADMIN");

            emp.PagadoServicio = true;
            emp.EstadoServicio = SuscripcionEstados.Activa;
            emp.ReconexionPendiente = false;
            emp.FechaUltimoPago = DateTime.Now;
            emp.FechaProximoPago = ProximoDia30(DateTime.Now);
            _ctx.Empresas.Update(emp);

            var ciclo = await ObtenerOCrearCicloActualAsync(emp);
            if (ciclo != null)
            {
                var tracked = await _ctx.SuscripcionCiclo.AsTracking()
                    .FirstOrDefaultAsync(c => c.IdCiclo == ciclo.IdCiclo);
                if (tracked != null)
                {
                    tracked.Estado = SuscripcionEstados.CicloPagado;
                    _ctx.SuscripcionCiclo.Update(tracked);
                }
            }

            await RegistrarEventoAsync(idEmpresa, "MARCAR_PAGO_MANUAL",
                "Pago marcado manualmente. Servicio activado.", idCiclo: ciclo?.IdCiclo);
            await _ctx.SaveChangesAsync();
        }

        private async Task CerrarPagosPendientesAsync(
            int idEmpresa, int? exceptoIdPago, string motivo, string usuarioValida)
        {
            var q = _ctx.PagosEmpresa.AsTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.Estado == "PENDIENTE");
            if (exceptoIdPago.HasValue)
                q = q.Where(p => p.Id != exceptoIdPago.Value);

            var pendientes = await q.ToListAsync();
            foreach (var p in pendientes)
            {
                p.Estado = "DESCARTADO";
                p.Observacion = motivo;
                p.FechaValidacion = DateTime.Now;
                p.UsuarioValida = usuarioValida;
            }
        }

        public async Task OnPagoRechazadoAsync(int idEmpresa, int idPago, string? observacion)
        {
            var emp = await _ctx.Empresas.AsTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (emp == null || emp.EsEmpresaSistema) return;

            var dia = ObtenerDiaCobro(idEmpresa);
            emp.EstadoServicio = (dia == 30 || dia <= 3)
                ? SuscripcionEstados.PendientePago
                : SuscripcionEstados.Suspendida;
            _ctx.Empresas.Update(emp);

            await RegistrarEventoAsync(idEmpresa, "PAGO_RECHAZADO",
                $"Pago #{idPago} rechazado. {observacion}");
            await _ctx.SaveChangesAsync();
        }

        public async Task OnPlanCambiadoAsync(int idEmpresa, int idPlanAnterior, int idPlanNuevo, int? idUsuario)
        {
            await RegistrarEventoAsync(idEmpresa, "CAMBIO_PLAN",
                $"Plan cambiado de {idPlanAnterior} a {idPlanNuevo}. Se cobrará en el próximo ciclo (día 30).",
                idUsuario: idUsuario);
        }

        public async Task<List<SuscripcionCicloDto>> ListarCiclosAsync(int? idEmpresa = null)
        {
            var q = from c in _ctx.SuscripcionCiclo.AsNoTracking()
                    join e in _ctx.Empresas.AsNoTracking() on c.IdEmpresa equals e.IdEmpresa
                    select new { c, e };

            if (idEmpresa.HasValue)
                q = q.Where(x => x.c.IdEmpresa == idEmpresa.Value);

            var rows = await q.OrderByDescending(x => x.c.Anio).ThenByDescending(x => x.c.Mes).Take(200).ToListAsync();
            return rows.Select(x => new SuscripcionCicloDto
            {
                IdCiclo = x.c.IdCiclo,
                IdEmpresa = x.c.IdEmpresa,
                NombreEmpresa = x.e.NombreComercial,
                Anio = x.c.Anio,
                Mes = x.c.Mes,
                FechaGeneracion = x.c.FechaGeneracion,
                Monto = x.c.Monto,
                IdPlan = x.c.IdPlan,
                Estado = x.c.Estado
            }).ToList();
        }

        public async Task<List<SuscripcionEventoDto>> ListarEventosAsync(int idEmpresa, int top = 100)
        {
            return await _ctx.SuscripcionEvento.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .OrderByDescending(e => e.Fecha)
                .Take(top)
                .Select(e => new SuscripcionEventoDto
                {
                    IdEvento = e.IdEvento,
                    IdEmpresa = e.IdEmpresa,
                    IdCiclo = e.IdCiclo,
                    Tipo = e.Tipo,
                    Detalle = e.Detalle,
                    Canal = e.Canal,
                    IdUsuario = e.IdUsuario,
                    Fecha = e.Fecha
                }).ToListAsync();
        }

        public async Task<SuscripcionResumenCobrosDto> ObtenerResumenAsync()
        {
            var empresas = await _ctx.Empresas.AsNoTracking()
                .Where(e => !e.EsEmpresaSistema)
                .ToListAsync();

            var resumen = new SuscripcionResumenCobrosDto
            {
                Activas = empresas.Count(e => NormalizarEstado(e.EstadoServicio) == SuscripcionEstados.Activa),
                PendientePago = empresas.Count(e => NormalizarEstado(e.EstadoServicio) == SuscripcionEstados.PendientePago),
                PagoReportado = empresas.Count(e => NormalizarEstado(e.EstadoServicio) == SuscripcionEstados.PagoReportado),
                Suspendidas = empresas.Count(e => NormalizarEstado(e.EstadoServicio) == SuscripcionEstados.Suspendida),
                Canceladas = empresas.Count(e => NormalizarEstado(e.EstadoServicio) == SuscripcionEstados.Cancelada),
                PagosPendientesValidacion = await _ctx.PagosEmpresa.CountAsync(p => p.Estado == "PENDIENTE"),
                CiclosAbiertos = await ListarCiclosAsync()
            };
            resumen.CiclosAbiertos = resumen.CiclosAbiertos
                .Where(c => c.Estado == SuscripcionEstados.CicloAbierto || c.Estado == SuscripcionEstados.CicloVencido)
                .Take(50)
                .ToList();
            return resumen;
        }

        public async Task<List<SuscripcionEmpresaCobroDto>> ListarEmpresasCobroAsync()
        {
            var rows = await _ctx.Empresas.AsNoTracking()
                .Where(e => !e.EsEmpresaSistema && e.Estado)
                .OrderBy(e => e.NombreComercial)
                .Select(e => new SuscripcionEmpresaCobroDto
                {
                    IdEmpresa = e.IdEmpresa,
                    NombreComercial = e.NombreComercial ?? "",
                    EstadoServicio = e.EstadoServicio ?? SuscripcionEstados.Activa,
                    PagadoServicio = e.PagadoServicio,
                    IdPlan = e.IdPlan,
                    NombrePlan = "Plan " + (e.NombreComercial ?? ("Empresa " + e.IdEmpresa)),
                    MontoServicio = e.MontoServicio,
                    CargoAdicional = e.CargoAdicional,
                    LimiteFacturacion = e.LimiteFacturacion,
                    CargoReconexionDop = e.CargoReconexionDop,
                    ReconexionPendiente = e.ReconexionPendiente
                })
                .ToListAsync();

            return rows;
        }

        public async Task<List<SuscripcionCuentaCobroDto>> ListarCuentasCobroAsync(bool soloActivas = true)
        {
            var q = _ctx.SuscripcionCuentaCobro.AsNoTracking().AsQueryable();
            if (soloActivas)
                q = q.Where(c => c.Activo);

            return await q
                .OrderBy(c => c.Orden)
                .ThenBy(c => c.Banco)
                .Select(c => new SuscripcionCuentaCobroDto
                {
                    Id = c.Id,
                    Banco = c.Banco,
                    NumeroCuenta = c.NumeroCuenta,
                    Titular = c.Titular,
                    Cedula = c.Cedula,
                    Correo = c.Correo,
                    CuentaEstandar = c.CuentaEstandar,
                    Activo = c.Activo,
                    Orden = c.Orden
                })
                .ToListAsync();
        }

        public async Task<SuscripcionCuentaCobroDto> GuardarCuentaCobroAsync(GuardarSuscripcionCuentaCobroDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Banco)
                || string.IsNullOrWhiteSpace(dto.NumeroCuenta)
                || string.IsNullOrWhiteSpace(dto.Titular)
                || string.IsNullOrWhiteSpace(dto.Cedula))
                throw new Exception("Banco, número de cuenta, titular y cédula son obligatorios.");

            SuscripcionCuentaCobro entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _ctx.SuscripcionCuentaCobro.AsTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.Id.Value)
                    ?? throw new Exception("Cuenta de cobro no encontrada.");
                entity.FechaModificacion = DateTime.Now;
            }
            else
            {
                entity = new SuscripcionCuentaCobro { FechaCreacion = DateTime.Now };
                _ctx.SuscripcionCuentaCobro.Add(entity);
            }

            entity.Banco = dto.Banco.Trim();
            entity.NumeroCuenta = dto.NumeroCuenta.Trim();
            entity.Titular = dto.Titular.Trim();
            entity.Cedula = dto.Cedula.Trim();
            entity.Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim();
            entity.CuentaEstandar = string.IsNullOrWhiteSpace(dto.CuentaEstandar) ? null : dto.CuentaEstandar.Trim();
            entity.Activo = dto.Activo;
            entity.Orden = dto.Orden;

            await _ctx.SaveChangesAsync();

            return new SuscripcionCuentaCobroDto
            {
                Id = entity.Id,
                Banco = entity.Banco,
                NumeroCuenta = entity.NumeroCuenta,
                Titular = entity.Titular,
                Cedula = entity.Cedula,
                Correo = entity.Correo,
                CuentaEstandar = entity.CuentaEstandar,
                Activo = entity.Activo,
                Orden = entity.Orden
            };
        }

        public async Task EliminarCuentaCobroAsync(int id)
        {
            var entity = await _ctx.SuscripcionCuentaCobro.AsTracking()
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new Exception("Cuenta de cobro no encontrada.");
            _ctx.SuscripcionCuentaCobro.Remove(entity);
            await _ctx.SaveChangesAsync();
        }

        private static DateTime ProximoDia30(DateTime desde)
        {
            if (desde.Day < 30)
                return new DateTime(desde.Year, desde.Month, Math.Min(30, DateTime.DaysInMonth(desde.Year, desde.Month)));
            var next = desde.AddMonths(1);
            return new DateTime(next.Year, next.Month, Math.Min(30, DateTime.DaysInMonth(next.Year, next.Month)));
        }

        /// <summary>Demo / prueba: sin monto de servicio y con fecha de terminación vigente.</summary>
        private bool EsDemoVigente(Empresas empresa)
        {
            if (empresa == null) return false;
            if (empresa.FechaTerminacion.Date < DateTime.Now.Date) return false;
            return empresa.MontoServicio <= 0m;
        }

        private Task<bool> EsPlanDemoAsync(Empresas emp)
            => Task.FromResult(emp != null && emp.MontoServicio <= 0m);

        public static string NormalizarEstado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return SuscripcionEstados.Activa;
            return estado.ToUpperInvariant() switch
            {
                "ACTIVO" or "ACTIVA" => SuscripcionEstados.Activa,
                "VENCIDO" or "PENDIENTE" or "PENDIENTE_PAGO" => SuscripcionEstados.PendientePago,
                "PAGO_REPORTADO" => SuscripcionEstados.PagoReportado,
                "BLOQUEADO" or "SUSPENDIDA" => SuscripcionEstados.Suspendida,
                "CANCELADA" => SuscripcionEstados.Cancelada,
                _ => estado.ToUpperInvariant()
            };
        }
    }
}
