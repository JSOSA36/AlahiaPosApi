using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using AlahiaPos.DataAccess.Servicios.AlahiaAi;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlahiaAiController : ControllerBase
    {
        private readonly IAlahiaAiService _service;
        private readonly IAiConversationHistory _history;
        private readonly IAiUsageMonitor _usage;
        private readonly IAiProviderFactory _providers;
        private readonly IAiSqlExecutor _sql;
        private readonly IEmpresaAiConfigService _empresaAi;

        public AlahiaAiController(
            IAlahiaAiService service,
            IAiConversationHistory history,
            IAiUsageMonitor usage,
            IAiProviderFactory providers,
            IAiSqlExecutor sql,
            IEmpresaAiConfigService empresaAi)
        {
            _service = service;
            _history = history;
            _usage = usage;
            _providers = providers;
            _sql = sql;
            _empresaAi = empresaAi;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AlahiaAiChatResponse>> Chat([FromBody] AlahiaAiChatRequest request, CancellationToken ct)
        {
            if (request == null || request.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("IdEmpresa y Message son requeridos.");

            var result = await _service.ChatAsync(request, ct);
            return Ok(result);
        }

        [HttpGet("resumen/{idEmpresa:int}")]
        public async Task<ActionResult<AlahiaAiResumenResponse>> Resumen(
            int idEmpresa,
            [FromQuery] int idUsuario = 0,
            [FromQuery] string? modulos = null,
            CancellationToken ct = default)
        {
            if (idEmpresa <= 0) return BadRequest("IdEmpresa inválido.");
            var mods = (modulos ?? "ALAHIA_AI,DASHBOARD")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var result = await _service.ResumenAsync(idEmpresa, idUsuario, mods, ct);
            return Ok(result);
        }

        [HttpGet("historial/{conversationId}")]
        public ActionResult<object> Historial(string conversationId)
        {
            return Ok(new
            {
                conversationId,
                messages = _history.GetMessages(conversationId),
                ticketBody = _history.FormatForTicket(conversationId)
            });
        }

        [HttpGet("providers")]
        public ActionResult<object> Providers()
        {
            var current = _providers.GetCurrent();
            return Ok(new
            {
                current = current.ProviderId,
                configured = current.IsConfigured,
                available = _providers.AvailableProviders
            });
        }

        [HttpGet("usage/{idEmpresa:int}")]
        public async Task<ActionResult<object>> Usage(int idEmpresa, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            var items = await _usage.GetRecentAsync(idEmpresa, take, ct);
            return Ok(items);
        }

        /// <summary>
        /// Smoke test Fase 1: ejecuta SELECT de solo lectura con SESSION_CONTEXT (sin LLM).
        /// </summary>
        [HttpGet("sql-smoke/{idEmpresa:int}")]
        public async Task<ActionResult<object>> SqlSmoke(int idEmpresa, CancellationToken ct = default)
        {
            if (idEmpresa <= 0) return BadRequest("IdEmpresa inválido.");
            if (!_sql.IsEnabled)
            {
                return Ok(new
                {
                    enabled = false,
                    mensaje = "AlahiaAi:Sql:Enabled está en false. Actívalo en appsettings para probar."
                });
            }

            var sql = @"
SELECT
  (SELECT COUNT(*) FROM ai.v_FacturaHeaders) AS Facturas,
  (SELECT COUNT(*) FROM ai.v_Clientes) AS Clientes,
  (SELECT COUNT(*) FROM ai.v_Productos) AS Productos";

            var result = await _sql.ExecuteAsync(idEmpresa, sql, requireTenantContext: true, ct);
            return Ok(new
            {
                enabled = true,
                idEmpresa,
                success = result.Success,
                error = result.Error,
                detail = result.Detail,
                rowCount = result.RowCount,
                data = result.JsonRows,
                sql = result.SqlExecuted
            });
        }

        [HttpGet("config/{idEmpresa:int}")]
        public async Task<ActionResult<EmpresaAiConfigDto>> GetConfig(int idEmpresa, CancellationToken ct)
        {
            if (idEmpresa <= 0) return BadRequest("IdEmpresa inválido.");
            var dto = await _empresaAi.ObtenerAsync(idEmpresa, ct);
            return Ok(dto);
        }

        [HttpPut("config")]
        public async Task<ActionResult<EmpresaAiConfigDto>> SaveConfig(
            [FromBody] GuardarEmpresaAiConfigDto dto,
            CancellationToken ct)
        {
            if (dto == null || dto.IdEmpresa <= 0)
                return BadRequest("IdEmpresa es requerido.");
            try
            {
                var saved = await _empresaAi.GuardarAsync(dto, ct);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
