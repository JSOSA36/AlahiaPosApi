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
        private const int DigitosSecuencia = 4;

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
                $"{NormalizarPrefijo(secuencia.Prefijo)}" +
                secuencia.SecuenciaActual
                    .ToString()
                    .PadLeft(DigitosSecuencia, '0');

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

        private static string NormalizarPrefijo(string? prefijo)
        {
            if (string.IsNullOrWhiteSpace(prefijo))
            {
                return string.Empty;
            }

            prefijo = prefijo.Trim();

            var lastDash = prefijo.LastIndexOf('-');
            if (lastDash < 0)
            {
                return prefijo;
            }

            var suffix = prefijo[(lastDash + 1)..];

            if (suffix.Length > 0 && suffix.All(c => c == '0'))
            {
                return prefijo[..(lastDash + 1)];
            }

            return prefijo;
        }
    }
}