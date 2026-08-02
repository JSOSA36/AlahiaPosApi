namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Resultado de intentar alimentar Contabilidad desde un módulo operativo.
    /// Nunca implica fallo del negocio: Advertencia es informativa.
    /// </summary>
    public class ContabilidadPublishResult
    {
        public bool IntegracionActiva { get; set; }
        public bool AsientoGenerado { get; set; }
        public string? Advertencia { get; set; }

        public static ContabilidadPublishResult Inactiva() => new()
        {
            IntegracionActiva = false,
            AsientoGenerado = false
        };

        public static ContabilidadPublishResult Ok() => new()
        {
            IntegracionActiva = true,
            AsientoGenerado = true
        };

        public static ContabilidadPublishResult ConAdvertencia(string mensaje) => new()
        {
            IntegracionActiva = true,
            AsientoGenerado = false,
            Advertencia = mensaje
        };
    }
}
