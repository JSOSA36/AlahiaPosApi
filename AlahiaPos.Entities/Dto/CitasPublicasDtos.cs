using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AlahiaPos.Entities.Dto
{
    public class CitaPublicaSalonDto
    {
        public Guid GuidPublico { get; set; }
        public string NombreComercial { get; set; } = "";
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? LogoUrl { get; set; }
        public string? InfoAgendar { get; set; }
        public bool PedirVoucherCitas { get; set; }
        public decimal MontoReservaCitas { get; set; }
        public string? PrimaryColor { get; set; }
        public string? TitleColor { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public List<CitaPublicaServicioDto> Servicios { get; set; } = new();
        public List<CitaPublicaEstilistaDto> Estilistas { get; set; } = new();
    }

    public class EmpresaCitasConfigDto
    {
        public bool PedirVoucherCitas { get; set; }
        public decimal MontoReservaCitas { get; set; }
        public string? InfoAgendar { get; set; }
        public bool NotificarCitasWhatsApp { get; set; } = true;
    }

    public class CitaPublicaServicioDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int DuracionMinutos { get; set; }
        public string? Imagen { get; set; }
    }

    public class CitaPublicaEstilistaDto
    {
        public int IdEmpleado { get; set; }
        public string Nombre { get; set; } = "";
        public int[] DiasDisponibles { get; set; } = Array.Empty<int>();
    }

    public class CitaPublicaDisponibilidadDto
    {
        public string Fecha { get; set; } = "";
        public List<string> HorasDisponibles { get; set; } = new();
    }

    public class CitaPublicaCrearRequest
    {
        public string NombreCliente { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string? Correo { get; set; }
        public int IdEmpleado { get; set; }
        public int IdProducto { get; set; }
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string? Nota { get; set; }
        public string? Banco { get; set; }
        public decimal? Abono { get; set; }
        public IFormFile? ReciboPago { get; set; }
    }

    public class CitaPublicaConfirmacionDto
    {
        public int IdCita { get; set; }
        public string NombreCliente { get; set; } = "";
        public string Servicio { get; set; } = "";
        public string Estilista { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string HoraFin { get; set; } = "";
        public string Estado { get; set; } = "Programada";
        public string Mensaje { get; set; } = "";
    }

    public class CitaPublicaItemDto
    {
        public int IdCita { get; set; }
        public string Servicio { get; set; } = "";
        public string Estilista { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string Estado { get; set; } = "";
        public string? Nota { get; set; }
    }
}
