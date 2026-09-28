using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
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
            var secuencia = await ObtenerSecuenciaAsync(idEmpresa, idTipoDocumento);

            secuencia.SecuenciaActual++;

            var numeroDocumento = Formatear(secuencia.Prefijo, secuencia.SecuenciaActual);

            _repository.Update(
                secuencia.Id,
                secuencia
            );

            return numeroDocumento;
        }

        public async Task<string> ConfirmarReservadoAsync(
            int idEmpresa,
            int idTipoDocumento,
            string numeroReservado
        )
        {
            var numero = (numeroReservado ?? "").Trim();
            if (numero.Length == 0)
            {
                return await GenerarDocumentoAsync(idEmpresa, idTipoDocumento);
            }

            var secuencia = await ObtenerSecuenciaAsync(idEmpresa, idTipoDocumento);
            var leido = IntentarLeerNumero(numero, secuencia.Prefijo);
            if (leido is > 0 && leido.Value > secuencia.SecuenciaActual)
            {
                secuencia.SecuenciaActual = leido.Value;
                _repository.Update(secuencia.Id, secuencia);
            }

            return numero;
        }

        public async Task<IReadOnlyList<SecuenciaDocumentoEstadoDto>> ListarPorEmpresaAsync(
            int idEmpresa
        )
        {
            var filas = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa);

            return filas
                .Select(x => new SecuenciaDocumentoEstadoDto
                {
                    IdTipoDocumento = x.IdTipoDocumento,
                    Prefijo = NormalizarPrefijo(x.Prefijo),
                    SecuenciaActual = x.SecuenciaActual
                })
                .ToList();
        }

        private async Task<SecuenciaDocumentos> ObtenerSecuenciaAsync(
            int idEmpresa,
            int idTipoDocumento)
        {
            var secuencia =
                (await _repository
                .GetAllByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa &&
                    x.IdTipoDocumento == idTipoDocumento
                ))
                .FirstOrDefault();

            if (secuencia == null)
            {
                throw new Exception(
                    "No existe secuencia configurada para este documento"
                );
            }

            return secuencia;
        }

        private static string Formatear(string? prefijo, int actual)
        {
            return $"{NormalizarPrefijo(prefijo)}" +
                actual.ToString().PadLeft(DigitosSecuencia, '0');
        }

        private static int? IntentarLeerNumero(string numero, string? prefijoRaw)
        {
            var n = (numero ?? "").Trim();
            if (n.Length == 0)
            {
                return null;
            }

            var prefijo = NormalizarPrefijo(prefijoRaw);
            if (prefijo.Length > 0 && n.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            {
                n = n[prefijo.Length..];
            }
            else
            {
                var i = n.Length;
                while (i > 0 && char.IsDigit(n[i - 1]))
                {
                    i--;
                }
                n = n[i..];
            }

            return int.TryParse(n, out var v) && v > 0 ? v : null;
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