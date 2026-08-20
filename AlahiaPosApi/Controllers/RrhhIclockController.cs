using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    /// <summary>
    /// ADMS/iClock de ZKTeco (y clones). El reloj empuja las marcas; Alahia las integra a RrhhPonchada.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("iclock")]
    public class RrhhIclockController : ControllerBase
    {
        private readonly IRrhhDispositivoService _dispositivos;

        public RrhhIclockController(IRrhhDispositivoService dispositivos) => _dispositivos = dispositivos;

        [HttpGet("cdata")]
        [HttpPost("cdata")]
        public async Task<IActionResult> CData()
        {
            var sn = Query("SN") ?? Query("sn") ?? "";
            var table = Query("table") ?? "";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.Equals(Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await _dispositivos.HeartbeatAsync(sn, ip);
                var option = $"GET OPTION FROM: {sn}\n" +
                             "Stamp=9999\nOpStamp=9999\nPhotoStamp=9999\n" +
                             "ErrorDelay=30\nDelay=10\nTransTimes=00:00;14:00\nTransInterval=1\n" +
                             "TransFlag=TransData AttLog\nTimeZone=-240\nRealtime=1\nEncrypt=0\n";
                return Content(option, "text/plain");
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (table.Equals("ATTLOG", StringComparison.OrdinalIgnoreCase) ||
                body.Contains('\t') || body.Contains("20"))
            {
                await _dispositivos.IngestarAttLogAsync(sn, body, ip);
            }
            else
            {
                await _dispositivos.HeartbeatAsync(sn, ip);
            }

            return Content("OK", "text/plain");
        }

        [HttpGet("getrequest")]
        public async Task<IActionResult> GetRequest()
        {
            var sn = Query("SN") ?? Query("sn") ?? "";
            await _dispositivos.HeartbeatAsync(sn, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Content("OK", "text/plain");
        }

        [HttpPost("devicecmd")]
        [HttpGet("registry")]
        [HttpPost("registry")]
        public IActionResult Ack() => Content("OK", "text/plain");

        private string? Query(string key) => Request.Query[key].FirstOrDefault();
    }

    [AllowAnonymous]
    [ApiController]
    [Route("api/rrhh/dispositivos")]
    public class RrhhDispositivoIngestaController : ControllerBase
    {
        private readonly IRrhhDispositivoService _dispositivos;

        public RrhhDispositivoIngestaController(IRrhhDispositivoService dispositivos) => _dispositivos = dispositivos;

        /// <summary>Webhook JSON para cualquier reloj/SDK que pueda hacer HTTP POST.</summary>
        [HttpPost("ingesta")]
        public async Task<IActionResult> Ingesta([FromBody] RrhhDispositivoJsonIngestaRequest body)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                return Ok(await _dispositivos.IngestarJsonAsync(body, ip));
            }
            catch (Exception ex)
            {
                return ex is ArgumentException or InvalidOperationException
                    ? BadRequest(new { message = ex.Message })
                    : StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
