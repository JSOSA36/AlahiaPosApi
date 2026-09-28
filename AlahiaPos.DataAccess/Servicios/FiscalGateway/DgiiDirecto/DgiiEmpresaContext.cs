namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Empresa del request actual (AsyncLocal). Header X-Dgii-IdEmpresa.
    /// Consulta TrackId debe firmar la semilla con el .p12 de esa empresa, no con un P12 de appsettings.
    /// </summary>
    public static class DgiiEmpresaContext
    {
        private static readonly AsyncLocal<int> CurrentEmpresa = new();

        public static int Current => CurrentEmpresa.Value;

        public static IDisposable Push(int idEmpresa)
        {
            var prev = CurrentEmpresa.Value;
            if (idEmpresa > 0)
                CurrentEmpresa.Value = idEmpresa;
            return new PopScope(prev);
        }

        private sealed class PopScope : IDisposable
        {
            private readonly int _prev;
            private bool _disposed;

            public PopScope(int prev) => _prev = prev;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                CurrentEmpresa.Value = _prev;
            }
        }
    }
}
