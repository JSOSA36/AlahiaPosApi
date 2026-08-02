using System.Security.Cryptography;
using System.Text;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesoreriaExtractoController : ControllerBase
    {
        private static readonly HashSet<string> Extensiones =
            new(StringComparer.OrdinalIgnoreCase) { ".csv", ".txt", ".xlsx", ".xls", ".pdf" };

        private const long MaxBytes = 8 * 1024 * 1024;

        private readonly ITesoreriaExtractoService _service;
        private readonly IPdfTextExtractor _pdfTextExtractor;

        public TesoreriaExtractoController(
            ITesoreriaExtractoService service,
            IPdfTextExtractor pdfTextExtractor)
        {
            _service = service;
            _pdfTextExtractor = pdfTextExtractor;
        }

        [HttpPost("Importar")]
        public async Task<ActionResult<TesoreriaExtractoImport>> Importar([FromBody] ImportarExtractoDto dto)
        {
            try
            {
                var result = await _service.ImportarAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ImportarArchivo")]
        public async Task<ActionResult<TesoreriaExtractoImport>> ImportarArchivo(
            [FromForm] int idEmpresa,
            [FromForm] int idCuentaFinanciera,
            [FromForm] int idUsuario,
            IFormFile archivo)
        {
            try
            {
                var dto = await BuildImportDtoFromArchivoAsync(idEmpresa, idCuentaFinanciera, idUsuario, archivo);
                var result = await _service.ImportarAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Parsea el archivo y persiste un extracto en estado PREVIEW (editable),
        /// sin matching ni mutación de MovimientoFinanciero.
        /// </summary>
        [HttpPost("PreviewArchivo")]
        public async Task<ActionResult<ExtractoPreviewDto>> PreviewArchivo(
            [FromForm] int idEmpresa,
            [FromForm] int idCuentaFinanciera,
            [FromForm] int idUsuario,
            IFormFile archivo)
        {
            try
            {
                var dto = await BuildImportDtoFromArchivoAsync(idEmpresa, idCuentaFinanciera, idUsuario, archivo);
                return Ok(await _service.PreviewAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{idEmpresa}/{idImport}/Preview")]
        public async Task<ActionResult<ExtractoPreviewDto>> GetPreview(int idEmpresa, int idImport)
        {
            try
            {
                return Ok(await _service.GetPreviewAsync(idImport, idEmpresa));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Edita líneas del preview de forma segura (agregar / editar / eliminar)
        /// o reemplaza el set completo vía <c>reemplazarTodas</c>.
        /// </summary>
        [HttpPut("{idEmpresa}/{idImport}/Preview/Lineas")]
        public async Task<ActionResult<ExtractoPreviewDto>> ActualizarLineasPreview(
            int idEmpresa,
            int idImport,
            [FromBody] ActualizarLineasPreviewDto dto)
        {
            try
            {
                return Ok(await _service.ActualizarLineasPreviewAsync(idImport, idEmpresa, dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idImport}/Confirmar")]
        public async Task<ActionResult<TesoreriaExtractoImport>> ConfirmarPreview(
            int idEmpresa,
            int idImport,
            [FromQuery] int idUsuario,
            [FromQuery] int toleranciaDiasMatch = 3)
        {
            try
            {
                return Ok(await _service.ConfirmarPreviewAsync(idImport, idEmpresa, idUsuario, toleranciaDiasMatch));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idImport}/DescartarPreview")]
        public async Task<IActionResult> DescartarPreview(
            int idEmpresa,
            int idImport,
            [FromQuery] int idUsuario)
        {
            try
            {
                await _service.DescartarPreviewAsync(idImport, idEmpresa, idUsuario);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{idEmpresa}/{idImport}")]
        public async Task<ActionResult<TesoreriaExtractoImport>> Get(int idEmpresa, int idImport)
        {
            var result = await _service.GetImportByIdAsync(idImport, idEmpresa);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("{idEmpresa}/{idImport}/Lineas")]
        public async Task<ActionResult<IEnumerable<TesoreriaExtractoLinea>>> Lineas(int idEmpresa, int idImport)
        {
            return Ok(await _service.GetLineasAsync(idImport, idEmpresa));
        }

        [HttpGet("{idEmpresa}/{idImport}/Sugerencias")]
        public async Task<ActionResult<IEnumerable<ExtractoLineaMatchDto>>> Sugerencias(int idEmpresa, int idImport)
        {
            try
            {
                return Ok(await _service.SugerirMatchesAsync(idImport, idEmpresa));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{idEmpresa}/{idImport}/Resumen")]
        public async Task<ActionResult<ExtractoResumenDto>> Resumen(
            int idEmpresa,
            int idImport,
            [FromQuery] decimal tolerancia = 0.01m)
        {
            try
            {
                return Ok(await _service.GetResumenAsync(idImport, idEmpresa, tolerancia));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idImport}/Cerrar")]
        public async Task<ActionResult<ExtractoResumenDto>> Cerrar(
            int idEmpresa,
            int idImport,
            [FromQuery] int idUsuario,
            [FromQuery] decimal tolerancia = 0.01m)
        {
            try
            {
                return Ok(await _service.CerrarAsync(idImport, idEmpresa, idUsuario, tolerancia));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ResolverLinea")]
        public async Task<ActionResult<ResolverExtractoLineaResultadoDto>> ResolverLinea(
            [FromBody] ResolverExtractoLineaDto dto)
        {
            try
            {
                return Ok(await _service.ResolverLineaAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ConfirmarMatch")]
        public async Task<IActionResult> ConfirmarMatch([FromBody] ConfirmarExtractoMatchDto dto)
        {
            try
            {
                await _service.ConfirmarMatchAsync(dto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idLinea}/Descartar")]
        public async Task<IActionResult> Descartar(int idEmpresa, int idLinea, [FromQuery] int idUsuario)
        {
            try
            {
                await _service.DescartarLineaAsync(idLinea, idEmpresa, idUsuario);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("CrearMovimiento")]
        public async Task<ActionResult<int>> CrearMovimiento([FromBody] CrearMovimientoDesdeExtractoDto dto)
        {
            try
            {
                var id = await _service.CrearMovimientoDesdeLineaAsync(dto);
                return Ok(id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<ImportarExtractoDto> BuildImportDtoFromArchivoAsync(
            int idEmpresa,
            int idCuentaFinanciera,
            int idUsuario,
            IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                throw new InvalidOperationException("Archivo requerido.");

            if (archivo.Length > MaxBytes)
                throw new InvalidOperationException("El archivo excede 8 MB.");

            var nombre = Path.GetFileName(archivo.FileName ?? "extracto.csv");
            var ext = Path.GetExtension(nombre);
            if (!Extensiones.Contains(ext))
                throw new InvalidOperationException($"Extensión no permitida: {ext}");

            await using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(bytes));

            var dto = new ImportarExtractoDto
            {
                IdEmpresa = idEmpresa,
                IdCuentaFinanciera = idCuentaFinanciera,
                IdUsuario = idUsuario,
                NombreArchivo = nombre,
                ContenidoArchivo = bytes,
                HashArchivo = hash,
                Formato = ext.Trim('.').ToUpperInvariant()
            };

            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                dto.TextoExtraido = _pdfTextExtractor.ExtractText(bytes);
                dto.Formato = "PDF";
            }
            else if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                     || ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                dto.ContenidoCsv = Encoding.UTF8.GetString(bytes);
                dto.Formato = "CSV";
            }
            else
            {
                dto.Formato = ext.Equals(".xls", StringComparison.OrdinalIgnoreCase) ? "XLS" : "XLSX";
            }

            return dto;
        }
    }
}
