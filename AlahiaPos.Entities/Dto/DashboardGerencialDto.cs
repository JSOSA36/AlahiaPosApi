namespace AlahiaPos.Entities.Dto
{
    public class DashboardGerencialDto
    {
        public int IdEmpresa { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public string PeriodoLabel { get; set; } = string.Empty;

        public DashboardGerencialPlDto Pl { get; set; } = new();
        public DashboardGerencialIndicadoresDto Indicadores { get; set; } = new();
        public DashboardGerencialChartsDto Charts { get; set; } = new();
    }

    public class DashboardGerencialPlDto
    {
        public decimal VentasBrutas { get; set; }
        public decimal CostoVenta { get; set; }
        public decimal UtilidadBruta { get; set; }
        public decimal GastosOperativos { get; set; }
        public decimal Comisiones { get; set; }
        public decimal PerdidasInventario { get; set; }
        public decimal OtrosIngresos { get; set; }
        public decimal OtrosEgresos { get; set; }
        public decimal UtilidadOperativa { get; set; }

        /// <summary>Utilidad bruta / ventas brutas × 100</summary>
        public decimal MargenBrutoPct { get; set; }

        /// <summary>Utilidad operativa / ventas brutas × 100</summary>
        public decimal MargenOperativoPct { get; set; }
    }

    public class DashboardGerencialIndicadoresDto
    {
        public decimal Caja { get; set; }
        public decimal Bancos { get; set; }
        public decimal ValorInventario { get; set; }
        public decimal ValorActivosFijos { get; set; }
        public decimal CuentasPorCobrar { get; set; }
        public decimal CuentasPorPagar { get; set; }
    }

    public class DashboardGerencialChartsDto
    {
        public List<SerieNombreMontoDto> VentasVsCostosVsUtilidad { get; set; } = new();
        public List<SerieDiariaDto> EvolucionVentasMes { get; set; } = new();
        public List<SerieNombreMontoDto> DistribucionGastos { get; set; } = new();
        public List<SerieNombreMontoDto> DistribucionPerdidas { get; set; } = new();
        public List<SerieNombreMontoDto> ComisionesPorEmpleado { get; set; } = new();
        public List<ProductoRentabilidadDto> TopProductosRentables { get; set; } = new();
        public List<SerieNombreMontoDto> TopProductosPerdidas { get; set; } = new();

        /// <summary>Entradas vs salidas de tesorería del mes (excluye transferencias).</summary>
        public List<SerieNombreMontoDto> FlujoEfectivo { get; set; } = new();

        /// <summary>Pasos del estado de resultados para waterfall / explicación visual.</summary>
        public List<EstadoResultadosPasoDto> EstadoResultados { get; set; } = new();
    }

    public class SerieDiariaDto
    {
        public string Fecha { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class SerieNombreMontoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class ProductoRentabilidadDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal VentasNetas { get; set; }
        public decimal Costo { get; set; }
        public decimal Margen { get; set; }

        /// <summary>Margen / ventas netas × 100</summary>
        public decimal RentabilidadPct { get; set; }
    }

    public class EstadoResultadosPasoDto
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public decimal Acumulado { get; set; }

        /// <summary>base | resta | suma | subtotal | total</summary>
        public string Tipo { get; set; } = "base";
    }
}
