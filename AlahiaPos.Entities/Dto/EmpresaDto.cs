using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class EmpresaDto
{
    public int IdEmpresa { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? RNC { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreElectronico { get; set; }
    public string? Nota { get; set; }
        public int? LimiteUsuario { get; set; }
        public string? CorreoSMTP { get; set; }
        public string? InfoAgendar { get; set; }

        public string? PasswordSMTP { get; set; }
        public string? ServidorSMTP { get; set; }
        public int? PuertoSMTP { get; set; }
        public bool? UsaSSL { get; set; }
        public string? NombreRemitente { get; set; }

        // URL original (FTP o nube)
        public string? Logo { get; set; }

    // URL calculada en el API (proxy con CORS)
    public string? LogoUrl { get; set; }
     public string? UrlCitas { get; set; }
     public string? UrlCatalogo { get; set; }
        // Para carga de imágenes al hacer PUT
        public IFormFile? Imagen { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? TertiaryColor { get; set; }

        // 📍 Precisión decimal 9,6 para lat/lng
        public Guid GuidPublico { get; set; } // 👈 único para cada empresa
        public string? Latitude { get; set; }
        public string? titleColor { get; set; }
        public string? TokenNotificacion { get; set; }
        public string? Longitude { get; set; }
        public string? NombrePlan { get; set; }
    }

}
