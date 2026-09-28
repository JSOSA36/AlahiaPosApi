using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AlahiaPos.DataAccess.Servicios.WhatsApp
{
    public class WhatsAppCitasService : IWhatsAppCitas
    {
        private readonly IOptionsMonitor<WhatsAppCitasOptions> _monitor;
        private readonly IHttpClientFactory _httpFactory;
        private readonly AlahiaPosContext _ctx;
        private readonly ILogger<WhatsAppCitasService> _logger;
        private static readonly object InitLock = new();
        private static string? _initedSid;
        private static readonly ConcurrentDictionary<string, string> SidPorNombre = new(StringComparer.OrdinalIgnoreCase);

        public WhatsAppCitasService(
            IOptionsMonitor<WhatsAppCitasOptions> monitor,
            IHttpClientFactory httpFactory,
            AlahiaPosContext ctx,
            ILogger<WhatsAppCitasService> logger)
        {
            _monitor = monitor;
            _httpFactory = httpFactory;
            _ctx = ctx;
            _logger = logger;
        }

        private WhatsAppCitasOptions Opt => _monitor.CurrentValue ?? new WhatsAppCitasOptions();

        public bool EstaListo
        {
            get
            {
                var o = Opt;
                return o.Enabled
                    && !string.IsNullOrWhiteSpace(o.AccountSid)
                    && !string.IsNullOrWhiteSpace(o.AuthToken)
                    && !string.IsNullOrWhiteSpace(o.From)
                    && o.From.IndexOf("XXXX", StringComparison.OrdinalIgnoreCase) < 0;
            }
        }

        public IReadOnlyList<WhatsAppCitasPlantillaDef> PlantillasMeta()
            => WhatsAppCitasPlantillas.Definiciones();

        public WhatsAppCitasEstadoDto ObtenerEstado()
        {
            var o = Opt;
            var from = string.IsNullOrWhiteSpace(o.From) || o.From.Contains("XXXX", StringComparison.OrdinalIgnoreCase)
                ? null
                : NormalizarFrom(o.From);
            var listo = EstaListo;
            return new WhatsAppCitasEstadoDto
            {
                Listo = listo,
                Enabled = o.Enabled,
                TieneAccountSid = !string.IsNullOrWhiteSpace(o.AccountSid),
                TieneAuthToken = !string.IsNullOrWhiteSpace(o.AuthToken),
                From = from,
                HoraRecordatorio = string.IsNullOrWhiteSpace(o.HoraRecordatorio) ? "08:00" : o.HoraRecordatorio,
                Nota = listo
                    ? $"Listo. Cada WhatsApp de cita se evalúa a RD${Opt.PrecioClienteDop:0} (costo Alahia RD${Opt.CostoAlahiaDop:0}). Aún no se factura el ciclo."
                    : "Falta AccountSid, AuthToken o From (Twilio / WhatsApp Business). El salón no se entera: la cita se guarda igual.",
                Plantillas = PlantillasMeta().Select(p => new WhatsAppCitasPlantillaRegistroDto
                {
                    Nombre = p.Nombre,
                    ContentSid = SidConfigurado(p.Nombre)
                        ?? (SidPorNombre.TryGetValue(p.Nombre, out var sid) ? sid : null)
                }).ToList()
            };
        }

        public async Task EnviarCitaAsync(WhatsAppCitaMensaje mensaje, CancellationToken ct = default)
        {
            if (!EstaListo || mensaje == null)
                return;

            var e164 = NormalizarTelefonoDo(mensaje.Telefono);
            if (e164 == null)
            {
                _logger.LogInformation("WhatsApp citas: teléfono inválido para {Tipo}", mensaje.Tipo);
                return;
            }

            try
            {
                AsegurarInit();
                var from = new PhoneNumber(NormalizarFrom(Opt.From!));
                var to = new PhoneNumber($"whatsapp:{e164}");
                var contentSid = await ResolverContentSidAsync(mensaje.Tipo, ct);

                MessageResource? enviado;
                if (!string.IsNullOrWhiteSpace(contentSid))
                {
                    var vars = JsonSerializer.Serialize(WhatsAppCitasPlantillas.Variables(mensaje));
                    enviado = await MessageResource.CreateAsync(
                        from: from,
                        to: to,
                        contentSid: contentSid,
                        contentVariables: vars);
                }
                else
                {
                    _logger.LogInformation(
                        "WhatsApp citas: {Nombre} sin ContentSid aprobado; texto libre (sandbox).",
                        WhatsAppCitasPlantillas.Nombre(mensaje.Tipo));
                    enviado = await MessageResource.CreateAsync(
                        from: from,
                        to: to,
                        body: WhatsAppCitasPlantillas.Render(mensaje));
                }

                await RegistrarConsumoAsync(mensaje, enviado?.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp citas no se envió ({Tipo} → {Tel})", mensaje.Tipo, e164);
            }
        }

        private async Task RegistrarConsumoAsync(WhatsAppCitaMensaje mensaje, string? twilioSid)
        {
            if (mensaje.IdEmpresa <= 0)
                return;

            try
            {
                var o = Opt;
                var precio = mensaje.EsPrueba ? 0m : (o.PrecioClienteDop > 0 ? o.PrecioClienteDop : 5m);
                var costo = mensaje.EsPrueba ? 0m : (o.CostoAlahiaDop > 0 ? o.CostoAlahiaDop : 3m);

                _ctx.WhatsAppCitasConsumo.Add(new WhatsAppCitasConsumo
                {
                    IdEmpresa = mensaje.IdEmpresa,
                    IdCita = mensaje.IdCita,
                    Tipo = mensaje.Tipo.ToString(),
                    Telefono = mensaje.Telefono,
                    PrecioClienteDop = precio,
                    CostoAlahiaDop = costo,
                    TwilioSid = twilioSid,
                    EsPrueba = mensaje.EsPrueba,
                    Fecha = DateTime.Now
                });
                await _ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp citas: no se pudo registrar consumo empresa {Id}", mensaje.IdEmpresa);
            }
        }

        public async Task<WhatsAppCitasConsumoResumenDto> ResumenConsumoAsync(
            int? idEmpresa = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)
        {
            var o = Opt;
            var q = _ctx.WhatsAppCitasConsumo.AsNoTracking().Where(c => !c.EsPrueba);
            if (idEmpresa.HasValue && idEmpresa.Value > 0)
                q = q.Where(c => c.IdEmpresa == idEmpresa.Value);
            if (desde.HasValue)
                q = q.Where(c => c.Fecha >= desde.Value);
            if (hasta.HasValue)
                q = q.Where(c => c.Fecha < hasta.Value.Date.AddDays(1));

            var mensajes = await q.CountAsync(ct);
            var precioCliente = await q.Select(c => (decimal?)c.PrecioClienteDop).SumAsync(ct) ?? 0m;
            var costoAlahia = await q.Select(c => (decimal?)c.CostoAlahiaDop).SumAsync(ct) ?? 0m;

            var porEmpresa = await q
                .GroupBy(c => c.IdEmpresa)
                .Select(g => new WhatsAppCitasConsumoEmpresaDto
                {
                    IdEmpresa = g.Key,
                    Mensajes = g.Count(),
                    PrecioClienteDop = g.Sum(x => x.PrecioClienteDop),
                    CostoAlahiaDop = g.Sum(x => x.CostoAlahiaDop),
                    MargenDop = g.Sum(x => x.PrecioClienteDop - x.CostoAlahiaDop)
                })
                .OrderByDescending(x => x.PrecioClienteDop)
                .ToListAsync(ct);

            var ids = porEmpresa.Select(p => p.IdEmpresa).ToList();
            var nombres = await _ctx.Empresas.AsNoTracking()
                .Where(e => ids.Contains(e.IdEmpresa))
                .ToDictionaryAsync(e => e.IdEmpresa, e => e.NombreComercial, ct);
            foreach (var p in porEmpresa)
                p.NombreEmpresa = nombres.GetValueOrDefault(p.IdEmpresa);

            var ultimosRows = await q.OrderByDescending(c => c.Fecha).Take(50).ToListAsync(ct);
            var ultimos = ultimosRows.Select(c => new WhatsAppCitasConsumoDto
            {
                Id = c.Id,
                IdEmpresa = c.IdEmpresa,
                NombreEmpresa = nombres.GetValueOrDefault(c.IdEmpresa),
                IdCita = c.IdCita,
                Tipo = c.Tipo,
                Telefono = c.Telefono,
                PrecioClienteDop = c.PrecioClienteDop,
                CostoAlahiaDop = c.CostoAlahiaDop,
                MargenDop = c.PrecioClienteDop - c.CostoAlahiaDop,
                EsPrueba = c.EsPrueba,
                Fecha = c.Fecha
            }).ToList();

            return new WhatsAppCitasConsumoResumenDto
            {
                PrecioPorMensajeDop = o.PrecioClienteDop > 0 ? o.PrecioClienteDop : 5m,
                CostoPorMensajeDop = o.CostoAlahiaDop > 0 ? o.CostoAlahiaDop : 3m,
                Mensajes = mensajes,
                PrecioClienteDop = precioCliente,
                CostoAlahiaDop = costoAlahia,
                MargenDop = precioCliente - costoAlahia,
                PorEmpresa = porEmpresa,
                Ultimos = ultimos
            };
        }

        public async Task<IReadOnlyList<WhatsAppCitasPlantillaRegistroDto>> AsegurarPlantillasAsync(CancellationToken ct = default)
        {
            if (!EstaListo)
                throw new InvalidOperationException("WhatsApp Citas no está configurado (AccountSid, AuthToken y From).");

            AsegurarInit();
            await CargarSidsDesdeTwilioAsync(ct);

            var resultado = new List<WhatsAppCitasPlantillaRegistroDto>();
            foreach (var def in PlantillasMeta())
            {
                var item = new WhatsAppCitasPlantillaRegistroDto { Nombre = def.Nombre };
                try
                {
                    var sid = SidConfigurado(def.Nombre)
                        ?? (SidPorNombre.TryGetValue(def.Nombre, out var existente) ? existente : null);
                    if (string.IsNullOrWhiteSpace(sid))
                    {
                        sid = await CrearContentAsync(def, ct);
                        item.CreadaAhora = true;
                        item.Detalle = "Creada en Twilio Content.";
                    }
                    else
                    {
                        item.Detalle = "Ya existía.";
                    }

                    item.ContentSid = sid;
                    if (!string.IsNullOrWhiteSpace(sid))
                    {
                        SidPorNombre[def.Nombre] = sid;
                        item.EstadoAprobacion = await SolicitarAprobacionWhatsAppAsync(sid, def, ct);
                    }
                }
                catch (Exception ex)
                {
                    item.Detalle = ex.Message;
                    _logger.LogWarning(ex, "No se pudo registrar plantilla {Nombre}", def.Nombre);
                }

                resultado.Add(item);
            }

            return resultado;
        }

        private async Task<string?> ResolverContentSidAsync(WhatsAppCitaTipo tipo, CancellationToken ct)
        {
            var nombre = WhatsAppCitasPlantillas.Nombre(tipo);
            var deConfig = SidConfigurado(nombre);
            if (!string.IsNullOrWhiteSpace(deConfig))
                return deConfig.Trim();

            if (SidPorNombre.TryGetValue(nombre, out var cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            await CargarSidsDesdeTwilioAsync(ct);
            return SidPorNombre.TryGetValue(nombre, out var found) ? found : null;
        }

        private string? SidConfigurado(string nombre)
        {
            var t = Opt.Templates;
            var sid = nombre switch
            {
                WhatsAppCitasPlantillas.RecibidaNombre => t?.Recibida,
                WhatsAppCitasPlantillas.ConfirmadaNombre => t?.Confirmada,
                WhatsAppCitasPlantillas.RecordatorioNombre => t?.Recordatorio,
                WhatsAppCitasPlantillas.CanceladaNombre => t?.Cancelada,
                _ => null
            };
            return string.IsNullOrWhiteSpace(sid) ? null : sid.Trim();
        }

        private async Task CargarSidsDesdeTwilioAsync(CancellationToken ct)
        {
            using var doc = await GetJsonAsync("Content?PageSize=200", ct);
            if (doc == null)
                return;

            foreach (var item in doc.RootElement.GetProperty("contents").EnumerateArray())
            {
                var name = item.TryGetProperty("friendly_name", out var n) ? n.GetString() : null;
                var sid = item.TryGetProperty("sid", out var s) ? s.GetString() : null;
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(sid))
                    SidPorNombre[name] = sid;
            }
        }

        private async Task<string?> CrearContentAsync(WhatsAppCitasPlantillaDef def, CancellationToken ct)
        {
            var payload = new Dictionary<string, object?>
            {
                ["friendly_name"] = def.Nombre,
                ["language"] = def.Idioma,
                ["variables"] = def.VariablesEjemplo,
                ["types"] = new Dictionary<string, object>
                {
                    ["twilio/text"] = new Dictionary<string, string> { ["body"] = def.Cuerpo }
                }
            };

            using var doc = await PostJsonAsync("Content", payload, ct);
            var sid = doc?.RootElement.TryGetProperty("sid", out var s) == true ? s.GetString() : null;
            if (string.IsNullOrWhiteSpace(sid))
                throw new InvalidOperationException($"Twilio no devolvió SID para {def.Nombre}.");
            return sid;
        }

        private async Task<string> SolicitarAprobacionWhatsAppAsync(
            string contentSid,
            WhatsAppCitasPlantillaDef def,
            CancellationToken ct)
        {
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["name"] = def.Nombre,
                    ["category"] = def.Categoria
                };
                using var doc = await PostJsonAsync($"Content/{contentSid}/ApprovalRequests/whatsapp", payload, ct);
                if (doc != null && doc.RootElement.TryGetProperty("status", out var st))
                    return st.GetString() ?? "submitted";
                return "submitted";
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Aprobación WhatsApp {Nombre}: {Msg}", def.Nombre, ex.Message);
                return "pendiente_o_ya_enviada";
            }
        }

        private async Task<JsonDocument?> GetJsonAsync(string path, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, ContentUrl(path));
            AdjuntarAuth(req);
            using var client = _httpFactory.CreateClient();
            using var res = await client.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twilio Content GET {Path}: {Code} {Body}", path, (int)res.StatusCode, text);
                return null;
            }
            return JsonDocument.Parse(text);
        }

        private async Task<JsonDocument?> PostJsonAsync(string path, object payload, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, ContentUrl(path));
            AdjuntarAuth(req);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var client = _httpFactory.CreateClient();
            using var res = await client.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Twilio {path}: {(int)res.StatusCode} {text}");
            return string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text);
        }

        private void AdjuntarAuth(HttpRequestMessage req)
        {
            var raw = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Opt.AccountSid}:{Opt.AuthToken}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        private static Uri ContentUrl(string path)
            => new($"https://content.twilio.com/v1/{path.TrimStart('/')}");

        private void AsegurarInit()
        {
            var sid = Opt.AccountSid;
            lock (InitLock)
            {
                if (_initedSid == sid)
                    return;
                TwilioClient.Init(sid, Opt.AuthToken);
                _initedSid = sid;
            }
        }

        internal static string NormalizarFrom(string from)
        {
            var f = from.Trim();
            if (f.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
                return f;
            if (!f.StartsWith("+"))
                f = "+" + Regex.Replace(f, @"\D", "");
            return "whatsapp:" + f;
        }

        /// <summary>RD 10 dígitos → +1XXXXXXXXXX. Acepta +1 / 1 ya prefijado.</summary>
        public static string? NormalizarTelefonoDo(string? raw)
        {
            var d = Regex.Replace(raw ?? "", @"\D", "");
            if (d.StartsWith("00"))
                d = d[2..];
            if (d.Length == 11 && d.StartsWith("1"))
                return "+" + d;
            if (d.Length == 10)
                return "+1" + d;
            if (d.Length >= 11 && d.Length <= 15)
                return "+" + d;
            return null;
        }
    }
}
