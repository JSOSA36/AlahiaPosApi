using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
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

        public AlahiaAiController(
            IAlahiaAiService service,
            IAiConversationHistory history,
            IAiUsageMonitor usage,
            IAiProviderFactory providers)
        {
            _service = service;
            _history = history;
            _usage = usage;
            _providers = providers;
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
    }
}
