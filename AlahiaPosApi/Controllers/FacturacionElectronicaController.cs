using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturacionElectronicaController : ControllerBase
    {
        private readonly IFacturacionElectronicaService _feService;
        private readonly ISecuenciaEcfService _secuencias;
        private readonly IFiscalGateway _gateway;
        private readonly IEmpresaFiscalGatewayResolver _gatewayResolver;
        private readonly FiscalGatewayOptions _gatewayDefaults;
        private readonly AlahiaPosContext _ctx;

        public FacturacionElectronicaController(
            IFacturacionElectronicaService feService,
            ISecuenciaEcfService secuencias,
            IFiscalGateway gateway,
            IEmpresaFiscalGatewayResolver gatewayResolver,
            IOptions<FiscalGatewayOptions> gatewayDefaults,
            AlahiaPosContext ctx)
        {
            _feService = feService;
            _secuencias = secuencias;
            _gateway = gateway;
            _gatewayResolver = gatewayResolver;
            _gatewayDefaults = gatewayDefaults.Value;
            _ctx = ctx;
        }

        // ============================================================
        // Emisión transversal
        // ============================================================

        [HttpPost("emitir")]
        public async Task<IActionResult> Emitir([FromBody] EmisionEcfRequest request)
        {
            if (request.TipoEcfDgii == 0 || request.IdOrigen == 0)
                return BadRequest("TipoEcfDgii e IdOrigen son requeridos");

            var resultado = await _feService.EmitirDocumentoAsync(request);

            if (!resultado.Exitoso)
                return UnprocessableEntity(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Emisión síncrona: reserva e-NCF, envía al Gateway y retorna QR + TrackId.
        /// Uso principal: POS preview inmediato después de facturar.
        /// </summary>
        [HttpPost("emitir-enviar")]
        public async Task<IActionResult> EmitirYEnviar([FromBody] EmisionEcfRequest request)
        {
            if (request.TipoEcfDgii == 0 || request.IdOrigen == 0)
                return BadRequest("TipoEcfDgii e IdOrigen son requeridos");

            var resultado = await _feService.EmitirYEnviarAsync(request);

            if (!resultado.Exitoso)
                return UnprocessableEntity(resultado);

            return Ok(resultado);
        }

        // ============================================================
        // Secuencias e-CF disponibles (para selectores POS, NC, etc.)
        // ============================================================

        [HttpGet("secuencias-disponibles/{idEmpresa}")]
        public async Task<IActionResult> SecuenciasDisponibles(int idEmpresa)
        {
            var lista = await _feService.ObtenerSecuenciasDisponiblesAsync(idEmpresa);
            return Ok(lista);
        }

        // ============================================================
        // CRUD Secuencias
        // ============================================================

        [HttpGet("secuencias/{idEmpresa}")]
        public async Task<IActionResult> GetSecuencias(int idEmpresa)
        {
            var lista = await _secuencias.GetAllAsync(idEmpresa);
            return Ok(lista);
        }

        [HttpGet("secuencia/{id}")]
        public async Task<IActionResult> GetSecuencia(int id)
        {
            var dto = await _secuencias.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost("secuencias")]
        public async Task<IActionResult> CreateSecuencia([FromBody] SecuenciaEcfCreateDto dto)
        {
            if (dto.IdEmpresa == 0 || dto.TipoEcfDgii == 0 || dto.SecuenciaFinal == 0)
                return BadRequest("IdEmpresa, TipoEcfDgii y SecuenciaFinal son requeridos");

            var created = await _secuencias.CreateAsync(dto);
            return CreatedAtAction(nameof(GetSecuencia),
                new { id = created.IdSecuencia }, created);
        }

        [HttpPut("secuencias/{id}")]
        public async Task<IActionResult> UpdateSecuencia(int id, [FromBody] SecuenciaEcfUpdateDto dto)
        {
            await _secuencias.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpPatch("secuencias/{id}/desactivar")]
        public async Task<IActionResult> DesactivarSecuencia(int id)
        {
            await _secuencias.DesactivarAsync(id);
            return NoContent();
        }

        // ============================================================
        // Validación de disponibilidad
        // ============================================================

        [HttpGet("validar/{idEmpresa}/{tipoEcf}")]
        public async Task<IActionResult> Validar(int idEmpresa, int tipoEcf)
        {
            var alerta = await _secuencias.ValidarDisponibilidadAsync(idEmpresa, tipoEcf);
            return Ok(alerta);
        }

        [HttpGet("peek/{idEmpresa}/{tipoEcf}")]
        public async Task<IActionResult> Peek(int idEmpresa, int tipoEcf)
        {
            var siguiente = await _secuencias.PeekSiguienteAsync(idEmpresa, tipoEcf);
            if (siguiente == null) return NotFound("No hay secuencia disponible");
            return Ok(new { encf = siguiente });
        }

        // ============================================================
        // Proveedor fiscal (por empresa)
        // ============================================================

        [HttpGet("proveedor/{idEmpresa}")]
        public async Task<IActionResult> GetProveedor(int idEmpresa)
        {
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            var modo = ProveedorFiscalHelper.Normalize(empresa.ProveedorFE);
            return Ok(BuildProveedorDto(empresa, modo));
        }

        [HttpPut("proveedor/{idEmpresa}")]
        public async Task<IActionResult> PutProveedor(int idEmpresa, [FromBody] ProveedorFeRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Proveedor))
                return BadRequest("proveedor es requerido (DGII_DIRECTO | PROVEEDOR_EXTERNO)");

            if (!ProveedorFiscalHelper.EsValido(body.Proveedor))
                return BadRequest("Proveedor inválido. Use DGII_DIRECTO o PROVEEDOR_EXTERNO.");

            var modo = ProveedorFiscalHelper.Normalize(body.Proveedor);
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            if (modo == ProveedorFiscalHelper.ProveedorExterno)
            {
                var url = (body.BaseUrl ?? empresa.ProveedorFE_BaseUrl ?? "").Trim();
                if (string.IsNullOrWhiteSpace(url))
                    return BadRequest("baseUrl es requerido para PROVEEDOR_EXTERNO");
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return BadRequest("baseUrl debe ser una URL http(s) absoluta");

                empresa.ProveedorFE = modo;
                empresa.ProveedorFE_Nombre = string.IsNullOrWhiteSpace(body.Nombre)
                    ? empresa.ProveedorFE_Nombre
                    : body.Nombre.Trim();
                empresa.ProveedorFE_BaseUrl = url.TrimEnd('/');
                empresa.ProveedorFE_Usuario = body.Usuario?.Trim();

                if (!string.IsNullOrWhiteSpace(body.ApiKey))
                    empresa.ProveedorFE_ApiKey = body.ApiKey.Trim();
                else if (body.ClearApiKey == true)
                    empresa.ProveedorFE_ApiKey = null;

                if (!string.IsNullOrWhiteSpace(body.Password))
                    empresa.ProveedorFE_Password = body.Password;
                else if (body.ClearPassword == true)
                    empresa.ProveedorFE_Password = null;
            }
            else
            {
                empresa.ProveedorFE = ProveedorFiscalHelper.DgiiDirecto;
                // Credenciales externas se conservan por si vuelven a modo externo.
            }

            await _ctx.SaveChangesAsync();
            return Ok(BuildProveedorDto(empresa, modo));
        }

        private object BuildProveedorDto(Empresas empresa, string modo)
        {
            var esExterno = ProveedorFiscalHelper.EsExterno(modo);
            var effectiveUrl = esExterno
                ? (empresa.ProveedorFE_BaseUrl ?? "")
                : (_gatewayDefaults.BaseUrl ?? "");
            return new
            {
                proveedor = modo,
                etiqueta = ProveedorFiscalHelper.Etiqueta(modo),
                nombre = esExterno ? empresa.ProveedorFE_Nombre : "Alahia.eCF.Api",
                baseUrl = esExterno ? empresa.ProveedorFE_BaseUrl : _gatewayDefaults.BaseUrl,
                usuario = esExterno ? empresa.ProveedorFE_Usuario : null,
                apiKeyConfigurado = esExterno
                    ? !string.IsNullOrWhiteSpace(empresa.ProveedorFE_ApiKey)
                    : !string.IsNullOrWhiteSpace(_gatewayDefaults.ApiKey),
                passwordConfigurado = esExterno && !string.IsNullOrWhiteSpace(empresa.ProveedorFE_Password),
                endpointEfectivo = string.IsNullOrWhiteSpace(effectiveUrl)
                    ? null
                    : effectiveUrl.TrimEnd('/'),
                contrato = "api/Receipt (X-Api-Key y/o Basic Auth)",
                ambienteDgiiAplicable = !esExterno
            };
        }

        // ============================================================
        // Certificado digital (modo DGII_DIRECTO)
        // ============================================================

        [HttpGet("certificado/{idEmpresa}")]
        public async Task<IActionResult> GetCertificado(int idEmpresa)
        {
            var existeEmpresa = await _ctx.Empresas.AsNoTracking()
                .AnyAsync(e => e.IdEmpresa == idEmpresa);
            if (!existeEmpresa) return NotFound("Empresa no encontrada");

            var cert = await _ctx.CertificadosDigitales.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => new
                {
                    c.IdCertificado,
                    c.NombreArchivo,
                    c.FechaExpiracion,
                    c.FechaCreacion,
                    c.Ambiente,
                    tieneBytes = c.ArchivoBytes != null && c.ArchivoBytes.Length > 0,
                    tieneRuta = c.RutaArchivo != null && c.RutaArchivo != ""
                })
                .FirstOrDefaultAsync();

            if (cert == null)
            {
                return Ok(new
                {
                    configurado = false,
                    mensaje = "No hay certificado digital activo. Suba el .p12/.pfx y la contraseña."
                });
            }

            return Ok(new
            {
                configurado = true,
                idCertificado = cert.IdCertificado,
                nombreArchivo = cert.NombreArchivo,
                fechaExpiracion = cert.FechaExpiracion,
                fechaCreacion = cert.FechaCreacion,
                ambiente = cert.Ambiente,
                vencido = cert.FechaExpiracion < DateTime.Now,
                usable = cert.tieneBytes || cert.tieneRuta
            });
        }

        [HttpPost("certificado/{idEmpresa}")]
        [RequestSizeLimit(15_000_000)]
        public async Task<IActionResult> UploadCertificado(
            int idEmpresa,
            IFormFile archivo,
            [FromForm] string password,
            [FromForm] string? ambiente = null)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("archivo .p12/.pfx es requerido");
            if (string.IsNullOrWhiteSpace(password))
                return BadRequest("password del certificado es requerido");

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            var name = (archivo.FileName ?? "certificado.p12").Trim();
            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (ext is not ".p12" and not ".pfx")
                return BadRequest("Solo se aceptan archivos .p12 o .pfx");

            byte[] bytes;
            await using (var ms = new MemoryStream())
            {
                await archivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            DateTime fechaExp;
            string? subject;
            string? thumbprint;
            try
            {
                using var x509 = new X509Certificate2(
                    bytes,
                    password,
                    X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
                if (!x509.HasPrivateKey)
                    return BadRequest("El certificado no contiene llave privada. Use el .p12/.pfx de firma.");
                fechaExp = x509.NotAfter;
                subject = x509.Subject;
                thumbprint = x509.Thumbprint;
            }
            catch (Exception ex)
            {
                return BadRequest($"No se pudo abrir el certificado con esa contraseña: {ex.Message}");
            }

            var activos = await _ctx.CertificadosDigitales.AsTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .ToListAsync();
            foreach (var c in activos)
                c.Activo = false;

            var ambienteNorm = string.IsNullOrWhiteSpace(ambiente)
                ? DgiiAmbienteHelper.EtiquetaSecuencia(empresa.AmbienteFE)
                : ambiente.Trim().ToUpperInvariant();

            var nuevo = new CertificadoDigital
            {
                IdEmpresa = idEmpresa,
                NombreArchivo = Path.GetFileName(name),
                ArchivoBytes = bytes,
                PasswordEncriptado = password,
                FechaExpiracion = fechaExp,
                Activo = true,
                Ambiente = ambienteNorm,
                FechaCreacion = DateTime.Now
            };
            _ctx.CertificadosDigitales.Add(nuevo);
            await _ctx.SaveChangesAsync();

            return Ok(new
            {
                configurado = true,
                idCertificado = nuevo.IdCertificado,
                nombreArchivo = nuevo.NombreArchivo,
                fechaExpiracion = nuevo.FechaExpiracion,
                fechaCreacion = nuevo.FechaCreacion,
                ambiente = nuevo.Ambiente,
                subject,
                thumbprint,
                vencido = fechaExp < DateTime.Now,
                usable = true,
                mensaje = "Certificado digital guardado y activado para esta empresa."
            });
        }

        // ============================================================
        // Ambiente DGII (por empresa)
        // ============================================================

        [HttpGet("ambiente/{idEmpresa}")]
        public async Task<IActionResult> GetAmbiente(int idEmpresa)
        {
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            var ambiente = DgiiAmbienteHelper.Normalize(empresa.AmbienteFE);
            var settings = new DgiiDirectoSettings { Ambiente = ambiente };
            return Ok(new
            {
                ambiente,
                etiqueta = DgiiAmbienteHelper.EtiquetaUi(ambiente),
                etiquetaSecuencia = DgiiAmbienteHelper.EtiquetaSecuencia(ambiente),
                urls = DgiiAmbienteHelper.BuildUrlsDto(settings)
            });
        }

        [HttpPut("ambiente/{idEmpresa}")]
        public async Task<IActionResult> PutAmbiente(int idEmpresa, [FromBody] AmbienteFeRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Ambiente))
                return BadRequest("ambiente es requerido (testecf | certecf | ecf)");

            var ambiente = DgiiAmbienteHelper.Normalize(body.Ambiente);
            if (!DgiiAmbienteHelper.EsValido(body.Ambiente))
                return BadRequest("Ambiente inválido. Use testecf, certecf o ecf.");

            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            empresa.AmbienteFE = ambiente;

            var etiquetaSeq = DgiiAmbienteHelper.EtiquetaSecuencia(ambiente);
            var secuencias = await _ctx.SecuenciasECF.AsTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.Activo)
                .ToListAsync();
            foreach (var s in secuencias)
                s.Ambiente = etiquetaSeq;

            await _ctx.SaveChangesAsync();

            var settings = new DgiiDirectoSettings { Ambiente = ambiente };
            return Ok(new
            {
                ambiente,
                etiqueta = DgiiAmbienteHelper.EtiquetaUi(ambiente),
                etiquetaSecuencia = etiquetaSeq,
                secuenciasActualizadas = secuencias.Count,
                urls = DgiiAmbienteHelper.BuildUrlsDto(settings)
            });
        }

        // ============================================================
        // Gateway Fiscal
        // ============================================================

        [HttpGet("gateway/health")]
        public async Task<IActionResult> GatewayHealth([FromQuery] int? idEmpresa = null)
        {
            if (idEmpresa is > 0)
            {
                var okEmpresa = await _gatewayResolver.VerificarConexionAsync(idEmpresa.Value);
                var emp = await _ctx.Empresas.AsNoTracking()
                    .Where(e => e.IdEmpresa == idEmpresa.Value)
                    .Select(e => new { e.ProveedorFE, e.AmbienteFE })
                    .FirstOrDefaultAsync();
                return Ok(new
                {
                    conectado = okEmpresa,
                    proveedor = ProveedorFiscalHelper.Normalize(emp?.ProveedorFE),
                    ambiente = DgiiAmbienteHelper.Normalize(emp?.AmbienteFE)
                });
            }

            var ok = await _gateway.VerificarConexionAsync();
            return Ok(new { conectado = ok, proveedor = ProveedorFiscalHelper.DgiiDirecto });
        }

        [HttpGet("gateway/consultar/{trackId}")]
        public async Task<IActionResult> ConsultarEstado(string trackId)
        {
            var resultado = await _gateway.ConsultarEstadoAsync(trackId);

            // Persistir estado final en historial ERP (sin reenviar)
            if (!string.IsNullOrWhiteSpace(trackId) && !string.IsNullOrWhiteSpace(resultado.Estado))
            {
                var ecf = await _ctx.ECFEncabezados
                    .AsTracking()
                    .FirstOrDefaultAsync(e => e.TrackId == trackId);
                if (ecf != null && !string.Equals(ecf.EstadoDGII, resultado.Estado, StringComparison.OrdinalIgnoreCase))
                {
                    ecf.EstadoDGII = resultado.Estado;
                    if (resultado.EsAceptado || resultado.Estado is "AceptadoCondicional")
                        ecf.EstadoDocumento = EstadoDocumentoElectronico.Aceptado;
                    else if (resultado.EsRechazado)
                        ecf.EstadoDocumento = EstadoDocumentoElectronico.Rechazado;
                    ecf.FechaRespuesta = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(resultado.SecurityCode))
                        ecf.SecurityCode = resultado.SecurityCode;
                    if (!string.IsNullOrWhiteSpace(resultado.UrlQR))
                        ecf.UrlQR = resultado.UrlQR;
                    await _ctx.SaveChangesAsync();
                }
            }

            return Ok(resultado);
        }

        // ============================================================
        // Health (alias)
        // ============================================================

        [HttpGet("health")]
        public async Task<IActionResult> Health([FromQuery] int? idEmpresa = null)
        {
            if (idEmpresa is > 0)
            {
                var okEmpresa = await _gatewayResolver.VerificarConexionAsync(idEmpresa.Value);
                var emp = await _ctx.Empresas.AsNoTracking()
                    .Where(e => e.IdEmpresa == idEmpresa.Value)
                    .Select(e => new { e.ProveedorFE, e.AmbienteFE })
                    .FirstOrDefaultAsync();
                return Ok(new
                {
                    conectado = okEmpresa,
                    proveedor = ProveedorFiscalHelper.Normalize(emp?.ProveedorFE),
                    ambiente = DgiiAmbienteHelper.Normalize(emp?.AmbienteFE)
                });
            }

            var ok = await _gateway.VerificarConexionAsync();
            return Ok(new { conectado = ok, proveedor = ProveedorFiscalHelper.DgiiDirecto });
        }

        // ============================================================
        // Historial de envíos
        // ============================================================

        [HttpGet("historial/{idEmpresa}")]
        public async Task<IActionResult> Historial(
            int idEmpresa,
            [FromQuery] string? desde = null,
            [FromQuery] string? hasta = null,
            [FromQuery] int? tipo = null,
            [FromQuery] string? estado = null)
        {
            var query = _ctx.ECFEncabezados
                .AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa);

            if (DateTime.TryParse(desde, out var dDesde))
                query = query.Where(e => e.FechaEmision >= dDesde);

            if (DateTime.TryParse(hasta, out var dHasta))
                query = query.Where(e => e.FechaEmision <= dHasta.Date.AddDays(1));

            if (tipo.HasValue)
                query = query.Where(e => e.TipoECF == tipo.Value.ToString());

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(e => e.EstadoDGII == estado);

            var items = await query
                .OrderByDescending(e => e.FechaEmision)
                .Take(500)
                .Select(e => new
                {
                    idEcf = e.IdECF,
                    encf = e.ENCF,
                    tipoEcfDgii = e.TipoECF,
                    estadoDocumento = e.EstadoDocumento,
                    estadoDGII = e.EstadoDGII,
                    trackId = e.TrackId,
                    transmissionJobId = e.TransmissionJobId,
                    fechaEmision = e.FechaEmision,
                    fechaEnvio = e.FechaEnvio,
                    montoTotal = e.TotalGeneral,
                    rncComprador = e.RncReceptor,
                    nombreReceptor = e.NombreReceptor,
                    mensajeRespuesta = e.MensajeRespuesta
                })
                .ToListAsync();

            return Ok(items);
        }

        // ============================================================
        // Reprocesar documento en error
        // ============================================================

        [HttpPost("reprocesar/{idEcf}")]
        public async Task<IActionResult> Reprocesar(int idEcf)
        {
            var ecf = await _ctx.ECFEncabezados.FindAsync(idEcf);
            if (ecf == null)
                return NotFound("Documento no encontrado");

            if (ecf.EstadoDGII != "Error" && ecf.EstadoDGII != "Rechazado" && ecf.EstadoDocumento != "ERROR")
                return BadRequest("Solo se pueden reprocesar documentos en estado Error o Rechazado");

            ecf.EstadoDGII = "Pendiente";
            ecf.EstadoDocumento = EstadoDocumentoElectronico.PendienteEnvio;
            ecf.MensajeRespuesta = null;
            ecf.CodigoError = null;

            var outboxEvento = new EventoOutbox
            {
                TipoEvento = "ECF_ENVIAR_GATEWAY",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    idEcf = ecf.IdECF,
                    idEmpresa = ecf.IdEmpresa
                }),
                Estado = "Pendiente",
                FechaCreacion = DateTime.Now,
                IdempotencyKey = $"REPROCESAR_{ecf.IdECF}_{DateTime.UtcNow.Ticks}"
            };

            _ctx.Set<EventoOutbox>().Add(outboxEvento);
            await _ctx.SaveChangesAsync();

            return Ok(new { mensaje = $"Documento {ecf.ENCF} reencolado para envío" });
        }

        // ============================================================
        // Dashboard / estadísticas
        // ============================================================

        [HttpGet("dashboard/{idEmpresa}")]
        public async Task<IActionResult> Dashboard(int idEmpresa)
        {
            var ecfs = _ctx.ECFEncabezados
                .AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa);

            var total = await ecfs.CountAsync();
            var aceptados = await ecfs.CountAsync(e => e.EstadoDGII == "Aceptado");
            var rechazados = await ecfs.CountAsync(e => e.EstadoDGII == "Rechazado");
            var pendientes = await ecfs.CountAsync(e => e.EstadoDGII == "Pendiente" || e.EstadoDGII == "Enviado" || e.EstadoDGII == null || e.EstadoDGII == "");
            var errores = await ecfs.CountAsync(e => e.EstadoDGII == "Error" || e.EstadoDocumento == "ERROR");
            var condicionales = await ecfs.CountAsync(e => e.EstadoDGII == "AceptadoCondicional");

            return Ok(new
            {
                totalDocumentos = total,
                aceptados,
                rechazados,
                pendientes,
                errores,
                condicionales
            });
        }
    }

    public class AmbienteFeRequest
    {
        public string Ambiente { get; set; } = "testecf";
    }

    public class ProveedorFeRequest
    {
        public string Proveedor { get; set; } = "DGII_DIRECTO";
        public string? Nombre { get; set; }
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public bool? ClearApiKey { get; set; }
        public bool? ClearPassword { get; set; }
    }
}
