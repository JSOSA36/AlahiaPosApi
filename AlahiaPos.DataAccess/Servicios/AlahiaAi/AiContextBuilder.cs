using AlahiaPos.Entities.Interfaces.AlahiaAi;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AiContextBuilder : IAiContextBuilder
    {
        private readonly IAlahiaAiErpGateway _erp;
        private readonly IAiPermissionService _permissions;

        public AiContextBuilder(IAlahiaAiErpGateway erp, IAiPermissionService permissions)
        {
            _erp = erp;
            _permissions = permissions;
        }

        public async Task<(string Intent, object Context)> BuildAsync(
            string message,
            int idEmpresa,
            IReadOnlyCollection<string> modulosPermitidos,
            CancellationToken ct = default)
        {
            var intent = DetectIntent(message);
            if (!_permissions.CanUseIntent(intent, modulosPermitidos))
            {
                return (intent, new
                {
                    error = "Sin permiso",
                    mensaje = $"No tienes permiso para consultar '{intent}'. Módulos requeridos: {string.Join(", ", _permissions.RequiredModulesForIntent(intent))}."
                });
            }

            object context = intent switch
            {
                "ventas_hoy" => await _erp.GetVentasHoyAsync(idEmpresa, ct),
                "clientes_deben" => await _erp.GetClientesDebenAsync(idEmpresa, ct),
                "stock_bajo" => await _erp.GetStockBajoAsync(idEmpresa, ct),
                "utilidad_mes" => await _erp.GetUtilidadMesAsync(idEmpresa, ct),
                "gastos_altos" => await _erp.GetGastosAltosAsync(idEmpresa, ct),
                "productos_sin_rotar" => await _erp.GetProductosSinRotarAsync(idEmpresa, ct),
                "catalogo_productos" => await _erp.GetCatalogoProductosAsync(idEmpresa, ct),
                "facturas_vencidas" => await _erp.GetFacturasVencidasAsync(idEmpresa, ct),
                "flujo_caja" => await _erp.GetFlujoCajaAsync(idEmpresa, ct),
                "top_productos" => await _erp.GetTopProductosAsync(idEmpresa, ct),
                "conteo_clientes" => await _erp.GetConteoClientesAsync(idEmpresa, ct),
                "ayuda" => await _erp.GetAyudaDocumentalAsync(message, ct),
                "resumen" => await _erp.GetResumenOperativoAsync(idEmpresa, ct),
                _ => await _erp.GetResumenOperativoAsync(idEmpresa, ct)
            };

            return (intent, context);
        }

        public static string DetectIntent(string message)
        {
            var q = Normalize(message);

            if (ContainsAny(q, "como hago", "como registro", "como cierro", "ayuda", "tutorial"))
                return "ayuda";

            if (ContainsAny(q, "cuantos clientes", "cuanto clientes", "clientes tengo", "clientes registrados",
                    "numero de clientes", "total de clientes", "cuantos clientes tengo"))
                return "conteo_clientes";

            if (ContainsAny(q, "cuantos productos", "cuanto productos", "cuanot productos",
                    "cuantos servicios", "cuanto servicios",
                    "productos tengo", "servicios tengo", "productos registrados", "servicios registrados",
                    "productos o servicios", "catalogo", "que productos tengo", "que productos hay"))
                return "catalogo_productos";

            if (ContainsAny(q, "mas rentables", "productos rentables", "top productos", "mejores productos",
                    "productos mas vendidos", "que se vende mas", "que vende mas"))
                return "top_productos";

            if (ContainsAny(q, "flujo de caja", "flujo caja", "mi caja", "como esta mi caja", "como esta la caja",
                    "bancos", "liquidez", "efectivo disponible", "saldo en caja", "saldo en banco"))
                return "flujo_caja";

            if (ContainsAny(q, "vendi hoy", "ventas de hoy", "cuanto vendi hoy", "cuantos vendi hoy",
                    "venta del dia", "vende hoy", "vendio hoy"))
                return "ventas_hoy";

            if (ContainsAny(q, "venta del mes", "ventas del mes", "total de venta", "total de ventas",
                    "cuanto vendi", "cuantos vendi", "ventas brutas", "vendi este mes", "vende este mes"))
                return "utilidad_mes";

            if (ContainsAny(q, "me deben", "cuentas por cobrar", "clientes deben", "quien me debe",
                    "quien debe", "cuanto me deben", "cuantos me deben", "deuda de clientes"))
                return "clientes_deben";

            if (ContainsAny(q, "poco inventario", "stock bajo", "inventario critico", "reabastecer", "sin stock",
                    "inventario bajo", "faltante de inventario"))
                return "stock_bajo";

            if (ContainsAny(q, "utilidad", "ganancia", "resultado del mes", "margen"))
                return "utilidad_mes";

            if (ContainsAny(q, "gastos", "gaste", "egresos", "gaste mucho"))
                return "gastos_altos";

            if (ContainsAny(q, "no se venden", "sin rotar", "inmovilizado", "casi no se venden"))
                return "productos_sin_rotar";

            if (ContainsAny(q, "vencidas", "facturas vencidas", "por cobrar vencid", "atrasadas"))
                return "facturas_vencidas";

            if (ContainsAny(q, "resumen", "que debo mirar", "hoy tengo", "como va el negocio", "estado del negocio"))
                return "resumen";

            return "resumen";
        }

        private static string Normalize(string message)
        {
            var q = (message ?? string.Empty).ToLowerInvariant();
            return q
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
                .Replace("¿", "").Replace("?", "");
        }

        private static bool ContainsAny(string q, params string[] terms)
            => terms.Any(t => q.Contains(t, StringComparison.OrdinalIgnoreCase));
    }
}
