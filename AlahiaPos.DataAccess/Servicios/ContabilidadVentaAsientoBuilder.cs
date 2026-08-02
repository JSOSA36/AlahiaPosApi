using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Stub reemplazado: la lógica vive en ContabilidadAsientoBuilders.DesdeVentaAsync.
    /// Se mantiene el tipo por compatibilidad de referencias.
    /// </summary>
    internal static class ContabilidadVentaAsientoBuilder
    {
        public static Task<List<ContabilidadIntegracionRequest>> Construir(
            VentaConfirmadaEvent venta,
            ContabilidadConfiguracion config,
            IContabilidadCuentaMapeoService mapeo)
            => ContabilidadAsientoBuilders.DesdeVentaAsync(venta, config, mapeo);
    }
}
