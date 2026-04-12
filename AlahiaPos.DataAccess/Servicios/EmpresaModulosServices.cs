using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpresaModulosServices : IEmpresaModulos
    {
        private readonly IRepository<EmpresaModulo> _empresaModuloRepository;
        private readonly IRepository<Empleados> _empleadoRepository;
        

        public EmpresaModulosServices(
            IRepository<EmpresaModulo> empresaModuloRepository,
            IRepository<Empleados> empleadoRepository
           
        )
        {
            _empresaModuloRepository = empresaModuloRepository;
            _empleadoRepository = empleadoRepository;
            
        }

        // ======================================================
        // 🔹 LICENCIAMIENTO / CONFIGURACIÓN
        // ======================================================

        public async Task<IEnumerable<EmpresaModulo>> GetModulosByEmpresa(int empresaId)
        {
            return await _empresaModuloRepository.GetAllByExpresionAsync(
                em => em.EmpresaId == empresaId && em.Activo,
                "Modulo"
            );
        }

        public async Task<bool> EmpresaTieneModulo(int empresaId, int moduloId)
        {
            var modulo = await _empresaModuloRepository.GetByExpresionAsync(
                em => em.EmpresaId == empresaId
                   && em.ModuloId == moduloId
                   && em.Activo
            );

            return modulo != null;
        }

        public async Task InsertEmpresaModulo(EmpresaModulo empresaModulo)
        {
            await _empresaModuloRepository.Save(empresaModulo);
        }

        public void UpdateEmpresaModulo(int id, EmpresaModulo empresaModulo)
        {
            _empresaModuloRepository.Update(id, empresaModulo);
        }

        // ======================================================
        // 🔥 AUTORIZACIÓN REAL (LOGIN / MENÚ / UI)
        // ======================================================

       
    }
}
