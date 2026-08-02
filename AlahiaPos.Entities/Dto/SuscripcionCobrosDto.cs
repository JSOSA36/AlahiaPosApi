using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class SuscripcionCicloDto
    {
        public int IdCiclo { get; set; }
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public decimal Monto { get; set; }
        public int? IdPlan { get; set; }
        public string Estado { get; set; } = "";
    }

    public class SuscripcionEventoDto
    {
        public int IdEvento { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdCiclo { get; set; }
        public string Tipo { get; set; } = "";
        public string? Detalle { get; set; }
        public string? Canal { get; set; }
        public int? IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class SuscripcionResumenCobrosDto
    {
        public int Activas { get; set; }
        public int PendientePago { get; set; }
        public int PagoReportado { get; set; }
        public int Suspendidas { get; set; }
        public int Canceladas { get; set; }
        public int PagosPendientesValidacion { get; set; }
        public List<SuscripcionCicloDto> CiclosAbiertos { get; set; } = new();
    }

    /// <summary>Cliente para que MacroBits configure tarifa / revise cobro.</summary>
    public class SuscripcionEmpresaCobroDto
    {
        public int IdEmpresa { get; set; }
        public string NombreComercial { get; set; } = "";
        public string EstadoServicio { get; set; } = "";
        public bool PagadoServicio { get; set; }
        public int? IdPlan { get; set; }
        /// <summary>"Plan {NombreComercial}".</summary>
        public string? NombrePlan { get; set; }
        public decimal MontoServicio { get; set; }
        public decimal CargoAdicional { get; set; }
        public int LimiteFacturacion { get; set; }
        public decimal CargoReconexionDop { get; set; }
        public bool ReconexionPendiente { get; set; }
        public decimal TotalCiclo => MontoServicio + CargoAdicional;
    }

    public class NotificacionSuscripcionMensaje
    {
        public int IdEmpresa { get; set; }
        public int? IdCiclo { get; set; }
        public string TipoAviso { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? CorreoDestino { get; set; }
        public string? NombreEmpresa { get; set; }
    }

    public class SuscripcionCuentaCobroDto
    {
        public int Id { get; set; }
        public string Banco { get; set; } = "";
        public string NumeroCuenta { get; set; } = "";
        public string Titular { get; set; } = "";
        public string Cedula { get; set; } = "";
        public string? Correo { get; set; }
        public string? CuentaEstandar { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class GuardarSuscripcionCuentaCobroDto
    {
        public int? Id { get; set; }
        public string Banco { get; set; } = "";
        public string NumeroCuenta { get; set; } = "";
        public string Titular { get; set; } = "";
        public string Cedula { get; set; } = "";
        public string? Correo { get; set; }
        public string? CuentaEstandar { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
    }
}
