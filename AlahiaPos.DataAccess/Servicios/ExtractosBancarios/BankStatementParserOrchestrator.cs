namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Selecciona el primer adapter capaz de interpretar el archivo (orden estable:
    /// Excel, Popular texto, CSV genérico como fallback) y sella el adapter usado.
    /// </summary>
    public sealed class BankStatementParserOrchestrator : IBankStatementParserOrchestrator
    {
        private readonly IReadOnlyList<IBankStatementAdapter> _adapters;

        public BankStatementParserOrchestrator(IEnumerable<IBankStatementAdapter> adapters)
        {
            _adapters = adapters.ToList();
            if (_adapters.Count == 0)
                throw new ArgumentException("Se requiere al menos un adapter.", nameof(adapters));
        }

        public static BankStatementParserOrchestrator CreateDefault() => new(new IBankStatementAdapter[]
        {
            new ExcelStatementAdapter(),
            new PopularTextoStatementAdapter(),
            new CsvStatementAdapter()
        });

        public BankStatementParseResult Parse(BankStatementParseRequest request)
        {
            var adapter = _adapters.FirstOrDefault(a => a.CanParse(request))
                ?? throw new InvalidOperationException(
                    $"Ningún adapter puede procesar el formato '{request.Formato}'.");

            var result = adapter.Parse(request);
            result.AdapterUsado = adapter.Nombre;
            return result;
        }
    }
}
