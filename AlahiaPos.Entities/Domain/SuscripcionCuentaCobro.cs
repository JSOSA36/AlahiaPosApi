using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>Cuentas bancarias donde MacroBits recibe pagos de suscripción.</summary>
    [Table("SuscripcionCuentaCobro")]
    public class SuscripcionCuentaCobro
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(120)]
        public string Banco { get; set; } = "";

        [MaxLength(80)]
        public string NumeroCuenta { get; set; } = "";

        [MaxLength(200)]
        public string Titular { get; set; } = "";

        [MaxLength(40)]
        public string Cedula { get; set; } = "";

        [MaxLength(200)]
        public string? Correo { get; set; }

        /// <summary>Cuenta estándar / IBAN interbancario (opcional).</summary>
        [MaxLength(80)]
        public string? CuentaEstandar { get; set; }

        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
    }
}
