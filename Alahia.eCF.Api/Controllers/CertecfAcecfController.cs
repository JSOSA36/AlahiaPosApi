using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;
using Microsoft.AspNetCore.Mvc;

namespace Alahia.eCF.Api.Controllers
{
    /// <summary>
    /// Aprobación comercial CerteCF. Independiente de POST /api/Receipt (e-CF certificado).
    /// </summary>
    [ApiController]
    [Route("api/CertecfAcecf")]
    public class CertecfAcecfController : ControllerBase
    {
        private readonly ICertecfAcecfSender _sender;

        public CertecfAcecfController(ICertecfAcecfSender sender) => _sender = sender;

        [HttpPost]
        public async Task<IActionResult> Enviar([FromBody] AcecfDocumento documento, CancellationToken ct)
        {
            if (documento == null || string.IsNullOrWhiteSpace(documento.Encf))
                return BadRequest(new { error = "eNCF es requerido" });
            var r = await _sender.EnviarAsync(documento, ct);
            return r.Exitoso ? Ok(r) : UnprocessableEntity(r);
        }
    }
}
