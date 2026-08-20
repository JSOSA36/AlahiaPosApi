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
        public bool PagadoServicio { get; set; } = false;

        public DateTime? FechaUltimoPago { get; set; }
        public string? Politicas { get; set; }
        public DateTime? FechaProximoPago { get; set; }

        [MaxLength(20)]
        public string EstadoServicio { get; set; } = "ACTIVA";
        public int? IdPlan { get; set; }

        /// <summary>
        /// Precio USD del plan solo para esta empresa (legado). Preferir <see cref="MontoServicio"/>.
        /// </summary>
        public decimal? PrecioPlanEspecialUsd { get; set; }

        /// <summary>Monto mensual del servicio acordado con este cliente (USD).</summary>
        public decimal MontoServicio { get; set; } = 0m;

        /// <summary>
        /// Tope de facturas (documentos de venta) por mes calendario.
        /// 0 = sin límite.
        /// </summary>
        public int LimiteFacturacion { get; set; } = 0;

        /// <summary>Cargo extra mensual acordado (USD), se suma a <see cref="MontoServicio"/>.</summary>
        public decimal CargoAdicional { get; set; } = 0m;

        /// <summary>
        /// Cargo por reconexión en RD$ (pesos). Se aplica solo si <see cref="ReconexionPendiente"/>.
        /// 0 = este cliente no tiene cargo de reconexión.
        /// </summary>
        public decimal CargoReconexionDop { get; set; } = 500m;

        /// <summary>
        /// True tras suspensión por falta de pago: el próximo cobro incluye cargo de reconexión.
        /// Se limpia al aprobar el pago / marcar pagado.
        /// </summary>
        public bool ReconexionPendiente { get; set; } = false;

        /// <summary>Empresa dueña de la plataforma (MacroBits). Exenta de cobros/suspensión.</summary>
        public bool EsEmpresaSistema { get; set; } = false;
        public string? ApiPrint { get; set; }
        public string? NombreComercial { get; set; } = "";
        public string? RNC { get; set; } = "";
        public string? Direccion { get; set; } = "";
        public string? Telefono { get; set; } = "";
        public string? Logo { get; set; } = "";
        public string? CorreElectronico { get; set; } = "";
        public string? Nota { get; set; } = "";
        public bool Estado { get; set; }
        public bool PoliticasAceptadas { get; set; }
        public DateTime FechaTerminacion { get; set; }

        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? TertiaryColor { get; set; }

        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? Municipio { get; set; }

        public string? Provincia { get; set; }
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

        /// <summary>Nivel de soporte contratado: STANDARD | GOLD | PREMIUM.</summary>
        [MaxLength(20)]
        public string NivelSoporte { get; set; } = NivelesSoporte.Standard;

        // ======================================================
        // 🔥 FACTURACIÓN ELECTRÓNICA
        // ======================================================

        public bool? EsEmisorElectronico { get; set; } = false;

        /// <summary>
        /// Ambiente DGII: testecf | certecf | ecf (solo modo DGII_DIRECTO).
        /// </summary>
        public string? AmbienteFE { get; set; }

        /// <summary>
        /// Modo de envío FE: DGII_DIRECTO | PROVEEDOR_EXTERNO.
        /// Null = DGII_DIRECTO (FiscalGateway appsettings).
        /// </summary>
        [MaxLength(40)]
        public string? ProveedorFE { get; set; }

        /// <summary>Nombre amigable del proveedor externo (ej. Pedro).</summary>
        [MaxLength(100)]
        public string? ProveedorFE_Nombre { get; set; }

        [MaxLength(500)]
        public string? ProveedorFE_BaseUrl { get; set; }

        [MaxLength(500)]
        public string? ProveedorFE_ApiKey { get; set; }

        [MaxLength(200)]
        public string? ProveedorFE_Usuario { get; set; }

        [MaxLength(500)]
        public string? ProveedorFE_Password { get; set; }

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