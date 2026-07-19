using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionConfiguracionEmpresa")]
    public class ProduccionConfiguracionEmpresa
    {
        [Key]
        public int IdEmpresa { get; set; }

        public bool Activo { get; set; }
        public bool UsarEstaciones { get; set; }
        public bool UsarEstadosPorItem { get; set; }
        public bool SonidoActivo { get; set; } = true;
        public int TiempoAdvertenciaSegDefault { get; set; } = 600;
        public int TiempoCriticoSegDefault { get; set; } = 900;
        public bool PermitirCompletarDesdeEstacion { get; set; } = true;
        public int? IdEstacionPredeterminada { get; set; }
        public bool ModoOscuroDefault { get; set; } = true;
        public bool MostrarNombreCliente { get; set; } = true;
        public bool MostrarUsuarioSolicita { get; set; } = true;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
