using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotification _notification;

        public NotificationController(INotification notification)
        {
            _notification = notification;
        }

        // ============================================
        // 🔥 POST → Enviar Notificación por EMPRESA (TAG)
        // ============================================
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendTopicDto dto)
        {
            if (dto == null || dto.IdEmpresa <= 0)
                return BadRequest("Datos inválidos.");

            bool ok = await _notification.EnviarNotificacionPorTagAsync(
                "empresa",
                dto.IdEmpresa.ToString(),
                dto.Titulo,
                dto.Cuerpo
            );

            if (!ok)
                return StatusCode(500, "Error enviando la notificación.");

            return Ok(new { message = "Notificación enviada correctamente." });
        }
    }
}
