namespace AlahiaPos.Entities.Dto
{
    /// <summary>Línea del Formato 607 (ventas) — 23 columnas DGII + metadatos.</summary>
    public class Reporte607LineaDto
    {
        public int IdDocumento { get; set; }
        /// <summary>Venta | NotaCredito</summary>
        public string TipoDocumentoAlahia { get; set; } = "Venta";
        public string? NumeroDocumento { get; set; }
        public string? ClienteNombre { get; set; }

        /// <summary>1 — RNC/Cédula/Pasaporte del comprador</summary>
        public string RncCedulaComprador { get; set; } = "";
        /// <summary>2 — 1=RNC, 2=Cédula, 3=Pasaporte</summary>
        public int TipoIdentificacion { get; set; }
        /// <summary>3 — NCF / e-NCF</summary>
        public string Ncf { get; set; } = "";
        /// <summary>4 — NCF modificado (NC/ND)</summary>
        public string? NcfModificado { get; set; }
        /// <summary>5 — Tipo de ingreso 1-6</summary>
        public int TipoIngreso { get; set; } = 1;
        /// <summary>6</summary>
        public DateTime FechaComprobante { get; set; }
        /// <summary>7 — solo si hay retención</summary>
        public DateTime? FechaRetencion { get; set; }

        /// <summary>8 — sin impuestos</summary>
        public decimal MontoFacturado { get; set; }
        /// <summary>9</summary>
        public decimal ItbisFacturado { get; set; }
        /// <summary>10</summary>
        public decimal ItbisRetenidoPorTercero { get; set; }
        /// <summary>11 — siempre 0 mientras no haya régimen</summary>
        public decimal ItbisPercibido { get; set; }
        /// <summary>12</summary>
        public decimal IsrRetenidoPorTercero { get; set; }
        /// <summary>13 — siempre 0</summary>
        public decimal IsrPercibido { get; set; }
        /// <summary>14</summary>
        public decimal ImpuestoSelectivoConsumo { get; set; }
        /// <summary>15</summary>
        public decimal OtrosImpuestos { get; set; }
        /// <summary>16</summary>
        public decimal MontoPropinaLegal { get; set; }

        /// <summary>17 — con impuestos</summary>
        public decimal Efectivo { get; set; }
        /// <summary>18</summary>
        public decimal ChequeTransferenciaDeposito { get; set; }
        /// <summary>19</summary>
        public decimal TarjetaDebitoCredito { get; set; }
        /// <summary>20</summary>
        public decimal VentaCredito { get; set; }
        /// <summary>21</summary>
        public decimal BonosCertificados { get; set; }
        /// <summary>22</summary>
        public decimal Permuta { get; set; }
        /// <summary>23</summary>
        public decimal OtrasFormasVenta { get; set; }

        /// <summary>True si es B02/E32 con total &lt; 250,000 (va a resumen OFV, no al TXT detalle).</summary>
        public bool EsResumenFacturaConsumo { get; set; }

        public List<string> Alertas { get; set; } = new();
        public bool EsValidaParaEnvio => Alertas.Count == 0;
    }

    public class Reporte607Dto
    {
        public int IdEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        /// <summary>Periodo AAAAMM para el nombre del TXT.</summary>
        public string Periodo { get; set; } = "";
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        /// <summary>Líneas del detalle TXT (excluye FC &lt; 250K).</summary>
        public int CantidadRegistros { get; set; }
        public int CantidadConAlertas { get; set; }
        public decimal TotalMontoFacturado { get; set; }
        public decimal TotalItbisFacturado { get; set; }

        /// <summary>Resumen OFV — Facturas de Consumo &lt; RD$250,000.</summary>
        public int ResumenFcCantidad { get; set; }
        public decimal ResumenFcMonto { get; set; }
        public decimal ResumenFcItbis { get; set; }

        public string ContenidoTxt { get; set; } = "";
        public List<Reporte607LineaDto> Lineas { get; set; } = new();
    }
}
