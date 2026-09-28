using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    public class FiscalDocumentoElectronico
    {
        public int IdEmpresa { get; set; }
        public int IdDocumentoInterno { get; set; }
        public string TipoDocumentoAlahia { get; set; } = "Venta";
        /// <summary>Ambiente DGII: testecf | certecf | ecf (desde Empresas.AmbienteFE).</summary>
        public string? AmbienteDgii { get; set; }
        /// <summary>
        /// Texto de cada celda del Excel CerteCF (clave normalizada).
        /// Vacío = no emitir el nodo. Presente = emitir exactamente ese texto.
        /// </summary>
        public Dictionary<string, string> CeldasExcel { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public FiscalDocumentoEncabezado Encabezado { get; set; } = new();
        public List<FiscalDocumentoLinea> Lineas { get; set; } = new();
        public List<FiscalDocumentoDescuento> Descuentos { get; set; } = new();
        public List<FiscalDocumentoFormaPagoDgii> FormasPago { get; set; } = new();
        public FiscalDocumentoReferencia? Referencia { get; set; }
    }

    public class FiscalDocumentoEncabezado
    {
        public int TipoEcf { get; set; }
        public string Encf { get; set; } = "";
        public int TipoIngreso { get; set; } = 1;
        public int TipoPago { get; set; } = 1;
        public int? IndicadorMontoGravado { get; set; }
        /// <summary>Tipo 34: 0 si emisión ≤30 días del NCF modificado; 1 si &gt;30 días.</summary>
        public int? IndicadorNotaCredito { get; set; }
        public DateTime? FechaVencimientoSecuencia { get; set; }
        public DateTime FechaEmision { get; set; }

        public string RncEmisor { get; set; } = "";
        public string RazonSocialEmisor { get; set; } = "";
        public string? NombreComercialEmisor { get; set; }
        public string? DireccionEmisor { get; set; }
        public string? MunicipioEmisor { get; set; }
        public string? ProvinciaEmisor { get; set; }
        public string? TelefonoEmisor { get; set; }
        public string? CorreoEmisor { get; set; }
        public string? WebSite { get; set; }
        public string? Sucursal { get; set; }
        public string? ActividadEconomica { get; set; }
        public string? CodigoVendedor { get; set; }
        public string? NumeroFacturaInterna { get; set; }
        public string? NumeroPedidoInterno { get; set; }
        public string? ZonaVenta { get; set; }
        public string? InformacionAdicionalEmisor { get; set; }

        public string? RncComprador { get; set; }
        public string? IdentificadorExtranjero { get; set; }
        public string? RazonSocialComprador { get; set; }
        public string? ContactoComprador { get; set; }
        public string? DireccionComprador { get; set; }
        public string? CorreoComprador { get; set; }
        public string? MunicipioComprador { get; set; }
        public string? ProvinciaComprador { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaOrdenCompra { get; set; }
        public string? NumeroOrdenCompra { get; set; }
        public string? CodigoInternoComprador { get; set; }

        public decimal MontoGravadoTotal { get; set; }
        public decimal MontoGravadoI1 { get; set; }
        public decimal MontoGravadoI2 { get; set; }
        public decimal MontoGravadoI3 { get; set; }
        public decimal MontoExento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalItbis1 { get; set; }
        public decimal TotalItbis2 { get; set; }
        public decimal TotalItbis3 { get; set; }
        /// <summary>XSD Totales: antes de MontoTotal. CerteCF: solo si el Excel trae número &gt; 0.</summary>
        public decimal? MontoImpuestoAdicional { get; set; }
        public List<FiscalImpuestoAdicional> ImpuestosAdicionales { get; set; } = new();
        public decimal MontoTotal { get; set; }
        public decimal? MontoNoFacturable { get; set; }
        public decimal? MontoPeriodo { get; set; }
        /// <summary>CerteCF compara este campo con el Excel; emitir aunque sea 0.</summary>
        public decimal? ValorPagar { get; set; }
        public decimal? SaldoAnterior { get; set; }
        public decimal? MontoAvancePago { get; set; }
        public decimal TotalItbisRetenido { get; set; }
        public decimal TotalIsrRetencion { get; set; }
        public decimal MontoPropinaLegal { get; set; }
    }

    public class FiscalDocumentoLinea
    {
        public int NumeroLinea { get; set; }
        public int IndicadorFacturacion { get; set; } = 1;
        public string NombreItem { get; set; } = "";
        public string? DescripcionItem { get; set; }
        public bool EsBien { get; set; } = true;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal MontoItem { get; set; }
        public decimal? DescuentoMonto { get; set; }
        public decimal? RecargoMonto { get; set; }
        /// <summary>XSD TablaSubDescuento: si hay DescuentoMonto, DGII exige al menos un SubDescuento.</summary>
        public List<FiscalSubDescuentoRecargo> SubDescuentos { get; set; } = new();
        /// <summary>XSD TablaSubRecargo: si hay RecargoMonto, DGII exige al menos un SubRecargo.</summary>
        public List<FiscalSubDescuentoRecargo> SubRecargos { get; set; } = new();
        public int? UnidadMedida { get; set; }
        /// <summary>XSD Item: después de UnidadMedida. Vacío Excel = omitir.</summary>
        public decimal? CantidadReferencia { get; set; }
        public int? UnidadReferencia { get; set; }
        public decimal? GradosAlcohol { get; set; }
        public decimal? PrecioUnitarioReferencia { get; set; }
        public DateTime? FechaElaboracion { get; set; }
        public DateTime? FechaVencimientoItem { get; set; }
        public decimal? Subcantidad { get; set; }
        public int? CodigoSubcantidad { get; set; }
        /// <summary>TablaImpuestoAdicional/TipoImpuesto (p. ej. 006 Cerveza). Vacío = omitir.</summary>
        public string? TipoImpuestoAdicional { get; set; }
        /// <summary>1=Retención, 2=Percepción. Requerido en ítems E41.</summary>
        public int? IndicadorAgenteRetencionoPercepcion { get; set; }
        public decimal? MontoItbisRetenido { get; set; }
        public decimal? MontoIsrRetenido { get; set; }
    }

    public class FiscalSubDescuentoRecargo
    {
        /// <summary>$ = monto, % = porcentaje (TipoDescuentoRecargoType).</summary>
        public string Tipo { get; set; } = "$";
        public decimal? Porcentaje { get; set; }
        public decimal? Monto { get; set; }
    }

    public class FiscalDocumentoDescuento
    {
        public int NumeroLinea { get; set; }
        public bool EsDescuento { get; set; } = true;
        public string? Descripcion { get; set; }
        public bool EsMontoFijo { get; set; } = true;
        public decimal Monto { get; set; }
        public int? IndicadorFacturacion { get; set; }
    }

    public class FiscalDocumentoFormaPagoDgii
    {
        public int FormaPago { get; set; }
        public decimal Monto { get; set; }
    }

    public class FiscalImpuestoAdicional
    {
        public string TipoImpuesto { get; set; } = "";
        public decimal TasaImpuestoAdicional { get; set; }
        public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public decimal? OtrosImpuestosAdicionales { get; set; }
    }

    public class FiscalDocumentoReferencia
    {
        public string NcfModificado { get; set; } = "";
        public string? RncOtroContribuyente { get; set; }
        public DateTime? FechaNcfModificado { get; set; }
        public int CodigoModificacion { get; set; }
        public string? RazonModificacion { get; set; }
    }
}
