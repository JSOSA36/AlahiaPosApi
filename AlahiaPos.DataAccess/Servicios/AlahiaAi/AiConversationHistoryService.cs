using System.Collections.Concurrent;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AiConversationHistoryService : IAiConversationHistory
    {
        private static readonly ConcurrentDictionary<string, ConversationState> Store = new();

        public string EnsureConversation(string? conversationId, int idEmpresa, int idUsuario)
        {
            if (!string.IsNullOrWhiteSpace(conversationId) && Store.ContainsKey(conversationId))
                return conversationId!;

            var id = $"ai-{idEmpresa}-{idUsuario}-{Guid.NewGuid():N}"[..40];
            Store[id] = new ConversationState
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Messages = new List<AiConversationMessage>()
            };
            return id;
        }

        public void AddMessage(string conversationId, AiConversationMessage message)
        {
            if (!Store.TryGetValue(conversationId, out var state)) return;
            lock (state.Sync)
            {
                state.Messages.Add(message);
                if (state.Messages.Count > 40)
                    state.Messages.RemoveRange(0, state.Messages.Count - 40);
            }
        }

        public IReadOnlyList<AiConversationMessage> GetMessages(string conversationId)
        {
            if (!Store.TryGetValue(conversationId, out var state))
                return Array.Empty<AiConversationMessage>();
            lock (state.Sync)
                return state.Messages.ToList();
        }

        public string FormatForTicket(string conversationId)
        {
            var msgs = GetMessages(conversationId);
            if (msgs.Count == 0) return "Sin historial de conversación.";
            return string.Join("\n\n", msgs.Select(m => $"[{m.At:yyyy-MM-dd HH:mm}] {m.Role.ToUpperInvariant()}: {m.Content}"));
        }

        private sealed class ConversationState
        {
            public int IdEmpresa { get; set; }
            public int IdUsuario { get; set; }
            public List<AiConversationMessage> Messages { get; set; } = new();
            public object Sync { get; } = new();
        }
    }
}
