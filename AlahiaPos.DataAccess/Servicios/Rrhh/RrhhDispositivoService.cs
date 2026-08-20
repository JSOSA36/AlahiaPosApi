using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhDispositivoService : IRrhhDispositivoService
    {
        private readonly AlahiaPosContext _db;
        private readonly IRrhhPonchadorService _ponchador;

        public RrhhDispositivoService(AlahiaPosContext db, IRrhhPonchadorService ponchador)
        {
            _db = db;
            _ponchador = ponchador;
        }

        public async Task<IReadOnlyList<RrhhDispositivoDto>> ListarAsync(int idEmpresa) =>
            await _db.RrhhPonchadorDispositivo.AsNoTracking()
                .Where(d => d.IdEmpresa == idEmpresa)
                .OrderBy(d => d.Nombre)
                .Select(d => MapDispositivo(d))
                .ToListAsync();

        public async Task<RrhhDispositivoDto> UpsertAsync(RrhhDispositivoDto dto)
        {
            if (dto.IdEmpresa <= 0) throw new ArgumentException("Indique la empresa.");
            var serial = (dto.Serial ?? "").Trim().ToUpperInvariant();
            var ip = string.IsNullOrWhiteSpace(dto.DireccionIp) ? null : dto.DireccionIp.Trim();
            if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("Indique el serial o la IP del reloj.");
            if (string.IsNullOrWhiteSpace(serial))
                serial = "IP-" + ip!.Replace('.', '-');
            var nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? serial : dto.Nombre.Trim();
            var proveedor = string.IsNullOrWhiteSpace(dto.Proveedor) ? "ZKTECO" : dto.Proveedor.Trim().ToUpperInvariant();
            var puerto = dto.Puerto is > 0 and < 65536
                ? dto.Puerto
                : proveedor == "ZKTECO" ? 4370 : 80;

            var otro = await _db.RrhhPonchadorDispositivo.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Serial == serial && d.IdEmpresa != dto.IdEmpresa);
            if (otro != null)
                throw new InvalidOperationException("Ese serial ya está registrado en otra empresa.");

            RrhhPonchadorDispositivo row;
            if (dto.IdDispositivo > 0)
            {
                row = await _db.RrhhPonchadorDispositivo.AsTracking()
                    .FirstOrDefaultAsync(d => d.IdDispositivo == dto.IdDispositivo && d.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Reloj no encontrado.");
            }
            else
            {
                row = await _db.RrhhPonchadorDispositivo.AsTracking()
                    .FirstOrDefaultAsync(d => d.IdEmpresa == dto.IdEmpresa && d.Serial == serial)
                    ?? new RrhhPonchadorDispositivo { IdEmpresa = dto.IdEmpresa };
                if (row.IdDispositivo == 0) _db.RrhhPonchadorDispositivo.Add(row);
            }

            row.Serial = serial;
            row.Nombre = nombre;
            row.Proveedor = proveedor;
            row.Token = string.IsNullOrWhiteSpace(dto.Token) ? null : dto.Token.Trim();
            row.DireccionIp = ip;
            row.Puerto = puerto;
            row.ClaveComunicacion = string.IsNullOrWhiteSpace(dto.ClaveComunicacion) ? null : dto.ClaveComunicacion.Trim();
            row.Activo = dto.Activo;
            await _db.SaveChangesAsync();
            return MapDispositivo(row);
        }

        public async Task<IReadOnlyList<RrhhDispositivoPersonaDto>> ListarPersonasAsync(int idEmpresa)
        {
            var maps = await _db.RrhhPonchadorPersona.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa)
                .ToListAsync();
            var ids = maps.Select(m => m.IdEmpleados).Distinct().ToList();
            var nombres = await _db.EmpleadosP.AsNoTracking()
                .Where(e => ids.Contains(e.IdEmpleados))
                .ToDictionaryAsync(e => e.IdEmpleados, e => e.Nombre ?? "");
            return maps
                .OrderBy(m => nombres.TryGetValue(m.IdEmpleados, out var n) ? n : m.CodigoDispositivo)
                .Select(m => new RrhhDispositivoPersonaDto
                {
                    IdPersonaDispositivo = m.IdPersonaDispositivo,
                    IdEmpresa = m.IdEmpresa,
                    IdEmpleados = m.IdEmpleados,
                    NombreEmpleado = nombres.TryGetValue(m.IdEmpleados, out var n) ? n : "",
                    CodigoDispositivo = m.CodigoDispositivo,
                    Activo = m.Activo
                })
                .ToList();
        }

        public async Task<RrhhDispositivoPersonaDto> UpsertPersonaAsync(RrhhDispositivoPersonaDto dto)
        {
            var codigo = (dto.CodigoDispositivo ?? "").Trim();
            if (dto.IdEmpresa <= 0 || dto.IdEmpleados <= 0)
                throw new ArgumentException("Indique empresa y colaborador.");
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código del reloj (PIN/ID) es obligatorio.");

            var emp = await _db.EmpleadosP.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa && e.IdEmpleados == dto.IdEmpleados)
                ?? throw new InvalidOperationException("El colaborador no existe.");

            var row = dto.IdPersonaDispositivo > 0
                ? await _db.RrhhPonchadorPersona.AsTracking()
                    .FirstOrDefaultAsync(p => p.IdPersonaDispositivo == dto.IdPersonaDispositivo && p.IdEmpresa == dto.IdEmpresa)
                : await _db.RrhhPonchadorPersona.AsTracking()
                    .FirstOrDefaultAsync(p => p.IdEmpresa == dto.IdEmpresa && p.CodigoDispositivo == codigo);

            if (row is null)
            {
                row = new RrhhPonchadorPersona { IdEmpresa = dto.IdEmpresa };
                _db.RrhhPonchadorPersona.Add(row);
            }

            row.IdEmpleados = dto.IdEmpleados;
            row.CodigoDispositivo = codigo;
            row.Activo = dto.Activo;
            await _db.SaveChangesAsync();
            return new RrhhDispositivoPersonaDto
            {
                IdPersonaDispositivo = row.IdPersonaDispositivo,
                IdEmpresa = row.IdEmpresa,
                IdEmpleados = row.IdEmpleados,
                NombreEmpleado = emp.Nombre ?? "",
                CodigoDispositivo = row.CodigoDispositivo,
                Activo = row.Activo
            };
        }

        public async Task<IReadOnlyList<RrhhDispositivoIngestaDto>> ListarIngestasAsync(int idEmpresa, int take = 40)
        {
            var rows = await _db.RrhhPonchadorIngesta.AsNoTracking()
                .Where(i => i.IdEmpresa == idEmpresa)
                .OrderByDescending(i => i.Fecha)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();
            var ids = rows.Where(r => r.IdEmpleados.HasValue).Select(r => r.IdEmpleados!.Value).Distinct().ToList();
            var nombres = await _db.EmpleadosP.AsNoTracking()
                .Where(e => ids.Contains(e.IdEmpleados))
                .ToDictionaryAsync(e => e.IdEmpleados, e => e.Nombre ?? "");
            return rows.Select(r => new RrhhDispositivoIngestaDto
            {
                IdIngesta = r.IdIngesta,
                Serial = r.Serial,
                CodigoDispositivo = r.CodigoDispositivo,
                IdEmpleados = r.IdEmpleados,
                NombreEmpleado = r.IdEmpleados.HasValue && nombres.TryGetValue(r.IdEmpleados.Value, out var n) ? n : null,
                FechaHoraReloj = r.FechaHoraReloj,
                Estado = r.Estado,
                Detalle = r.Detalle,
                Fecha = r.Fecha
            }).ToList();
        }

        public async Task HeartbeatAsync(string serial, string? ip)
        {
            var sn = (serial ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(sn)) return;
            var row = await _db.RrhhPonchadorDispositivo.AsTracking()
                .FirstOrDefaultAsync(d => d.Serial == sn && d.Activo);
            if (row is null) return;
            row.UltimaComunicacion = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        public Task<RrhhDispositivoIngestaResumenDto> IngestarAttLogAsync(string serial, string body, string? ip)
        {
            var lineas = ParseAttLog(body);
            return IngestarLineasAsync(serial, lineas, ip, "ZKTECO");
        }

        public async Task<RrhhDispositivoIngestaResumenDto> IngestarJsonAsync(RrhhDispositivoJsonIngestaRequest req, string? ip)
        {
            var serial = (req.Serial ?? "").Trim().ToUpperInvariant();
            var dev = await _db.RrhhPonchadorDispositivo.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Serial == serial && d.Activo);
            if (dev is null)
                throw new InvalidOperationException("Reloj no registrado. Guarde el serial en Ponchador antes de enviar marcas.");
            if (!string.IsNullOrWhiteSpace(dev.Token) &&
                !string.Equals(dev.Token, req.Token?.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Token del reloj inválido.");

            var lineas = (req.Marcaciones ?? new List<RrhhDispositivoJsonMarcacion>())
                .Select(m => new LineaMarcacion(m.Codigo.Trim(), m.FechaHora, m.Status, m.Tipo))
                .ToList();
            return await IngestarLineasAsync(serial, lineas, ip, string.IsNullOrWhiteSpace(dev.Proveedor) ? "RELOJ" : dev.Proveedor);
        }

        private async Task<RrhhDispositivoIngestaResumenDto> IngestarLineasAsync(
            string serial, IReadOnlyList<LineaMarcacion> lineas, string? ip, string origen)
        {
            var resumen = new RrhhDispositivoIngestaResumenDto { Recibidas = lineas.Count };
            var sn = (serial ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(sn) || lineas.Count == 0) return resumen;

            var dev = await _db.RrhhPonchadorDispositivo.AsTracking()
                .FirstOrDefaultAsync(d => d.Serial == sn && d.Activo);
            if (dev is null)
            {
                resumen.Ignoradas = lineas.Count;
                _db.RrhhPonchadorIngesta.Add(new RrhhPonchadorIngesta
                {
                    Serial = sn,
                    Estado = "SERIAL_DESCONOCIDO",
                    Detalle = $"ip={ip}; lineas={lineas.Count}"
                });
                await _db.SaveChangesAsync();
                return resumen;
            }

            dev.UltimaComunicacion = DateTime.Now;
            var mapas = await _db.RrhhPonchadorPersona.AsNoTracking()
                .Where(p => p.IdEmpresa == dev.IdEmpresa && p.Activo)
                .ToListAsync();
            var byCodigo = mapas.ToDictionary(p => p.CodigoDispositivo, StringComparer.OrdinalIgnoreCase);

            foreach (var linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea.Codigo) || linea.Cuando == default)
                {
                    resumen.Ignoradas++;
                    continue;
                }

                var clave = Trunc($"{sn}|{linea.Codigo}|{linea.Cuando:yyyyMMddHHmmss}", 80)!;
                if (!byCodigo.TryGetValue(linea.Codigo, out var mapa))
                {
                    resumen.SinMapa++;
                    _db.RrhhPonchadorIngesta.Add(new RrhhPonchadorIngesta
                    {
                        IdEmpresa = dev.IdEmpresa,
                        IdDispositivo = dev.IdDispositivo,
                        Serial = sn,
                        CodigoDispositivo = linea.Codigo,
                        FechaHoraReloj = linea.Cuando,
                        Estado = "SIN_MAPA",
                        ClaveExterna = clave,
                        Detalle = "Código del reloj sin colaborador en Alahia"
                    });
                    continue;
                }

                var ya = await _db.RrhhPonchada.AsNoTracking()
                    .AnyAsync(p => p.IdEmpresa == dev.IdEmpresa && p.ClaveExterna == clave);
                if (ya)
                {
                    resumen.Duplicadas++;
                    continue;
                }

                var tipo = ResolverTipo(linea);
                var punch = await _ponchador.PoncharAsync(new RrhhPoncharRequest
                {
                    IdEmpresa = dev.IdEmpresa,
                    IdEmpleados = mapa.IdEmpleados,
                    FechaHora = linea.Cuando,
                    Tipo = tipo,
                    Origen = Trunc(origen, 20) ?? "RELOJ",
                    Dispositivo = Trunc(sn, 120),
                    ClaveExterna = clave,
                    Nota = "reloj " + sn
                }, 0, ip);

                resumen.Integradas++;
                _db.RrhhPonchadorIngesta.Add(new RrhhPonchadorIngesta
                {
                    IdEmpresa = dev.IdEmpresa,
                    IdDispositivo = dev.IdDispositivo,
                    Serial = sn,
                    CodigoDispositivo = linea.Codigo,
                    IdEmpleados = mapa.IdEmpleados,
                    FechaHoraReloj = linea.Cuando,
                    Estado = "OK",
                    ClaveExterna = clave,
                    IdPonchada = punch.IdPonchada
                });
            }

            await _db.SaveChangesAsync();
            return resumen;
        }

        public async Task<RrhhDispositivoConexionDto> ProbarConexionAsync(RrhhDispositivoProbarRequest req)
        {
            string? ip = req.DireccionIp?.Trim();
            int? puerto = req.Puerto;
            if (req.IdDispositivo > 0)
            {
                var d = await _db.RrhhPonchadorDispositivo.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdDispositivo == req.IdDispositivo && x.IdEmpresa == req.IdEmpresa)
                    ?? throw new InvalidOperationException("Reloj no encontrado.");
                ip ??= d.DireccionIp;
                puerto ??= d.Puerto;
            }

            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("Indique la IP del reloj en la red local.");
            var port = puerto is > 0 and < 65536 ? puerto.Value : 4370;
            var sw = Stopwatch.StartNew();
            try
            {
                using var tcp = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await tcp.ConnectAsync(ip, port, cts.Token);
                sw.Stop();
                return new RrhhDispositivoConexionDto
                {
                    Ok = true,
                    Mensaje = $"El reloj responde en {ip}:{port}. Ya se puede conectar el conector/SDK.",
                    DireccionIp = ip,
                    Puerto = port,
                    TiempoMs = (int)sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new RrhhDispositivoConexionDto
                {
                    Ok = false,
                    Mensaje = $"No se alcanzó {ip}:{port}. El API y el reloj deben estar en la misma red (o un agente local). {ex.Message}",
                    DireccionIp = ip,
                    Puerto = port,
                    TiempoMs = (int)sw.ElapsedMilliseconds
                };
            }
        }

        private static string? ResolverTipo(LineaMarcacion linea)
        {
            if (!string.IsNullOrWhiteSpace(linea.Tipo))
                return linea.Tipo.Trim().ToUpperInvariant();
            return linea.StatusZk switch
            {
                0 or 4 => RrhhEstados.Entrada,
                1 or 5 => RrhhEstados.Salida,
                2 => RrhhEstados.SalidaReceso,
                3 => RrhhEstados.RetornoReceso,
                _ => null
            };
        }

        public static List<LineaMarcacion> ParseAttLog(string? body)
        {
            var list = new List<LineaMarcacion>();
            if (string.IsNullOrWhiteSpace(body)) return list;
            foreach (var raw in body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("OK", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = Regex.Split(line, @"[\t,;]+").Where(p => p.Length > 0).ToArray();
                if (parts.Length < 2)
                    parts = Regex.Split(line, @"\s+").Where(p => p.Length > 0).ToArray();
                if (parts.Length < 2) continue;

                var codigo = parts[0].Trim();
                DateTime cuando;
                int statusIdx;
                if (parts.Length >= 3 && DateTime.TryParse($"{parts[1]} {parts[2]}", CultureInfo.InvariantCulture, DateTimeStyles.None, out cuando))
                    statusIdx = 3;
                else if (DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out cuando))
                    statusIdx = 2;
                else
                    continue;

                int? status = null;
                if (parts.Length > statusIdx && int.TryParse(parts[statusIdx], out var st))
                    status = st;
                list.Add(new LineaMarcacion(codigo, cuando, status, null));
            }
            return list;
        }

        private static RrhhDispositivoDto MapDispositivo(RrhhPonchadorDispositivo d) => new()
        {
            IdDispositivo = d.IdDispositivo,
            IdEmpresa = d.IdEmpresa,
            Serial = d.Serial,
            Nombre = d.Nombre,
            Proveedor = d.Proveedor,
            Token = d.Token,
            DireccionIp = d.DireccionIp,
            Puerto = d.Puerto,
            ClaveComunicacion = d.ClaveComunicacion,
            Activo = d.Activo,
            UltimaComunicacion = d.UltimaComunicacion
        };

        private static string? Trunc(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var t = value.Trim();
            return t.Length <= max ? t : t[..max];
        }

        public readonly record struct LineaMarcacion(string Codigo, DateTime Cuando, int? StatusZk, string? Tipo);
    }
}
