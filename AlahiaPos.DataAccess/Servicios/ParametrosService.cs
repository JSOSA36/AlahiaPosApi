using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ParametrosService : IParametrosService
    {
        private readonly IRepository<Parametros> _repository;

        public ParametrosService(IRepository<Parametros> repository)
        {
            _repository = repository;
        }

        // 🔹 Obtener todos los parámetros de la empresa
        public async Task<List<Parametros>> GetParametrosEmpresa(int idEmpresa)
        {
            var result = await _repository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa &&
                (p.CodigoPOS == null || p.CodigoPOS == ""));

            return result.ToList();
        }

        // 🔹 Obtener parámetros del POS
        public async Task<List<Parametros>> GetParametrosPOS(int idEmpresa, string codigoPOS)
        {
            var result = await _repository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa &&
                p.CodigoPOS == codigoPOS);

            return result.ToList();
        }

        // 🔹 Obtener un parámetro específico de empresa
        public async Task<Parametros?> GetParametro(int idEmpresa, string clave)
        {
            return await _repository.GetByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa &&
                p.Clave == clave &&
                (p.CodigoPOS == null || p.CodigoPOS == ""));
        }

        // 🔹 Obtener parámetro específico del POS
        public async Task<Parametros?> GetParametroPOS(int idEmpresa, string codigoPOS, string clave)
        {
            return await _repository.GetByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa &&
                p.CodigoPOS == codigoPOS &&
                p.Clave == clave);
        }

        // 🔹 Insertar parámetro
        public async Task InsertParametro(Parametros parametro)
        {
            await _repository.Save(parametro);
        }

        // 🔹 Actualizar parámetro
        public async Task UpdateParametro(Parametros parametro)
        {
            _repository.Update(parametro.IdParametro, parametro);
        }

        // 🔹 Eliminar parámetro
        public async Task DeleteParametro(int idParametro)
        {
            _repository.Delete(idParametro);
        }
    }
}