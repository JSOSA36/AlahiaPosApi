using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AntiguedadSaldosService : IAntiguedadSaldosService
    {
        private const int TipoDocumentoVenta = 1;
        private const int TipoDocumentoFacturaCompra = 11;

        private readonly IRepository<FacturaHeaders> _facturaRepo;
        private readonly IRepository<OrdenCompraHeader> _compraRepo;
        private readonly IRepository<Clientes> _clientesRepo;
        private readonly IRepository<Proveedores> _proveedoresRepo;
        private readonly IRepository<Empresas> _empresaRepo;

        public AntiguedadSaldosService(
            IRepository<FacturaHeaders> facturaRepo,
            IRepository<OrdenCompraHeader> compraRepo,
            IRepository<Clientes> clientesRepo,
            IRepository<Proveedores> proveedoresRepo,
            IRepository<Empresas> empresaRepo)
        {
            _facturaRepo = facturaRepo;
            _compraRepo = compraRepo;
            _clientesRepo = clientesRepo;
            _proveedoresRepo = proveedoresRepo;
            _empresaRepo = empresaRepo;
        }

        public async Task<AntiguedadSaldosReporteDto> ObtenerAntiguedadCxCAsync(AntiguedadSaldosFiltroRequest filtro)
        {
            ValidarFiltro(filtro);
            var corte = (filtro.FechaCorte ?? DateTime.Today).Date;

            var facturas = (await _facturaRepo.GetAllByExpresionAsync(h =>
                h.IdEmpresa == filtro.IdEmpresa
                && h.IdTipoDocumentos == TipoDocumentoVenta
                && h.TipoFactura == "Credito"
                && h.EstaCancelada == false
                && (filtro.IdTercero <= 0 || h.IDCliente == filtro.IdTercero)
            )).ToList();

            if (filtro.SoloPendientes)
                facturas = facturas.Where(h => h.Pendiente > 0 && h.Estado == "Pendiente").ToList();
            else
                facturas = facturas.Where(h => h.Pendiente > 0).ToList();

            facturas = AplicarFiltroFechasDocumento(facturas, filtro, h => h.FechaInseccion.Date);

            if (!string.IsNullOrWhiteSpace(filtro.Documento))
            {
                var q = filtro.Documento.Trim().ToLowerInvariant();
                facturas = facturas.Where(h =>
                    (h.NumeroDocumento ?? "").ToLowerInvariant().Contains(q)
                    || (h.NCF ?? "").ToLowerInvariant().Contains(q)
                    || h.IdFacturaHeader.ToString().Contains(q)
                ).ToList();
            }

            var clienteIds = facturas
                .Where(h => h.IDCliente.HasValue && h.IDCliente > 0)
                .Select(h => h.IDCliente!.Value)
                .Distinct()
                .ToList();

            var clientes = clienteIds.Count == 0
                ? new Dictionary<int, Clientes>()
                : (await _clientesRepo.GetAllByExpresionAsync(c =>
                    c.IdEmpresa == filtro.IdEmpresa && clienteIds.Contains(c.IDCliente)))
                  .ToDictionary(c => c.IDCliente);

            var lineas = new List<AntiguedadSaldosLineaDto>();
            foreach (var h in facturas)
            {
                var venc = h.FechaBencimiento == default ? h.FechaInseccion.Date : h.FechaBencimiento.Date;
                var dias = CalcularDiasVencidos(corte, venc);
                if (filtro.SoloVencidas && dias <= 0)
                    continue;

                var idCli = h.IDCliente ?? 0;
                clientes.TryGetValue(idCli, out var cli);
                var doc = !string.IsNullOrWhiteSpace(h.NumeroDocumento)
                    ? h.NumeroDocumento!
                    : $"FACT-{h.IdFacturaHeader}";

                lineas.Add(CrearLinea(
                    idDocumento: h.IdFacturaHeader,
                    documento: doc,
                    idTercero: idCli,
                    tercero: cli?.NombreComercial ?? "Cliente",
                    fechaDoc: h.FechaInseccion.Date,
                    fechaVenc: venc,
                    dias: dias,
                    saldo: h.Pendiente,
                    estado: h.Estado ?? ""
                ));
            }

            return await ConstruirReporteAsync(filtro.IdEmpresa, "CXC", corte, lineas);
        }

        public async Task<AntiguedadSaldosReporteDto> ObtenerAntiguedadCxPAsync(AntiguedadSaldosFiltroRequest filtro)
        {
            ValidarFiltro(filtro);
            var corte = (filtro.FechaCorte ?? DateTime.Today).Date;

            var estadosExcluidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BORRADOR", "ANULADA", "PAGADA"
            };

            var facturas = (await _compraRepo.GetAllByExpresionAsync(h =>
                h.IdEmpresa == filtro.IdEmpresa
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && (filtro.IdTercero <= 0 || h.IdProveedor == filtro.IdTercero)
            )).ToList();

            facturas = facturas
                .Where(h => !estadosExcluidos.Contains(h.Estado ?? ""))
                .ToList();

            if (filtro.SoloPendientes)
                facturas = facturas.Where(h => h.Pendiente > 0).ToList();

            facturas = AplicarFiltroFechasDocumento(facturas, filtro, h => h.FechaInseccion.Date);

            if (!string.IsNullOrWhiteSpace(filtro.Documento))
            {
                var q = filtro.Documento.Trim().ToLowerInvariant();
                facturas = facturas.Where(h =>
                    (h.NumeroDocumento ?? "").ToLowerInvariant().Contains(q)
                    || (h.NCF ?? "").ToLowerInvariant().Contains(q)
                    || h.IdOrdenCompraHeader.ToString().Contains(q)
                ).ToList();
            }

            var proveedorIds = facturas.Select(h => h.IdProveedor).Where(id => id > 0).Distinct().ToList();
            var proveedores = proveedorIds.Count == 0
                ? new Dictionary<int, Proveedores>()
                : (await _proveedoresRepo.GetAllByExpresionAsync(p =>
                    p.IdEmpresa == filtro.IdEmpresa && proveedorIds.Contains(p.IdProveedor)))
                  .ToDictionary(p => p.IdProveedor);

            var lineas = new List<AntiguedadSaldosLineaDto>();
            foreach (var h in facturas)
            {
                var venc = h.FechaBencimiento == default ? h.FechaInseccion.Date : h.FechaBencimiento.Date;
                var dias = CalcularDiasVencidos(corte, venc);
                if (filtro.SoloVencidas && dias <= 0)
                    continue;

                proveedores.TryGetValue(h.IdProveedor, out var prov);
                var doc = !string.IsNullOrWhiteSpace(h.NumeroDocumento)
                    ? h.NumeroDocumento!
                    : $"FACTC-{h.IdOrdenCompraHeader}";

                lineas.Add(CrearLinea(
                    idDocumento: h.IdOrdenCompraHeader,
                    documento: doc,
                    idTercero: h.IdProveedor,
                    tercero: prov?.NombreComercial ?? "Proveedor",
                    fechaDoc: h.FechaInseccion.Date,
                    fechaVenc: venc,
                    dias: dias,
                    saldo: h.Pendiente,
                    estado: h.Estado ?? ""
                ));
            }

            return await ConstruirReporteAsync(filtro.IdEmpresa, "CXP", corte, lineas);
        }

        private static void ValidarFiltro(AntiguedadSaldosFiltroRequest filtro)
        {
            if (filtro == null)
                throw new ArgumentException("Filtro inválido.");
            if (filtro.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");
            if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue
                && filtro.FechaHasta.Value.Date < filtro.FechaDesde.Value.Date)
                throw new ArgumentException("La fecha hasta no puede ser menor que desde.");
        }

        private static List<T> AplicarFiltroFechasDocumento<T>(
            List<T> items,
            AntiguedadSaldosFiltroRequest filtro,
            Func<T, DateTime> fechaSelector)
        {
            if (filtro.FechaDesde.HasValue)
            {
                var d = filtro.FechaDesde.Value.Date;
                items = items.Where(x => fechaSelector(x) >= d).ToList();
            }
            if (filtro.FechaHasta.HasValue)
            {
                var h = filtro.FechaHasta.Value.Date;
                items = items.Where(x => fechaSelector(x) <= h).ToList();
            }
            return items;
        }

        private static int CalcularDiasVencidos(DateTime corte, DateTime vencimiento)
        {
            return (corte.Date - vencimiento.Date).Days;
        }

        private static AntiguedadSaldosLineaDto CrearLinea(
            int idDocumento,
            string documento,
            int idTercero,
            string tercero,
            DateTime fechaDoc,
            DateTime fechaVenc,
            int dias,
            decimal saldo,
            string estado)
        {
            var diasAging = Math.Max(0, dias);
            var codigo = ClasificarRango(diasAging);
            var etiqueta = EtiquetaRango(codigo);
            var linea = new AntiguedadSaldosLineaDto
            {
                IdDocumento = idDocumento,
                Documento = documento,
                IdTercero = idTercero,
                TerceroNombre = tercero,
                FechaDocumento = fechaDoc,
                FechaVencimiento = fechaVenc,
                DiasVencidos = Math.Max(0, dias),
                SaldoPendiente = saldo,
                RangoCodigo = codigo,
                RangoEtiqueta = etiqueta,
                Estado = estado
            };

            switch (codigo)
            {
                case "0-30":
                    linea.Rango0a30 = saldo;
                    break;
                case "31-60":
                    linea.Rango31a60 = saldo;
                    break;
                case "61-90":
                    linea.Rango61a90 = saldo;
                    break;
                default:
                    linea.RangoMas90 = saldo;
                    break;
            }

            return linea;
        }

        private static string ClasificarRango(int diasAging)
        {
            if (diasAging <= 30) return "0-30";
            if (diasAging <= 60) return "31-60";
            if (diasAging <= 90) return "61-90";
            return ">90";
        }

        private static string EtiquetaRango(string codigo) => codigo switch
        {
            "0-30" => "0-30 días",
            "31-60" => "31-60 días",
            "61-90" => "61-90 días",
            _ => "Más de 90 días"
        };

        private async Task<AntiguedadSaldosReporteDto> ConstruirReporteAsync(
            int idEmpresa,
            string tipo,
            DateTime corte,
            List<AntiguedadSaldosLineaDto> lineas)
        {
            lineas = lineas
                .OrderByDescending(l => l.DiasVencidos)
                .ThenBy(l => l.TerceroNombre)
                .ThenBy(l => l.FechaVencimiento)
                .ToList();

            var totales = new AntiguedadSaldosTotalesDto
            {
                TotalPendiente = lineas.Sum(l => l.SaldoPendiente),
                Total0a30 = lineas.Sum(l => l.Rango0a30),
                Total31a60 = lineas.Sum(l => l.Rango31a60),
                Total61a90 = lineas.Sum(l => l.Rango61a90),
                TotalMas90 = lineas.Sum(l => l.RangoMas90),
                CantidadDocumentos = lineas.Count,
                CantidadTerceros = lineas.Select(l => l.IdTercero).Where(id => id > 0).Distinct().Count()
            };

            var porTercero = lineas
                .Where(l => l.IdTercero > 0)
                .GroupBy(l => new { l.IdTercero, l.TerceroNombre })
                .Select(g => new AntiguedadSaldosTopTerceroDto
                {
                    IdTercero = g.Key.IdTercero,
                    Nombre = g.Key.TerceroNombre,
                    Saldo = g.Sum(x => x.SaldoPendiente),
                    CantidadDocumentos = g.Count(),
                    MaxDiasVencidos = g.Max(x => x.DiasVencidos)
                })
                .OrderByDescending(x => x.Saldo)
                .ToList();

            var mayor = porTercero.FirstOrDefault();
            var indicadores = new AntiguedadSaldosIndicadoresDto
            {
                TotalTerceros = totales.CantidadTerceros,
                PromedioPorTercero = totales.CantidadTerceros > 0
                    ? Math.Round(totales.TotalPendiente / totales.CantidadTerceros, 2)
                    : 0,
                SaldoPromedioDocumento = totales.CantidadDocumentos > 0
                    ? Math.Round(totales.TotalPendiente / totales.CantidadDocumentos, 2)
                    : 0,
                MayorDeuda = mayor?.Saldo ?? 0,
                TerceroMayorDeuda = mayor?.Nombre,
                IdTerceroMayorDeuda = mayor?.IdTercero,
                PromedioDiasVencidos = lineas.Count > 0
                    ? Math.Round((decimal)lineas.Average(l => l.DiasVencidos), 1)
                    : 0
            };

            var total = totales.TotalPendiente;
            AntiguedadSaldosRangoDto Rango(string codigo, string etiqueta, decimal monto, int cant) =>
                new()
                {
                    Codigo = codigo,
                    Etiqueta = etiqueta,
                    Monto = monto,
                    Cantidad = cant,
                    Porcentaje = total > 0 ? Math.Round(monto / total * 100m, 2) : 0
                };

            var distribucion = new List<AntiguedadSaldosRangoDto>
            {
                Rango("0-30", "0-30 días", totales.Total0a30, lineas.Count(l => l.RangoCodigo == "0-30")),
                Rango("31-60", "31-60 días", totales.Total31a60, lineas.Count(l => l.RangoCodigo == "31-60")),
                Rango("61-90", "61-90 días", totales.Total61a90, lineas.Count(l => l.RangoCodigo == "61-90")),
                Rango(">90", "Más de 90 días", totales.TotalMas90, lineas.Count(l => l.RangoCodigo == ">90")),
            };

            var empresa = await _empresaRepo.GetByIdAsync(idEmpresa);

            return new AntiguedadSaldosReporteDto
            {
                IdEmpresa = idEmpresa,
                NombreEmpresa = empresa?.NombreComercial,
                FechaCorte = corte,
                Tipo = tipo,
                Lineas = lineas,
                Totales = totales,
                Indicadores = indicadores,
                Distribucion = distribucion,
                TopTerceros = porTercero.Take(10).ToList()
            };
        }
    }
}
