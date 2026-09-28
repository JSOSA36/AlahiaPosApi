using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>
    /// Un renglón por cada WhatsApp de citas que sí salió.
    /// Precio al salón (RD$5) vs costo Alahia (RD$3). Aún no se factura el ciclo.
    /// </summary>
    [Table("WhatsAppCitasConsumo")]
    public class WhatsAppCitasConsumo
    {
        [Key]
        public int Id { get; set; }

        public int IdEmpresa { get; set; }
        public int? IdCita { get; set; }

        [MaxLength(40)]
        public string Tipo { get; set; } = "";

        [MaxLength(30)]
        public string? Telefono { get; set; }

        public decimal PrecioClienteDop { get; set; }
        public decimal CostoAlahiaDop { get; set; }

        [MaxLength(80)]
        public string? TwilioSid { get; set; }

        public bool EsPrueba { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
