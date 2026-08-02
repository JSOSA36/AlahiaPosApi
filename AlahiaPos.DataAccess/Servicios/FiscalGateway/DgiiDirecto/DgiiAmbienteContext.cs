namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Ambiente DGII efectivo del request actual (AsyncLocal).
    /// Usado por Auth/Recepción cuando el ERP o el header X-Dgii-Ambiente lo fijan.
    /// </summary>
    public static class DgiiAmbienteContext
    {
        private static readonly AsyncLocal<string?> CurrentAmbiente = new();

        public static string? Current => CurrentAmbiente.Value;

        public static IDisposable Push(string? ambiente)
        {
            var prev = CurrentAmbiente.Value;
            if (!string.IsNullOrWhiteSpace(ambiente))
                CurrentAmbiente.Value = DgiiAmbienteHelper.Normalize(ambiente);
            return new PopScope(prev);
        }

        private sealed class PopScope : IDisposable
        {
            private readonly string? _prev;
            private bool _disposed;

            public PopScope(string? prev) => _prev = prev;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                CurrentAmbiente.Value = _prev;
            }
        }
    }
}
