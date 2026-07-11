using Alahia_Pos.Services;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class BizcochoEncargoServices : IBizcochoEncargoService
    {
        private const int TipoDocumentoEncargo = 14;

        private readonly IRepository<FacturaHeaders> _repository;
        private readonly IIngresos _ingresos;
        private readonly IMetodoPagoCuentaService _MetodoPagoCuentaService;

        private readonly IMovimientoFinancieroService _MovimientoFinancieroService;
        private readonly IRepository<Clientes> _clientesRepository;
        private readonly IRepository<Empresas> _RepositoryEmpresa;
        private readonly IRepository<FacturaDetalles> _facturaDetalleRepository;
        private readonly IProductos _productosServices;
        private readonly IRepository<Productos> _RproductosServices;
        private readonly INCF_Secuencias _Secuencias;
        private readonly ISecuenciaDocumentoService _secuenciaDocumentoService;

        public BizcochoEncargoServices(
            IRepository<FacturaHeaders> repository,
            IIngresos ingresos, IRepository<Clientes> clientesRepository,
            IRepository<FacturaDetalles> facturaDetalleRepository,
            IProductos productosServices, INCF_Secuencias secuencias,
            ISecuenciaDocumentoService secuenciaDocumentoService,
            IRepository<Productos> RproductosServices,
            IRepository<Empresas> RepositoryEmpresa,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            IMovimientoFinancieroService movimientoFinancieroService

            )
        {
            _clientesRepository = clientesRepository;
            _secuenciaDocumentoService = secuenciaDocumentoService;
            _repository = repository;
            _RepositoryEmpresa = RepositoryEmpresa;
            _productosServices = productosServices;
            _RproductosServices = RproductosServices;
            _facturaDetalleRepository = facturaDetalleRepository;
            _ingresos = ingresos;
            _Secuencias = secuencias;
            _MetodoPagoCuentaService =
    metodoPagoCuentaService;

            _MovimientoFinancieroService =
                movimientoFinancieroService;
        }

        private static void ValidateCreateRequest(RequestBizcochoEncargoDto request)
        {
            if (request?.Encargo == null)
                throw new ArgumentException("Datos inválidos");

            var dto = request.Encargo;

            if (dto.IdEmpresa <= 0)
                throw new ArgumentException("idEmpresa es obligatorio");

            if (dto.Total <= 0)
                throw new ArgumentException("El total debe ser mayor a 0");

            if (dto.FechaEntrega == null)
                throw new ArgumentException("Debe indicar fecha de entrega");

            if (string.IsNullOrWhiteSpace(dto.HoraEntrega))
                throw new ArgumentException("Debe indicar hora de entrega");

            if (dto.IDCliente <= 0 && string.IsNullOrWhiteSpace(dto.cliente))
                throw new ArgumentException("Debe indicar nombre del cliente");

            var pagos = request.Pagos ?? new List<PagoDTO>();

            if (pagos.Any(x => x.Monto <= 0))
                throw new ArgumentException("Hay pagos inválidos");

            if (pagos.Sum(x => x.Monto) > dto.Total)
                throw new ArgumentException("Los pagos exceden el total");
        }

        private async Task<FacturaHeaders?> GetEncargoAsync(int id, int idEmpresa)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null ||
                entity.IdEmpresa != idEmpresa ||
                entity.IdTipoDocumentos != TipoDocumentoEncargo)
            {
                return null;
            }

            return entity;
        }

        // 🔥 CREAR
        public async Task<int> CreateAsync(RequestBizcochoEncargoDto request)
        {
            ValidateCreateRequest(request);

            var dto = request.Encargo;

            var pagos = request.Pagos ?? new List<PagoDTO>();

            // =====================================================
            // 🔥 SOLO PAGOS VÁLIDOS
            // =====================================================

            pagos = pagos
                .Where(x => x.Monto > 0)
                .ToList();

            // =====================================================
            // 🔥 ABONO REAL
            // =====================================================

            decimal abono = pagos.Any()
                ? pagos.Sum(x => x.Monto)
                : dto.Abono;

            // =====================================================
            // 🔥 CLIENTE
            // =====================================================

            int? idCliente = dto.IDCliente;

            // 🔥 SI NO EXISTE → CREAR
            if (idCliente == null || idCliente <= 0)
            {
                var nuevoCliente = new Clientes
                {
                    IdEmpresa = dto.IdEmpresa,

                    NombreComercial =
                        dto.cliente,
                    LimiteCredito = 0,
                    Estado = true,

                    Telefono =
                        dto.celular,

                    Celular =
                        dto.celular,
                };
                try
                {
                    await _clientesRepository
                    .Save(nuevoCliente);

                    idCliente =
                        nuevoCliente.IDCliente;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error guardando encargo: {ex.Message}", ex);
                }

            }

            // =====================================================
            // 🔥 AGRUPAR PAGOS
            // =====================================================

            var pagosAgrupados = pagos
                .Where(x => x.Monto > 0)
                .GroupBy(x => x.Metodo)
                .Select(g => new
                {
                    Metodo = g.Key,
                    Monto = g.Sum(x => x.Monto)
                })
                .ToList();

            // =====================================================
            // 🔥 FORMA PAGO
            // =====================================================

            var metodosPago = pagosAgrupados
                .Select(x => x.Metodo)
                .Distinct()
                .ToList();

            string formaPago = metodosPago.Count == 1
                ? metodosPago.First()
                : "Mixto";

            // =====================================================
            // 🔥 ENTITY
            // =====================================================
            string Secu = await _secuenciaDocumentoService.GenerarDocumentoAsync(dto.IdEmpresa, 14);
            var entity = new FacturaHeaders
            {
                IdEmpresa = dto.IdEmpresa,

                IDCliente = idCliente,

                // 🔥 ENCARGO
                IdTipoDocumentos = TipoDocumentoEncargo,
                NumeroDocumento = Secu,

                TipoFactura = "Credito",

                Estado = abono >= dto.Total
                    ? "Pagado"
                    : "Pendiente",

                // 🔥 FECHAS
                FechaInseccion = DateTime.Now,



                FechaBencimiento =
                    (DateTime)dto.FechaEntrega,

                FechaEntrega =
                    dto.FechaEntrega,

                HoraEntrega =
                    dto.HoraEntrega,

                // 🔥 MONTOS
                Total =
                    dto.Total,

                SubTotal =
                    dto.Total,

                Pagado =
                    abono,

                Abono =
                    abono,

                Pendiente =
                    dto.Total - abono,

                TotalItbis = 0,

                // 🔥 OTROS
                Nota =
                    dto.Nota,

                FormaPago =
                    formaPago,

                NCF = "",

                PrintAcount = false,

                IdMesa = 1,

                EstaCancelada = false
            };

            // =====================================================
            // 🔥 GUARDAR HEADER
            // =====================================================
            entity.Clientes = null;
            try
            {
                await _repository.Save(entity);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error guardando encargo: {ex.Message}"
                );
            }

            // =====================================================
            // 🔥 SERVICIO GENÉRICO
            // =====================================================

            var servicio = await _productosServices
                .GetServicioBizcochoEncargo(entity.IdEmpresa);

            // =====================================================
            // 🔥 DETALLES
            // =====================================================

            if (dto.FacturaDetalles != null &&
                dto.FacturaDetalles.Any())
            {
                foreach (var item in dto.FacturaDetalles)
                {

                    try
                    {
                        await _facturaDetalleRepository
                        .Save(new FacturaDetalles
                        {
                            IdFacturaHeader =
                                entity.IdFacturaHeader,

                            // 🔥 SERVICIO GENÉRICO
                            IdProducto =
                                servicio?.IdProducto ?? 0,

                            Cantidad =
                                item.Cantidad,

                            SubTotal =
                                item.SubTotal,

                            TipoMasa =
                                item.TipoMasa,

                            TipoRelleno =
                                item.TipoRelleno,

                            Libras =
                                item.Libras
                        });
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"Error guardando encargo: {ex.Message}"
                        );
                    }

                }
            }

            // =====================================================
            // 🔥 CLIENTE
            // =====================================================


            // =====================================================
            // 🔥 INGRESOS
            // =====================================================

            if (abono > 0)
            {
                foreach (var p in pagosAgrupados)
                {
                    await _ingresos.InsertIngreso(
                        new Ingresos
                        {
                            IdEmpresa =
                                entity.IdEmpresa,

                            FechaRegistro =
                                DateTime.Now,

                            Descripcion =
                                $"Abono Encargo #{entity.IdFacturaHeader}",

                            Categoria =
                                "Abono Encargo",

                            Origen =
                                "Bizcocho",

                            Monto =
                                p.Monto,

                            FormaPago =
                                p.Metodo,

                            Referencia =
                                $"Encargo #{entity.IdFacturaHeader} - {dto.cliente}",

                            IdFacturaHeader =
                                entity.IdFacturaHeader,

                            IdCliente =
                                entity.IDCliente
                        });

                    // ============================================
                    // 🔥 MOVIMIENTO FINANCIERO
                    // ============================================

                    var metodoConfigurado =
                        await _MetodoPagoCuentaService
                        .GetByMetodoAsync(
                            entity.IdEmpresa,
                            p.Metodo
                        );

                    if (
                        metodoConfigurado != null
                        &&
                        metodoConfigurado.IdCuentaFinanciera > 0
                    )
                    {
                        await _MovimientoFinancieroService
                        .RegistrarEntradaAsync(

                            entity.IdEmpresa,

                            0,

                            metodoConfigurado
                            .IdCuentaFinanciera,

                            p.Monto,

                            $"Encargo #{entity.IdFacturaHeader}",

                            $"Abono automático de encargo ({p.Metodo})"
                        );
                    }
                }
            }

            return entity.IdFacturaHeader;
        }

        // 🔥 UPDATE
        public async Task<bool> UpdateAsync(UpdateBizcochoEncargoDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Datos inválidos");

            var entity = await GetEncargoAsync(dto.IdFacturaHeader, dto.IdEmpresa);
            if (entity == null)
                return false;

            entity.Total = dto.Total;
            entity.Abono = dto.Abono;
            entity.Pagado = dto.Pagado;
            entity.Pendiente = dto.Pendiente;
            entity.Nota = dto.Nota;
            entity.FechaEntrega = dto.FechaEntrega;
            entity.HoraEntrega = dto.HoraEntrega;

            if (dto.FechaBencimiento.HasValue)
                entity.FechaBencimiento = dto.FechaBencimiento.Value;
            else if (dto.FechaEntrega.HasValue)
                entity.FechaBencimiento = dto.FechaEntrega.Value;

            entity.Estado = entity.Abono >= entity.Total
                ? "Pagado"
                : "Pendiente";

            _repository.Update(entity.IdFacturaHeader, entity);
            return true;
        }

        // 🔥 DELETE
        public async Task<bool> DeleteAsync(int id, int idEmpresa)
        {
            var entity = await GetEncargoAsync(id, idEmpresa);
            if (entity == null)
                return false;

            _repository.Delete(id);
            await Task.CompletedTask;
            return true;
        }
        public async Task<FacturaHeaderDto?> GetEncargoPrintByIdAsync(
     int IdFacturaHeader,
     int IdEmpresa)
        {
            // 🔥 HEADER
            var x = await _repository.GetByExpresionAsync(h =>
                h.IdFacturaHeader == IdFacturaHeader &&
                h.IdEmpresa == IdEmpresa
                );

            if (x == null)
                return null;

            var _Empresa = _RepositoryEmpresa.GetById(x.IdEmpresa);
           

            // 🔥 CLIENTE
            Clientes? cliente = null;

            if (x.IDCliente.HasValue && x.IDCliente.Value > 0)
            {
                cliente = await _clientesRepository
                    .GetByIdAsync(x.IDCliente.Value);
            }

            // 🔥 DETALLES
            var detalles = await _facturaDetalleRepository
                .GetAllByExpresionAsync(d =>

                    d.IdFacturaHeader ==
                    x.IdFacturaHeader

                );

            // 🔥 DTO
            var listaDetalles = new List<FacturaDetallesDto>();

            foreach (var d in detalles)
            {
                // 🔥 PRODUCTO
                var prod = await _RproductosServices
                    .GetByExpresionAsync(p =>

                        p.IdProducto ==
                        d.IdProducto

                    );

                listaDetalles.Add(
                    new FacturaDetallesDto
                    {
                        IdFacturaDetalle = d.IdFacturaDetalle,
                        IdFacturaHeader = d.IdFacturaHeader,
                        IdProducto = d.IdProducto,

                        Cantidad = d.Cantidad,
                        SubTotal = d.SubTotal,
                        Itbis = d.Itbis,
                        // 🔥 BIZCOCHO
                        Libras = d.Libras,
                        TipoMasa = d.TipoMasa,
                        TipoRelleno = d.TipoRelleno,

                        // 🔥 PRODUCTO
                        Productos = prod
                    }
                );
            }

            // 🔥 RETURN
            return new FacturaHeaderDto
            {
                // HEADER
                IdFacturaHeader = x.IdFacturaHeader,
                IdEmpresa = x.IdEmpresa,
                IDCliente = x.IDCliente,
                IdTipoDocumentos = x.IdTipoDocumentos,
                Politicas=_Empresa.Politicas,
                NumeroDocumento = x.NumeroDocumento,
                NombreEmpresa = x.NombreEmpresa,
                TipoFactura = x.TipoFactura,
                FormaPago = x.FormaPago,
                Estado = x.Estado,
                RNC = x.RNC,
                Nota = x.Nota,
                NCF = x.NCF,
                
                // FECHAS
                FechaInseccion = x.FechaInseccion,
                FechaBencimiento = x.FechaBencimiento,

                FechaEntrega = x.FechaEntrega,
                HoraEntrega = x.HoraEntrega,

                // MONTOS
                Total = x.Total,
                SubTotal = x.SubTotal,
                TotalItbis = x.TotalItbis,
                TotalDescuento = x.TotalDescuento,

                Pagado = x.Pagado,
                Pendiente = x.Pendiente,
                Abono = x.Abono,

                Balance = x.Total - x.Pagado,

                // 🔥 EMPRESA
                Empresa = _Empresa.NombreComercial,
                DireccionEmpresa = _Empresa.Direccion,
                RNCEmpresa = _Empresa.RNC,

                TelefonoEmpresa =
        _Empresa.Telefono,

                // 🔥 CLIENTE
                cliente =
        cliente?.NombreComercial
        ?? "",

                celular =
        cliente?.Telefono
        ?? cliente?.Celular
        ?? "",

                Clientes = cliente == null
        ? null
        : new ClienteDto
        {
            IdCliente = cliente.IDCliente,
            NombreComercial = cliente.NombreComercial,
            Telefono = cliente.Telefono,
            Celular = cliente.Celular,
            CedulaRNC = cliente.CedulaRNC
        },

                // 🔥 DETALLES
                FacturaDetalles = listaDetalles
            };
        }
        // 🔥 GET BY ID
        public async Task<FacturaHeaders?> GetByIdAsync(int id, int idEmpresa)
        {
            return await GetEncargoAsync(id, idEmpresa);
        }

        // 🔥 TODOS
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllAsync(int IdEmpresa)
        {
            var headers =
 await _repository
 .GetAllByExpresionAsync(x =>

     x.IdEmpresa == IdEmpresa

     &&

     (
         x.IdTipoDocumentos == 14

         ||

         (
             x.IdTipoDocumentos == 1

             &&

             x.FechaEntrega != null
         )
     )
 );

            var result = new List<FacturaHeaderDto>();

            foreach (var x in headers)
            {
                // 🔥 CONSULTAR CLIENTE APARTE
                Clientes? cliente = null;

                if (x.IDCliente.HasValue && x.IDCliente.Value > 0)
                {
                    cliente = await _clientesRepository.GetByIdAsync(x.IDCliente.Value);
                }

                // 🔥 CONSULTAR DETALLES APARTE
                var detalles = await _facturaDetalleRepository.GetAllByExpresionAsync(d =>
                    d.IdFacturaHeader == x.IdFacturaHeader
                );

                result.Add(new FacturaHeaderDto
                {
                    // HEADER
                    IdFacturaHeader = x.IdFacturaHeader,
                    IdEmpresa = x.IdEmpresa,
                    IDCliente = x.IDCliente,
                    IdTipoDocumentos = x.IdTipoDocumentos,
                    NumeroDocumento = x.NumeroDocumento,
                    TipoFactura = x.TipoFactura,
                    FormaPago = x.FormaPago,
                    Estado = x.Estado,
                    Nota = x.Nota,
                    NCF = x.NCF,

                    // FECHAS
                    FechaInseccion = x.FechaInseccion,
                    FechaBencimiento = x.FechaBencimiento,
                    FechaEntrega = x.FechaEntrega,
                    HoraEntrega = x.HoraEntrega,

                    // MONTOS
                    Total = x.Total,
                    SubTotal = x.SubTotal,
                    TotalItbis = x.TotalItbis,
                    TotalDescuento = x.TotalDescuento,
                    Pagado = x.Pagado,
                    Pendiente = x.Pendiente,
                    Abono = x.Abono,
                    Balance = x.Total - x.Pagado,

                    // CLIENTE
                    cliente = cliente?.NombreComercial ?? "",
                    celular = cliente?.Telefono ?? cliente?.Celular ?? "",

                    Clientes = cliente == null
                        ? null
                        : new ClienteDto
                        {
                            IdCliente = cliente.IDCliente,
                            NombreComercial = cliente.NombreComercial,
                            Telefono = cliente.Telefono,
                            Celular = cliente.Celular,
                            CedulaRNC = cliente.CedulaRNC
                        },

                    // DETALLES
                    FacturaDetalles = detalles.Select(d => new FacturaDetallesDto
                    {
                        IdFacturaDetalle = d.IdFacturaDetalle,
                        IdFacturaHeader = d.IdFacturaHeader,
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        SubTotal = d.SubTotal,
                        Libras = d.Libras,
                        TipoMasa = d.TipoMasa,
                        TipoRelleno = d.TipoRelleno
                    }).ToList()
                });
            }

            return result;
        }

        // 🔥 PENDIENTES
        public async Task<IEnumerable<FacturaHeaders>> GetPendientesAsync(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                x.Estado == "Pendiente");
        }

        // 🔥 POR FECHA
        public async Task<IEnumerable<FacturaHeaders>> GetPorFechaAsync(int idEmpresa, DateTime fecha)
        {
            return await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                x.FechaEntrega.HasValue &&
                x.FechaEntrega.Value.Date == fecha.Date);
        }

        // 🔥 BUSCAR
        public async Task<IEnumerable<FacturaHeaders>> BuscarAsync(int idEmpresa, string filtro)
        {
            filtro = filtro?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(filtro))
            {
                return await _repository.GetAllByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa &&
                    x.IdTipoDocumentos == TipoDocumentoEncargo);
            }

            var clientes = await _clientesRepository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa &&
                ((c.NombreComercial ?? "").Contains(filtro) ||
                 (c.CedulaRNC ?? "").Contains(filtro) ||
                 (c.Telefono ?? "").Contains(filtro)));

            var idsClientes = clientes
                .Select(c => c.IDCliente)
                .ToList();

            return await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                ((x.IDCliente.HasValue && idsClientes.Contains(x.IDCliente.Value)) ||
                 (x.NCF ?? "").Contains(filtro) ||
                 (x.Nota ?? "").Contains(filtro)));
        }

        // 💳 REGISTRAR PAGO
        public async Task RegistrarPagoAsync(
           int idEmpresa,
           int idEncargo,
           decimal monto,
           decimal itbis,
           decimal totalPago,
           string formaPago,
           string? tipoComprobante,
           string? rnc,
           string? nombreEmpresa)
        {
            if (idEmpresa <= 0)
                throw new ArgumentException("idEmpresa es obligatorio");

            if (idEncargo <= 0)
                throw new ArgumentException("Encargo inválido");

            if (monto <= 0)
                throw new ArgumentException("Monto inválido");

            var entity =
                await GetEncargoAsync(
                    idEncargo,
                    idEmpresa
                );

            if (entity == null)
                throw new KeyNotFoundException(
                    "Encargo no encontrado"
                );

            // ==========================================
            // 🔥 BALANCE ACTUAL
            // ==========================================

            decimal pendienteActual =
                entity.Total - entity.Abono;

            if (pendienteActual <= 0)
            {
                throw new Exception(
                    "Este encargo ya está totalmente pagado"
                );
            }

            // ==========================================
            // 🔥 SOLO COBRAR LO PENDIENTE
            // ==========================================

            decimal montoAplicar =
                Math.Min(
                    monto,
                    pendienteActual
                );

            // ==========================================
            // 🔥 PAGOS
            // ==========================================

            entity.Abono += montoAplicar;

            if (entity.Abono > entity.Total)
            {
                entity.Abono =
                    entity.Total;
            }

            entity.Pagado =
                entity.Abono;

            entity.Balance =
                entity.Total -
                entity.Pagado;

            entity.Pendiente =
                entity.Balance;

            entity.FormaPago =
                formaPago;

            // ==========================================
            // 🔥 FISCALES
            // ==========================================

            if (
                !string.IsNullOrWhiteSpace(
                    tipoComprobante
                )
                &&
                tipoComprobante !=
                "Sin Comprobante"
            )
            {
                entity.TotalItbis =
                    itbis;

                entity.RNC =
                    rnc;

                entity.NombreEmpresa =
                    nombreEmpresa;

                if (
                    string.IsNullOrWhiteSpace(
                        entity.NCF
                    )
                )
                {
                    entity.NCF =
                        await _Secuencias
                        .GenerarNCF(
                            entity.IdEmpresa,
                            tipoComprobante
                        );
                }
            }

            // ==========================================
            // 🔥 ESTADO
            // ==========================================

            if (entity.Abono >= entity.Total)
            {
                entity.Estado =
                    "Pagado";

                entity.TipoFactura =
                    "Contado";

                entity.IdTipoDocumentos =
                    1;

                if (
                    string.IsNullOrWhiteSpace(
                        entity.NumeroDocumento
                    )
                )
                {
                    entity.NumeroDocumento =
                        await _secuenciaDocumentoService
                        .GenerarDocumentoAsync(
                            entity.IdEmpresa,
                            1
                        );
                }
            }
            else
            {
                entity.Estado =
                    "Pendiente";
            }

            // ==========================================
            // 🔥 UPDATE
            // ==========================================

            entity.Clientes = null;
            entity.FechaInseccion = DateTime.Now;

            _repository.Update(
                entity.IdFacturaHeader,
                entity
            );

            // ==========================================
            // 🔥 INGRESO
            // ==========================================

            await _ingresos.InsertIngreso(
                new Ingresos
                {
                    IdFacturaHeader =
                        entity.IdFacturaHeader,

                    IdEmpresa =
                        entity.IdEmpresa,

                    FechaRegistro =
                        DateTime.Now,

                    Descripcion =
                        $"Pago Encargo #{entity.IdFacturaHeader}",

                    Categoria =
                        "Pago Encargo",

                    Origen =
                        "Bizcocho",

                    Monto =
                        montoAplicar,

                    FormaPago =
                        formaPago,

                    Referencia =
                        $"Encargo #{entity.IdFacturaHeader}",

                    IdCliente =
                        entity.IDCliente
                });

            // ==========================================
            // 🔥 MOVIMIENTO FINANCIERO
            // ==========================================

            var metodoConfigurado =
                await _MetodoPagoCuentaService
                .GetByMetodoAsync(
                    entity.IdEmpresa,
                    formaPago
                );

            if (
                metodoConfigurado != null
                &&
                metodoConfigurado.IdCuentaFinanciera > 0
            )
            {
                await _MovimientoFinancieroService
                    .RegistrarEntradaAsync(

                        entity.IdEmpresa,

                        0,

                        metodoConfigurado
                        .IdCuentaFinanciera,

                        montoAplicar,

                        $"Encargo #{entity.IdFacturaHeader}",

                        $"Pago automático de encargo ({formaPago})"
                    );
            }
        }
        // 📦 ENTREGAR
        public async Task<bool> MarcarComoEntregadoAsync(int idEncargo, int idEmpresa)
        {
            var entity = await GetEncargoAsync(idEncargo, idEmpresa);
            if (entity == null)
                return false;

            entity.Estado = "Entregado";
            _repository.Update(entity.IdFacturaHeader, entity);
            await Task.CompletedTask;
            return true;
        }

        // 💰 TOTAL COBRADO HOY
        public async Task<decimal> GetTotalCobradoHoyAsync(int idEmpresa)
        {
            var hoy = DateTime.Now.Date;

            var data = await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                x.Abono > 0 &&
                x.FechaInseccion.Date == hoy);

            return data.Sum(x => x.Abono);
        }

        // 🔥 PAGADOS DEL DÍA
        public async Task<IEnumerable<FacturaHeaders>> GetPagadosDelDiaAsync(int idEmpresa)
        {
            var hoy = DateTime.Now.Date;

            return await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                x.Estado == "Pagado" &&
                x.FechaEntrega.HasValue &&
                x.FechaEntrega.Value.Date == hoy);
        }

        // 📊 FACTURAS
        public async Task<IEnumerable<FacturaDto>> GetFacturasPagadasAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta)
        {
            var data = await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdTipoDocumentos == TipoDocumentoEncargo &&
                x.FechaInseccion.Date >= desde.Date &&
                x.FechaInseccion.Date <= hasta.Date &&
                x.Estado == "Pagado");

            return data.Select(f => new FacturaDto
            {
                NumeroFactura = "0000" + f.IdFacturaHeader,

                Fecha = f.FechaInseccion,

                MetodoPago = f.FormaPago,

                Total = f.Total,

                NCF = f.NCF,

                RNC = f.Clientes.CedulaRNC
            }).ToList();
        }
    
    public async Task<CierreEncargoDiaDto>
GetCierreDelDiaAsync(int idEmpresa)
        {
            var hoy = DateTime.Now.Date;

            // Encargos creados hoy
            var encargosHoy =
                await _repository.GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa == idEmpresa

                        &&

                        x.FechaInseccion.Date == hoy

                        &&

                        (
                            x.IdTipoDocumentos == 14

                            ||

                            (
                                x.IdTipoDocumentos == 1
                                &&
                                x.FechaEntrega != null
                            )
                        )
                );

            // Cobros del día
            var ingresosHoy =
                await _ingresos.GetIngresosEncargosPorFecha(
                    idEmpresa,
                    hoy,
                    hoy
                );

            return new CierreEncargoDiaDto
            {
                Fecha = hoy,

                CantidadEncargosNuevos =
                    encargosHoy.Count(),

                TotalEncargosNuevos =
                    encargosHoy.Sum(x => x.Total),

                TotalCobradoHoy =
                    ingresosHoy.Sum(x =>(decimal) x.Total),

                TotalPendiente =
                    encargosHoy.Sum(x => x.Pendiente),

                TotalEntregados =
                    encargosHoy.Count(x =>
                        x.Estado == "Entregado"),

                TotalPendientes =
                    encargosHoy.Count(x =>
                        x.Estado == "Pendiente"),

                TotalVencidos =
                    encargosHoy.Count(x =>

                        x.FechaEntrega.HasValue

                        &&

                        x.FechaEntrega.Value.Date < hoy

                        &&

                        x.Estado != "Entregado"
                    ),

                MetodosPago = ingresosHoy

                    .GroupBy(x => x.FormaPago)

                    .Select(g => new
                        CierreEncargoMetodoPagoDto
                    {
                        FormaPago = g.Key,

                        Total = g.Sum(x => (decimal)x.Total)
                    })

                    .ToList(),

                Encargos = encargosHoy

                    .Select(x => new
                        CierreEncargoDetalleDto
                    {
                        IdFacturaHeader =
                                x.IdFacturaHeader,

                        NumeroDocumento =
                                x.NumeroDocumento,

                        Cliente =
                                x.Clientes?.NombreComercial
                                ?? "",

                        Celular =
                                x.Clientes?.Telefono
                                ?? "",

                        FechaEntrega =
                                x.FechaEntrega,

                        Total =
                                x.Total,

                        Abonado =
                                x.Abono,

                        Pendiente =
                                x.Pendiente,

                        Estado =
                                x.Estado
                    })

                    .ToList()
            };
        }
    } 
}