using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ClienteSaldoAFavor")]
    public class ClienteSaldoAFavor
    {
        [Key]
        public int IdSaldoAFavor { get; set; }

        public int IdEmpresa { get; set; }

        public int IdCliente { get; set; }

        public int IdNotaCredito { get; set; }

        public decimal MontoOriginal { get; set; }

        public decimal SaldoDisponible { get; set; }

        /// <summary>Disponible | Agotado | Anulado</summary>
        [MaxLength(20)]
        public string Estado { get; set; } = ClienteSaldoAFavorEstado.Disponible;

        public DateTime Fecha { get; set; }

        public int? IdUsuario { get; set; }

        [MaxLength(500)]
        public string? Observacion { get; set; }
    }

    public static class ClienteSaldoAFavorEstado
    {
        public const string Disponible = "Disponible";
        public const string Agotado = "Agotado";
        public const string Anulado = "Anulado";
    }
}
