namespace AlahiaPos.Entities.Dto.AlahiaAi
{
    public class EmpresaAiConfigDto
    {
        public int IdEmpresa { get; set; }
        public string Provider { get; set; } = "OpenAI";
        public string Model { get; set; } = "gpt-4o-mini";
        public string? BaseUrl { get; set; }
        public bool Activo { get; set; } = true;
        /// <summary>True si hay API key guardada (nunca se envía el valor).</summary>
        public bool HasApiKey { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class GuardarEmpresaAiConfigDto
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Provider { get; set; } = "OpenAI";
        public string Model { get; set; } = "gpt-4o-mini";
        public string? BaseUrl { get; set; }
        public bool Activo { get; set; } = true;
        /// <summary>Si viene vacío/null, se conserva la key existente.</summary>
        public string? ApiKey { get; set; }
        public bool ClearApiKey { get; set; }
    }
}
