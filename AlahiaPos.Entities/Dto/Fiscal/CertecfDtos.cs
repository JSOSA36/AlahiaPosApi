using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    public class CertecfLabEstadoDto
    {
        public int IdEmpresa { get; set; }
        public string? Rnc { get; set; }
        public string? NombreEmpresa { get; set; }
        public bool CertificadoOk { get; set; }
        public string? CertificadoNombre { get; set; }
        public DateTime? CertificadoExpira { get; set; }
        public bool CertificadoVencido { get; set; }
        public string Ambiente { get; set; } = "certecf";
        public string Proveedor { get; set; } = "DGII_DIRECTO";
        public int PasoActual { get; set; } = 1;
        public CertecfPostulacionDto Postulacion { get; set; } = new();
        /// <summary>Host público de Alahia.eCF.Api (FiscalGateway:BaseUrl), para armar URLs CerteCF.</summary>
        public string? InboundBaseUrl { get; set; }
        public List<CertecfPasoEstadoDto> Pasos { get; set; } = new();
        public CertecfSesionDto? SesionActiva { get; set; }
        public CertecfSesionDto? SesionAcecf { get; set; }
        public CertecfSesionDto? SesionSimulacion { get; set; }
        public List<CertecfInboundLogDto> Inbound { get; set; } = new();
        /// <summary>Escritorio: XML íntegros E32 &lt; 250 mil para Browse + ENVIAR en el portal.</summary>
        public string? RutaXmlConsumo250 { get; set; }
        /// <summary>Escritorio: HTML/PDF de representación impresa para el paso 5 del portal.</summary>
        public string? RutaRi { get; set; }
        public string? Aviso { get; set; }
        public string FuenteNorma { get; set; } =
            "https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/Paginas/documentacionSobreE-CF.aspx";
    }

    public class CertecfPasoEstadoDto
    {
        public int Numero { get; set; }
        public string Titulo { get; set; } = "";
        public string Estado { get; set; } = "Pendiente";
        public string QuePidePortal { get; set; } = "";
        public string QuePideNorma { get; set; } = "";
        public string QueHaceAlahia { get; set; } = "";
        public bool AccionEnPortal { get; set; }
    }

    public class CertecfPostulacionDto
    {
        public string NombreSoftware { get; set; } = "Alahia ERP";
        public string VersionSoftware { get; set; } = "1.0";
        public string TipoSoftware { get; set; } = "EXTERNO";
        public string? UrlRecepcion { get; set; }
        public string? UrlAprobacion { get; set; }
        public string? UrlAutenticacion { get; set; }
        public string? UrlRecepcionProd { get; set; }
        public string? UrlAprobacionProd { get; set; }
        public string? UrlAutenticacionProd { get; set; }
        public string? RepresentanteNombre { get; set; }
        public string? RepresentanteCedula { get; set; }
    }

    public class CertecfMarcarPasoDto
    {
        public int Paso { get; set; }
        public string Estado { get; set; } = "Hecho";
        public string? Nota { get; set; }
    }

    public class AcecfDocumento
    {
        public int IdEmpresa { get; set; }
        public string RncEmisor { get; set; } = "";
        public string Encf { get; set; } = "";
        public string FechaEmision { get; set; } = "";
        public decimal MontoTotal { get; set; }
        public string RncComprador { get; set; } = "";
        /// <summary>1 = Aceptado, 2 = Rechazado (XSD ACECF EstadoType).</summary>
        public int Estado { get; set; } = 1;
        public string? DetalleMotivoRechazo { get; set; }
        /// <summary>CerteCF: copiar tal cual del Excel (dd-MM-yyyy HH:mm:ss). No usar DateTime.Now.</summary>
        public string? FechaHoraAprobacionComercial { get; set; }
    }

    public class CertecfInboundLogDto
    {
        public int IdLog { get; set; }
        public string Tipo { get; set; } = "";
        public string? Encf { get; set; }
        public string? Estado { get; set; }
        public string? Mensaje { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CertecfArchivoDto
    {
        public string NombreArchivo { get; set; } = "";
        public string Contenido { get; set; } = "";
        public string ContentType { get; set; } = "application/xml";
    }

    public class CertecfSesionDto
    {
        public int IdSesion { get; set; }
        public int IdEmpresa { get; set; }
        public string NombreArchivo { get; set; } = "";
        public string TipoSet { get; set; } = "ECF";
        public string Estado { get; set; } = "";
        public string Ambiente { get; set; } = "certecf";
        public string? Mensaje { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int Aceptados { get; set; }
        public int Rechazados { get; set; }
        public List<CertecfCasoDto> Casos { get; set; } = new();
    }

    public class CertecfCasoDto
    {
        public int IdCaso { get; set; }
        public int Orden { get; set; }
        public int Oleada { get; set; }
        public int TipoEcf { get; set; }
        public string Encf { get; set; } = "";
        public string TipoPrueba { get; set; } = "DATOS";
        public string Estado { get; set; } = "";
        public string? TrackId { get; set; }
        public string? Mensaje { get; set; }
        public string? RespuestaDgii { get; set; }
        public decimal MontoTotal { get; set; }
        public string? RncComprador { get; set; }
        public string? RncEmisor { get; set; }
        public string? NcfModificado { get; set; }
        public int Lineas { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? UrlQR { get; set; }
        public bool QrListo { get; set; }
    }

    public class CertecfRiLoteDto
    {
        public string Ruta { get; set; } = "";
        public string Aviso { get; set; } =
            "Abra cada HTML → Imprimir → Guardar como PDF. Suba un PDF por recuadro del portal (11 archivos, suma ≤ 10 MB). No es una foto.";
        public List<CertecfRiSlotDto> Slots { get; set; } = new();
    }

    public class CertecfRiSlotDto
    {
        public string Clave { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public int TipoEcf { get; set; }
        public string Encf { get; set; } = "";
        public int IdCaso { get; set; }
        public bool QrListo { get; set; }
        public string NombreArchivo { get; set; } = "";
    }
}
