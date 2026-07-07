using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class MetodoPagoCuentaService
        : IMetodoPagoCuentaService
    {
        private readonly IRepository<MetodoPagoCuenta>
            _repository;

        public MetodoPagoCuentaService(

            IRepository<MetodoPagoCuenta>
                repository
        )
        {
            _repository =
                repository;
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        public async Task<int>
            CreateAsync(
                MetodoPagoCuenta entity
            )
        {
            await _repository.Save(
                entity
            );

            return entity
                .IdMetodoPagoCuenta;
        }

        /* =====================================
        🔥 UPDATE
        ===================================== */

        public async Task
            UpdateAsync(
                MetodoPagoCuenta entity
            )
        {
            _repository.Update(

                entity.IdMetodoPagoCuenta,

                entity
            );

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 DELETE
        ===================================== */

        public async Task
            DeleteAsync(
                int id
            )
        {
            _repository.Delete(id);

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        public async Task<MetodoPagoCuenta?>
            GetByIdAsync(
                int id
            )
        {
            return await _repository
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        public async Task<IEnumerable<MetodoPagoCuenta>>
            GetAllAsync()
        {
            return await _repository
                .GetAllAsync();
        }

        /* =====================================
        🔥 GET BY EMPRESA
        ===================================== */

        public async Task<IEnumerable<MetodoPagoCuenta>>
            GetByEmpresaAsync(
                int idEmpresa
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa

                        &&

                        x.Activo
                );
        }

        /* =====================================
        🔥 GET BY METODO
        ===================================== */

        public async Task<MetodoPagoCuenta?>
            GetByMetodoAsync(

                int idEmpresa,

                string metodoPago
            )
        {
            return await _repository
                .GetByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa

                        &&

                        x.MetodoPago
                        ==
                        metodoPago

                        &&

                        x.Activo
                );
        }
    }
}
