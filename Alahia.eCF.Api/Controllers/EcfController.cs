using Microsoft.AspNetCore.Mvc;
using Alahia.eCF.Api.Interfaces;

namespace Alahia.eCF.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EcfController : ControllerBase
    {
        private readonly IEcfService _ecfService;

        public EcfController(IEcfService ecfService)
        {
            _ecfService = ecfService;
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar(
            IFormFile file,
            [FromForm] string rnc,
            [FromForm] string rutaCertificado,
            [FromForm] string passwordCertificado)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Debe subir un archivo Excel");

            var request = new EcfRequestDto
            {
                Excel = file.OpenReadStream(),
                RNC = rnc,
                RutaCertificado = rutaCertificado,
                PasswordCertificado = passwordCertificado
            };

            var result = await _ecfService.ProcesarEcf(request);

            return Ok(result);
        }
    }
}