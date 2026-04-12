using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace AlahiaPos.Entities.Domain
{
    public class Empresas : BaseEntity
    {
        [Key]
        public new int IdEmpresa { get; set; }

        public int? IdPlan { get; set; }
        public string? ApiPrint { get; set; }
        public string? NombreComercial { get; set; } = "";
        public string? RNC { get; set; } = "";
        public string? Direccion { get; set; } = "";
        public string? Telefono { get; set; } = "";
        public string? Logo { get; set; } = "";
        public string? CorreElectronico { get; set; } = "";
        public string? Nota { get; set; } = "";
        public bool Estado { get; set; }

        public DateTime FechaTerminacion { get; set; }

        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? TertiaryColor { get; set; }

        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public string? UrlCatalogo { get; set; }
        public string? UrlCitas { get; set; }

        public Guid GuidPublico { get; set; }

        public string? titleColor { get; set; }
        public string? TokenNotificacion { get; set; }

        public string? CorreoSMTP { get; set; }
        public string? PasswordSMTP { get; set; }
        public string? ServidorSMTP { get; set; }
        public int? PuertoSMTP { get; set; }
        public bool? UsaSSL { get; set; }

        public string? InfoAgendar { get; set; }
        public string? NombreRemitente { get; set; }

        public int? LimiteUsuario { get; set; }

        // ======================================================
        // 🔥 FACTURACIÓN ELECTRÓNICA
        // ======================================================

        public bool? EsEmisorElectronico { get; set; } = false;

        /// <summary>
        /// PRUEBA / PRODUCCION
        /// </summary>
        public string? AmbienteFE { get; set; }

        /// <summary>
        /// Última secuencia utilizada (control interno)
        /// </summary>
        public string? SecuenciaActualECF { get; set; }

        public DateTime? FechaHabilitacionFE { get; set; }

        /// <summary>
        /// Control de estado general FE
        /// Activo / Suspendido / EnProceso
        /// </summary>
        public string? EstadoFE { get; set; }

        // ======================================================
        // 🔗 RELACIONES
        // ======================================================

        public ICollection<EmpresaModulo> EmpresaModulos { get; set; } = new List<EmpresaModulo>();
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaVencimientoPlan { get; set; }
        public string? EstadoPlan { get; set; }
        [ForeignKey(nameof(IdPlan))]
        public PlanesCloud? Plan { get; set; }

        // 🔥 Nuevas relaciones FE
        public ICollection<CertificadoDigital> CertificadosDigitales { get; set; } = new List<CertificadoDigital>();
        public ICollection<ECFEncabezado> ECFs { get; set; } = new List<ECFEncabezado>();
        public ICollection<SecuenciaECF> SecuenciasECF { get; set; } = new List<SecuenciaECF>();
    }
}