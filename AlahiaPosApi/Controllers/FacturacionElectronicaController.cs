using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
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
        public async Task<IActionResult> SecuenciasDisponibles(int idEmpresa, int? idSucursal = null)
        {
            var lista = await _feService.ObtenerSecuenciasDisponiblesAsync(idEmpresa, idSucursal);
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
            if (dto.IdEmpresa == 0 || dto.TipoEcfDgii == 0)
                return BadRequest("IdEmpresa y TipoEcfDgii son requeridos");
            if (dto.SecuenciaInicial < 1)
                return BadRequest("La secuencia inicial debe ser mayor o igual a 1");
            if (dto.SecuenciaFinal < dto.SecuenciaInicial)
                return BadRequest("Defina un rango: secuencia inicial y final (ejemplo: del 1 al 10)");

            try
            {
                var created = await _secuencias.CreateAsync(dto);
                return CreatedAtAction(nameof(GetSecuencia),
                    new { id = created.IdSecuencia }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("secuencias/{id}")]
        public async Task<IActionResult> UpdateSecuencia(int id, [FromBody] SecuenciaEcfUpdateDto dto)
        {
            try
            {
                await _secuencias.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("secuencias/{id}/desactivar")]
        public async Task<IActionResult> DesactivarSecuencia(int id)
        {
            await _secuencias.DesactivarAsync(id);
            return NoContent();
        }

        [HttpPost("secuencias/{id}/asignaciones")]
        public async Task<IActionResult> AsignarRango(int id, [FromBody] SecuenciaEcfAsignarDto dto)
        {
            if (dto == null || dto.IdSucursal <= 0)
                return BadRequest("Indique la sucursal que usará este rango.");
            if (dto.SecuenciaInicial < 1 || dto.SecuenciaFinal < dto.SecuenciaInicial)
                return BadRequest("Defina un rango dentro de la autorización de la empresa.");

            try
            {
                var creada = await _secuencias.AsignarRangoAsync(id, dto);
                return Ok(creada);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("secuencias/asignaciones/{idAsignacion}")]
        public async Task<IActionResult> ActualizarAsignacion(int idAsignacion, [FromBody] SecuenciaEcfAsignarDto dto)
        {
            try
            {
                await _secuencias.ActualizarAsignacionAsync(idAsignacion, dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("secuencias/asignaciones/{idAsignacion}/desactivar")]
        public async Task<IActionResult> DesactivarAsignacion(int idAsignacion)
        {
            await _secuencias.DesactivarAsignacionAsync(idAsignacion);
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
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            if (empresa == null) return NotFound("Empresa no encontrada");

            empresa.ProveedorFE = ProveedorFiscalHelper.DgiiDirecto;

            await _ctx.SaveChangesAsync();
            return Ok(BuildProveedorDto(empresa, ProveedorFiscalHelper.DgiiDirecto));
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
                password = esExterno ? empresa.ProveedorFE_Password : null,
                apiKey = esExterno ? empresa.ProveedorFE_ApiKey : null,
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
        [PermitirEmpresaObjetivo]
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
                    c.RutaArchivo,
                    password = c.PasswordEncriptado,
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
                rutaArchivo = cert.RutaArchivo,
                password = cert.password,
                vencido = cert.FechaExpiracion < DateTime.Now,
                usable = cert.tieneBytes || cert.tieneRuta
            });
        }

        [HttpPost("certificado/{idEmpresa}")]
        [PermitirEmpresaObjetivo]
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
                using var abierto = CertificadoP12.Abrir(bytes, password);
                fechaExp = abierto.Certificado.NotAfter;
                subject = abierto.Certificado.Subject;
                thumbprint = abierto.Certificado.Thumbprint;
                bytes = abierto.BytesCompatibles;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
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

            // Persistir estado, QR y fecha de firma (aunque el estado no cambie:
            // reconsulta repara ConsultaTimbre de E31 con FechaFirma mal parseada).
            if (!string.IsNullOrWhiteSpace(trackId) && !string.IsNullOrWhiteSpace(resultado.Estado))
            {
                var ecf = await _ctx.ECFEncabezados
                    .AsTracking()
                    .FirstOrDefaultAsync(e => e.TrackId == trackId);
                if (ecf != null)
                {
                    var dirty = false;
                    if (!string.Equals(ecf.EstadoDGII, resultado.Estado, StringComparison.OrdinalIgnoreCase))
                    {
                        ecf.EstadoDGII = resultado.Estado;
                        if (resultado.EsAceptado || resultado.Estado is "AceptadoCondicional")
                            ecf.EstadoDocumento = EstadoDocumentoElectronico.Aceptado;
                        else if (resultado.EsRechazado)
                            ecf.EstadoDocumento = EstadoDocumentoElectronico.Rechazado;
                        ecf.FechaRespuesta = DateTime.Now;
                        dirty = true;
                    }
                    if (!string.IsNullOrWhiteSpace(resultado.SecurityCode)
                        && !string.Equals(ecf.SecurityCode, resultado.SecurityCode, StringComparison.Ordinal))
                    {
                        ecf.SecurityCode = resultado.SecurityCode;
                        dirty = true;
                    }
                    if (!string.IsNullOrWhiteSpace(resultado.UrlQR)
                        && !string.Equals(ecf.UrlQR, resultado.UrlQR, StringComparison.Ordinal))
                    {
                        ecf.UrlQR = resultado.UrlQR;
                        dirty = true;
                    }
                    if (resultado.FechaFirma.HasValue && ecf.FechaFirma != resultado.FechaFirma)
                    {
                        ecf.FechaFirma = resultado.FechaFirma;
                        dirty = true;
                    }
                    if (resultado.Mensajes.Count > 0)
                    {
                        var texto = string.Join("; ", resultado.Mensajes.Where(m => !string.IsNullOrWhiteSpace(m)));
                        if (!string.IsNullOrWhiteSpace(texto) && !string.Equals(ecf.MensajeRespuesta, texto, StringComparison.Ordinal))
                        {
                            ecf.MensajeRespuesta = texto;
                            dirty = true;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(resultado.CodigoError)
                        && !string.Equals(ecf.CodigoError, resultado.CodigoError, StringComparison.Ordinal))
                    {
                        ecf.CodigoError = resultado.CodigoError;
                        dirty = true;
                    }
                    if (dirty)
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
        // Vista previa: factura, QR y motivo DGII
        // ============================================================

        [HttpGet("detalle/{idEcf}")]
        public async Task<IActionResult> Detalle(int idEcf)
        {
            var ecf = await _ctx.ECFEncabezados.AsTracking().FirstOrDefaultAsync(e => e.IdECF == idEcf);
            if (ecf == null)
                return NotFound("Documento no encontrado");

            var empresa = await _ctx.Empresas.AsNoTracking()
                .Where(x => x.IdEmpresa == ecf.IdEmpresa)
                .Select(x => new { x.NombreComercial, x.Direccion, x.Telefono })
                .FirstOrDefaultAsync();

            int tipo = 0;
            int.TryParse(ecf.TipoECF, out tipo);

            var mensajes = (ecf.MensajeRespuesta ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            var estado = (ecf.EstadoDGII ?? "").Trim();
            var necesitaMotivo = mensajes.Count == 0
                && (estado.Contains("Rechazado", StringComparison.OrdinalIgnoreCase)
                    || estado.Contains("Error", StringComparison.OrdinalIgnoreCase));
            var trackOk = !string.IsNullOrWhiteSpace(ecf.TrackId)
                && !string.Equals(ecf.TrackId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase);

            if (necesitaMotivo && trackOk)
            {
                var consulta = await _feService.ConsultarEstadoDgiiAsync(ecf.TrackId!, ecf.IdEmpresa);
                var texto = string.Join("; ", (consulta.Mensajes ?? new List<string>()).Where(m => !string.IsNullOrWhiteSpace(m)));
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    ecf.MensajeRespuesta = texto;
                    mensajes = texto.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                }
                if (!string.IsNullOrWhiteSpace(consulta.CodigoError))
                    ecf.CodigoError = consulta.CodigoError;
                if (!string.IsNullOrWhiteSpace(consulta.Estado)
                    && !string.Equals(consulta.Estado, "Error", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(ecf.EstadoDGII, consulta.Estado, StringComparison.OrdinalIgnoreCase))
                {
                    ecf.EstadoDGII = consulta.Estado;
                    estado = consulta.Estado;
                }
                await _ctx.SaveChangesAsync();
            }

            var subMostrado = ecf.MontoGravado;
            var itbisMostrado = ecf.TotalITBIS;
            var totalMostrado = ecf.TotalGeneral;
            string? numeroDocumento = ecf.NumeroFacturaInterna;
            var items = new List<object>();
            if (ecf.OrigenDocumento is (int)OrigenDocumento.Pos or (int)OrigenDocumento.Facturacion && ecf.IdOrigen > 0)
            {
                var factura = await _ctx.FacturaHeaders.AsNoTracking()
                    .Where(f => f.IdFacturaHeader == ecf.IdOrigen)
                    .Select(f => f.NumeroDocumento)
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(factura))
                    numeroDocumento = factura;

                var lineas = await (
                    from d in _ctx.FacturaDetalles.AsNoTracking()
                    join p in _ctx.Productos.AsNoTracking() on d.IdProducto equals p.IdProducto into pj
                    from p in pj.DefaultIfEmpty()
                    where d.IdFacturaHeader == ecf.IdOrigen
                    orderby d.IdFacturaDetalle
                    select new { d.Cantidad, d.Itbis, d.SubTotal, d.Comentario, Nombre = p != null ? p.Nombre : null }
                ).ToListAsync();

                decimal baseSum = 0, itbisSum = 0;
                foreach (var d in lineas)
                {
                    var cantidad = d.Cantidad <= 0 ? 1m : d.Cantidad;
                    var itbisLinea = ItbisPosLinea.ExtenderSiEsUnitario(d.Itbis, d.SubTotal, d.Cantidad);
                    var baseLinea = d.SubTotal - d.Itbis;
                    if (baseLinea < 0) baseLinea = 0;
                    var nombre = string.IsNullOrWhiteSpace(d.Nombre) ? "Ítem" : d.Nombre.Trim();
                    if (!string.IsNullOrWhiteSpace(d.Comentario))
                        nombre = nombre + " — " + d.Comentario.Trim();
                    baseSum += baseLinea;
                    itbisSum += itbisLinea;
                    items.Add(new
                    {
                        nombre,
                        cantidad = d.Cantidad,
                        precio = Math.Round(baseLinea / cantidad, 2, MidpointRounding.AwayFromZero),
                        itbis = itbisLinea,
                        subTotal = baseLinea + itbisLinea
                    });
                }

                if (lineas.Count > 0)
                {
                    subMostrado = baseSum;
                    itbisMostrado = itbisSum;
                    totalMostrado = baseSum + itbisSum;
                }
            }

            var fechaVencimiento = await _ctx.SecuenciasECF.AsNoTracking()
                .Where(s => s.IdEmpresa == ecf.IdEmpresa && s.TipoEcfDgii == tipo && s.Activo)
                .Select(s => (DateTime?)s.fechaVencimiento)
                .FirstOrDefaultAsync();

            var xmlGuardado = await EcfXmlStore.TieneFirmadoAsync(_ctx, idEcf);

            string? notaTrackId = null;
            if (!trackOk)
            {
                notaTrackId = tipo == 32
                    ? "DGII no asignó TrackId. Este e32 se envió por resumen de consumo (RFCE) y la aceptación es directa. La constancia es el código de seguridad y el QR."
                    : "DGII no devolvió TrackId para este comprobante.";
            }

            if (necesitaMotivo && mensajes.Count == 0)
            {
                mensajes.Add(trackOk
                    ? "DGII no devolvió el texto del rechazo en la consulta de este TrackId."
                    : "No hay TrackId ni respuesta DGII guardada, así que no se puede leer el motivo.");
            }

            return Ok(new
            {
                idEcf = ecf.IdECF,
                encf = ecf.ENCF,
                tipoEcfDgii = tipo,
                estadoDGII = ecf.EstadoDGII,
                trackId = ecf.TrackId,
                fechaEmision = ecf.FechaEmision,
                fechaFirma = ecf.FechaFirma,
                montoTotal = totalMostrado,
                subTotal = subMostrado,
                totalItbis = itbisMostrado,
                rncComprador = ecf.RncReceptor,
                nombreReceptor = ecf.NombreReceptor,
                mensajeRespuesta = ecf.MensajeRespuesta,
                codigoError = ecf.CodigoError,
                securityCode = ecf.SecurityCode,
                urlQR = ecf.UrlQR,
                rncEmisor = ecf.RncEmisor,
                razonSocialEmisor = empresa?.NombreComercial,
                nombreComercial = empresa?.NombreComercial,
                fechaVencimiento,
                direccion = empresa?.Direccion,
                telefono = empresa?.Telefono,
                numeroDocumento,
                notaTrackId,
                xmlGuardado,
                mensajesDgii = mensajes,
                items
            });
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

            if (EcfSecuenciaYaUtilizada.EncabezadoYaFueEnviado(ecf)
                || EcfSecuenciaYaUtilizada.EnMensajes(ecf.MensajeRespuesta))
            {
                if (!int.TryParse(ecf.TipoECF, out var tipoEcf) || tipoEcf <= 0)
                    return BadRequest("Tipo e-CF inválido para reprocesar con secuencia nueva");

                var resultado = await _feService.EmitirYEnviarAsync(new EmisionEcfRequest
                {
                    IdEmpresa = ecf.IdEmpresa,
                    TipoEcfDgii = tipoEcf,
                    OrigenDocumento = (OrigenDocumento)ecf.OrigenDocumento,
                    IdOrigen = ecf.IdOrigen
                });
                return Ok(resultado);
            }

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
