using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ArsAseguradoraService : IArsAseguradoraService
    {
        private readonly IRepository<ArsAseguradora> _arsRepo;
        private readonly IRepository<FacturaHeaders> _facturaRepo;
        private readonly IRepository<PagosFacturasClientes> _pagosRepo;
        private readonly IRepository<Ingresos> _ingresosRepo;
        private readonly IRepository<Clientes> _clientesRepo;
        private readonly IParametrosService _parametros;
        private readonly IIngresos _ingresosService;
        private readonly IPagosFacturasClientes _pagosFacturas;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly IContabilidadEventPublisher _contabilidadEvents;

        public ArsAseguradoraService(
            IRepository<ArsAseguradora> arsRepo,
            IRepository<FacturaHeaders> facturaRepo,
            IRepository<PagosFacturasClientes> pagosRepo,
            IRepository<Ingresos> ingresosRepo,
            IRepository<Clientes> clientesRepo,
            IParametrosService parametros,
            IIngresos ingresosService,
            IPagosFacturasClientes pagosFacturas,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            IMovimientoFinancieroService movimientoFinancieroService,
            IContabilidadEventPublisher contabilidadEvents)
        {
            _arsRepo = arsRepo;
            _facturaRepo = facturaRepo;
            _pagosRepo = pagosRepo;
            _ingresosRepo = ingresosRepo;
            _clientesRepo = clientesRepo;
            _parametros = parametros;
            _ingresosService = ingresosService;
            _pagosFacturas = pagosFacturas;
            _metodoPagoCuentaService = metodoPagoCuentaService;
            _movimientoFinancieroService = movimientoFinancieroService;
            _contabilidadEvents = contabilidadEvents;
        }

        public async Task<IEnumerable<ArsAseguradora>> GetAll(int idEmpresa, bool soloActivos = true)
        {
            return await _arsRepo.GetAllByExpresionAsync(a =>
                a.IdEmpresa == idEmpresa && (!soloActivos || a.Activo));
        }

        public async Task<ArsAseguradora?> GetById(int idArs, int idEmpresa)
        {
            return await _arsRepo.GetByExpresionAsync(a =>
                a.IdArs == idArs && a.IdEmpresa == idEmpresa);
        }

        public async Task<ArsAseguradora> Insert(ArsAseguradora ars)
        {
            if (ars.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");
            if (string.IsNullOrWhiteSpace(ars.Nombre))
                throw new ArgumentException("El nombre de la ARS es requerido.");

            Normalizar(ars);
            ars.FechaInseccion = DateTime.Now;
            ars.Activo = true;
            await _arsRepo.Save(ars);
            return ars;
        }

        public async Task Update(int idArs, ArsAseguradora ars, int idEmpresa)
        {
            var existente = await GetById(idArs, idEmpresa)
                ?? throw new KeyNotFoundException("ARS no encontrada.");

            if (string.IsNullOrWhiteSpace(ars.Nombre))
                throw new ArgumentException("El nombre de la ARS es requerido.");

            existente.Nombre = ars.Nombre.Trim();
            existente.RNC = ars.RNC?.Trim() ?? string.Empty;
            existente.Telefono = ars.Telefono?.Trim() ?? string.Empty;
            existente.Direccion = ars.Direccion?.Trim() ?? string.Empty;
            existente.Email = ars.Email?.Trim() ?? string.Empty;
            existente.Contacto = ars.Contacto?.Trim() ?? string.Empty;
            existente.Observaciones = ars.Observaciones?.Trim() ?? string.Empty;
            existente.Activo = ars.Activo;
            _arsRepo.Update(idArs, existente);
            await Task.CompletedTask;
        }

        public async Task SetActivo(int idArs, int idEmpresa, bool activo)
        {
            var existente = await GetById(idArs, idEmpresa)
                ?? throw new KeyNotFoundException("ARS no encontrada.");
            existente.Activo = activo;
            _arsRepo.Update(idArs, existente);
            await Task.CompletedTask;
        }

        public async Task<bool> EmpresaUsaArsAsync(int idEmpresa)
        {
            var parametro = await _parametros.GetParametro(idEmpresa, FormaPagoArs.ParametroClave);
            return FormaPagoArs.ParametroActivo(parametro?.Valor);
        }

        public async Task<decimal> AplicarCoberturaEnVentaAsync(
            FacturaHeaders header,
            IEnumerable<PagoDTO> pagos,
            bool esAbonoInicialCredito)
        {
            var pagosArs = (pagos ?? Enumerable.Empty<PagoDTO>())
                .Where(p => p != null && p.Monto > 0.009m && FormaPagoArs.EsArs(p.Metodo))
                .ToList();

            if (pagosArs.Count == 0)
                return 0;

            if (!await EmpresaUsaArsAsync(header.IdEmpresa))
                throw new InvalidOperationException("La empresa no tiene habilitado el uso de ARS.");

            var ids = pagosArs
                .Select(p => p.IdArs ?? 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count != 1)
                throw new InvalidOperationException("Debe indicar una sola ARS para la cobertura.");

            var idArs = ids[0];
            var ars = await GetById(idArs, header.IdEmpresa)
                ?? throw new InvalidOperationException("La ARS seleccionada no existe en esta empresa.");

            if (!ars.Activo)
                throw new InvalidOperationException("La ARS está inactiva. No se puede usar en operaciones nuevas.");

            var cobertura = Math.Round(pagosArs.Sum(p => p.Monto), 2);
            if (cobertura <= 0)
                return 0;

            var tope = Math.Round(header.Total - header.Pagado, 2);
            if (cobertura > tope + 0.02m)
                throw new InvalidOperationException("La cobertura ARS no puede superar el total de la factura.");

            header.IdArs = idArs;
            header.MontoCubiertoArs = cobertura;
            header.PagadoArs = 0;
            if (!header.FechaVencimientoArs.HasValue)
            {
                var dias = ExtraerDiasPlazo(header.Plazo);
                if (dias <= 0)
                    dias = 30;
                header.FechaVencimientoArs = (header.FechaInseccion == default ? DateTime.Now : header.FechaInseccion)
                    .Date.AddDays(dias);
            }

            var existeIngreso = await _ingresosService.ExisteIngreso(header.IdFacturaHeader, FormaPagoArs.Metodo);
            if (!existeIngreso)
            {
                await _ingresosService.InsertIngreso(new Ingresos
                {
                    IdEmpresa = header.IdEmpresa,
                    FechaRegistro = DateTime.Now,
                    Descripcion = esAbonoInicialCredito
                        ? $"Cobertura ARS {ars.Nombre} — Factura #{header.IdFacturaHeader}"
                        : $"Cobertura ARS {ars.Nombre} — Factura #{header.IdFacturaHeader}",
                    Categoria = "Cobertura ARS",
                    Origen = "Sistema",
                    Monto = cobertura,
                    FormaPago = FormaPagoArs.Metodo,
                    Referencia = $"ARS-{idArs}-F{header.IdFacturaHeader}",
                    IdFacturaHeader = header.IdFacturaHeader,
                    IdCliente = header.IDCliente,
                    IdUsuario = header.IdUsuario,
                    Nota = $"Cobertura {ars.Nombre}. No es dinero recibido en caja."
                });
            }

            await _pagosFacturas.InsertPagosFacturasClientes(new PagosFacturasClientes
            {
                IdFacturaHeader = header.IdFacturaHeader,
                NumeroDocumento = header.NumeroDocumento ?? "",
                IDCliente = header.IDCliente,
                IdArs = idArs,
                EsCoberturaArs = true,
                FormaPago = FormaPagoArs.Metodo,
                Monto = cobertura,
                IdEmpresa = header.IdEmpresa,
                Nota = $"Cobertura ARS {ars.Nombre} RD$ {cobertura:N2} — usuario #{header.IdUsuario}"
            });

            FormaPagoArs.RecalcularSaldos(header);
            return cobertura;
        }

        public async Task RegistrarPagoArsAsync(ArsPagoRequest request)
        {
            if (request == null)
                throw new ArgumentException("Datos del pago inválidos.");
            if (request.Monto <= 0)
                throw new ArgumentException("El monto del pago debe ser mayor a cero.");

            var factura = await _facturaRepo.GetByIdAsync(request.IdFacturaHeader)
                ?? throw new KeyNotFoundException("No se encontró la factura.");

            if (factura.IdEmpresa != request.IdEmpresa)
                throw new InvalidOperationException("La factura no pertenece a esta empresa.");
            if (factura.EstaCancelada)
                throw new InvalidOperationException("La factura está anulada.");
            if (factura.IdArs is not > 0 || factura.MontoCubiertoArs <= 0.009m)
                throw new InvalidOperationException("La factura no tiene cobertura ARS pendiente.");

            FormaPagoArs.RecalcularSaldos(factura);
            if (request.Monto > factura.PendienteArs + 0.02m)
                throw new ArgumentException("El monto ingresado excede el pendiente de la ARS.");

            var formaPago = string.IsNullOrWhiteSpace(request.FormaPago)
                ? "Transferencia"
                : request.FormaPago.Trim();

            if (FormaPagoArs.EsArs(formaPago))
                throw new ArgumentException("El cobro de la ARS debe registrarse con el medio real recibido (transferencia, cheque, etc.).");

            var metodoConfigurado = await _metodoPagoCuentaService
                .GetByMetodoAsync(factura.IdEmpresa, formaPago);

            if (metodoConfigurado == null || metodoConfigurado.IdCuentaFinanciera <= 0)
            {
                throw new InvalidOperationException(
                    "El cobro ARS debe usar una forma de pago vinculada a una cuenta financiera (Tesorería).");
            }

            var ars = await GetById(factura.IdArs.Value, factura.IdEmpresa);
            var nombreArs = ars?.Nombre ?? $"ARS #{factura.IdArs}";

            var pago = new PagosFacturasClientes
            {
                IdFacturaHeader = factura.IdFacturaHeader,
                NumeroDocumento = factura.NumeroDocumento ?? "",
                IDCliente = factura.IDCliente,
                IdArs = factura.IdArs,
                EsCoberturaArs = false,
                FormaPago = formaPago,
                Monto = Math.Round(request.Monto, 2),
                IdEmpresa = factura.IdEmpresa,
                FechaInseccion = DateTime.Now,
                Nota = string.IsNullOrWhiteSpace(request.Nota)
                    ? $"Cobro ARS {nombreArs} RD$ {request.Monto:N2} — {formaPago}"
                    : request.Nota.Trim()
            };

            await _pagosFacturas.InsertPagosFacturasClientes(pago);

            factura.Clientes = null;
            factura.PagadoArs += pago.Monto;
            FormaPagoArs.RecalcularSaldos(factura);
            _facturaRepo.Update(factura.IdFacturaHeader, factura);

            var categoria = factura.PendienteArs <= 0.009m
                ? "Saldo de ARS"
                : "Abono ARS";

            await _ingresosService.InsertIngreso(new Ingresos
            {
                IdEmpresa = factura.IdEmpresa,
                FechaRegistro = DateTime.Now,
                Descripcion = factura.PendienteArs <= 0.009m
                    ? $"Pago completo ARS {nombreArs} — Factura #{factura.IdFacturaHeader}"
                    : $"Abono ARS {nombreArs} — Factura #{factura.IdFacturaHeader}",
                Categoria = categoria,
                Origen = "Cobro ARS",
                Monto = pago.Monto,
                FormaPago = formaPago,
                Referencia = $"ARS-{factura.IdArs}-F{factura.IdFacturaHeader}",
                IdFacturaHeader = factura.IdFacturaHeader,
                IdCliente = factura.IDCliente,
                IdUsuario = request.IdUsuario ?? factura.IdUsuario,
                Nota = pago.Nota
            });

            await _movimientoFinancieroService.RegistrarEntradaAsync(
                factura.IdEmpresa,
                request.IdUsuario ?? factura.IdUsuario ?? factura.IdEmpleados ?? 0,
                metodoConfigurado.IdCuentaFinanciera,
                pago.Monto,
                $"Factura #{factura.IdFacturaHeader}",
                $"Cobro ARS {nombreArs} ({formaPago})",
                categoria: "COBRO_CXC",
                referenciaId: factura.IdFacturaHeader,
                referenciaTipo: "FACTURA_ARS",
                claveIdempotencia: $"ARS-{factura.IdFacturaHeader}-{formaPago}-{pago.Monto}-{pago.FechaInseccion:yyyyMMddHHmmss}"
            );

            await _contabilidadEvents.TryPublishAsync(new CobroClienteRegistradoEvent
            {
                IdEmpresa = factura.IdEmpresa,
                IdUsuario = request.IdUsuario ?? factura.IdUsuario ?? 0,
                Fecha = pago.FechaInseccion,
                ReferenciaId = pago.Id > 0 ? pago.Id : factura.IdFacturaHeader,
                ReferenciaTipo = "CobroArs",
                Monto = pago.Monto,
                FormaPago = formaPago,
                IdFacturaHeader = factura.IdFacturaHeader
            });
        }

        public async Task<ArsPagoLoteResultadoDto> RegistrarPagoLoteArsAsync(ArsPagoLoteRequest request)
        {
            if (request == null)
                throw new ArgumentException("Datos del pago inválidos.");

            var ids = (request.IdFacturaHeaders ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                throw new ArgumentException("Seleccione al menos una factura ARS.");

            var facturas = (await _facturaRepo.GetAllByExpresionAsync(f =>
                f.IdEmpresa == request.IdEmpresa && ids.Contains(f.IdFacturaHeader)))
                .ToList();

            if (facturas.Count == 0)
                throw new KeyNotFoundException("No se encontraron las facturas seleccionadas.");

            foreach (var f in facturas)
                FormaPagoArs.RecalcularSaldos(f);

            var pendientes = facturas
                .Where(f => !f.EstaCancelada && f.PendienteArs > 0.009m)
                .OrderBy(f => f.FechaInseccion)
                .ThenBy(f => f.IdFacturaHeader)
                .ToList();

            if (pendientes.Count == 0)
                throw new InvalidOperationException("Las facturas seleccionadas no tienen pendiente ARS.");

            var arsIds = pendientes.Select(f => f.IdArs ?? 0).Where(id => id > 0).Distinct().ToList();
            if (arsIds.Count != 1)
                throw new InvalidOperationException("Solo puede cotejar facturas de la misma ARS.");

            var totalPendiente = Math.Round(pendientes.Sum(f => f.PendienteArs), 2);
            if (request.Monto > totalPendiente + 0.02m)
                throw new ArgumentException("El monto ingresado excede el pendiente de las facturas seleccionadas.");

            var partes = FormaPagoArs.DistribuirPagoLote(
                pendientes.Select(f => (f.IdFacturaHeader, f.PendienteArs)),
                request.Monto);

            var nombreArs = (await GetById(arsIds[0], request.IdEmpresa))?.Nombre ?? $"ARS #{arsIds[0]}";
            var notaLote = string.IsNullOrWhiteSpace(request.Nota)
                ? $"Cotejo lote ARS {nombreArs} ({partes.Count} facturas)"
                : request.Nota.Trim();

            decimal total = 0;
            foreach (var parte in partes)
            {
                await RegistrarPagoArsAsync(new ArsPagoRequest
                {
                    IdEmpresa = request.IdEmpresa,
                    IdFacturaHeader = parte.IdFactura,
                    Monto = parte.Monto,
                    FormaPago = request.FormaPago,
                    Nota = notaLote,
                    IdUsuario = request.IdUsuario
                });
                total += parte.Monto;
            }

            return new ArsPagoLoteResultadoDto
            {
                Documentos = partes.Count,
                TotalAplicado = Math.Round(total, 2),
                NombreArs = nombreArs
            };
        }

        public async Task<IEnumerable<ArsCuentaPorCobrarDto>> GetCuentasPorCobrarArsAsync(ArsResumenFiltroRequest filtro)
        {
            var docs = await GetDocumentosArsAsync(filtro);
            return docs
                .GroupBy(d => d.IdArs)
                .Select(g =>
                {
                    var sample = g.First();
                    return new ArsCuentaPorCobrarDto
                    {
                        IdArs = g.Key,
                        NombreArs = sample.NombreArs,
                        TotalOriginal = g.Sum(x => x.MontoCubiertoArs),
                        TotalPagado = g.Sum(x => x.PagadoArs),
                        TotalPendiente = g.Sum(x => x.PendienteArs),
                        Documentos = g.Count()
                    };
                })
                .OrderByDescending(x => x.TotalPendiente)
                .ToList();
        }

        public async Task<IEnumerable<ArsDocumentoCxCDto>> GetDocumentosArsAsync(ArsResumenFiltroRequest filtro)
        {
            ValidarEmpresa(filtro);
            var facturas = (await _facturaRepo.GetAllByExpresionAsync(f =>
                f.IdEmpresa == filtro.IdEmpresa
                && f.IdArs != null
                && f.IdArs > 0
                && f.MontoCubiertoArs > 0
                && f.EstaCancelada == false
                && f.IdTipoDocumentos == 1
                && (filtro.IdArs <= 0 || f.IdArs == filtro.IdArs)
                && (filtro.IdSucursal <= 0 || f.IdSucursal == filtro.IdSucursal)
            )).ToList();

            if (filtro.FechaDesde.HasValue)
            {
                var desde = filtro.FechaDesde.Value.Date;
                facturas = facturas.Where(f => f.FechaInseccion.Date >= desde).ToList();
            }

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value.Date;
                facturas = facturas.Where(f => f.FechaInseccion.Date <= hasta).ToList();
            }

            foreach (var f in facturas)
                FormaPagoArs.RecalcularSaldos(f);

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
            {
                var estado = filtro.Estado.Trim();
                facturas = facturas
                    .Where(f => string.Equals(f.EstadoArs, estado, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var arsIds = facturas.Select(f => f.IdArs!.Value).Distinct().ToList();
            var arsMap = arsIds.Count == 0
                ? new Dictionary<int, ArsAseguradora>()
                : (await _arsRepo.GetAllByExpresionAsync(a =>
                    a.IdEmpresa == filtro.IdEmpresa && arsIds.Contains(a.IdArs)))
                  .ToDictionary(a => a.IdArs);

            var clienteIds = facturas
                .Where(f => f.IDCliente is > 0)
                .Select(f => f.IDCliente!.Value)
                .Distinct()
                .ToList();
            var clientes = clienteIds.Count == 0
                ? new Dictionary<int, Clientes>()
                : (await _clientesRepo.GetAllByExpresionAsync(c =>
                    clienteIds.Contains(c.IDCliente)))
                  .ToDictionary(c => c.IDCliente);

            return facturas
                .OrderByDescending(f => f.FechaInseccion)
                .Select(f =>
                {
                    arsMap.TryGetValue(f.IdArs!.Value, out var ars);
                    clientes.TryGetValue(f.IDCliente ?? 0, out var cliente);
                    return new ArsDocumentoCxCDto
                    {
                        IdFacturaHeader = f.IdFacturaHeader,
                        IdArs = f.IdArs.Value,
                        NombreArs = ars?.Nombre ?? $"ARS #{f.IdArs}",
                        NumeroDocumento = f.NumeroDocumento,
                        NCF = f.NCF,
                        Fecha = f.FechaInseccion,
                        FechaVencimiento = f.FechaVencimientoArs,
                        IdCliente = f.IDCliente,
                        NombreCliente = cliente?.NombreComercial
                            ?? (string.IsNullOrWhiteSpace(f.NombreEmpresa) ? "Cliente" : f.NombreEmpresa),
                        TotalFactura = f.Total,
                        MontoCubiertoArs = f.MontoCubiertoArs,
                        PagadoArs = f.PagadoArs,
                        PendienteArs = f.PendienteArs,
                        EstadoArs = f.EstadoArs ?? "",
                        IdEmpresa = f.IdEmpresa
                    };
                })
                .ToList();
        }

        public async Task<IEnumerable<ArsPagoHistorialDto>> GetPagosArsAsync(int idEmpresa, int idFacturaHeader, int idArs)
        {
            var pagos = await _pagosRepo.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa
                && (idFacturaHeader <= 0 || p.IdFacturaHeader == idFacturaHeader)
                && p.IdArs != null
                && (idArs <= 0 || p.IdArs == idArs));

            return pagos
                .OrderByDescending(p => p.FechaInseccion)
                .Select(p => new ArsPagoHistorialDto
                {
                    IdPago = p.Id,
                    IdFacturaHeader = p.IdFacturaHeader,
                    IdArs = p.IdArs,
                    NumeroDocumento = p.NumeroDocumento,
                    FormaPago = p.FormaPago,
                    Monto = p.Monto,
                    Nota = p.Nota,
                    EsCoberturaArs = p.EsCoberturaArs,
                    Fecha = p.FechaInseccion
                })
                .ToList();
        }

        public async Task<IEnumerable<ArsVentasResumenDto>> GetVentasPorArsAsync(ArsResumenFiltroRequest filtro)
        {
            var docs = await GetDocumentosArsAsync(filtro);
            var facturaIds = docs.Select(d => d.IdFacturaHeader).ToList();
            var headers = facturaIds.Count == 0
                ? new List<FacturaHeaders>()
                : (await _facturaRepo.GetAllByExpresionAsync(f =>
                    facturaIds.Contains(f.IdFacturaHeader) && f.IdEmpresa == filtro.IdEmpresa))
                  .ToList();
            var headerMap = headers.ToDictionary(h => h.IdFacturaHeader);

            return docs
                .GroupBy(d => d.IdArs)
                .Select(g =>
                {
                    var sample = g.First();
                    return new ArsVentasResumenDto
                    {
                        IdArs = g.Key,
                        NombreArs = sample.NombreArs,
                        Documentos = g.Count(),
                        TotalVentas = g.Sum(x =>
                            headerMap.TryGetValue(x.IdFacturaHeader, out var h) ? h.Total : x.TotalFactura),
                        CoberturaArs = g.Sum(x => x.MontoCubiertoArs),
                        PagadoPaciente = g.Sum(x =>
                            headerMap.TryGetValue(x.IdFacturaHeader, out var h) ? h.Pagado : 0),
                        PagadoArs = g.Sum(x => x.PagadoArs),
                        PendienteArs = g.Sum(x => x.PendienteArs)
                    };
                })
                .OrderByDescending(x => x.CoberturaArs)
                .ToList();
        }

        public async Task<IEnumerable<ArsAntiguedadBucketDto>> GetAntiguedadArsAsync(ArsResumenFiltroRequest filtro)
        {
            filtro.Estado = FormaPagoArs.EstadoPendiente;
            var pendientes = (await GetDocumentosArsAsync(filtro)).ToList();
            filtro.Estado = FormaPagoArs.EstadoParcial;
            pendientes.AddRange(await GetDocumentosArsAsync(filtro));

            var corte = DateTime.Today;
            return pendientes
                .GroupBy(d => d.IdArs)
                .Select(g =>
                {
                    decimal d0 = 0, d31 = 0, d61 = 0, d90 = 0;
                    foreach (var doc in g)
                    {
                        var dias = (corte - doc.Fecha.Date).Days;
                        if (dias <= 30) d0 += doc.PendienteArs;
                        else if (dias <= 60) d31 += doc.PendienteArs;
                        else if (dias <= 90) d61 += doc.PendienteArs;
                        else d90 += doc.PendienteArs;
                    }

                    return new ArsAntiguedadBucketDto
                    {
                        IdArs = g.Key,
                        NombreArs = g.First().NombreArs,
                        Dias0A30 = d0,
                        Dias31A60 = d31,
                        Dias61A90 = d61,
                        DiasMas90 = d90,
                        TotalPendiente = d0 + d31 + d61 + d90
                    };
                })
                .OrderByDescending(x => x.TotalPendiente)
                .ToList();
        }

        public async Task<IEnumerable<ArsDesgloseCajaDto>> GetDesgloseCajaAbiertaAsync(int idEmpresa, int idUsuario)
        {
            var facturas = (await _facturaRepo.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa
                && x.IdUsuario == idUsuario
                && x.IdTipoDocumentos == 1
                && x.EstaCerrada == false
                && x.EstaCancelada == false
                && x.IdArs != null
                && x.MontoCubiertoArs > 0
            )).ToList();

            if (facturas.Count == 0)
                return Array.Empty<ArsDesgloseCajaDto>();

            var ids = facturas.Select(f => f.IdFacturaHeader).ToList();
            var ingresos = (await _ingresosRepo.GetAllByExpresionAsync(i =>
                i.IdEmpresa == idEmpresa
                && i.IdFacturaHeader != null
                && ids.Contains(i.IdFacturaHeader.Value)
                && i.EstaAnulado == false
                && i.FormaPago != null
            )).Where(i => FormaPagoArs.EsArs(i.FormaPago)).ToList();

            var arsIds = facturas.Select(f => f.IdArs!.Value).Distinct().ToList();
            var arsMap = (await _arsRepo.GetAllByExpresionAsync(a =>
                a.IdEmpresa == idEmpresa && arsIds.Contains(a.IdArs)))
                .ToDictionary(a => a.IdArs);

            return facturas
                .GroupBy(f => f.IdArs!.Value)
                .Select(g =>
                {
                    var idsGrupo = g.Select(x => x.IdFacturaHeader).ToHashSet();
                    var total = ingresos
                        .Where(i => i.IdFacturaHeader is > 0 && idsGrupo.Contains(i.IdFacturaHeader.Value))
                        .Sum(i => i.Monto);
                    if (total <= 0)
                        total = g.Sum(x => x.MontoCubiertoArs);

                    arsMap.TryGetValue(g.Key, out var ars);
                    return new ArsDesgloseCajaDto
                    {
                        IdArs = g.Key,
                        NombreArs = ars?.Nombre ?? $"ARS #{g.Key}",
                        Total = total
                    };
                })
                .Where(x => x.Total > 0)
                .OrderByDescending(x => x.Total)
                .ToList();
        }

        private static void Normalizar(ArsAseguradora ars)
        {
            ars.Nombre = ars.Nombre.Trim();
            ars.RNC ??= string.Empty;
            ars.Telefono ??= string.Empty;
            ars.Direccion ??= string.Empty;
            ars.Email ??= string.Empty;
            ars.Contacto ??= string.Empty;
            ars.Observaciones ??= string.Empty;
        }

        private static void ValidarEmpresa(ArsResumenFiltroRequest filtro)
        {
            if (filtro == null || filtro.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");
        }

        private static int ExtraerDiasPlazo(string? plazo)
        {
            if (string.IsNullOrWhiteSpace(plazo))
                return 0;
            var digits = new string(plazo.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var dias) ? dias : 0;
        }
    }
}
