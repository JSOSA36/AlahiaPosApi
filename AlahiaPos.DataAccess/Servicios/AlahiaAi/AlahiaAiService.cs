using System.Text.Json;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AlahiaAiService : IAlahiaAiService
    {
        private readonly AlahiaAiOptions _options;
        private readonly IAiProviderFactory _providers;
        private readonly IAiPromptManager _prompts;
        private readonly IAiContextBuilder _contextBuilder;
        private readonly IAiConversationHistory _history;
        private readonly IAiPermissionService _permissions;
        private readonly IAiUsageMonitor _usage;
        private readonly IAlahiaAiErpGateway _erp;
        private readonly IAiSqlExecutor _sql;

        public AlahiaAiService(
            IOptions<AlahiaAiOptions> options,
            IAiProviderFactory providers,
            IAiPromptManager prompts,
            IAiContextBuilder contextBuilder,
            IAiConversationHistory history,
            IAiPermissionService permissions,
            IAiUsageMonitor usage,
            IAlahiaAiErpGateway erp,
            IAiSqlExecutor sql)
        {
            _options = options.Value;
            _providers = providers;
            _prompts = prompts;
            _contextBuilder = contextBuilder;
            _history = history;
            _permissions = permissions;
            _usage = usage;
            _erp = erp;
            _sql = sql;
        }

        public async Task<AlahiaAiChatResponse> ChatAsync(AlahiaAiChatRequest request, CancellationToken ct = default)
        {
            if (!_options.Enabled)
            {
                return new AlahiaAiChatResponse
                {
                    ConversationId = request.ConversationId ?? string.Empty,
                    Answer = "Alahia AI está deshabilitada en este ambiente.",
                    Intent = "disabled",
                    SuggestTicket = true
                };
            }

            if (!_permissions.HasAlahiaAi(request.ModulosPermitidos))
            {
                return new AlahiaAiChatResponse
                {
                    ConversationId = request.ConversationId ?? string.Empty,
                    Answer = "Tu usuario no tiene el módulo Alahia AI habilitado.",
                    Intent = "forbidden",
                    SuggestTicket = false
                };
            }

            var conversationId = _history.EnsureConversation(request.ConversationId, request.IdEmpresa, request.IdUsuario);
            _history.AddMessage(conversationId, new AiConversationMessage
            {
                Role = "user",
                Content = request.Message
            });

            // Fase SQL: si está habilitado y no es ayuda, consultar datos vía SELECT + aislamiento SQL
            if (_sql.IsEnabled && !EsAyuda(request.Message))
            {
                var sqlPath = await TrySqlDataPathAsync(request, conversationId, ct);
                if (sqlPath != null)
                    return sqlPath;
            }

            var (intent, context) = await _contextBuilder.BuildAsync(
                request.Message,
                request.IdEmpresa,
                request.ModulosPermitidos,
                ct);

            var contextJson = JsonSerializer.Serialize(context);
            var suggestTicket = intent == "ayuda" || contextJson.Contains("sugerirTicket\":true", StringComparison.OrdinalIgnoreCase);

            string answer;
            bool usedLlm = false;
            string providerName = "Template";
            var completion = new AiCompletionResult();

            var provider = await _providers.ResolveForEmpresaAsync(request.IdEmpresa, ct);
            if (provider.IsConfigured)
            {
                completion = await provider.CompleteAsync(new AiCompletionRequest
                {
                    SystemPrompt = _prompts.GetSystemPrompt(),
                    UserPrompt = _prompts.BuildAnswerPrompt(request.Message, intent, contextJson),
                    Temperature = 0.2,
                    MaxTokens = 700
                }, ct);

                if (completion.Success && !string.IsNullOrWhiteSpace(completion.Text))
                {
                    answer = completion.Text;
                    usedLlm = true;
                    providerName = completion.Provider;
                }
                else
                {
                    answer = _prompts.BuildTemplateAnswer(intent, contextJson);
                    if (!string.IsNullOrWhiteSpace(completion.Error))
                        answer += $"\n\n(Nota: el proveedor {provider.ProviderId} no respondió: {completion.Error})";
                }
            }
            else
            {
                answer = _prompts.BuildTemplateAnswer(intent, contextJson);
            }

            _history.AddMessage(conversationId, new AiConversationMessage
            {
                Role = "assistant",
                Content = answer,
                Intent = intent
            });

            await _usage.TrackAsync(new AiUsageLogEntry
            {
                IdEmpresa = request.IdEmpresa,
                IdUsuario = request.IdUsuario,
                Provider = providerName,
                Model = completion.Model,
                Feature = "chat",
                Intent = intent,
                PromptTokens = completion.PromptTokens,
                CompletionTokens = completion.CompletionTokens,
                EstimatedCostUsd = completion.EstimatedCostUsd,
                Success = usedLlm || providerName == "Template",
                Error = completion.Error
            }, ct);

            return new AlahiaAiChatResponse
            {
                ConversationId = conversationId,
                Answer = answer,
                Intent = intent,
                UsedLlm = usedLlm,
                Provider = providerName,
                SuggestTicket = suggestTicket,
                Insights = ExtractInsights(contextJson),
                ContextPreview = context
            };
        }

        public async Task<AlahiaAiResumenResponse> ResumenAsync(
            int idEmpresa,
            int idUsuario,
            IReadOnlyCollection<string> modulos,
            CancellationToken ct = default)
        {
            if (!_options.Enabled || !_permissions.HasAlahiaAi(modulos))
            {
                return new AlahiaAiResumenResponse
                {
                    Greeting = Saludo(),
                    Bullets = new List<string> { "Alahia AI no está disponible para este usuario." },
                    Provider = "none"
                };
            }

            var context = await _erp.GetResumenOperativoAsync(idEmpresa, ct);
            var contextJson = JsonSerializer.Serialize(context);
            var bullets = BuildBulletsFromContext(contextJson);
            var greeting = Saludo();
            var usedLlm = false;
            var providerName = "Template";

            var provider = await _providers.ResolveForEmpresaAsync(idEmpresa, ct);
            if (provider.IsConfigured)
            {
                var completion = await provider.CompleteAsync(new AiCompletionRequest
                {
                    SystemPrompt = _prompts.GetSystemPrompt(),
                    UserPrompt = _prompts.BuildResumenPrompt(contextJson),
                    Temperature = 0.3,
                    MaxTokens = 500
                }, ct);

                await _usage.TrackAsync(new AiUsageLogEntry
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = idUsuario,
                    Provider = completion.Provider,
                    Model = completion.Model,
                    Feature = "resumen",
                    Intent = "resumen",
                    PromptTokens = completion.PromptTokens,
                    CompletionTokens = completion.CompletionTokens,
                    EstimatedCostUsd = completion.EstimatedCostUsd,
                    Success = completion.Success,
                    Error = completion.Error
                }, ct);

                if (completion.Success && !string.IsNullOrWhiteSpace(completion.Text))
                {
                    usedLlm = true;
                    providerName = completion.Provider;
                    var lines = completion.Text
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(l => l.StartsWith("•") || l.StartsWith("-") || l.StartsWith("*") || char.IsDigit(l[0]))
                        .Select(l => l.TrimStart('•', '-', '*', ' ', '\t'))
                        .Where(l => l.Length > 3)
                        .Take(6)
                        .ToList();

                    if (lines.Count > 0) bullets = lines;
                    var first = completion.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first) && !first.StartsWith("•") && !first.StartsWith("-"))
                        greeting = first.Trim();
                }
            }

            return new AlahiaAiResumenResponse
            {
                Greeting = greeting,
                Bullets = bullets,
                Provider = providerName,
                UsedLlm = usedLlm,
                GeneratedAt = DateTime.Now
            };
        }

        private static string Saludo()
        {
            var h = DateTime.Now.Hour;
            if (h < 12) return "Buenos días.";
            if (h < 19) return "Buenas tardes.";
            return "Buenas noches.";
        }

        private static List<string> BuildBulletsFromContext(string contextJson)
        {
            var bullets = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(contextJson);
                var root = doc.RootElement;

                // Prioridad: cobros / vencidas → stock → flujo → utilidad → ventas
                if (root.TryGetProperty("cxC", out var cxc))
                {
                    var cant = cxc.TryGetProperty("cantidad", out var c) ? c.GetInt32() : 0;
                    var total = cxc.TryGetProperty("totalPendiente", out var tp) ? tp.GetDecimal() : 0m;
                    if (cant > 0)
                        bullets.Add($"Cobros: {cant} clientes te deben RD$ {total:N2}. Prioriza llamadas o visitas hoy.");
                }

                if (root.TryGetProperty("vencidas", out var vx) && vx.TryGetProperty("vencidas", out var vv))
                {
                    var n = vv.GetInt32();
                    if (n > 0)
                        bullets.Add($"Hay {n} facturas vencidas: abre Cuentas por cobrar y gestiona las más atrasadas.");
                }

                if (root.TryGetProperty("stock", out var st) && st.TryGetProperty("cantidad", out var sc))
                {
                    var n = sc.GetInt32();
                    if (n > 0)
                        bullets.Add($"Stock crítico: {n} productos bajo mínimo. Revisa reposición antes de que frene la venta.");
                }

                if (root.TryGetProperty("flujo", out var fl))
                {
                    var liquidez = fl.TryGetProperty("liquidez", out var liq) ? liq.GetDecimal() : 0m;
                    var caja = fl.TryGetProperty("caja", out var cj) ? cj.GetDecimal() : 0m;
                    var bancos = fl.TryGetProperty("bancos", out var bn) ? bn.GetDecimal() : 0m;
                    bullets.Add($"Flujo: liquidez RD$ {liquidez:N2} (caja {caja:N2} + bancos {bancos:N2}).");
                }

                if (root.TryGetProperty("utilidad", out var ut) && ut.TryGetProperty("utilidadOperativa", out var uo))
                {
                    var util = uo.GetDecimal();
                    var margen = ut.TryGetProperty("margenOperativoPct", out var mo) ? mo.GetDecimal() : 0m;
                    bullets.Add(
                        util >= 0
                            ? $"Utilidad operativa del mes: RD$ {util:N2} (margen {margen:N1}%). Mantén control de gastos."
                            : $"Utilidad operativa negativa: RD$ {util:N2}. Revisa gastos altos y margen de productos.");
                }

                if (root.TryGetProperty("ventasHoy", out var vh))
                {
                    var v = vh.GetDecimal();
                    bullets.Add(
                        v > 0
                            ? $"Ventas de hoy: RD$ {v:N2}. Compara con tu meta diaria si la tienes definida."
                            : "Aún no hay ventas registradas hoy. Verifica turnos abiertos en el POS.");
                }
            }
            catch { /* ignore */ }

            if (bullets.Count == 0)
                bullets.Add("Revisa cobros, stock crítico y flujo de caja para mantener el control del día.");
            return bullets.Take(6).ToList();
        }

        private static List<string> ExtractInsights(string contextJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(contextJson);
                if (doc.RootElement.TryGetProperty("mensaje", out var m))
                    return new List<string> { m.GetString() ?? string.Empty };
            }
            catch { }
            return new List<string>();
        }

        private static bool EsAyuda(string message)
        {
            var t = (message ?? string.Empty).Trim().ToLowerInvariant();
            return t.Contains("ayuda") || t.Contains("cómo") || t.Contains("como uso") || t.Contains("manual");
        }

        private async Task<AlahiaAiChatResponse?> TrySqlDataPathAsync(
            AlahiaAiChatRequest request,
            string conversationId,
            CancellationToken ct)
        {
            var provider = await _providers.ResolveForEmpresaAsync(request.IdEmpresa, ct);
            if (!provider.IsConfigured)
                return null;

            var catalog = await _sql.GetCatalogAsync(ct);
            var gen = await provider.CompleteAsync(new AiCompletionRequest
            {
                SystemPrompt =
                    "Eres un generador de SQL Server de solo lectura para Alahia ERP. " +
                    "Responde únicamente con una sentencia SELECT sobre vistas ai.v_*.",
                UserPrompt = _prompts.BuildSqlGenerationPrompt(request.Message, catalog),
                Temperature = 0,
                MaxTokens = 400
            }, ct);

            if (!gen.Success || string.IsNullOrWhiteSpace(gen.Text))
                return null;

            var sql = ExtraerSql(gen.Text);
            var exec = await _sql.ExecuteAsync(request.IdEmpresa, sql, requireTenantContext: true, ct);
            if (!exec.Success)
                return null;

            var answerCompletion = await provider.CompleteAsync(new AiCompletionRequest
            {
                SystemPrompt = _prompts.GetSystemPrompt(),
                UserPrompt = _prompts.BuildSqlAnswerPrompt(request.Message, exec.SqlExecuted ?? sql, exec.JsonRows),
                Temperature = 0.2,
                MaxTokens = 700
            }, ct);

            var answer = answerCompletion.Success && !string.IsNullOrWhiteSpace(answerCompletion.Text)
                ? answerCompletion.Text
                : $"Consulté los datos ({exec.RowCount} filas). No pude redactar la respuesta con el LLM.";

            _history.AddMessage(conversationId, new AiConversationMessage
            {
                Role = "assistant",
                Content = answer,
                Intent = "sql_select"
            });

            await _usage.TrackAsync(new AiUsageLogEntry
            {
                IdEmpresa = request.IdEmpresa,
                IdUsuario = request.IdUsuario,
                Provider = provider.ProviderId,
                Model = answerCompletion.Model ?? gen.Model,
                Feature = "chat_sql",
                Intent = "sql_select",
                PromptTokens = gen.PromptTokens + answerCompletion.PromptTokens,
                CompletionTokens = gen.CompletionTokens + answerCompletion.CompletionTokens,
                EstimatedCostUsd = gen.EstimatedCostUsd + answerCompletion.EstimatedCostUsd,
                Success = true
            }, ct);

            return new AlahiaAiChatResponse
            {
                ConversationId = conversationId,
                Answer = answer,
                Intent = "sql_select",
                UsedLlm = true,
                Provider = provider.ProviderId,
                SuggestTicket = false,
                Insights = new List<string>
                {
                    exec.Truncated ? $"Resultado truncado a {_options.Sql.MaxRows} filas." : $"{exec.RowCount} filas."
                },
                ContextPreview = new
                {
                    sql = exec.SqlExecuted,
                    rowCount = exec.RowCount,
                    truncated = exec.Truncated
                }
            };
        }

        private static string ExtraerSql(string raw)
        {
            var t = (raw ?? string.Empty).Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                var lines = t.Split('\n');
                t = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```")));
            }
            return t.Trim().TrimEnd(';');
        }
    }
}
