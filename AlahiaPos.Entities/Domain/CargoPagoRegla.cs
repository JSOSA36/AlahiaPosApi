using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CargoPagoRegla")]
    public class CargoPagoRegla : BaseEntity
    {
        [Key]
        public int IdCargoPagoRegla { get; set; }

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        /// <summary>PORCENTAJE | MONTO_FIJO</summary>
        [Required, MaxLength(20)]
        public string Tipo { get; set; } = CargoPagoTipos.Porcentaje;

        public decimal Valor { get; set; }

        /// <summary>TARJETA | EFECTIVO | TRANSFERENCIA | CHEQUE | TODOS | PERSONALIZADO</summary>
        [Required, MaxLength(30)]
        public string GrupoMetodo { get; set; } = CargoPagoGrupos.Tarjeta;

        /// <summary>
        /// Nombres exactos de MetodoPagoCuenta (ej. Billet BHD).
        /// Se suman al grupo, salvo PERSONALIZADO (solo estos).
        /// En SQL se guardan unidos por |.
        /// </summary>
        public List<string> MetodosVinculados { get; set; } = new();

        public bool Activo { get; set; } = true;

        public int Orden { get; set; } = 1;
    }

    public static class CargoPagoTipos
    {
        public const string Porcentaje = "PORCENTAJE";
        public const string MontoFijo = "MONTO_FIJO";
    }

    public static class CargoPagoGrupos
    {
        public const string Tarjeta = "TARJETA";
        public const string Efectivo = "EFECTIVO";
        public const string Transferencia = "TRANSFERENCIA";
        public const string Cheque = "CHEQUE";
        public const string Todos = "TODOS";
        public const string Personalizado = "PERSONALIZADO";
    }
}
