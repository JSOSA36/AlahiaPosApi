using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Clientes:BaseEntity
    {
        [Key]
        public int IDCliente { get; set; }
        public string? CedulaRNC { get; set; } = "";
        public string? NombreComercial { get; set; } = "";
        public DateTime? FechaNacimiento { get; set; }
        public string? Telefono { get; set; } = "";

        public string? Celular { get; set; } = "";
        public bool? Estado { get; set; } 
        public string? Email { get; set; } = "";
        public string? Direccion { get; set; } = "";
        public string? Nota { get; set; } = "";
        public decimal? LimiteCredito { get; set; }
        public List<CorreosElectronicos>? ClienteCorreos { get; set; }

        // Defaults fiscales futuros (Sprint A)
        public string? RegimenDgii { get; set; }
        public bool EsRegimenEspecial { get; set; }
        public byte? TipoIdentificacionDgii { get; set; }
    }
}
