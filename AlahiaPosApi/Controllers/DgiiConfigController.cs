using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DgiiConfigController : ControllerBase
    {
        private readonly IDgiiConfigService _config;
        private readonly IFiscalFeatureService _features;
        private readonly IDgiiFiscalAuthService _auth;
        private readonly IReporteIt1Service _reporteIt1;

        public DgiiConfigController(
            IDgiiConfigService config,
            IFiscalFeatureService features,
            IDgiiFiscalAuthService auth,
            IReporteIt1Service reporteIt1)
        {
            _config = config;
            _features = features;
            _auth = auth;
            _reporteIt1 = reporteIt1;
        }

        [HttpGet("{idEmpresa:int}")]
        public async Task<ActionResult<DgiiConfiguracionEmpresaDto>> Get(int idEmpresa)
            => Ok(await _config.GetAsync(idEmpresa));

        [HttpGet("{idEmpresa:int}/features")]
        public async Task<ActionResult<FiscalFeatureFlags>> GetFeatures(int idEmpresa)
            => Ok(await _features.GetFeaturesAsync(idEmpresa));

        /// <summary>
        /// Requiere Authorization: Bearer {token} e IdUsuario en body o header X-IdUsuario.
        /// </summary>
        [HttpPut("{idEmpresa:int}")]
        public async Task<ActionResult<DgiiConfiguracionEmpresaDto>> Put(
            int idEmpresa,
            [FromBody] DgiiConfiguracionEmpresaDto dto,
            [FromHeader(Name = "X-IdUsuario")] int idUsuarioHeader = 0)
        {
            if (dto == null)
                return BadRequest("Payload vacío.");

            var idUsuario = idUsuarioHeader > 0 ? idUsuarioHeader : 0;
            // permitir IdUsuario en body si FE no envía header (campo extendido via Motivo no)
            try
            {
                var token = Request.Headers.Authorization.FirstOrDefault();
                // IdUsuario debe venir en header X-IdUsuario (obligatorio en PUT)
                if (idUsuario <= 0)
                    return Unauthorized(new { message = "Header X-IdUsuario requerido." });

                await _auth.EnsureCanManageConfigAsync(token, idEmpresa, idUsuario);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            dto.IdEmpresa = idEmpresa;
            var result = await _config.UpsertAsync(dto, idUsuario, dto.MotivoCambio);
            return Ok(result);
        }

        /// <summary>
        /// Liquidación IT-1 2020 + Anexo A (preview en vivo).
        /// GET api/DgiiConfig/ReporteIt1/{idEmpresa}?periodo=yyyyMM
        /// </summary>
        [HttpGet("ReporteIt1/{idEmpresa:int}")]
        public async Task<ActionResult<ReporteIt1Dto>> ReporteIt1(
            int idEmpresa,
            [FromQuery] string? periodo = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var result = await _reporteIt1.ObtenerAsync(idEmpresa, desde, hasta, periodo);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
