namespace AlahiaPos.Entities.Dto
{
    public class AlmacenExistenciaDto
    {
        public int IdAlmacen { get; set; }

        public string NombreAlmacen { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public bool EsPrincipal { get; set; }
    }
}
