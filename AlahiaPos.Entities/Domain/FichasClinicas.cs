using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("FichasClinicas")]
    public class FichasClinicas
    {
        [Key]
        public int IdFichaClinica { get; set; }

        public int IdEmpresa { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas? Empresa { get; set; }

        public int IdCliente { get; set; }

        [ForeignKey(nameof(IdCliente))]
        public Clientes? Cliente { get; set; }

        [MaxLength(120)]
        public string? Nombres { get; set; }

        [MaxLength(120)]
        public string? Apellidos { get; set; }

        [MaxLength(20)]
        public string? Sexo { get; set; }

        [MaxLength(30)]
        public string? EstadoCivil { get; set; }

        [MaxLength(80)]
        public string? Nacionalidad { get; set; }

        [MaxLength(150)]
        public string? ContactoEmergenciaNombre { get; set; }

        [MaxLength(40)]
        public string? ContactoEmergenciaTelefono { get; set; }

        public string? AnamnesisJson { get; set; }

        public string? OdontogramaJson { get; set; }

        [MaxLength(500)]
        public string? Medicamentos { get; set; }

        public string? Observaciones { get; set; }

        [MaxLength(80)]
        public string? Color { get; set; }

        [MaxLength(120)]
        public string? TipoProtesis { get; set; }

        [MaxLength(150)]
        public string? Laboratorio { get; set; }

        public int IdUsuarioCreacion { get; set; }

        public int? IdUsuarioModificacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaModificacion { get; set; } = DateTime.Now;
    }
}
