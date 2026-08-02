namespace AlahiaPos.Entities.Dto
{
    /// <summary>Línea del Formato 606 (compras) lista para grilla y TXT DGII.</summary>
    public class Reporte606LineaDto
    {
        public int IdOrdenCompraHeader { get; set; }
        /// <summary>Compra | Gasto — origen Alahia del renglón 606.</summary>
        public string OrigenDocumento { get; set; } = "Compra";
        public string? NumeroDocumento { get; set; }
        public string? ProveedorNombre { get; set; }

        public string RncCedula { get; set; } = "";
        /// <summary>1 = RNC, 2 = Cédula</summary>
        public int TipoId { get; set; }
        /// <summary>Código DGII 1-11</summary>
        public int TipoBienesServicios { get; set; }
        public string Ncf { get; set; } = "";
        public string? NcfModificado { get; set; }
        public DateTime FechaComprobante { get; set; }
        public DateTime? FechaPago { get; set; }

        public decimal MontoFacturadoServicios { get; set; }
        public decimal MontoFacturadoBienes { get; set; }
        public decimal TotalMontoFacturado { get; set; }
        public decimal ItbisFacturado { get; set; }
        public decimal ItbisRetenido { get; set; }
        public decimal ItbisProporcionalidad { get; set; }
        public decimal ItbisLlevadoAlCosto { get; set; }
        public decimal ItbisPorAdelantar { get; set; }
        public decimal ItbisPercibido { get; set; }

        public int? TipoRetencionIsr { get; set; }
        public decimal MontoRetencionRenta { get; set; }
        public decimal IsrPercibido { get; set; }

        public decimal ImpuestoSelectivo { get; set; }
        public decimal OtrosImpuestos { get; set; }
        public decimal MontoPropinaLegal { get; set; }

        /// <summary>Código DGII 1-7</summary>
        public int FormaPagoDgii { get; set; }

        public string Estado { get; set; } = "";
        public List<string> Alertas { get; set; } = new();
        public bool EsValidaParaEnvio => Alertas.Count == 0;
    }

    public class Reporte606Dto
    {
        public int IdEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        /// <summary>Periodo AAAAMM sugerido para el TXT (mes de "hasta").</summary>
        public string Periodo { get; set; } = "";
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int CantidadRegistros { get; set; }
        public int CantidadConAlertas { get; set; }
        public decimal TotalMontoFacturado { get; set; }
        public decimal TotalItbisFacturado { get; set; }
        public decimal TotalItbisRetenido { get; set; }
        public decimal TotalRetencionRenta { get; set; }
        /// <summary>Contenido listo para archivo DGII_F_606_....TXT</summary>
        public string ContenidoTxt { get; set; } = "";
        public List<Reporte606LineaDto> Lineas { get; set; } = new();
    }
}
