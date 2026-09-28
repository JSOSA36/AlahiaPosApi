using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ISecuenciaDocumentoService
    {
        Task<string> GenerarDocumentoAsync(
            int idEmpresa,
            int idTipoDocumento
        );

        /// <summary>
        /// Conserva el número ya impreso en el POS (modo local) y adelanta
        /// SecuenciaActual si hace falta. No incrementa si el número ya estaba cubierto.
        /// </summary>
        Task<string> ConfirmarReservadoAsync(
            int idEmpresa,
            int idTipoDocumento,
            string numeroReservado
        );

        Task<IReadOnlyList<SecuenciaDocumentoEstadoDto>> ListarPorEmpresaAsync(
            int idEmpresa
        );
    }
}

