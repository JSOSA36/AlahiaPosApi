using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public static class PoliticasEstados
    {
        public const string Borrador = "Borrador";
        public const string Publicada = "Publicada";
        public const string Archivada = "Archivada";
    }

    public class PoliticasVersionDto
    {
        public int IdVersion { get; set; }
        public string NumeroVersion { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Contenido { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public int? IdUsuarioCreacion { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public int? IdUsuarioPublicacion { get; set; }
        public string? Notas { get; set; }
    }

    public class PoliticasEstadoDto
    {
        public bool RequiereAceptacion { get; set; }
        public bool EsAdministrador { get; set; }
        public PoliticasVersionDto? VersionActiva { get; set; }
    }

    public class AceptarPoliticasRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdVersion { get; set; }
        public string? DireccionIp { get; set; }
        public string? Navegador { get; set; }
        public string? SistemaOperativo { get; set; }
    }

    public class AceptarPoliticasResultado
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public int? IdAceptacion { get; set; }
        public bool CorreoEnviado { get; set; }
    }

    public class CrearPoliticasVersionRequest
    {
        public string NumeroVersion { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Contenido { get; set; } = "";
        public string? Notas { get; set; }
        public int IdUsuario { get; set; }
    }

    public class PublicarPoliticasRequest
    {
        public int IdVersion { get; set; }
        public int IdUsuario { get; set; }
    }

    public class PoliticasAceptacionDto
    {
        public int IdAceptacion { get; set; }
        public int IdVersion { get; set; }
        public string NumeroVersion { get; set; } = "";
        public string TituloVersion { get; set; } = "";
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? NombreUsuario { get; set; }
        public DateTime FechaAceptacion { get; set; }
        public string? DireccionIp { get; set; }
        public string? Navegador { get; set; }
        public string? SistemaOperativo { get; set; }
        public bool CorreoEnviado { get; set; }
        public DateTime? FechaCorreoEnviado { get; set; }
    }
}
