using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Http;

public class CitaDto : BaseEntity
{
    public int IdEmpresa { get; set; }

    public int IdCita { get; set; }
    public int IdEmpleado { get; set; }
    public string? NombreEstilista { get; set; }
    public string? RutaReciboPago { get; set; }
    public int IdProducto { get; set; }
    public string? NombreServicio { get; set; }
    public IFormFile? ReciboPago { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan Hora { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int? IdCliente { get; set; }
    public int DuracionMinutos { get; set; }
    public bool? esSeguimiento { get; set; }
    public string? Estado { get; set; }
    public string? Nota { get; set; }

    public decimal Costo { get; set; }
    public string? NombreCliente { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public decimal? Abono { get; set; }
    public string? Banco { get; set; }
    public int? IdFacturaHeader { get; set; }
}
