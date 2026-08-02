using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public static class TipoCargoRecurrente
    {
        public const string Plan = "PLAN";
        public const string Modulo = "MODULO";
        public const string Usuarios = "USUARIOS";
        public const string Almacenamiento = "ALMACENAMIENTO";
        public const string Integracion = "INTEGRACION";
        public const string Servicio = "SERVICIO";
        public const string Reconexion = "RECONEXION";
        public const string Otro = "OTRO";
    }

    public class EmpresaCargoRecurrenteDto
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public string TipoCargo { get; set; } = TipoCargoRecurrente.Otro;
        public int? IdModulo { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public decimal MontoMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activo { get; set; }
        public string? Observacion { get; set; }
        public string? NombreEmpresa { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class CrearEmpresaCargoRecurrenteDto
    {
        public int IdEmpresa { get; set; }
        public string TipoCargo { get; set; } = TipoCargoRecurrente.Otro;
        public int? IdModulo { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        /// <summary>Si null y Tipo=MODULO, se usa Modulos.PrecioUSD como sugerido.</summary>
        public decimal? MontoMensual { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? Observacion { get; set; }
        public int? IdUsuarioCreacion { get; set; }
    }

    public class ActualizarEmpresaCargoRecurrenteDto
    {
        public int Id { get; set; }
        public decimal MontoMensual { get; set; }
        /// <summary>Opcional: si no viene, se conserva la fecha actual del cargo.</summary>
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? Observacion { get; set; }
        public string? Nombre { get; set; }
        public int? IdUsuarioModificacion { get; set; }
    }

    public class SuscripcionLineaFacturaDto
    {
        public string TipoLinea { get; set; } = "";
        public int? IdCargo { get; set; }
        public int? IdModulo { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = "";
        /// <summary>Monto en USD (cobro oficial).</summary>
        public decimal Monto { get; set; }
        /// <summary>Equivalente en DOP a la tasa fija del ciclo.</summary>
        public decimal MontoDop { get; set; }
    }

    public class SuscripcionCalculoFacturaDto
    {
        public int IdEmpresa { get; set; }
        public int? IdPlan { get; set; }
        /// <summary>Nombre dinámico: "Plan {NombreComercial}".</summary>
        public string? NombrePlan { get; set; }
        public decimal MontoServicio { get; set; }
        public decimal CargoAdicional { get; set; }
        public int LimiteFacturacion { get; set; }
        /// <summary>Cargo de reconexión en RD$ configurado para la empresa.</summary>
        public decimal CargoReconexionDop { get; set; }
        /// <summary>Si true, el total incluye la línea de reconexión.</summary>
        public bool ReconexionPendiente { get; set; }
        /// <summary>Monto de reconexión aplicado en este cálculo (USD).</summary>
        public decimal MontoReconexion { get; set; }
        public decimal MontoReconexionDop { get; set; }
        /// <summary>Alias de MontoServicio (compat UI).</summary>
        public decimal MontoPlan { get; set; }
        /// <summary>Cargos adicionales + reconexión (compat UI desglose).</summary>
        public decimal MontoCargos { get; set; }
        public decimal Total { get; set; }
        /// <summary>Tasa fija USD→DOP (ej. 60).</summary>
        public decimal TasaUsdDop { get; set; } = 60m;
        public decimal MontoPlanDop { get; set; }
        public decimal MontoCargosDop { get; set; }
        public decimal TotalDop { get; set; }
        public List<SuscripcionLineaFacturaDto> Lineas { get; set; } = new();
    }

    /// <summary>Actualiza tarifa dinámica del cliente (sin catálogo de planes).</summary>
    public class ActualizarTarifaEmpresaDto
    {
        public int IdEmpresa { get; set; }
        public decimal MontoServicio { get; set; }
        public decimal CargoAdicional { get; set; }
        /// <summary>0 = sin límite de facturas mensuales.</summary>
        public int LimiteFacturacion { get; set; }
        /// <summary>Cargo por reconexión en RD$. Null = no cambiar.</summary>
        public decimal? CargoReconexionDop { get; set; }
        public int? IdUsuario { get; set; }
    }

    /// <summary>Legado — preferir <see cref="ActualizarTarifaEmpresaDto"/>.</summary>
    public class ActualizarPrecioPlanEspecialDto
    {
        public int IdEmpresa { get; set; }
        public decimal? PrecioPlanEspecialUsd { get; set; }
        public int? IdUsuario { get; set; }
    }
}
