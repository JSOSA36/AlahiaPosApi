using System.Text.Json;
using AlahiaPos.Entities.Interfaces.AlahiaAi;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AiPromptManager : IAiPromptManager
    {
        public string GetSystemPrompt() =>
            "Eres Alahia AI, el asesor empresarial integrado en Alahia ERP (República Dominicana).\n" +
            "Reglas:\n" +
            "- Solo usa el contexto JSON que te entregan. No inventes montos, clientes ni productos.\n" +
            "- Si falta información, dilo con claridad.\n" +
            "- Explica qué está ocurriendo en español claro, como un asesor (no solo listes números).\n" +
            "- Usa RD$ cuando hables de dinero.\n" +
            "- Sé conciso y accionable.";

        public string BuildAnswerPrompt(string userMessage, string intent, string contextJson) =>
            "Pregunta del usuario: " + userMessage + "\n" +
            "Intent detectado: " + intent + "\n" +
            "Contexto del ERP (JSON):\n" +
            contextJson + "\n\n" +
            "Responde como asesor de Alahia AI.";

        public string BuildResumenPrompt(string contextJson) =>
            "Genera un \"Resumen Inteligente\" matutino para el empresario.\n" +
            "Usa este contexto JSON del ERP:\n" +
            contextJson + "\n\n" +
            "Devuelve:\n" +
            "1) Un saludo breve (Buenos días / Buenas tardes según hora local).\n" +
            "2) Entre 4 y 6 viñetas accionables (facturas por cobrar, stock crítico, ventas, pagos, flujo de caja).\n" +
            "No inventes datos fuera del JSON.";

        public string BuildSqlGenerationPrompt(string userMessage, string catalogJson) =>
            "Genera UNA sola consulta SQL Server de solo lectura para responder la pregunta.\n" +
            "Reglas estrictas:\n" +
            "- Solo SELECT (o WITH ... SELECT).\n" +
            "- Usa únicamente vistas del schema ai (ej. ai.v_FacturaHeaders).\n" +
            "- NO filtres por IdEmpresa: el aislamiento lo aplica SQL Server.\n" +
            "- Prefiere agregaciones (SUM, COUNT, TOP 20).\n" +
            "- Fechas en República Dominicana; mes actual si no se indica.\n" +
            "- Facturas de venta típicas: IdTipoDocumentos = 1 y EstaCancelada = 0.\n" +
            "- Responde SOLO con el SQL, sin markdown ni explicación.\n\n" +
            "Catálogo de vistas:\n" + catalogJson + "\n\n" +
            "Pregunta: " + userMessage;

        public string BuildSqlAnswerPrompt(string userMessage, string sql, string rowsJson) =>
            "Pregunta del usuario: " + userMessage + "\n" +
            "SQL ejecutado (solo lectura):\n" + sql + "\n" +
            "Resultado JSON (puede estar truncado):\n" + rowsJson + "\n\n" +
            "Responde en español claro como asesor Alahia AI. Usa RD$. No inventes filas que no estén en el JSON.";

        public string BuildTemplateAnswer(string intent, string contextJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(contextJson) ? "{}" : contextJson);
                var root = doc.RootElement;
                return intent switch
                {
                    "ventas_hoy" => FormatMoney(root, "total", "Hoy has vendido"),
                    "utilidad_mes" => FormatUtilidadMes(root),
                    "catalogo_productos" => FormatCatalogo(root),
                    "flujo_caja" => FormatFlujoCaja(root),
                    "top_productos" => FormatTopProductos(root),
                    "conteo_clientes" => FormatConteoClientes(root),
                    "clientes_deben" => FormatList(root, "clientes", "clientes con saldo pendiente", "nombre", "saldo"),
                    "stock_bajo" => FormatList(root, "productos", "productos con inventario bajo", "nombre", "existencia"),
                    "gastos_altos" => FormatList(root, "gastos", "gastos más altos del mes", "descripcion", "monto"),
                    "facturas_vencidas" => FormatList(root, "facturas", "facturas vencidas o por cobrar", "cliente", "pendiente"),
                    "productos_sin_rotar" => FormatList(root, "productos", "productos con poca o nula rotación", "nombre", "existencia"),
                    "ayuda" => root.TryGetProperty("respuesta", out var r) ? r.GetString() ?? "Consulta la ayuda del ERP o crea un ticket." : "Consulta la ayuda del ERP o crea un ticket.",
                    _ => "Analicé tu consulta con los datos disponibles del ERP. Si necesitas más detalle, reformula la pregunta (ventas, cobros, inventario, utilidad, caja o clientes)."
                };
            }
            catch
            {
                return "Tengo los datos del ERP, pero no pude redactar la respuesta automáticamente. Intenta de nuevo o configura el proveedor de IA.";
            }
        }

        private static string FormatMoney(JsonElement root, string prop, string prefix)
        {
            var value = root.TryGetProperty(prop, out var p) ? p.GetDecimal() : 0m;
            return $"{prefix} RD$ {value:N2}.";
        }

        private static string FormatUtilidadMes(JsonElement root)
        {
            var periodo = root.TryGetProperty("periodo", out var per) ? per.GetString() : "este mes";
            var ventas = root.TryGetProperty("ventasBrutas", out var vb) ? vb.GetDecimal() : 0m;
            var utilidad = root.TryGetProperty("utilidadOperativa", out var uo) ? uo.GetDecimal() : 0m;
            var margen = root.TryGetProperty("margenOperativoPct", out var mo) ? mo.GetDecimal() : 0m;
            return
                $"En {periodo}, las ventas brutas fueron RD$ {ventas:N2}.\n" +
                $"La utilidad operativa es RD$ {utilidad:N2} (margen {margen:N1}%).";
        }

        private static string FormatCatalogo(JsonElement root)
        {
            var total = root.TryGetProperty("totalActivos", out var ta) ? ta.GetInt32() : 0;
            var productos = root.TryGetProperty("productos", out var pr) ? pr.GetInt32() : 0;
            var servicios = root.TryGetProperty("servicios", out var se) ? se.GetInt32() : 0;
            var inactivos = root.TryGetProperty("inactivos", out var ina) ? ina.GetInt32() : 0;
            return
                $"Tienes {total} ítems activos en el catálogo: {productos} productos y {servicios} servicios." +
                (inactivos > 0 ? $" Además hay {inactivos} inactivos." : "");
        }

        private static string FormatFlujoCaja(JsonElement root)
        {
            var caja = root.TryGetProperty("caja", out var c) ? c.GetDecimal() : 0m;
            var bancos = root.TryGetProperty("bancos", out var b) ? b.GetDecimal() : 0m;
            var liquidez = root.TryGetProperty("liquidez", out var l) ? l.GetDecimal() : caja + bancos;
            var cxC = root.TryGetProperty("cuentasPorCobrar", out var cc) ? cc.GetDecimal() : 0m;
            var cxP = root.TryGetProperty("cuentasPorPagar", out var cp) ? cp.GetDecimal() : 0m;
            return
                $"Tu liquidez disponible es RD$ {liquidez:N2} (caja RD$ {caja:N2} + bancos RD$ {bancos:N2}).\n" +
                $"Cuentas por cobrar: RD$ {cxC:N2}. Cuentas por pagar: RD$ {cxP:N2}.\n" +
                (liquidez < cxP
                    ? "Prioriza cobros: los compromisos a pagar superan el efectivo disponible."
                    : "Tienes cobertura de caja/bancos frente a lo por pagar; mantén el ritmo de cobros.");
        }

        private static string FormatTopProductos(JsonElement root)
        {
            if (!root.TryGetProperty("productos", out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                return "Aún no hay productos con margen suficiente en el período para armar un top.";

            var periodo = root.TryGetProperty("periodo", out var per) ? per.GetString() : "este mes";
            var lines = new List<string> { $"Estos son tus productos más rentables en {periodo}:" };
            var i = 0;
            foreach (var item in arr.EnumerateArray())
            {
                if (i++ >= 8) break;
                var name = item.TryGetProperty("nombre", out var n) ? n.GetString() : "—";
                var margen = item.TryGetProperty("margen", out var m) ? m.GetDecimal() : 0m;
                var pct = item.TryGetProperty("rentabilidadPct", out var p) ? p.GetDecimal() : 0m;
                lines.Add($"• {name}: margen RD$ {margen:N2} ({pct:N1}%)");
            }
            lines.Add("Enfoca inventario y promociones en estos ítems.");
            return string.Join("\n", lines);
        }

        private static string FormatConteoClientes(JsonElement root)
        {
            var total = root.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
            var activos = root.TryGetProperty("activos", out var a) ? a.GetInt32() : 0;
            var credito = root.TryGetProperty("conLimiteCredito", out var c) ? c.GetInt32() : 0;
            return
                $"Tienes {total} clientes registrados ({activos} activos)." +
                (credito > 0 ? $" {credito} tienen límite de crédito configurado." : "");
        }

        private static string FormatList(JsonElement root, string arrayName, string label, string nameProp, string valueProp)
        {
            if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                return $"No encontré {label} en este momento.";

            var lines = new List<string> { $"Esto es lo que veo sobre {label}:" };
            var i = 0;
            foreach (var item in arr.EnumerateArray())
            {
                if (i++ >= 8) break;
                var name = item.TryGetProperty(nameProp, out var n) ? n.GetString() : "—";
                var val = item.TryGetProperty(valueProp, out var v)
                    ? (v.ValueKind == JsonValueKind.Number ? $"RD$ {v.GetDecimal():N2}" : v.ToString())
                    : "";
                lines.Add($"• {name}: {val}");
            }
            return string.Join("\n", lines);
        }
    }
}
