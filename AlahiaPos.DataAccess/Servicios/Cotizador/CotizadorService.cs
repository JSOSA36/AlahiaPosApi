using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain.Cotizador;
using AlahiaPos.Entities.Dto.Cotizador;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AlahiaPos.DataAccess.Servicios.Cotizador
{
    public class CotizadorService : ICotizadorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly AlahiaPosContext _ctx;
        private readonly ICotizadorRecomendador _recomendador;
        private readonly IConfiguration _config;

        public CotizadorService(
            AlahiaPosContext ctx,
            ICotizadorRecomendador recomendador,
            IConfiguration config)
        {
            _ctx = ctx;
            _recomendador = recomendador;
            _config = config;
        }

        public async Task<CotizadorCatalogoDto> ObtenerCatalogoAsync()
        {
            var tipos = await _ctx.TipoNegocio.AsNoTracking()
                .Where(t => t.Activo)
                .OrderBy(t => t.Orden)
                .ToListAsync();

            var preselecciones = await _ctx.ModuloTipoNegocio.AsNoTracking()
                .Include(x => x.Modulo)
                .Include(x => x.TipoNegocio)
                .Where(x => x.Preseleccionado && x.Modulo != null && x.TipoNegocio != null)
                .ToListAsync();

            var comerciales = await _ctx.ModuloComercial.AsNoTracking()
                .Include(m => m.Modulo)
                .Where(m => m.Activo && m.VisibleCotizador && m.Modulo != null && m.Modulo.Activo)
                .OrderBy(m => m.Orden)
                .ThenBy(m => m.Modulo!.Nombre)
                .ToListAsync();

            var moduloTipos = await _ctx.ModuloTipoNegocio.AsNoTracking()
                .Include(x => x.TipoNegocio)
                .Include(x => x.Modulo)
                .Where(x => x.Modulo != null && x.TipoNegocio != null)
                .ToListAsync();

            var deps = await _ctx.ModuloDependencia.AsNoTracking()
                .Include(d => d.Modulo)
                .Include(d => d.ModuloRequerido)
                .Where(d => d.Activo && d.Modulo != null && d.ModuloRequerido != null)
                .ToListAsync();

            var parametros = await _ctx.CotizadorParametro.AsNoTracking().ToListAsync();
            var tramos = await _ctx.CotizadorTramoDocumento.AsNoTracking()
                .Where(t => t.Activo)
                .OrderBy(t => t.Orden)
                .ToListAsync();

            var categorias = comerciales
                .GroupBy(c => c.CategoriaComercial)
                .Select(g => new CategoriaModulosDto
                {
                    Codigo = g.Key,
                    Nombre = g.Key,
                    Orden = g.Min(x => x.Orden)
                })
                .OrderBy(c => c.Orden)
                .ThenBy(c => c.Nombre)
                .ToList();

            return new CotizadorCatalogoDto
            {
                TiposNegocio = tipos.Select(t => new TipoNegocioDto
                {
                    Id = t.Id,
                    Codigo = t.Codigo,
                    Nombre = t.Nombre,
                    Descripcion = t.Descripcion,
                    Orden = t.Orden,
                    ModulosPreseleccionados = preselecciones
                        .Where(p => p.TipoNegocioId == t.Id)
                        .Select(p => p.Modulo!.Codigo)
                        .Distinct()
                        .ToList()
                }).ToList(),
                Categorias = categorias,
                Modulos = comerciales.Select(c => new ModuloComercialDto
                {
                    ModuloId = c.ModuloId,
                    Codigo = c.Modulo!.Codigo,
                    Nombre = c.Modulo.Nombre,
                    DescripcionComercial = c.DescripcionComercial ?? c.Modulo.Descripcion,
                    CategoriaComercial = c.CategoriaComercial,
                    Nivel = c.Nivel,
                    ParticipaPrecio = c.ParticipaPrecio,
                    PrecioBaseUSD = c.PrecioBaseUSD,
                    Orden = c.Orden,
                    Icono = c.Icono,
                    TiposNegocio = moduloTipos
                        .Where(mt => mt.ModuloId == c.ModuloId)
                        .Select(mt => mt.TipoNegocio!.Codigo)
                        .Distinct()
                        .ToList()
                }).ToList(),
                Dependencias = deps.Select(d => new DependenciaModuloDto
                {
                    CodigoModulo = d.Modulo!.Codigo,
                    CodigoRequerido = d.ModuloRequerido!.Codigo,
                    Tipo = d.Tipo,
                    Mensaje = d.Mensaje
                }).ToList(),
                Parametros = parametros
                    .Where(p => p.VisibleCliente)
                    .ToDictionary(p => p.Clave, p => p.Valor),
                TramosDocumentos = tramos.Select(t => new TramoDocumentoDto
                {
                    DesdeDocs = t.DesdeDocs,
                    HastaDocs = t.HastaDocs,
                    CargoUSD = t.CargoUSD,
                    Etiqueta = t.Etiqueta
                }).ToList()
            };
        }

        public async Task<CotizadorPropuestaDto> CalcularAsync(CotizadorCalcularRequest request)
        {
            request ??= new CotizadorCalcularRequest();
            request.CodigosModulos ??= new List<string>();
            request.Usuarios = Math.Max(1, request.Usuarios);
            request.Sucursales = Math.Max(1, request.Sucursales);
            request.DocumentosElectronicosMensuales = Math.Max(0, request.DocumentosElectronicosMensuales);

            var comerciales = await _ctx.ModuloComercial.AsNoTracking()
                .Include(m => m.Modulo)
                .Where(m => m.Activo && m.VisibleCotizador && m.Modulo != null && m.Modulo.Activo)
                .ToListAsync();

            var porCodigo = comerciales
                .Where(c => c.Modulo != null)
                .ToDictionary(c => c.Modulo!.Codigo, c => c, StringComparer.OrdinalIgnoreCase);

            var seleccion = request.CodigosModulos
                .Where(c => !string.IsNullOrWhiteSpace(c) && porCodigo.ContainsKey(c.Trim()))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var idToCodigo = comerciales
                .Where(c => c.Modulo != null)
                .ToDictionary(c => c.ModuloId, c => c.Modulo!.Codigo);

            var deps = await _ctx.ModuloDependencia.AsNoTracking()
                .Where(d => d.Activo)
                .ToListAsync();

            var matches = DependenciaValidator.Evaluar(seleccion, deps, idToCodigo);
            var incluidos = DependenciaValidator.ExpandirIncluidos(seleccion, matches);

            // Auto-incluir REQUIERE en la selección efectiva de precio
            foreach (var req in matches.Where(m => m.Tipo == "REQUIERE"))
                incluidos.Add(req.CodigoRequerido);

            var nombres = comerciales
                .Where(c => c.Modulo != null)
                .ToDictionary(c => c.Modulo!.Codigo, c => c.Modulo!.Nombre, StringComparer.OrdinalIgnoreCase);

            var recomendacionReglas = matches.Select(m => new RecomendacionModuloDto
            {
                Codigo = m.CodigoRequerido,
                Nombre = nombres.GetValueOrDefault(m.CodigoRequerido, m.CodigoRequerido),
                Tipo = m.Tipo,
                Mensaje = m.Mensaje,
                OrigenCodigo = m.CodigoOrigen
            }).ToList();

            var paramsDict = await ObtenerParametrosAsync();
            var usuariosIncluidos = ParseInt(paramsDict, "USUARIOS_INCLUIDOS", 1);
            var precioUsuario = ParseDec(paramsDict, "PRECIO_USUARIO_EXTRA", 0);
            var precioSucursal = ParseDec(paramsDict, "PRECIO_SUCURSAL_EXTRA", 0);
            var sucursalesIncluidas = ParseInt(paramsDict, "SUCURSALES_INCLUIDAS", 1);
            var piso = ParseDec(paramsDict, "PISO_MENSUAL_USD", 30);
            var moneda = paramsDict.GetValueOrDefault("MONEDA", "USD");

            var tramos = await _ctx.CotizadorTramoDocumento.AsNoTracking()
                .Where(t => t.Activo)
                .ToListAsync();

            var modsPrecio = incluidos
                .Where(c => porCodigo.ContainsKey(c))
                .Select(c =>
                {
                    var mc = porCodigo[c];
                    return (
                        Codigo: c,
                        Nombre: mc.Modulo!.Nombre,
                        PrecioBase: mc.PrecioBaseUSD,
                        ParticipaPrecio: mc.ParticipaPrecio);
                })
                .ToList();

            var precio = PrecioCalculator.Calcular(new PrecioCalculoInput
            {
                Modulos = modsPrecio,
                Usuarios = request.Usuarios,
                Sucursales = request.Sucursales,
                UsaFacturacionElectronica = request.UsaFacturacionElectronica,
                DocumentosElectronicosMensuales = request.DocumentosElectronicosMensuales,
                UsuariosIncluidos = usuariosIncluidos,
                PrecioUsuarioExtra = precioUsuario,
                PrecioSucursalExtra = precioSucursal,
                SucursalesIncluidas = sucursalesIncluidas,
                PisoMensualUSD = piso,
                Tramos = tramos
            });

            var propuestaParcial = new CotizadorPropuestaDto
            {
                TipoNegocioCodigo = request.TipoNegocioCodigo,
                ModulosSeleccionados = seleccion,
                ModulosIncluidos = incluidos.OrderBy(x => x).ToList(),
                Desglose = precio.Desglose,
                SubtotalUSD = precio.SubtotalUSD,
                AjustePisoUSD = precio.AjustePisoUSD,
                PrecioMensualUSD = precio.PrecioMensualUSD,
                AplicoPisoMinimo = precio.AplicoPisoMinimo,
                PisoMensualUSD = piso,
                Moneda = moneda,
                UsuariosIncluidos = usuariosIncluidos,
                Usuarios = request.Usuarios,
                Sucursales = request.Sucursales,
                UsaFacturacionElectronica = request.UsaFacturacionElectronica,
                DocumentosElectronicosMensuales = request.DocumentosElectronicosMensuales
            };

            var recomendaciones = await _recomendador.RecomendarAsync(
                request, recomendacionReglas, propuestaParcial);

            propuestaParcial.Recomendaciones = recomendaciones.ToList();
            propuestaParcial.Explicaciones = ExplicacionBuilder.Construir(
                request, matches, precio, nombres);
            propuestaParcial.Resumen = ExplicacionBuilder.Resumen(
                request, incluidos, precio.PrecioMensualUSD);

            return propuestaParcial;
        }

        public async Task<CotizadorGuardarResponse> GuardarAsync(CotizadorGuardarRequest request)
        {
            var propuesta = request.Propuesta ?? await CalcularAsync(request.Seleccion);
            var folio = $"COT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var entity = new Cotizacion
            {
                Folio = folio,
                TipoNegocioCodigo = request.Seleccion?.TipoNegocioCodigo ?? propuesta.TipoNegocioCodigo,
                Usuarios = propuesta.Usuarios,
                Sucursales = propuesta.Sucursales,
                UsaFacturacionElectronica = propuesta.UsaFacturacionElectronica,
                DocumentosElectronicosMensuales = propuesta.DocumentosElectronicosMensuales,
                PrecioMensualUSD = propuesta.PrecioMensualUSD,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    seleccion = request.Seleccion,
                    propuesta
                }, JsonOpts),
                FechaCreacion = DateTime.Now
            };

            foreach (var linea in propuesta.Desglose)
            {
                entity.Detalles.Add(new CotizacionDetalle
                {
                    CodigoModulo = linea.CodigoModulo,
                    Concepto = linea.Concepto,
                    MontoUSD = linea.MontoUSD,
                    TipoLinea = linea.TipoLinea
                });
            }

            _ctx.Cotizacion.Add(entity);
            await _ctx.SaveChangesAsync();

            return new CotizadorGuardarResponse
            {
                Folio = folio,
                CotizacionId = entity.Id,
                Propuesta = propuesta
            };
        }

        public async Task<CotizadorVistaPublicaDto?> ObtenerVistaPublicaAsync(string folio)
        {
            folio = (folio ?? "").Trim();
            if (string.IsNullOrWhiteSpace(folio))
                return null;

            var cot = await _ctx.Cotizacion.AsNoTracking()
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Folio == folio);
            if (cot == null)
                return null;

            CotizadorPropuestaDto? propuesta = null;
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(cot.SnapshotJson) ? "{}" : cot.SnapshotJson);
                if (doc.RootElement.TryGetProperty("propuesta", out var propEl))
                    propuesta = JsonSerializer.Deserialize<CotizadorPropuestaDto>(propEl.GetRawText(), JsonOpts);
            }
            catch
            {
                // snapshot opcional
            }

            var desglose = (propuesta?.Desglose != null && propuesta.Desglose.Count > 0)
                ? propuesta.Desglose
                : cot.Detalles
                    .OrderBy(d => d.Id)
                    .Select(d => new LineaPrecioDto
                    {
                        Concepto = d.Concepto,
                        TipoLinea = d.TipoLinea,
                        CodigoModulo = d.CodigoModulo,
                        MontoUSD = d.MontoUSD
                    })
                    .ToList();

            return new CotizadorVistaPublicaDto
            {
                Folio = cot.Folio,
                FechaCreacion = cot.FechaCreacion,
                TipoNegocioCodigo = cot.TipoNegocioCodigo,
                Usuarios = cot.Usuarios,
                Sucursales = cot.Sucursales,
                UsaFacturacionElectronica = cot.UsaFacturacionElectronica,
                DocumentosElectronicosMensuales = cot.DocumentosElectronicosMensuales,
                PrecioMensualUSD = cot.PrecioMensualUSD,
                Moneda = propuesta?.Moneda ?? "USD",
                Resumen = propuesta?.Resumen ?? $"Cotización Alahia ERP {cot.Folio}",
                Desglose = desglose,
                Explicaciones = propuesta?.Explicaciones ?? new List<string>()
            };
        }

        public async Task EnviarCorreoAsync(CotizadorEnviarCorreoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo))
                throw new Exception("Correo requerido.");
            if (string.IsNullOrWhiteSpace(request.Folio))
                throw new Exception("Folio requerido.");

            var cot = await _ctx.Cotizacion.AsNoTracking()
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Folio == request.Folio)
                ?? throw new Exception("Cotización no encontrada. Guarde la propuesta primero.");

            var publicBase = (_config["Cotizador:PublicBaseUrl"] ?? "").Trim().TrimEnd('/');
            var linkPublico = string.IsNullOrWhiteSpace(publicBase)
                ? ""
                : $"{publicBase}/cotizador/ver/{Uri.EscapeDataString(cot.Folio)}";

            var cuerpo =
                $"Hola {(string.IsNullOrWhiteSpace(request.Nombre) ? "" : request.Nombre.Trim())},\n\n"
                + $"Adjuntamos su cotización Alahia ERP {cot.Folio}.\n"
                + $"Precio mensual estimado: {cot.PrecioMensualUSD:0.##} USD (antes de impuestos).\n"
                + $"Usuarios: {cot.Usuarios} | Sucursales: {cot.Sucursales}\n"
                + (cot.UsaFacturacionElectronica
                    ? $"Documentos e-CF/mes: {cot.DocumentosElectronicosMensuales}\n"
                    : "")
                + (string.IsNullOrEmpty(linkPublico) ? "" : $"\nVer cotización en línea:\n{linkPublico}\n")
                + "\nDesglose:\n"
                + string.Join("\n", cot.Detalles.Select(d => $"- {d.Concepto}: {d.MontoUSD:0.##} USD"))
                + "\n\nEsta estimación es orientativa y no constituye una oferta oficial.\n"
                + "Equipo Alahia ERP / MacroBits";

            await EnviarSmtpAsync(request.Correo.Trim(), $"Cotización Alahia ERP {cot.Folio}", cuerpo);

            _ctx.CotizacionLead.Add(new CotizacionLead
            {
                CotizacionId = cot.Id,
                Tipo = "ENVIAR_CORREO",
                Nombre = request.Nombre,
                Correo = request.Correo.Trim(),
                FechaCreacion = DateTime.Now
            });
            await _ctx.SaveChangesAsync();
        }

        public async Task SolicitarAsync(CotizadorSolicitarRequest request)
        {
            request ??= new CotizadorSolicitarRequest();
            int? cotizacionId = null;

            if (!string.IsNullOrWhiteSpace(request.Folio))
            {
                cotizacionId = await _ctx.Cotizacion.AsNoTracking()
                    .Where(c => c.Folio == request.Folio)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync();
            }
            else if (request.Seleccion != null)
            {
                var guardado = await GuardarAsync(new CotizadorGuardarRequest
                {
                    Seleccion = request.Seleccion
                });
                cotizacionId = guardado.CotizacionId;
            }

            var tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "CONTACTO" : request.Tipo.Trim().ToUpperInvariant();
            _ctx.CotizacionLead.Add(new CotizacionLead
            {
                CotizacionId = cotizacionId,
                Tipo = tipo,
                Nombre = request.Nombre,
                Correo = request.Correo,
                Telefono = request.Telefono,
                Mensaje = request.Mensaje,
                FechaCreacion = DateTime.Now
            });
            await _ctx.SaveChangesAsync();

            await NotificarLeadPorCorreoAsync(request, tipo);
        }

        private async Task NotificarLeadPorCorreoAsync(CotizadorSolicitarRequest request, string tipo)
        {
            try
            {
                var to = _config["Cotizador:LeadNotifyTo"]
                    ?? _config["Smtp:LeadNotifyTo"]
                    ?? "ing.joelarielsosa@gmail.com";

                var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
                var user = _config["Smtp:User"] ?? "ing.joelarielsosa@gmail.com";
                var pass = _config["Smtp:Password"] ?? "wrcsdhewqdgrtula";
                var fromName = _config["Smtp:FromName"] ?? "Alahia ERP Web";
                var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;

                var nombre = string.IsNullOrWhiteSpace(request.Nombre) ? "Sin nombre" : request.Nombre.Trim();
                var correo = string.IsNullOrWhiteSpace(request.Correo) ? "No indicado" : request.Correo.Trim();
                var telefono = string.IsNullOrWhiteSpace(request.Telefono) ? "No indicado" : request.Telefono.Trim();
                var mensaje = string.IsNullOrWhiteSpace(request.Mensaje)
                    ? "Sin mensaje adicional."
                    : request.Mensaje.Trim().Replace("\n", "<br/>");

                var subject = $"Nueva solicitud web ({tipo}) — {nombre}";
                var body = $@"
                    <h2>Nueva solicitud desde Alahia ERP Web</h2>
                    <p><strong>Tipo:</strong> {System.Net.WebUtility.HtmlEncode(tipo)}</p>
                    <p><strong>Contacto:</strong> {System.Net.WebUtility.HtmlEncode(nombre)}</p>
                    <p><strong>Correo:</strong> {System.Net.WebUtility.HtmlEncode(correo)}</p>
                    <p><strong>Teléfono / WhatsApp:</strong> {System.Net.WebUtility.HtmlEncode(telefono)}</p>
                    <hr/>
                    <p><strong>Detalle del formulario:</strong></p>
                    <p style=""white-space:pre-wrap;font-family:Segoe UI,Arial,sans-serif;line-height:1.5"">{mensaje}</p>
                    <hr/>
                    <p style=""color:#64748b;font-size:12px"">Enviado automáticamente desde Cotizador/solicitar</p>
                ";

                await Task.Run(() =>
                    PrinterLibrary.Utility.Send(
                        host,
                        port,
                        true,
                        user,
                        pass,
                        fromName,
                        to,
                        subject,
                        body
                    ));
            }
            catch (Exception ex)
            {
                // El lead ya quedó guardado; registrar el fallo de correo para diagnóstico.
                System.Diagnostics.Debug.WriteLine($"[Cotizador] Fallo al notificar lead por correo: {ex}");
                Console.Error.WriteLine($"[Cotizador] Fallo al notificar lead por correo: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine($"[Cotizador] Inner: {ex.InnerException.Message}");
            }
        }

        private async Task<Dictionary<string, string>> ObtenerParametrosAsync()
        {
            return await _ctx.CotizadorParametro.AsNoTracking()
                .ToDictionaryAsync(p => p.Clave, p => p.Valor);
        }

        private static int ParseInt(Dictionary<string, string> dict, string key, int def)
            => dict.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : def;

        private static decimal ParseDec(Dictionary<string, string> dict, string key, decimal def)
            => dict.TryGetValue(key, out var v) && decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : def;

        private async Task EnviarSmtpAsync(string to, string subject, string body)
        {
            var host = _config["Smtp:Host"] ?? _config["Email:SmtpHost"];
            var user = _config["Smtp:User"] ?? _config["Email:User"];
            var pass = _config["Smtp:Password"] ?? _config["Email:Password"];
            var from = _config["Smtp:From"] ?? _config["Email:From"] ?? user;
            var port = int.TryParse(_config["Smtp:Port"] ?? _config["Email:Port"], out var p) ? p : 587;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new Exception("SMTP no configurado en el servidor. La cotización quedó guardada; contacte a soporte.");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = string.IsNullOrWhiteSpace(user)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(user, pass)
            };

            using var msg = new MailMessage(from!, to, subject, body);
            await client.SendMailAsync(msg);
        }
    }
}
