using System;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Request canónico para registrar un gasto (UI Gastos o Conciliación).
    /// Una sola ruta de negocio: movimiento + fila Gastos + evento contable.
    /// </summary>
    public class RegistrarGastoRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
        public string TipoGasto { get; set; } = string.Empty;
        public int? IdCategoriaGasto { get; set; }
        public string? Detalle { get; set; }
        public string? FormaPago { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public string? Referencia { get; set; }
        /// <summary>MANUAL | COMPRAS | CONCILIACION</summary>
        public string OrigenModulo { get; set; } = "MANUAL";
        public DateTime? Fecha { get; set; }
        public string? TipoComprobante { get; set; }
        public string? NumeroComprobante { get; set; }
        public DateTime? FechaComprobante { get; set; }
        public int IdProveedor { get; set; } = 1;

        /// <summary>Tipo de Bienes y Servicios DGII (1–11). Requerido para gastos menores / 606.</summary>
        public int? IdTipoBienesServicios { get; set; }

        /// <summary>
        /// Si true, no valida fondos (origen extracto / banco fuente de verdad).
        /// El movimiento usa ReferenciaTipo EXTRACTO para el bypass en tesorería.
        /// </summary>
        public bool DesdeExtractoBancario { get; set; }

        public int? IdTesoreriaExtractoLinea { get; set; }
        public int? IdTesoreriaConciliacion { get; set; }
        public string? ClaveIdempotencia { get; set; }
        public DateTime? FechaMovimiento { get; set; }
        public int? IdSucursal { get; set; }
    }

    public class RegistrarGastoResult
    {
        public int IdGasto { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public string? ContabilidadAdvertencia { get; set; }
        public bool YaExistia { get; set; }
    }

    /// <summary>
    /// Request canónico para ingreso extraordinario (UI Ingresos o Conciliación).
    /// </summary>
    public class RegistrarIngresoExtraRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        /// <summary>Texto libre de origen de negocio; para conciliacion: "Conciliación Bancaria".</summary>
        public string? Origen { get; set; }
        public string? FormaPago { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public string? Referencia { get; set; }
        public string? Nota { get; set; }
        public DateTime? Fecha { get; set; }

        public bool DesdeExtractoBancario { get; set; }
        public int? IdTesoreriaExtractoLinea { get; set; }
        public int? IdTesoreriaConciliacion { get; set; }
        public string? ClaveIdempotencia { get; set; }
        public DateTime? FechaMovimiento { get; set; }
    }

    public class RegistrarIngresoExtraResult
    {
        public int IdIngreso { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public string? ContabilidadAdvertencia { get; set; }
        public bool YaExistia { get; set; }
    }
}
