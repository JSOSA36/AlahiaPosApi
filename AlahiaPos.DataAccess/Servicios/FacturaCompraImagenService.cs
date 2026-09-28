using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Interpreta una foto de factura de proveedor (RD) y propone encabezado + líneas.
    /// Empareja RNC y productos del catálogo. No graba el documento.
    /// </summary>
    public class FacturaCompraImagenService : IFacturaCompraImagenService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const int MaxArchivoBytes = 12 * 1024 * 1024;

        private const string SystemPrompt =
            "Eres un extractor de facturas de compra de República Dominicana. " +
            "Lees la imagen o el texto del comprobante y devuelves SOLO JSON válido, sin markdown. " +
            "Campos: rncEmisor (solo dígitos), nombreEmisor, ncf (solo e-CF: E31/E32/E33/E34/E41; nunca B01/B02 ni NCF tradicional), " +
            "fecha (YYYY-MM-DD), condicionPago (Contado o Credito), fechaVencimiento (YYYY-MM-DD o null), " +
            "subtotal, itbis, total (números), preciosIncluyenItbis (boolean), " +
            "lineas: [{descripcion, codigo, cantidad, precioUnitario, itbis, importe}]. " +
            "precioUnitario es el costo NETO por unidad SIN ITBIS. Si el documento muestra precios con ITBIS, desglósalo. " +
            "itbis de línea es el ITBIS de esa línea (no unitario). " +
            "Ignora logos y totales repetidos. Si un dato no se ve, usa null o 0. " +
            "Si el NCF no empieza por E, ncf debe ser null.";

        private readonly AlahiaPosContext _context;
        private readonly IAiProviderFactory _providers;
        private readonly IAiUsageMonitor _usage;
        private readonly ILogger<FacturaCompraImagenService> _logger;
        private readonly string _tessdataPath;

        public FacturaCompraImagenService(
            AlahiaPosContext context,
            IAiProviderFactory providers,
            IAiUsageMonitor usage,
            ILogger<FacturaCompraImagenService> logger,
            IConfiguration config)
        {
            _context = context;
            _providers = providers;
            _usage = usage;
            _logger = logger;
            var configured = config["Suscripcion:TessdataPath"];
            _tessdataPath = !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim()
                : Path.Combine(AppContext.BaseDirectory, "tessdata");
        }

        public async Task<FacturaCompraImagenResultadoDto> InterpretarAsync(
            FacturaCompraInterpretarRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentException("La solicitud es requerida.");
            if (request.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");

            var paginas = (request.Paginas ?? new List<FacturaCompraInterpretarPagina>())
                .Where(p => p?.Bytes != null && p.Bytes.Length >= 32)
                .Take(3)
                .ToList();
            var texto = Truncate(request.TextoDocumento?.Trim() ?? "", 14000);
            var tieneVision = paginas.Count > 0;
            var tieneTexto = texto.Length >= 40;

            if (!tieneVision && !tieneTexto)
                throw new InvalidOperationException("Adjunte una foto o un PDF de la factura.");

            var peso = paginas.Sum(p => p.Bytes.Length);
            if (peso > MaxArchivoBytes)
                throw new InvalidOperationException("El archivo no puede superar 12 MB.");

            var origen = tieneTexto && !tieneVision ? "PDF" : "archivo";
            if (!tieneTexto && tieneVision)
            {
                texto = OcrLocal(paginas);
                tieneTexto = texto.Length >= 40;
                origen = "OCR";
            }

            var local = FacturaCompraTextoParser.Parse(texto);
            if (FacturaCompraTextoParser.EsUtil(local))
                return await ConstruirResultadoAsync(request, DesdeLocal(local), origen + "-local", ct);

            var provider = await _providers.ResolveForEmpresaAsync(request.IdEmpresa, ct);
            if (provider.IsConfigured)
            {
                var ai = await InterpretarConIaAsync(request, paginas, texto, tieneVision, tieneTexto, ct);
                if (ai != null)
                    return ai;
            }

            if (local.Confianza > 0 && (!string.IsNullOrEmpty(local.Ncf) || !string.IsNullOrEmpty(local.RncEmisor) || local.Lineas.Count > 0))
                return await ConstruirResultadoAsync(request, DesdeLocal(local), origen + "-parcial", ct);

            throw new InvalidOperationException(
                "No se pudo leer NCF, RNC o líneas. Adjunte el PDF digital del e-CF o una foto nítida y de frente.");
        }

        private async Task<FacturaCompraImagenResultadoDto?> InterpretarConIaAsync(
            FacturaCompraInterpretarRequest request,
            List<FacturaCompraInterpretarPagina> paginas,
            string texto,
            bool tieneVision,
            bool tieneTexto,
            CancellationToken ct)
        {
            var provider = await _providers.ResolveForEmpresaAsync(request.IdEmpresa, ct);
            if (!provider.IsConfigured)
                return null;

            var completionReq = new AiCompletionRequest
            {
                SystemPrompt = SystemPrompt,
                Temperature = 0.1,
                MaxTokens = 3000
            };

            if (tieneVision)
            {
                completionReq.UserPrompt = tieneTexto
                    ? "Extrae la factura de compra de estas páginas. Texto de apoyo del PDF:\n" + texto + "\nResponde únicamente el JSON."
                    : "Extrae la factura de compra de esta imagen. Responde únicamente el JSON.";
                completionReq.ImageBase64 = Convert.ToBase64String(paginas[0].Bytes);
                completionReq.ImageMimeType = NormalizarMime(paginas[0].Mime);
                if (paginas.Count > 1)
                {
                    completionReq.ExtraImages = paginas.Skip(1).Select(p => new AiImageContent
                    {
                        Base64 = Convert.ToBase64String(p.Bytes),
                        MimeType = NormalizarMime(p.Mime)
                    }).ToList();
                }
            }
            else
            {
                completionReq.UserPrompt =
                    "Texto extraído del PDF de la factura de compra:\n\n" + texto + "\n\nResponde únicamente el JSON.";
            }

            var completion = await provider.CompleteAsync(completionReq, ct);

            await _usage.TrackAsync(new AiUsageLogEntry
            {
                IdEmpresa = request.IdEmpresa,
                IdUsuario = request.IdUsuario,
                Provider = completion.Provider,
                Model = completion.Model,
                Feature = "compras_imagen",
                Intent = tieneVision ? "factura_compra_ocr" : "factura_compra_pdf",
                PromptTokens = completion.PromptTokens,
                CompletionTokens = completion.CompletionTokens,
                EstimatedCostUsd = completion.EstimatedCostUsd,
                Success = completion.Success,
                Error = completion.Error
            }, ct);

            if (!completion.Success || string.IsNullOrWhiteSpace(completion.Text))
                return null;

            var extracted = ParsearExtraccion(completion.Text);
            if (extracted == null)
            {
                _logger.LogWarning("JSON de factura ilegible. Preview: {Preview}", Truncate(completion.Text, 300));
                return null;
            }

            return await ConstruirResultadoAsync(request, extracted, completion.Provider, ct);
        }

        private async Task<FacturaCompraImagenResultadoDto> ConstruirResultadoAsync(
            FacturaCompraInterpretarRequest request,
            ExtraccionAi extracted,
            string provider,
            CancellationToken ct)
        {
            var rnc = SoloDigitos(extracted.RncEmisor);
            var proveedor = await BuscarProveedorAsync(request.IdEmpresa, rnc, extracted.NombreEmisor, ct);

            var catalogo = await _context.Productos
                .AsNoTracking()
                .Where(p => p.IdEmpresa == request.IdEmpresa && p.IsActivo)
                .Select(p => new CatalogoItem
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre ?? "",
                    CodigoBarra = p.CodigoBarra ?? "",
                    TipoComportamiento = p.TipoComportamiento
                })
                .ToListAsync(ct);

            var lineas = new List<FacturaCompraImagenLineaDto>();
            foreach (var raw in extracted.Lineas ?? new List<LineaAi>())
            {
                var linea = MapearLinea(raw, extracted.PreciosIncluyenItbis);
                EmparejarProducto(linea, catalogo);
                if (!linea.Emparejado)
                    await AsegurarProductoDesdeLineaAsync(request.IdEmpresa, proveedor?.IdProveedor ?? 0, linea, catalogo, ct);
                lineas.Add(linea);
            }

            var condicion = string.Equals(extracted.CondicionPago, "Credito", StringComparison.OrdinalIgnoreCase)
                ? "Credito"
                : "Contado";

            var creados = lineas.Count(l => l.MotivoEmparejado == "Alta desde factura");
            var mensaje = proveedor != null
                ? "Factura leída. Revise proveedor, NCF y líneas antes de guardar."
                : "Factura leída. El proveedor no está en el catálogo: selecciónelo o créelo, luego revise las líneas.";
            if (creados > 0)
            {
                mensaje = creados == 1
                    ? "Factura leída. Se creó 1 producto en el catálogo para integrar la línea. Revise y guarde."
                    : $"Factura leída. Se crearon {creados} productos en el catálogo para integrar las líneas. Revise y guarde.";
            }

            return new FacturaCompraImagenResultadoDto
            {
                Success = true,
                Message = mensaje,
                RncEmisor = rnc,
                NombreEmisor = extracted.NombreEmisor?.Trim(),
                IdProveedor = proveedor?.IdProveedor,
                ProveedorNombre = proveedor?.NombreComercial,
                ProveedorEncontrado = proveedor != null,
                Ncf = NormalizarNcf(extracted.Ncf),
                Fecha = ParseFecha(extracted.Fecha),
                CondicionPago = condicion,
                FechaVencimiento = ParseFecha(extracted.FechaVencimiento),
                Subtotal = extracted.Subtotal,
                Itbis = extracted.Itbis,
                Total = extracted.Total,
                PreciosIncluyenItbis = extracted.PreciosIncluyenItbis,
                Lineas = lineas,
                LineasEmparejadas = lineas.Count(l => l.Emparejado),
                LineasSinProducto = lineas.Count(l => !l.Emparejado),
                Provider = provider
            };
        }

        private static ExtraccionAi DesdeLocal(FacturaCompraCamposExtraidos local) => new()
        {
            RncEmisor = local.RncEmisor,
            NombreEmisor = local.NombreEmisor,
            Ncf = local.Ncf,
            Fecha = local.Fecha,
            CondicionPago = local.CondicionPago,
            FechaVencimiento = local.FechaVencimiento,
            Subtotal = local.Subtotal,
            Itbis = local.Itbis,
            Total = local.Total,
            PreciosIncluyenItbis = local.PreciosIncluyenItbis,
            Lineas = local.Lineas.Select(l => new LineaAi
            {
                Descripcion = l.Descripcion,
                Codigo = l.Codigo,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Itbis = l.Itbis,
                Importe = l.Importe
            }).ToList()
        };

        private string OcrLocal(List<FacturaCompraInterpretarPagina> paginas)
        {
            var trained = Path.Combine(_tessdataPath, "eng.traineddata");
            if (!Directory.Exists(_tessdataPath) || !File.Exists(trained))
            {
                _logger.LogWarning("tessdata no encontrado en {Path}. OCR local omitido.", _tessdataPath);
                return "";
            }

            var sb = new StringBuilder();
            try
            {
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                foreach (var pagina in paginas)
                {
                    using var img = Pix.LoadFromMemory(pagina.Bytes);
                    using var page = engine.Process(img);
                    sb.AppendLine(page.GetText() ?? "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR local de factura de compra falló.");
                return "";
            }

            return Truncate(sb.ToString().Trim(), 14000);
        }

        private async Task<Proveedores?> BuscarProveedorAsync(
            int idEmpresa,
            string rnc,
            string? nombre,
            CancellationToken ct)
        {
            var proveedores = await _context.Proveedores
                .AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.IsActivo)
                .ToListAsync(ct);

            if (!string.IsNullOrEmpty(rnc))
            {
                var porRnc = proveedores.FirstOrDefault(p => SoloDigitos(p.RNC) == rnc);
                if (porRnc != null)
                    return porRnc;
            }

            var nombreN = Normalizar(nombre);
            if (nombreN.Length < 4)
                return null;

            return proveedores.FirstOrDefault(p =>
            {
                var n = Normalizar(p.NombreComercial);
                return n.Length >= 4 && (n.Contains(nombreN) || nombreN.Contains(n));
            });
        }

        private static FacturaCompraImagenLineaDto MapearLinea(LineaAi raw, bool preciosConItbis)
        {
            var cantidad = raw.Cantidad > 0 ? raw.Cantidad : 1;
            var precio = raw.PrecioUnitario;
            var itbis = raw.Itbis;
            if (preciosConItbis && precio > 0 && itbis <= 0)
            {
                var neto = Math.Round(precio / 1.18m, 2, MidpointRounding.AwayFromZero);
                itbis = Math.Round((precio - neto) * cantidad, 2, MidpointRounding.AwayFromZero);
                precio = neto;
            }

            var importe = raw.Importe > 0
                ? raw.Importe
                : Math.Round((cantidad * precio) + itbis, 2, MidpointRounding.AwayFromZero);

            return new FacturaCompraImagenLineaDto
            {
                Descripcion = (raw.Descripcion ?? "").Trim(),
                Codigo = string.IsNullOrWhiteSpace(raw.Codigo) ? null : raw.Codigo.Trim(),
                Cantidad = cantidad,
                PrecioUnitario = precio,
                Itbis = itbis,
                Importe = importe
            };
        }

        private static void EmparejarProducto(FacturaCompraImagenLineaDto linea, List<CatalogoItem> catalogo)
        {
            if (catalogo.Count == 0)
                return;

            var codigo = (linea.Codigo ?? "").Trim();
            if (codigo.Length >= 4)
            {
                var porCodigo = catalogo.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.CodigoBarra)
                    && string.Equals(p.CodigoBarra.Trim(), codigo, StringComparison.OrdinalIgnoreCase));
                if (porCodigo != null)
                {
                    Asignar(linea, porCodigo, "Código de barra");
                    return;
                }
            }

            var desc = Normalizar(linea.Descripcion);
            if (desc.Length < 3)
                return;

            CatalogoItem? best = null;
            var bestScore = 0;
            foreach (var p in catalogo)
            {
                var score = ScoreNombre(desc, Normalizar(p.Nombre));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            if (best != null && bestScore >= 70)
                Asignar(linea, best, $"Nombre ({bestScore}%)");
        }

        private async Task AsegurarProductoDesdeLineaAsync(
            int idEmpresa,
            int idProveedor,
            FacturaCompraImagenLineaDto linea,
            List<CatalogoItem> catalogo,
            CancellationToken ct)
        {
            var nombre = (linea.Descripcion ?? "").Trim();
            if (nombre.Length < 3)
                return;

            var clave = Normalizar(nombre);
            var ya = catalogo.FirstOrDefault(p => Normalizar(p.Nombre) == clave);
            if (ya != null)
            {
                Asignar(linea, ya, "Catálogo");
                return;
            }

            var idAlmacen = await _context.Almacenes.AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activo)
                .OrderByDescending(a => a.EsPrincipal)
                .Select(a => (int?)a.IdAlmacen)
                .FirstOrDefaultAsync(ct);

            var idCategoria = await _context.Categorias.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .OrderBy(c => c.IdCategoria)
                .Select(c => (int?)c.IdCategoria)
                .FirstOrDefaultAsync(ct);

            var costo = linea.PrecioUnitario > 0 ? linea.PrecioUnitario : 0;
            var venta = Math.Round(costo <= 0 ? 0 : costo * 1.30m, 2, MidpointRounding.AwayFromZero);

            var producto = new Productos
            {
                IdEmpresa = idEmpresa,
                Nombre = nombre,
                Descripcion = nombre,
                CodigoBarra = string.IsNullOrWhiteSpace(linea.Codigo) ? "" : linea.Codigo.Trim(),
                PrecioCompra = costo,
                PrecioVenta = venta,
                Precio1 = venta,
                Precio2 = venta,
                Precio3 = venta,
                PrecioDolar = 0,
                Descuento = 0,
                PorcientoDescuento = 0,
                PorcientoGanancia = 30,
                Ganancia = Math.Round(venta - costo, 2, MidpointRounding.AwayFromZero),
                Rentado = 0,
                Cantidad = 0,
                Stock = 0,
                TipoComportamiento = TipoComportamientoConstantes.Inventario,
                TipoOperacion = "AMBAS",
                TipoProducto = "Producto",
                SeCompra = true,
                SeVende = true,
                SeAlquila = false,
                ControlarStock = true,
                IsActivo = true,
                Itbis = linea.Itbis > 0,
                EsServicio = false,
                IdProveedor = idProveedor > 0 ? idProveedor : 0,
                IdAlmacen = idAlmacen,
                IdCategoria = idCategoria,
                IdUnidadMedida = 1,
                FechaInseccion = DateTime.Now,
                DisponibleEnCitas = false,
                DuracionServicio = 0,
                Nota = "Alta automática desde factura de compra"
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync(ct);

            var item = new CatalogoItem
            {
                IdProducto = producto.IdProducto,
                Nombre = producto.Nombre ?? nombre,
                CodigoBarra = producto.CodigoBarra ?? "",
                TipoComportamiento = producto.TipoComportamiento
            };
            catalogo.Add(item);
            Asignar(linea, item, "Alta desde factura");
        }

        private static void Asignar(FacturaCompraImagenLineaDto linea, CatalogoItem p, string motivo)
        {
            linea.IdProducto = p.IdProducto;
            linea.NombreProducto = p.Nombre;
            linea.TipoComportamiento = p.TipoComportamiento;
            linea.Emparejado = true;
            linea.MotivoEmparejado = motivo;
        }

        private static int ScoreNombre(string factura, string catalogo)
        {
            if (string.IsNullOrEmpty(factura) || string.IsNullOrEmpty(catalogo))
                return 0;
            if (factura == catalogo)
                return 100;
            if (catalogo.StartsWith(factura) || factura.StartsWith(catalogo))
                return factura.Length >= 8 && catalogo.Length >= 8 ? 88 : 40;
            if (factura.Length >= 8 && catalogo.Length >= 8 && (catalogo.Contains(factura) || factura.Contains(catalogo)))
                return 78;

            var tokensF = factura.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokensC = catalogo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokensF.Length == 0 || tokensC.Length == 0)
                return 0;

            var hits = tokensF.Count(t => TokenCoincide(t, tokensC));
            if (hits == 0)
                return 0;
            return (int)Math.Round(100.0 * hits / Math.Max(tokensF.Length, tokensC.Length));
        }

        private static bool TokenCoincide(string token, string[] catalogo)
        {
            if (token.Length < 4)
                return false;
            foreach (var c in catalogo)
            {
                if (c.Length < 4)
                    continue;
                if (c == token)
                    return true;
                if (c.StartsWith(token) || token.StartsWith(c))
                    return true;
            }
            return false;
        }

        private static ExtraccionAi? ParsearExtraccion(string text)
        {
            try
            {
                return JsonSerializer.Deserialize<ExtraccionAi>(ExtraerJson(text), JsonOpts);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string ExtraerJson(string text)
        {
            var t = text.Trim();
            if (t.StartsWith("```"))
            {
                var firstNl = t.IndexOf('\n');
                if (firstNl > 0)
                    t = t[(firstNl + 1)..];
                var fence = t.LastIndexOf("```", StringComparison.Ordinal);
                if (fence >= 0)
                    t = t[..fence];
                t = t.Trim();
            }

            var start = t.IndexOf('{');
            var end = t.LastIndexOf('}');
            if (start >= 0 && end > start)
                return t[start..(end + 1)];
            return t;
        }

        private static DateTime? ParseFecha(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.Date;
            if (DateTime.TryParse(value, new CultureInfo("es-DO"), DateTimeStyles.None, out d))
                return d.Date;
            return null;
        }

        private static string? NormalizarNcf(string? ncf)
        {
            if (string.IsNullOrWhiteSpace(ncf))
                return null;
            var clean = Regex.Replace(ncf.Trim().ToUpperInvariant(), @"\s+", "");
            if (clean.Length < 3 || !clean.StartsWith("E", StringComparison.Ordinal))
                return null;
            return clean;
        }

        private static string SoloDigitos(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static string Normalizar(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            var formD = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var c in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static string NormalizarMime(string? contentType)
        {
            var mime = (contentType ?? "").Trim().ToLowerInvariant();
            return mime switch
            {
                "image/png" => "image/png",
                "image/webp" => "image/webp",
                "image/gif" => "image/gif",
                _ => "image/jpeg"
            };
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

        private sealed class CatalogoItem
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = "";
            public string CodigoBarra { get; set; } = "";
            public string? TipoComportamiento { get; set; }
        }

        private sealed class ExtraccionAi
        {
            public string? RncEmisor { get; set; }
            public string? NombreEmisor { get; set; }
            public string? Ncf { get; set; }
            public string? Fecha { get; set; }
            public string? CondicionPago { get; set; }
            public string? FechaVencimiento { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Itbis { get; set; }
            public decimal Total { get; set; }
            public bool PreciosIncluyenItbis { get; set; }
            public List<LineaAi>? Lineas { get; set; }
        }

        private sealed class LineaAi
        {
            public string? Descripcion { get; set; }
            public string? Codigo { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Itbis { get; set; }
            public decimal Importe { get; set; }
        }
    }
}
