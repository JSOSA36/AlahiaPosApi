using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INCF_Secuencias
    {
        Task<IEnumerable<SecuenciaECF>> GetAll(int idEmpresa);

        // 🔍 Obtener por ID
        Task<SecuenciaECF> GetById(int id);

        // ➕ Registrar nueva secuencia
        Task Insert(SecuenciaECF secuencia);

        // ✏️ Editar secuencia
        void Update(SecuenciaECF secuencia);

        // ❌ Eliminar secuencia
        Task Delete(int id);

        // 🔥 Generar siguiente NCF
        Task<string> GenerarNCF(int idEmpresa, string tipoNCF);
    }
}
