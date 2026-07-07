using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Alahia_Pos.Services
{
    public class RNCService : IRNCService
    {
        private readonly IRepository<ClientesDGII> _clientesRepo;

        public RNCService(
            IRepository<ClientesDGII> clientesRepo
        )
        {
            _clientesRepo = clientesRepo;
        }

        // =====================================================
        // 🔥 CONSULTAR RNC / CÉDULA
        // =====================================================

        public async Task<ClienteDgiiDto?> ConsultarAsync(
            string rncOrCedula)
        {
            if (string.IsNullOrWhiteSpace(
                rncOrCedula))
            {
                return null;
            }

            // 🔥 LIMPIAR
            rncOrCedula =

                rncOrCedula
                .Replace("-", "")
                .Trim();

            // =====================================================
            // 🔥 BUSCAR CLIENTE
            // =====================================================

            var cliente = await _clientesRepo
                .GetByExpresionAsync(c =>

                    c.RNC != null &&

                    c.RNC.Replace("-", "")
                    == rncOrCedula
                );

            if (cliente == null)
                return null;

            // =====================================================
            // 🔥 DEVOLVER DTO
            // =====================================================

            return new ClienteDgiiDto
            {
                RNC = cliente.RNC,

                Nombre =
                    cliente.NombreComercial
                    ?? cliente.NombreComercial
                    ?? "",

                Estado = "ACTIVO"
            };
        }
    }
}