using AlahiaPos.Entities.Dto.AlahiaAi;

namespace AlahiaPos.Entities.Interfaces.AlahiaAi
{
    public interface IAiProvider
    {
        string ProviderId { get; }
        bool IsConfigured { get; }
        Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default);
    }

    public interface IAiProviderFactory
    {
        IAiProvider GetCurrent();
        IAiProvider Get(string providerId);
        IReadOnlyList<string> AvailableProviders { get; }
        /// <summary>Proveedor de la empresa si tiene config activa; si no, el global.</summary>
        Task<IAiProvider> ResolveForEmpresaAsync(int idEmpresa, CancellationToken ct = default);
    }

    public interface IAiPromptManager
    {
        string GetSystemPrompt();
        string BuildAnswerPrompt(string userMessage, string intent, string contextJson);
        string BuildResumenPrompt(string contextJson);
        string BuildTemplateAnswer(string intent, string contextJson);
        string BuildSqlGenerationPrompt(string userMessage, string catalogJson);
        string BuildSqlAnswerPrompt(string userMessage, string sql, string rowsJson);
    }

    public interface IAiContextBuilder
    {
        Task<(string Intent, object Context)> BuildAsync(
            string message,
            int idEmpresa,
            IReadOnlyCollection<string> modulosPermitidos,
            CancellationToken ct = default);
    }

    public interface IAiConversationHistory
    {
        string EnsureConversation(string? conversationId, int idEmpresa, int idUsuario);
        void AddMessage(string conversationId, AiConversationMessage message);
        IReadOnlyList<AiConversationMessage> GetMessages(string conversationId);
        string FormatForTicket(string conversationId);
    }

    public interface IAiPermissionService
    {
        bool HasAlahiaAi(IReadOnlyCollection<string> modulosPermitidos);
        bool CanUseIntent(string intent, IReadOnlyCollection<string> modulosPermitidos);
        IReadOnlyList<string> RequiredModulesForIntent(string intent);
    }

    public interface IAiUsageMonitor
    {
        Task TrackAsync(AiUsageLogEntry entry, CancellationToken ct = default);
        Task<IReadOnlyList<AiUsageLogEntry>> GetRecentAsync(int idEmpresa, int take = 50, CancellationToken ct = default);
    }

    /// <summary>
    /// Puerta de acceso a datos del ERP. La IA nunca toca DbContext directamente.
    /// </summary>
    public interface IAlahiaAiErpGateway
    {
        Task<object> GetVentasHoyAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetClientesDebenAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetStockBajoAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetUtilidadMesAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetGastosAltosAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetProductosSinRotarAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetCatalogoProductosAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetFacturasVencidasAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetFlujoCajaAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetTopProductosAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetConteoClientesAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetResumenOperativoAsync(int idEmpresa, CancellationToken ct = default);
        Task<object> GetAyudaDocumentalAsync(string pregunta, CancellationToken ct = default);
    }

    public interface IAiSqlExecutor
    {
        bool IsEnabled { get; }
        Task<string> GetCatalogAsync(CancellationToken ct = default);
        Task<AiSqlExecutionResult> ExecuteAsync(
            int idEmpresa,
            string sql,
            bool requireTenantContext = true,
            CancellationToken ct = default);
    }

    public interface IAlahiaAiService
    {
        Task<AlahiaAiChatResponse> ChatAsync(AlahiaAiChatRequest request, CancellationToken ct = default);
        Task<AlahiaAiResumenResponse> ResumenAsync(int idEmpresa, int idUsuario, IReadOnlyCollection<string> modulos, CancellationToken ct = default);
    }
}
