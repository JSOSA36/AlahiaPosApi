using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class NotificacionEvento
    {
        public string Tipo { get; set; } = "";
        public int IdEmpresa { get; set; }

        /// <summary>USUARIO | EMPRESA | ROL. Default EMPRESA si no se indica usuario/rol.</summary>
        public string? DestinoTipo { get; set; }

        public int? IdUsuarioDestino { get; set; }
        public int? IdRolDestino { get; set; }
        public string? RolCodigo { get; set; }

        /// <summary>INFO | ADVERTENCIA | ERROR | EXITO</summary>
        public string? Prioridad { get; set; }

        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? Ruta { get; set; }
        public string? ReferenciaTipo { get; set; }
        public int? ReferenciaId { get; set; }
        public string? MetadataJson { get; set; }
        public string? CorreoDestino { get; set; }
        public string? NombreEmpresa { get; set; }
    }

    public class NotificacionDto
    {
        public int IdNotificacion { get; set; }
        public int IdEmpresa { get; set; }
        public string DestinoTipo { get; set; } = "";
        public int? IdUsuarioDestino { get; set; }
        public int? IdRolDestino { get; set; }
        public string? RolCodigo { get; set; }
        public string Tipo { get; set; } = "";
        public string Prioridad { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? Ruta { get; set; }
        public string? ReferenciaTipo { get; set; }
        public int? ReferenciaId { get; set; }
        public bool Leida { get; set; }
        public bool Archivada { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class NotificacionPolitica
    {
        public bool Email { get; set; }
        public bool Push { get; set; }
        public bool WhatsApp { get; set; }
    }
}
