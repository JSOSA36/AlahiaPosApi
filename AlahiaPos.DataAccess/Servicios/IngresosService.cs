using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
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
        private readonly IPagoReclasificacionService _reclasificacionService;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly ICuentaFinancieraService _cuentaFinancieraService;
        private readonly IContabilidadEventPublisher _contabilidadEvents;

        public IngresosService(
            IRepository<Ingresos> repository,
            IRepository<FacturaHeaders> facturaHeaderRepository,
            IRepository<FacturaDetalles> facturaDetalleRepository,
            IRepository<Productos> productoRepository,
            IRepository<Area> areaRepository,
            IRepository<AreaNegocio> areaNegocioRepository,
            AlahiaPosContext alahiaPosContext,
            IPagoReclasificacionService reclasificacionService,
            IMovimientoFinancieroService movimientoFinancieroService,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            ICuentaFinancieraService cuentaFinancieraService,
            IContabilidadEventPublisher contabilidadEvents)
        {
            _repository = repository;
            _facturaHeaderRepository = facturaHeaderRepository;
            _facturaDetalleRepository = facturaDetalleRepository;
            _productoRepository = productoRepository;
            _areaRepository = areaRepository;
            _areaNegocioRepository = areaNegocioRepository;
            _alahiaPosContext = alahiaPosContext;
            _reclasificacionService = reclasificacionService;
            _movimientoFinancieroService = movimientoFinancieroService;
            _metodoPagoCuentaService = metodoPagoCuentaService;
            _cuentaFinancieraService = cuentaFinancieraService;
            _contabilidadEvents = contabilidadEvents;
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

            var metodosEfectivos = await _reclasificacionService
                .ObtenerMetodosEfectivosAntesCierreAsync(
                    idEmpresa,
                    ingresos.Select(i => i.IdIngreso));

            return ingresos

                .GroupBy(x => metodosEfectivos.TryGetValue(x.IdIngreso, out var m) ? m : x.FormaPago)

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
            // Rango inclusivo de días calendario (evita perder ventas si fechaFin llega a 00:00:00).
            var desde = fechaInicio.Date;
            var hastaExclusivo = fechaFin.Date.AddDays(1);

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
                      && h.FechaInseccion >= desde
                      && h.FechaInseccion < hastaExclusivo
                      && h.EstaCancelada == false
                      && i.EstaAnulado == false
                      && a.IdEmpresa == idEmpresa
                      && an.IdEmpresa == idEmpresa
                      

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
                .GroupBy(x => CajaMetodoPagoDto.Clave(x.FormaPago))
                .Select(g => new CajaMetodoPagoDto
                {
                    FormaPago = CajaMetodoPagoDto.Etiqueta(
                        g.Key,
                        g.Select(x => x.FormaPago).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))),
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.FormaPago)
                .ToList();
        }

        public async Task<RegistrarIngresoExtraResult> RegistrarIngresoExtraCompletoAsync(
            RegistrarIngresoExtraRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Monto <= 0)
                throw new InvalidOperationException("Debe ingresar un monto válido.");

            if (!string.IsNullOrWhiteSpace(request.ClaveIdempotencia))
            {
                var movExistente = await _alahiaPosContext.MovimientoFinanciero.AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IdEmpresa == request.IdEmpresa
                        && x.ClaveIdempotencia == request.ClaveIdempotencia);

                if (movExistente != null)
                {
                    var ingresoExistente = await _alahiaPosContext.Ingresos.AsNoTracking()
                        .FirstOrDefaultAsync(i =>
                            i.IdEmpresa == request.IdEmpresa
                            && i.IdMovimientoFinanciero == movExistente.IdMovimientoFinanciero
                            && i.EstaAnulado == false)
                        ?? await _alahiaPosContext.Ingresos.AsNoTracking()
                            .FirstOrDefaultAsync(i =>
                                i.IdEmpresa == request.IdEmpresa
                                && i.Referencia == (request.Referencia ?? $"EXT-{request.IdTesoreriaExtractoLinea}")
                                && i.EstaAnulado == false);

                    return new RegistrarIngresoExtraResult
                    {
                        IdIngreso = ingresoExistente?.IdIngreso ?? 0,
                        IdMovimientoFinanciero = movExistente.IdMovimientoFinanciero,
                        YaExistia = true
                    };
                }
            }

            var formaPago = string.IsNullOrWhiteSpace(request.FormaPago)
                ? (request.DesdeExtractoBancario ? "TRANSFERENCIA" : "Efectivo")
                : request.FormaPago.Trim();

            int? idCuenta = request.IdCuentaFinanciera;
            MetodoPagoCuenta? metodoCuenta = null;
            if (!idCuenta.HasValue || idCuenta.Value <= 0)
            {
                metodoCuenta = await _metodoPagoCuentaService.GetByMetodoAsync(
                    request.IdEmpresa, formaPago);
                if (metodoCuenta != null && metodoCuenta.IdCuentaFinanciera > 0)
                    idCuenta = metodoCuenta.IdCuentaFinanciera;
            }

            if (request.DesdeExtractoBancario && (idCuenta is not > 0))
                throw new InvalidOperationException(
                    "La conciliación requiere una cuenta financiera válida para el ingreso.");

            var fecha = request.Fecha ?? DateTime.Now;
            var referencia = request.Referencia;
            if (string.IsNullOrWhiteSpace(referencia) && request.IdTesoreriaExtractoLinea is > 0)
                referencia = $"EXT-{request.IdTesoreriaExtractoLinea}";

            var ingreso = new Ingresos
            {
                IdEmpresa = request.IdEmpresa,
                FechaRegistro = fecha,
                Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
                    ? (request.Categoria ?? "Ingreso")
                    : request.Descripcion.Trim(),
                Categoria = request.Categoria?.Trim(),
                Origen = string.IsNullOrWhiteSpace(request.Origen)
                    ? (request.DesdeExtractoBancario ? "Conciliación Bancaria" : null)
                    : request.Origen.Trim(),
                Monto = request.Monto,
                FormaPago = formaPago,
                Referencia = referencia ?? string.Empty,
                IdUsuario = request.IdUsuario,
                Nota = request.Nota,
                EstaAnulado = false,
                EstaCerrada = false,
                IdFacturaHeader = null
            };

            await _repository.Save(ingreso);

            int? idMov = null;
            string? tipoCuenta = null;

            if (idCuenta is > 0)
            {
                var cuenta = await _cuentaFinancieraService.GetByIdAsync(idCuenta.Value);
                tipoCuenta = cuenta?.TipoCuenta;

                var referenciaTipoMov = request.DesdeExtractoBancario ? "EXTRACTO" : "INGRESO";
                var referenciaIdMov = request.DesdeExtractoBancario
                    ? request.IdTesoreriaExtractoLinea
                    : (ingreso.IdIngreso > 0 ? ingreso.IdIngreso : (int?)null);

                await _movimientoFinancieroService.RegistrarEntradaAsync(
                    request.IdEmpresa,
                    request.IdUsuario,
                    idCuenta.Value,
                    request.Monto,
                    $"Ingreso - {ingreso.Categoria}",
                    ingreso.Descripcion ?? "Entrada automática por ingreso",
                    categoria: "INGRESO",
                    referenciaId: referenciaIdMov,
                    referenciaTipo: referenciaTipoMov,
                    claveIdempotencia: request.ClaveIdempotencia);

                if (!string.IsNullOrWhiteSpace(request.ClaveIdempotencia))
                {
                    var mov = await _alahiaPosContext.MovimientoFinanciero.AsTracking()
                        .FirstOrDefaultAsync(x =>
                            x.IdEmpresa == request.IdEmpresa
                            && x.ClaveIdempotencia == request.ClaveIdempotencia);
                    if (mov != null)
                    {
                        idMov = mov.IdMovimientoFinanciero;
                        if (request.FechaMovimiento.HasValue)
                            mov.FechaMovimiento = request.FechaMovimiento.Value;
                        if (request.IdTesoreriaConciliacion is > 0)
                        {
                            mov.IdTesoreriaConciliacion = request.IdTesoreriaConciliacion;
                            mov.EstadoConciliacion = "CONCILIADO";
                            mov.FechaConciliacion = DateTime.UtcNow;
                            mov.IdUsuarioConciliacion = request.IdUsuario;
                        }
                    }
                }

                if (idMov is > 0)
                {
                    ingreso.IdMovimientoFinanciero = idMov;
                    _repository.Update(ingreso.IdIngreso, ingreso);
                }
            }

            var contab = await _contabilidadEvents.TryPublishAsync(new IngresoExtraRegistradoEvent
            {
                IdEmpresa = ingreso.IdEmpresa,
                IdUsuario = ingreso.IdUsuario ?? 0,
                Fecha = ingreso.FechaRegistro == default ? DateTime.Now : ingreso.FechaRegistro,
                ReferenciaId = ingreso.IdIngreso,
                ReferenciaTipo = "Ingreso",
                Monto = ingreso.Monto,
                Categoria = ingreso.Categoria,
                FormaPago = ingreso.FormaPago,
                TipoCuentaFinanciera = tipoCuenta,
                Descripcion = ingreso.Descripcion
            });

            return new RegistrarIngresoExtraResult
            {
                IdIngreso = ingreso.IdIngreso,
                IdMovimientoFinanciero = idMov,
                ContabilidadAdvertencia = contab.Advertencia,
                YaExistia = false
            };
        }
    }
}
