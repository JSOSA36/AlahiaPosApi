namespace AlahiaPos.Entities.Dto
{
    public class SalonSnapshotDto
    {
        public int IdEmpresa { get; set; }
        public List<SalonZonaDto> Zonas { get; set; } = new();
        public int MesasDisponibles { get; set; }
        public int MesasOcupadas { get; set; }
        public int MesasReservadas { get; set; }
        public int MesasCuenta { get; set; }
        public int MesasFuera { get; set; }
    }

    public class SalonZonaDto
    {
        public int ZonaId { get; set; }
        public string Nombre { get; set; } = "";
        public List<SalonMesaDto> Mesas { get; set; } = new();
    }

    public class SalonMesaDto
    {
        public int IdMesa { get; set; }
        public int ZonaId { get; set; }
        public string Numero { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int Capacidad { get; set; }
        public int Ocupantes { get; set; }
        public string Forma { get; set; } = "square";
        public string Estado { get; set; } = "disponible";
        public bool IsActiva { get; set; }
        public decimal? PosX { get; set; }
        public decimal? PosY { get; set; }
        public decimal? Rotacion { get; set; }
        public decimal? Escala { get; set; }
        public int? MinutosOcupada { get; set; }
        public decimal Total { get; set; }
        public int CantidadOrdenes { get; set; }
        public string? Cliente { get; set; }
        public string? ZonaNombre { get; set; }
    }

    public class SalonOrdenDto
    {
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; } = "";
        public string NombreCuenta { get; set; } = "";
        public decimal Total { get; set; }
        public string Hora { get; set; } = "";
        public int Items { get; set; }
        public List<string> Productos { get; set; } = new();
    }

    public class SalonZonaWriteDto
    {
        public int IdEmpresa { get; set; }
        public int ZonaId { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class SalonMesaWriteDto
    {
        public int IdEmpresa { get; set; }
        public int ZonaId { get; set; }
        public string Numero { get; set; } = "";
        public int Capacidad { get; set; } = 4;
    }
}
