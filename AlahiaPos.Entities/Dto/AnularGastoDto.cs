namespace AlahiaPos.Entities.Dto
{
    public class AnularGastoDto
    {
        public int IdGasto { get; set; }
        public int IdEmpresa { get; set; }
        public string MotivoAnulacion { get; set; } = "";
        public string UsuarioAnulo { get; set; } = "";
    }
}
