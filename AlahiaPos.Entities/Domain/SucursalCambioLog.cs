using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("SucursalCambioLog")]
    public class SucursalCambioLog
    {
        [Key]
        public int IdSucursalCambioLog { get; set; }

        public int IdUsuario { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursalOrigen { get; set; }

        public int IdSucursalDestino { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(150)]
        public string? Dispositivo { get; set; }

        [MaxLength(64)]
        public string? Ip { get; set; }
    }
}
