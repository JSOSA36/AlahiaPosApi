using System.Collections.Concurrent;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AiPermissionService : IAiPermissionService
    {
        public bool HasAlahiaAi(IReadOnlyCollection<string> modulosPermitidos)
            => Contains(modulosPermitidos, "ALAHIA_AI") || Contains(modulosPermitidos, "DASHBOARD");

        public bool CanUseIntent(string intent, IReadOnlyCollection<string> modulosPermitidos)
        {
            if (!HasAlahiaAi(modulosPermitidos)) return false;
            var required = RequiredModulesForIntent(intent);
            if (required.Count == 0) return true;
            return required.Any(r => Contains(modulosPermitidos, r));
        }

        public IReadOnlyList<string> RequiredModulesForIntent(string intent) => intent switch
        {
            "ventas_hoy" => new[] { "POS", "DASHBOARD", "HISTORICO_FACTURAS", "ALAHIA_AI" },
            "utilidad_mes" or "flujo_caja" or "top_productos" => new[] { "DASHBOARD", "ALAHIA_AI" },
            "clientes_deben" or "facturas_vencidas" => new[] { "CUENTAS_COBRAR", "DASHBOARD", "ALAHIA_AI" },
            "conteo_clientes" => new[] { "CLIENTES", "CUENTAS_COBRAR", "DASHBOARD", "ALAHIA_AI" },
            "stock_bajo" or "productos_sin_rotar" or "catalogo_productos" =>
                new[] { "PRODUCTOS", "MOVIMIENTO_INVENTARIO", "ALAHIA_AI", "DASHBOARD" },
            "gastos_altos" => new[] { "GASTOS", "DASHBOARD", "ALAHIA_AI" },
            "ayuda" => Array.Empty<string>(),
            "resumen" => new[] { "ALAHIA_AI", "DASHBOARD" },
            _ => new[] { "ALAHIA_AI", "DASHBOARD" }
        };

        private static bool Contains(IReadOnlyCollection<string> mods, string code)
            => mods.Any(m => string.Equals(m, code, StringComparison.OrdinalIgnoreCase));
    }

    public class AiUsageMonitor : IAiUsageMonitor
    {
        private static readonly ConcurrentQueue<AiUsageLogEntry> Logs = new();

        public Task TrackAsync(AiUsageLogEntry entry, CancellationToken ct = default)
        {
            Logs.Enqueue(entry);
            while (Logs.Count > 500 && Logs.TryDequeue(out _)) { }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AiUsageLogEntry>> GetRecentAsync(int idEmpresa, int take = 50, CancellationToken ct = default)
        {
            var items = Logs.Where(x => x.IdEmpresa == idEmpresa)
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Clamp(take, 1, 200))
                .ToList();
            return Task.FromResult<IReadOnlyList<AiUsageLogEntry>>(items);
        }
    }
}
