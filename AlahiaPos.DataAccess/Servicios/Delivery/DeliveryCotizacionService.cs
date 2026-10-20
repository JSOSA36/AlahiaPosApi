using System.Text.Json;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Delivery;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Delivery
{
    public class DeliveryCotizacionService : IDeliveryCotizacionService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly AlahiaPosContext _ctx;
        private readonly IDeliveryDistanceProvider _proveedor;
        private readonly ILogger<DeliveryCotizacionService> _log;

        public DeliveryCotizacionService(
            AlahiaPosContext ctx,
            IDeliveryDistanceProvider proveedor,
            ILogger<DeliveryCotizacionService> log)
        {
            _ctx = ctx;
            _proveedor = proveedor;
            _log = log;
        }

        public async Task<DeliveryCotizacionDto> CotizarAsync(
            int idEmpresa,
            int? idSucursal,
            bool esDelivery,
            decimal? latitud,
            decimal? longitud,
            CancellationToken cancellationToken)
        {
            var snap = await EvaluarAsync(idEmpresa, idSucursal, esDelivery, latitud, longitud, cancellationToken);
            return MapDto(snap);
        }

        public async Task<DeliverySnapshot> ResolverParaPedidoAsync(
            int idEmpresa,
            int? idSucursal,
            bool esDelivery,
            decimal? latitud,
            decimal? longitud,
            CancellationToken cancellationToken)
        {
            var snap = await EvaluarAsync(idEmpresa, idSucursal, esDelivery, latitud, longitud, cancellationToken);
            if (!snap.PuedeConfirmar)
                throw new ArgumentException(snap.Mensaje ?? "No se pudo calcular el delivery.");
            return snap;
        }

        public async Task<DeliveryConfigPantallaDto> ObtenerPantallaAsync(int idEmpresa, int idSucursalSesion)
        {
            var sucursales = await ListarSucursalesAsync(idEmpresa);
            var canal = await _ctx.PedidoOnlineCanal.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.Activo);
            var empresa = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => new { e.Latitude, e.Longitude })
                .FirstOrDefaultAsync();

            DeliveryConfig? fila = null;
            var alcance = "ninguna";
            if (idSucursalSesion > 0)
            {
                fila = await _ctx.DeliveryConfig.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.IdSucursal == idSucursalSesion);
                if (fila != null) alcance = "sucursal";
            }
            if (fila == null)
            {
                fila = await _ctx.DeliveryConfig.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.IdSucursal == null);
                if (fila != null) alcance = "empresa";
            }

            var sucursalCanal = sucursales.FirstOrDefault(s => canal?.IdSucursal == s.IdSucursal);
            var sesion = sucursales.FirstOrDefault(s => s.IdSucursal == idSucursalSesion);
            var dto = new DeliveryConfigPantallaDto
            {
                Existe = fila != null,
                Alcance = alcance,
                IdSucursalSesion = idSucursalSesion,
                IdSucursalCanal = canal?.IdSucursal,
                NombreSucursalCanal = sucursalCanal?.Nombre,
                SucursalTieneCoordenadas = sesion?.TieneCoordenadas == true || sucursalCanal?.TieneCoordenadas == true,
                EmpresaTieneCoordenadas = DeliveryCotizacionMotor.TryCoord(empresa?.Latitude, empresa?.Longitude, out _, out _),
                Sucursales = sucursales
            };
            if (fila == null)
                return dto;

            dto.DeliveryHabilitado = fila.DeliveryHabilitado;
            dto.CalculoPorDistancia = fila.CalculoPorDistancia;
            dto.TarifaBase = fila.TarifaBase;
            dto.PrecioPorKilometro = fila.PrecioPorKilometro;
            dto.CostoMinimoDelivery = fila.CostoMinimoDelivery;
            dto.RedondeoDelivery = fila.RedondeoDelivery;
            dto.DistanciaMaximaKm = fila.DistanciaMaximaKm;
            dto.AccionFueraDeZona = fila.AccionFueraDeZona;
            dto.Proveedor = fila.Proveedor;
            dto.MinutosCache = fila.MinutosCache;
            dto.ExigirGps = fila.ExigirGps;
            return dto;
        }

        public async Task<DeliveryConfigPantallaDto> GuardarAsync(
            int idEmpresa,
            int idSucursalSesion,
            int idUsuario,
            DeliveryConfigGuardarRequest request)
        {
            request ??= new DeliveryConfigGuardarRequest();
            Validar(request);
            var alcanceEmpresa = !string.Equals(request.Alcance, "sucursal", StringComparison.OrdinalIgnoreCase);
            int? idSucursal = null;
            if (!alcanceEmpresa)
            {
                if (idSucursalSesion <= 0)
                    throw new ArgumentException("Seleccione la sucursal que va a usar esta tarifa.");
                idSucursal = idSucursalSesion;
            }

            var fila = await _ctx.DeliveryConfig
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.IdSucursal == idSucursal);
            var anterior = fila == null ? null : JsonSerializer.Serialize(Vista(fila), JsonOpts);
            if (fila == null)
            {
                fila = new DeliveryConfig
                {
                    IdEmpresa = idEmpresa,
                    IdSucursal = idSucursal,
                    FechaCreacion = DateTime.Now
                };
                _ctx.DeliveryConfig.Add(fila);
            }

            fila.DeliveryHabilitado = request.DeliveryHabilitado;
            fila.CalculoPorDistancia = request.CalculoPorDistancia;
            fila.TarifaBase = request.TarifaBase;
            fila.PrecioPorKilometro = request.PrecioPorKilometro;
            fila.CostoMinimoDelivery = request.CostoMinimoDelivery;
            fila.RedondeoDelivery = request.RedondeoDelivery;
            fila.DistanciaMaximaKm = request.DistanciaMaximaKm is > 0 ? request.DistanciaMaximaKm : null;
            fila.AccionFueraDeZona = request.AccionFueraDeZona.Trim().ToUpperInvariant();
            fila.Proveedor = "OSRM";
            fila.MinutosCache = request.MinutosCache;
            fila.ExigirGps = request.ExigirGps;
            fila.FechaActualizacion = DateTime.Now;

            var canal = await _ctx.PedidoOnlineCanal
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.Activo);
            if (canal != null)
            {
                if (request.IdSucursalCanal is > 0)
                {
                    var existe = await _ctx.Sucursales.AsNoTracking()
                        .AnyAsync(s => s.IdSucursal == request.IdSucursalCanal && s.IdEmpresa == idEmpresa && s.Activa);
                    if (!existe)
                        throw new ArgumentException("La sucursal que recibe Pedir no pertenece a esta empresa.");
                    canal.IdSucursal = request.IdSucursalCanal;
                }
                else
                {
                    canal.IdSucursal = null;
                }
            }

            var nuevo = JsonSerializer.Serialize(Vista(fila), JsonOpts);
            if (!string.Equals(anterior, nuevo, StringComparison.Ordinal))
            {
                _ctx.DeliveryConfigAuditoria.Add(new DeliveryConfigAuditoria
                {
                    IdEmpresa = idEmpresa,
                    IdSucursal = idSucursal,
                    IdUsuario = idUsuario,
                    Fecha = DateTime.Now,
                    Accion = anterior == null ? "ALTA" : "UPSERT",
                    ValorAnteriorJson = anterior,
                    ValorNuevoJson = nuevo
                });
            }

            await _ctx.SaveChangesAsync();
            return await ObtenerPantallaAsync(idEmpresa, idSucursalSesion);
        }

        private async Task<DeliverySnapshot> EvaluarAsync(
            int idEmpresa,
            int? idSucursal,
            bool esDelivery,
            decimal? latitud,
            decimal? longitud,
            CancellationToken cancellationToken)
        {
            var sucursalId = idSucursal is > 0 ? idSucursal : null;
            var configs = await _ctx.DeliveryConfig.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && (c.IdSucursal == null || c.IdSucursal == sucursalId))
                .ToListAsync(cancellationToken);
            var deSucursal = sucursalId == null ? null : configs.FirstOrDefault(c => c.IdSucursal == sucursalId);
            var deEmpresa = configs.FirstOrDefault(c => c.IdSucursal == null);
            var tarifa = DeliveryCotizacionMotor.Elegir(deSucursal == null ? null : ATarifa(deSucursal), deEmpresa == null ? null : ATarifa(deEmpresa));

            var tieneGps = latitud.HasValue && longitud.HasValue && latitud.Value != 0 && longitud.Value != 0;
            var origen = await OrigenAsync(idEmpresa, sucursalId, cancellationToken);
            var tieneOrigen = sucursalId is > 0 && origen.ok;
            var preparado = DeliveryCotizacionMotor.Preparar(tarifa, esDelivery, tieneGps, tieneOrigen);
            if (!preparado.LlamarProveedor || tarifa == null || sucursalId is not > 0)
                return preparado;

            if (!string.Equals(tarifa.Proveedor, _proveedor.Nombre, StringComparison.OrdinalIgnoreCase))
                return DeliveryCotizacionMotor.ConDistancia(
                    tarifa,
                    new DeliveryMedicion { Ok = false, Proveedor = tarifa.Proveedor, Error = "Proveedor no disponible." },
                    origen.lat, origen.lng, latitud!.Value, longitud!.Value, DateTime.Now);

            var medicion = await LeerCacheAsync(idEmpresa, sucursalId.Value, tarifa.Proveedor, origen.lat, origen.lng, latitud!.Value, longitud!.Value, cancellationToken);
            if (medicion == null)
            {
                medicion = await _proveedor.MedirAsync(origen.lat, origen.lng, latitud.Value, longitud.Value, cancellationToken);
                if (!medicion.Ok)
                    _log.LogWarning(
                        "Delivery sin ruta. Empresa {Empresa} sucursal {Sucursal}. {Error}",
                        idEmpresa, sucursalId, medicion.Error);
                else if (tarifa.MinutosCache > 0)
                    await GuardarCacheAsync(idEmpresa, sucursalId.Value, medicion, origen.lat, origen.lng, latitud.Value, longitud.Value, tarifa.MinutosCache, cancellationToken);
            }

            return DeliveryCotizacionMotor.ConDistancia(
                tarifa, medicion, origen.lat, origen.lng, latitud.Value, longitud.Value, DateTime.Now);
        }

        private async Task<(bool ok, decimal lat, decimal lng)> OrigenAsync(int idEmpresa, int? idSucursal, CancellationToken ct)
        {
            string? sucLat = null, sucLng = null;
            if (idSucursal is > 0)
            {
                var suc = await _ctx.Sucursales.AsNoTracking()
                    .Where(s => s.IdSucursal == idSucursal && s.IdEmpresa == idEmpresa)
                    .Select(s => new { s.Latitude, s.Longitude })
                    .FirstOrDefaultAsync(ct);
                sucLat = suc?.Latitude;
                sucLng = suc?.Longitude;
            }
            var emp = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => new { e.Latitude, e.Longitude })
                .FirstOrDefaultAsync(ct);
            var ok = DeliveryCotizacionMotor.TryOrigen(sucLat, sucLng, emp?.Latitude, emp?.Longitude, out var lat, out var lng, out _);
            return (ok, lat, lng);
        }

        private async Task<DeliveryMedicion?> LeerCacheAsync(
            int idEmpresa, int idSucursal, string proveedor,
            decimal origenLat, decimal origenLng, decimal destLat, decimal destLng,
            CancellationToken ct)
        {
            var oLat = DeliveryCotizacionMotor.ClaveCoord(origenLat);
            var oLng = DeliveryCotizacionMotor.ClaveCoord(origenLng);
            var dLat = DeliveryCotizacionMotor.ClaveCoord(destLat);
            var dLng = DeliveryCotizacionMotor.ClaveCoord(destLng);
            var ahora = DateTime.Now;
            var hit = await _ctx.DeliveryDistanciaCache.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa
                    && c.IdSucursal == idSucursal
                    && c.Proveedor == proveedor
                    && c.LatitudClave == dLat
                    && c.LongitudClave == dLng
                    && c.OrigenLatitudClave == oLat
                    && c.OrigenLongitudClave == oLng
                    && c.FechaExpiracion > ahora)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync(ct);
            if (hit == null) return null;
            return new DeliveryMedicion
            {
                Ok = true,
                DistanciaKm = hit.DistanciaKm,
                DuracionSegundos = hit.DuracionSegundos,
                Proveedor = hit.Proveedor,
                DesdeCache = true
            };
        }

        private async Task GuardarCacheAsync(
            int idEmpresa, int idSucursal, DeliveryMedicion medicion,
            decimal origenLat, decimal origenLng, decimal destLat, decimal destLng,
            int minutos, CancellationToken ct)
        {
            _ctx.DeliveryDistanciaCache.Add(new DeliveryDistanciaCache
            {
                IdEmpresa = idEmpresa,
                IdSucursal = idSucursal,
                Proveedor = medicion.Proveedor,
                LatitudDestino = destLat,
                LongitudDestino = destLng,
                LatitudClave = DeliveryCotizacionMotor.ClaveCoord(destLat),
                LongitudClave = DeliveryCotizacionMotor.ClaveCoord(destLng),
                OrigenLatitudClave = DeliveryCotizacionMotor.ClaveCoord(origenLat),
                OrigenLongitudClave = DeliveryCotizacionMotor.ClaveCoord(origenLng),
                DistanciaKm = medicion.DistanciaKm,
                DuracionSegundos = medicion.DuracionSegundos,
                FechaCreacion = DateTime.Now,
                FechaExpiracion = DateTime.Now.AddMinutes(minutos)
            });
            await _ctx.SaveChangesAsync(ct);
        }

        private async Task<List<DeliverySucursalOpcionDto>> ListarSucursalesAsync(int idEmpresa)
        {
            var rows = await _ctx.Sucursales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.Activa)
                .OrderByDescending(s => s.EsPrincipal)
                .ThenBy(s => s.Nombre)
                .Select(s => new { s.IdSucursal, s.Nombre, s.EsPrincipal, s.Latitude, s.Longitude })
                .ToListAsync();
            return rows.Select(s => new DeliverySucursalOpcionDto
            {
                IdSucursal = s.IdSucursal,
                Nombre = s.Nombre,
                EsPrincipal = s.EsPrincipal,
                TieneCoordenadas = DeliveryCotizacionMotor.TryCoord(s.Latitude, s.Longitude, out _, out _)
            }).ToList();
        }

        private static DeliveryTarifa ATarifa(DeliveryConfig fila) => new()
        {
            DeliveryHabilitado = fila.DeliveryHabilitado,
            CalculoPorDistancia = fila.CalculoPorDistancia,
            TarifaBase = fila.TarifaBase,
            PrecioPorKilometro = fila.PrecioPorKilometro,
            CostoMinimo = fila.CostoMinimoDelivery,
            Redondeo = fila.RedondeoDelivery,
            DistanciaMaximaKm = fila.DistanciaMaximaKm,
            AccionFueraDeZona = fila.AccionFueraDeZona,
            Proveedor = fila.Proveedor,
            MinutosCache = fila.MinutosCache,
            ExigirGps = fila.ExigirGps
        };

        private static DeliveryCotizacionDto MapDto(DeliverySnapshot snap) => new()
        {
            DistanciaKm = snap.DistanciaKm,
            DuracionSegundos = snap.DuracionSegundos,
            CostoDelivery = snap.Costo,
            Proveedor = snap.Proveedor,
            FueraDeZona = snap.FueraDeZona,
            PuedeConfirmar = snap.PuedeConfirmar,
            CalculoAutomatico = snap.CalculoAutomatico,
            Mensaje = snap.Mensaje
        };

        private static object Vista(DeliveryConfig fila) => new
        {
            fila.DeliveryHabilitado,
            fila.CalculoPorDistancia,
            fila.TarifaBase,
            fila.PrecioPorKilometro,
            fila.CostoMinimoDelivery,
            fila.RedondeoDelivery,
            fila.DistanciaMaximaKm,
            fila.AccionFueraDeZona,
            fila.Proveedor,
            fila.MinutosCache,
            fila.ExigirGps
        };

        private static void Validar(DeliveryConfigGuardarRequest request)
        {
            if (request.TarifaBase < 0 || request.PrecioPorKilometro < 0 || request.CostoMinimoDelivery < 0 || request.RedondeoDelivery < 0)
                throw new ArgumentException("Las tarifas no pueden ser negativas.");
            if (request.DistanciaMaximaKm is < 0)
                throw new ArgumentException("La distancia máxima no puede ser negativa.");
            if (request.MinutosCache < 0 || request.MinutosCache > 10080)
                throw new ArgumentException("El tiempo de caché debe estar entre 0 y 10080 minutos.");
            var accion = (request.AccionFueraDeZona ?? "").Trim().ToUpperInvariant();
            if (accion != DeliveryFueraDeZona.Rechazar
                && accion != DeliveryFueraDeZona.AvisarYPermitir
                && accion != DeliveryFueraDeZona.SinLimite)
                throw new ArgumentException("La acción fuera de zona no es válida.");
            request.AccionFueraDeZona = accion;
            var proveedor = (request.Proveedor ?? "").Trim();
            if (proveedor.Length > 0 && !proveedor.Equals("OSRM", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Ese proveedor de rutas todavía no está disponible.");
        }
    }
}
