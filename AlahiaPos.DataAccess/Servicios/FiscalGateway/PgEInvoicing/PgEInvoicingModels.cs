using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing
{
    /// <summary>
    /// Estructura raíz que espera POST /api/Receipt del proveedor PG eInvoicing.
    /// Nombres en PascalCase; el serializador aplica CamelCase automáticamente.
    /// </summary>
    public class PgDgiiDocumentDto
    {
        public int IdEmpresa { get; set; }
        /// <summary>Ambiente DGII en el cuerpo (ley CerteCF): testecf | certecf | ecf. El header X-Dgii-Ambiente es respaldo.</summary>
        public string? AmbienteDgii { get; set; }
        /// <summary>Texto exacto de celdas del Excel CerteCF. Vacío = omitir nodo.</summary>
        public Dictionary<string, string>? CeldasExcel { get; set; }
        public PgEncabezadoWrapper Encabezado { get; set; } = new();
        public List<PgItemDto> Detalle { get; set; } = new();
        public List<PgDescuentoRecargoDto>? DescuentosORecargos { get; set; }
        public PgReferenciaDto? Referencia { get; set; }
    }

    public class PgEncabezadoWrapper
    {
        public PgIdDocDto IdDoc { get; set; } = new();
        public PgEmisorDto Emisor { get; set; } = new();
        public PgCompradorDto Comprador { get; set; } = new();
        public PgTotalesDto Totales { get; set; } = new();
    }

    public class PgIdDocDto
    {
        public int TipoeCF { get; set; }
        [JsonPropertyName("eNCF")]
        public string ENCF { get; set; } = "";
        /// <summary>E34: debe ir antes de IndicadorMontoGravado (orden XSD DGII).</summary>
        public int? IndicadorNotaCredito { get; set; }
        public int? IndicadorMontoGravado { get; set; }
        public string? FechaVencimientoSecuencia { get; set; }
        public int? TipoIngresos { get; set; }
        public int? TipoPago { get; set; }
        public List<PgFormaPagoDto>? TablaFormasPago { get; set; }
    }

    public class PgEmisorDto
    {
        [JsonPropertyName("rncEmisor")]
        public string RNCEmisor { get; set; } = "";
        public string RazonSocialEmisor { get; set; } = "";
        public string? NombreComercial { get; set; }
        public string DireccionEmisor { get; set; } = "";
        public string? Municipio { get; set; }
        public string? Provincia { get; set; }
        public List<PgTelefonoEmisorDto>? TablaTelefonoEmisor { get; set; }
        public string? CorreoEmisor { get; set; }
        public string? WebSite { get; set; }
        public string? Sucursal { get; set; }
        public string? ActividadEconomica { get; set; }
        public string? CodigoVendedor { get; set; }
        public string? NumeroFacturaInterna { get; set; }
        public string? NumeroPedidoInterno { get; set; }
        public string? ZonaVenta { get; set; }
        public string? InformacionAdicionalEmisor { get; set; }
        public string FechaEmision { get; set; } = "";
    }

    public class PgTelefonoEmisorDto
    {
        public string TelefonoEmisor { get; set; } = "";
    }

    public class PgCompradorDto
    {
        [JsonPropertyName("rncComprador")]
        public string RNCComprador { get; set; } = "";
        public string RazonSocialComprador { get; set; } = "CONSUMIDOR";
        public string? ContactoComprador { get; set; }
        public string? DireccionComprador { get; set; }
        public string? CorreoComprador { get; set; }
        public string? MunicipioComprador { get; set; }
        public string? ProvinciaComprador { get; set; }
        public string? FechaEntrega { get; set; }
        public string? FechaOrdenCompra { get; set; }
        public string? NumeroOrdenCompra { get; set; }
        public string? CodigoInternoComprador { get; set; }
    }

    public class PgItemDto
    {
        public int NumeroLinea { get; set; }
        public int IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; } = "";
        public string? DescripcionItem { get; set; }
        public int IndicadorBienoServicio { get; set; } = 1;
        public decimal CantidadItem { get; set; }
        public decimal? CantidadReferencia { get; set; }
        public int? UnidadReferencia { get; set; }
        public decimal? GradosAlcohol { get; set; }
        public decimal? PrecioUnitarioReferencia { get; set; }
        public string? FechaElaboracion { get; set; }
        public string? FechaVencimientoItem { get; set; }
        public decimal? Subcantidad { get; set; }
        public int? CodigoSubcantidad { get; set; }
        public decimal PrecioUnitarioItem { get; set; }
        public decimal MontoItem { get; set; }
        public decimal? DescuentoMonto { get; set; }
        public decimal? RecargoMonto { get; set; }
        public int? UnidadMedida { get; set; }
        public string? TipoImpuestoAdicional { get; set; }
        /// <summary>Obligatorio en e-CF 41 (nodo Retencion por ítem).</summary>
        public PgRetencionDto? Retencion { get; set; }
    }

    public class PgRetencionDto
    {
        public int IndicadorAgenteRetencionoPercepcion { get; set; } = 1;
        [JsonPropertyName("montoITBISRetenido")]
        public decimal? MontoITBISRetenido { get; set; }
        [JsonPropertyName("montoISRRetenido")]
        public decimal? MontoISRRetenido { get; set; }
    }

    public class PgDescuentoRecargoDto
    {
        public int NumeroLinea { get; set; }
        public string TipoAjuste { get; set; } = "D";
        public string? DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; } = "$";
        public decimal MontoDescuentooRecargo { get; set; }
        public int? IndicadorFacturacionDescuentooRecargo { get; set; }
    }

    public class PgTotalesDto
    {
        public decimal MontoTotal { get; set; }
        public decimal? MontoNoFacturable { get; set; }
        public decimal? MontoPeriodo { get; set; }
        public decimal? SaldoAnterior { get; set; }
        public decimal? MontoAvancePago { get; set; }
        public decimal? ValorPagar { get; set; }
        public decimal? MontoGravadoTotal { get; set; }
        public decimal? MontoGravado1 { get; set; }
        public decimal? MontoGravado2 { get; set; }
        public decimal? MontoGravado3 { get; set; }
        public decimal? MontoExento { get; set; }
        [JsonPropertyName("totalITBIS")]
        public decimal? TotalITBIS { get; set; }
        [JsonPropertyName("totalITBIS1")]
        public decimal? TotalITBIS1 { get; set; }
        [JsonPropertyName("totalITBIS2")]
        public decimal? TotalITBIS2 { get; set; }
        [JsonPropertyName("totalITBIS3")]
        public decimal? TotalITBIS3 { get; set; }
        public decimal? MontoImpuestoAdicional { get; set; }
        public List<PgImpuestoAdicionalDto>? ImpuestosAdicionales { get; set; }
        [JsonPropertyName("totalITBISRetenido")]
        public decimal? TotalITBISRetenido { get; set; }
        [JsonPropertyName("totalISRRetencion")]
        public decimal? TotalISRRetencion { get; set; }
        public decimal? MontoPropinaLegal { get; set; }
    }

    public class PgImpuestoAdicionalDto
    {
        public string TipoImpuesto { get; set; } = "";
        public decimal TasaImpuestoAdicional { get; set; }
        public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public decimal? OtrosImpuestosAdicionales { get; set; }
    }

    public class PgFormaPagoDto
    {
        public int FormaPago { get; set; }
        public decimal MontoPago { get; set; }
    }

    public class PgReferenciaDto
    {
        [JsonPropertyName("ncfModificado")]
        public string? NCFModificado { get; set; }
        [JsonPropertyName("rncOtroContribuyente")]
        public string? RNCOtroContribuyente { get; set; }
        [JsonPropertyName("fechaNCFModificado")]
        public string? FechaNCFModificado { get; set; }
        public int? CodigoModificacion { get; set; }
        public string? RazonModificacion { get; set; }
    }

    public class PgTrackIdResponse
    {
        public string? trackId { get; set; }
        public string? codigo { get; set; }
        public string? estado { get; set; }
        public string? rnc { get; set; }
        public string? encf { get; set; }
        public bool? secuenciaUtilizada { get; set; }
        public string? fechaRecepcion { get; set; }
        public List<PgMensajeDto>? mensajes { get; set; }
        public string? qr { get; set; }
        public string? securityCode { get; set; }
        public string? fechaFirma { get; set; }
        /// <summary>Solo en errores (debug trama DGII).</summary>
        public string? xmlFirmado { get; set; }
        public string? xmlRespuestaDgii { get; set; }
        /// <summary>Canal usado por Alahia.eCF.Api: ECF | RFCE.</summary>
        public string? canal { get; set; }
        /// <summary>Id del job en Transmission Engine.</summary>
        public string? transmissionJobId { get; set; }
        /// <summary>Estado canónico del Transmission Engine.</summary>
        public string? transmissionState { get; set; }
    }

    public class PgMensajeDto
    {
        public string? valor { get; set; }
        public int? codigo { get; set; }
    }

    /// <summary>
    /// Obsoleto: usar <see cref="FiscalGatewayOptions"/> (sección appsettings FiscalGateway).
    /// Se mantiene solo para deserialización legacy de secciones PgEInvoicing/Einvoicing.
    /// </summary>
    [Obsolete("Usar FiscalGatewayOptions / sección FiscalGateway.")]
    public class PgEInvoicingSettings
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
    }
}
