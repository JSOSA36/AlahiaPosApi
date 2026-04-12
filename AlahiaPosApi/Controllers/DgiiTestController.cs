using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Setting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/dgii-test")]
    public class DgiiTestController : ControllerBase
    {
        private readonly ITestSetLoader _loader;
        private readonly IRfceBuilder _rfceBuilder;
        private readonly IECFValidator _validator;
        private readonly IECFSigner _signer;
        private readonly IDgiiClientService _dgii;
        private readonly IExcelMapperService _excelMapper;
        private readonly IXmlGeneratorService _xmlGenerator;
        private readonly DgiiSettings _dgiiSettings;
        public DgiiTestController(
            ITestSetLoader loader,
            IRfceBuilder rfceBuilder,
            IECFValidator validator,
            IECFSigner signer,
            IDgiiClientService dgii,
            IOptions<DgiiSettings> dgiiSettings,
            IExcelMapperService excelMapper,
            IXmlGeneratorService xmlGenerator)
        {
            _loader = loader;
            _rfceBuilder = rfceBuilder;
            _validator = validator;
            _signer = signer;
            _dgii = dgii;
            _dgiiSettings = dgiiSettings.Value;
            _excelMapper = excelMapper;
            _xmlGenerator = xmlGenerator;
        }

        // ==========================================
        // LEER EXCEL
        // ==========================================

        [HttpGet("leer-excel")]
        public IActionResult LeerExcel([FromQuery] string path)
        {
            var (ecfs, rfces) = _loader.Cargar(path);

            return Ok(new
            {
                ecfsCount = ecfs.Count,
                rfcesCount = rfces.Count,
                ejemploEcf = ecfs.FirstOrDefault(),
                ejemploRfce = rfces.FirstOrDefault()
            });
        }
        [HttpGet("consultar-track")]
        public async Task<IActionResult> ConsultarTrack([FromQuery] string trackId)
        {
            var resp = await _dgii.ConsultarResultado(trackId);

            return Ok(resp);
        }
        // ==========================================
        // GENERAR Y VALIDAR RFCE
        // ==========================================
        [HttpGet("rfce-enviar-por-encf")]
        public async Task<IActionResult> EnviarRfcePorEncf(
           [FromQuery] string path,
             [FromQuery] string encf)
        {
            var (_, rfces) = _loader.Cargar(path);

            var rfce = rfces.FirstOrDefault(x =>
                string.Equals(x.ENCF?.Trim(), encf?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (rfce == null)
                return BadRequest($"No se encontró RFCE con ENCF: {encf}");

            var xml = _rfceBuilder.Build(rfce);

            var rutaXsd = Path.Combine(AppContext.BaseDirectory, "Resource", "XSD", "RFCE32v.1.0.xsd");
            _validator.Validar(xml, rutaXsd);

            var xmlFirmado = _signer.Firmar(xml, _dgiiSettings.P12Path, _dgiiSettings.P12Password);

            // 👇 devuelve data real del envío
            var respDbg = await _dgii.EnviarRFCE_Debug(xmlFirmado,"");

            return Ok(new
            {
                mensaje = "RFCE intentado enviar a DGII",
                encf = rfce.ENCF,
                dgii = respDbg
            });
        }
        [HttpGet("rfce-validar")]
        public IActionResult GenerarYValidarRfce([FromQuery] string path)
        {
            var (_, rfces) = _loader.Cargar(path);

            var rfce = rfces.FirstOrDefault();

            if (rfce == null)
                return BadRequest("No hay RFCE en el Excel");

            var xml = _rfceBuilder.Build(rfce);

            var rutaXsd = Path.Combine(
                AppContext.BaseDirectory,
                "Resource",
                "XSD",
                "RFCE32v.1.0.xsd");

            _validator.Validar(xml, rutaXsd);

            return Ok(new
            {
                mensaje = "RFCE válido contra XSD",
                xml
            });
        }

        // ==========================================
        // GENERAR, FIRMAR Y ENVIAR RFCE A DGII
        // ==========================================
        [HttpGet("rfce-enviar")]
        public async Task<IActionResult> GenerarFirmarYEnviarRfce([FromQuery] string path)
        {
            var (_, rfces) = _loader.Cargar(path);

            var rfce = rfces.FirstOrDefault();

            if (rfce == null)
                return BadRequest("No hay RFCE en el Excel");

            // 1️⃣ Generar XML (SIN firma)
            var xml = _rfceBuilder.Build(rfce);

            // 2️⃣ Validar contra XSD (SIEMPRE el XML SIN firmar)
            var rutaXsd = Path.Combine(
                AppContext.BaseDirectory,
                "Resource",
                "XSD",
                "RFCE32v.1.0.xsd"
            );

            _validator.Validar(xml, rutaXsd);

            // 3️⃣ Firmar XML usando certificado configurado
            var xmlFirmado = _signer.Firmar(
                xml,
                _dgiiSettings.P12Path,
                _dgiiSettings.P12Password
            );

            // 4️⃣ Enviar a DGII (se envía el firmado)
            var respuesta = await _dgii.EnviarRFCE(xmlFirmado);

            return Ok(new
            {
                mensaje = "RFCE enviado a DGII",
                respuesta,

                // 🔎 Útil para depurar (si no quieres, quítalo)
                // xmlSinFirma = xml,
                // xmlFirmado = xmlFirmado
            });
        }
        [HttpGet("autenticar")]
        public async Task<IActionResult> Autenticar()
        {
            try
            {
                var token = await _dgii.Autenticar();

                return Ok(new
                {
                    mensaje = "Autenticación exitosa con DGII",
                    token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = "Error autenticando con DGII",
                    error = ex.Message
                });
            }
        }
        [HttpGet("enviar-xml-local")]
        [HttpPost("enviar-excel")]
        public async Task<IActionResult> EnviarExcel(IFormFile file, string rnc)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Archivo inválido");

                // 🔥 1. PASAR EL ARCHIVO AL MAPPER
                var lista = await _excelMapper.Mapear(file.OpenReadStream(), 31);

                var resultados = new List<object>();

                // 🔥 2. RECORRER LISTA
                foreach (var dto in lista)
                {
                    // 🔥 3. GENERAR XML
                    var xml = _xmlGenerator.GenerarXml(dto);

                    // 🔥 4. FIRMAR
                    var xmlFirmado = _signer.Firmar(
                        xml,
                        _dgiiSettings.P12Path,
                        _dgiiSettings.P12Password
                    );
                    string NombreFile = $"{rnc}{dto.Encabezado.IdDoc.eNCF}.xml";
                    // 🔥 5. ENVIAR (CON RNC)
                    var respuesta = await _dgii.EnviarRFCE_Debug(xmlFirmado, NombreFile);

                    resultados.Add(new
                    {
                        encf = dto.Encabezado.IdDoc.eNCF,
                        respuesta
                    });
                }

                return Ok(new
                {
                    mensaje = "Excel procesado correctamente",
                    total = resultados.Count,
                    resultados
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = "Error procesando Excel",
                    error = ex.Message
                });
            }
        }
    }
}