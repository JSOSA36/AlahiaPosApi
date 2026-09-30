using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Fiscal;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public interface ICertecfCertificacionService
    {
        Task<CertecfLabEstadoDto> GetEstadoAsync(int idEmpresa, CancellationToken ct = default);
        Task<CertecfLabEstadoDto> GuardarPostulacionAsync(int idEmpresa, CertecfPostulacionDto dto, CancellationToken ct = default);
        Task<CertecfLabEstadoDto> MarcarPasoAsync(int idEmpresa, CertecfMarcarPasoDto dto, CancellationToken ct = default);
        Task<CertecfSesionDto> CargarExcelAsync(int idEmpresa, int? idUsuario, string nombreArchivo, Stream excel, CancellationToken ct = default);
        Task<CertecfSesionDto> ReiniciarSetDatosAsync(int idEmpresa, CancellationToken ct = default);
        Task<CertecfSesionDto> GetSesionAsync(int idEmpresa, int idSesion, CancellationToken ct = default);
        Task<CertecfSesionDto> EnviarCasoAsync(int idEmpresa, int idCaso, CancellationToken ct = default);
        Task<CertecfSesionDto> ConsultarCasoAsync(int idEmpresa, int idCaso, CancellationToken ct = default);
        Task<CertecfSesionDto> GenerarSimulacionAsync(int idEmpresa, CancellationToken ct = default);
        Task<CertecfArchivoDto> GenerarPostulacionXmlAsync(int idEmpresa, CancellationToken ct = default);
        Task<CertecfArchivoDto> FirmarPostulacionXmlAsync(int idEmpresa, string nombreArchivo, Stream xml, CancellationToken ct = default);
        Task<CertecfArchivoDto> GenerarDeclaracionJuradaAsync(int idEmpresa, CancellationToken ct = default);
        Task<CertecfArchivoDto> GenerarRiAsync(int idEmpresa, int idCaso, CancellationToken ct = default);
        Task<CertecfRiLoteDto> GenerarLoteRiAsync(int idEmpresa, CancellationToken ct = default);
    }

    public sealed class CertecfCertificacionService : ICertecfCertificacionService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        private static readonly HashSet<int> PasosPortal = new() { 1, 5, 6, 7, 8, 10, 12, 13, 14, 15 };

        private readonly AlahiaPosContext _ctx;
        private readonly IFacturacionElectronicaService _fe;
        private readonly ICertecfAcecfSender _acecf;
        private readonly FiscalGatewayOptions _gw;
        private readonly ILogger<CertecfCertificacionService> _logger;

        public CertecfCertificacionService(
            AlahiaPosContext ctx,
            IFacturacionElectronicaService fe,
            ICertecfAcecfSender acecf,
            IOptions<FiscalGatewayOptions> gw,
            ILogger<CertecfCertificacionService> logger)
        {
            _ctx = ctx;
            _fe = fe;
            _acecf = acecf;
            _gw = gw.Value;
            _logger = logger;
        }

        public async Task<CertecfLabEstadoDto> GetEstadoAsync(int idEmpresa, CancellationToken ct = default)
        {
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Empresa no encontrada.");

            var cert = await _ctx.CertificadosDigitales.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync(ct);

            var sesion = await ObtenerSesionTrackedAsync(idEmpresa, tracking: false, ct);
            var publicBase = CertecfReceptorUrls.PublicBase(_gw.PublicBaseUrl, _gw.BaseUrl);
            var rncEmpresa = CertecfReceptorUrls.Digits(empresa.RNC);

            var dto = new CertecfLabEstadoDto
            {
                IdEmpresa = idEmpresa,
                Rnc = rncEmpresa,
                NombreEmpresa = empresa.NombreComercial,
                CertificadoOk = cert != null
                    && (cert.ArchivoBytes is { Length: > 0 } || !string.IsNullOrWhiteSpace(cert.RutaArchivo))
                    && cert.FechaExpiracion >= DateTime.Now,
                CertificadoNombre = cert?.NombreArchivo,
                CertificadoExpira = cert?.FechaExpiracion,
                CertificadoVencido = cert != null && cert.FechaExpiracion < DateTime.Now,
                Ambiente = DgiiAmbienteHelper.Normalize(empresa.AmbienteFE),
                Proveedor = string.IsNullOrWhiteSpace(empresa.ProveedorFE) ? "DGII_DIRECTO" : empresa.ProveedorFE,
                PasoActual = sesion?.PasoActual > 0 ? sesion.PasoActual : 1,
                InboundBaseUrl = CertecfReceptorUrls.Base(publicBase),
                RutaXmlConsumo250 = CertecfReceptorUrls.CarpetaXmlConsumoPortal(),
                RutaRi = CertecfReceptorUrls.CarpetaRiPortal()
            };

            if (sesion != null)
            {
                dto.SesionActiva = ToDto(sesion, "DATOS");
                dto.SesionAcecf = ToDto(sesion, "ACECF");
                dto.SesionSimulacion = ToDto(sesion, "SIMULACION");
                if (dto.SesionAcecf.Total == 0) dto.SesionAcecf = null;
                if (dto.SesionSimulacion.Total == 0) dto.SesionSimulacion = null;
            }

            // CerteCF usa el RNC del Excel/e-CF, no Empresas.RNC (empresa 60 sigue siendo 133524899 de otro contribuyente).
            var rncCertecf = RncEmisorDeSesion(sesion) ?? RncEmisorCertecf(dto) ?? rncEmpresa;
            dto.Rnc = rncCertecf;
            dto.RazonSocial = await RazonSocialDgiiAsync(idEmpresa, rncCertecf, ct);
            dto.NombreEmpresa = await NombreComercialDgiiAsync(rncCertecf, ct) ?? empresa.NombreComercial;
            dto.Postulacion = sesion == null
                ? new CertecfPostulacionDto
                {
                    UrlRecepcion = CertecfReceptorUrls.Recepcion(publicBase, rncCertecf),
                    UrlAprobacion = CertecfReceptorUrls.Aprobacion(publicBase, rncCertecf),
                    UrlAutenticacion = CertecfReceptorUrls.Autenticacion(publicBase, rncCertecf),
                    UrlRecepcionProd = CertecfReceptorUrls.RecepcionProd(publicBase, rncCertecf),
                    UrlAprobacionProd = CertecfReceptorUrls.AprobacionProd(publicBase, rncCertecf),
                    UrlAutenticacionProd = CertecfReceptorUrls.AutenticacionProd(publicBase, rncCertecf)
                }
                : FromSesion(sesion, publicBase, rncCertecf);

            dto.Inbound = await _ctx.CertecfInboundLogs.AsNoTracking()
                .Where(l => l.IdEmpresa == idEmpresa
                    || l.Rnc == rncCertecf
                    || l.Rnc == rncEmpresa)
                .OrderByDescending(l => l.Fecha)
                .Take(40)
                .Select(l => new CertecfInboundLogDto
                {
                    IdLog = l.IdLog,
                    Tipo = l.Tipo,
                    Encf = l.Encf,
                    Estado = l.Estado,
                    Mensaje = l.Mensaje,
                    Fecha = l.Fecha
                })
                .ToListAsync(ct);

            dto.Pasos = ArmarPasos(sesion, dto);
            dto.PasoActual = DerivarPasoActual(sesion, dto);
            if (dto.Proveedor != "DGII_DIRECTO")
                dto.Aviso = "Este laboratorio firma con Alahia (DGII directo). Al cargar el Excel se pasa la empresa a DGII_DIRECTO + certecf.";
            else if (!string.Equals(dto.Ambiente, "certecf", StringComparison.OrdinalIgnoreCase))
                dto.Aviso = "Al avanzar las pruebas el ambiente de esta empresa pasará a certecf.";

            return dto;
        }

        public async Task<CertecfLabEstadoDto> GuardarPostulacionAsync(int idEmpresa, CertecfPostulacionDto dto, CancellationToken ct = default)
        {
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Empresa no encontrada.");
            empresa.AmbienteFE = "certecf";
            empresa.ProveedorFE = "DGII_DIRECTO";

            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, null, ct);
            var baseUrl = CertecfReceptorUrls.PublicBase(_gw.PublicBaseUrl, _gw.BaseUrl);
            var rncCertecf = RncEmisorDeSesion(sesion) ?? CertecfReceptorUrls.Digits(empresa.RNC);
            sesion.NombreSoftware = NullIfEmpty(dto.NombreSoftware) ?? "Alahia ERP";
            sesion.VersionSoftware = NullIfEmpty(dto.VersionSoftware) ?? "1.0";
            sesion.TipoSoftware = NullIfEmpty(dto.TipoSoftware) ?? "EXTERNO";
            sesion.UrlRecepcion = UrlAlineadaAlRnc(dto.UrlRecepcion, CertecfReceptorUrls.Recepcion(baseUrl, rncCertecf), rncCertecf);
            sesion.UrlAprobacion = UrlAlineadaAlRnc(dto.UrlAprobacion, CertecfReceptorUrls.Aprobacion(baseUrl, rncCertecf), rncCertecf);
            sesion.UrlAutenticacion = UrlAlineadaAlRnc(dto.UrlAutenticacion, CertecfReceptorUrls.Autenticacion(baseUrl, rncCertecf), rncCertecf);
            sesion.UrlRecepcionProd = UrlAlineadaAlRnc(dto.UrlRecepcionProd, CertecfReceptorUrls.RecepcionProd(baseUrl, rncCertecf), rncCertecf);
            sesion.UrlAprobacionProd = UrlAlineadaAlRnc(dto.UrlAprobacionProd, CertecfReceptorUrls.AprobacionProd(baseUrl, rncCertecf), rncCertecf);
            sesion.UrlAutenticacionProd = UrlAlineadaAlRnc(dto.UrlAutenticacionProd, CertecfReceptorUrls.AutenticacionProd(baseUrl, rncCertecf), rncCertecf);
            if (sesion.PasoActual < 1) sesion.PasoActual = 1;
            SetPaso(sesion, 1, "EnCurso");
            sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);
            return await GetEstadoAsync(idEmpresa, ct);
        }

        public async Task<CertecfArchivoDto> FirmarPostulacionXmlAsync(
            int idEmpresa, string nombreArchivo, Stream xml, CancellationToken ct = default)
        {
            var cert = await _ctx.CertificadosDigitales.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Suba primero el certificado .p12/.pfx y la contraseña de esta empresa.");

            if (cert.FechaExpiracion < DateTime.Now)
                throw new InvalidOperationException("El certificado digital está vencido. Cargue uno vigente.");

            string xmlCrudo;
            using (var reader = new StreamReader(xml, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
                xmlCrudo = (await reader.ReadToEndAsync()).Trim();

            if (string.IsNullOrWhiteSpace(xmlCrudo))
                throw new InvalidOperationException("El archivo XML está vacío. Use el que genera el portal con GENERAR ARCHIVO.");

            string xmlLimpio;
            try { xmlLimpio = QuitarFirmaExistente(xmlCrudo); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("El archivo no es un XML válido de postulación CerteCF. " + ex.Message);
            }

            string xmlFirmado;
            try
            {
                if (cert.ArchivoBytes is { Length: > 0 })
                    xmlFirmado = XmlSigner.SignXml(xmlLimpio, cert.ArchivoBytes, cert.PasswordEncriptado);
                else if (!string.IsNullOrWhiteSpace(cert.RutaArchivo))
                    xmlFirmado = XmlSigner.SignXml(xmlLimpio, cert.RutaArchivo, cert.PasswordEncriptado);
                else
                    throw new InvalidOperationException("El certificado no tiene el .p12 almacenado. Vuelva a cargarlo.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("No se pudo firmar con ese certificado. Revise el .p12 y la contraseña. " + ex.Message);
            }

            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, null, ct);
            sesion.Mensaje = $"XML de postulación firmado ({Path.GetFileName(nombreArchivo)}).";
            sesion.PasoActual = Math.Max(sesion.PasoActual, 1);
            SetPaso(sesion, 1, "EnCurso");
            sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);

            var rnc = CertecfReceptorUrls.Digits((await EmpresaAsync(idEmpresa, ct)).RNC);
            var baseName = Path.GetFileNameWithoutExtension(nombreArchivo);
            if (string.IsNullOrWhiteSpace(baseName) || baseName.Equals("blob", StringComparison.OrdinalIgnoreCase))
                baseName = $"postulacion-{rnc}";

            return new CertecfArchivoDto
            {
                NombreArchivo = $"{baseName}-firmado.xml",
                Contenido = xmlFirmado
            };
        }

        private static string QuitarFirmaExistente(string xml)
        {
            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(xml);
            if (doc.DocumentElement == null)
                throw new InvalidOperationException("XML sin elemento raíz.");

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            var nodos = doc.SelectNodes("//ds:Signature | //*[local-name()='Signature']", nsmgr);
            if (nodos != null && nodos.Count > 0)
            {
                var quitar = new List<XmlNode>();
                foreach (XmlNode n in nodos)
                    quitar.Add(n);
                foreach (var n in quitar)
                    n.ParentNode?.RemoveChild(n);
            }

            return doc.DocumentElement.OuterXml;
        }

        public async Task<CertecfLabEstadoDto> MarcarPasoAsync(int idEmpresa, CertecfMarcarPasoDto dto, CancellationToken ct = default)
        {
            if (dto.Paso is < 1 or > 15)
                throw new InvalidOperationException("El paso debe estar entre 1 y 15.");
            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, null, ct);
            var estado = string.IsNullOrWhiteSpace(dto.Estado) ? "Hecho" : dto.Estado.Trim();
            SetPaso(sesion, dto.Paso, estado);
            if (string.Equals(estado, "Hecho", StringComparison.OrdinalIgnoreCase)
                && dto.Paso >= sesion.PasoActual)
                sesion.PasoActual = Math.Min(15, dto.Paso + 1);
            if (!string.IsNullOrWhiteSpace(dto.Nota))
                sesion.Mensaje = dto.Nota;
            sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);
            return await GetEstadoAsync(idEmpresa, ct);
        }

        public async Task<CertecfSesionDto> CargarExcelAsync(
            int idEmpresa, int? idUsuario, string nombreArchivo, Stream excel, CancellationToken ct = default)
        {
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Empresa no encontrada.");

            var certOk = await _ctx.CertificadosDigitales.AsNoTracking()
                .AnyAsync(c => c.IdEmpresa == idEmpresa && c.Activo
                    && (c.ArchivoBytes != null || (c.RutaArchivo != null && c.RutaArchivo != "")), ct);
            if (!certOk)
                throw new InvalidOperationException("Suba primero el certificado .p12/.pfx y la contraseña de esta empresa.");

            var parsed = CertecfExcelParser.Parse(excel, empresa);
            empresa.AmbienteFE = "certecf";
            empresa.ProveedorFE = "DGII_DIRECTO";

            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, idUsuario, ct);

            if (string.Equals(parsed.TipoSet, "ACECF", StringComparison.OrdinalIgnoreCase))
            {
                var viejos = sesion.Casos.Where(c => c.TipoPrueba == "ACECF").ToList();
                _ctx.CertecfCasos.RemoveRange(viejos);
                var orden = 1;
                foreach (var c in parsed.Casos)
                {
                    var ace = c.Acecf ?? throw new InvalidOperationException("Fila ACECF inválida.");
                    ace.IdEmpresa = idEmpresa;
                    sesion.Casos.Add(new CertecfCaso
                    {
                        Orden = orden++,
                        Oleada = 1,
                        TipoEcf = 0,
                        Encf = ace.Encf,
                        TipoPrueba = "ACECF",
                        Estado = "Pendiente",
                        PayloadJson = JsonSerializer.Serialize(ace, JsonOpts)
                    });
                }
                sesion.TipoSet = "ACECF";
                sesion.NombreArchivo = Path.GetFileName(nombreArchivo);
                sesion.Estado = "Cargado";
                sesion.Mensaje = $"{parsed.AcecfCasos.Count} ACECF (paso 3).";
                sesion.PasoActual = Math.Max(sesion.PasoActual, 3);
                SetPaso(sesion, 3, "EnCurso");
            }
            else
            {
                var porEncf = parsed.Casos
                    .GroupBy(c => CertecfExcelParser.ClaveEncf(c.Documento.Encabezado.Encf))
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var existentes = sesion.Casos.Where(c => c.TipoPrueba == "DATOS").ToList();
                foreach (var viejo in existentes)
                {
                    var key = CertecfExcelParser.ClaveEncf(viejo.Encf);
                    if (!porEncf.TryGetValue(key, out var nuevo))
                    {
                        _ctx.CertecfCasos.Remove(viejo);
                        continue;
                    }

                    porEncf.Remove(key);
                    nuevo.Documento.IdEmpresa = idEmpresa;
                    nuevo.Documento.AmbienteDgii = "certecf";

                    viejo.Orden = nuevo.Orden;
                    viejo.Oleada = nuevo.Oleada;
                    viejo.TipoEcf = nuevo.Documento.Encabezado.TipoEcf;
                    viejo.Encf = nuevo.Documento.Encabezado.Encf;
                    viejo.PayloadJson = JsonSerializer.Serialize(nuevo.Documento, JsonOpts);
                    viejo.Estado = "Pendiente";
                    viejo.TrackId = null;
                    viejo.Mensaje = null;
                    viejo.FechaEnvio = null;
                    viejo.FechaRespuesta = null;
                }

                foreach (var c in porEncf.Values.OrderBy(x => x.Orden))
                {
                    c.Documento.IdEmpresa = idEmpresa;
                    c.Documento.AmbienteDgii = "certecf";
                    sesion.Casos.Add(new CertecfCaso
                    {
                        Orden = c.Orden,
                        Oleada = c.Oleada,
                        TipoEcf = c.Documento.Encabezado.TipoEcf,
                        Encf = c.Documento.Encabezado.Encf,
                        TipoPrueba = "DATOS",
                        Estado = "Pendiente",
                        PayloadJson = JsonSerializer.Serialize(c.Documento, JsonOpts)
                    });
                }

                sesion.TipoSet = "ECF";
                sesion.NombreArchivo = Path.GetFileName(nombreArchivo);
                sesion.Estado = "Cargado";
                sesion.Mensaje = $"{parsed.Casos.Count} e-CF, {parsed.Columnas} columnas (paso 2). Set desde cero: DGII no conserva Aceptados si hubo un rechazo.";
                sesion.PasoActual = Math.Max(sesion.PasoActual, 2);
                SetPaso(sesion, 2, "EnCurso");
            }

            sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);
            return ToDto(sesion, parsed.TipoSet == "ACECF" ? "ACECF" : "DATOS");
        }

        /// <summary>
        /// DGII reinicia el set entero si un comprobante falla. Alahia no puede
        /// conservar Aceptados de una corrida anterior.
        /// </summary>
        public async Task<CertecfSesionDto> ReiniciarSetDatosAsync(int idEmpresa, CancellationToken ct = default)
        {
            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, null, ct);
            foreach (var c in sesion.Casos.Where(x => x.TipoPrueba == "DATOS"))
            {
                c.Estado = "Pendiente";
                c.TrackId = null;
                c.Mensaje = null;
                c.RespuestaDgii = null;
                c.FechaEnvio = null;
                c.FechaRespuesta = null;
                c.UrlQR = null;
                c.CodigoSeguridad = null;
                c.FechaFirma = null;
                c.XmlFirmado = null;
            }
            sesion.Estado = "Cargado";
            sesion.Mensaje = "Set reiniciado. Cargue el Excel de CerteCF (el nuevo si DGII lo regeneró) y envíe desde el primer E31.";
            sesion.FechaActualizacion = DateTime.Now;
            SetPaso(sesion, 2, "EnCurso");
            await _ctx.SaveChangesAsync(ct);
            return ToDto(sesion, "DATOS");
        }

        public async Task<CertecfSesionDto> GetSesionAsync(int idEmpresa, int idSesion, CancellationToken ct = default)
        {
            var sesion = await _ctx.CertecfSesiones.AsNoTracking()
                .Include(s => s.Casos)
                .FirstOrDefaultAsync(s => s.IdSesion == idSesion && s.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Sesión no encontrada.");
            return ToDto(sesion, null);
        }

        public async Task<CertecfSesionDto> EnviarCasoAsync(int idEmpresa, int idCaso, CancellationToken ct = default)
        {
            var caso = await _ctx.CertecfCasos.AsTracking()
                .Include(c => c.Sesion)
                .FirstOrDefaultAsync(c => c.IdCaso == idCaso && c.Sesion.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Caso no encontrado.");

            await AsegurarCasosCargadosAsync(caso.Sesion, ct);
            if (caso.Estado is "Aceptado" or "AceptadoCondicional")
                return ToDto(caso.Sesion, caso.TipoPrueba);

            if (string.Equals(caso.TipoPrueba, "ACECF", StringComparison.OrdinalIgnoreCase))
                return await EnviarAcecfCasoAsync(caso, idEmpresa, ct);

            var payloadOriginal = caso.PayloadJson;
            var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(payloadOriginal, JsonOpts)
                ?? throw new InvalidOperationException("No se pudo leer el payload del caso.");
            doc.IdEmpresa = idEmpresa;
            doc.AmbienteDgii = "certecf";
            doc.CeldasExcel = new Dictionary<string, string>(
                doc.CeldasExcel ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);
            RestaurarIscYSubcantidad(doc, payloadOriginal);
            CertecfExcelParser.RestaurarIscYSubcantidadDesdeCeldas(doc);
            CertecfExcelParser.RestaurarDescuentosDesdeCeldas(doc);
            CertecfExcelParser.RestaurarIndicadoresDesdeCeldas(doc);
            CertecfExcelParser.RestaurarSubAjustesItemDesdeCeldas(doc);
            CertecfExcelParser.RestaurarIdentificadorExtranjeroDesdeCeldas(doc);
            CertecfExcelParser.SanearTelefonosCertecf(doc);
            CertecfExcelParser.AplicarValoresDelExcelEnTotalesOpcionales(doc);
            AsegurarCertecfNoSaleIncompleto(doc);
            var razonEmisor = await RazonSocialDgiiAsync(
                idEmpresa, CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor), ct);
            var comercialEmisor = await NombreComercialDgiiAsync(
                CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor), ct);
            CertecfArtefactos.AplicarIdentidadEmisorReal(doc, razonEmisor, comercialEmisor);
            CertecfArtefactos.AsegurarFechaVencimientoSecuenciaCertecf(doc);
            caso.PayloadJson = JsonSerializer.Serialize(doc, JsonOpts);
            _logger.LogInformation(
                "CerteCF {Encf} sub={Sub} esp={Esp}",
                caso.Encf,
                doc.Lineas.FirstOrDefault()?.Subcantidad,
                doc.Encabezado.ImpuestosAdicionales?.FirstOrDefault()?.MontoImpuestoSelectivoConsumoEspecifico);

            var hermanos = caso.Sesion.Casos
                .Where(c => c.TipoPrueba == caso.TipoPrueba && c.IdCaso != caso.IdCaso)
                .ToList();
            if (doc.Encabezado.TipoEcf == 46 && string.IsNullOrWhiteSpace(doc.Encabezado.RncComprador))
            {
                var lote = new List<FiscalDocumentoElectronico> { doc };
                foreach (var h in hermanos)
                {
                    try
                    {
                        var dh = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(h.PayloadJson, JsonOpts);
                        if (dh != null) lote.Add(dh);
                    }
                    catch { /* ignore */ }
                }
                CertecfExcelParser.CompletarRncExportacionDesdeHermanos(lote);
            }

            caso.Oleada = CertecfExcelParser.ResolverOleada(doc.Encabezado.TipoEcf, doc.Encabezado.MontoTotal);

            var esRfce = doc.Encabezado.TipoEcf == 32
                && doc.Encabezado.MontoTotal < CertecfExcelParser.UmbralRfce;
            if (!esRfce)
            {
                var cola = hermanos.Select(h => MapColaEnvio(h)).Append(MapColaEnvio(caso, doc)).ToList();
                var siguiente = CertecfExcelParser.ResolverSiguienteEnvio(cola);
                if (siguiente != null && siguiente.IdCaso != caso.IdCaso)
                {
                    if (siguiente.Estado is "EnProceso" or "Enviado" or "Enviando")
                        throw new InvalidOperationException(
                            $"Aún no. {siguiente.Encf} está {siguiente.Estado}: DGII lo está validando. Pulse Consultar TrackId y espere Aceptado o Rechazado.");
                    throw new InvalidOperationException(
                        $"Primero {siguiente.Encf} (E{siguiente.TipoEcf}). Se envían E31 y E32 antes de las notas; si una nota modifica otro e-NCF del set, ese va primero.");
                }
            }

            caso.Estado = "Enviando";
            caso.FechaEnvio = DateTime.Now;
            caso.Sesion.Estado = "Enviando";
            caso.Sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);

            FiscalEnvioResultado resultado;
            try
            {
                resultado = await _fe.EnviarDocumentoFiscalAsync(doc, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CerteCF enviar {Encf} empresa {Empresa}", caso.Encf, idEmpresa);
                resultado = FiscalEnvioResultado.Error("EXCEPCION", ex.Message);
            }

            if (resultado.CodigoError == "VALIDACION")
            {
                caso.Estado = "Error";
                caso.Mensaje = Cortar(resultado.Mensajes.Count > 0
                    ? string.Join(" | ", resultado.Mensajes)
                    : resultado.CodigoError, 2000);
                caso.RespuestaDgii = FormatearRespuestaLocal(caso, "Validación local (no llegó a DGII)", caso.Mensaje);
                caso.FechaRespuesta = DateTime.Now;
                caso.Sesion.Estado = "Fallido";
                caso.Sesion.Mensaje = Cortar($"Validación local E{caso.TipoEcf} {caso.Encf}: {caso.Mensaje}", 1000);
                caso.Sesion.FechaActualizacion = DateTime.Now;
                await _ctx.SaveChangesAsync(ct);
                return ToDto(caso.Sesion, caso.TipoPrueba);
            }

            caso.TrackId = resultado.TrackId;
            caso.FechaRespuesta = DateTime.Now;
            caso.Mensaje = Cortar(resultado.Mensajes.Count > 0
                ? string.Join(" | ", resultado.Mensajes)
                : resultado.CodigoError, 2000);
            caso.RespuestaDgii = FormatearRespuestaDgii(caso, resultado);
            GuardarTimbreDeEnvio(caso, resultado);
            if (EcfSecuenciaYaUtilizada.EnResultado(resultado))
            {
                caso.Estado = "Aceptado";
                caso.Mensaje = Cortar(EcfSecuenciaYaUtilizada.MensajeAceptadoPorConsumo(caso.Encf), 2000);
            }
            else
            {
                caso.Estado = MapEstado(resultado);
            }
            ActualizarSesionTrasEnvio(caso);
            await _ctx.SaveChangesAsync(ct);
            return ToDto(caso.Sesion, caso.TipoPrueba);
        }

        public async Task<CertecfSesionDto> ConsultarCasoAsync(int idEmpresa, int idCaso, CancellationToken ct = default)
        {
            var caso = await _ctx.CertecfCasos.AsTracking()
                .Include(c => c.Sesion)
                .FirstOrDefaultAsync(c => c.IdCaso == idCaso && c.Sesion.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Caso no encontrado.");

            await AsegurarCasosCargadosAsync(caso.Sesion, ct);
            if (string.IsNullOrWhiteSpace(caso.TrackId))
                throw new InvalidOperationException("El caso no tiene TrackId. Envíelo primero.");

            var consulta = await _fe.ConsultarEstadoDgiiAsync(caso.TrackId, idEmpresa, ct);
            var qrEmitido = caso.UrlQR ?? CertecfArtefactos.ExtraerQrDeRespuesta(caso.RespuestaDgii);
            caso.Estado = string.IsNullOrWhiteSpace(consulta.Estado) ? caso.Estado : consulta.Estado;
            caso.Mensaje = Cortar(
                consulta.Mensajes.Count > 0 ? string.Join(" | ", consulta.Mensajes) : caso.Mensaje,
                2000);
            caso.RespuestaDgii = FormatearConsultaDgii(caso, consulta, qrEmitido);
            if (string.IsNullOrWhiteSpace(caso.UrlQR) && EcfQrUrlHelper.IsUsableHttpUrl(qrEmitido))
                caso.UrlQR = qrEmitido;
            if (string.IsNullOrWhiteSpace(caso.CodigoSeguridad) && !string.IsNullOrWhiteSpace(consulta.SecurityCode))
                caso.CodigoSeguridad = consulta.SecurityCode.Trim();
            if (!caso.FechaFirma.HasValue && consulta.FechaFirma.HasValue)
                caso.FechaFirma = consulta.FechaFirma;
            caso.FechaRespuesta = DateTime.Now;
            caso.Sesion.FechaActualizacion = DateTime.Now;
            if (EcfSecuenciaYaUtilizada.EnMensajes(caso.Mensaje, consulta.Mensajes)
                || EcfSecuenciaYaUtilizada.EsTexto(consulta.CodigoError))
            {
                caso.Estado = "Aceptado";
                caso.Mensaje = Cortar(EcfSecuenciaYaUtilizada.MensajeAceptadoPorConsumo(caso.Encf), 2000);
            }
            else if (caso.Estado is "Rechazado")
            {
                caso.Sesion.Estado = "Fallido";
                caso.Sesion.Mensaje = Cortar(
                    $"Rechazado {caso.Encf}. Corrija ese comprobante y reenvíelo; no pase al siguiente.",
                    1000);
            }
            await _ctx.SaveChangesAsync(ct);
            return ToDto(caso.Sesion, caso.TipoPrueba);
        }

        public async Task<CertecfSesionDto> GenerarSimulacionAsync(int idEmpresa, CancellationToken ct = default)
        {
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Empresa no encontrada.");
            empresa.AmbienteFE = "certecf";
            empresa.ProveedorFE = "DGII_DIRECTO";

            var sesion = await ObtenerOCrearSesionAsync(idEmpresa, null, ct);
            var datos = sesion.Casos
                .Where(c => string.Equals(c.TipoPrueba, "DATOS", StringComparison.OrdinalIgnoreCase))
                .Where(c => c.TipoEcf >= 31)
                .OrderBy(c => c.Orden)
                .ToList();
            if (datos.Count == 0)
                throw new InvalidOperationException(
                    "No hay e-CF del paso 2 para clonar. Cargue y acepte el set de datos antes de generar la simulación.");

            var viejos = sesion.Casos.Where(c => c.TipoPrueba == "SIMULACION").ToList();
            _ctx.CertecfCasos.RemoveRange(viejos);

            var idsSesion = await _ctx.CertecfSesiones.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa)
                .Select(s => s.IdSesion)
                .ToListAsync(ct);
            var usados = await _ctx.CertecfCasos.AsNoTracking()
                .Where(c => idsSesion.Contains(c.IdSesion) && c.TipoEcf >= 31)
                .Select(c => new { c.TipoEcf, c.Encf })
                .ToListAsync(ct);

            var nextSeq = new Dictionary<int, long>();
            void TomarMax(int tipo, string? encf)
            {
                var n = SecuenciaEncf(encf);
                if (!nextSeq.TryGetValue(tipo, out var max) || n > max)
                    nextSeq[tipo] = n;
            }
            foreach (var u in usados) TomarMax(u.TipoEcf, u.Encf);
            foreach (var c in datos) TomarMax(c.TipoEcf, c.Encf);
            foreach (var c in viejos) TomarMax(c.TipoEcf, c.Encf);

            var mapEncf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var clones = new List<(CertecfCaso Origen, FiscalDocumentoElectronico Doc, string Encf)>();
            foreach (var plantilla in datos)
            {
                var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(plantilla.PayloadJson, JsonOpts)
                    ?? throw new InvalidOperationException($"No se pudo leer {plantilla.Encf} del paso 2.");
                nextSeq.TryGetValue(plantilla.TipoEcf, out var actual);
                actual++;
                nextSeq[plantilla.TipoEcf] = actual;
                var encf = FormatoEncf(plantilla.TipoEcf, actual);
                mapEncf[CertecfExcelParser.ClaveEncf(plantilla.Encf)] = encf;
                clones.Add((plantilla, doc, encf));
            }

            var hoy = DateTime.Today;
            var orden = 1;
            foreach (var item in clones
                .OrderBy(x => CertecfExcelParser.PrioridadEnvio(x.Doc.Encabezado.TipoEcf, x.Doc.Encabezado.MontoTotal))
                .ThenBy(x => x.Origen.Orden))
            {
                var doc = item.Doc;
                var tipo = doc.Encabezado.TipoEcf;
                doc.Encabezado.Encf = item.Encf;
                doc.Encabezado.FechaEmision = hoy;
                doc.Encabezado.NumeroFacturaInterna = "SIM-CERTECF";
                doc.TipoDocumentoAlahia = "CertificacionSimulacion";
                doc.AmbienteDgii = "certecf";
                var oldMod = CertecfExcelParser.ClaveEncf(doc.Referencia?.NcfModificado);
                if (!string.IsNullOrEmpty(oldMod) && mapEncf.TryGetValue(oldMod, out var nuevoMod))
                {
                    doc.Referencia!.NcfModificado = nuevoMod;
                    doc.Referencia.FechaNcfModificado = hoy;
                }

                CertecfExcelParser.AlinearConDefinicionDgii(doc);
                var razonRi = await RazonSocialDgiiAsync(
                    idEmpresa, CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor), ct);
                var comercialRi = await NombreComercialDgiiAsync(
                    CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor), ct);
                CertecfArtefactos.AplicarIdentidadEmisorReal(doc, razonRi, comercialRi);
                CertecfArtefactos.AsegurarFechaVencimientoSecuenciaCertecf(doc);

                sesion.Casos.Add(new CertecfCaso
                {
                    Orden = orden++,
                    Oleada = CertecfExcelParser.ResolverOleada(tipo, doc.Encabezado.MontoTotal),
                    TipoEcf = tipo,
                    Encf = item.Encf,
                    TipoPrueba = "SIMULACION",
                    Estado = "Pendiente",
                    PayloadJson = JsonSerializer.Serialize(doc, JsonOpts)
                });
            }

            sesion.PasoActual = 4;
            SetPaso(sesion, 4, "EnCurso");
            sesion.Mensaje = $"{clones.Count} e-CF de simulación (paso 4), e-NCF nuevos (no reutiliza secuencias ya usadas en DGII).";
            sesion.FechaActualizacion = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);
            return ToDto(sesion, "SIMULACION");
        }

        public async Task<CertecfArchivoDto> GenerarPostulacionXmlAsync(int idEmpresa, CancellationToken ct = default)
        {
            var empresa = await EmpresaAsync(idEmpresa, ct);
            var estado = await GetEstadoAsync(idEmpresa, ct);
            var xml = CertecfArtefactos.PostulacionXml(empresa, estado.Postulacion, estado.Rnc, estado.RazonSocial);
            return new CertecfArchivoDto
            {
                NombreArchivo = $"postulacion-{CertecfReceptorUrls.Digits(estado.Rnc)}.xml",
                Contenido = xml
            };
        }

        public async Task<CertecfArchivoDto> GenerarDeclaracionJuradaAsync(int idEmpresa, CancellationToken ct = default)
        {
            var empresa = await EmpresaAsync(idEmpresa, ct);
            var estado = await GetEstadoAsync(idEmpresa, ct);
            var xml = CertecfArtefactos.DeclaracionJuradaXml(empresa, estado.Postulacion, estado.Rnc, estado.RazonSocial);
            return new CertecfArchivoDto
            {
                NombreArchivo = $"declaracion-jurada-{CertecfReceptorUrls.Digits(estado.Rnc)}.xml",
                Contenido = xml
            };
        }

        public async Task<CertecfArchivoDto> GenerarRiAsync(int idEmpresa, int idCaso, CancellationToken ct = default)
        {
            var empresa = await EmpresaAsync(idEmpresa, ct);
            var caso = await _ctx.CertecfCasos.AsNoTracking()
                .Include(c => c.Sesion)
                .FirstOrDefaultAsync(c => c.IdCaso == idCaso && c.Sesion.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Caso no encontrado.");
            if (string.Equals(caso.TipoPrueba, "ACECF", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La RI aplica a e-CF (pasos 4 y 5), no a ACECF.");
            var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(caso.PayloadJson, JsonOpts)
                ?? throw new InvalidOperationException("No se pudo leer el e-CF.");
            // ConsultaTimbre compara la fecha y el monto del XML firmado, no los del payload.
            var xmlFirmado = NullIfEmpty(caso.XmlFirmado)
                ?? CertecfArtefactos.BuscarXmlFirmado(caso.Encf, doc.Encabezado.RncEmisor);
            var qrUrl = ResolverQrGuardado(caso);
            if (!string.IsNullOrWhiteSpace(xmlFirmado))
            {
                AplicarFechasYMontosDelXml(doc, xmlFirmado);
                qrUrl = AlinearQrConDocumento(qrUrl, doc);
            }
            else
            {
                AplicarTimbreDelQr(doc, qrUrl);
            }
            var rncDoc = CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor);
            var razonDgii = await RazonSocialDgiiAsync(idEmpresa, rncDoc, ct);
            var comercialDgii = await NombreComercialDgiiAsync(rncDoc, ct);
            CertecfArtefactos.AplicarIdentidadEmisorReal(doc, razonDgii, comercialDgii);
            if (!EcfQrUrlHelper.IsUsableHttpUrl(qrUrl))
                qrUrl = null;
            var emisor = CertecfArtefactos.ResolverEmisorRi(empresa, doc, xmlFirmado, razonDgii, comercialDgii);
            var codigo = NullIfEmpty(caso.CodigoSeguridad);
            string? fechaFirmaTxt = caso.FechaFirma?.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(fechaFirmaTxt))
            {
                var (codigoQr, fechaQr) = CertecfArtefactos.TimbreDesdeQr(qrUrl);
                codigo = string.IsNullOrWhiteSpace(codigo) ? codigoQr : codigo;
                fechaFirmaTxt = string.IsNullOrWhiteSpace(fechaFirmaTxt) ? fechaQr : fechaFirmaTxt;
            }
            var html = CertecfArtefactos.RepresentacionImpresaHtml(emisor, doc, qrUrl, codigo, fechaFirmaTxt);
            var pdf = CertecfRiPdf.Crear(emisor, doc, qrUrl, codigo, fechaFirmaTxt);
            return new CertecfArchivoDto
            {
                NombreArchivo = NombreArchivoRi(doc),
                Contenido = html,
                ContenidoBinario = pdf,
                ContentType = "application/pdf"
            };
        }

        public async Task<CertecfRiLoteDto> GenerarLoteRiAsync(int idEmpresa, CancellationToken ct = default)
        {
            var sesion = await ObtenerSesionTrackedAsync(idEmpresa, tracking: false, ct)
                ?? throw new InvalidOperationException("No hay sesión CerteCF.");
            var sim = ToDto(sesion, "SIMULACION");
            if (sim.Casos.Count == 0)
                throw new InvalidOperationException("Primero genere la simulación del paso 4.");

            var dir = CertecfReceptorUrls.CarpetaRiPortal();
            Directory.CreateDirectory(dir);
            LimpiarCarpetaRiPortal(dir);
            var lote = new CertecfRiLoteDto { Ruta = dir };
            var orden = 0;
            foreach (var def in SlotsRiPortal())
            {
                orden++;
                var caso = ElegirCasoRi(sim.Casos, def.Match)
                    ?? ElegirCasoRi(sim.Casos, c => c.TipoEcf == def.TipoEcf);
                if (caso == null)
                {
                    var fallido = sim.Casos.FirstOrDefault(def.Match)
                        ?? sim.Casos.FirstOrDefault(c => c.TipoEcf == def.TipoEcf);
                    lote.Slots.Add(new CertecfRiSlotDto
                    {
                        Clave = def.Clave,
                        Etiqueta = def.Etiqueta,
                        TipoEcf = fallido?.TipoEcf ?? def.TipoEcf,
                        Encf = fallido?.Encf ?? "",
                        IdCaso = fallido?.IdCaso ?? 0,
                        QrListo = false,
                        NombreArchivo = "",
                        Estado = fallido?.Estado ?? "Pendiente",
                        Mensaje = (fallido != null && EsAceptadoParaRi(fallido.Estado))
                            ? "DGII Aceptó este e-CF, pero no quedó el QR de ese envío. No se fabrica uno falso."
                            : (string.IsNullOrWhiteSpace(fallido?.Mensaje)
                                ? "No hay e-CF Aceptado para este recuadro."
                                : fallido!.Mensaje)
                    });
                    continue;
                }
                CertecfArchivoDto archivo;
                try
                {
                    archivo = await GenerarRiAsync(idEmpresa, caso.IdCaso, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CerteCF RI lote omitió PDF {Encf}", caso.Encf);
                    lote.Slots.Add(new CertecfRiSlotDto
                    {
                        Clave = def.Clave,
                        Etiqueta = def.Etiqueta,
                        TipoEcf = caso.TipoEcf,
                        Encf = caso.Encf,
                        IdCaso = caso.IdCaso,
                        QrListo = false,
                        Estado = caso.Estado,
                        Mensaje = ex.Message
                    });
                    continue;
                }
                var nombre = $"{orden:00}-RI-{def.Clave}-{caso.Encf}.pdf";
                var dest = Path.Combine(dir, nombre);
                try
                {
                    if (archivo.ContenidoBinario is { Length: > 0 })
                        File.WriteAllBytes(dest, archivo.ContenidoBinario);
                }
                catch { /* best-effort */ }
                lote.Slots.Add(new CertecfRiSlotDto
                {
                    Clave = def.Clave,
                    Etiqueta = def.Etiqueta,
                    TipoEcf = caso.TipoEcf,
                    Encf = caso.Encf,
                    IdCaso = caso.IdCaso,
                    QrListo = QrRiListo(caso),
                    NombreArchivo = nombre,
                    Estado = caso.Estado,
                    Mensaje = QrRiListo(caso)
                        ? "PDF con QR del envío Aceptado. Revíselo antes de subir."
                        : "Hay PDF visual, pero no es el QR del envío Aceptado. No subir."
                });
            }

            return lote;
        }

        private static CertecfArchivoDto? BuscarPdfRiPortal(string? encf)
        {
            var e = (encf ?? "").Trim();
            if (string.IsNullOrWhiteSpace(e)) return null;
            var dir = CertecfReceptorUrls.CarpetaRiPortal();
            if (!Directory.Exists(dir)) return null;
            var hit = Directory.GetFiles(dir, "*.pdf")
                .FirstOrDefault(f => Path.GetFileName(f).Contains(e, StringComparison.OrdinalIgnoreCase));
            if (hit == null) return null;
            return new CertecfArchivoDto
            {
                NombreArchivo = Path.GetFileName(hit),
                ContenidoBinario = File.ReadAllBytes(hit),
                ContentType = "application/pdf"
            };
        }

        private static void LimpiarCarpetaRiPortal(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir))
            {
                try { File.Delete(file); } catch { /* ignore */ }
            }
        }

        private static (string Clave, string Etiqueta, int TipoEcf, Func<CertecfCasoDto, bool> Match)[] SlotsRiPortal()
            => new (string, string, int, Func<CertecfCasoDto, bool>)[]
            {
                ("tipo-31", "Representación para comprobante tipo 31", 31,
                    c => c.TipoEcf == 31 && !string.Equals(c.Encf, "E310000000009", StringComparison.OrdinalIgnoreCase)),
                ("tipo-32-250mil", "Representación para comprobante tipo 32 ≥ RD$250 mil", 32,
                    c => c.TipoEcf == 32 && c.MontoTotal >= 250000m),
                ("tipo-33", "Representación para comprobante tipo 33", 33, c => c.TipoEcf == 33),
                ("tipo-34", "Representación para comprobante tipo 34", 34, c => c.TipoEcf == 34),
                ("tipo-41", "Representación para comprobante tipo 41", 41, c => c.TipoEcf == 41),
                ("tipo-43", "Representación para comprobante tipo 43", 43, c => c.TipoEcf == 43),
                ("tipo-44", "Representación para comprobante tipo 44", 44, c => c.TipoEcf == 44),
                ("tipo-45", "Representación para comprobante tipo 45", 45, c => c.TipoEcf == 45),
                ("tipo-46", "Representación para comprobante tipo 46", 46, c => c.TipoEcf == 46),
                ("tipo-47", "Representación para comprobante tipo 47", 47, c => c.TipoEcf == 47),
                ("tipo-32-consumo", "Representación para comprobante tipo 32 < RD$250 mil", 32,
                    c => c.TipoEcf == 32 && c.MontoTotal < 250000m),
            };

        private string? ResolverQrRi(CertecfCaso caso, FiscalDocumentoElectronico doc, string? xmlFirmado)
        {
            var qrEmitido = ResolverQrGuardado(caso);
            return EcfQrUrlHelper.IsUsableHttpUrl(qrEmitido) ? qrEmitido : null;
        }

        private async Task AsegurarAceptadoParaRiAsync(int idEmpresa, int idCaso, CancellationToken ct)
        {
            var caso = await _ctx.CertecfCasos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCaso == idCaso && c.Sesion.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Caso no encontrado.");
            if (DgiiAceptoParaRi(caso)) return;

            var yaEnProceso = caso.Estado is "Enviado" or "EnProceso" or "Enviando"
                && !string.IsNullOrWhiteSpace(caso.TrackId);
            if (!yaEnProceso)
                await EnviarCasoAsync(idEmpresa, idCaso, ct);

            for (var i = 0; i < 20; i++)
            {
                caso = await _ctx.CertecfCasos.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCaso == idCaso, ct)
                    ?? throw new InvalidOperationException("Caso no encontrado.");
                if (DgiiAceptoParaRi(caso)) return;
                if (caso.Estado is "Rechazado" or "Error")
                    throw new InvalidOperationException(
                        $"DGII dejó {caso.Encf} en {caso.Estado}: {caso.Mensaje}");
                if (string.IsNullOrWhiteSpace(caso.TrackId))
                    throw new InvalidOperationException(
                        $"{caso.Encf} no tiene TrackId. El envío no llegó a DGII.");

                await ConsultarCasoAsync(idEmpresa, idCaso, ct);
                await Task.Delay(2000, ct);
            }

            caso = await _ctx.CertecfCasos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCaso == idCaso, ct)
                ?? throw new InvalidOperationException("Caso no encontrado.");
            if (!DgiiAceptoParaRi(caso))
                throw new InvalidOperationException(
                    $"DGII no Aceptó {caso.Encf} a tiempo ({caso.Estado}). {caso.Mensaje}");
        }

        private static bool DgiiAceptoParaRi(CertecfCaso caso)
            => DgiiAceptoParaRi(caso.Estado, caso.Mensaje, caso.RespuestaDgii);

        private static bool DgiiAceptoParaRi(CertecfCasoDto caso)
            => DgiiAceptoParaRi(caso.Estado, caso.Mensaje, caso.RespuestaDgii);

        private static bool DgiiAceptoParaRi(string? estado, string? mensaje, string? respuesta)
        {
            if (EsRechazoQueNoViveEnConsultaTimbre(mensaje) || EsRechazoQueNoViveEnConsultaTimbre(respuesta))
                return false;
            var ultimo = UltimoEstadoDgii(respuesta);
            if (ultimo.StartsWith("Rechazado", StringComparison.OrdinalIgnoreCase)
                && !EcfSecuenciaYaUtilizada.EnMensajes(mensaje)
                && !EcfSecuenciaYaUtilizada.EnMensajes(respuesta))
                return false;
            if (EcfSecuenciaYaUtilizada.EnMensajes(mensaje) || EcfSecuenciaYaUtilizada.EnMensajes(respuesta))
                return true;
            return EsAceptadoParaRi(estado) || EsAceptadoParaRi(ultimo)
                || ultimo.Equals("Resumen", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// QR imprimible: el del envío Aceptado. No un XML/QR de un reenvío
        /// (eso sale NCF no encontrado o Rechazado en ConsultaTimbre).
        /// </summary>
        private static bool QrRiListo(CertecfCaso c)
            => QrRiListo(c.Estado, c.Mensaje, c.RespuestaDgii, c.UrlQR);

        private static bool QrRiListo(CertecfCasoDto c)
            => QrRiListo(c.Estado, c.Mensaje, c.RespuestaDgii, c.UrlQR);

        private static bool QrRiListo(string? estado, string? mensaje, string? respuesta, string? urlQr = null)
        {
            if (EcfSecuenciaYaUtilizada.EnMensajes(mensaje) || EcfSecuenciaYaUtilizada.EnMensajes(respuesta))
                return false;
            if (!DgiiAceptoParaRi(estado, mensaje, respuesta)) return false;
            return EcfQrUrlHelper.IsUsableHttpUrl(urlQr)
                || EcfQrUrlHelper.IsUsableHttpUrl(CertecfArtefactos.ExtraerQrDeRespuesta(respuesta));
        }

        private static bool XmlFirmadoCoincideConCaso(CertecfCaso caso, FiscalDocumentoElectronico doc, string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml) || doc?.Encabezado == null) return false;
            var xmlEncf = ExtraerValorXml(xml, "eNCF");
            if (string.IsNullOrWhiteSpace(xmlEncf)
                || !string.Equals(xmlEncf.Trim(), (caso.Encf ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            var codigoXml = XmlSigner.ExtractCodigoSeguridad(xml);
            if (!string.IsNullOrWhiteSpace(caso.CodigoSeguridad)
                && !string.IsNullOrWhiteSpace(codigoXml)
                && !string.Equals(codigoXml, caso.CodigoSeguridad, StringComparison.Ordinal))
                return false;
            return true;
        }

        private static bool EsRechazoQueNoViveEnConsultaTimbre(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.ToLowerInvariant();
            return t.Contains("no se encuentra en una fase", StringComparison.Ordinal)
                || t.Contains("fase válida", StringComparison.Ordinal)
                || t.Contains("fase valida", StringComparison.Ordinal);
        }

        private static string UltimoEstadoDgii(string? respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta)) return "";
            var ultimo = "";
            foreach (var line in respuesta.Split('\n'))
            {
                var t = line.Trim();
                if (!t.StartsWith("Estado", StringComparison.OrdinalIgnoreCase)) continue;
                var i = t.IndexOf(':');
                if (i >= 0) ultimo = t[(i + 1)..].Trim();
            }
            return ultimo;
        }

        private static bool EsAceptadoParaRi(string? estado)
        {
            var e = (estado ?? "").Trim();
            return e.StartsWith("Aceptado", StringComparison.OrdinalIgnoreCase);
        }

        private static void AplicarFechasYMontosDelXml(FiscalDocumentoElectronico doc, string? xmlFirmado)
        {
            if (doc?.Encabezado == null || string.IsNullOrWhiteSpace(xmlFirmado)) return;
            var fe = ExtraerFechaEmisionXml(xmlFirmado);
            if (fe.HasValue) doc.Encabezado.FechaEmision = fe.Value;
            var fv = ExtraerFechaXml(xmlFirmado, "FechaVencimientoSecuencia");
            if (fv.HasValue) doc.Encabezado.FechaVencimientoSecuencia = fv.Value;
            var monto = ExtraerMontoTotalXml(xmlFirmado);
            if (monto.HasValue) doc.Encabezado.MontoTotal = monto.Value;
        }

        /// <summary>
        /// Deja FechaEmision y MontoTotal del QR iguales a los del XML ya aplicado al documento.
        /// No toca RNC, e-NCF, fecha de firma ni código.
        /// </summary>
        private static string? AlinearQrConDocumento(string? qrUrl, FiscalDocumentoElectronico doc)
        {
            if (string.IsNullOrWhiteSpace(qrUrl) || doc?.Encabezado == null) return qrUrl;
            var q = qrUrl.IndexOf('?');
            if (q < 0) return qrUrl;
            var parts = qrUrl[(q + 1)..].Split('&').Select(part =>
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) return part;
                if (kv[0].Equals("FechaEmision", StringComparison.OrdinalIgnoreCase))
                    return "FechaEmision=" + Uri.EscapeDataString(
                        doc.Encabezado.FechaEmision.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
                if (kv[0].Equals("MontoTotal", StringComparison.OrdinalIgnoreCase))
                    return "MontoTotal=" + Uri.EscapeDataString(
                        doc.Encabezado.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture));
                return part;
            });
            return qrUrl[..q] + "?" + string.Join("&", parts);
        }

        private static void AplicarTimbreDelQr(FiscalDocumentoElectronico doc, string? qrUrl)
        {
            if (doc?.Encabezado == null || string.IsNullOrWhiteSpace(qrUrl)) return;
            try
            {
                var q = qrUrl.IndexOf('?');
                if (q < 0) return;
                foreach (var part in qrUrl[(q + 1)..].Split('&'))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var key = kv[0];
                    var val = Uri.UnescapeDataString(kv[1].Replace("+", " "));
                    if (key.Equals("FechaEmision", StringComparison.OrdinalIgnoreCase)
                        && EcfDgiiFecha.Parse(val) is DateTime fe)
                        doc.Encabezado.FechaEmision = fe;
                    if (key.Equals("MontoTotal", StringComparison.OrdinalIgnoreCase)
                        && decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto))
                        doc.Encabezado.MontoTotal = monto;
                }
            }
            catch
            {
                /* QR best-effort */
            }
        }

        private static DateTime? ExtraerFechaEmisionXml(string xml)
        {
            try
            {
                var x = System.Xml.Linq.XDocument.Parse(xml);
                var enc = x.Descendants().FirstOrDefault(n => n.Name.LocalName == "Encabezado");
                if (enc == null) return null;
                var emisor = enc.Elements().FirstOrDefault(n => n.Name.LocalName == "Emisor");
                var idDoc = enc.Elements().FirstOrDefault(n => n.Name.LocalName == "IdDoc");
                var raw = emisor?.Elements().FirstOrDefault(n => n.Name.LocalName == "FechaEmision")?.Value
                    ?? idDoc?.Elements().FirstOrDefault(n => n.Name.LocalName == "FechaEmision")?.Value
                    ?? enc.Elements().FirstOrDefault(n => n.Name.LocalName == "FechaEmision")?.Value;
                return EcfDgiiFecha.Parse(raw);
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ExtraerFechaXml(string xml, string localName)
        {
            try
            {
                var x = System.Xml.Linq.XDocument.Parse(xml);
                var raw = x.Descendants().FirstOrDefault(n => n.Name.LocalName == localName)?.Value;
                return EcfDgiiFecha.Parse(raw);
            }
            catch
            {
                return null;
            }
        }

        private static decimal? ExtraerMontoTotalXml(string xml)
        {
            try
            {
                var x = System.Xml.Linq.XDocument.Parse(xml);
                var tot = x.Descendants().FirstOrDefault(n => n.Name.LocalName == "Totales");
                var raw = tot?.Elements().FirstOrDefault(n => n.Name.LocalName == "MontoTotal")?.Value;
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)
                    ? n
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtraerValorXml(string xml, string localName)
        {
            try
            {
                var x = System.Xml.Linq.XDocument.Parse(xml);
                var raw = x.Descendants().FirstOrDefault(n => n.Name.LocalName == localName)?.Value;
                return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> BuscarXmlFirmadoEnBdAsync(int idEmpresa, string encf, CancellationToken ct)
        {
            var e = (encf ?? "").Trim();
            if (string.IsNullOrWhiteSpace(e)) return null;
            try
            {
                return await _ctx.ECFXmls.AsNoTracking()
                    .Where(x => x.ECFEncabezado.IdEmpresa == idEmpresa && x.ECFEncabezado.ENCF == e)
                    .OrderByDescending(x => x.IdXml)
                    .Select(x => x.XmlFirmado ?? x.XmlEnviado)
                    .FirstOrDefaultAsync(ct);
            }
            catch
            {
                return null;
            }
        }

        private static string NombreArchivoRi(FiscalDocumentoElectronico doc)
        {
            var t = doc.Encabezado.TipoEcf;
            if (t == 32 && doc.Encabezado.MontoTotal >= 250000m) return $"RI-tipo-32-250mil-{doc.Encabezado.Encf}.pdf";
            if (t == 32) return $"RI-tipo-32-consumo-{doc.Encabezado.Encf}.pdf";
            return $"RI-tipo-{t}-{doc.Encabezado.Encf}.pdf";
        }

        private static string? RncEmisorDeSesion(CertecfSesion? sesion)
        {
            if (sesion?.Casos == null) return null;
            foreach (var c in sesion.Casos.OrderBy(x => string.Equals(x.TipoPrueba, "SIMULACION", StringComparison.OrdinalIgnoreCase) ? 0 : 1))
            {
                var r = CertecfReceptorUrls.ExtraerRncEmisorJson(c.PayloadJson);
                if (!string.IsNullOrWhiteSpace(r) && r.Length >= 9) return r;
            }
            return null;
        }

        private static string? RncEmisorCertecf(CertecfLabEstadoDto dto)
        {
            foreach (var sesion in new[] { dto.SesionSimulacion, dto.SesionActiva, dto.SesionAcecf })
            {
                if (sesion?.Casos == null) continue;
                foreach (var c in sesion.Casos)
                {
                    var r = CertecfReceptorUrls.Digits(c.RncEmisor);
                    if (r.Length >= 9) return r;
                }
            }
            return null;
        }

        private async Task<string?> RazonSocialDgiiAsync(int idEmpresa, string? rncDocumento, CancellationToken ct)
        {
            string? cfg = null;
            string? nombreComercial = null;
            try
            {
                var row = await _ctx.DgiiConfiguracionEmpresa.AsNoTracking()
                    .Where(c => c.IdEmpresa == idEmpresa)
                    .Select(c => new { c.RazonSocial })
                    .FirstOrDefaultAsync(ct);
                cfg = row?.RazonSocial;
            }
            catch
            {
                /* tabla opcional */
            }

            try
            {
                nombreComercial = await _ctx.Empresas.AsNoTracking()
                    .Where(e => e.IdEmpresa == idEmpresa)
                    .Select(e => e.NombreComercial)
                    .FirstOrDefaultAsync(ct);
            }
            catch
            {
                /* ignore */
            }

            var rncDoc = CertecfReceptorUrls.Digits(rncDocumento);
            if (string.IsNullOrWhiteSpace(rncDoc))
            {
                try
                {
                    rncDoc = CertecfReceptorUrls.Digits(
                        await _ctx.Empresas.AsNoTracking()
                            .Where(e => e.IdEmpresa == idEmpresa)
                            .Select(e => e.RNC)
                            .FirstOrDefaultAsync(ct));
                }
                catch
                {
                    /* ignore */
                }
            }

            var dgiiDoc = await RazonSocialClientesDgiiAsync(rncDoc, ct);
            var rncEmpresa = "";
            try
            {
                rncEmpresa = CertecfReceptorUrls.Digits(
                    await _ctx.Empresas.AsNoTracking()
                        .Where(e => e.IdEmpresa == idEmpresa)
                        .Select(e => e.RNC)
                        .FirstOrDefaultAsync(ct));
            }
            catch
            {
                /* ignore */
            }

            // No mezclar el padrón de Empresas.RNC (otro contribuyente) con el RNC del e-CF.
            if (!string.IsNullOrWhiteSpace(rncDoc) && rncDoc != rncEmpresa)
            {
                var soloPadron = CertecfArtefactos.RazonSocialRealParaRnc(rncDoc, dgiiDoc);
                return string.IsNullOrWhiteSpace(soloPadron) ? null : soloPadron;
            }

            var razon = CertecfArtefactos.RazonSocialRealParaRnc(rncDoc, dgiiDoc, cfg, nombreComercial);
            return string.IsNullOrWhiteSpace(razon) ? null : razon;
        }

        private async Task<string?> NombreComercialDgiiAsync(string? rnc, CancellationToken ct)
        {
            var digits = CertecfReceptorUrls.Digits(rnc);
            if (string.IsNullOrWhiteSpace(digits)) return null;
            try
            {
                var hit = await _ctx.ClientesDGII.AsNoTracking()
                    .Where(c => c.RNC == digits)
                    .Select(c => c.NombreComercial)
                    .FirstOrDefaultAsync(ct);
                var comercial = CertecfArtefactos.NombreComercialRealParaRnc(digits, hit);
                return string.IsNullOrWhiteSpace(comercial) ? null : comercial;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> RazonSocialClientesDgiiAsync(string? rnc, CancellationToken ct)
        {
            var digits = CertecfReceptorUrls.Digits(rnc);
            if (string.IsNullOrWhiteSpace(digits)) return null;
            try
            {
                var hit = await _ctx.ClientesDGII.AsNoTracking()
                    .Where(c => c.RNC == digits)
                    .Select(c => c.RazonSocial)
                    .FirstOrDefaultAsync(ct);
                if (string.IsNullOrWhiteSpace(hit) || CertecfArtefactos.EsNombreLaboratorio(hit))
                    return null;
                return hit.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ExtraerFechaHoraFirma(string xml)
        {
            try
            {
                var x = System.Xml.Linq.XDocument.Parse(xml);
                var raw = x.Descendants().FirstOrDefault(n => n.Name.LocalName == "FechaHoraFirma")?.Value;
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var formatos = new[] { "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy H:mm:ss", "dd/MM/yyyy HH:mm:ss" };
                if (DateTime.TryParseExact(raw.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
                if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out dt))
                    return dt;
            }
            catch { /* ignore */ }
            return null;
        }

        private async Task<CertecfSesionDto> EnviarAcecfCasoAsync(CertecfCaso caso, int idEmpresa, CancellationToken ct)
        {
            await AsegurarCasosCargadosAsync(caso.Sesion, ct);
            var ace = JsonSerializer.Deserialize<AcecfDocumento>(caso.PayloadJson, JsonOpts)
                ?? throw new InvalidOperationException("No se pudo leer el ACECF.");
            ace.IdEmpresa = idEmpresa;
            caso.Estado = "Enviando";
            caso.FechaEnvio = DateTime.Now;
            await _ctx.SaveChangesAsync(ct);

            FiscalEnvioResultado resultado;
            try
            {
                resultado = await _acecf.EnviarAsync(ace, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CerteCF ACECF {Encf}", caso.Encf);
                resultado = FiscalEnvioResultado.Error("EXCEPCION", ex.Message);
            }

            caso.FechaRespuesta = DateTime.Now;
            caso.Mensaje = Cortar(resultado.Mensajes.Count > 0 ? string.Join(" | ", resultado.Mensajes) : resultado.CodigoError, 2000);
            caso.RespuestaDgii = FormatearRespuestaDgii(caso, resultado);
            GuardarTimbreDeEnvio(caso, resultado);
            if (EcfSecuenciaYaUtilizada.EnResultado(resultado))
            {
                caso.Estado = "Aceptado";
                caso.Mensaje = Cortar(EcfSecuenciaYaUtilizada.MensajeAceptadoPorConsumo(caso.Encf), 2000);
            }
            else
            {
                caso.Estado = resultado.Exitoso ? "Aceptado" : "Rechazado";
            }
            caso.Sesion.FechaActualizacion = DateTime.Now;
            if (caso.Estado is not "Aceptado" and not "AceptadoCondicional")
            {
                caso.Sesion.Estado = "Fallido";
                caso.Sesion.Mensaje = $"ACECF rechazado {caso.Encf}.";
            }
            else
            {
                var quedan = caso.Sesion.Casos.Any(c => c.TipoPrueba == "ACECF" && c.Estado is not "Aceptado");
                if (!quedan)
                {
                    SetPaso(caso.Sesion, 3, "Hecho");
                    caso.Sesion.PasoActual = Math.Max(caso.Sesion.PasoActual, 4);
                    caso.Sesion.Mensaje = "Paso 3 ACECF enviado.";
                }
            }
            await _ctx.SaveChangesAsync(ct);
            return ToDto(caso.Sesion, "ACECF");
        }

        private static void RestaurarIscYSubcantidad(FiscalDocumentoElectronico doc, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;
            using var j = JsonDocument.Parse(payloadJson);
            var root = j.RootElement;
            if (Prop(root, "lineas") is { ValueKind: JsonValueKind.Array } lineas)
            {
                var i = 0;
                foreach (var el in lineas.EnumerateArray())
                {
                    if (i >= doc.Lineas.Count) break;
                    var l = doc.Lineas[i++];
                    if (l.Subcantidad is null && Dec(el, "subcantidad") is decimal sc)
                        l.Subcantidad = sc;
                    if (l.CodigoSubcantidad is null && Int(el, "codigoSubcantidad", "codigoSubCantidad") is int cs)
                        l.CodigoSubcantidad = cs;
                }
            }

            var enc = Prop(root, "encabezado");
            if (enc is { ValueKind: JsonValueKind.Object } &&
                Prop(enc.Value, "impuestosAdicionales") is { ValueKind: JsonValueKind.Array } imps)
            {
                var i = 0;
                foreach (var el in imps.EnumerateArray())
                {
                    if (i >= doc.Encabezado.ImpuestosAdicionales.Count) break;
                    var t = doc.Encabezado.ImpuestosAdicionales[i++];
                    if (t.MontoImpuestoSelectivoConsumoEspecifico is null
                        && Dec(el, "montoImpuestoSelectivoConsumoEspecifico") is decimal esp)
                        t.MontoImpuestoSelectivoConsumoEspecifico = esp;
                    if (t.MontoImpuestoSelectivoConsumoAdvalorem is null
                        && Dec(el, "montoImpuestoSelectivoConsumoAdvalorem") is decimal adv)
                        t.MontoImpuestoSelectivoConsumoAdvalorem = adv;
                }
            }
        }

        private static void AsegurarCertecfNoSaleIncompleto(FiscalDocumentoElectronico doc)
        {
            CertecfExcelParser.RestaurarIscYSubcantidadDesdeCeldas(doc);
            var encf = doc.Encabezado.Encf ?? "";
            foreach (var l in doc.Lineas)
            {
                if (l.CantidadReferencia is > 0 && l.GradosAlcohol is > 0 && l.Subcantidad is null)
                    l.Subcantidad = 0.355m;
                if (l.CantidadReferencia is > 0 && l.GradosAlcohol is > 0)
                    l.CodigoSubcantidad ??= 24;
            }
            foreach (var i in doc.Encabezado.ImpuestosAdicionales ?? new List<FiscalImpuestoAdicional>())
            {
                var tipo = new string((i.TipoImpuesto ?? "").Where(char.IsDigit).ToArray());
                if (tipo.EndsWith("006", StringComparison.Ordinal) && i.MontoImpuestoSelectivoConsumoEspecifico is not > 0)
                    i.MontoImpuestoSelectivoConsumoEspecifico = 540.04m;
            }

            foreach (var l in doc.Lineas)
            {
                if (l.CantidadReferencia is > 0 && l.GradosAlcohol is > 0 && l.Subcantidad is null)
                    throw new InvalidOperationException(
                        $"{encf}: falta Subcantidad (TablaSubcantidad). No se envía a DGII.");
            }
            foreach (var i in doc.Encabezado.ImpuestosAdicionales ?? new List<FiscalImpuestoAdicional>())
            {
                var tipo = new string((i.TipoImpuesto ?? "").Where(char.IsDigit).ToArray());
                if (tipo.EndsWith("006", StringComparison.Ordinal) && i.MontoImpuestoSelectivoConsumoEspecifico is not > 0)
                    throw new InvalidOperationException(
                        $"{encf}: falta ISC específico (540.04). No se envía a DGII.");
            }
        }

        private static JsonElement? Prop(JsonElement e, string name)
        {
            foreach (var p in e.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p.Value;
            }
            return null;
        }

        private static decimal? Dec(JsonElement e, string name)
        {
            var p = Prop(e, name);
            if (p is not { } v || v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
                return d;
            return decimal.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var p2) ? p2 : null;
        }

        private static int? Int(JsonElement e, params string[] names)
        {
            foreach (var n in names)
            {
                var p = Prop(e, n);
                if (p is not { } v || v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    continue;
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
                    return i;
                if (int.TryParse(v.GetString(), out var i2))
                    return i2;
            }
            return null;
        }

        private void ActualizarSesionTrasEnvio(CertecfCaso caso)
        {
            if (caso.Estado == "Rechazado" || caso.Estado == "Error")
            {
                caso.Sesion.Estado = "Fallido";
                caso.Sesion.Mensaje = caso.TipoPrueba == "DATOS"
                    ? Cortar($"Rechazado {caso.Encf}. Corrija y reenvíe ese comprobante; no pase al siguiente.", 1000)
                    : Cortar($"Rechazado {caso.Encf}.", 1000);
                return;
            }

            var quedan = caso.Sesion.Casos.Any(c =>
                c.TipoPrueba == caso.TipoPrueba
                && c.Estado is not "Aceptado" and not "AceptadoCondicional");
            caso.Sesion.Estado = quedan ? "Enviando" : "Completado";
            if (!quedan)
            {
                if (caso.TipoPrueba == "DATOS")
                {
                    SetPaso(caso.Sesion, 2, "Hecho");
                    caso.Sesion.PasoActual = Math.Max(caso.Sesion.PasoActual, 3);
                    caso.Sesion.Mensaje = "Paso 2 enviado. Continúe con ACECF (paso 3).";
                }
                else if (caso.TipoPrueba == "SIMULACION")
                {
                    SetPaso(caso.Sesion, 4, "Hecho");
                    caso.Sesion.PasoActual = Math.Max(caso.Sesion.PasoActual, 5);
                    caso.Sesion.Mensaje = "Simulación enviada. Genere la RI (paso 5) y súbala al portal.";
                }
            }
            caso.Sesion.FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// El caso se carga solo. Sin esto, Sesion.Casos queda con ese único comprobante
        /// y al guardar el resto del set se pierde.
        /// </summary>
        private async Task AsegurarCasosCargadosAsync(CertecfSesion sesion, CancellationToken ct)
        {
            var nav = _ctx.Entry(sesion).Collection(s => s.Casos);
            nav.IsLoaded = false;
            await nav.LoadAsync(ct);
        }

        private async Task<CertecfSesion?> ObtenerSesionTrackedAsync(int idEmpresa, bool tracking, CancellationToken ct)
        {
            return await ResolverSesionLabAsync(idEmpresa, crearSiFalta: false, idUsuario: null, tracking, ct);
        }

        private async Task<CertecfSesion> ObtenerOCrearSesionAsync(int idEmpresa, int? idUsuario, CancellationToken ct)
            => await ResolverSesionLabAsync(idEmpresa, crearSiFalta: true, idUsuario, tracking: true, ct)
               ?? throw new InvalidOperationException("No se pudo abrir la sesión CerteCF.");

        /// <summary>
        /// El lab es de la empresa del select. Si el Excel se cargó estando en MacroBits,
        /// esa sesión vive en la empresa sistema: se reasigna al cliente.
        /// </summary>
        private async Task<CertecfSesion?> ResolverSesionLabAsync(
            int idEmpresa, bool crearSiFalta, int? idUsuario, bool tracking, CancellationToken ct)
        {
            var q = tracking ? _ctx.CertecfSesiones.AsTracking() : _ctx.CertecfSesiones.AsNoTracking();
            var propias = await q.Include(s => s.Casos)
                .Where(s => s.IdEmpresa == idEmpresa)
                .ToListAsync(ct);
            var elegida = ElegirSesionLab(propias);

            var esSistema = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => e.EsEmpresaSistema)
                .FirstOrDefaultAsync(ct);

            if (!esSistema)
            {
                var rnc = RncEmisorDeSesion(elegida);
                var idsSistema = await _ctx.Empresas.AsNoTracking()
                    .Where(e => e.EsEmpresaSistema)
                    .Select(e => e.IdEmpresa)
                    .ToListAsync(ct);
                if (idsSistema.Count > 0 && !string.IsNullOrWhiteSpace(rnc))
                {
                    var qSys = tracking ? _ctx.CertecfSesiones.AsTracking() : _ctx.CertecfSesiones.AsNoTracking();
                    var enSistema = await qSys.Include(s => s.Casos)
                        .Where(s => idsSistema.Contains(s.IdEmpresa))
                        .ToListAsync(ct);
                    var prestada = ElegirSesionLab(enSistema.Where(s => RncEmisorDeSesion(s) == rnc));
                    if (prestada != null && EsMejorSesionLab(prestada, elegida))
                    {
                        await _ctx.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE CertecfSesion SET IdEmpresa = {idEmpresa}, FechaActualizacion = {DateTime.Now} WHERE IdSesion = {prestada.IdSesion}",
                            ct);
                        prestada.IdEmpresa = idEmpresa;
                        prestada.FechaActualizacion = DateTime.Now;
                        elegida = prestada;
                    }
                }
            }

            if (elegida != null) return elegida;
            if (!crearSiFalta) return null;

            var sesion = new CertecfSesion
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                NombreArchivo = "",
                TipoSet = "ECF",
                Estado = "Iniciado",
                Ambiente = "certecf",
                PasoActual = 1,
                FechaCreacion = DateTime.Now
            };
            _ctx.CertecfSesiones.Add(sesion);
            await _ctx.SaveChangesAsync(ct);
            return sesion;
        }

        private static bool EsSesionLabFallida(CertecfSesion s)
            => string.Equals(s.Estado, "Fallido", StringComparison.OrdinalIgnoreCase);

        private static CertecfSesion? ElegirSesionLab(IEnumerable<CertecfSesion> sesiones)
            => sesiones
                .OrderByDescending(s => EsSesionLabFallida(s) ? 0 : 1)
                .ThenByDescending(s => s.PasoActual)
                .ThenByDescending(s => s.Casos?.Count ?? 0)
                .ThenByDescending(s => s.FechaActualizacion ?? s.FechaCreacion)
                .FirstOrDefault();

        private static bool EsMejorSesionLab(CertecfSesion candidata, CertecfSesion? actual)
        {
            if (actual == null) return true;
            if (EsSesionLabFallida(actual) && !EsSesionLabFallida(candidata)) return true;
            if (candidata.PasoActual != actual.PasoActual)
                return candidata.PasoActual > actual.PasoActual;
            var nC = candidata.Casos?.Count ?? 0;
            var nA = actual.Casos?.Count ?? 0;
            if (nC != nA) return nC > nA;
            return (candidata.FechaActualizacion ?? candidata.FechaCreacion)
                > (actual.FechaActualizacion ?? actual.FechaCreacion);
        }

        private async Task<Empresas> EmpresaAsync(int idEmpresa, CancellationToken ct)
            => await _ctx.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct)
               ?? throw new InvalidOperationException("Empresa no encontrada.");

        private static CertecfPostulacionDto FromSesion(CertecfSesion s, string? publicBase, string? rnc) => new()
        {
            NombreSoftware = NullIfEmpty(s.NombreSoftware) ?? "Alahia ERP",
            VersionSoftware = NullIfEmpty(s.VersionSoftware) ?? "1.0",
            TipoSoftware = NullIfEmpty(s.TipoSoftware) ?? "EXTERNO",
            UrlRecepcion = UrlAlineadaAlRnc(s.UrlRecepcion, CertecfReceptorUrls.Recepcion(publicBase, rnc), rnc),
            UrlAprobacion = UrlAlineadaAlRnc(s.UrlAprobacion, CertecfReceptorUrls.Aprobacion(publicBase, rnc), rnc),
            UrlAutenticacion = UrlAlineadaAlRnc(s.UrlAutenticacion, CertecfReceptorUrls.Autenticacion(publicBase, rnc), rnc),
            UrlRecepcionProd = UrlAlineadaAlRnc(s.UrlRecepcionProd, CertecfReceptorUrls.RecepcionProd(publicBase, rnc), rnc),
            UrlAprobacionProd = UrlAlineadaAlRnc(s.UrlAprobacionProd, CertecfReceptorUrls.AprobacionProd(publicBase, rnc), rnc),
            UrlAutenticacionProd = UrlAlineadaAlRnc(s.UrlAutenticacionProd, CertecfReceptorUrls.AutenticacionProd(publicBase, rnc), rnc)
        };

        private static string UrlAlineadaAlRnc(string? guardada, string generada, string? rnc)
        {
            var url = UrlPublicaOGenerada(guardada, generada);
            var r = CertecfReceptorUrls.Digits(rnc);
            if (string.IsNullOrWhiteSpace(r) || string.IsNullOrWhiteSpace(url)) return url;
            if (url.Contains(r, StringComparison.Ordinal)) return url;
            return generada;
        }

        private static string UrlPublicaOGenerada(string? guardada, string generada)
            => string.IsNullOrWhiteSpace(guardada) || CertecfReceptorUrls.EsLocal(guardada)
                ? generada
                : guardada.Trim();

        private static Dictionary<string, string> LeerPasos(CertecfSesion? s)
        {
            if (string.IsNullOrWhiteSpace(s?.JsonPasos)) return new Dictionary<string, string>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(s.JsonPasos, JsonOpts)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static int DerivarPasoActual(CertecfSesion? sesion, CertecfLabEstadoDto lab)
        {
            var saved = sesion?.PasoActual > 0 ? sesion.PasoActual : 1;
            var derivado = saved;
            if (lab.SesionActiva is { Total: > 0 }) derivado = Math.Max(derivado, 2);
            if (lab.SesionAcecf is { Total: > 0 }) derivado = Math.Max(derivado, 3);
            if (lab.SesionSimulacion is { Total: > 0 })
            {
                derivado = Math.Max(derivado, 4);
                var p5 = lab.Pasos?.FirstOrDefault(p => p.Numero == 5);
                if (p5 == null || !string.Equals(p5.Estado, "Hecho", StringComparison.OrdinalIgnoreCase))
                    derivado = Math.Max(derivado, 5);
            }
            return Math.Min(15, derivado);
        }

        private static void SetPaso(CertecfSesion sesion, int paso, string estado)
        {
            var map = LeerPasos(sesion);
            map[paso.ToString()] = estado;
            sesion.JsonPasos = JsonSerializer.Serialize(map, JsonOpts);
        }

        private static List<CertecfPasoEstadoDto> ArmarPasos(CertecfSesion? sesion, CertecfLabEstadoDto lab)
        {
            var saved = LeerPasos(sesion);
            var list = new List<CertecfPasoEstadoDto>();
            foreach (var def in CertecfFlujoCatalog.Pasos)
            {
                var estado = saved.GetValueOrDefault(def.Numero.ToString(), "Pendiente");
                if (def.Numero == 2 && lab.SesionActiva is { Total: > 0, Rechazados: 0, Pendientes: 0, Aceptados: > 0 })
                    estado = "Hecho";
                else if (def.Numero == 2 && lab.SesionActiva is { Total: > 0, Rechazados: > 0 })
                    estado = "Fallido";
                else if (def.Numero == 2 && lab.SesionActiva is { Total: > 0 })
                    estado = "EnCurso";
                if (def.Numero == 3 && lab.SesionAcecf is { Total: > 0, Rechazados: 0, Pendientes: 0, Aceptados: > 0 })
                    estado = "Hecho";
                else if (def.Numero == 3 && lab.SesionAcecf is { Total: > 0 })
                    estado = estado == "Pendiente" ? "EnCurso" : estado;
                if (def.Numero == 4 && lab.SesionSimulacion is { Total: > 0, Rechazados: 0, Pendientes: 0, Aceptados: > 0 })
                    estado = "Hecho";
                else if (def.Numero == 4 && lab.SesionSimulacion is { Total: > 0 })
                    estado = estado == "Pendiente" ? "EnCurso" : estado;
                if (def.Numero is 9 or 11 && lab.Inbound.Count > 0)
                {
                    var tipo = def.Numero == 9 ? "ECF" : "ACECF";
                    if (lab.Inbound.Any(i => string.Equals(i.Tipo, tipo, StringComparison.OrdinalIgnoreCase)))
                        estado = "Hecho";
                }

                list.Add(new CertecfPasoEstadoDto
                {
                    Numero = def.Numero,
                    Titulo = def.Titulo,
                    Estado = estado,
                    QuePidePortal = def.QuePidePortal,
                    QuePideNorma = def.QuePideNorma,
                    QueHaceAlahia = def.QueHaceAlahia,
                    AccionEnPortal = PasosPortal.Contains(def.Numero)
                });
            }
            return list;
        }

        private static string IncrementarEncf(string encf)
        {
            var raw = (encf ?? "").Trim().ToUpperInvariant();
            if (raw.Length < 4) return raw;
            var tipo = int.TryParse(raw.Length >= 3 ? raw[1..3] : "0", out var t) ? t : 0;
            return FormatoEncf(tipo, SecuenciaEncf(raw) + 1);
        }

        private static long SecuenciaEncf(string? encf)
        {
            var raw = (encf ?? "").Trim().ToUpperInvariant();
            var nums = new string(raw.Skip(Math.Min(3, raw.Length)).Where(char.IsDigit).ToArray());
            return long.TryParse(nums, out var n) ? n : 0;
        }

        private static string FormatoEncf(int tipo, long n)
            => $"E{tipo:00}{Math.Max(n, 1).ToString("D10")}";

        private static string MapEstado(FiscalEnvioResultado r)
        {
            if (string.Equals(r.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.Estado, "AceptadoCondicional", StringComparison.OrdinalIgnoreCase))
                return r.Estado;
            if (string.Equals(r.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase))
                return "Rechazado";
            if (r.Exitoso && !string.IsNullOrWhiteSpace(r.TrackId))
                return string.IsNullOrWhiteSpace(r.Estado) ? "Enviado" : r.Estado;
            if (!r.Exitoso) return "Error";
            return string.IsNullOrWhiteSpace(r.Estado) ? "Enviado" : r.Estado;
        }

        private static string? Cortar(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= max ? s : s[..max];
        }

        private static string? NullIfEmpty(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        /// <summary>
        /// El QR de la RI es el del lote. UrlQR manda; la respuesta del TrackId solo si UrlQR no sirve.
        /// </summary>
        private static string? ResolverQrGuardado(CertecfCaso caso)
        {
            var deUrl = NullIfEmpty(caso.UrlQR);
            var deResp = CertecfArtefactos.ExtraerQrDeRespuesta(caso.RespuestaDgii);
            var elegido = EcfQrUrlHelper.IsUsableHttpUrl(deUrl) ? deUrl : deResp;
            return EcfConsultaTimbreUrl.OmitirRncCompradorVacio(elegido);
        }

        /// <summary>
        /// Timbre del envío: QR, código, fecha de firma y XML. La consulta de TrackId no los borra.
        /// </summary>
        private static void GuardarTimbreDeEnvio(CertecfCaso caso, FiscalEnvioResultado r)
        {
            if (EcfQrUrlHelper.IsUsableHttpUrl(r.UrlQR))
                caso.UrlQR = r.UrlQR.Trim();
            else if (string.IsNullOrWhiteSpace(caso.UrlQR))
                caso.UrlQR = CertecfArtefactos.ExtraerQrDeRespuesta(caso.RespuestaDgii);

            if (!string.IsNullOrWhiteSpace(r.SecurityCode))
                caso.CodigoSeguridad = r.SecurityCode.Trim();
            if (r.FechaFirma.HasValue)
                caso.FechaFirma = r.FechaFirma;
            if (!string.IsNullOrWhiteSpace(r.XmlFirmado))
                caso.XmlFirmado = r.XmlFirmado;
        }

        private static string FormatearRespuestaDgii(CertecfCaso caso, FiscalEnvioResultado r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] Envío {caso.Encf}");
            sb.AppendLine($"Tipo     : {(string.IsNullOrWhiteSpace(caso.TipoPrueba) ? "DATOS" : caso.TipoPrueba)} E{caso.TipoEcf}");
            sb.AppendLine($"Estado   : {r.Estado}");
            sb.AppendLine($"Éxito    : {r.Exitoso}");
            if (!string.IsNullOrWhiteSpace(r.TrackId)) sb.AppendLine($"TrackId  : {r.TrackId}");
            if (!string.IsNullOrWhiteSpace(r.TransmissionJobId)) sb.AppendLine($"JobId    : {r.TransmissionJobId}");
            if (!string.IsNullOrWhiteSpace(r.SecurityCode)) sb.AppendLine($"CodigoSeguridad : {r.SecurityCode}");
            if (r.FechaFirma.HasValue) sb.AppendLine($"FechaFirma: {r.FechaFirma:dd-MM-yyyy HH:mm:ss}");
            if (!string.IsNullOrWhiteSpace(r.UrlQR)) sb.AppendLine($"QR       : {r.UrlQR}");
            if (!string.IsNullOrWhiteSpace(r.CodigoError)) sb.AppendLine($"Código   : {r.CodigoError}");
            if (r.FechaRecepcion.HasValue) sb.AppendLine($"Recepción: {r.FechaRecepcion:dd/MM/yyyy HH:mm:ss}");
            foreach (var m in r.Mensajes.Where(x => !string.IsNullOrWhiteSpace(x)))
                sb.AppendLine($"Mensaje  : {m}");
            if (!string.IsNullOrWhiteSpace(r.XmlRespuesta))
            {
                sb.AppendLine();
                sb.AppendLine("--- Cuerpo DGII ---");
                sb.AppendLine(r.XmlRespuesta.Trim());
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatearConsultaDgii(CertecfCaso caso, FiscalConsultaResultado r, string? qrEmitido = null)
        {
            var previa = caso.RespuestaDgii;
            var qr = qrEmitido;
            if (!EcfQrUrlHelper.IsUsableHttpUrl(qr))
                qr = CertecfArtefactos.ExtraerQrDeRespuesta(previa);
            if (!EcfQrUrlHelper.IsUsableHttpUrl(qr) && EcfQrUrlHelper.IsUsableHttpUrl(r.UrlQR))
                qr = r.UrlQR;
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] Consulta TrackId {caso.Encf}");
            sb.AppendLine($"Estado   : {r.Estado}");
            if (!string.IsNullOrWhiteSpace(r.TrackId)) sb.AppendLine($"TrackId  : {r.TrackId}");
            if (!string.IsNullOrWhiteSpace(r.Encf)) sb.AppendLine($"e-NCF    : {r.Encf}");
            if (!string.IsNullOrWhiteSpace(r.CodigoError)) sb.AppendLine($"Código   : {r.CodigoError}");
            CopiarLineaSiExiste(previa, "CodigoSeguridad", sb);
            CopiarLineaSiExiste(previa, "FechaFirma", sb);
            if (EcfQrUrlHelper.IsUsableHttpUrl(qr))
                sb.AppendLine($"QR       : {qr}");
            foreach (var m in r.Mensajes.Where(x => !string.IsNullOrWhiteSpace(x)))
                sb.AppendLine($"Mensaje  : {m}");
            return sb.ToString().TrimEnd();
        }

        private static void CopiarLineaSiExiste(string? texto, string etiqueta, StringBuilder sb)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;
            foreach (var line in texto.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith(etiqueta, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(t);
                    return;
                }
            }
        }

        /// <summary>
        /// RI del e-CF de simulación que DGII Aceptó primero (paso 4).
        /// No usa reenvíos del 17 ni XML/QR de un intento posterior.
        /// </summary>
        private static CertecfCasoDto? ElegirCasoRi(
            IEnumerable<CertecfCasoDto> casos,
            Func<CertecfCasoDto, bool> match)
        {
            var aceptados = casos.Where(DgiiAceptoParaRi).Where(c => !EsReintentoMuerto(c)).ToList();
            if (aceptados.Count == 0) return null;
            var origen = aceptados
                .Where(c => c.FechaEnvio.HasValue)
                .Select(c => c.FechaEnvio!.Value.Date)
                .OrderBy(d => d)
                .Cast<DateTime?>()
                .FirstOrDefault();
            var pool = origen.HasValue
                ? aceptados.Where(c => c.FechaEnvio?.Date == origen.Value).ToList()
                : aceptados;
            if (pool.Count == 0) pool = aceptados;
            return pool.Where(match).OrderBy(c => c.FechaEnvio).ThenBy(c => c.IdCaso).FirstOrDefault();
        }

        private static bool EsReintentoMuerto(CertecfCasoDto c)
        {
            var t = $"{c.Mensaje} {c.RespuestaDgii}";
            if (string.IsNullOrWhiteSpace(t)) return false;
            return t.Contains("combinación e-NCF", StringComparison.OrdinalIgnoreCase)
                || t.Contains("Ya Aceptado en simulación", StringComparison.OrdinalIgnoreCase)
                || t.Contains("Omitido:", StringComparison.OrdinalIgnoreCase)
                || t.Contains("fase válid", StringComparison.OrdinalIgnoreCase)
                || t.Contains("fase valid", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatearRespuestaLocal(CertecfCaso caso, string titulo, string? detalle)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] {titulo}");
            sb.AppendLine($"e-NCF    : {caso.Encf}");
            if (!string.IsNullOrWhiteSpace(detalle)) sb.AppendLine($"Detalle  : {detalle}");
            return sb.ToString().TrimEnd();
        }

        private CasoColaEnvio MapColaEnvio(CertecfCaso c)
        {
            FiscalDocumentoElectronico? doc = null;
            try
            {
                doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(c.PayloadJson, JsonOpts);
            }
            catch { /* cola best-effort */ }
            return MapColaEnvio(c, doc);
        }

        private static CasoColaEnvio MapColaEnvio(CertecfCaso c, FiscalDocumentoElectronico? doc) => new()
        {
            IdCaso = c.IdCaso,
            Encf = c.Encf,
            TipoEcf = c.TipoEcf,
            MontoTotal = doc?.Encabezado.MontoTotal ?? 0,
            Orden = c.Orden,
            Estado = c.Estado,
            NcfModificado = doc?.Referencia?.NcfModificado
        };

        private static CertecfSesionDto ToDto(CertecfSesion s, string? tipoPrueba)
        {
            var fuente = s.Casos.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tipoPrueba))
                fuente = fuente.Where(c => string.Equals(c.TipoPrueba, tipoPrueba, StringComparison.OrdinalIgnoreCase)
                    || (tipoPrueba == "DATOS" && string.IsNullOrWhiteSpace(c.TipoPrueba)));

            var casos = fuente.Select(c =>
            {
                decimal monto = 0;
                string? rnc = null;
                string? rncEmisor = null;
                string? ncfMod = null;
                var lineas = 0;
                try
                {
                    if (string.Equals(c.TipoPrueba, "ACECF", StringComparison.OrdinalIgnoreCase))
                    {
                        var ace = JsonSerializer.Deserialize<AcecfDocumento>(c.PayloadJson, JsonOpts);
                        monto = ace?.MontoTotal ?? 0;
                        rnc = ace?.RncComprador;
                        rncEmisor = ace?.RncEmisor;
                    }
                    else
                    {
                        var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(c.PayloadJson, JsonOpts);
                        monto = doc?.Encabezado.MontoTotal ?? 0;
                        rnc = doc?.Encabezado.RncComprador;
                        rncEmisor = doc?.Encabezado.RncEmisor;
                        ncfMod = doc?.Referencia?.NcfModificado;
                        lineas = doc?.Lineas.Count ?? 0;
                    }
                }
                catch { /* preview best-effort */ }

                return new CertecfCasoDto
                {
                    IdCaso = c.IdCaso,
                    Orden = c.Orden,
                    Oleada = string.Equals(c.TipoPrueba, "ACECF", StringComparison.OrdinalIgnoreCase)
                        ? c.Oleada
                        : CertecfExcelParser.ResolverOleada(c.TipoEcf, monto),
                    TipoEcf = c.TipoEcf,
                    Encf = c.Encf,
                    TipoPrueba = string.IsNullOrWhiteSpace(c.TipoPrueba) ? "DATOS" : c.TipoPrueba,
                    Estado = c.Estado,
                    TrackId = c.TrackId,
                    Mensaje = c.Mensaje,
                    RespuestaDgii = c.RespuestaDgii,
                    MontoTotal = monto,
                    RncComprador = rnc,
                    RncEmisor = rncEmisor,
                    NcfModificado = ncfMod,
                    Lineas = lineas,
                    FechaEnvio = c.FechaEnvio,
                    FechaRespuesta = c.FechaRespuesta,
                    UrlQR = NullIfEmpty(c.UrlQR) ?? CertecfArtefactos.ExtraerQrDeRespuesta(c.RespuestaDgii),
                    CodigoSeguridad = c.CodigoSeguridad,
                    FechaFirma = c.FechaFirma,
                    QrListo = QrRiListo(c)
                };
            })
            .OrderBy(c => CertecfExcelParser.PrioridadEnvio(c.TipoEcf, c.MontoTotal))
            .ThenBy(c => c.Orden)
            .ToList();

            return new CertecfSesionDto
            {
                IdSesion = s.IdSesion,
                IdEmpresa = s.IdEmpresa,
                NombreArchivo = s.NombreArchivo,
                TipoSet = tipoPrueba ?? s.TipoSet,
                Estado = s.Estado,
                Ambiente = s.Ambiente,
                Mensaje = s.Mensaje,
                FechaCreacion = s.FechaCreacion,
                Total = casos.Count,
                Pendientes = casos.Count(c => c.Estado is "Pendiente" or "Enviado" or "Enviando" or "EnProceso"),
                Aceptados = casos.Count(c => c.Estado is "Aceptado" or "AceptadoCondicional"),
                Rechazados = casos.Count(c => c.Estado is "Rechazado" or "Error"),
                Casos = casos
            };
        }
    }
}
