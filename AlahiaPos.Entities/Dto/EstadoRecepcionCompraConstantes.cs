namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Estado de recepción física (independiente del estado de pago CxP).
    /// </summary>
    public static class EstadoRecepcionCompraConstantes
    {
        /// <summary>Documento sin líneas que requieren recepción física (solo Gasto/Servicio).</summary>
        public const string NoAplica = "NO_APLICA";

        /// <summary>Confirmada; almacén aún no ha recibido.</summary>
        public const string PendienteRecepcion = "PENDIENTE_RECEPCION";

        /// <summary>Al menos una línea con recepción parcial.</summary>
        public const string ParcialmenteRecibida = "PARCIALMENTE_RECIBIDA";

        /// <summary>Todas las líneas recibibles están completas.</summary>
        public const string Recibida = "RECIBIDA";

        public static bool EstaAbierta(string? estado)
        {
            return string.Equals(estado, PendienteRecepcion, StringComparison.OrdinalIgnoreCase)
                || string.Equals(estado, ParcialmenteRecibida, StringComparison.OrdinalIgnoreCase);
        }
    }
}
