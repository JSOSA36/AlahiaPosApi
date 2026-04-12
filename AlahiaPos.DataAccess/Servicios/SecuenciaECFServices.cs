using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ENCFSequenceServices : IENCFSequence
    {
        private readonly IRepository<SecuenciaECF> _repository;

        public ENCFSequenceServices(IRepository<SecuenciaECF> repository)
        {
            _repository = repository;
        }

        public async Task<string> GetNextENCFAsync(int idEmpresa, string tipoECF)
        {
            tipoECF = tipoECF.Trim();

            var secuencia = await _repository
                .GetByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa &&
                    x.TipoNCF == tipoECF);

            if (secuencia == null)
            {
                secuencia = new SecuenciaECF
                {
                    IdEmpresa = idEmpresa,
                    TipoNCF = tipoECF,
                    Serie = $"E{tipoECF}",
                    
                    SecuenciaActual = 1,
                    SecuenciaFinal = 0,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _repository.Save(secuencia);
            }

            if (!secuencia.Activo)
                throw new Exception("Secuencia ECF inactiva.");

            if (secuencia.SecuenciaActual > secuencia.SecuenciaFinal)
                throw new Exception("Secuencia ECF agotada.");

            string encf = BuildENCF(secuencia.Serie, secuencia.SecuenciaActual);

            secuencia.SecuenciaActual += 1;

            _repository.Update(secuencia.IdSecuencia, secuencia);

            return encf;
        }

        public async Task<string> PeekNextENCFAsync(int idEmpresa, string tipoECF)
        {
            var secuencia = await _repository
                .GetByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa &&
                    x.TipoNCF == tipoECF);

            if (secuencia == null)
                return $"E{tipoECF}0000000000001";

            return BuildENCF(secuencia.Serie, secuencia.SecuenciaActual);
        }

        public async Task ResetAsync(int idEmpresa, string tipoECF, int nuevoConsecutivo)
        {
            var secuencia = await _repository
                .GetByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa &&
                    x.TipoNCF == tipoECF);

            if (secuencia == null)
                throw new Exception("Secuencia no encontrada.");

            secuencia.SecuenciaActual = nuevoConsecutivo;

            _repository.Update(secuencia.IdSecuencia, secuencia);
        }

        private string BuildENCF(string prefijo, long consecutivo)
        {
            string numero = consecutivo.ToString().PadLeft(13, '0');
            return $"{prefijo}{numero}";
        }
    }
}