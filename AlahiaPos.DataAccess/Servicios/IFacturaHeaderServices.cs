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
        IRepository<LavadorConsumo> _ILavadorConsumo;
        public IFacturaHeaderServices(IRepository<FacturaHeaders> repository,
            IRepository<Empleados> Empleados, IRepository<Productos> Productos,
            IRepository<FacturaDetalles> facturaDetalles, IRepository<EmpleadoAreaComision> 
            iEmpleadoComision, IRepository<Clientes> clientes, IRepository<Empresas> empresas, IRepository<LavadorConsumo> LavadorConsumo)
        {
            _repository = repository;
            _Empleados = Empleados;
            _Productos = Productos;
            _FacturaDetalles = facturaDetalles;
            _IEmpleadoComision = iEmpleadoComision;
            _Clientes = clientes;
            _Empresas = empresas;
            _ILavadorConsumo = LavadorConsumo;
        }


        public FacturaHeaders GetById(int Id)
        {
            return _repository.GetById(Id);
        }
        public async Task<IEnumerable<FacturaHeaders>> GetAllOrdenes(int IdEmpresa)
        {
            DateTime _Fecha = System.DateTime.Now.Date;
            return await _repository.GetAllByExpresionAsync(c =>
             c.IdTipoDocumentos == 10 && c.IdEmpresa == IdEmpresa
             && c.FechaInseccion.Date == _Fecha.Date,
                 "Clientes", "FacturaDetalles");
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
            await _repository.Save(FacturaHeader);
        }

        public async void UpdateFacturaHeader(int Id, FacturaHeaders FacturaHeader)
        {

            FacturaHeader.IdEmpleadoComision = 1;



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
        public IEnumerable<ComisionesResultDto> GetComisionesDetalle(DateTime Desde, DateTime Hasta, int IdEmpresa)
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

            var idsFacturas = facturas.Select(f => f.IdFacturaHeader).ToList();

            // 🔵 2️⃣ DETALLES EN BLOQUE
            var detalles = _FacturaDetalles
                .GetAllByExpresionNoAsync(d => idsFacturas.Contains(d.IdFacturaHeader))
                .ToList();

            if (!detalles.Any())
                return new List<ComisionesResultDto>();

            // 🔵 3️⃣ PRODUCTOS EN BLOQUE
            var idsProductos = detalles.Select(d => d.IdProducto).Distinct().ToList();

            var productos = _Productos
                .GetAllByExpresionNoAsync(p => idsProductos.Contains(p.IdProducto))
                .ToDictionary(p => p.IdProducto);

            // 🔵 4️⃣ EMPLEADOS EN BLOQUE
            var idsEmpleados = detalles
                .Select(d => d.IdEmpleadoComision)
                .Distinct()
                .ToList();

            var empleados = _Empleados
                .GetAllByExpresionNoAsync(e => idsEmpleados.Contains(e.IdEmpleados))
                .ToDictionary(e => e.IdEmpleados);

            // 🔵 5️⃣ COMISIONES EN BLOQUE
            var comisionesAreas = _IEmpleadoComision
                .GetAllByExpresionNoAsync(c => c.IdEmpresa == IdEmpresa)
                .ToList();

            // 🔵 6️⃣ CONSUMOS EN BLOQUE
            var consumos = _ILavadorConsumo
                .GetAllByExpresionNoAsync(c =>
                    c.Fecha.Date >= Desde.Date &&
                    c.Fecha.Date <= Hasta.Date &&
                    c.IdEmpresa == IdEmpresa
                )
                .GroupBy(c => c.IdEmpleado)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

            // 🔵 7️⃣ RECORRIDO PRINCIPAL (YA TODO EN MEMORIA)
            foreach (var item in detalles)
            {
                if (!productos.ContainsKey(item.IdProducto))
                    continue;

                if (!empleados.ContainsKey((int)item.IdEmpleadoComision))
                    continue;

                var producto = productos[item.IdProducto];
                var empleado = empleados[(int)item.IdEmpleadoComision];

                var factura = facturas.First(f => f.IdFacturaHeader == item.IdFacturaHeader);

                var comisionArea = comisionesAreas
                    .FirstOrDefault(c => c.IdEmpleado == empleado.IdEmpleados &&
                                         c.IdArea == producto.IdArea);

                if (comisionArea == null)
                    continue;

                decimal proporcionPagada = 1;

                if (factura.TipoFactura == "Credito")
                {
                    if (factura.Total <= 0)
                        continue;

                    proporcionPagada = factura.Pagado / factura.Total;
                    if (proporcionPagada > 1) proporcionPagada = 1;
                }

                decimal baseParaComision = factura.TipoFactura == "Credito"
                    ? item.SubTotal * proporcionPagada
                    : item.SubTotal;

                if (baseParaComision <= 0)
                    continue;

                decimal totalComision = 0;

                if (comisionArea.TipoComision == "MONTO")
                {
                    totalComision = (comisionArea.MontoComision ?? 0) * item.Cantidad;
                }
                else
                {
                    decimal porciento = comisionArea.PorcientoComision ?? 0;
                    if (porciento <= 0) continue;

                    totalComision = baseParaComision * porciento / 100;
                }

                if (totalComision <= 0)
                    continue;

                listado.Add(new ComisionesDto
                {
                    IdEmpleado = empleado.IdEmpleados,
                    Nombre = empleado.Nombre,
                    Fecha = factura.FechaInseccion,
                    ProductoServicio = producto.Nombre,
                    Total = baseParaComision,
                    TotalComisiones = totalComision
                });
            }

            // 🔵 8️⃣ AGRUPAR RESULTADO FINAL
            var resultado = listado
                .GroupBy(l => new { l.IdEmpleado, l.Nombre })
                .Select(g =>
                {
                    var totalComision = g.Sum(x => x.TotalComisiones);

                    var consumo = consumos.ContainsKey(g.Key.IdEmpleado)
                        ? consumos[g.Key.IdEmpleado]
                        : 0;

                    return new ComisionesResultDto
                    {
                        IdEmpleado = g.Key.IdEmpleado,
                        Empleados = g.Key.Nombre,
                        TotalComisiones = totalComision,
                        TotalConsumo = consumo,
                        NetoPagar = totalComision - consumo < 0 ? 0 : totalComision - consumo
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
                        Cliente = factura.NombreCuenta ?? "Consumidor Final",
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
            var Totalver=result.Sum(c=>c.SubTotal);

            return result;
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
                        Veces = g.Count(),
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
                .GroupBy(f => f.IDCliente)
                .Select(g =>
                {
                    var cliente = clientes.FirstOrDefault(c => c.IDCliente == g.Key);

                    return new CuentaPorCobrarDto
                    {
                        IdCliente = (int)g.Key,
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

            // ⭐ ARMAR DTO
            var dto = new TicketFacturaClienteDto
            {
                NumeroFactura = factura.IdFacturaHeader,
                Fecha = factura.FechaInseccion,
                Hora = factura.Hora,
                Cliente = cliente?.NombreComercial ?? "Consumidor Final",
                Total = factura.Total,

                NombreEmpresa = empresa?.NombreComercial ?? "",
                TelefonoEmpresa = empresa?.Telefono ?? "",
                DireccionEmpresa = empresa?.Direccion ?? "",

                Detalles = detalles.Select(det =>
                {
                    var prod = productos.First(p => p.IdProducto == det.IdProducto);

                    return new TicketFacturaClienteDetalleDto
                    {
                        Cantidad = det.Cantidad,
                        Descripcion = prod.Nombre,
                        Precio = det.SubTotal
                    };
                }).ToList()
            };

            return dto;
        }
    } 
}

