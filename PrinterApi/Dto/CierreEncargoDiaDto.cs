namespace PrinterApi.Dto
{
    namespace PrinterApi.Dto
    {
        public class CierreEncargoDiaDto
        {
            public DateTime Fecha { get; set; }

            public int CantidadEncargosNuevos { get; set; }
            public decimal TotalEncargosNuevos { get; set; }

            public decimal TotalCobradoHoy { get; set; }
            public decimal TotalPendiente { get; set; }

            public int TotalEntregados { get; set; }
            public int TotalPendientes { get; set; }
            public int TotalVencidos { get; set; }

            public List<CierreEncargoMetodoPagoDto> MetodosPago { get; set; } = new();

            public List<CierreEncargoDetalleDto> Encargos { get; set; } = new();
        }

        public class CierreEncargoMetodoPagoDto
        {
            public string FormaPago { get; set; } = string.Empty;
            public decimal Total { get; set; }
        }

        public class CierreEncargoDetalleDto
        {
            public int IdFacturaHeader { get; set; }

            public string NumeroDocumento { get; set; } = string.Empty;
            public string Cliente { get; set; } = string.Empty;
            public string Celular { get; set; } = string.Empty;

            public DateTime? FechaEntrega { get; set; }

            public decimal Total { get; set; }
            public decimal Abonado { get; set; }
            public decimal Pendiente { get; set; }

            public string Estado { get; set; } = string.Empty;
        }
    }
}
