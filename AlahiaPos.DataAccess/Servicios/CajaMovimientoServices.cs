using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
namespace AlahiaPos.DataAccess.Servicios
{
    public class CajaMovimientoServices
        : ICajaMovimientoService
    {

        IRepository<CajaMovimiento> _repository;

        public CajaMovimientoServices(
            IRepository<CajaMovimiento> repository
        )
        {
            _repository = repository;
        }

        public async Task<int> CreateAsync(
            CajaMovimiento entity
        )
        {
            await _repository.Save(entity);

            return entity.IdCajaMovimiento;
        }

        public async Task UpdateAsync(
            CajaMovimiento entity
        )
        {
            _repository.Update(
                entity.IdCajaMovimiento,
                entity
            );

            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            _repository.Delete(id);

            await Task.CompletedTask;
        }

        public async Task<CajaMovimiento?>
            GetByIdAsync(int id)
        {
            return await _repository
                .GetByIdAsync(id);
        }

        public async Task<IEnumerable<CajaMovimiento>>
            GetAllAsync()
        {
            return await _repository
                .GetAllAsync();
        }

        public async Task<IEnumerable<CajaMovimiento>>
            GetByCajaAsync(
                int idCajaApertura
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdCajaApertura ==
                        idCajaApertura
                );
        }

        public async Task<IEnumerable<CajaMovimiento>>
            GetByFechaAsync(
                DateTime desde,
                DateTime hasta
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.FechaMovimiento.Date >=
                        desde.Date

                        &&

                        x.FechaMovimiento.Date <=
                        hasta.Date
                );
        }

        public async Task RegistrarEntradaAsync(
            CajaMovimiento entity
        )
        {
            entity.TipoMovimiento =
                "ENTRADA";

            await _repository.Save(entity);
        }

        public async Task RegistrarSalidaAsync(
            CajaMovimiento entity
        )
        {
            entity.TipoMovimiento =
                "SALIDA";

            await _repository.Save(entity);
        }

        public async Task<decimal>
            GetTotalEntradasAsync(
                int idCajaApertura
            )
        {
            var movimientos =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdCajaApertura ==
                        idCajaApertura

                        &&

                        x.TipoMovimiento ==
                        "ENTRADA"
                );

            return movimientos
                .Sum(x => x.Monto);
        }

        public async Task<decimal>
            GetTotalSalidasAsync(
                int idCajaApertura
            )
        {
            var movimientos =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdCajaApertura ==
                        idCajaApertura

                        &&

                        x.TipoMovimiento ==
                        "SALIDA"
                );

            return movimientos
                .Sum(x => x.Monto);
        }
    }
}
