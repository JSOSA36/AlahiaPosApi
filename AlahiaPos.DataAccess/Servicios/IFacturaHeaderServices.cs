using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using PrinterLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IFacturaHeaderServices : IFacturaHeader
    {

        IRepository<FacturaHeaders> _repository;
        IRepository<Empleados> _Empleados;
        IRepository<Productos> _Productos;
        IRepository<EmpleadoAreaComision> _IEmpleadoComision;
        IRepository<FacturaDetalles> _FacturaDetalles;
        IRepository<Clientes> _Clientes;
        IRepository<Empresas> _Empresas;
        IRepository<Gastos> _GastosRepository;
        IRepository<LavadorConsumo> _ILavadorConsumo;
        IRepository<Ingresos> _Ingresos;
        IRepository<ECFEncabezado> _EcfEncabezados;
        IPagoReclasificacionService _reclasificacionService;
        public IFacturaHeaderServices(IRepository<FacturaHeaders> repository,
            IRepository<Empleados> Empleados, IRepository<Productos> Productos,
            IRepository<FacturaDetalles> facturaDetalles, IRepository<EmpleadoAreaComision>
            iEmpleadoComision, IRepository<Clientes> clientes,
             IRepository<Gastos> GastosRepository,
        IRepository<Empresas> empresas,
            IRepository<LavadorConsumo> LavadorConsumo, IRepository<Ingresos> ingresos,
            IPagoReclasificacionService reclasificacionService,
            IRepository<ECFEncabezado> ecfEncabezados)
        {
            _repository = repository;
            _Empleados = Empleados;
            _Productos = Productos;
            _FacturaDetalles = facturaDetalles;
            _IEmpleadoComision = iEmpleadoComision;
            _Clientes = clientes;
            _GastosRepository = GastosRepository;
            _Empresas = empresas;
            _ILavadorConsumo = LavadorConsumo;
            _Ingresos = ingresos;
            _reclasificacionService = reclasificacionService;
            _EcfEncabezados = ecfEncabezados;
        }


        public FacturaHeaders GetById(int Id)
        {
            return _repository.GetById(Id);
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllOrdenes(
    int IdEmpresa
)
        {
            return await _repository
                .GetAllByExpresionAsync(c =>

                    c.IdTipoDocumentos == 10
                    &&

                    c.IdEmpresa == IdEmpresa,

                    "Clientes",
                    "FacturaDetalles.Productos"
                );
        }

        public async Task<IEnumerable<FacturaHeaders>> GetAllCotizaciones(
            int IdEmpresa
        )
        {
            return await _repository
                .GetAllByExpresionAsync(c =>

                    c.IdTipoDocumentos == 2
                    &&

                    c.IdEmpresa == IdEmpresa,

                    "Clientes",
                    "FacturaDetalles.Productos"
                );
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllFacturas(int IdEmpresa)
        {
            DateTime fechaDesde = DateTime.Now.AddDays(-30);

            return await _repository.GetAllByExpresionAsync(c =>
                c.IdTipoDocumentos == 1
                && c.IdEmpresa == IdEmpresa

                && c.FechaInseccion >= fechaDesde,

                "Clientes", "FacturaDetalles");
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllFacturaPendientes(int IdCliente, int IdEmpresa)
        {
            if (IdCliente > 0)
            {
                return await _repository.GetAllByExpresionAsync(c =>
                 c.IdTipoDocumentos == 1 &&
                 c.Estado == "Pendiente" &&
                 c.TipoFactura == "Credito" &&
                 c.EstaCancelada == false &&
                 c.Pendiente > 0 &&
                 c.IdEmpresa == IdEmpresa &&
                 c.IDCliente == IdCliente,
                    "Clientes", "FacturaDetalles");
            }
            else
            {
                return await _repository.GetAllByExpresionAsync(c =>
                 c.IdTipoDocumentos == 1 &&
                 c.Estado == "Pendiente" &&
                 c.TipoFactura == "Credito" &&
                 c.EstaCancelada == false &&
                 c.Pendiente > 0 &&
                 c.IdEmpresa == IdEmpresa,
                    "Clientes", "FacturaDetalles");
            }
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllFactById(int _IdFact)
        {

            return await _repository.GetAllByExpresionAsync(c =>
             c.IdTipoDocumentos == 1
             && c.IdFacturaHeader == _IdFact);
        }
        public async Task<bool> EliminarFacturaCompleta(int idFactura)
        {
            // 🔥 eliminar detalles primero
            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(x => x.IdFacturaHeader == idFactura);

            if (detalles != null && detalles.Any())
            {
                foreach (var d in detalles)
                {
                    _FacturaDetalles.Delete(d.IdFacturaDetalle);
                }
            }

            // 🔥 eliminar header limpio sin tracking de relaciones
            var factura = new FacturaHeaders
            {
                IdFacturaHeader = idFactura
            };

            _repository.DeleteEntity(factura); // 👈 ahora te explico esto

            return true;
        }

        public async Task<IEnumerable<FacturaHeaders>> GetAllFacturaFacturaHeader(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == IdEmpresa);
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllFacturaFacturaHeaderByIdMesa(int IdEmpresa)
        {



            return await _repository.GetAllByExpresionAsync(c => c.IdTipoDocumentos == 10 && c.IdEmpresa ==
            IdEmpresa, "FacturaDetalles");
        }
        public async Task<FacturaHeaders> GetAllFacturaFacturaHeaderById(int IdFacturaHeader, int IdEmpresa)
        {

            return await _repository.GetByExpresionAsync(c => c.IdFacturaHeader == IdFacturaHeader && c.IdEmpresa == IdEmpresa,
                 "Clientes", "Empleados", "Mesas", "FacturaDetalles");
        }
        public async Task<FacturaHeaders> GetFacturaHeaderById(int IdFacturaHeader, int IdEmpresa)
        {

            return await _repository.GetByExpresionAsync(c => c.IdFacturaHeader == IdFacturaHeader &&
            c.IdEmpresa == IdEmpresa,
                 "FacturaDetalles");
        }

        public async Task InsertFacturaHeader(FacturaHeaders FacturaHeader)
        {
            await AsegurarLimiteFacturacionAsync(FacturaHeader);
            await _repository.Save(FacturaHeader);
        }

        /// <summary>
        /// LimiteFacturacion en Empresa: 0 = ilimitado; &gt;0 = tope de ingresos RD$ (ventas tipo 1) del mes.
        /// </summary>
        private async Task AsegurarLimiteFacturacionAsync(FacturaHeaders header)
        {
            if (header == null || header.IdEmpresa <= 0) return;
            if (header.IdTipoDocumentos != 1) return;
            if (header.EstaCancelada) return;

            var emp = await _Empresas.GetByExpresionAsync(e => e.IdEmpresa == header.IdEmpresa);
            if (emp == null || emp.LimiteFacturacion <= 0) return;

            var limite = (decimal)emp.LimiteFacturacion;
            var inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var fin = inicio.AddMonths(1);
            var delMes = await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == header.IdEmpresa
                && c.IdTipoDocumentos == 1
                && !c.EstaCancelada
                && c.FechaInseccion >= inicio
                && c.FechaInseccion < fin);

            var idActual = header.IdFacturaHeader;
            var acumulado = delMes?
                .Where(c => c.IdFacturaHeader != idActual)
                .Sum(c => c.Total) ?? 0m;
            var proyectado = acumulado + header.Total;

            if (proyectado > limite)
            {
                throw new Exception(
                    $"Ha llegado al límite de ingresos de su plan contratado (RD$ {limite:N0}). " +
                    $"Usado este mes: RD$ {acumulado:N0}. " +
                    "Debe ponerse en contacto con nosotros para ampliar su servicio.");
            }
        }

        public void UpdateFacturaHeader(int Id, FacturaHeaders FacturaHeader)
        {
            FacturaHeader.IdEmpleadoComision = 1;
            if (FacturaHeader.IdFacturaHeader <= 0)
                FacturaHeader.IdFacturaHeader = Id;

            // Nunca async void: el cobro de una orden debe esperar este UPDATE
            // (IdTipoDocumentos = 1). Si no, la petición termina y el documento
            // se queda como orden — no aparece en histórico de facturas.
            AsegurarLimiteFacturacionAsync(FacturaHeader).GetAwaiter().GetResult();
            _repository.Update(Id, FacturaHeader);
        }
        public async Task<decimal> GetTotalFacturaPorCobrar(int IdEmpresa)
        {

            var result = await _repository.GetAllByExpresionAsync(c => c.Estado ==
            "Pendiente" && c.TipoFactura == "Credito" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false && c.IdEmpresa ==
            IdEmpresa);
            if (result == null)
            {
                return 0;
            }
            else
            {
                return (decimal)result.Sum(c => c.Pendiente);
            }
        }
        public async Task<decimal> GetTotalFactura(int IdEmpresa)
        {

            var Total = await _repository.
                GetAllByExpresionAsync(c => c.Estado == "Pagada" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false &&
             c.IdEmpresa == IdEmpresa && c.FechaInseccion.Year == DateTime.Now.Year
                && c.FechaInseccion.Month == DateTime.Now.Month);

            if (Total == null)
            {
                return 0;
            }
            else
            {
                return (decimal)Total.Sum(c => c.Total);
            }
        }

        public async Task<IEnumerable<FacturaHeaders>> GetFacturasXCobrar(DateTime? Desde,
            DateTime? Hasta, int IdCliente, int IdEmpresa)
        {

            IEnumerable<FacturaHeaders>? facturaHeaders = null;

            if (Desde != null && Hasta != null && IdCliente > 0)
            {
                facturaHeaders = await _repository.
                GetAllByExpresionAsync(c => c.Estado ==
            "Pendiente" && c.TipoFactura == "Credito" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false &&
                c.IdEmpresa == IdEmpresa && c.FechaInseccion >= Desde
                && c.FechaInseccion <= Hasta && c.IDCliente == IdCliente);



            }
            else if (Desde != null && Hasta != null && IdCliente == 0)
            {
                facturaHeaders = await _repository.
                GetAllByExpresionAsync(c => c.Estado ==
            "Pendiente" && c.TipoFactura == "Credito" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false &&
                c.IdEmpresa == IdEmpresa && c.FechaInseccion >= Desde
                && c.FechaInseccion <= Hasta);



            }
            else if (Desde == null && Hasta == null && IdCliente == 0)
            {
                facturaHeaders = await _repository.
                GetAllByExpresionAsync(c => c.Estado ==
            "Pendiente" && c.TipoFactura == "Credito" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false &&
                c.IdEmpresa == IdEmpresa);



            }
            else if (Desde == null && Hasta == null && IdCliente > 0)
            {
                facturaHeaders = await _repository.
                GetAllByExpresionAsync(c => c.Estado ==
            "Pendiente" && c.TipoFactura == "Credito" &&
            c.IdTipoDocumentos == 1 && c.EstaCancelada == false &&
                c.IdEmpresa == IdEmpresa && c.IDCliente == IdCliente);



            }

            return facturaHeaders;


        }
        public async Task<List<HistoricoVentaDto>> GetSumFactura(int IdEmpresa)
        {
            List<HistoricoVentaDto> historicoVentaDtos = new List<HistoricoVentaDto>();
            var robotFactories = await _repository.GetAllByExpresionAsync(c => c.FechaInseccion.Year == DateTime.Now.Year &&
                c.IdTipoDocumentos == 1 && c.Estado == "Pagada" && c.EstaCancelada == false && c.IdEmpresa == IdEmpresa);
            var result = robotFactories.
              GroupBy(x => x.FechaInseccion.Month)
              .Select(x => new
              {

                  Id = x.Key,
                  Total = x.Sum(c => c.Total)
              }).ToList().Take(4).OrderByDescending(c => c.Id);

            foreach (var item in result)
            {
                HistoricoVentaDto h = new HistoricoVentaDto();

                h.Mes = Utility.ReturnNameOfMonth(item.Id);
                h.Total = item.Total;
                historicoVentaDtos.Add(h);
            }

            return historicoVentaDtos;
        }
        public async Task<decimal> GetVentaDelDia(int IdEmpresa)
        {
            var Total = await _repository.
                GetAllByExpresionAsync(c => c.Estado == "Pagada" &&
           c.IdTipoDocumentos == IdEmpresa && c.EstaCancelada == false &&
           c.FechaInseccion == DateTime.Now.Date &&
           c.IdEmpresa == IdEmpresa);



            if (Total == null)
            {
                return 0;
            }
            else
            {
                return (decimal)Total.Sum(c => c.Total);
            }
        }

        public async Task<IEnumerable<FacturaHeaders>>
            GetFacturaByFechas(DateTime Desde, DateTime Hasta, int IdEmpresa)
        {
            return await _repository.
                GetAllByExpresionAsync(c => c.FechaInseccion >= Desde
           && c.FechaInseccion <= Hasta && c.Pagado > 0 && c.IdTipoDocumentos == 1 && c.EstaCancelada == false
           && c.IdEmpresa == IdEmpresa && c.TipoFactura == "Contado");


        }
        public decimal GetMontoEfectivo(DateTime _Desde, DateTime _Hasta, int IdEmpresa)
        {
            decimal Total = 0;


            bool factura = _repository.GetAnyNotAsync(c =>
                 c.EstaCerrada == false && c.EstaCancelada == false &&
                 c.IdEmpresa == IdEmpresa && c.FormaPago == "Efectivo"
                && c.FechaInseccion.Date >= _Desde.Date && c.IdTipoDocumentos == 1
                && c.FechaInseccion.Date <= _Hasta.Date);



            if (factura != false)
            {
                var _Result = _repository.GetAllByExpresionNoAsync(c =>
               c.EstaCerrada == false && c.EstaCancelada == false &&
               c.IdEmpresa == IdEmpresa &&
               c.FormaPago == "Efectivo" && c.IdTipoDocumentos == 1 &&
               c.EstaCancelada == false && c.FechaInseccion.Date >= _Desde
                && c.FechaInseccion <= _Hasta);

                Total = _Result.Sum(c => c.Total);
            }
            else
            {

                Total = 0;
            }
            return Total;

        }
        public decimal GetMontoTarjeta(DateTime _Desde, DateTime _Hasta, int IdEmpresa)
        {
            decimal Total = 0;
            DateTime _Fecha = Convert.ToDateTime(DateTime.Now.ToShortDateString());
            bool factura = _repository.GetAnyNotAsync(c =>
                 c.EstaCerrada == false && c.EstaCancelada == false && c.IdTipoDocumentos == 1 &&
                 c.IdEmpresa == IdEmpresa && c.FormaPago == "Tarjeta"
                 && c.FechaInseccion.Date >= _Desde
                && c.FechaInseccion <= _Hasta);



            if (factura != false)
            {
                var _Result = _repository.GetAllByExpresionNoAsync(c =>
               c.EstaCerrada == false && c.EstaCancelada == false &&
               c.IdEmpresa == IdEmpresa &&
               c.FormaPago == "Tarjeta" && c.IdTipoDocumentos == 1 &&
               c.EstaCancelada == false && c.FechaInseccion.Date >= _Desde
                && c.FechaInseccion <= _Hasta);

                Total = _Result.Sum(c => c.Total);
            }
            else
            {

                Total = 0;
            }
            return Total;

        }
        public decimal GetMontoTransferencia(DateTime _Desde, DateTime _Hasta, int IdEmpresa)
        {
            decimal Total = 0;
            DateTime _Fecha = Convert.ToDateTime(DateTime.Now.ToShortDateString());
            bool factura = _repository.GetAnyNotAsync(c =>
                 c.EstaCerrada == false && c.EstaCancelada == false &&
                 c.IdTipoDocumentos == 1 &&
                 c.IdEmpresa == IdEmpresa && c.FormaPago == "Transferencia"
                && c.FechaInseccion.Date >= _Desde
                && c.FechaInseccion.Date <= _Hasta);



            if (factura != false)
            {
                var _Result = _repository.GetAllByExpresionNoAsync(c =>
               c.EstaCerrada == false && c.EstaCancelada == false &&
               c.IdEmpresa == IdEmpresa &&
               c.FormaPago == "Transferencia" && c.IdTipoDocumentos == 1 &&
               c.EstaCancelada == false && c.FechaInseccion.Date >= _Desde
                && c.FechaInseccion <= _Hasta);

                Total = _Result.Sum(c => c.Total);
            }
            else
            {

                Total = 0;
            }
            return Total;

        }
        public IEnumerable<ComisionesResultDto> GetComisionesDetalle(
     DateTime Desde,
     DateTime Hasta,
     int IdEmpresa
 )
        {
            var listado = new List<ComisionesDto>();

            // 🔵 1️⃣ FACTURAS
            var facturas = _repository.GetAllByExpresionNoAsync(
                c => c.FechaInseccion.Date >= Desde.Date &&
                     c.FechaInseccion.Date <= Hasta.Date &&
                     c.IdTipoDocumentos == 1 &&
                     c.EstaCancelada != true &&
                     c.IdEmpresa == IdEmpresa
            ).ToList();

            if (!facturas.Any())
                return new List<ComisionesResultDto>();

            var facturasDict =
                facturas.ToDictionary(f => f.IdFacturaHeader);

            var idsFacturas =
                facturasDict.Keys.ToList();

            // 🔵 2️⃣ DETALLES
            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(
                    d => idsFacturas.Contains(d.IdFacturaHeader)
                )
                .ToList();

            if (!detalles.Any())
                return new List<ComisionesResultDto>();

            // 🔵 3️⃣ PRODUCTOS
            var idsProductos = detalles
                .Select(d => d.IdProducto)
                .Distinct()
                .ToList();

            var productos = _Productos
                .GetAllByExpresionNoAsync(
                    p => idsProductos.Contains(p.IdProducto)
                )
                .ToDictionary(p => p.IdProducto);

            // 🔵 4️⃣ EMPLEADOS
            var idsEmpleados = detalles
                .Where(d => d.IdEmpleadoComision.HasValue)
                .Select(d => d.IdEmpleadoComision.Value)
                .Distinct()
                .ToList();

            var empleados = _Empleados
                .GetAllByExpresionNoAsync(
                    e => idsEmpleados.Contains(e.IdEmpleados)
                )
                .ToDictionary(e => e.IdEmpleados);

            // 🔵 5️⃣ CONFIG COMISIONES
            var comisionesAreas = _IEmpleadoComision
                .GetAllByExpresionNoAsync(
                    c => c.IdEmpresa == IdEmpresa
                )
                .ToList();

            // 🔵 6️⃣ CONSUMOS
            var consumos = _ILavadorConsumo
                .GetAllByExpresionNoAsync(c =>
                    c.Fecha.Date >= Desde.Date &&
                    c.Fecha.Date <= Hasta.Date &&
                    c.IdEmpresa == IdEmpresa
                )
                .GroupBy(c => c.IdEmpleado)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Monto)
                );

            // 🔵 7️⃣ PROCESAR
            foreach (var item in detalles)
            {
                // 🔥 SIN EMPLEADO
                if (!item.IdEmpleadoComision.HasValue)
                    continue;

                var idEmpleado =
                    item.IdEmpleadoComision.Value;

                // 🔥 PRODUCTO
                if (!productos.TryGetValue(
                    item.IdProducto,
                    out var producto))
                    continue;

                // 🔥 EMPLEADO
                if (!empleados.TryGetValue(
                    idEmpleado,
                    out var empleado))
                    continue;

                // 🔥 FACTURA
                if (!facturasDict.TryGetValue(
                    item.IdFacturaHeader,
                    out var factura))
                    continue;

                // 🔥 CONFIG COMISIÓN
                var comisionArea = comisionesAreas
                    .FirstOrDefault(c =>
                        c.IdEmpleado == empleado.IdEmpleados &&
                        c.IdArea == producto.IdArea
                    );

                if (comisionArea == null)
                    continue;

                // =====================================
                // 🔥 FACTURA CRÉDITO
                // =====================================

                decimal proporcionPagada = 1;

                if (factura.TipoFactura == "Credito")
                {
                    if (factura.Total <= 0)
                        continue;

                    proporcionPagada =
                        factura.Pagado / factura.Total;

                    if (proporcionPagada > 1)
                        proporcionPagada = 1;
                }

                // =====================================
                // 🔥 SUBTOTAL SIN ITBIS
                // =====================================

                decimal subtotalSinItbis =
                    item.SubTotal - item.Itbis;

                if (subtotalSinItbis < 0)
                    subtotalSinItbis = 0;

                // =====================================
                // 🔥 BASE COMISIÓN
                // =====================================

                decimal baseParaComision =
                    factura.TipoFactura == "Credito"
                    ? subtotalSinItbis * proporcionPagada
                    : subtotalSinItbis;

                if (baseParaComision <= 0)
                    continue;

                // =====================================
                // 🔥 CALCULAR COMISIÓN
                // =====================================

                decimal totalComision = 0;

                if (comisionArea.TipoComision == "MONTO")
                {
                    totalComision =
                        (comisionArea.MontoComision ?? 0)
                        * item.Cantidad;
                }
                else
                {
                    decimal porciento =
                        comisionArea.PorcientoComision ?? 0;

                    if (porciento <= 0)
                        continue;

                    totalComision =
                        baseParaComision * porciento / 100;
                }

                if (totalComision <= 0)
                    continue;

                // =====================================
                // 🔥 AGREGAR RESULTADO
                // =====================================

                listado.Add(new ComisionesDto
                {
                    IdEmpleado = empleado.IdEmpleados,
                    Nombre = empleado.Nombre,
                    Fecha = factura.FechaInseccion,
                    ProductoServicio = producto.Nombre,

                    // 🔥 TOTAL SIN ITBIS
                    Total = baseParaComision,

                    TotalComisiones = totalComision
                });
            }

            // 🔵 8️⃣ AGRUPAR
            var resultado = listado
                .GroupBy(l => new
                {
                    l.IdEmpleado,
                    l.Nombre
                })

                .Select(g =>
                {
                    var totalComision =
                        g.Sum(x => x.TotalComisiones);

                    var montoBase =
                        g.Sum(x => x.Total);

                    var consumo =
                        consumos.TryGetValue(
                            g.Key.IdEmpleado,
                            out var c
                        )
                        ? c
                        : 0;

                    return new ComisionesResultDto
                    {
                        IdEmpleado = g.Key.IdEmpleado,

                        Empleados = g.Key.Nombre,

                        MontoBase = montoBase,

                        TotalComisiones = totalComision,

                        TotalConsumo = consumo,

                        NetoPagar =
                            totalComision - consumo < 0
                            ? 0
                            : totalComision - consumo
                    };
                })

                .OrderByDescending(x => x.NetoPagar)

                .ToList();

            return resultado;
        }

        public IEnumerable<ServicioEmpleadoDto> GetServicioByEMpleados(
     DateTime Desde,
     DateTime Hasta,
     int IdEmpresa)
        {
            var result = new List<ServicioEmpleadoDto>();

            var facturas = _repository.GetAllByExpresionNoAsync(
                c => c.FechaInseccion.Date >= Desde.Date &&
                     c.FechaInseccion.Date <= Hasta.Date &&
                     c.IdTipoDocumentos == 1 &&
                     c.EstaCancelada != true &&
                     c.IdEmpresa == IdEmpresa
            ).ToList();

            var Total = facturas.Sum(c => c.Total);
            if (facturas == null || facturas.Count == 0)
                return result;

            var clientesIds = facturas
                .Where(f => f.IDCliente.HasValue && f.IDCliente > 0)
                .Select(f => f.IDCliente!.Value)
                .Distinct()
                .ToList();

            var clientesPorId = clientesIds.Any()
                ? _Clientes
                    .GetAllByExpresionNoAsync(c => clientesIds.Contains(c.IDCliente))
                    .ToDictionary(c => c.IDCliente)
                : new Dictionary<int, Clientes>();

            foreach (var factura in facturas)
            {
                var detalles = _FacturaDetalles.GetAllByExpresionNoAsync(
                    d => d.IdFacturaHeader == factura.IdFacturaHeader
                );

                if (detalles == null || !detalles.Any())
                    continue;

                foreach (var item in detalles)
                {
                    var producto = _Productos.GetById(item.IdProducto);
                    if (producto == null) continue;



                    if (item.IdEmpleadoComision == null) continue;

                    var empleado = _Empleados.GetByExpresion(
                        e => e.IdEmpleados == item.IdEmpleadoComision
                    );
                    if (empleado == null) continue;

                    var comisionConfig = _IEmpleadoComision
                        .GetByExpresion(c =>
                            c.IdEmpleado == empleado.IdEmpleados &&
                            c.IdArea == producto.IdArea
                        );

                    if (comisionConfig == null) continue;

                    decimal totalComision = 0;

                    if (comisionConfig.TipoComision == "MONTO")
                    {
                        totalComision =
                            (comisionConfig.MontoComision ?? 0) * item.Cantidad;
                    }
                    else
                    {
                        decimal porciento =
                            comisionConfig.PorcientoComision ?? 0;

                        totalComision =
                            item.SubTotal * porciento / 100;
                    }

                    result.Add(new ServicioEmpleadoDto
                    {
                        NoFactura = "0000" + factura.IdFacturaHeader,
                        Fecha = factura.FechaInseccion,
                        Cliente = ObtenerNombreCliente(factura, clientesPorId),
                        Hora = factura.Hora,

                        TipoComision = comisionConfig.TipoComision,
                        IdEmpleado = empleado.IdEmpleados,
                        Empleado = empleado.Nombre,

                        TotalFactura = factura.Total,
                        PagadoFactura = factura.Pagado,
                        PendienteFactura = factura.Total - factura.Pagado,
                        TipoFactura = factura.TipoFactura,

                        IdProducto = producto.IdProducto,
                        Producto = producto.Nombre,

                        Cantidad = item.Cantidad,
                        Precio = producto.PrecioVenta,

                        SubTotal = item.SubTotal,
                        PorcientoComision =
                            comisionConfig.PorcientoComision ?? 0,

                        Comision = totalComision
                    });
                }
            }
            var Totalver = result.Sum(c => c.SubTotal);

            return result;
        }

        private static string ObtenerNombreCliente(
            FacturaHeaders factura,
            Dictionary<int, Clientes> clientesPorId)
        {
            if (!string.IsNullOrWhiteSpace(factura.NombreCuenta))
            {
                return factura.NombreCuenta.Trim();
            }

            if (factura.IDCliente.HasValue
                && factura.IDCliente > 0
                && clientesPorId.TryGetValue(factura.IDCliente.Value, out var cliente)
                && !string.IsNullOrWhiteSpace(cliente.NombreComercial))
            {
                return cliente.NombreComercial.Trim();
            }

            return "Consumidor Final";
        }

        public async Task<IEnumerable<ServicioRankingDto>> GetTopServiciosDelMes(int IdEmpresa)
        {
            var mesActual = DateTime.Now.Month;
            var añoActual = DateTime.Now.Year;

            // 🔹 Paso 1: Traer las facturas del mes actual
            var facturas = await _repository.GetAllByExpresionAsync(f =>
                f.IdEmpresa == IdEmpresa &&
                f.FechaInseccion.Month == mesActual &&
                f.FechaInseccion.Year == añoActual &&
                f.TipoFactura == "Contado" &&
                f.IdTipoDocumentos == 1 &&
                f.EstaCancelada == false
            );

            if (facturas == null || !facturas.Any())
                return new List<ServicioRankingDto>();

            // 🔹 Paso 2: Obtener todos los IDs de factura
            var facturasIds = facturas.Select(f => f.IdFacturaHeader).ToList();

            // 🔹 Paso 3: Obtener los detalles correspondientes a esas facturas
            var detalles = await _FacturaDetalles.GetAllByExpresionAsync(d => facturasIds.Contains(d.IdFacturaHeader));

            if (detalles == null || !detalles.Any())
                return new List<ServicioRankingDto>();

            // 🔹 Paso 4: Obtener los productos asociados a los detalles
            var productosIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var productos = await _Productos.GetAllByExpresionAsync(p => productosIds.Contains(p.IdProducto));

            // 🔹 Paso 5: Agrupar los servicios vendidos por producto
            var ranking = detalles
    .GroupBy(d => d.IdProducto)
    .Select(g =>
    {
        var producto = productos.FirstOrDefault(p => p.IdProducto == g.Key);

        return new ServicioRankingDto
        {
            NombreServicio = producto?.Nombre ?? "Desconocido",

            // 🔥 Total de unidades vendidas
            Veces = (int)g.Sum(x => x.Cantidad),

            // 🔥 Total facturado
            TotalFacturado = g.Sum(x => x.SubTotal)
        };
    })
    .OrderByDescending(x => x.Veces)
    .Take(5)
    .ToList();

            return ranking;
        }






        public async Task<IEnumerable<CuentaPorCobrarDto>> GetCuentasPorCobrar(int IdEmpresa)
        {
            // 🔹 Traer facturas pendientes de crédito
            var facturasPendientes = await _repository.GetAllByExpresionAsync(f =>
                f.IdEmpresa == IdEmpresa &&
                f.TipoFactura == "Credito" &&
                f.Estado == "Pendiente" &&
                f.EstaCancelada == false &&
                f.IdTipoDocumentos == 1
            );

            if (facturasPendientes == null || !facturasPendientes.Any())
                return new List<CuentaPorCobrarDto>();

            // 🔹 Obtenemos los Ids únicos de clientes que tienen facturas pendientes
            var clientesIds = facturasPendientes
                .Select(f => f.IDCliente)
                .Distinct()
                .ToList();

            // 🔹 Consultamos solo los clientes involucrados
            var clientes = await _Clientes.GetAllByExpresionAsync(c => clientesIds.Contains(c.IDCliente));

            // 🔹 Agrupamos las facturas por cliente y calculamos la deuda
            var cuentas = facturasPendientes
                .GroupBy(f => f.IdEmpleadoConsumo is > 0
                    ? $"E:{f.IdEmpleadoConsumo}"
                    : $"C:{f.IDCliente ?? 0}")
                .Select(g =>
                {
                    var sample = g.First();
                    if (sample.IdEmpleadoConsumo is > 0)
                    {
                        return new CuentaPorCobrarDto
                        {
                            IdCliente = 0,
                            IdEmpleados = sample.IdEmpleadoConsumo,
                            EsEmpleado = true,
                            NombreCliente = string.IsNullOrWhiteSpace(sample.NombreCuenta)
                                ? (sample.NombreEmpresa ?? "Colaborador")
                                : sample.NombreCuenta,
                            Telefono = "—",
                            TotalDeuda = g.Sum(x => x.Pendiente)
                        };
                    }

                    var cliente = clientes.FirstOrDefault(c => c.IDCliente == sample.IDCliente);
                    return new CuentaPorCobrarDto
                    {
                        IdCliente = sample.IDCliente ?? 0,
                        NombreCliente = cliente?.NombreComercial ?? "Consumidor Final",
                        Telefono = cliente?.Telefono ?? "—",
                        TotalDeuda = g.Sum(x => x.Pendiente)
                    };
                })
                .OrderByDescending(x => x.TotalDeuda)
                .ToList();

            return cuentas;
        }

        public async Task MarcarFacturaClienteImpresa(int idFactura)
        {
            var factura = _repository.GetById(idFactura);

            if (factura == null)
                return;

            factura.PrintPending = false;

            _repository.Update(factura.IdFacturaHeader, factura);
        }
        public async Task<List<TicketLavadorDto>> GetTicketsLavadorByFactura(int idFacturaHeader)
        {
            var result = new List<TicketLavadorDto>();

            // ⭐ traer solo la factura necesaria
            var factura = await _repository.GetByIdAsync(idFacturaHeader);
            if (factura == null || factura.EstaCancelada)
                return result;

            // ⭐ traer solo detalles pendientes de esa factura
            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(d =>
                    d.IdFacturaHeader == idFacturaHeader &&

                    d.IdEmpleadoComision != null
                )
                .ToList();

            if (!detalles.Any())
                return result;

            // ⭐ traer productos involucrados
            var productosIds = detalles.Select(x => x.IdProducto).Distinct().ToList();
            var productos = _Productos
                .GetAllByExpresionNoAsync(p => productosIds.Contains(p.IdProducto) && p.EsServicio == true)
                .ToList();

            if (!productos.Any())
                return result;

            // ⭐ filtrar solo servicios
            detalles = detalles
                .Where(d => productos.Any(p => p.IdProducto == d.IdProducto))
                .ToList();

            if (!detalles.Any())
                return result;

            // ⭐ traer empleados
            var empleadosIds = detalles.Select(x => x.IdEmpleadoComision.Value).Distinct().ToList();
            var empleados = _Empleados
                .GetAllByExpresionNoAsync(e => empleadosIds.Contains(e.IdEmpleados))
                .ToList();

            // ⭐ traer cliente
            Clientes cliente = null;
            if (factura.IDCliente != null)
            {
                cliente = _Clientes
                    .GetAllByExpresionNoAsync(c => c.IDCliente == factura.IDCliente)
                    .FirstOrDefault();
            }

            // ⭐ agrupar por lavador
            var gruposLavador = detalles.GroupBy(x => x.IdEmpleadoComision);

            foreach (var grupo in gruposLavador)
            {
                var empleado = empleados.FirstOrDefault(e => e.IdEmpleados == grupo.Key);

                var dto = new TicketLavadorDto
                {
                    NumeroFactura = factura.IdFacturaHeader,
                    Fecha = factura.FechaInseccion,
                    Cliente = cliente?.NombreComercial ?? "Consumidor Final",
                    AtendidoPor = empleado?.Nombre ?? "—",
                    Caja = "Caja 1",
                    Servicios = grupo.Select(det =>
                    {
                        var producto = productos.First(p => p.IdProducto == det.IdProducto);

                        return new TicketLavadorDetalleDto
                        {
                            Cantidad = (int)det.Cantidad,
                            Servicio = producto.Nombre,
                            Precio = det.SubTotal
                        };
                    }).ToList()
                };

                result.Add(dto);
            }

            return result;
        }

        public async Task<CotizacionPublicaLinkDto?> CrearLinkCotizacionPublicaAsync(
            int idFacturaHeader,
            int idEmpresa)
        {
            var factura = await _repository.GetByExpresionAsync(
                c => c.IdFacturaHeader == idFacturaHeader
                     && c.IdEmpresa == idEmpresa
                     && c.IdTipoDocumentos == 2
                     && !c.EstaCancelada);

            if (factura == null)
                return null;

            return new CotizacionPublicaLinkDto
            {
                Token = CotizacionShareToken.Create(idEmpresa, idFacturaHeader),
                IdFacturaHeader = idFacturaHeader,
                NumeroDocumento = string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                    ? idFacturaHeader.ToString()
                    : factura.NumeroDocumento.Trim()
            };
        }

        public async Task<CotizacionPublicaDto?> ObtenerCotizacionPublicaAsync(string token)
        {
            if (!CotizacionShareToken.TryParse(token, out var idEmpresa, out var idFacturaHeader))
                return null;

            var factura = await _repository.GetByExpresionAsync(
                c => c.IdFacturaHeader == idFacturaHeader
                     && c.IdEmpresa == idEmpresa
                     && c.IdTipoDocumentos == 2
                     && !c.EstaCancelada);

            if (factura == null)
                return null;

            var empresa = _Empresas.GetById(idEmpresa);

            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(d => d.IdFacturaHeader == idFacturaHeader)
                .ToList();

            var productosIds = detalles.Select(x => x.IdProducto).Distinct().ToList();
            var productos = productosIds.Count == 0
                ? new List<Productos>()
                : _Productos
                    .GetAllByExpresionNoAsync(p => productosIds.Contains(p.IdProducto))
                    .ToList();

            Clientes? cliente = null;
            if (factura.IDCliente != null)
            {
                cliente = _Clientes
                    .GetAllByExpresionNoAsync(c => c.IDCliente == factura.IDCliente)
                    .FirstOrDefault();
            }

            var fecha = factura.FechaInseccion;
            var validez = fecha.AddDays(15);

            return new CotizacionPublicaDto
            {
                NumeroDocumento = string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                    ? idFacturaHeader.ToString()
                    : factura.NumeroDocumento.Trim(),
                Fecha = fecha,
                FechaValidez = validez,
                NombreEmpresa = empresa?.NombreComercial ?? factura.NombreEmpresa ?? "",
                TelefonoEmpresa = empresa?.Telefono,
                DireccionEmpresa = empresa?.Direccion,
                LogoEmpresa = null,
                RncEmpresa = empresa?.RNC,
                ClienteNombre = cliente?.NombreComercial
                    ?? (string.IsNullOrWhiteSpace(factura.NombreCuenta) ? "Cliente" : factura.NombreCuenta),
                ClienteTelefono = PrimeroNoVacio(cliente?.Celular, cliente?.Telefono),
                ClienteRnc = PrimeroNoVacio(cliente?.CedulaRNC, factura.RNC),
                ClienteDireccion = PrimeroNoVacio(cliente?.Direccion),
                ClienteCorreo = PrimeroNoVacio(cliente?.Email),
                SubTotal = factura.SubTotal,
                TotalItbis = factura.TotalItbis,
                TotalDescuento = factura.TotalDescuento,
                Total = factura.Total,
                Nota = factura.Nota,
                Moneda = string.IsNullOrWhiteSpace(factura.Moneda) ? "DOP" : factura.Moneda,
                Lineas = detalles.Select(det =>
                {
                    var prod = productos.FirstOrDefault(p => p.IdProducto == det.IdProducto);
                    var precio = det.PrecioOferta > 0 ? det.PrecioOferta : det.SubTotal;
                    return new CotizacionPublicaLineaDto
                    {
                        Cantidad = det.Cantidad,
                        Descripcion = prod?.Nombre ?? prod?.Descripcion ?? "Producto",
                        PrecioUnitario = precio,
                        SubTotal = det.SubTotal > 0
                            ? det.SubTotal
                            : Math.Round(precio * det.Cantidad, 2)
                    };
                }).ToList()
            };
        }

        public async Task<TicketFacturaClienteDto?> GetFacturaClienteById(int idFacturaHeader)
        {
            // ⭐ FACTURA
            var factura = await _repository.GetByIdAsync(idFacturaHeader);

            if (factura == null || factura.EstaCancelada)
                return null;

            // ⭐ EMPRESA
            var empresa = _Empresas.GetById(factura.IdEmpresa);

            // ⭐ DETALLES
            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(d => d.IdFacturaHeader == idFacturaHeader)
                .ToList();

            if (!detalles.Any())
                return null;

            // ⭐ PRODUCTOS
            var productosIds = detalles.Select(x => x.IdProducto).Distinct().ToList();

            var productos = _Productos
                .GetAllByExpresionNoAsync(p => productosIds.Contains(p.IdProducto))
                .ToList();

            // ⭐ CLIENTE
            Clientes? cliente = null;
            if (factura.IDCliente != null)
            {
                cliente = _Clientes
                    .GetAllByExpresionNoAsync(c => c.IDCliente == factura.IDCliente)
                    .FirstOrDefault();
            }

            // ⭐ e-CF DGII (si existe) — TrackId no se expone al ticket
            var ecf = _EcfEncabezados
                .GetAllByExpresionNoAsync(e =>
                    e.IdEmpresa == factura.IdEmpresa
                    && (
                        (e.IdOrigen == idFacturaHeader
                            && (e.OrigenDocumento == (int)OrigenDocumento.Pos
                                || e.OrigenDocumento == (int)OrigenDocumento.Facturacion))
                        || e.IdFacturaInterna == idFacturaHeader
                        || (!string.IsNullOrWhiteSpace(factura.NCF) && e.ENCF == factura.NCF)
                    ))
                .OrderByDescending(e => e.IdECF)
                .FirstOrDefault();

            var ncf = !string.IsNullOrWhiteSpace(ecf?.ENCF) ? ecf!.ENCF : (factura.NCF ?? "");
            var esElectronico = !string.IsNullOrWhiteSpace(ncf)
                && ncf.StartsWith("E", StringComparison.OrdinalIgnoreCase);

            // ⭐ ARMAR DTO
            var dto = new TicketFacturaClienteDto
            {
                NumeroFactura = factura.IdFacturaHeader,
                NumeroDocumento = factura.NumeroDocumento ?? "",
                Fecha = factura.FechaInseccion,
                Hora = factura.Hora ?? "",
                Cliente = !string.IsNullOrWhiteSpace(cliente?.NombreComercial)
                    ? cliente!.NombreComercial!
                    : !string.IsNullOrWhiteSpace(factura.NombreEmpresa)
                        ? factura.NombreEmpresa!
                        : "Consumidor Final",
                RncCliente = !string.IsNullOrWhiteSpace(cliente?.CedulaRNC)
                    ? cliente!.CedulaRNC
                    : factura.RNC,
                SubTotal = factura.SubTotal,
                TotalItbis = factura.TotalItbis,
                TotalDescuento = factura.TotalDescuento,
                Total = factura.Total,
                Pagado = factura.Pagado,
                Pendiente = factura.Pendiente,
                TipoFactura = factura.TipoFactura ?? "",
                FormaPago = factura.FormaPago ?? "",

                NombreEmpresa = empresa?.NombreComercial ?? "",
                TelefonoEmpresa = empresa?.Telefono ?? "",
                DireccionEmpresa = empresa?.Direccion ?? "",
                RncEmpresa = empresa?.RNC,

                NCF = ncf,
                TipoComprobante = factura.TipoFactura,
                EsComprobanteElectronico = esElectronico || ecf != null,
                TipoECF = ecf?.TipoECF,
                SecurityCode = ecf?.SecurityCode,
                UrlQR = ecf?.UrlQR,
                FechaFirma = ecf?.FechaFirma,
                FechaEmisionEcf = ecf?.FechaEmision,
                EstadoDgii = ecf?.EstadoDGII,

                Detalles = detalles.Select(det =>
                {
                    var prod = productos.FirstOrDefault(p => p.IdProducto == det.IdProducto);

                    return new TicketFacturaClienteDetalleDto
                    {
                        Cantidad = det.Cantidad,
                        Descripcion = prod?.Nombre ?? $"Producto {det.IdProducto}",
                        Precio = det.SubTotal
                    };
                }).ToList()
            };

            var pagosIng = _Ingresos
                .GetAllByExpresionNoAsync(i =>
                    i.IdFacturaHeader == idFacturaHeader && !i.EstaAnulado)
                ?.Where(i => i.Monto > 0 && !string.IsNullOrWhiteSpace(i.FormaPago))
                .GroupBy(i => i.FormaPago.Trim())
                .Select(g => new TicketFacturaClientePagoDto
                {
                    Metodo = g.Key,
                    Monto = g.Sum(x => x.Monto)
                })
                .ToList() ?? new List<TicketFacturaClientePagoDto>();

            dto.Pagos = pagosIng;
            if (string.IsNullOrWhiteSpace(dto.FormaPago))
            {
                dto.FormaPago = pagosIng.Count == 1
                    ? pagosIng[0].Metodo
                    : pagosIng.Count > 1 ? "Mixto" : "";
            }

            // Invoice guarda data:image truncada en UrlQR (NVARCHAR 500).
            // Reconstruir ConsultaTimbre para que la térmica pueda imprimir el QR.
            // Nunca tumbar el ticket si falla el armado del QR.
            try
            {
                if (dto.EsComprobanteElectronico
                    && !string.IsNullOrWhiteSpace(dto.SecurityCode)
                    && !EcfQrUrlHelper.IsUsableHttpUrl(dto.UrlQR))
                {
                    dto.UrlQR = EcfQrUrlHelper.ResolveFromEncabezadoFields(
                        dto.UrlQR,
                        empresa?.AmbienteFE,
                        dto.RncEmpresa ?? "",
                        dto.RncCliente,
                        dto.NCF ?? "",
                        dto.FechaEmisionEcf ?? dto.Fecha,
                        dto.Total,
                        dto.FechaFirma,
                        dto.SecurityCode);
                }
            }
            catch
            {
                // Dejar UrlQR original; el PrinterApi puede reconstruir en impresión.
            }

            // QR ya armado con la marca de DGII. En el ticket, completar 00:00 con Hora de la factura.
            dto.FechaFirma = TicketFechaHora.ParaImpresion(
                dto.FechaFirma,
                dto.FechaEmisionEcf,
                dto.Fecha,
                dto.Hora);

            return dto;
        }

        public async Task
        CerrarFacturasPendientes(

            int idEmpresa,

            int idUsuario,

            int idCajaCierre
        )
        {
            try
            {
                /* =====================================
                🔥 FACTURAS ABIERTAS
                ====================================== */

                var facturas =

                    await _repository
                    .GetAllByExpresionAsync(

                        x =>

                            x.IdEmpresa
                            == idEmpresa

                            &&

                            x.IdUsuario
                            == idUsuario

                            &&

                            x.EstaCerrada
                            == false

                            &&

                            x.IdTipoDocumentos
                            == 1
                    );

                if (
                    facturas == null
                    ||
                    !facturas.Any()
                )
                {
                    return;
                }

                /* =====================================
                🔥 CERRAR
                ====================================== */

                foreach (var factura in facturas)
                {
                    try
                    {
                        factura.Clientes = null;
                        factura.EstaCerrada =
                            true;

                        factura.IdCajaCierre =
                            idCajaCierre;

                        _repository.Update(

                            factura.IdFacturaHeader,

                            factura
                        );
                    }
                    catch (Exception exFactura)
                    {
                        Console.WriteLine(

                            "ERROR FACTURA: "
                            +
                            factura.IdFacturaHeader
                        );

                        Console.WriteLine(
                            exFactura.Message
                        );

                        Console.WriteLine(
                            exFactura.InnerException?.Message
                        );

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "ERROR CERRANDO FACTURAS"
                );

                Console.WriteLine(
                    ex.Message
                );

                Console.WriteLine(
                    ex.InnerException?.Message
                );

                throw;
            }
        }
        public async Task<List<CierreCajaDto>> GetIngresosCajaAbierta(
     int idEmpresa,
     int idUsuario)
        {
            // =====================================
            // 🔥 FACTURAS PENDIENTES DE CIERRE
            // =====================================

            var facturas = await _repository
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa

                    && x.IdUsuario == idUsuario

                    && x.IdTipoDocumentos == 1

                    && x.EstaCerrada == false

                    && x.EstaCancelada == false
                );

            if (facturas == null || !facturas.Any())
                return new();

            // =====================================
            // 🔥 TOTALES FACTURAS
            // =====================================

            decimal totalVentasBrutas =
                facturas.Sum(x => x.Total);

            decimal totalDescuento =
                facturas.Sum(x => x.TotalDescuento);

            var idsFacturas = facturas
                .Select(x => x.IdFacturaHeader)
                .ToList();

            // =====================================
            // 🔥 INGRESOS DE FACTURAS
            // =====================================

            var ingresos = await _Ingresos
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa

                    && idsFacturas.Contains((int)x.IdFacturaHeader)

                    && x.EstaAnulado == false
                );

            // =====================================
            // 🔥 INGRESOS EXTRAORDINARIOS
            // =====================================

            var ingresosExtra = await _Ingresos
      .GetAllByExpresionAsync(x =>

          x.IdEmpresa == idEmpresa

          && x.IdUsuario == idUsuario

          && (
              x.IdFacturaHeader == null
              ||
              x.IdFacturaHeader == 0
          )

          && x.EstaAnulado == false
      );

            decimal totalIngresosExtra =
                ingresosExtra.Sum(x => x.Monto);

            // =====================================
            // 🔥 UNIR TODOS LOS INGRESOS
            // =====================================

            var todosLosIngresos =
                ingresos
                .Concat(ingresosExtra)
                .ToList();

            // Forma de pago efectiva: reclasificaciones ANTES_CIERRE (caja aún abierta).
            // No muta Ingresos históricos; solo la proyección del cierre en curso.
            var metodosEfectivos = await _reclasificacionService
                .ObtenerMetodosEfectivosAntesCierreAsync(
                    idEmpresa,
                    todosLosIngresos.Select(i => i.IdIngreso));

            // =====================================
            // 🔥 GASTOS PAGADOS EN EFECTIVO
            // =====================================

            var gastos = await _GastosRepository
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa

                    && x.IdUsuario == idUsuario

                    && x.EstaAnulado == false

                    && x.EstaCerrada != true

                    && x.FormaPago != null

                    && x.FormaPago.ToUpper() == "EFECTIVO"
                );

            decimal totalGastos =
                gastos.Sum(x => x.Monto);

            // =====================================
            // 🔥 INGRESOS NETOS
            // =====================================

            decimal ingresosNetos =

                totalVentasBrutas

                + totalIngresosExtra

                - totalDescuento

                - totalGastos;

            // =====================================
            // 🔥 AGRUPAR POR MÉTODO DE PAGO (efectivo si hay reclasificación)
            // =====================================

            var resultado = todosLosIngresos
                .GroupBy(x => metodosEfectivos.TryGetValue(x.IdIngreso, out var m) ? m : x.FormaPago)
                .Select(g => new CierreCajaDto
                {
                    FormaPago = g.Key,

                    Total = g.Sum(x => x.Monto),

                    TotalVentasBrutas = totalVentasBrutas,

                    TotalDescuento = totalDescuento,

                    TotalGastos = totalGastos,

                    TotalIngresosExtra = totalIngresosExtra,

                    TotalIngresosNetos = ingresosNetos
                })
                .OrderBy(x => x.FormaPago)
                .ToList();

            return resultado;
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllOrdenesByFecha(
        int IdEmpresa,
        DateTime fechaDesde,
        DateTime fechaHasta)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                 c.IdTipoDocumentos == 1
                 && c.IdEmpresa == IdEmpresa
                 && c.FechaInseccion.Date >= fechaDesde.Date
                 && c.FechaInseccion.Date <= fechaHasta.Date,
                 "Clientes",
                 "FacturaDetalles");
        }

        public async Task<
    List<CajaProductoDto>>
    GetProductosPendientesCierre(

        int idEmpresa,

        int idUsuario
        
    )
        {
            // =========================================
            // 🔥 FACTURAS ABIERTAS
            // =========================================

            var facturas =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        == idEmpresa

                        &&

                        x.IdUsuario
                        == idUsuario

                        &&

                        x.IdTipoDocumentos
                        == 1

                        &&
                        x.EstaCerrada==false
                );

            if (
                facturas == null
                ||
                !facturas.Any()
            )
            {
                return new();
            }

            // =========================================
            // 🔥 IDS FACTURAS
            // =========================================

            var idsFacturas =
                facturas
                .Select(x =>
                    x.IdFacturaHeader
                )
                .ToList();

            // =========================================
            // 🔥 DETALLES
            // =========================================

            var detalles =
                _FacturaDetalles
                .GetAllByExpresionNoAsync(

                    x =>

                        idsFacturas
                        .Contains(
                            x.IdFacturaHeader
                        )
                )
                .ToList();

            if (
                !detalles.Any()
            )
            {
                return new();
            }

            // =========================================
            // 🔥 PRODUCTOS
            // =========================================

            var productosIds =
                detalles
                .Select(x =>
                    x.IdProducto
                )
                .Distinct()
                .ToList();

            var productos =
                _Productos
                .GetAllByExpresionNoAsync(

                    x =>

                        productosIds
                        .Contains(
                            x.IdProducto
                        )
                )
                .ToList();

            // =========================================
            // 🔥 AGRUPAR
            // =========================================

            var result =
                detalles

                .GroupBy(x =>
                    x.IdProducto
                )

                .Select(g =>
                {
                    var producto =
                        productos
                        .FirstOrDefault(

                            p =>

                                p.IdProducto
                                == g.Key
                        );

                    return new CajaProductoDto
                    {
                        IdProducto =
                            g.Key,

                        Producto =
                            producto?.Nombre
                            ?? "",

                        CantidadVendida =
                            g.Sum(x =>
                                x.Cantidad
                            ),

                        TotalVendido =
                            g.Sum(x =>
                                x.SubTotal
                            ),

                        ExistenciaActual =
                            producto?.Cantidad
                            ?? 0
                    };
                })

                .OrderByDescending(x =>
                    x.CantidadVendida
                )

                .ToList();

            return result;
        }


        public async Task<List<CajaProductoDto>>
        GetProductosPorCajaCierre(

            int idEmpresa,

            int idUsuario,

            int idCajaCierre
        )
        {
            /* =====================================
            🔥 FACTURAS DEL CIERRE
            ====================================== */

            var facturas =

                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa == idEmpresa

                        &&

                        x.IdUsuario == idUsuario

                        &&

                        x.IdCajaCierre == idCajaCierre

                        &&

                        x.EstaCerrada == true

                        &&

                        x.IdTipoDocumentos == 1
                );

            if (
                facturas == null
                ||
                !facturas.Any()
            )
            {
                return new();
            }

            /* =====================================
            🔥 IDS FACTURAS
            ====================================== */

            var idsFacturas =

                facturas
                .Select(x =>

                    x.IdFacturaHeader
                )
                .ToList();

            /* =====================================
            🔥 DETALLES
            ====================================== */

            var detalles =

                _FacturaDetalles
                .GetAllByExpresionNoAsync(

                    x =>

                        idsFacturas
                        .Contains(

                            x.IdFacturaHeader
                        )
                )
                .ToList();

            if (
                !detalles.Any()
            )
            {
                return new();
            }

            /* =====================================
            🔥 IDS PRODUCTOS
            ====================================== */

            var productosIds =

                detalles
                .Select(x =>

                    x.IdProducto
                )
                .Distinct()
                .ToList();

            /* =====================================
            🔥 PRODUCTOS
            ====================================== */

            var productos =

                _Productos
                .GetAllByExpresionNoAsync(

                    x =>

                        productosIds
                        .Contains(

                            x.IdProducto
                        )
                )
                .ToList();

            /* =====================================
            🔥 AGRUPAR
            ====================================== */

            var result =

                detalles

                .GroupBy(x =>

                    x.IdProducto
                )

                .Select(g =>
                {
                    var producto =

                        productos
                        .FirstOrDefault(

                            p =>

                                p.IdProducto
                                == g.Key
                        );

                    return new CajaProductoDto
                    {
                        IdProducto =
                            g.Key,

                        Producto =
                            producto?.Nombre
                            ?? "",

                        CantidadVendida =

                            g.Sum(x =>

                                x.Cantidad
                            ),

                        TotalVendido =

                            g.Sum(x =>

                                x.SubTotal
                            ),

                        ExistenciaActual =

                            producto?.Cantidad
                            ?? 0
                    };
                })

                .OrderByDescending(x =>

                    x.CantidadVendida
                )

                .ToList();

            return result;
        }

        private static string? PrimeroNoVacio(params string?[] valores)
        {
            foreach (var v in valores)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return null;
        }
    }
}

