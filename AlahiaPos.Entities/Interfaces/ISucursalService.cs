using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ISucursalService
    {
        Task<Sucursal> AsegurarPrincipalAsync(int idEmpresa, CancellationToken ct = default);

        Task AsegurarAccesoUsuarioAsync(int idUsuario, int idEmpresa, CancellationToken ct = default);

        /// <summary>
        /// Asigna la sucursal operativa del usuario. El administrador no queda
        /// atado a ninguna; el resto debe tener exactamente una sucursal activa.
        /// </summary>
        Task AsignarOperativaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursal,
            bool esAdministrador,
            CancellationToken ct = default);

        Task<IReadOnlyList<SucursalSesionDto>> ListarPorUsuarioAsync(
            int idUsuario,
            int idEmpresa,
            CancellationToken ct = default);

        Task<bool> TieneAccesoAsync(
            int idUsuario,
            int idEmpresa,
            int idSucursal,
            CancellationToken ct = default);

        Task<CambiarSucursalResultado> CambiarActivaAsync(
            int idUsuario,
            int idEmpresa,
            int idSucursal,
            string? dispositivo,
            string? ip,
            CancellationToken ct = default);

        Task<int?> ResolverSucursalActivaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursalActiva,
            CancellationToken ct = default);

        /// <summary>
        /// Sucursales que el usuario puede consultar.
        /// Cajero: siempre su sucursal operativa (el filtro a otra sucursal se rechaza).
        /// Administrador: idSucursalFiltro nulo o 0 = todas; un id ajeno lanza UnauthorizedAccessException.
        /// </summary>
        Task<SucursalConsultaScope> ResolverConsultaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursalFiltro,
            CancellationToken ct = default);
    }
}
