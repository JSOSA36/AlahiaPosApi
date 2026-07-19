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

            var estado = NormalizarEstado(empresa.EstadoServicio);
            return estado == SuscripcionEstados.Activa
                || estado == SuscripcionEstados.PendientePago;
        }

        public bool EstaBloqueada(Empresas empresa)
        {
            if (empresa == null) return true;
            if (empresa.EsEmpresaSistema) return false;

            var estado = NormalizarEstado(empresa.EstadoServicio);
            return estado == SuscripcionEstados.Suspendida
                || estado == SuscripcionEstados.PagoReportado
                || estado == SuscripcionEstados.Cancelada;
        }

        public AlertaPagoDto? ObtenerAlertaPago(Empresas empresa)
        {
            if (empresa == null || empresa.EsEmpresaSistema) return null;

            var dia = ObtenerDiaCobro(empresa.IdEmpresa);
            var estado = NormalizarEstado(empresa.EstadoServicio);
            var enGracia = dia == 30 || dia == 1 || dia == 2 || dia == 3;
            var pendienteCobro = !empresa.PagadoServicio
                || estado == SuscripcionEstados.PendientePago;

            // Bloqueo / validación (pantalla dedicada; no es solo warning)
            if (estado == SuscripcionEstados.Suspendida || estado == SuscripcionEstados.PagoReportado)
            {
                return new AlertaPagoDto
                {
                    Tipo = "critico",
                    Mensaje = estado == SuscripcionEstados.PagoReportado
                        ? "Su pago está en validación. El acceso se restaurará cuando MacroBits lo apruebe."
                        : "Su servicio se encuentra suspendido por falta de pago."
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

            // Al día: sin aviso
            if (empresa.PagadoServicio && estado == SuscripcionEstados.Activa)
                return null;

            // Warnings informativos en ventana de pago (día 30 → 3)
            const string msgPendiente =
                "Su suscripción tiene un pago pendiente. Puede continuar utilizando el sistema hasta el día 3. A partir de esa fecha el acceso será suspendido automáticamente si el pago no ha sido reportado.";

            if (pendienteCobro && enGracia)
            {
                var diasRestantes = DiasHastaLimitePago(dia);
                var tipo = dia == 3 ? "critico" : "advertencia";
                return new AlertaPagoDto
                {
                    Tipo = tipo,
                    Mensaje = msgPendiente,
                    DiaCobro = dia,
                    DiasRestantes = diasRestantes
                };
            }

            // Pendiente fuera de ventana (estado inconsistente o ciclo abierto)
            if (pendienteCobro || estado == SuscripcionEstados.PendientePago)
            {
                return new AlertaPagoDto
                {
                    Tipo = "advertencia",
                    Mensaje = msgPendiente,
                    DiaCobro = dia,
                    DiasRestantes = DiasHastaLimitePago(dia)
                };
            }

            return null;
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

            var hoy = DateTime.Now;
            var dia = ObtenerDiaCobro(emp.IdEmpresa);
            var tienePendiente = await TienePagoPendienteAsync(emp.IdEmpresa);
            var ciclo = await ObtenerOCrearCicloActualAsync(emp);

            // Día 30: nuevo ciclo / reset
            if (dia == 30)
            {
                if (emp.PagadoServicio)
                    emp.PagadoServicio = false;

                ciclo = await ObtenerOCrearCicloActualAsync(emp);
                if (ciclo != null && ciclo.Estado == SuscripcionEstados.CicloPagado && !emp.PagadoServicio)
                {
                    // nuevo mes: el ciclo del día 30 es del mes actual
                }
            }

            if (emp.PagadoServicio && !tienePendiente)
            {
                emp.EstadoServicio = SuscripcionEstados.Activa;
                emp.FechaProximoPago = ProximoDia30(hoy);
                if (ciclo != null && ciclo.Estado != SuscripcionEstados.CicloPagado)
                {
                    ciclo.Estado = SuscripcionEstados.CicloPagado;
                }
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

                if (enviarAvisos && (dia == 30 || dia == 2 || dia == 3))
                    await EnviarAvisoSiCorrespondeAsync(emp, ciclo, TipoAvisoPorDia(dia));

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

                if (enviarAvisos)
                    await EnviarAvisoSiCorrespondeAsync(emp, ciclo, TipoAvisoPorDia(dia));

                return;
            }

            // Día ≥ 4 → suspensión
            if (dia >= 4)
            {
                if (ciclo != null && ciclo.Estado == SuscripcionEstados.CicloAbierto)
                    ciclo.Estado = SuscripcionEstados.CicloVencido;

                if (emp.EstadoServicio != SuscripcionEstados.Suspendida)
                {
                    emp.EstadoServicio = SuscripcionEstados.Suspendida;
                    await RegistrarEventoAsync(emp.IdEmpresa, "SUSPENSION",
                        "Suspensión automática por falta de pago validado", idCiclo: ciclo?.IdCiclo);

                    if (enviarAvisos)
                        await EnviarAvisoSiCorrespondeAsync(emp, ciclo, SuscripcionEstados.AvisoSuspension);
                }
            }
        }

        private static string TipoAvisoPorDia(int dia) => dia switch
        {
            30 => SuscripcionEstados.AvisoDia30,
            2 => SuscripcionEstados.AvisoDia2,
            3 => SuscripcionEstados.AvisoDia3,
            _ => SuscripcionEstados.AvisoDia30
        };

        private async Task EnviarAvisoSiCorrespondeAsync(Empresas emp, SuscripcionCiclo? ciclo, string tipoAviso)
        {
            if (ciclo == null) return;

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

            await RegistrarEventoAsync(emp.IdEmpresa, "AVISO_ENVIADO", mensaje, CanalCentro, idCiclo: ciclo.IdCiclo);

            // Canales legacy (WhatsApp stub / log) sin email directo: el Centro ya decide EMAIL.
            foreach (var canal in _canales.Where(c =>
                !string.Equals(c.Canal, SuscripcionEstados.CanalEmail, StringComparison.OrdinalIgnoreCase)))
            {
                var yaEnviado = await _ctx.SuscripcionAvisoLog.AnyAsync(a =>
                    a.IdEmpresa == emp.IdEmpresa
                    && a.IdCiclo == ciclo.IdCiclo
                    && a.TipoAviso == tipoAviso
                    && a.Canal == canal.Canal);

                if (yaEnviado) continue;

                await canal.EnviarAsync(new NotificacionSuscripcionMensaje
                {
                    IdEmpresa = emp.IdEmpresa,
                    IdCiclo = ciclo.IdCiclo,
                    TipoAviso = tipoAviso,
                    Titulo = titulo,
                    Mensaje = mensaje,
                    CorreoDestino = emp.CorreElectronico,
                    NombreEmpresa = emp.NombreComercial
                });

                _ctx.SuscripcionAvisoLog.Add(new SuscripcionAvisoLog
                {
                    IdEmpresa = emp.IdEmpresa,
                    IdCiclo = ciclo.IdCiclo,
                    TipoAviso = tipoAviso,
                    Canal = canal.Canal,
                    FechaEnvio = DateTime.Now
                });
            }
        }

        private static (string titulo, string mensaje) MensajeAviso(string tipo) => tipo switch
        {
            SuscripcionEstados.AvisoDia30 => (
                "Renovación de suscripción Alahia ERP",
                "Su suscripción vence próximamente. Favor realizar su pago antes de la fecha límite (día 3)."
            ),
            SuscripcionEstados.AvisoDia2 => (
                "Recordatorio de pago — Alahia ERP",
                "Segundo recordatorio: su pago aún no ha sido confirmado. Fecha límite: día 3."
            ),
            SuscripcionEstados.AvisoDia3 => (
                "Último aviso de pago — Alahia ERP",
                "Último recordatorio: si no se recibe y valida el pago, el servicio será suspendido a partir del día 4."
            ),
            SuscripcionEstados.AvisoSuspension => (
                "Servicio suspendido — Alahia ERP",
                "Su servicio se encuentra suspendido por falta de pago. Inicie sesión para reportar su pago."
            ),
            _ => ("Aviso de suscripción", "Tiene un pendiente relacionado con su suscripción.")
        };

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
            var fecha = (fechaReferencia ?? DateTime.Now).Date;
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            var calc = new SuscripcionCalculoFacturaDto
            {
                IdEmpresa = idEmpresa,
                IdPlan = empresa.IdPlan
            };

            if (empresa.IdPlan.HasValue)
            {
                var plan = await _ctx.PlanesCloud.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPlan == empresa.IdPlan.Value);
                var precioCatalogo = plan?.PrecioUSD ?? 0;
                calc.MontoPlanCatalogo = precioCatalogo;
                calc.NombrePlan = plan?.Nombre ?? "Plan";
                calc.PrecioPlanEspecialUsd = empresa.PrecioPlanEspecialUsd;
                calc.UsaPrecioPlanEspecial = empresa.PrecioPlanEspecialUsd.HasValue
                    && empresa.PrecioPlanEspecialUsd.Value >= 0;
                calc.MontoPlan = calc.UsaPrecioPlanEspecial
                    ? empresa.PrecioPlanEspecialUsd!.Value
                    : precioCatalogo;

                var nombreLinea = calc.UsaPrecioPlanEspecial
                    ? $"{calc.NombrePlan} (precio especial)"
                    : calc.NombrePlan!;

                calc.Lineas.Add(new SuscripcionLineaFacturaDto
                {
                    TipoLinea = TipoCargoRecurrente.Plan,
                    Codigo = $"PLAN_{empresa.IdPlan}",
                    Nombre = nombreLinea,
                    Monto = calc.MontoPlan
                });
            }

            var cargos = await _ctx.EmpresaCargoRecurrente.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .Where(c => c.FechaInicio.Date <= fecha)
                .Where(c => c.FechaFin == null || c.FechaFin.Value.Date >= fecha)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            foreach (var c in cargos)
            {
                calc.Lineas.Add(new SuscripcionLineaFacturaDto
                {
                    TipoLinea = c.TipoCargo,
                    IdCargo = c.Id,
                    IdModulo = c.IdModulo,
                    Codigo = c.Codigo,
                    Nombre = c.Nombre,
                    Monto = c.MontoMensual
                });
                calc.MontoCargos += c.MontoMensual;
            }

            calc.Total = calc.MontoPlan + calc.MontoCargos;

            // Cobro en USD; equivalente DOP a tasa fija (config Suscripcion:TasaUsdDop)
            var tasa = ObtenerTasaUsdDop();
            calc.TasaUsdDop = tasa;
            calc.MontoPlanDop = RedondearDop(calc.MontoPlan * tasa);
            calc.MontoCargosDop = RedondearDop(calc.MontoCargos * tasa);
            calc.TotalDop = RedondearDop(calc.Total * tasa);
            foreach (var linea in calc.Lineas)
                linea.MontoDop = RedondearDop(linea.Monto * tasa);

            return calc;
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

        public async Task<SuscripcionCalculoFacturaDto> ActualizarPrecioPlanEspecialAsync(ActualizarPrecioPlanEspecialDto dto)
        {
            var emp = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            if (emp.EsEmpresaSistema)
                throw new Exception("No aplica a la empresa de sistema.");

            var anterior = emp.PrecioPlanEspecialUsd;
            if (dto.PrecioPlanEspecialUsd.HasValue)
            {
                if (dto.PrecioPlanEspecialUsd.Value < 0)
                    throw new Exception("El precio especial no puede ser negativo.");
                emp.PrecioPlanEspecialUsd = dto.PrecioPlanEspecialUsd.Value;
            }
            else
            {
                emp.PrecioPlanEspecialUsd = null;
            }

            await _ctx.SaveChangesAsync();

            await RegistrarEventoAsync(dto.IdEmpresa, "PRECIO_PLAN_ESPECIAL",
                emp.PrecioPlanEspecialUsd.HasValue
                    ? $"Precio especial del plan: {(anterior?.ToString("0.00") ?? "catálogo")} → {emp.PrecioPlanEspecialUsd:0.00} USD"
                    : $"Precio especial removido (antes {(anterior?.ToString("0.00") ?? "n/a")}); vuelve a catálogo",
                idUsuario: dto.IdUsuario);

            await RecalcularCicloAbiertoAsync(dto.IdEmpresa);
            return await CalcularFacturaAsync(dto.IdEmpresa);
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

            emp.PagadoServicio = true;
            emp.EstadoServicio = SuscripcionEstados.Activa;
            emp.FechaUltimoPago = DateTime.Now;
            emp.FechaProximoPago = ProximoDia30(DateTime.Now);
            _ctx.Empresas.Update(emp);

            if (idCiclo.HasValue)
            {
                var ciclo = await _ctx.SuscripcionCiclo.AsTracking()
                    .FirstOrDefaultAsync(c => c.IdCiclo == idCiclo.Value);
                if (ciclo != null)
                {
                    ciclo.Estado = SuscripcionEstados.CicloPagado;
                    _ctx.SuscripcionCiclo.Update(ciclo);
                }
            }
            else
            {
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
            }

            await RegistrarEventoAsync(idEmpresa, "REACTIVACION",
                $"Pago #{idPago} aprobado por {usuarioValida}. Servicio reactivado.",
                idCiclo: idCiclo);
            await _ctx.SaveChangesAsync();
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
            var rows = await (
                from e in _ctx.Empresas.AsNoTracking()
                join p in _ctx.PlanesCloud.AsNoTracking() on e.IdPlan equals p.IdPlan into pj
                from p in pj.DefaultIfEmpty()
                where !e.EsEmpresaSistema && e.Estado
                orderby e.NombreComercial
                select new SuscripcionEmpresaCobroDto
                {
                    IdEmpresa = e.IdEmpresa,
                    NombreComercial = e.NombreComercial,
                    EstadoServicio = e.EstadoServicio ?? SuscripcionEstados.Activa,
                    PagadoServicio = e.PagadoServicio,
                    IdPlan = e.IdPlan,
                    NombrePlan = p != null ? p.Nombre : null,
                    PrecioPlanCatalogo = p != null ? p.PrecioUSD : null,
                    PrecioPlanEspecialUsd = e.PrecioPlanEspecialUsd
                }
            ).ToListAsync();

            return rows;
        }

        private static DateTime ProximoDia30(DateTime desde)
        {
            if (desde.Day < 30)
                return new DateTime(desde.Year, desde.Month, Math.Min(30, DateTime.DaysInMonth(desde.Year, desde.Month)));
            var next = desde.AddMonths(1);
            return new DateTime(next.Year, next.Month, Math.Min(30, DateTime.DaysInMonth(next.Year, next.Month)));
        }

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
