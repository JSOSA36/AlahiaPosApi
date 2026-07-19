using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Construye solicitudes de asiento para ventas. Usa montos del evento;
    /// las cuentas se resuelven en fases posteriores vía mapeo parametrizado.
    /// Por ahora retorna lista vacía si no hay líneas explícitas en el evento.
    /// </summary>
    internal static class ContabilidadVentaAsientoBuilder
    {
        public static List<ContabilidadIntegracionRequest> Construir(
            VentaConfirmadaEvent venta,
            ContabilidadConfiguracion config)
        {
            var resultado = new List<ContabilidadIntegracionRequest>();

            // Fase 3.1: aquí se resolverán cuentas vía ContabilidadCuentaMapeo.
            // La infraestructura está lista; el builder se completará al cablear ventas.
            // Mientras tanto, si el evento no trae líneas pre-calculadas, no genera asiento.
            if (venta.Total <= 0)
                return resultado;

            // Placeholder: se activará cuando exista mapeo de cuentas por empresa.
            // Evita generar asientos incorrectos sin parametrización.
            return resultado;
        }
    }
}
