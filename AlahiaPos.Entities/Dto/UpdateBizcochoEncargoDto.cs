using System;

namespace AlahiaPos.Entities.Dto
{
    public class UpdateBizcochoEncargoDto
    {
        public int IdFacturaHeader { get; set; }
        public int IdEmpresa { get; set; }
        public decimal Total { get; set; }
        public decimal Abono { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public string? Nota { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? HoraEntrega { get; set; }
        public DateTime? FechaBencimiento { get; set; }
    }
}
