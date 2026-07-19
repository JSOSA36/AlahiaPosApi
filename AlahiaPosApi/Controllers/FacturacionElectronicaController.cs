using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturacionElectronicaController : ControllerBase
    {
        private readonly IFacturacionElectronicaService _feService;
        private readonly ISecuenciaEcfService _secuencias;
        private readonly IFiscalGateway _gateway;
        private readonly AlahiaPosContext _ctx;

        public FacturacionElectronicaController(
            IFacturacionElectronicaService feService,
            ISecuenciaEcfService secuencias,
            IFiscalGateway gateway,
            AlahiaPosContext ctx)
        {
            _feService = feService;
            _secuencias = secuencias;
            _gateway = gateway;
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
        // Gateway Fiscal
        // ============================================================

        [HttpGet("gateway/health")]
        public async Task<IActionResult> GatewayHealth()
        {
            var ok = await _gateway.VerificarConexionAsync();
            return Ok(new { conectado = ok });
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
        public async Task<IActionResult> Health()
        {
            var ok = await _gateway.VerificarConexionAsync();
            return Ok(new { conectado = ok });
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
}
