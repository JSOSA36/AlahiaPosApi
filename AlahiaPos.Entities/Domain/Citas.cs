using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Cita : BaseEntity
{
    [Key]
    public int IdCita { get; set; }

    public string? NombreCliente { get; set; }
    public string? RutaReciboPago { get; set; }

    // ============================
    // ESTILISTA
    // ============================
    [Required]
    public int IdEmpleado { get; set; }

    [ForeignKey(nameof(IdEmpleado))]
    public Empleados? Estilista { get; set; }

    // ============================
    // SERVICIO
    // ============================
    [Required]
    public int IdProducto { get; set; }

    [ForeignKey(nameof(IdProducto))]
    public Productos? Producto { get; set; }

    // ============================
    // FECHA Y HORAS
    // ============================
    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public TimeSpan Hora { get; set; }

    [Required]
    public TimeSpan HoraFin { get; set; }

    public int DuracionMinutos { get; set; }

    // ============================
    // ESTADO / INFO
    // ============================
    public string? Estado { get; set; } = "Programada";

    [NotMapped]
    public IFormFile? ReciboPago { get; set; }
    public bool? esSeguimiento { get; set; }
    public string? Nota { get; set; }
    public decimal Costo { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    // ============================
    // CLIENTE (🔥 AQUÍ ESTABA EL PROBLEMA)
    // ============================
    public int? IdCliente { get; set; }

    [ForeignKey(nameof(IdCliente))]
    public Clientes? Cliente { get; set; }

    // ============================
    // FACTURA (🔥 AQUÍ TAMBIÉN)
    // ============================
    public int? IdFacturaHeader { get; set; }

    [ForeignKey(nameof(IdFacturaHeader))]
    public FacturaHeaders? FacturaHeader { get; set; }

    // ============================
    // OTROS
    // ============================
    public decimal? Abono { get; set; }
    public string? Banco { get; set; }
    [Required]
    public int IdEmpresa { get; set; }

    [ForeignKey(nameof(IdEmpresa))]
    public Empresas Empresa { get; set; }
}
