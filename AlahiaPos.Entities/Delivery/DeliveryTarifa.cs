namespace AlahiaPos.Entities.Delivery
{
    public class DeliveryTarifa
    {
        public bool DeliveryHabilitado { get; set; }
        public bool CalculoPorDistancia { get; set; }
        public decimal TarifaBase { get; set; }
        public decimal PrecioPorKilometro { get; set; }
        public decimal CostoMinimo { get; set; }
        public decimal Redondeo { get; set; }
        public decimal? DistanciaMaximaKm { get; set; }
        public string AccionFueraDeZona { get; set; } = "SIN_LIMITE";
        public string Proveedor { get; set; } = "OSRM";
        public int MinutosCache { get; set; }
        public bool ExigirGps { get; set; } = true;
    }

    public class DeliverySnapshot
    {
        public decimal Costo { get; init; }
        public decimal DistanciaKm { get; init; }
        public decimal? TarifaBase { get; init; }
        public decimal? PrecioKm { get; init; }
        public decimal? Redondeo { get; init; }
        public decimal? Minimo { get; init; }
        public string? Proveedor { get; init; }
        public decimal? OrigenLatitud { get; init; }
        public decimal? OrigenLongitud { get; init; }
        public decimal? DestinoLatitud { get; init; }
        public decimal? DestinoLongitud { get; init; }
        public DateTime? FechaCalculo { get; init; }
        public int? DuracionSegundos { get; init; }
        public bool PuedeConfirmar { get; init; }
        public bool CalculoAutomatico { get; init; }
        public bool FueraDeZona { get; init; }
        public bool LlamarProveedor { get; init; }
        public string? Mensaje { get; init; }
    }

    public class DeliveryMedicion
    {
        public bool Ok { get; set; }
        public decimal DistanciaKm { get; set; }
        public int? DuracionSegundos { get; set; }
        public string Proveedor { get; set; } = "";
        public string? Error { get; set; }
        public bool DesdeCache { get; set; }
    }
}
