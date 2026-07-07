using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class LavadorConsumoServices : ILavadorConsumoServices
    {
        IRepository<LavadorConsumo> _repository;
        IRepository<Empleados> _empleados;
        IFacturaHeader _facturaHeader;

        public LavadorConsumoServices(
            IRepository<LavadorConsumo> repository,
            IRepository<Empleados> empleados,
            IFacturaHeader facturaHeader
        )
        {
            _repository = repository;
            _empleados = empleados;
            _facturaHeader = facturaHeader;
        }
        public async Task<IEnumerable<LavadorConsumo>> GetHistorialByEmpleado(
         int idEmpleado,
         int idEmpresa,
         DateTime desde,
         DateTime hasta)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpleado == idEmpleado &&
                c.IdEmpresa == idEmpresa &&
                c.Fecha.Date >= desde.Date &&
                c.Fecha.Date <= hasta.Date
            );
        }
        public decimal GetConsumoLavador(DateTime desde, DateTime hasta, int idEmpleado, int idEmpresa)
        {
            return _repository
                .GetAllByExpresionNoAsync(c =>
                    c.Fecha.Date >= desde.Date &&
                    c.Fecha.Date <= hasta.Date &&
                    c.IdEmpleado == idEmpleado &&
                    c.IdEmpresa == idEmpresa
                )
                .Sum(c => (decimal?)c.Monto) ?? 0;
        }
        // ⭐ Registrar consumo (fiado)
        public async Task RegistrarConsumo(LavadorConsumoCreateDto dto)
        {
            var entity = new LavadorConsumo
            {
                IdEmpleado = dto.IdEmpleado,
                IdEmpresa = dto.IdEmpresa,
                IdConsumo=dto.IdConsumo,
                Fecha = DateTime.Now,
                Concepto = dto.Concepto,
                Monto = dto.Monto,
                Pagado = false,

            };

            await _repository.Save(entity);
        }

        // ⭐ Abonar consumo parcial
        public async Task AbonarConsumo(int idConsumo, decimal montoAbono)
        {
            var consumo = _repository.GetById(idConsumo);

            if (consumo == null)
                return;

            consumo.Monto += montoAbono;

            if (consumo.Monto >= consumo.Monto)
            {
                consumo.Monto = consumo.Monto;

            }

            _repository.Update(idConsumo, consumo);
        }

        // ⭐ Obtener pendientes por lavador
        public async Task<IEnumerable<LavadorConsumo>> GetPendientesByEmpleado(int idEmpleado, int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpleado == idEmpleado &&
                c.IdEmpresa == idEmpresa

            );
        }

        // ⭐ Balance total real del lavador
        public async Task<decimal> GetBalanceLavador(int idEmpleado, int idEmpresa)
        {
            var consumos = await GetPendientesByEmpleado(idEmpleado, idEmpresa);

            if (consumos == null || !consumos.Any())
                return 0;

            return consumos.Sum(c => c.Monto);
        }

        // ⭐ Listado administrativo
        public async Task<IEnumerable<LavadorConsumo>> GetListadoGeneral(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa
            );
        }
        public async Task EliminarConsumo(int idConsumo)
        {
            var consumo = _repository.GetById(idConsumo);

            if (consumo == null)
                return;

            _repository.Delete(idConsumo);
        }
        // ⭐ Saldar consumo completo
        public async Task SaldarConsumo(int idConsumo)
        {
            var consumo = _repository.GetById(idConsumo);

            if (consumo == null)
                return;

            consumo.Monto = consumo.Monto;


            _repository.Update(idConsumo, consumo);
        }

        // ⭐ DASHBOARD FINANCIERO DEL LAVADOR (🔥 NUEVO NIVEL)
        public async Task<LavadorDashboardDto> GetDashboardLavador(
      int idEmpleado,
      int idEmpresa,
      DateTime desde,
      DateTime hasta
  )
        {
            var empleado = _empleados.GetById(idEmpleado);

            // 🔵 CONSUMOS EN RANGO
            var consumos = await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpleado == idEmpleado &&
                c.IdEmpresa == idEmpresa &&
                c.Fecha.Date >= desde.Date &&
                c.Fecha.Date <= hasta.Date
            );

            decimal totalConsumido = 0;
            decimal balancePendiente = 0;

            var listaDetalle = new List<LavadorConsumoDetalleDto>();

            if (consumos != null && consumos.Any())
            {
                totalConsumido = consumos.Sum(c => c.Monto);

                balancePendiente = consumos
                    .Where(c => c.Pagado == false)
                    .Sum(c => c.Monto);

                listaDetalle = consumos
                    .OrderByDescending(c => c.Fecha)
                    .Select(c => new LavadorConsumoDetalleDto
                    {
                        Fecha = c.Fecha,
                        IdConsumo = c.IdConsumo,
                        Concepto = c.Concepto,
                        Monto = c.Monto
                    }).ToList();
            }

            // 🔵 COMISIONES EN RANGO
            var comisiones = _facturaHeader
                .GetComisionesDetalle(desde, hasta, idEmpresa);

            decimal totalComision = comisiones
                .Where(c => c.IdEmpleado == idEmpleado)
                .Sum(c => c.TotalComisiones);

            return new LavadorDashboardDto
            {
                IdEmpleado = idEmpleado,
                NombreLavador = empleado?.Nombre,
                TotalConsumido = totalConsumido,
                BalancePendiente = balancePendiente,
                TotalComision = totalComision,
                PagoNetoEstimado = totalComision - balancePendiente < 0
                    ? 0
                    : totalComision - balancePendiente,
                Consumos = listaDetalle
            };
        }
    }
}