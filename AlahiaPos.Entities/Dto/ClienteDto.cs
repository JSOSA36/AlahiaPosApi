using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ClienteDto: BaseEntity
    {
        public int IdCliente { get; set; }
        public string? CedulaRNC { get; set; } = "";
        public string? NombreComercial { get; set; } = "";

        public string? Telefono { get; set; } = "";
        public DateTime? FechaNacimiento { get; set; }
        public string? Celular { get; set; } = "";
        public bool Estado { get; set; }
        public string? Email { get; set; } = "";
        public string? Direccion { get; set; } = "";
        public string? Nota { get; set; } = "";
        public decimal LimiteCredito { get; set; }
    }
}
