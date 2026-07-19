namespace AlahiaPos.Entities.Domain
{
    /// <summary>Estados de fotografía / proceso fiscal documental (Sprint B).</summary>
    public static class EstadoFiscalDocumentoConstantes
    {
        public const string NoAplica = "NO_APLICA";
        public const string PendienteGenerar = "PENDIENTE_GENERAR";
        public const string Generada = "GENERADA";
        public const string PendienteValidar = "PENDIENTE_VALIDAR";
        public const string ErrorFiscal = "ERROR_FISCAL";
    }

    public static class EstadoClasificacionItbisConstantes
    {
        public const string NoAplica = "NO_APLICA";
        public const string PendienteValidar = "PENDIENTE_VALIDAR";
        public const string Confirmada = "CONFIRMADA";
    }

    public enum FiscalDocumentoTipo
    {
        Venta = 1,
        NotaCredito = 2,
        Compra = 3
    }
}
