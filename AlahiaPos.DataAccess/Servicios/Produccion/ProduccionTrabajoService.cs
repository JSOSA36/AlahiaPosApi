using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Produccion
{
    public class ProduccionTrabajoService : IProduccionTrabajoService
    {
        private readonly AlahiaPosContext _ctx;
        private readonly IProduccionConfiguracionService _config;
        private readonly IProduccionFlujoService _flujos;
        private readonly IEmpresaModulos _empresaModulos;
        private readonly IModulo _modulos;
        private readonly IProduccionRealtime _realtime;

        public ProduccionTrabajoService(
            AlahiaPosContext ctx,
            IProduccionConfiguracionService config,
            IProduccionFlujoService flujos,
            IEmpresaModulos empresaModulos,
            IModulo modulos,
            IProduccionRealtime realtime)
        {
            _ctx = ctx;
            _config = config;
            _flujos = flujos;
            _empresaModulos = empresaModulos;
            _modulos = modulos;
            _realtime = realtime;
        }

        public async Task<ProduccionTrabajoDto?> CrearDesdeEventoAsync(ProduccionTrabajoSolicitadoEvent solicitud)
        {
            if (solicitud == null || solicitud.IdEmpresa <= 0)
                return null;

            if (!await MotorDisponibleAsync(solicitud.IdEmpresa))
                return null;

            if (string.IsNullOrWhiteSpace(solicitud.IdempotencyKey)
                || string.IsNullOrWhiteSpace(solicitud.TipoTrabajo)
                || string.IsNullOrWhiteSpace(solicitud.OrigenTipo)
                || solicitud.OrigenId <= 0)
                return null;

            var existente = await _ctx.ProduccionTrabajo
                .AsNoTracking()
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t =>
                    t.IdEmpresa == solicitud.IdEmpresa &&
                    t.IdempotencyKey == solicitud.IdempotencyKey);

            if (existente != null)
                return await MapTrabajoDtoAsync(existente);

            var porOrigen = await _ctx.ProduccionTrabajo
                .AsNoTracking()
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t =>
                    t.IdEmpresa == solicitud.IdEmpresa &&
                    t.OrigenTipo == solicitud.OrigenTipo &&
                    t.OrigenId == solicitud.OrigenId);

            if (porOrigen != null)
                return await MapTrabajoDtoAsync(porOrigen);

            var flujo = await ResolverFlujoEntityAsync(solicitud.IdEmpresa, solicitud.TipoTrabajo);
            if (flujo == null)
                return null;

            var estadoInicial = flujo.Estados
                .Where(e => e.EsInicial)
                .OrderBy(e => e.Orden)
                .FirstOrDefault();
            if (estadoInicial == null)
                return null;

            var estacionId = await ResolverEstacionIdAsync(solicitud.IdEmpresa, null);
            var ahora = DateTime.Now;
            var prioridad = NormalizarPrioridad(solicitud.Prioridad);
            var slaModo = NormalizarSlaModo(flujo.SlaModoInicio);

            var trabajo = new ProduccionTrabajo
            {
                IdEmpresa = solicitud.IdEmpresa,
                TipoTrabajoCodigo = solicitud.TipoTrabajo.Trim(),
                IdFlujo = flujo.IdFlujo,
                CodigoEstadoActual = estadoInicial.Codigo,
                OrigenModulo = Truncate(solicitud.OrigenModulo, 40) ?? ProduccionConstantes.OrigenModuloPos,
                OrigenTipo = Truncate(solicitud.OrigenTipo, 80)!,
                OrigenId = solicitud.OrigenId,
                IdempotencyKey = Truncate(solicitud.IdempotencyKey, 120)!,
                NumeroVisible = Truncate(string.IsNullOrWhiteSpace(solicitud.NumeroVisible)
                    ? solicitud.OrigenId.ToString()
                    : solicitud.NumeroVisible, 60)!,
                NombreVisible = Truncate(string.IsNullOrWhiteSpace(solicitud.NombreVisible)
                    ? "Sin nombre"
                    : solicitud.NombreVisible, 200)!,
                Referencia = Truncate(solicitud.Referencia, 200),
                EtiquetaContexto = Truncate(solicitud.EtiquetaContexto, 40),
                Observacion = Truncate(solicitud.Observacion, 1000),
                IdUsuarioSolicita = solicitud.IdUsuarioSolicita ?? (solicitud.IdUsuario > 0 ? solicitud.IdUsuario : null),
                Prioridad = prioridad,
                SlaObjetivoSegundosSnapshot = flujo.SlaObjetivoSegundos,
                SlaAdvertenciaSegundosSnapshot = flujo.SlaAdvertenciaSegundos,
                SlaModoInicioSnapshot = slaModo,
                FechaCreacion = ahora,
                FechaLimiteObjetivo = slaModo == ProduccionConstantes.SlaModoCreacion
                    ? ahora.AddSeconds(flujo.SlaObjetivoSegundos)
                    : null,
                ActivoEnTablero = true,
                PlantillaCodigo = Truncate(solicitud.PlantillaCodigo, 40)
            };

            var orden = 0;
            foreach (var item in solicitud.Items ?? new List<ProduccionTrabajoItemSolicitudDto>())
            {
                if (string.IsNullOrWhiteSpace(item.NombreItem))
                    continue;

                var itemEstacionId = await ResolverEstacionIdAsync(solicitud.IdEmpresa, item.EstacionCodigo)
                    ?? estacionId;

                trabajo.Items.Add(new ProduccionTrabajoItem
                {
                    OrigenDetalleId = item.OrigenDetalleId,
                    IdEstacion = itemEstacionId,
                    CodigoItem = Truncate(item.CodigoItem, 40),
                    NombreItem = Truncate(item.NombreItem, 200)!,
                    Cantidad = item.Cantidad <= 0 ? 1 : item.Cantidad,
                    Observacion = Truncate(item.Observacion, 500),
                    VariacionesTexto = Truncate(item.VariacionesTexto, 1000),
                    CodigoEstado = estadoInicial.Codigo,
                    Orden = orden++
                });
            }

            _ctx.ProduccionTrabajo.Add(trabajo);

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var race = await _ctx.ProduccionTrabajo
                    .AsNoTracking()
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t =>
                        t.IdEmpresa == solicitud.IdEmpresa &&
                        t.IdempotencyKey == solicitud.IdempotencyKey);
                if (race != null)
                    return await MapTrabajoDtoAsync(race);
                throw;
            }

            _ctx.ProduccionHistorial.Add(new ProduccionHistorial
            {
                IdTrabajo = trabajo.IdTrabajo,
                CodigoEstadoAnterior = null,
                CodigoEstadoNuevo = estadoInicial.Codigo,
                IdUsuario = trabajo.IdUsuarioSolicita,
                Fecha = ahora,
                Origen = ProduccionConstantes.HistorialOrigenEvento,
                Motivo = "Trabajo creado"
            });
            await _ctx.SaveChangesAsync();

            return await ObtenerAsync(solicitud.IdEmpresa, trabajo.IdTrabajo);
        }

        public async Task<ProduccionTrabajoDto?> ActualizarDesdeEventoAsync(ProduccionTrabajoActualizadoEvent solicitud)
        {
            if (solicitud == null || solicitud.IdEmpresa <= 0)
                return null;

            if (!await MotorDisponibleAsync(solicitud.IdEmpresa))
                return null;

            if (string.IsNullOrWhiteSpace(solicitud.OrigenTipo) || solicitud.OrigenId <= 0)
                return null;

            var itemsNuevos = (solicitud.Items ?? new List<ProduccionTrabajoItemSolicitudDto>())
                .Where(i => !string.IsNullOrWhiteSpace(i.NombreItem))
                .ToList();
            if (itemsNuevos.Count == 0)
                return null;

            var origenBusqueda = solicitud.OrigenIdAnterior > 0
                ? solicitud.OrigenIdAnterior
                : solicitud.OrigenId;

            var trabajo = await _ctx.ProduccionTrabajo
                .AsTracking()
                .Include(t => t.Items)
                .Where(t =>
                    t.IdEmpresa == solicitud.IdEmpresa &&
                    t.OrigenTipo == solicitud.OrigenTipo &&
                    t.OrigenId == origenBusqueda &&
                    t.ActivoEnTablero)
                .OrderByDescending(t => t.IdTrabajo)
                .FirstOrDefaultAsync();

            if (trabajo == null)
            {
                // Sin trabajo activo previo → crear como solicitud nueva
                return await CrearDesdeEventoAsync(new ProduccionTrabajoSolicitadoEvent
                {
                    IdEmpresa = solicitud.IdEmpresa,
                    IdUsuario = solicitud.IdUsuario,
                    Fecha = solicitud.Fecha,
                    ReferenciaId = solicitud.ReferenciaId,
                    ReferenciaTipo = solicitud.ReferenciaTipo,
                    TipoTrabajo = solicitud.TipoTrabajo,
                    OrigenModulo = solicitud.OrigenModulo,
                    OrigenTipo = solicitud.OrigenTipo,
                    OrigenId = solicitud.OrigenId,
                    IdempotencyKey = solicitud.IdempotencyKey,
                    NumeroVisible = solicitud.NumeroVisible,
                    NombreVisible = solicitud.NombreVisible,
                    Referencia = solicitud.Referencia,
                    EtiquetaContexto = solicitud.EtiquetaContexto,
                    Observacion = solicitud.Observacion,
                    IdUsuarioSolicita = solicitud.IdUsuarioSolicita,
                    Prioridad = solicitud.Prioridad,
                    PlantillaCodigo = solicitud.PlantillaCodigo,
                    Items = itemsNuevos
                });
            }

            // Evitar colisión si ya existe otro trabajo con el nuevo OrigenId
            if (solicitud.OrigenId != trabajo.OrigenId)
            {
                var existenteNuevo = await _ctx.ProduccionTrabajo
                    .AsTracking()
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t =>
                        t.IdEmpresa == solicitud.IdEmpresa &&
                        t.OrigenTipo == solicitud.OrigenTipo &&
                        t.OrigenId == solicitud.OrigenId &&
                        t.IdTrabajo != trabajo.IdTrabajo &&
                        t.ActivoEnTablero);

                if (existenteNuevo != null)
                {
                    // Quedarse con el trabajo del nuevo origen; retirar el anterior
                    trabajo.ActivoEnTablero = false;
                    trabajo.CodigoEstadoActual = "CANCELADA";
                    trabajo.FechaCancelacion = DateTime.Now;
                    trabajo.FechaCompletado = DateTime.Now;
                    trabajo.MotivoCancelacion = "Reemplazado por edición en origen";
                    _ctx.ProduccionHistorial.Add(new ProduccionHistorial
                    {
                        IdTrabajo = trabajo.IdTrabajo,
                        CodigoEstadoAnterior = trabajo.CodigoEstadoActual,
                        CodigoEstadoNuevo = "CANCELADA",
                        IdUsuario = solicitud.IdUsuarioSolicita ?? (solicitud.IdUsuario > 0 ? solicitud.IdUsuario : null),
                        Fecha = DateTime.Now,
                        Origen = ProduccionConstantes.HistorialOrigenEvento,
                        Motivo = "Duplicado retirado tras edición en origen"
                    });
                    await _ctx.SaveChangesAsync();
                    trabajo = existenteNuevo;
                }
            }

            var ahora = DateTime.Now;
            trabajo.OrigenId = solicitud.OrigenId;
            trabajo.OrigenModulo = Truncate(solicitud.OrigenModulo, 40) ?? trabajo.OrigenModulo;
            if (!string.IsNullOrWhiteSpace(solicitud.IdempotencyKey))
                trabajo.IdempotencyKey = Truncate(solicitud.IdempotencyKey, 120)!;
            trabajo.NumeroVisible = Truncate(string.IsNullOrWhiteSpace(solicitud.NumeroVisible)
                ? solicitud.OrigenId.ToString()
                : solicitud.NumeroVisible, 60)!;
            trabajo.NombreVisible = Truncate(string.IsNullOrWhiteSpace(solicitud.NombreVisible)
                ? trabajo.NombreVisible
                : solicitud.NombreVisible, 200)!;
            trabajo.Referencia = Truncate(solicitud.Referencia, 200);
            trabajo.EtiquetaContexto = Truncate(solicitud.EtiquetaContexto, 40);
            trabajo.Observacion = Truncate(solicitud.Observacion, 1000);
            if (solicitud.IdUsuarioSolicita.HasValue)
                trabajo.IdUsuarioSolicita = solicitud.IdUsuarioSolicita;
            if (!string.IsNullOrWhiteSpace(solicitud.Prioridad))
                trabajo.Prioridad = NormalizarPrioridad(solicitud.Prioridad);
            if (!string.IsNullOrWhiteSpace(solicitud.PlantillaCodigo))
                trabajo.PlantillaCodigo = Truncate(solicitud.PlantillaCodigo, 40);

            var estacionId = await ResolverEstacionIdAsync(solicitud.IdEmpresa, null);
            _ctx.ProduccionTrabajoItem.RemoveRange(trabajo.Items.ToList());
            trabajo.Items.Clear();

            var orden = 0;
            foreach (var item in itemsNuevos)
            {
                var itemEstacionId = await ResolverEstacionIdAsync(solicitud.IdEmpresa, item.EstacionCodigo)
                    ?? estacionId;
                trabajo.Items.Add(new ProduccionTrabajoItem
                {
                    OrigenDetalleId = item.OrigenDetalleId,
                    IdEstacion = itemEstacionId,
                    CodigoItem = Truncate(item.CodigoItem, 40),
                    NombreItem = Truncate(item.NombreItem, 200)!,
                    Cantidad = item.Cantidad <= 0 ? 1 : item.Cantidad,
                    Observacion = Truncate(item.Observacion, 500),
                    VariacionesTexto = Truncate(item.VariacionesTexto, 1000),
                    CodigoEstado = trabajo.CodigoEstadoActual,
                    Orden = orden++,
                    FechaInicio = trabajo.FechaInicio
                });
            }

            _ctx.ProduccionHistorial.Add(new ProduccionHistorial
            {
                IdTrabajo = trabajo.IdTrabajo,
                CodigoEstadoAnterior = trabajo.CodigoEstadoActual,
                CodigoEstadoNuevo = trabajo.CodigoEstadoActual,
                IdUsuario = solicitud.IdUsuarioSolicita ?? (solicitud.IdUsuario > 0 ? solicitud.IdUsuario : null),
                Fecha = ahora,
                Origen = ProduccionConstantes.HistorialOrigenEvento,
                Motivo = origenBusqueda != solicitud.OrigenId
                    ? $"Actualizado desde origen ({origenBusqueda} → {solicitud.OrigenId})"
                    : "Actualizado desde origen"
            });

            await _ctx.SaveChangesAsync();
            return await ObtenerAsync(solicitud.IdEmpresa, trabajo.IdTrabajo);
        }

        public async Task<List<ProduccionTrabajoDto>> ListarActivosAsync(int idEmpresa, string? tipoTrabajoCodigo = null)
        {
            var q = _ctx.ProduccionTrabajo
                .AsNoTracking()
                .Include(t => t.Items)
                .Where(t => t.IdEmpresa == idEmpresa && t.ActivoEnTablero);

            if (!string.IsNullOrWhiteSpace(tipoTrabajoCodigo))
                q = q.Where(t => t.TipoTrabajoCodigo == tipoTrabajoCodigo);

            var list = await q
                .OrderByDescending(t => t.Prioridad == ProduccionConstantes.PrioridadUrgente)
                .ThenByDescending(t => t.Prioridad == ProduccionConstantes.PrioridadAlta)
                .ThenBy(t => t.FechaCreacion)
                .ToListAsync();

            var result = new List<ProduccionTrabajoDto>();
            foreach (var t in list)
                result.Add(await MapTrabajoDtoAsync(t));
            return result;
        }

        public async Task<List<ProduccionEstadoOrigenDto>> ObtenerEstadosPorOrigenAsync(
            int idEmpresa, string origenTipo, IEnumerable<int> origenIds)
        {
            var ids = (origenIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            if (idEmpresa <= 0 || ids.Count == 0 || string.IsNullOrWhiteSpace(origenTipo))
                return new List<ProduccionEstadoOrigenDto>();

            var trabajos = await _ctx.ProduccionTrabajo
                .AsNoTracking()
                .Where(t =>
                    t.IdEmpresa == idEmpresa &&
                    t.OrigenTipo == origenTipo &&
                    ids.Contains(t.OrigenId))
                .Select(t => new
                {
                    t.IdTrabajo,
                    t.OrigenId,
                    t.CodigoEstadoActual,
                    t.ActivoEnTablero,
                    t.IdFlujo
                })
                .ToListAsync();

            // Último trabajo por origen (mayor IdTrabajo)
            var latest = trabajos
                .GroupBy(t => t.OrigenId)
                .Select(g => g.OrderByDescending(x => x.IdTrabajo).First())
                .ToList();

            if (latest.Count == 0)
                return new List<ProduccionEstadoOrigenDto>();

            var flujoIds = latest.Select(x => x.IdFlujo).Distinct().ToList();
            var estados = await _ctx.ProduccionFlujoEstado
                .AsNoTracking()
                .Where(e => flujoIds.Contains(e.IdFlujo))
                .ToListAsync();

            return latest.Select(t => new ProduccionEstadoOrigenDto
            {
                OrigenId = t.OrigenId,
                IdTrabajo = t.IdTrabajo,
                CodigoEstado = t.CodigoEstadoActual,
                NombreEstado = estados.FirstOrDefault(e => e.IdFlujo == t.IdFlujo && e.Codigo == t.CodigoEstadoActual)?.NombreVisible,
                ActivoEnTablero = t.ActivoEnTablero
            }).ToList();
        }

        public async Task<ProduccionTrabajoDto?> ObtenerAsync(int idEmpresa, int idTrabajo)
        {
            var t = await _ctx.ProduccionTrabajo
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.IdTrabajo == idTrabajo);
            return t == null ? null : await MapTrabajoDtoAsync(t);
        }

        public async Task<ProduccionTrabajoDto> TransicionarAsync(int idEmpresa, int idTrabajo, ProduccionTransicionRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Solicitud inválida.");

            var trabajo = await CargarParaMutacionAsync(idEmpresa, idTrabajo);
            ValidarConcurrencia(trabajo, request.CodigoEstadoEsperado, request.RowVersion);

            var flujo = await _ctx.ProduccionFlujo
                .AsNoTracking()
                .Include(f => f.Estados)
                .Include(f => f.Transiciones)
                .FirstAsync(f => f.IdFlujo == trabajo.IdFlujo);

            var estadoActual = flujo.Estados.FirstOrDefault(e => e.Codigo == trabajo.CodigoEstadoActual);
            if (estadoActual?.EsTerminal == true)
                throw new InvalidOperationException("El trabajo ya está en un estado terminal.");

            var permitida = flujo.Transiciones.Any(t =>
                t.CodigoDesde == trabajo.CodigoEstadoActual &&
                t.CodigoHasta == request.CodigoEstadoNuevo);

            if (!permitida)
                throw new InvalidOperationException(
                    $"Transición no permitida: {trabajo.CodigoEstadoActual} → {request.CodigoEstadoNuevo}.");

            var destino = flujo.Estados.FirstOrDefault(e => e.Codigo == request.CodigoEstadoNuevo)
                ?? throw new InvalidOperationException("Estado destino no existe en el flujo.");

            var transicion = flujo.Transiciones.First(t =>
                t.CodigoDesde == trabajo.CodigoEstadoActual &&
                t.CodigoHasta == request.CodigoEstadoNuevo);

            if (transicion.RequiereMotivo && string.IsNullOrWhiteSpace(request.Motivo))
                throw new InvalidOperationException("Esta transición requiere un motivo.");

            var anterior = trabajo.CodigoEstadoActual;
            var ahora = DateTime.Now;
            trabajo.CodigoEstadoActual = destino.Codigo;

            if (destino.Codigo == "EN_PREPARACION" && trabajo.FechaInicio == null)
            {
                trabajo.FechaInicio = ahora;
                if (NormalizarSlaModo(trabajo.SlaModoInicioSnapshot) == ProduccionConstantes.SlaModoInicioPreparacion)
                    trabajo.FechaLimiteObjetivo = ahora.AddSeconds(trabajo.SlaObjetivoSegundosSnapshot);
            }

            if (destino.EsTerminal)
            {
                trabajo.FechaCompletado = ahora;
                trabajo.ActivoEnTablero = false;
                if (destino.Codigo == "CANCELADA")
                {
                    trabajo.FechaCancelacion = ahora;
                    trabajo.IdUsuarioCancelacion = request.IdUsuario;
                    trabajo.MotivoCancelacion = Truncate(request.Motivo, 500);
                }
            }

            foreach (var item in trabajo.Items)
            {
                item.CodigoEstado = destino.Codigo;
                item.IdUsuarioUltimoCambio = request.IdUsuario;
                if (destino.Codigo == "EN_PREPARACION" && item.FechaInicio == null)
                    item.FechaInicio = ahora;
                if (destino.Codigo == "LISTA" || (destino.EsTerminal && destino.CuentaParaCompletar))
                    item.FechaListo ??= ahora;
            }

            _ctx.ProduccionHistorial.Add(new ProduccionHistorial
            {
                IdTrabajo = trabajo.IdTrabajo,
                CodigoEstadoAnterior = anterior,
                CodigoEstadoNuevo = destino.Codigo,
                IdUsuario = request.IdUsuario,
                Fecha = ahora,
                Motivo = Truncate(request.Motivo, 500),
                Origen = ProduccionConstantes.HistorialOrigenUi
            });

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "El trabajo fue modificado por otro usuario. Recargue e intente de nuevo.");
            }

            return (await ObtenerAsync(idEmpresa, idTrabajo))!;
        }

        public async Task<ProduccionTrabajoDto> CancelarAsync(int idEmpresa, int idTrabajo, ProduccionCancelarRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Motivo))
                throw new InvalidOperationException("El motivo de cancelación es obligatorio.");

            var trabajo = await CargarParaMutacionAsync(idEmpresa, idTrabajo);
            ValidarConcurrencia(trabajo, request.CodigoEstadoEsperado, request.RowVersion);

            var flujo = await _ctx.ProduccionFlujo
                .AsNoTracking()
                .Include(f => f.Estados)
                .Include(f => f.Transiciones)
                .FirstAsync(f => f.IdFlujo == trabajo.IdFlujo);

            var cancelado = flujo.Estados.FirstOrDefault(e => e.Codigo == "CANCELADA" && e.EsTerminal)
                ?? throw new InvalidOperationException("El flujo no define estado CANCELADA.");

            var permitida = flujo.Transiciones.Any(t =>
                t.CodigoDesde == trabajo.CodigoEstadoActual && t.CodigoHasta == cancelado.Codigo);

            if (!permitida)
                throw new InvalidOperationException("No se puede cancelar desde el estado actual.");

            var anterior = trabajo.CodigoEstadoActual;
            var ahora = DateTime.Now;
            trabajo.CodigoEstadoActual = cancelado.Codigo;
            trabajo.FechaCancelacion = ahora;
            trabajo.FechaCompletado = ahora;
            trabajo.IdUsuarioCancelacion = request.IdUsuario;
            trabajo.MotivoCancelacion = Truncate(request.Motivo, 500);
            trabajo.ActivoEnTablero = false;

            foreach (var item in trabajo.Items)
            {
                item.CodigoEstado = cancelado.Codigo;
                item.IdUsuarioUltimoCambio = request.IdUsuario;
            }

            _ctx.ProduccionHistorial.Add(new ProduccionHistorial
            {
                IdTrabajo = trabajo.IdTrabajo,
                CodigoEstadoAnterior = anterior,
                CodigoEstadoNuevo = cancelado.Codigo,
                IdUsuario = request.IdUsuario,
                Fecha = ahora,
                Motivo = trabajo.MotivoCancelacion,
                Origen = ProduccionConstantes.HistorialOrigenUi
            });

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "El trabajo fue modificado por otro usuario. Recargue e intente de nuevo.");
            }

            return (await ObtenerAsync(idEmpresa, idTrabajo))!;
        }

        public async Task<ProduccionTrabajoDto> CambiarPrioridadAsync(int idEmpresa, int idTrabajo, ProduccionPrioridadRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Solicitud inválida.");

            var prioridad = NormalizarPrioridad(request.Prioridad);
            var trabajo = await CargarParaMutacionAsync(idEmpresa, idTrabajo);
            ValidarConcurrencia(trabajo, request.CodigoEstadoEsperado, request.RowVersion);

            trabajo.Prioridad = prioridad;
            _ctx.ProduccionHistorial.Add(new ProduccionHistorial
            {
                IdTrabajo = trabajo.IdTrabajo,
                CodigoEstadoAnterior = trabajo.CodigoEstadoActual,
                CodigoEstadoNuevo = trabajo.CodigoEstadoActual,
                IdUsuario = request.IdUsuario,
                Fecha = DateTime.Now,
                Motivo = $"Prioridad: {prioridad}",
                Origen = ProduccionConstantes.HistorialOrigenUi
            });

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "El trabajo fue modificado por otro usuario. Recargue e intente de nuevo.");
            }

            return (await ObtenerAsync(idEmpresa, idTrabajo))!;
        }

        public async Task<List<ProduccionHistorialDto>> ListarHistorialAsync(int idEmpresa, int idTrabajo)
        {
            var existe = await _ctx.ProduccionTrabajo.AsNoTracking()
                .AnyAsync(t => t.IdEmpresa == idEmpresa && t.IdTrabajo == idTrabajo);
            if (!existe)
                return new List<ProduccionHistorialDto>();

            return await _ctx.ProduccionHistorial.AsNoTracking()
                .Where(h => h.IdTrabajo == idTrabajo)
                .OrderByDescending(h => h.Fecha)
                .Select(h => new ProduccionHistorialDto
                {
                    IdHistorial = h.IdHistorial,
                    IdTrabajo = h.IdTrabajo,
                    IdTrabajoItem = h.IdTrabajoItem,
                    CodigoEstadoAnterior = h.CodigoEstadoAnterior,
                    CodigoEstadoNuevo = h.CodigoEstadoNuevo,
                    IdUsuario = h.IdUsuario,
                    Fecha = h.Fecha,
                    Motivo = h.Motivo,
                    Origen = h.Origen
                })
                .ToListAsync();
        }

        public async Task<ProduccionDashboardResumenDto> ObtenerDashboardAsync(int idEmpresa, string? tipoTrabajoCodigo = null)
        {
            var q = _ctx.ProduccionTrabajo.AsNoTracking().Where(t => t.IdEmpresa == idEmpresa);
            if (!string.IsNullOrWhiteSpace(tipoTrabajoCodigo))
                q = q.Where(t => t.TipoTrabajoCodigo == tipoTrabajoCodigo);

            var activos = await q.Where(t => t.ActivoEnTablero).ToListAsync();
            var inicioHoy = DateTime.Today;
            var completadosHoy = await q
                .Where(t => t.FechaCompletado != null && t.FechaCompletado >= inicioHoy && t.CodigoEstadoActual != "CANCELADA")
                .ToListAsync();

            var pendientes = 0;
            var enEjecucion = 0;
            var retrasados = 0;

            foreach (var t in activos)
            {
                if (t.CodigoEstadoActual == "PENDIENTE") pendientes++;
                else enEjecucion++;

                if (CalcularSemaforo(t) == ProduccionConstantes.SemaforoRetrasado)
                    retrasados++;
            }

            double? promedio = null;
            var conDuracion = completadosHoy
                .Where(t => t.FechaCompletado.HasValue && t.FechaInicio.HasValue)
                .Select(t => (t.FechaCompletado!.Value - t.FechaInicio!.Value).TotalSeconds)
                .Where(s => s >= 0)
                .ToList();
            if (conDuracion.Count > 0)
                promedio = conDuracion.Average();

            return new ProduccionDashboardResumenDto
            {
                Pendientes = pendientes,
                EnEjecucion = enEjecucion,
                CompletadosHoy = completadosHoy.Count,
                Retrasados = retrasados,
                TiempoPromedioSegundosHoy = promedio
            };
        }

        private async Task<bool> MotorDisponibleAsync(int idEmpresa)
        {
            if (!await _config.EstaActivoAsync(idEmpresa))
                return false;

            var modulo = await _modulos.GetModuloByCodigo(ProduccionConstantes.CodigoModulo);
            if (modulo == null) return false;
            return await _empresaModulos.EmpresaTieneModulo(idEmpresa, modulo.Id);
        }

        private async Task<ProduccionFlujo?> ResolverFlujoEntityAsync(int idEmpresa, string tipoTrabajo)
        {
            var empresa = await _ctx.ProduccionFlujo
                .Include(f => f.Estados)
                .Include(f => f.Transiciones)
                .Where(f => f.Activo && f.IdEmpresa == idEmpresa && f.TipoTrabajoCodigo == tipoTrabajo)
                .OrderByDescending(f => f.Version)
                .FirstOrDefaultAsync();
            if (empresa != null) return empresa;

            return await _ctx.ProduccionFlujo
                .Include(f => f.Estados)
                .Include(f => f.Transiciones)
                .Where(f => f.Activo && f.IdEmpresa == null && f.TipoTrabajoCodigo == tipoTrabajo)
                .OrderByDescending(f => f.Version)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ResolverEstacionIdAsync(int idEmpresa, string? codigo)
        {
            var cod = string.IsNullOrWhiteSpace(codigo) ? ProduccionConstantes.EstacionGeneral : codigo.Trim();
            var est = await _ctx.ProduccionEstacion.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.Codigo == cod && e.Activa);
            if (est != null) return est.IdEstacion;

            var general = await _ctx.ProduccionEstacion.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.Codigo == ProduccionConstantes.EstacionGeneral && e.Activa);
            return general?.IdEstacion;
        }

        private async Task<ProduccionTrabajo> CargarParaMutacionAsync(int idEmpresa, int idTrabajo)
        {
            var trabajo = await _ctx.ProduccionTrabajo
                .AsTracking()
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.IdEmpresa == idEmpresa && t.IdTrabajo == idTrabajo);
            if (trabajo == null)
                throw new InvalidOperationException("Trabajo no encontrado.");
            return trabajo;
        }

        private static void ValidarConcurrencia(ProduccionTrabajo trabajo, string estadoEsperado, string rowVersionBase64)
        {
            if (!string.Equals(trabajo.CodigoEstadoActual, estadoEsperado, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Estado esperado '{estadoEsperado}' no coincide con el actual '{trabajo.CodigoEstadoActual}'.");

            if (string.IsNullOrWhiteSpace(rowVersionBase64))
                throw new InvalidOperationException("RowVersion es obligatorio.");

            byte[] incoming;
            try
            {
                incoming = Convert.FromBase64String(rowVersionBase64);
            }
            catch
            {
                throw new InvalidOperationException("RowVersion inválido.");
            }

            if (trabajo.RowVersion == null || trabajo.RowVersion.Length == 0)
            {
                throw new InvalidOperationException(
                    "El trabajo fue modificado por otro usuario. Recargue e intente de nuevo.");
            }

            var actual = Convert.ToBase64String(trabajo.RowVersion);
            if (!string.Equals(actual, Convert.ToBase64String(incoming), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "El trabajo fue modificado por otro usuario. Recargue e intente de nuevo.");
            }
        }

        private async Task<ProduccionTrabajoDto> MapTrabajoDtoAsync(ProduccionTrabajo t)
        {
            var estados = await _ctx.ProduccionFlujoEstado.AsNoTracking()
                .Where(e => e.IdFlujo == t.IdFlujo)
                .ToListAsync();
            var nombreEstado = estados.FirstOrDefault(e => e.Codigo == t.CodigoEstadoActual)?.NombreVisible;

            var ahora = DateTime.Now;
            var metricas = CalcularMetricasTiempo(t, ahora);

            return new ProduccionTrabajoDto
            {
                IdTrabajo = t.IdTrabajo,
                IdEmpresa = t.IdEmpresa,
                TipoTrabajoCodigo = t.TipoTrabajoCodigo,
                IdFlujo = t.IdFlujo,
                CodigoEstadoActual = t.CodigoEstadoActual,
                NombreEstadoActual = nombreEstado,
                OrigenModulo = t.OrigenModulo,
                OrigenTipo = t.OrigenTipo,
                OrigenId = t.OrigenId,
                NumeroVisible = t.NumeroVisible,
                NombreVisible = t.NombreVisible,
                Referencia = t.Referencia,
                EtiquetaContexto = t.EtiquetaContexto,
                Observacion = t.Observacion,
                IdUsuarioSolicita = t.IdUsuarioSolicita,
                Prioridad = t.Prioridad,
                SlaObjetivoSegundosSnapshot = t.SlaObjetivoSegundosSnapshot,
                SlaAdvertenciaSegundosSnapshot = t.SlaAdvertenciaSegundosSnapshot,
                SlaModoInicioSnapshot = NormalizarSlaModo(t.SlaModoInicioSnapshot),
                FechaCreacion = t.FechaCreacion,
                FechaLimiteObjetivo = t.FechaLimiteObjetivo,
                FechaInicio = t.FechaInicio,
                FechaCompletado = t.FechaCompletado,
                FechaCancelacion = t.FechaCancelacion,
                MotivoCancelacion = t.MotivoCancelacion,
                ActivoEnTablero = t.ActivoEnTablero,
                PlantillaCodigo = t.PlantillaCodigo,
                SemaforoSla = CalcularSemaforo(t, ahora),
                SegundosTranscurridos = metricas.SegundosSla,
                SegundosEnCola = metricas.SegundosEnCola,
                SegundosPreparacion = metricas.SegundosPreparacion,
                SegundosTotal = metricas.SegundosTotal,
                RowVersion = t.RowVersion != null ? Convert.ToBase64String(t.RowVersion) : "",
                Items = (t.Items ?? new List<ProduccionTrabajoItem>())
                    .OrderBy(i => i.Orden)
                    .Select(i => new ProduccionTrabajoItemDto
                    {
                        IdTrabajoItem = i.IdTrabajoItem,
                        OrigenDetalleId = i.OrigenDetalleId,
                        IdEstacion = i.IdEstacion,
                        CodigoItem = i.CodigoItem,
                        NombreItem = i.NombreItem,
                        Cantidad = i.Cantidad,
                        Observacion = i.Observacion,
                        VariacionesTexto = i.VariacionesTexto,
                        CodigoEstado = i.CodigoEstado,
                        Orden = i.Orden,
                        RowVersion = i.RowVersion != null ? Convert.ToBase64String(i.RowVersion) : ""
                    })
                    .ToList()
            };
        }

        private static string CalcularSemaforo(ProduccionTrabajo t, DateTime? ahoraRef = null)
        {
            if (!t.ActivoEnTablero || t.CodigoEstadoActual == "CANCELADA" || t.CodigoEstadoActual == "ENTREGADA")
                return ProduccionConstantes.SemaforoCompletado;

            var ahora = ahoraRef ?? DateTime.Now;
            var modo = NormalizarSlaModo(t.SlaModoInicioSnapshot);
            var inicioSla = ResolverInicioSla(t, modo);
            if (inicioSla == null)
                return ProduccionConstantes.SemaforoEnCola;

            var limite = t.FechaLimiteObjetivo ?? inicioSla.Value.AddSeconds(t.SlaObjetivoSegundosSnapshot);
            var advertencia = inicioSla.Value.AddSeconds(t.SlaAdvertenciaSegundosSnapshot);

            if (ahora >= limite)
                return ProduccionConstantes.SemaforoRetrasado;
            if (ahora >= advertencia)
                return ProduccionConstantes.SemaforoAdvertencia;
            return ProduccionConstantes.SemaforoOk;
        }

        private static DateTime? ResolverInicioSla(ProduccionTrabajo t, string modo)
        {
            if (modo == ProduccionConstantes.SlaModoInicioPreparacion)
                return t.FechaInicio;
            return t.FechaCreacion;
        }

        private static (int SegundosSla, int SegundosEnCola, int SegundosPreparacion, int SegundosTotal)
            CalcularMetricasTiempo(ProduccionTrabajo t, DateTime ahora)
        {
            var finCola = t.FechaInicio ?? ahora;
            var segundosEnCola = (int)Math.Max(0, (finCola - t.FechaCreacion).TotalSeconds);

            var segundosPreparacion = 0;
            if (t.FechaInicio.HasValue)
            {
                var finPrep = t.FechaCompletado ?? ahora;
                segundosPreparacion = (int)Math.Max(0, (finPrep - t.FechaInicio.Value).TotalSeconds);
            }

            var finTotal = t.FechaCompletado ?? ahora;
            var segundosTotal = (int)Math.Max(0, (finTotal - t.FechaCreacion).TotalSeconds);

            var modo = NormalizarSlaModo(t.SlaModoInicioSnapshot);
            var inicioSla = ResolverInicioSla(t, modo);
            var segundosSla = inicioSla == null
                ? 0
                : (int)Math.Max(0, ((t.FechaCompletado ?? ahora) - inicioSla.Value).TotalSeconds);

            return (segundosSla, segundosEnCola, segundosPreparacion, segundosTotal);
        }

        private static string NormalizarSlaModo(string? modo)
        {
            if (string.Equals(modo, ProduccionConstantes.SlaModoInicioPreparacion, StringComparison.OrdinalIgnoreCase))
                return ProduccionConstantes.SlaModoInicioPreparacion;
            return ProduccionConstantes.SlaModoCreacion;
        }

        private static string NormalizarPrioridad(string? prioridad)
        {
            if (string.Equals(prioridad, ProduccionConstantes.PrioridadUrgente, StringComparison.OrdinalIgnoreCase))
                return ProduccionConstantes.PrioridadUrgente;
            if (string.Equals(prioridad, ProduccionConstantes.PrioridadAlta, StringComparison.OrdinalIgnoreCase))
                return ProduccionConstantes.PrioridadAlta;
            return ProduccionConstantes.PrioridadNormal;
        }

        private static string? Truncate(string? value, int max)
        {
            if (value == null) return null;
            var v = value.Trim();
            return v.Length <= max ? v : v.Substring(0, max);
        }
    }
}
