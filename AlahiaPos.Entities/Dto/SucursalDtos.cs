using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class SucursalSesionDto
    {
        public int IdSucursal { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public bool EsPrincipal { get; set; }
        public bool EsDefault { get; set; }
        public bool Activa { get; set; } = true;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Municipio { get; set; }
        public string? Provincia { get; set; }
        public string? ApiPrint { get; set; }
        public int? IdAlmacenPrincipal { get; set; }
    }

    public class CambiarSucursalRequest
    {
        public int IdSucursal { get; set; }
    }

    public class CambiarSucursalResultado
    {
        public int IdSucursal { get; set; }
        public string Nombre { get; set; } = "";
        public string? ApiPrint { get; set; }
        public int? IdAlmacenPrincipal { get; set; }
        public IReadOnlyList<SucursalSesionDto> Sucursales { get; set; }
            = new List<SucursalSesionDto>();
    }

        /// <summary>
        /// Alcance de consulta. El cajero solo ve su sucursal operativa;
        /// el administrador puede consolidar o filtrar.
        /// IdPrincipal es siempre la sucursal principal de la empresa
        /// (documentos sin IdSucursal se atribuyen a esa, no a la del cajero).
        /// </summary>
        public sealed class SucursalConsultaScope
        {
            public static SucursalConsultaScope Vacio { get; } = new()
            {
                IdsPermitidos = Array.Empty<int>(),
                Sucursales = Array.Empty<SucursalSesionDto>(),
                IdPrincipal = 0
            };

            public IReadOnlyList<int> IdsPermitidos { get; init; } = Array.Empty<int>();
            public IReadOnlyList<SucursalSesionDto> Sucursales { get; init; } = Array.Empty<SucursalSesionDto>();
            public int IdPrincipal { get; init; }
            public bool EsConsolidado => IdsPermitidos.Count != 1;

            public bool Incluye(int? idSucursalDocumento)
            {
                if (IdsPermitidos.Count == 0)
                    return false;
                var id = idSucursalDocumento is > 0 ? idSucursalDocumento.Value : IdPrincipal;
                return id > 0 && IdsPermitidos.Contains(id);
            }
        }
}
