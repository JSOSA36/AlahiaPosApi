namespace AlahiaPos.Entities.Dto
{
    public class SecuenciaDocumentoEstadoDto
    {
        public int IdTipoDocumento { get; set; }

        public string Prefijo { get; set; } = "";

        public int SecuenciaActual { get; set; }
    }
}
