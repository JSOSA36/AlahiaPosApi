using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>
    /// Equipo (PC/tablet) amarrado a una licencia POS de la empresa.
    /// Una fila ACTIVA = un asiento de caja consumido.
    /// </summary>
    [Table("PosTerminal")]
    public class PosTerminal
    {
        [Key]
        public int IdPosTerminal { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        /// <summary>SHA-256 hex de la huella enviada por el cliente.</summary>
        [Required, MaxLength(64)]
        public string Huella { get; set; } = "";

        [MaxLength(80)]
        public string? Nombre { get; set; }

        [MaxLength(30)]
        public string? Plataforma { get; set; }

        [MaxLength(80)]
        public string? Modelo { get; set; }

        [MaxLength(80)]
        public string? Fabricante { get; set; }

        /// <summary>ACTIVO | REVOCADO</summary>
        [Required, MaxLength(20)]
        public string Estado { get; set; } = PosTerminalEstados.Activo;

        public DateTime FechaActivacion { get; set; } = DateTime.Now;
        public int? IdUsuarioActivacion { get; set; }

        public DateTime FechaUltimoAcceso { get; set; } = DateTime.Now;
        public int? IdUsuarioUltimoAcceso { get; set; }

        public DateTime? FechaRevocacion { get; set; }
        public int? IdUsuarioRevocacion { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas? Empresa { get; set; }
    }

    public static class PosTerminalEstados
    {
        public const string Activo = "ACTIVO";
        public const string Revocado = "REVOCADO";
    }
}
