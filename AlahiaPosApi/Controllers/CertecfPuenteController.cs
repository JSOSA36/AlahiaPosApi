using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Controllers;

/// <summary>
/// El portal de certificación usa el mismo laboratorio que la clínica Dra Sena.
/// La llave solo la lee el portal en el servidor. No es una sesión de usuario.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/certecf-puente")]
public class CertecfPuenteController : ControllerBase
{
    public const string HeaderLlave = "X-Certecf-Puente";
    public const string RutaLlave = @"C:\Alahia\certecf-puente.key";

    private readonly AlahiaPosContext _ctx;
    private readonly ICertecfCertificacionService _cert;

    public CertecfPuenteController(AlahiaPosContext ctx, ICertecfCertificacionService cert)
    {
        _ctx = ctx;
        _cert = cert;
    }

    [HttpGet("estado")]
    public async Task<IActionResult> Estado([FromQuery] string rnc, CancellationToken ct)
    {
        if (!LlaveOk()) return NoAutorizado();
        try
        {
            var id = await ResolverEmpresaAsync(rnc, null, ct);
            var estado = await _cert.GetEstadoAsync(id, ct);
            return Ok(new
            {
                estado.IdEmpresa,
                estado.CertificadoOk,
                estado.SesionActiva,
                estado.SesionAcecf,
                estado.Aviso
            });
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("excel")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Excel(
        [FromForm] string rnc,
        [FromForm] string? razonSocial,
        [FromForm] string? nombreComercial,
        [FromForm] string? direccion,
        [FromForm] string? telefono,
        [FromForm] string? correo,
        IFormFile archivo,
        IFormFile? certificado,
        [FromForm] string? password,
        CancellationToken ct)
    {
        if (!LlaveOk()) return NoAutorizado();
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "Suba el Excel descargado de CerteCF (paso 2 e-CF o paso 3 ACECF)." });
        var ext = Path.GetExtension(archivo.FileName ?? "").ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xls")
            return BadRequest(new { message = "Solo se acepta Excel (.xlsx)." });

        try
        {
            var id = await ResolverEmpresaAsync(rnc, new DatosEmpresa(razonSocial, nombreComercial, direccion, telefono, correo), ct);
            if (certificado is { Length: > 0 } && !string.IsNullOrWhiteSpace(password))
                await AsegurarCertificadoAsync(id, certificado, password, ct);

            await using var stream = archivo.OpenReadStream();
            var sesion = await _cert.CargarExcelAsync(id, null, archivo.FileName ?? "set.xlsx", stream, ct);
            return Ok(sesion);
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("caso/{idCaso:int}/enviar")]
    public async Task<IActionResult> Enviar(int idCaso, [FromQuery] string rnc, CancellationToken ct)
    {
        if (!LlaveOk()) return NoAutorizado();
        try
        {
            var id = await ResolverEmpresaAsync(rnc, null, ct);
            return Ok(await _cert.EnviarCasoAsync(id, idCaso, ct));
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("caso/{idCaso:int}/consultar")]
    public async Task<IActionResult> Consultar(int idCaso, [FromQuery] string rnc, CancellationToken ct)
    {
        if (!LlaveOk()) return NoAutorizado();
        try
        {
            var id = await ResolverEmpresaAsync(rnc, null, ct);
            return Ok(await _cert.ConsultarCasoAsync(id, idCaso, ct));
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("reiniciar")]
    public async Task<IActionResult> Reiniciar([FromQuery] string rnc, CancellationToken ct)
    {
        if (!LlaveOk()) return NoAutorizado();
        try
        {
            var id = await ResolverEmpresaAsync(rnc, null, ct);
            return Ok(await _cert.ReiniciarSetDatosAsync(id, ct));
        }
        catch (Exception ex) { return Error(ex); }
    }

    private IActionResult Error(Exception ex)
    {
        var texto = ex.GetBaseException().Message;
        if (string.IsNullOrWhiteSpace(texto))
            texto = ex.Message;
        return BadRequest(new { message = texto });
    }

    private async Task<int> ResolverEmpresaAsync(string? rncRaw, DatosEmpresa? alta, CancellationToken ct)
    {
        var rnc = Digitos(rncRaw);
        if (rnc.Length is not (9 or 11))
            throw new InvalidOperationException("El RNC debe tener 9 u 11 dígitos.");

        var filas = await _ctx.Empresas.AsNoTracking()
            .Where(e => e.RNC != null && e.RNC != "")
            .Select(e => new { e.IdEmpresa, e.RNC })
            .ToListAsync(ct);
        var ids = filas.Where(e => Digitos(e.RNC) == rnc).Select(e => e.IdEmpresa).Distinct().ToList();
        if (ids.Count == 0)
        {
            if (alta == null || string.IsNullOrWhiteSpace(alta.RazonSocial))
                throw new InvalidOperationException("Ese RNC no está en el ERP. La prueba de datos es la de la empresa, como en la clínica Dra Sena.");
            return await CrearEmpresaCertificacionAsync(rnc, alta, ct);
        }

        if (ids.Count == 1)
            return ids[0];

        var conCert = await _ctx.CertificadosDigitales.AsNoTracking()
            .Where(c => ids.Contains(c.IdEmpresa) && c.Activo)
            .Select(c => c.IdEmpresa)
            .Distinct()
            .ToListAsync(ct);
        return conCert.Count > 0 ? conCert.Min() : ids.Min();
    }

    private async Task<int> CrearEmpresaCertificacionAsync(string rnc, DatosEmpresa alta, CancellationToken ct)
    {
        var empresa = new Empresas
        {
            NombreComercial = (alta.NombreComercial ?? alta.RazonSocial ?? rnc).Trim(),
            RNC = rnc,
            Direccion = string.IsNullOrWhiteSpace(alta.Direccion) ? "Certificación e-CF" : alta.Direccion.Trim(),
            Telefono = alta.Telefono?.Trim() ?? "",
            CorreElectronico = alta.Correo?.Trim() ?? "",
            FechaInseccion = DateTime.Now,
            FechaTerminacion = DateTime.Now.AddYears(50),
            GuidPublico = Guid.NewGuid(),
            Estado = true,
            EstadoServicio = "ACTIVA",
            PagadoServicio = true,
            PoliticasAceptadas = true,
            IdPlan = 1,
            LimiteUsuario = 5,
            LimiteTerminalesPos = 1,
            NivelSoporte = NivelesSoporte.Standard,
            TrabajaDomingo = true,
            MontoServicio = 0m,
            PrecioPlanEspecialUsd = 0m,
            PrimaryColor = "#0a3d91",
            SecondaryColor = "#f5c518",
            TertiaryColor = "#072a66",
            titleColor = "#072a66",
            UsaSSL = true,
            PuertoSMTP = 587,
            AmbienteFE = DgiiAmbienteHelper.Certificacion,
            ProveedorFE = "DGII_DIRECTO",
            EsEmisorElectronico = false,
            EstadoFE = "EnProceso"
        };
        _ctx.Empresas.Add(empresa);
        await _ctx.SaveChangesAsync(ct);
        return empresa.IdEmpresa;
    }

    private async Task AsegurarCertificadoAsync(int idEmpresa, IFormFile certificado, string password, CancellationToken ct)
    {
        var ya = await _ctx.CertificadosDigitales.AsNoTracking()
            .AnyAsync(c => c.IdEmpresa == idEmpresa && c.Activo
                && (c.ArchivoBytes != null || (c.RutaArchivo != null && c.RutaArchivo != "")), ct);
        if (ya)
            return;

        await using var ms = new MemoryStream();
        await certificado.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        using var abierto = CertificadoP12.Abrir(bytes, password);

        var nuevo = new CertificadoDigital
        {
            IdEmpresa = idEmpresa,
            NombreArchivo = string.IsNullOrWhiteSpace(certificado.FileName) ? "certificado.p12" : Path.GetFileName(certificado.FileName),
            ArchivoBytes = abierto.BytesCompatibles,
            PasswordEncriptado = password,
            FechaExpiracion = abierto.Certificado.NotAfter,
            Activo = true,
            Ambiente = DgiiAmbienteHelper.EtiquetaSecuencia(DgiiAmbienteHelper.Certificacion),
            FechaCreacion = DateTime.Now
        };
        _ctx.CertificadosDigitales.Add(nuevo);
        await _ctx.SaveChangesAsync(ct);
    }

    private bool LlaveOk()
    {
        if (!System.IO.File.Exists(RutaLlave))
            return false;
        var esperada = System.IO.File.ReadAllText(RutaLlave).Trim();
        var dada = Request.Headers[HeaderLlave].ToString().Trim();
        if (esperada.Length < 16 || esperada.Length != dada.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperada),
            Encoding.UTF8.GetBytes(dada));
    }

    private IActionResult NoAutorizado()
        => StatusCode(StatusCodes.Status401Unauthorized, new { message = "El laboratorio de certificación no está disponible." });

    private static string Digitos(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        var chars = new char[valor.Length];
        var n = 0;
        foreach (var c in valor)
        {
            if (c is >= '0' and <= '9')
                chars[n++] = c;
        }
        return new string(chars, 0, n);
    }

    private sealed record DatosEmpresa(string? RazonSocial, string? NombreComercial, string? Direccion, string? Telefono, string? Correo);
}
