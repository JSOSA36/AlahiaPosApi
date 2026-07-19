using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AlahiaPos.Entities.Dto
{
    public class TicketListItemDto
    {
        public int IdTicket { get; set; }
        public string Numero { get; set; } = "";
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = "";
        public string? NombreCliente { get; set; }
        public string Asunto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Prioridad { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public string? UsuarioCrea { get; set; }
        public int CantidadMensajes { get; set; }
        public bool SinResponder { get; set; }
        public DateTime? FechaUltimaRespuestaAdmin { get; set; }
        public int? HorasEstimadasMin { get; set; }
        public int? HorasEstimadasMax { get; set; }
        public DateTime? FechaEstimadaResolucion { get; set; }
    }

    public class TicketDetalleDto : TicketListItemDto
    {
        public string Descripcion { get; set; } = "";
        public int IdUsuarioCrea { get; set; }
        public string? VersionSistema { get; set; }
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
        public string? SistemaOperativo { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public List<TicketMensajeDto> Mensajes { get; set; } = new();
        public List<TicketAdjuntoDto> Adjuntos { get; set; } = new();
    }

    public class TicketMensajeDto
    {
        public int IdMensaje { get; set; }
        public int IdTicket { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = "";
        public bool EsRespuestaAdmin { get; set; }
        public string Mensaje { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public List<TicketAdjuntoDto> Adjuntos { get; set; } = new();
    }

    public class TicketAdjuntoDto
    {
        public int IdAdjunto { get; set; }
        public int IdTicket { get; set; }
        public int? IdMensaje { get; set; }
        public string NombreArchivo { get; set; } = "";
        public string Url { get; set; } = "";
        public string? ContentType { get; set; }
        public long? TamanoBytes { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class TicketNotificacionDto
    {
        public int IdNotificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdTicket { get; set; }
        public string? NumeroTicket { get; set; }
        public string Tipo { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class TicketMetricasDto
    {
        public int Abiertos { get; set; }
        public int EnProceso { get; set; }
        public int PendientesCliente { get; set; }
        public int Resueltos { get; set; }
        public int Cerrados { get; set; }
        public int Reabiertos { get; set; }
        public int Total { get; set; }
        public double? TiempoPromedioRespuestaHoras { get; set; }
        public double? TiempoPromedioResolucionHoras { get; set; }
    }

    public class CrearTicketDto
    {
        public int IdEmpresa { get; set; }
        public int IdUsuarioCrea { get; set; }
        public string? NombreCliente { get; set; }
        public string Asunto { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Prioridad { get; set; } = "MEDIA";
        public string? VersionSistema { get; set; }
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
        public string? SistemaOperativo { get; set; }
        public List<IFormFile>? Archivos { get; set; }
    }

    /// <summary>
    /// Crear ticket desde login / pantallas de bloqueo (sin sesión ERP).
    /// Valida UserName + Password y reutiliza CrearAsync.
    /// </summary>
    public class CrearTicketDesdeLoginDto
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Asunto { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "Acceso al Sistema";
        public string Prioridad { get; set; } = "ALTA";
        public string? VersionSistema { get; set; }
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
        public string? SistemaOperativo { get; set; }
        public List<IFormFile>? Archivos { get; set; }
    }

    public class AgregarTicketMensajeDto
    {
        public int IdTicket { get; set; }
        public int IdUsuario { get; set; }
        public int IdEmpresaUsuario { get; set; }
        public string Mensaje { get; set; } = "";
        public List<IFormFile>? Archivos { get; set; }
    }

    public class CambiarTicketEstadoDto
    {
        public int IdTicket { get; set; }
        public string Estado { get; set; } = "";
        public int IdUsuario { get; set; }
        public string? Nota { get; set; }
    }

    public class TicketFiltroAdminDto
    {
        public string? Q { get; set; }
        public string? Estado { get; set; }
        public string? Prioridad { get; set; }
        public string? Categoria { get; set; }
        public int? IdEmpresa { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string Orden { get; set; } = "recientes";
    }
}
