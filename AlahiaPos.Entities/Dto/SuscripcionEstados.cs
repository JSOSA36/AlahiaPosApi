namespace AlahiaPos.Entities.Dto
{
    public static class SuscripcionEstados
    {
        public const string Activa = "ACTIVA";
        public const string PendientePago = "PENDIENTE_PAGO";
        public const string PagoReportado = "PAGO_REPORTADO";
        public const string Suspendida = "SUSPENDIDA";
        public const string Cancelada = "CANCELADA";

        public const string CicloAbierto = "ABIERTO";
        public const string CicloPagado = "PAGADO";
        public const string CicloVencido = "VENCIDO";

        public const string AvisoDia30 = "DIA_30";
        public const string AvisoDia2 = "DIA_2";
        public const string AvisoDia3 = "DIA_3";
        public const string AvisoSuspension = "SUSPENSION";

        public const string CanalEmail = "EMAIL";
        public const string CanalInApp = "IN_APP";
        public const string CanalWhatsApp = "WHATSAPP";
    }
}
