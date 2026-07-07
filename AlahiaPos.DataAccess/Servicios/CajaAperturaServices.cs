using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
namespace AlahiaPos.DataAccess.Servicios
{
    public class CajaAperturaServices
       : ICajaAperturaService
    {

        IRepository<CajaApertura> _repository;

        public CajaAperturaServices(
            IRepository<CajaApertura> repository
        )
        {
            _repository = repository;
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        public async Task<int> CreateAsync(
            CajaApertura entity
        )
        {
            await _repository.Save(entity);

            return entity.IdCajaApertura;
        }

        /* =====================================
        🔥 UPDATE
        ===================================== */

        public async Task UpdateAsync(
            CajaApertura entity
        )
        {
            _repository.Update(
                entity.IdCajaApertura,
                entity
            );

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 DELETE
        ===================================== */

        public async Task DeleteAsync(int id)
        {
            _repository.Delete(id);

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        public async Task<CajaApertura?>
            GetByIdAsync(int id)
        {
            return await _repository
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        public async Task<IEnumerable<CajaApertura>>
GetAllAsync(int idEmpresa)
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x => x.IdEmpresa == idEmpresa
                );
        }

        /* =====================================
        🔥 CAJA ABIERTA
        ===================================== */

        public async Task<CajaApertura?>
            GetCajaAbiertaAsync(
                int idEmpresa,
                int idUsuario
            )
        {
            var result =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa ==
                        idEmpresa

                        &&

                        x.IdUsuario ==
                        idUsuario

                        &&

                        x.Estado ==
                        "ABIERTA"
                );

            return result
                .FirstOrDefault();
        }

        /* =====================================
        🔥 POR FECHA
        ===================================== */

        public async Task<IEnumerable<CajaApertura>>
     GetByFechaAsync(
         int idEmpresa,
         DateTime desde,
         DateTime hasta
     )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa == idEmpresa

                        &&

                        x.FechaApertura.Date >=
                        desde.Date

                        &&

                        x.FechaApertura.Date <=
                        hasta.Date
                );
        }

        /* =====================================
        🔥 EXISTE ABIERTA
        ===================================== */

        public async Task<bool>
            ExisteCajaAbiertaAsync(
                int idEmpresa,
                int idUsuario
            )
        {
            var caja =
                await GetCajaAbiertaAsync(
                    idEmpresa,
                    idUsuario
                );

            return caja != null;
        }

        /* =====================================
        🔥 ABRIR
        ===================================== */

        public async Task AbrirCajaAsync(
            CajaApertura entity
        )
        {
            await _repository.Save(entity);
        }

        /* =====================================
        🔥 CERRAR
        ===================================== */

        public async Task CerrarCajaAsync(
            int idCajaApertura
        )
        {
            var caja =
                await _repository
                .GetByIdAsync(
                    idCajaApertura
                );

            if (caja == null)
                return;

            caja.Estado = "CERRADA";

            _repository.Update(
                caja.IdCajaApertura,
                caja
            );
        }
    }
}
