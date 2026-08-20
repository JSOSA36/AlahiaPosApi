namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AlahiaAiOptions
    {
        public const string SectionName = "AlahiaAi";

        public bool Enabled { get; set; } = true;

        /// <summary>OpenAI | AzureOpenAI | Anthropic | Gemini | Ollama</summary>
        public string Provider { get; set; } = "OpenAI";

        public OpenAiProviderOptions OpenAI { get; set; } = new();
        public AzureOpenAiProviderOptions AzureOpenAI { get; set; } = new();
        public AnthropicProviderOptions Anthropic { get; set; } = new();
        public GeminiProviderOptions Gemini { get; set; } = new();
        public OllamaProviderOptions Ollama { get; set; } = new();

        /// <summary>Consultas SQL de solo lectura con aislamiento por SESSION_CONTEXT.</summary>
        public AiSqlOptions Sql { get; set; } = new();
    }

    public class OpenAiProviderOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    }

    public class AzureOpenAiProviderOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Deployment { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "2024-08-01-preview";
    }

    public class AnthropicProviderOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-3-5-haiku-latest";
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
    }

    public class GeminiProviderOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-1.5-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    }

    public class OllamaProviderOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama3.2";
    }
}
