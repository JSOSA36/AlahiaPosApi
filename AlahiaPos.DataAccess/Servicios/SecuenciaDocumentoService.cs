using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class SecuenciaDocumentoService
        : ISecuenciaDocumentoService
    {
        private readonly IRepository<SecuenciaDocumentos>
            _repository;

        public SecuenciaDocumentoService(
            IRepository<SecuenciaDocumentos> repository
        )
        {
            _repository = repository;
        }

        // =====================================================
        // 🔥 GENERAR DOCUMENTO
        // =====================================================

        public async Task<string> GenerarDocumentoAsync(
            int idEmpresa,
            int idTipoDocumento
        )
        {
            var secuencia =

                (await _repository
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa &&
                    x.IdTipoDocumento == idTipoDocumento
                ))
                .FirstOrDefault();

            // 🔥 VALIDAR
            if (secuencia == null)
            {
                throw new Exception(
                    "No existe secuencia configurada para este documento"
                );
            }

            // =====================================================
            // 🔥 SUMAR SECUENCIA
            // =====================================================

            secuencia.SecuenciaActual++;

            // =====================================================
            // 🔥 GENERAR NUMERO
            // =====================================================

            string numeroDocumento =

                $"{secuencia.Prefijo}" +

                secuencia.SecuenciaActual
                .ToString()
                .PadLeft(4, '0');

            // =====================================================
            // 🔥 UPDATE
            // =====================================================

            _repository.Update(
                secuencia.Id,
                secuencia
            );

            // =====================================================
            // 🔥 RETORNO
            // =====================================================

            return numeroDocumento;
        }
    }
}