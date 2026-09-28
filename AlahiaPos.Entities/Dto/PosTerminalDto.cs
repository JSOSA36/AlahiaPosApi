using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class PosTerminalClaimRequest
    {
        public string DeviceId { get; set; } = "";
        public string? Nombre { get; set; }
        public string? Plataforma { get; set; }
        public string? Modelo { get; set; }
        public string? Fabricante { get; set; }
    }

    public class PosTerminalDto
    {
        public int IdPosTerminal { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = "";
        public string? Plataforma { get; set; }
        public string? Modelo { get; set; }
        public string? Fabricante { get; set; }
        public string Estado { get; set; } = "";
        public DateTime FechaActivacion { get; set; }
        public DateTime FechaUltimoAcceso { get; set; }
        public DateTime? FechaRevocacion { get; set; }
        public int? IdUsuarioActivacion { get; set; }
        public int? IdUsuarioUltimoAcceso { get; set; }
    }

    public class PosTerminalClaimResult
    {
        public bool Permitido { get; set; }
        public string Codigo { get; set; } = "OK";
        public string Mensaje { get; set; } = "";
        public int Limite { get; set; }
        public int Usadas { get; set; }
        public PosTerminalDto? Terminal { get; set; }
    }

    public static class PosTerminalCodigos
    {
        public const string Ok = "OK";
        public const string SinHuella = "SIN_HUELLA";
        public const string SinModulo = "SIN_MODULO";
        public const string Limite = "LIMITE";
        public const string Revocado = "REVOCADO";
    }
}
