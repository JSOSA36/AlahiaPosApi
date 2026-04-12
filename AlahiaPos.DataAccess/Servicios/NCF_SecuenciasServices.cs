using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class NCF_SecuenciasServices : INCF_Secuencias
    {
        private readonly IRepository<SecuenciaECF> _repository;

        public NCF_SecuenciasServices(IRepository<SecuenciaECF> repository)
        {
            _repository = repository;
        }

        // 🔹 Obtener todas las secuencias por empresa
        public async Task<IEnumerable<SecuenciaECF>> GetAll(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa
            );
        }

        // 🔹 Obtener por Id
        public async Task<SecuenciaECF> GetById(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // 🔹 Insertar nueva secuencia
        public async Task Insert(SecuenciaECF secuencia)
        {
            // 🚨 Validar que no exista otra activa del mismo tipo
            var existe = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == secuencia.IdEmpresa &&
                     x.TipoNCF == secuencia.TipoNCF &&
                     x.Activo
            );

            if (existe.Any())
                throw new Exception("Ya existe una secuencia activa para este tipo de NCF");

            await _repository.Save(secuencia);
        }

        // 🔹 Actualizar secuencia
        public void Update(SecuenciaECF secuencia)
        {
            _repository.Update(secuencia.IdSecuencia, secuencia);
        }

        // 🔹 Eliminar (mejor desactivar)
        public async Task Delete(int id)
        {
            var secuencia = await _repository.GetByIdAsync(id);

            if (secuencia == null)
                throw new Exception("Secuencia no encontrada");

            secuencia.Activo = false;

            _repository.Update(secuencia.IdSecuencia, secuencia);
        }

        // 🔥 Generar siguiente NCF
        public async Task<string> GenerarNCF(int idEmpresa, string tipoNCF)
        {
            var secuencia = (await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa &&
                     x.TipoNCF == tipoNCF &&
                     x.Activo
            )).FirstOrDefault();

            if (secuencia == null)
                throw new Exception("No hay secuencia activa para este tipo de NCF");

            // 🚨 Validación de límite
            if (secuencia.SecuenciaActual >= secuencia.SecuenciaFinal)
                throw new Exception("La secuencia de comprobantes ha llegado a su límite");

            // 🔥 Generar número
            var nuevoNumero = secuencia.SecuenciaActual + 1;

            var ncf = $"{secuencia.Serie}{nuevoNumero.ToString().PadLeft(8, '0')}";

            // 💾 Actualizar secuencia
            secuencia.SecuenciaActual = nuevoNumero;

            _repository.Update(secuencia.IdSecuencia, secuencia);

            return ncf;
        }
    }
}