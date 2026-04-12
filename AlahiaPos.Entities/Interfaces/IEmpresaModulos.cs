using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpresaModulos
    {
        // 🔹 LICENCIAMIENTO
        // Módulos que la empresa tiene contratados
        Task<IEnumerable<EmpresaModulo>> GetModulosByEmpresa(int empresaId);

        // 🔹 VALIDACIÓN DE PLAN
        Task<bool> EmpresaTieneModulo(int empresaId, int moduloId);

        // 🔹 GESTIÓN DE LICENCIA
        Task InsertEmpresaModulo(EmpresaModulo empresaModulo);
        void UpdateEmpresaModulo(int id, EmpresaModulo empresaModulo);

        // 🔥 AUTORIZACIÓN REAL (LOGIN / UI)
        // Módulos que ESTE EMPLEADO puede VER según su rol
        
    }
}
