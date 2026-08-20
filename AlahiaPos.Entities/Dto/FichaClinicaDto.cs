using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class FichaClinicaAnamnesisDto
    {
        public bool Diabetes { get; set; }
        public bool Hipertension { get; set; }
        public bool Anemia { get; set; }
        public bool Falcemia { get; set; }
        public bool Asma { get; set; }
        public bool Hemorragia { get; set; }
        public bool Cardiacos { get; set; }
        public bool Renales { get; set; }
        public bool Gastricas { get; set; }
        public bool Dolor { get; set; }
        public bool Hepatitis { get; set; }
        public bool Vih { get; set; }
        public bool Tuberculosis { get; set; }
        public string? OtraContagio { get; set; }
    }

    public class FichaClinicaDienteDto
    {
        public string Numero { get; set; } = string.Empty;
        public bool Marcado { get; set; }
        public string? Nota { get; set; }
    }

    public class FichaClinicaClienteDto
    {
        public int IdCliente { get; set; }
        public string? NombreComercial { get; set; }
        public string? CedulaRnc { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public int? Edad { get; set; }
    }

    public class FichaClinicaCuentaLineaDto
    {
        public DateTime Fecha { get; set; }
        public string? NumeroFactura { get; set; }
        public string? Diente { get; set; }
        public string Trabajo { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public decimal Pagos { get; set; }
        public decimal Balance { get; set; }
        public int IdFacturaHeader { get; set; }
    }

    public class FichaClinicaDto
    {
        public int IdFichaClinica { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCliente { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Sexo { get; set; }
        public string? EstadoCivil { get; set; }
        public string? Nacionalidad { get; set; }
        public string? ContactoEmergenciaNombre { get; set; }
        public string? ContactoEmergenciaTelefono { get; set; }
        public FichaClinicaAnamnesisDto Anamnesis { get; set; } = new();
        public List<FichaClinicaDienteDto> Dientes { get; set; } = new();
        public string? Medicamentos { get; set; }
        public string? Observaciones { get; set; }
        public string? Color { get; set; }
        public string? TipoProtesis { get; set; }
        public string? Laboratorio { get; set; }
        public int IdUsuarioCreacion { get; set; }
        public int? IdUsuarioModificacion { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public string? CedulaRnc { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    public class FichaClinicaVistaDto
    {
        public FichaClinicaDto Ficha { get; set; } = new();
        public FichaClinicaClienteDto Cliente { get; set; } = new();
        public List<FichaClinicaCuentaLineaDto> Cuenta { get; set; } = new();
        public decimal TotalCosto { get; set; }
        public decimal TotalPagos { get; set; }
        public decimal TotalBalance { get; set; }
        public string? NombreEmpresa { get; set; }
        public bool Existe { get; set; }
    }
}
