using System.Globalization;
using System.Text.RegularExpressions;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class SalonService : ISalonService
    {
        private const int TipoDocumentoOrden = 10;
        private static readonly Regex Digitos = new(@"(\d+)", RegexOptions.Compiled);

        private readonly AlahiaPosContext _ctx;

        public SalonService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<SalonSnapshotDto> ObtenerSnapshotAsync(int idEmpresa, int? idZona = null)
        {
            var snapshot = await CargarSnapshotAsync(idEmpresa, idZona);
            if (idEmpresa > 0 && EsDesarrollo() && snapshot.Zonas.Sum(z => z.Mesas.Count) == 0)
            {
                await SembrarSalonDevAsync(idEmpresa);
                snapshot = await CargarSnapshotAsync(idEmpresa, idZona);
            }

            return snapshot;
        }

        private async Task<SalonSnapshotDto> CargarSnapshotAsync(int idEmpresa, int? idZona = null)
        {
            var zonasQuery = _ctx.Zonas.AsNoTracking()
                .Where(z => z.IdEmpresa == idEmpresa);
            if (idZona.HasValue && idZona.Value > 0)
            {
                zonasQuery = zonasQuery.Where(z => z.ZonaId == idZona.Value);
            }

            var zonas = await zonasQuery
                .OrderBy(z => z.ZonaName)
                .Select(z => new { z.ZonaId, z.ZonaName })
                .ToListAsync();

            var zonaIds = zonas.Select(z => z.ZonaId).ToList();
            var mesas = zonaIds.Count == 0
                ? new List<MesaRaw>()
                : await _ctx.Mesas.AsNoTracking()
                    .Where(m => zonaIds.Contains(m.ZonaId))
                    .Select(m => new MesaRaw
                    {
                        IdMesa = m.IdMesa,
                        ZonaId = m.ZonaId,
                        Numero = m.Numero ?? "",
                        Tipo = m.Tipo ?? "",
                        Estado = m.Estado ?? "",
                        IsActiva = m.IsActiva
                    })
                    .ToListAsync();

            var mesaIds = mesas.Select(m => m.IdMesa).ToList();
            var ordenes = mesaIds.Count == 0
                ? new List<OrdenRaw>()
                : await _ctx.FacturaHeaders.AsNoTracking()
                    .Where(h =>
                        h.IdEmpresa == idEmpresa
                        && h.IdTipoDocumentos == TipoDocumentoOrden
                        && h.EstaCancelada == false
                        && h.EstaCerrada == false
                        && h.IdMesa != null
                        && mesaIds.Contains(h.IdMesa.Value))
                    .Select(h => new OrdenRaw
                    {
                        IdMesa = h.IdMesa!.Value,
                        Fecha = h.FechaInseccion,
                        Hora = h.Hora ?? "",
                        Total = h.Total,
                        EstadoOrden = h.Estado_Orden ?? "",
                        Cliente = h.NombreCuenta ?? h.Nota ?? ""
                    })
                    .ToListAsync();

            var porMesa = ordenes.GroupBy(o => o.IdMesa).ToDictionary(g => g.Key, g => g.ToList());
            var snapshot = new SalonSnapshotDto { IdEmpresa = idEmpresa };

            foreach (var zona in zonas)
            {
                var dtoZona = new SalonZonaDto
                {
                    ZonaId = zona.ZonaId,
                    Nombre = zona.ZonaName
                };

                foreach (var mesa in mesas.Where(m => m.ZonaId == zona.ZonaId).OrderBy(m => m.Numero))
                {
                    porMesa.TryGetValue(mesa.IdMesa, out var abiertas);
                    abiertas ??= new List<OrdenRaw>();
                    dtoZona.Mesas.Add(MapearMesa(mesa, abiertas, zona.ZonaName));
                }

                snapshot.Zonas.Add(dtoZona);
            }

            var todas = snapshot.Zonas.SelectMany(z => z.Mesas).ToList();
            snapshot.MesasDisponibles = todas.Count(m => m.Estado == "disponible");
            snapshot.MesasOcupadas = todas.Count(m => m.Estado == "ocupada");
            snapshot.MesasReservadas = todas.Count(m => m.Estado == "reservada");
            snapshot.MesasCuenta = todas.Count(m => m.Estado == "cuenta");
            snapshot.MesasFuera = todas.Count(m => m.Estado == "fuera");
            return snapshot;
        }

        public async Task<IReadOnlyList<SalonOrdenDto>> ObtenerOrdenesMesaAsync(int idEmpresa, int idMesa)
        {
            if (idEmpresa <= 0 || idMesa <= 0)
            {
                return Array.Empty<SalonOrdenDto>();
            }

            var headers = await _ctx.FacturaHeaders.AsNoTracking()
                .Where(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdMesa == idMesa
                    && h.IdTipoDocumentos == TipoDocumentoOrden
                    && h.EstaCancelada == false
                    && h.EstaCerrada == false)
                .OrderByDescending(h => h.IdFacturaHeader)
                .Select(h => new
                {
                    h.IdFacturaHeader,
                    h.NumeroDocumento,
                    h.NombreCuenta,
                    h.Nota,
                    h.Total,
                    h.Hora
                })
                .ToListAsync();

            if (headers.Count == 0)
            {
                return Array.Empty<SalonOrdenDto>();
            }

            var ids = headers.Select(h => h.IdFacturaHeader).ToList();
            var lineas = await _ctx.FacturaDetalles.AsNoTracking()
                .Where(d => ids.Contains(d.IdFacturaHeader))
                .Select(d => new { d.IdFacturaHeader, d.IdProducto, d.Comentario })
                .ToListAsync();

            var productoIds = lineas.Select(l => l.IdProducto).Distinct().ToList();
            var nombres = productoIds.Count == 0
                ? new Dictionary<int, string>()
                : await _ctx.Productos.AsNoTracking()
                    .Where(p => productoIds.Contains(p.IdProducto))
                    .ToDictionaryAsync(p => p.IdProducto, p => p.Descripcion ?? p.Nombre ?? "");

            return headers.Select(h =>
            {
                var items = lineas.Where(l => l.IdFacturaHeader == h.IdFacturaHeader).ToList();
                var productos = new List<string>();
                foreach (var item in items)
                {
                    if (nombres.TryGetValue(item.IdProducto, out var nombre) && !string.IsNullOrWhiteSpace(nombre))
                    {
                        productos.Add(nombre);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Comentario))
                    {
                        productos.Add(item.Comentario);
                    }
                }

                return new SalonOrdenDto
                {
                    IdFacturaHeader = h.IdFacturaHeader,
                    NumeroDocumento = h.NumeroDocumento ?? "",
                    NombreCuenta = string.IsNullOrWhiteSpace(h.NombreCuenta) ? (h.Nota ?? "") : h.NombreCuenta,
                    Total = h.Total,
                    Hora = h.Hora ?? "",
                    Items = items.Count,
                    Productos = productos
                };
            }).ToList();
        }

        private static bool EsDesarrollo()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        }

        private async Task SembrarSalonDevAsync(int idEmpresa)
        {
            var impresora = await _ctx.Zonas.AsNoTracking()
                .Select(z => (int?)z.ImpresoraId)
                .FirstOrDefaultAsync() ?? 1;

            await _ctx.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM dbo.Zonas WHERE IdEmpresa = {idEmpresa})
BEGIN
    INSERT INTO dbo.Zonas (ZonaName, Estado, ImpresoraId, IdEmpresa, FechaInseccion)
    VALUES (N'Salón principal', 1, {impresora}, {idEmpresa}, GETDATE());
END");

            await _ctx.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @ZonaId int = (SELECT TOP 1 ZonaId FROM dbo.Zonas WHERE IdEmpresa = {idEmpresa} ORDER BY ZonaId);
IF @ZonaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Mesas WHERE ZonaId = @ZonaId)
BEGIN
    INSERT INTO dbo.Mesas (Tipo, Numero, ImagePath, Detalle, IsActiva, Estado, ZonaId, FechaInseccion)
    VALUES
        (N'2 Sillas', N'Mesa 1', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 2', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 3', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'4 Sillas', N'Mesa 4', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'6 Sillas', N'Mesa 5', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'6 Sillas', N'Mesa 6', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'8 Sillas', N'Mesa 7', N'', N'', 1, N'Libre', @ZonaId, GETDATE()),
        (N'2 Sillas', N'Mesa 8', N'', N'', 1, N'Libre', @ZonaId, GETDATE());
END");
        }

        private static SalonMesaDto MapearMesa(MesaRaw mesa, List<OrdenRaw> abiertas, string zonaNombre = "")
        {
            var capacidad = ResolverCapacidad(mesa.Capacidad, mesa.Tipo);
            var ocupantes = abiertas
                .Select(o => o.Comensales ?? 0)
                .Where(c => c > 0)
                .DefaultIfEmpty(abiertas.Count > 0 ? capacidad : 0)
                .Max();
            if (ocupantes > capacidad)
            {
                ocupantes = capacidad;
            }

            var estado = ResolverEstado(mesa, abiertas);
            if (estado == "disponible")
            {
                ocupantes = 0;
            }

            var minutos = abiertas.Count == 0
                ? (int?)null
                : Math.Max(0, (int)(DateTime.Now - abiertas.Min(o => ResolverApertura(o.Fecha, o.Hora))).TotalMinutes);

            return new SalonMesaDto
            {
                IdMesa = mesa.IdMesa,
                ZonaId = mesa.ZonaId,
                Numero = string.IsNullOrWhiteSpace(mesa.Numero) ? $"Mesa {mesa.IdMesa}" : mesa.Numero.Trim(),
                Tipo = mesa.Tipo,
                Capacidad = capacidad,
                Ocupantes = ocupantes,
                Forma = ResolverForma(mesa.Forma, capacidad),
                Estado = estado,
                IsActiva = mesa.IsActiva,
                PosX = mesa.PosX,
                PosY = mesa.PosY,
                Rotacion = mesa.Rotacion,
                Escala = mesa.Escala,
                MinutosOcupada = minutos,
                Total = Math.Round(abiertas.Sum(o => o.Total), 2),
                CantidadOrdenes = abiertas.Count,
                Cliente = abiertas
                    .Select(o => o.Cliente)
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                ZonaNombre = zonaNombre
            };
        }

        public async Task<SalonZonaDto> CrearZonaAsync(int idEmpresa, string nombre)
        {
            var nombreZona = NormalizarNombre(nombre);
            if (idEmpresa <= 0)
            {
                throw new ArgumentException("Empresa inválida.");
            }

            if (string.IsNullOrWhiteSpace(nombreZona))
            {
                throw new ArgumentException("Escribe el nombre de la zona (ej. Terraza, Salón principal).");
            }

            var existe = await _ctx.Zonas.AsNoTracking()
                .AnyAsync(z => z.IdEmpresa == idEmpresa && z.ZonaName == nombreZona);
            if (existe)
            {
                throw new InvalidOperationException("Ya existe una zona con ese nombre.");
            }

            var impresora = await _ctx.Zonas.AsNoTracking()
                .Where(z => z.IdEmpresa == idEmpresa && z.ImpresoraId > 0)
                .Select(z => (int?)z.ImpresoraId)
                .FirstOrDefaultAsync()
                ?? await _ctx.Zonas.AsNoTracking()
                    .Select(z => (int?)z.ImpresoraId)
                    .FirstOrDefaultAsync()
                ?? 1;

            var zona = new AlahiaPos.Entities.Domain.Zonas
            {
                ZonaName = nombreZona,
                Estado = true,
                ImpresoraId = impresora,
                IdEmpresa = idEmpresa,
                FechaInseccion = DateTime.Now
            };
            _ctx.Zonas.Add(zona);
            await _ctx.SaveChangesAsync();

            return new SalonZonaDto { ZonaId = zona.ZonaId, Nombre = zona.ZonaName };
        }

        public async Task<SalonZonaDto> RenombrarZonaAsync(int idEmpresa, int zonaId, string nombre)
        {
            var nombreZona = NormalizarNombre(nombre);
            if (string.IsNullOrWhiteSpace(nombreZona))
            {
                throw new ArgumentException("Escribe el nombre de la zona.");
            }

            var zona = await _ctx.Zonas.AsTracking()
                .FirstOrDefaultAsync(z => z.ZonaId == zonaId && z.IdEmpresa == idEmpresa)
                ?? throw new InvalidOperationException("Zona no encontrada.");

            var duplicada = await _ctx.Zonas.AsNoTracking()
                .AnyAsync(z => z.IdEmpresa == idEmpresa && z.ZonaId != zonaId && z.ZonaName == nombreZona);
            if (duplicada)
            {
                throw new InvalidOperationException("Ya existe una zona con ese nombre.");
            }

            zona.ZonaName = nombreZona;
            await _ctx.SaveChangesAsync();
            return new SalonZonaDto { ZonaId = zona.ZonaId, Nombre = zona.ZonaName };
        }

        public async Task<SalonMesaDto> CrearMesaAsync(int idEmpresa, int zonaId, string numero, int capacidad)
        {
            if (idEmpresa <= 0)
            {
                throw new ArgumentException("Empresa inválida.");
            }

            var zona = await _ctx.Zonas.AsNoTracking()
                .FirstOrDefaultAsync(z => z.ZonaId == zonaId && z.IdEmpresa == idEmpresa)
                ?? throw new InvalidOperationException("Selecciona una zona válida.");

            var sillas = capacidad is >= 1 and <= 12 ? capacidad : 4;
            var nombreMesa = string.IsNullOrWhiteSpace(numero)
                ? await SiguienteNumeroMesaAsync(zonaId)
                : numero.Trim();

            var mesa = new AlahiaPos.Entities.Domain.Mesas
            {
                Tipo = $"{sillas} Sillas",
                Numero = nombreMesa,
                ImagePath = "",
                Detalle = "",
                IsActiva = true,
                Estado = "Libre",
                ZonaId = zonaId,
                FechaInseccion = DateTime.Now
            };
            _ctx.Mesas.Add(mesa);
            await _ctx.SaveChangesAsync();

            return MapearMesa(new MesaRaw
            {
                IdMesa = mesa.IdMesa,
                ZonaId = mesa.ZonaId,
                Numero = mesa.Numero ?? "",
                Tipo = mesa.Tipo ?? "",
                Estado = mesa.Estado ?? "",
                IsActiva = mesa.IsActiva
            }, new List<OrdenRaw>(), zona.ZonaName);
        }

        private async Task<string> SiguienteNumeroMesaAsync(int zonaId)
        {
            var cantidad = await _ctx.Mesas.CountAsync(m => m.ZonaId == zonaId);
            return $"Mesa {cantidad + 1}";
        }

        private static string NormalizarNombre(string nombre)
        {
            return string.Join(" ", (nombre ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        private static string ResolverEstado(MesaRaw mesa, List<OrdenRaw> abiertas)
        {
            var estadoMesa = (mesa.Estado ?? "").Trim().ToLowerInvariant();
            if (estadoMesa.Contains("fuera") || estadoMesa.Contains("inactiv") || estadoMesa.Contains("manten"))
            {
                return "fuera";
            }

            if (abiertas.Any(o => EsCuentaSolicitada(o.EstadoOrden)))
            {
                return "cuenta";
            }

            if (estadoMesa.Contains("reserv"))
            {
                return "reservada";
            }

            if (abiertas.Count > 0 || estadoMesa.Contains("ocupad"))
            {
                return "ocupada";
            }

            return "disponible";
        }

        private static bool EsCuentaSolicitada(string estadoOrden)
        {
            var t = (estadoOrden ?? "").Trim().ToLowerInvariant();
            return t.Contains("cuenta") || t.Contains("cobrar") || t == "listo";
        }

        private static int ResolverCapacidad(int? capacidad, string tipo)
        {
            if (capacidad.HasValue && capacidad.Value > 0)
            {
                return Math.Min(12, capacidad.Value);
            }

            var m = Digitos.Match(tipo ?? "");
            if (m.Success && int.TryParse(m.Value, out var n) && n > 0)
            {
                return Math.Min(12, n);
            }

            return 4;
        }

        private static DateTime ResolverApertura(DateTime fecha, string hora)
        {
            if (string.IsNullOrWhiteSpace(hora))
            {
                return fecha;
            }

            var culturas = new[] { CultureInfo.InvariantCulture, new CultureInfo("es-DO"), CultureInfo.CurrentCulture };
            foreach (var cultura in culturas)
            {
                if (DateTime.TryParse(hora, cultura, DateTimeStyles.None, out var parsed))
                {
                    return fecha.Date.Add(parsed.TimeOfDay);
                }
            }

            return fecha;
        }

        private static string ResolverForma(string forma, int capacidad)
        {
            var f = (forma ?? "").Trim().ToLowerInvariant();
            if (f is "round" or "square" or "rect")
            {
                return f;
            }

            if (capacidad <= 2)
            {
                return "rect";
            }

            return capacidad <= 4 ? "square" : "round";
        }

        private sealed class MesaRaw
        {
            public int IdMesa { get; set; }
            public int ZonaId { get; set; }
            public string Numero { get; set; } = "";
            public string Tipo { get; set; } = "";
            public int? Capacidad { get; set; }
            public string Forma { get; set; } = "";
            public string Estado { get; set; } = "";
            public bool IsActiva { get; set; }
            public decimal? PosX { get; set; }
            public decimal? PosY { get; set; }
            public decimal? Rotacion { get; set; }
            public decimal? Escala { get; set; }
        }

        private sealed class OrdenRaw
        {
            public int IdMesa { get; set; }
            public DateTime Fecha { get; set; }
            public string Hora { get; set; } = "";
            public decimal Total { get; set; }
            public int? Comensales { get; set; }
            public string EstadoOrden { get; set; } = "";
            public string Cliente { get; set; } = "";
        }
    }
}
