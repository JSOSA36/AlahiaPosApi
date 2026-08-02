namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Adapter capaz de interpretar un formato/origen concreto de extracto bancario.
    /// Nuevos bancos/formatos se agregan como nuevos adapters sin tocar el orquestador.
    /// </summary>
    public interface IBankStatementAdapter
    {
        /// <summary>Identificador estable del adapter (se persiste como trazabilidad).</summary>
        string Nombre { get; }

        bool CanParse(BankStatementParseRequest request);

        BankStatementParseResult Parse(BankStatementParseRequest request);
    }

    /// <summary>
    /// Selecciona el adapter adecuado y ejecuta el parseo.
    /// </summary>
    public interface IBankStatementParserOrchestrator
    {
        BankStatementParseResult Parse(BankStatementParseRequest request);
    }
}
