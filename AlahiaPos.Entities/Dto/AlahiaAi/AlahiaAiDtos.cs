namespace AlahiaPos.Entities.Dto.AlahiaAi
{
    public class AlahiaAiChatRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
        /// <summary>Códigos de módulo activos del usuario (permisos).</summary>
        public List<string> ModulosPermitidos { get; set; } = new();
    }

    public class AlahiaAiChatResponse
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public bool UsedLlm { get; set; }
        public string Provider { get; set; } = string.Empty;
        public bool SuggestTicket { get; set; }
        public List<string> Insights { get; set; } = new();
        public object? ContextPreview { get; set; }
    }

    public class AlahiaAiResumenResponse
    {
        public string Greeting { get; set; } = string.Empty;
        public List<string> Bullets { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public bool UsedLlm { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    public class AiCompletionRequest
    {
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 800;
    }

    public class AiCompletionResult
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
    }

    public class AiConversationMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public DateTime At { get; set; } = DateTime.UtcNow;
        public string? Intent { get; set; }
    }

    public class AiUsageLogEntry
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Feature { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
