using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers
{
    /// <summary>Cliente compatible con Chat Completions (OpenAI / Azure OpenAI / Ollama openai-compat).</summary>
    public abstract class OpenAiCompatibleProvider : IAiProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _endpoint;
        private readonly bool _useApiKeyHeader;
        private readonly string? _apiVersionQuery;

        protected OpenAiCompatibleProvider(
            HttpClient http,
            string providerId,
            string apiKey,
            string model,
            string endpoint,
            bool useApiKeyHeader = true,
            string? apiVersionQuery = null)
        {
            _http = http;
            ProviderId = providerId;
            _apiKey = apiKey ?? string.Empty;
            _model = model;
            _endpoint = endpoint.TrimEnd('/');
            _useApiKeyHeader = useApiKeyHeader;
            _apiVersionQuery = apiVersionQuery;
        }

        public string ProviderId { get; }
        public virtual bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) || ProviderId == "Ollama";

        public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
        {
            if (!IsConfigured)
            {
                return new AiCompletionResult
                {
                    Success = false,
                    Provider = ProviderId,
                    Model = _model,
                    Error = $"El proveedor {ProviderId} no está configurado."
                };
            }

            var url = string.IsNullOrEmpty(_apiVersionQuery)
                ? $"{_endpoint}/chat/completions"
                : $"{_endpoint}/chat/completions?api-version={_apiVersionQuery}";

            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            if (_useApiKeyHeader && !string.IsNullOrWhiteSpace(_apiKey))
            {
                if (ProviderId == "AzureOpenAI")
                    msg.Headers.Add("api-key", _apiKey);
                else
                    msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            var payload = new
            {
                model = _model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                }
            };

            msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                using var resp = await _http.SendAsync(msg, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    return new AiCompletionResult
                    {
                        Success = false,
                        Provider = ProviderId,
                        Model = _model,
                        Error = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 400)}"
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
                var usage = root.TryGetProperty("usage", out var u) ? u : default;
                var promptTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                var completionTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("completion_tokens", out var ctkn) ? ctkn.GetInt32() : 0;

                return new AiCompletionResult
                {
                    Success = true,
                    Text = text.Trim(),
                    Provider = ProviderId,
                    Model = _model,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    EstimatedCostUsd = EstimateCost(promptTokens, completionTokens)
                };
            }
            catch (Exception ex)
            {
                return new AiCompletionResult
                {
                    Success = false,
                    Provider = ProviderId,
                    Model = _model,
                    Error = ex.Message
                };
            }
        }

        protected virtual decimal EstimateCost(int promptTokens, int completionTokens)
            => (promptTokens * 0.00000015m) + (completionTokens * 0.0000006m);

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
    }

    public sealed class OpenAiProvider : OpenAiCompatibleProvider
    {
        public OpenAiProvider(HttpClient http, IOptions<AlahiaAiOptions> options)
            : base(
                http,
                "OpenAI",
                options.Value.OpenAI.ApiKey,
                options.Value.OpenAI.Model,
                string.IsNullOrWhiteSpace(options.Value.OpenAI.BaseUrl) ? "https://api.openai.com/v1" : options.Value.OpenAI.BaseUrl)
        {
        }
    }

    public sealed class AzureOpenAiProvider : OpenAiCompatibleProvider
    {
        private readonly AzureOpenAiProviderOptions _opts;

        public AzureOpenAiProvider(HttpClient http, IOptions<AlahiaAiOptions> options)
            : base(
                http,
                "AzureOpenAI",
                options.Value.AzureOpenAI.ApiKey,
                options.Value.AzureOpenAI.Deployment,
                BuildEndpoint(options.Value.AzureOpenAI),
                useApiKeyHeader: true,
                apiVersionQuery: options.Value.AzureOpenAI.ApiVersion)
        {
            _opts = options.Value.AzureOpenAI;
        }

        private static string BuildEndpoint(AzureOpenAiProviderOptions o)
        {
            var endpoint = (o.Endpoint ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(o.Deployment))
                return "https://localhost/openai/deployments/missing";
            return $"{endpoint}/openai/deployments/{o.Deployment}";
        }

        public override bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_opts.ApiKey)
            && !string.IsNullOrWhiteSpace(_opts.Endpoint)
            && !string.IsNullOrWhiteSpace(_opts.Deployment);
    }

    public sealed class OllamaProvider : OpenAiCompatibleProvider
    {
        public OllamaProvider(HttpClient http, IOptions<AlahiaAiOptions> options)
            : base(
                http,
                "Ollama",
                apiKey: string.Empty,
                model: options.Value.Ollama.Model,
                endpoint: (options.Value.Ollama.BaseUrl?.TrimEnd('/') ?? "http://localhost:11434") + "/v1",
                useApiKeyHeader: false)
        {
        }

        public override bool IsConfigured => true;
    }

    /// <summary>Stub listo para completar; mantiene el contrato del factory.</summary>
    public sealed class AnthropicProvider : IAiProvider
    {
        private readonly IOptions<AlahiaAiOptions> _options;
        public AnthropicProvider(IOptions<AlahiaAiOptions> options) => _options = options;
        public string ProviderId => "Anthropic";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Value.Anthropic.ApiKey);

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
            => Task.FromResult(new AiCompletionResult
            {
                Success = false,
                Provider = ProviderId,
                Model = _options.Value.Anthropic.Model,
                Error = "Proveedor Anthropic registrado. Implementar Messages API en una siguiente iteración."
            });
    }

    public sealed class GeminiProvider : IAiProvider
    {
        private readonly IOptions<AlahiaAiOptions> _options;
        public GeminiProvider(IOptions<AlahiaAiOptions> options) => _options = options;
        public string ProviderId => "Gemini";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Value.Gemini.ApiKey);

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
            => Task.FromResult(new AiCompletionResult
            {
                Success = false,
                Provider = ProviderId,
                Model = _options.Value.Gemini.Model,
                Error = "Proveedor Gemini registrado. Implementar generateContent en una siguiente iteración."
            });
    }

    public sealed class AiProviderFactory : IAiProviderFactory
    {
        private readonly IReadOnlyDictionary<string, IAiProvider> _providers;
        private readonly AlahiaAiOptions _options;

        public AiProviderFactory(IEnumerable<IAiProvider> providers, IOptions<AlahiaAiOptions> options)
        {
            _options = options.Value;
            _providers = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> AvailableProviders => _providers.Keys.OrderBy(x => x).ToList();

        public IAiProvider GetCurrent() => Get(_options.Provider);

        public IAiProvider Get(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                providerId = _options.Provider;

            if (_providers.TryGetValue(providerId, out var provider))
                return provider;

            throw new InvalidOperationException($"Proveedor de IA no registrado: {providerId}");
        }
    }
}
