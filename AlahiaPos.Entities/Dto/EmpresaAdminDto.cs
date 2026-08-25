using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class EmpresaAdminAltaRequest
    {
        public string NombreComercial { get; set; } = "";
        public string? RNC { get; set; }
        public string Direccion { get; set; } = "";
        public string? Telefono { get; set; }
        public string CorreElectronico { get; set; } = "";
        public string AdminPassword { get; set; } = "";
        public int LimiteUsuario { get; set; } = 5;

        /// <summary>STANDARD | GOLD | PREMIUM. Default STANDARD.</summary>
        public string NivelSoporte { get; set; } = "STANDARD";

        /// <summary>Si true: MontoServicio=0 y FechaTerminacion = hoy + DiasDemo.</summary>
        public bool EsDemo { get; set; } = true;
        public int DiasDemo { get; set; } = 15;

        /// <summary>Obligatorio si EsDemo=false.</summary>
        public decimal MontoServicio { get; set; }

        /// <summary>Códigos de módulo a licenciar. Null/vacío = plantilla bootstrap.</summary>
        public List<string>? CodigosModulo { get; set; }
    }

    public class EmpresaAdminDemoRequest
    {
        public bool EsDemo { get; set; }
        public int DiasDemo { get; set; } = 15;
        public decimal MontoServicio { get; set; }
    }

    public class EmpresaAdminModulosRequest
    {
        public List<string> CodigosModulo { get; set; } = new();
    }

    public class EmpresaAdminListItemDto
    {
        public int IdEmpresa { get; set; }
        public string NombreComercial { get; set; } = "";
        public string? RNC { get; set; }
        public string? CorreElectronico { get; set; }
        public string? Telefono { get; set; }
        public bool Estado { get; set; }
        public string EstadoServicio { get; set; } = "";
        public decimal MontoServicio { get; set; }
        public DateTime FechaTerminacion { get; set; }
        public bool EsDemoVigente { get; set; }
        public int CantidadModulos { get; set; }
        public int? LimiteUsuario { get; set; }
        public string NivelSoporte { get; set; } = "STANDARD";
    }

    public class EmpresaAdminNivelSoporteRequest
    {
        public string NivelSoporte { get; set; } = "STANDARD";
    }

    public class EmpresaAdminDetalleDto : EmpresaAdminListItemDto
    {
        public string? Direccion { get; set; }
        public List<string> CodigosModulo { get; set; } = new();
        public List<ModuloCatalogoItemDto> ModulosDisponibles { get; set; } = new();
        public List<EmpresaAdminPerfilDto> Perfiles { get; set; } = new();
    }

    public class EmpresaAdminPerfilDto
    {
        public int IdPerfil { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public List<int> IdsModulo { get; set; } = new();
    }

    public class EmpresaAdminPerfilRequest
    {
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public List<int> IdsModulo { get; set; } = new();
    }

    public class ModuloCatalogoItemDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public bool Asignable { get; set; }
        public bool Seleccionado { get; set; }
    }

    public class EmpresaAdminVerticalPresetDto
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public List<string> CodigosModulo { get; set; } = new();
    }

    public class EmpresaAdminAltaResultDto
    {
        public int IdEmpresa { get; set; }
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime FechaTerminacion { get; set; }
        public bool EsDemo { get; set; }
        public int IdPerfil { get; set; }
        public string Message { get; set; } = "";
    }
}
