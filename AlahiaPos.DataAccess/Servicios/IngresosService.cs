using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IngresosService : IIngresos
    {
        private readonly IRepository<Ingresos> _repository;
        private readonly IRepository<FacturaHeaders> _facturaHeaderRepository;
        private readonly IRepository<FacturaDetalles> _facturaDetalleRepository;
        private readonly IRepository<Productos> _productoRepository;
        private readonly IRepository<Area> _areaRepository;
        private readonly IRepository<AreaNegocio> _areaNegocioRepository;
        private readonly AlahiaPosContext _alahiaPosContext;
        public IngresosService(IRepository<Ingresos> repository, 
            IRepository<FacturaHeaders> facturaHeaderRepository,
            IRepository<FacturaDetalles> facturaDetalleRepository,
            IRepository<Productos> productoRepository,
            IRepository<Area> areaRepository,
            IRepository<AreaNegocio> areaNegocioRepository, AlahiaPosContext alahiaPosContext)
        {
            _repository = repository;
            _facturaHeaderRepository = facturaHeaderRepository;
            _facturaDetalleRepository = facturaDetalleRepository;
            _productoRepository = productoRepository;
            _areaRepository = areaRepository;
            _areaNegocioRepository = areaNegocioRepository;
            _alahiaPosContext = alahiaPosContext;
        }
        public async Task<bool> ExisteIngresoPorCita(int idCita)
        {
            var referencia = $"Cita #{idCita}";

            var ingreso = await _repository.GetByExpresionAsync(
                x => x.Referencia == referencia && x.EstaAnulado == false
            );

            return ingreso != null;
        }
        public async Task<List<CierreCajaDto>>
GetIngresosEncargosPorFecha(

    int idEmpresa,

    DateTime fechaInicio,

    DateTime fechaFin
)
        {
            var ingresos =

                await _repository
                .GetAllByExpresionAsync(i =>

                    i.IdEmpresa == idEmpresa

                    &&

                    i.FechaRegistro.Date >=
                    fechaInicio.Date

                    &&

                    i.FechaRegistro.Date <=
                    fechaFin.Date

                    &&

                    i.EstaAnulado == false

                    &&

                    (

                        i.Categoria == "Abono Encargo"

                        ||

                        i.Categoria == "Pago Encargo"
                    )
                );

            var resultado =

                ingresos

                .GroupBy(i => i.FormaPago)

                .Select(g =>

                    new CierreCajaDto
                    {
                        FormaPago = g.Key,

                        Total = g.Sum(x => x.Monto)
                    })

                .OrderBy(x => x.FormaPago)

                .ToList();

            return resultado;
        }
        public async Task<List<CajaMetodoPagoDto>>
   GetIngresosPendientesCaja(
       int idEmpresa,
       int idUsuario
   )
        {
            var ingresos =
                await _repository.GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa &&

                    x.IdUsuario == idUsuario &&

                    x.EstaAnulado == false &&

                    x.EstaCerrada == false
                );

            return ingresos

                .GroupBy(x => x.FormaPago)

                .Select(g => new CajaMetodoPagoDto
                {
                    FormaPago = g.Key,
                    Total = g.Sum(x => x.Monto)
                })

                .OrderBy(x => x.FormaPago)

                .ToList();
        }

        // 🔹 Obtener todos los ingresos por empresa
        public async Task<IEnumerable<Ingresos>> GetAllIngresos(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(x => x.IdEmpresa == IdEmpresa && x.EstaAnulado == false);
        }

        // 🔹 Obtener ingreso por ID
        public async Task<Ingresos?> GetIngresoById(int IdIngreso)
        {
            return await _repository.GetByExpresionAsync(x => x.IdIngreso == IdIngreso && x.EstaAnulado==false);
        }

        // 🔹 Insertar nuevo ingreso
        public async Task InsertIngreso(Ingresos ingreso)
        {
            ingreso.FechaRegistro = DateTime.Now;
            await _repository.Save(ingreso);
        }
        /* ==========================================
🔹 CERRAR INGRESOS EXTRAORDINARIOS
========================================== */

        public async Task CerrarIngresosPendientes(
            int idEmpresa,
            int idUsuario,
            int idCajaCierre
        )
        {
            var ingresos =
                await _repository
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa

                    && x.IdUsuario == idUsuario

                    && x.EstaAnulado == false

                    && x.EstaCerrada != true

                    // Solo ingresos extraordinarios
                    && x.IdFacturaHeader == null
                );

            foreach (var ingreso in ingresos)
            {
                ingreso.EstaCerrada = true;

                ingreso.IdCajaCierre = idCajaCierre;

                _repository.Update(

                    ingreso.IdIngreso,

                    ingreso
                );
            }
        }
        public async Task<List<CierreCajaDto>> GetIngresosByFechaCaja(
    int idEmpresa,
    DateTime fechaInicio,
    DateTime fechaFin)
        {
            var ingresos = await _repository.GetAllByExpresionAsync(i =>
                i.IdEmpresa == idEmpresa &&
                i.FechaRegistro.Date >= fechaInicio.Date &&
                i.FechaRegistro.Date <= fechaFin.Date
                && i.EstaAnulado == false
            );

            var resultado = ingresos
                .GroupBy(i => i.FormaPago)
                .Select(g => new CierreCajaDto
                {
                    FormaPago = g.Key,
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.FormaPago) // 🔥 ORDEN ALFABÉTICO
                .ToList();

            return resultado;
        }

        public async Task RevertirIngresoPorFactura(int idFactura, int idEmpresa)
        {
            var ingresos = await _repository
                .GetAllByExpresionAsync(x =>
                    x.IdFacturaHeader == idFactura &&
                    x.IdEmpresa == idEmpresa &&
                    x.EstaAnulado == false);

            foreach (var ing in ingresos)
            {
                ing.EstaAnulado = true;
                ing.FechaRegistro = DateTime.Now;

                 _repository.Update(ing.IdIngreso,ing);
            }
        }

        // 🔹 Actualizar ingreso existente
        public async Task UpdateIngreso(int IdIngreso, Ingresos ingreso)
        {
            var existing = await _repository.GetByExpresionAsync(x => x.IdIngreso == IdIngreso);
            if (existing == null) return;

            existing.Descripcion = ingreso.Descripcion;
            existing.Categoria = ingreso.Categoria;
            existing.Origen = ingreso.Origen;
            existing.Monto = ingreso.Monto;
            existing.FormaPago = ingreso.FormaPago;
            existing.Referencia = ingreso.Referencia;
            existing.Nota = ingreso.Nota;
            existing.IdCliente = ingreso.IdCliente;
            existing.IdFacturaHeader = ingreso.IdFacturaHeader;

            _repository.Update(IdIngreso, existing);
        }

        // 🔹 Eliminar ingreso
        public void DeleteIngreso(int IdIngreso)
        {
            _repository.Delete(IdIngreso);
        }

        // 🔹 Filtrar ingresos por rango de fechas
        public async Task<IEnumerable<Ingresos>> GetIngresosByFecha(
    int IdEmpresa,
    DateTime fechaInicio,
    DateTime fechaFin)
        {
            var lista = await _repository.GetAllByExpresionAsync(x =>

                x.IdEmpresa == IdEmpresa

                && x.FechaRegistro.Date >= fechaInicio.Date

                && x.FechaRegistro.Date <= fechaFin.Date

                && x.EstaAnulado == false
            );

            return lista
                .OrderByDescending(x => x.FechaRegistro);
        }

        // ==========================================
        // 🔹 MÉTODOS NUEVOS PARA EL DASHBOARD
        // ==========================================
        public async Task<IEnumerable<IngresosPorLineaNegocioDto>> GetIngresosPorLineaNegocio(
      int idEmpresa,
      DateTime fechaInicio,
      DateTime fechaFin)
        {
            var resultado = await (
                from d in _alahiaPosContext.FacturaDetalles
                join h in _alahiaPosContext.FacturaHeaders
                    on d.IdFacturaHeader equals h.IdFacturaHeader
                join p in _alahiaPosContext.Productos
                    on d.IdProducto equals p.IdProducto
                join a in _alahiaPosContext.Areas
                    on p.IdArea equals a.IdArea
                join an in _alahiaPosContext.AreaNegocio
                    on a.IdAreaNegocio equals an.IdAreaNegocio
                join i in _alahiaPosContext.Ingresos
                    on h.IdFacturaHeader equals i.IdFacturaHeader

                where h.IdEmpresa == idEmpresa
                      && h.FechaInseccion >= fechaInicio
                      && h.FechaInseccion <= fechaFin
                      && h.EstaCancelada == false
                      && i.EstaAnulado == false
                      

                // 🔥 total de la factura
                let totalFactura = h.Total

                // 🔥 proporción del detalle dentro de la factura
                let proporcion = totalFactura == 0
                    ? 0
                    : (d.SubTotal / totalFactura)

                group new { i, proporcion } by new
                {
                    an.IdAreaNegocio,
                    an.Nombre,
                    i.FormaPago
                } into g

                orderby g.Key.Nombre

                select new IngresosPorLineaNegocioDto
                {
                    IdAreaNegocio = g.Key.IdAreaNegocio,
                    AreaNegocio = g.Key.Nombre,
                    MetodoPago = g.Key.FormaPago,

                    // 🔥 DISTRIBUCIÓN CORRECTA
                    Total = g.Sum(x => x.i.Monto * x.proporcion)
                }
            ).ToListAsync();

            return resultado;
        }
        public async Task<bool> ExisteIngreso(int idFactura, string metodo)
        {
            return await _alahiaPosContext.Ingresos.AnyAsync(x =>
                x.IdFacturaHeader == idFactura &&
                x.FormaPago == metodo 
                
            );
        }
        // 🔸 Total de ingresos del día
        public async Task<decimal> GetTotalIngresosDia(int IdEmpresa)
        {
            var hoy = DateTime.Today;
            var ingresos = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == IdEmpresa && x.FechaRegistro.Date == hoy
                && x.EstaAnulado==false
            );

            return ingresos.Sum(x => x.Monto);
        }

        // 🔸 Total de ingresos del mes actual
        public async Task<decimal> GetTotalIngresosMes(int IdEmpresa)
        {
            var hoy = DateTime.Today;
            var ingresos = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == IdEmpresa &&
                     x.FechaRegistro.Month == hoy.Month &&
                     x.FechaRegistro.Year == hoy.Year
                      && x.EstaAnulado == false
            );

            return ingresos.Sum(x => x.Monto);
        }

        // 🔸 Histórico de ingresos agrupados por mes (últimos 12 meses)
        public async Task<IEnumerable<HistoricoIngresosDto>> GetHistoricoIngresos(int IdEmpresa)
        {
            var haceUnAnio = DateTime.Today.AddMonths(-11);
            var ingresos = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == IdEmpresa && x.FechaRegistro >= haceUnAnio
                 && x.EstaAnulado == false
            );

            var agrupado = ingresos
                .GroupBy(x => new { x.FechaRegistro.Year, x.FechaRegistro.Month })
                .Select(g => new HistoricoIngresosDto
                {
                    Mes = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Mes)
                .ToList();

            return agrupado;
        }
     public async Task<List<CajaMetodoPagoDto>>
GetIngresosByCajaCierre(
    int idCajaCierre
)
        {
            /* =====================================
            🔥 FACTURAS DEL CIERRE
            ===================================== */

            var facturas =
                await _facturaHeaderRepository
                .GetAllByExpresionAsync(x =>

                    x.IdCajaCierre == idCajaCierre

                    &&

                    x.EstaCancelada == false
                );

            var idsFacturas =

                facturas
                .Select(x => x.IdFacturaHeader)
                .ToList();

            /* =====================================
            🔥 INGRESOS DE FACTURAS
            ===================================== */

            var ingresosFacturas =
                await _repository
                .GetAllByExpresionAsync(x =>

                    idsFacturas.Contains((int)x.IdFacturaHeader)

                    &&

                    x.EstaAnulado == false
                );

            /* =====================================
            🔥 INGRESOS EXTRAORDINARIOS
            ===================================== */

            var ingresosExtra =
                await _repository
                .GetAllByExpresionAsync(x =>

                    x.IdCajaCierre == idCajaCierre

                    &&

                    x.IdFacturaHeader == null

                    &&

                    x.EstaAnulado == false
                );

            /* =====================================
            🔥 UNIR
            ===================================== */

            var ingresos =

                ingresosFacturas
                .Concat(ingresosExtra)
                .ToList();

            /* =====================================
            🔥 AGRUPAR
            ===================================== */

            return ingresos

                .GroupBy(x => x.FormaPago)

                .Select(g => new CajaMetodoPagoDto
                {
                    FormaPago = g.Key,

                    Total = g.Sum(x => x.Monto)
                })

                .OrderBy(x => x.FormaPago)

                .ToList();
        }
    }
}
