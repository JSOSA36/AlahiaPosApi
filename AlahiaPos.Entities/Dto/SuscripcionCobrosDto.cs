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

    /// <summary>Cliente para que MacroBits asigne cargos / revise cobro.</summary>
    public class SuscripcionEmpresaCobroDto
    {
        public int IdEmpresa { get; set; }
        public string NombreComercial { get; set; } = "";
        public string EstadoServicio { get; set; } = "";
        public bool PagadoServicio { get; set; }
        public int? IdPlan { get; set; }
        public string? NombrePlan { get; set; }
        public decimal? PrecioPlanCatalogo { get; set; }
        public decimal? PrecioPlanEspecialUsd { get; set; }
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
}
