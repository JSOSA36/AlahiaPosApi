namespace AlahiaPos.Entities.Dto
{
    public static class ContabilidadModuloConstantes
    {
        public const string CodigoModuloContabilidad = "CONTABILIDAD";
    }

    public static class ContabilidadIntegracionEstados
    {
        public const string Ok = "OK";
        public const string Omitido = "OMITIDO";
        public const string Error = "ERROR";
        public const string Duplicado = "DUPLICADO";
    }

    public static class EventoOutboxEstados
    {
        public const string Pendiente = "Pendiente";
        public const string Procesando = "Procesando";
        public const string Procesado = "Procesado";
        public const string Error = "Error";
        public const string Omitido = "Omitido";
    }

    public static class ContabilidadTipoOperacion
    {
        public const string Alta = "ALTA";
        public const string Cogs = "COGS";
        public const string Reverso = "REVERSO";
        public const string Cobro = "COBRO";
        public const string Pago = "PAGO";
        public const string ReclasificarPago = "RECLASIFICAR_PAGO";
    }
}
