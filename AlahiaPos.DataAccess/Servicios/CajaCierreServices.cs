using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CajaCierreServices
        : ICajaCierreService
    {

        IRepository<CajaCierre> _repository;

        IRepository<Usuarios> _usuarioRepository;

        IRepository<CajaApertura> _repositoryApertura;

        IFacturaHeader _facturaRepository;

        IRepository<FacturaDetalles> _detalleRepository;

        IRepository<Productos> _productoRepository;
        IGastos _IGastos;
        IIngresos _Ingresos;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly ICuentaFinancieraService _cuentaFinancieraService;

        public CajaCierreServices(

    IRepository<CajaCierre> repository,
    IGastos Gastos,
    IIngresos Ingresos,
    IRepository<CajaApertura> repositoryApertura,

    IRepository<Usuarios> usuarioRepository,

     IFacturaHeader facturaRepository,

    IRepository<FacturaDetalles> detalleRepository,

    IRepository<Productos> productoRepository,
    IContabilidadEventPublisher contabilidadEvents,
    IMovimientoFinancieroService movimientoFinancieroService,
    ICuentaFinancieraService cuentaFinancieraService
)
        {
            _repository = repository;
            _IGastos= Gastos;
            _Ingresos= Ingresos;
            _repositoryApertura = repositoryApertura;

            _usuarioRepository = usuarioRepository;

            _facturaRepository = facturaRepository;

            _detalleRepository = detalleRepository;

            _productoRepository = productoRepository;
            _contabilidadEvents = contabilidadEvents;
            _movimientoFinancieroService = movimientoFinancieroService;
            _cuentaFinancieraService = cuentaFinancieraService;
        }

        /* =====================================
        🔥 CREATE
        ====================================== */

        public async Task<int> CreateAsync(
            CajaCierre entity
        )
        {
            await _repository.Save(entity);

            return entity.IdCajaCierre;
        }

        /* =====================================
        🔥 UPDATE
        ====================================== */

        public async Task UpdateAsync(
            CajaCierre entity
        )
        {
            _repository.Update(

                entity.IdCajaCierre,

                entity
            );

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 DELETE
        ====================================== */

        public async Task DeleteAsync(
            int id
        )
        {
            _repository.Delete(id);

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 GET BY ID
        ====================================== */

        public async Task<CajaCierre?>
            GetByIdAsync(int id)
        {
            return await _repository
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 GET ALL
        ====================================== */
        public async Task<IEnumerable<CajaListadoDto>> GetAllAsync()
        {
            /* =====================================
            🔥 CIERRES
            ===================================== */

            var cierres =
                await _repository.GetAllAsync();

            /* =====================================
            🔥 APERTURAS
            ===================================== */

            var aperturas =
                await _repositoryApertura.GetAllAsync();

            /* =====================================
            🔥 USUARIOS
            ===================================== */

            var usuarios =
                await _usuarioRepository.GetAllAsync();

            /* =====================================
            🔥 LISTADO
            ===================================== */

            var listado = new List<CajaListadoDto>();

            foreach (var cierre in cierres)
            {
                var apertura =
                    aperturas.FirstOrDefault(x =>
                        x.IdCajaApertura == cierre.IdCajaApertura);

                if (apertura == null)
                    continue;

                var usuarioApertura =
                    usuarios.FirstOrDefault(x =>
                        x.IdUsuario == apertura.IdUsuario);

                var usuarioCierre =
                    usuarios.FirstOrDefault(x =>
                        x.IdUsuario == cierre.IdUsuario);

                /* =====================================
                🔥 PRODUCTOS
                ===================================== */

                var productosVendidos =
                    await _facturaRepository
                    .GetProductosPorCajaCierre(
                        apertura.IdEmpresa,
                        apertura.IdUsuario,
                        cierre.IdCajaCierre
                    );

                /* =====================================
                🔥 MÉTODOS DE PAGO
                ===================================== */

                var metodosPago =
                    await _Ingresos
                    .GetIngresosByCajaCierre(
                        cierre.IdCajaCierre
                    );

                /* =====================================
                🔥 DTO
                ===================================== */

                listado.Add(new CajaListadoDto
                {
                    /* =====================================
                    🔥 IDS
                    ===================================== */

                    IdCajaCierre =
                        cierre.IdCajaCierre,

                    IdCajaApertura =
                        apertura.IdCajaApertura,

                    IdEmpresa =
                        apertura.IdEmpresa,

                    IdUsuario =
                        cierre.IdUsuario,

                    /* =====================================
                    🔥 FECHAS
                    ===================================== */

                    FechaApertura =
                        apertura.FechaApertura,

                    FechaCierre =
                        cierre.FechaCierre,

                    /* =====================================
                    🔥 APERTURA
                    ===================================== */

                    MontoInicial =
                        apertura.MontoInicial,

                    /* =====================================
                    🔥 RESUMEN
                    ===================================== */

                    VentasBrutas =
                        cierre.VentasBrutas,

                    TotalDescuento =
                        cierre.TotalDescuento,

                    TotalIngresosExtra =
                        cierre.TotalIngresosExtra,

                    TotalGastos =
                        cierre.TotalGastos,

                    TotalIngresosNetos =
                        cierre.TotalIngresosNetos,

                    /* =====================================
                    🔥 CUADRE
                    ===================================== */

                    DebeHaber =
                        cierre.DebeHaber,

                    MontoRealCaja =
                        cierre.MontoRealCaja,

                    Diferencia =
                        cierre.Diferencia,

                    Observacion =
                        cierre.Observacion,

                    Estado =
                        "CERRADA",

                    /* =====================================
                    🔥 USUARIOS
                    ===================================== */

                    UsuarioApertura =
                        usuarioApertura?.UserName,

                    UsuarioCierre =
                        usuarioCierre?.UserName,

                    /* =====================================
                    🔥 DETALLES
                    ===================================== */

                    ProductosVendidos =
                        productosVendidos,

                    MetodosPago =
                        metodosPago
                });
            }

            return listado
                .OrderByDescending(x => x.IdCajaCierre);
        }

        /* =====================================
        🔥 GET BY FECHA
        ====================================== */
        /* =====================================
 🔥 GET BY FECHA
 ===================================== */
        public async Task<IEnumerable<CajaListadoDto>>
   GetByFechaAsync(
       int idEmpresa,
       DateTime desde,
       DateTime hasta
   )
        {
            /* =====================================
            🔥 APERTURAS FILTRADAS
            ====================================== */

            var aperturas =
                await _repositoryApertura
                .GetAllByExpresionAsync(
                    x =>
                        x.IdEmpresa == idEmpresa &&
                        x.FechaApertura.Date >= desde.Date &&
                        x.FechaApertura.Date <= hasta.Date
                );

            /* =====================================
            🔥 CIERRES FILTRADOS
            ====================================== */

            var cierres =
                await _repository
                .GetAllByExpresionAsync(
                    x =>
                        x.IdEmpresa == idEmpresa &&
                        x.FechaCierre.Date >= desde.Date &&
                        x.FechaCierre.Date <= hasta.Date
                );

            /* =====================================
            🔥 USUARIOS
            ====================================== */

            var usuarios =
                await _usuarioRepository.GetAllAsync();

            var listado = new List<CajaListadoDto>();

            foreach (var apertura in aperturas)
            {
                var cierre = cierres.FirstOrDefault(
                    x => x.IdCajaApertura == apertura.IdCajaApertura
                );

                var usuarioApertura = usuarios.FirstOrDefault(
                    x => x.IdUsuario == apertura.IdUsuario
                );

                var usuarioCierre = usuarios.FirstOrDefault(
                    x => x.IdUsuario == cierre?.IdUsuario
                );

                /* =====================================
                🔥 PRODUCTOS
                ====================================== */

                List<CajaProductoDto> productosVendidos = new();

                if (cierre != null)
                {
                    productosVendidos =
                        await _facturaRepository.GetProductosPorCajaCierre(
                            apertura.IdEmpresa,
                            apertura.IdUsuario,
                            cierre.IdCajaCierre
                        );
                }
                else
                {
                    productosVendidos =
                        await _facturaRepository.GetProductosPendientesCierre(
                            apertura.IdEmpresa,
                            apertura.IdUsuario
                        );
                }

                /* =====================================
                🔥 MÉTODOS DE PAGO
                ====================================== */

                List<CajaMetodoPagoDto> metodosPago = new();

                if (cierre != null)
                {
                    metodosPago =
                        await _Ingresos
                        .GetIngresosByCajaCierre(
                            cierre.IdCajaCierre
                        );
                }
                else
                {
                    metodosPago =
                        await _Ingresos
                        .GetIngresosPendientesCaja(
                            apertura.IdEmpresa,
                            apertura.IdUsuario
                        );
                }

                /* =====================================
                🔥 DTO
                ====================================== */

                listado.Add(new CajaListadoDto
                {
                    IdCajaCierre = cierre?.IdCajaCierre ?? 0,
                    IdCajaApertura = apertura.IdCajaApertura,
                    IdEmpresa = apertura.IdEmpresa,
                    IdUsuario = apertura.IdUsuario,

                    FechaApertura = apertura.FechaApertura,
                    FechaCierre = cierre?.FechaCierre,

                    MontoInicial = apertura.MontoInicial,

                    VentasBrutas = cierre?.VentasBrutas ?? 0,
                    TotalDescuento = cierre?.TotalDescuento ?? 0,
                    TotalIngresosExtra = cierre?.TotalIngresosExtra ?? 0,
                    TotalGastos = cierre?.TotalGastos ?? 0,
                    TotalIngresosNetos = cierre?.TotalIngresosNetos ?? 0,

                    DebeHaber = cierre?.DebeHaber ?? 0,
                    MontoRealCaja = cierre?.MontoRealCaja ?? 0,
                    Diferencia = cierre?.Diferencia ?? 0,
                    Observacion = cierre?.Observacion,

                    Estado = cierre != null
                        ? "CERRADA"
                        : "ABIERTA",

                    UsuarioApertura = usuarioApertura?.UserName,
                    UsuarioCierre = usuarioCierre?.UserName,

                    ProductosVendidos = productosVendidos,
                    MetodosPago = metodosPago
                });
            }

            return listado
                .OrderByDescending(x => x.IdCajaApertura);
        }
        /* =====================================
        🔥 ÚLTIMO CIERRE
        ====================================== */

        public async Task<CajaCierre?>
            GetUltimoCierreAsync(
                int idEmpresa
            )
        {

            var cierres =

                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa
                );

            return cierres
                .OrderByDescending(

                    x => x.FechaCierre
                )
                .FirstOrDefault();
        }
        public async Task<CajaListadoDto> ImprimirCierre(
      int idCajaCierre
  )
        {
            /* =====================================
            🔥 CIERRE
            ====================================== */

            var cierre =
                await _repository.GetByIdAsync(
                    idCajaCierre
                );

            if (cierre == null)
                return null;

            /* =====================================
            🔥 APERTURA
            ====================================== */

            var apertura =
                await _repositoryApertura.GetByIdAsync(
                    cierre.IdCajaApertura
                );

            if (apertura == null)
                return null;

            /* =====================================
            🔥 USUARIOS
            ====================================== */

            var usuarios =
                await _usuarioRepository.GetAllAsync();

            var usuarioApertura =
                usuarios.FirstOrDefault(x =>
                    x.IdUsuario == apertura.IdUsuario);

            var usuarioCierre =
                usuarios.FirstOrDefault(x =>
                    x.IdUsuario == cierre.IdUsuario);

            /* =====================================
            🔥 PRODUCTOS VENDIDOS
            ====================================== */

            var productosVendidos =
                await _facturaRepository
                .GetProductosPorCajaCierre(

                    apertura.IdEmpresa,

                    apertura.IdUsuario,

                    cierre.IdCajaCierre
                );

            /* =====================================
            🔥 MÉTODOS DE PAGO
            ====================================== */

            var metodosPago =
                await _Ingresos
                .GetIngresosByCajaCierre(

                    cierre.IdCajaCierre
                );

            /* =====================================
            🔥 RETORNO
            ====================================== */

            return new CajaListadoDto
            {
                IdCajaCierre =
                    cierre.IdCajaCierre,

                IdCajaApertura =
                    apertura.IdCajaApertura,

                IdEmpresa =
                    apertura.IdEmpresa,

                IdUsuario =
                    apertura.IdUsuario,

                FechaApertura =
                    apertura.FechaApertura,

                FechaCierre =
                    cierre.FechaCierre,

                /* =====================================
                🔥 RESUMEN
                ====================================== */

                MontoInicial =
                    apertura.MontoInicial,

                VentasBrutas =
                    cierre.VentasBrutas,

                TotalDescuento =
                    cierre.TotalDescuento,

                TotalIngresosExtra =
                    cierre.TotalIngresosExtra,

                TotalGastos =
                    cierre.TotalGastos,

                TotalIngresosNetos =
                    cierre.TotalIngresosNetos,

                /* =====================================
                🔥 MÉTODOS DE PAGO
                ====================================== */

                MetodosPago =
                    metodosPago,

                /* =====================================
                🔥 CUADRE
                ====================================== */

                DebeHaber =
                    cierre.DebeHaber,

                MontoRealCaja =
                    cierre.MontoRealCaja,

                Diferencia =
                    cierre.Diferencia,

                Observacion =
                    cierre.Observacion,

                Estado =
                    "CERRADA",

                /* =====================================
                🔥 USUARIOS
                ====================================== */

                UsuarioApertura =
                    usuarioApertura?.UserName,

                UsuarioCierre =
                    usuarioCierre?.UserName,

                /* =====================================
                🔥 PRODUCTOS
                ====================================== */

                ProductosVendidos =
                    productosVendidos
            };
        }
        /* =====================================
        🔥 PROCESAR CIERRE
        ====================================== */

        public async Task<CajaCierre> ProcesarCierreAsync(
      CajaCierre model
  )
        {
            /* =====================================
            🔥 BUSCAR APERTURA
            ====================================== */

            var apertura =
                await _repositoryApertura
                .GetByIdAsync(
                    model.IdCajaApertura
                );

            if (apertura == null)
            {
                throw new Exception(
                    "Caja apertura no encontrada"
                );
            }

            /* =====================================
            🔥 VALIDAR ABIERTA
            ====================================== */

            if (apertura.Estado != "ABIERTA")
            {
                throw new Exception(
                    "La caja ya fue cerrada"
                );
            }

            /* =====================================
            🔥 OBTENER RESUMEN DEL CIERRE
            ====================================== */

            var resumen =
                await _facturaRepository
                .GetIngresosCajaAbierta(

                    model.IdEmpresa,

                    model.IdUsuario
                );

            var datos =
                resumen.FirstOrDefault()

                ??

                new CierreCajaDto();

            /* =====================================
            🔥 FECHA
            ====================================== */

            model.FechaCierre =
                DateTime.Now;

            /* =====================================
            🔥 RESUMEN DEL CIERRE
            ====================================== */

            model.VentasBrutas =
                datos.TotalVentasBrutas;

            model.TotalDescuento =
                datos.TotalDescuento;

            model.TotalIngresosExtra =
                datos.TotalIngresosExtra;

            model.TotalGastos =
                datos.TotalGastos;

            model.TotalIngresosNetos =

                model.VentasBrutas

                + model.TotalIngresosExtra

                - model.TotalDescuento

                - model.TotalGastos;

            /* =====================================
            🔥 DEBE HABER
            ====================================== */

            model.DebeHaber =

                apertura.MontoInicial

                + model.TotalEfectivo

                + model.TotalIngresosExtra

                - model.TotalGastos;

            /* =====================================
            🔥 DIFERENCIA (efectivo físico no puede ser negativo)
            ====================================== */

            var esperadoFisico =

                model.DebeHaber < 0 ? 0m : model.DebeHaber;

            model.Diferencia =

                model.MontoRealCaja

                - esperadoFisico;

            /* =====================================
            🔥 GUARDAR CIERRE
            ====================================== */

            await _repository
                .Save(model);

            /* =====================================
            🔥 CERRAR FACTURAS
            ====================================== */

            await _facturaRepository
                .CerrarFacturasPendientes(

                    model.IdEmpresa,

                    model.IdUsuario,

                    model.IdCajaCierre
                );

            /* =====================================
            🔥 CERRAR GASTOS
            ====================================== */

            await _IGastos
                .CerrarGastosPendientes(

                    model.IdEmpresa,

                    model.IdUsuario,
                    model.IdCajaCierre
                );

            /* =====================================
            🔥 CERRAR INGRESOS EXTRA
            ====================================== */

            await _Ingresos
                .CerrarIngresosPendientes(

                    model.IdEmpresa,

                    model.IdUsuario,
                     model.IdCajaCierre
                );

            /* =====================================
            🔥 CERRAR APERTURA
            ====================================== */

            apertura.Estado =
                "CERRADA";

            _repositoryApertura.Update(

                apertura.IdCajaApertura,

                apertura
            );

            // Contabilidad: solo diferencia de cuadre (sobrante/faltante). Ventas/gastos ya están contabilizados.
            try
            {
                if (Math.Abs(model.Diferencia) >= 0.01m)
                {
                    var esSobrante = model.Diferencia > 0;
                    await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
                    {
                        IdEmpresa = model.IdEmpresa,
                        IdUsuario = model.IdUsuario,
                        Fecha = model.FechaCierre == default ? DateTime.Now : model.FechaCierre,
                        ReferenciaId = model.IdCajaCierre,
                        ReferenciaTipo = "CierreCaja",
                        TipoMovimiento = esSobrante ? "CIERRE_SOBRANTE" : "CIERRE_FALTANTE",
                        Categoria = "CIERRE",
                        Monto = Math.Abs(model.Diferencia),
                        TipoCuentaOrigen = "CAJA",
                        TipoCuentaDestino = "CAJA",
                        Motivo = $"Cierre caja #{model.IdCajaCierre}"
                    });
                }
            }
            catch
            {
                // Nunca tumbar cierre operativo
            }

            // Tesorería: reflejar sobrante/faltante en cuenta CAJA principal sin duplicar asiento contable.
            try
            {
                if (Math.Abs(model.Diferencia) >= 0.01m)
                {
                    var cuentas = await _cuentaFinancieraService.GetByEmpresaAsync(model.IdEmpresa);
                    var cajaPrincipal = cuentas.FirstOrDefault(c =>
                        c.TipoCuenta == "CAJA" && c.EsPrincipal && c.Activa)
                        ?? cuentas.FirstOrDefault(c => c.TipoCuenta == "CAJA" && c.Activa);

                    if (cajaPrincipal != null)
                    {
                        var monto = Math.Abs(model.Diferencia);
                        var motivo = $"Cierre caja #{model.IdCajaCierre}";

                        if (model.Diferencia > 0)
                        {
                            await _movimientoFinancieroService.RegistrarEntradaAsync(
                                model.IdEmpresa,
                                model.IdUsuario,
                                cajaPrincipal.IdCuentaFinanciera,
                                monto,
                                motivo,
                                null,
                                categoria: "CIERRE_CAJA",
                                referenciaId: model.IdCajaCierre,
                                referenciaTipo: "CierreCaja",
                                claveIdempotencia: $"CAJA_CIERRE_{model.IdCajaCierre}_SOBRANTE");
                        }
                        else
                        {
                            await _movimientoFinancieroService.RegistrarSalidaAsync(
                                model.IdEmpresa,
                                model.IdUsuario,
                                cajaPrincipal.IdCuentaFinanciera,
                                monto,
                                motivo,
                                null,
                                categoria: "CIERRE_CAJA",
                                referenciaId: model.IdCajaCierre,
                                referenciaTipo: "CierreCaja",
                                claveIdempotencia: $"CAJA_CIERRE_{model.IdCajaCierre}_FALTANTE");
                        }
                    }
                }
            }
            catch
            {
                // Nunca tumbar cierre operativo
            }

            /* =====================================
            🔥 RETORNO
            ====================================== */

            return model;
        }
    }
}