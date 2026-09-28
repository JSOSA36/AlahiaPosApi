using Microsoft.Extensions.Logging;
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
        private readonly IRepository<Sucursal> _sucursales;
        private readonly IRepository<Empresas> _empresas;
        private readonly IRepository<Perfiles> _perfiles;
        private readonly INotificacionCentro _notificaciones;
        private readonly ILogger<CajaCierreServices> _logger;

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
    ICuentaFinancieraService cuentaFinancieraService,
    IRepository<Sucursal> sucursales,
    IRepository<Empresas> empresas,
    IRepository<Perfiles> perfiles,
    INotificacionCentro notificaciones,
    ILogger<CajaCierreServices> logger
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
            _sucursales = sucursales;
            _empresas = empresas;
            _perfiles = perfiles;
            _notificaciones = notificaciones;
            _logger = logger;
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

                    IdSucursal =
                        apertura.IdSucursal ?? cierre.IdSucursal,

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
                    IdSucursal = apertura.IdSucursal ?? cierre?.IdSucursal,
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
                int idEmpresa,
                int idUsuario = 0
            )
        {

            var cierres =

                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa

                        &&
                        (
                            idUsuario <= 0
                            ||
                            x.IdUsuario
                            ==
                            idUsuario
                        )
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

            var idSucursal = apertura.IdSucursal ?? cierre.IdSucursal;
            string? nombreSucursal = null;
            if (idSucursal is > 0)
            {
                nombreSucursal = _sucursales
                    .GetAllByExpresionNoAsync(s =>
                        s.IdSucursal == idSucursal.Value
                        && s.IdEmpresa == apertura.IdEmpresa)
                    .FirstOrDefault()?.Nombre;
            }

            var empresa = await _empresas.GetByIdAsync(apertura.IdEmpresa);
            var nombreEmpresa = empresa?.NombreComercial?.Trim();
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
                nombreEmpresa = null;

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

                    cierre.IdUsuario,

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

                IdSucursal = idSucursal,
                NombreSucursal = nombreSucursal,
                NombreEmpresa = nombreEmpresa,

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

            if (model.IdSucursal is null or <= 0)
                model.IdSucursal = apertura.IdSucursal;

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
               Aviso al administrador (correo)
               ===================================== */
            try
            {
                await NotificarAdministradorCierreAsync(model, apertura);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo enviar el correo de cierre de caja #{Id} empresa {Empresa}",
                    model.IdCajaCierre, model.IdEmpresa);
            }

            /* =====================================
            🔥 RETORNO
            ====================================== */

            return model;
        }

        private async Task NotificarAdministradorCierreAsync(
            CajaCierre model,
            CajaApertura apertura)
        {
            var empresa = await _empresas.GetByIdAsync(model.IdEmpresa);
            if (empresa == null)
                return;

            var correos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AgregarCorreo(string? valor)
            {
                var c = (valor ?? "").Trim();
                if (c.Contains('@'))
                    correos.Add(c);
            }

            // Solo administradores / perfil principal — no al cajero ni al correo genérico de empresa.
            var perfilesEmpresa = await _perfiles.GetAllByExpresionAsync(p =>
                p.IdEmpresa == model.IdEmpresa && p.Activo && p.Nombre != null);

            static bool EsPerfilAdmin(string? nombre)
            {
                var n = (nombre ?? "").Trim();
                if (n.Length == 0) return false;
                return n.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("Administrador", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("Principal", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("ADMIN", StringComparison.OrdinalIgnoreCase);
            }

            var idsPerfilAdmin = perfilesEmpresa
                .Where(p => EsPerfilAdmin(p.Nombre))
                .Select(p => p.IdPerfil)
                .ToHashSet();

            if (idsPerfilAdmin.Count > 0)
            {
                var admins = await _usuarioRepository.GetAllByExpresionAsync(u =>
                    u.IdEmpresa == model.IdEmpresa
                    && u.Estado
                    && idsPerfilAdmin.Contains(u.IdPerfil));

                foreach (var admin in admins)
                {
                    AgregarCorreo(admin.Correo);
                    AgregarCorreo(admin.UserName);
                }
            }

            if (correos.Count == 0)
            {
                _logger.LogWarning(
                    "Cierre #{Id} empresa {Empresa}: no hay correo de administrador (perfiles ADMIN).",
                    model.IdCajaCierre, model.IdEmpresa);
                return;
            }

            _logger.LogInformation(
                "Cierre #{Id} empresa {Empresa}: correo a {Destinos}",
                model.IdCajaCierre, model.IdEmpresa, string.Join("; ", correos));

            var usuarioCierre = (await _usuarioRepository.GetAllByExpresionAsync(u =>
                    u.IdUsuario == model.IdUsuario))
                .FirstOrDefault();

            var metodos = await _Ingresos.GetIngresosByCajaCierre(model.IdCajaCierre)
                          ?? new List<CajaMetodoPagoDto>();

            var productos = await _facturaRepository.GetProductosPorCajaCierre(
                apertura.IdEmpresa,
                model.IdUsuario,
                model.IdCajaCierre)
                ?? new List<CajaProductoDto>();

            var totalGeneral = metodos.Sum(m => m.Total);
            var efectivo = metodos
                .Where(x => CajaMetodoPagoDto.EsEfectivo(x.FormaPago))
                .Sum(x => x.Total);

            string? nombreSucursal = null;
            var idSucursal = apertura.IdSucursal ?? model.IdSucursal;
            if (idSucursal is > 0)
            {
                nombreSucursal = _sucursales
                    .GetAllByExpresionNoAsync(s =>
                        s.IdSucursal == idSucursal.Value
                        && s.IdEmpresa == model.IdEmpresa)
                    .FirstOrDefault()?.Nombre;
            }

            var nombreUsuario = usuarioCierre?.Correo
                ?? usuarioCierre?.UserName
                ?? $"Usuario #{model.IdUsuario}";

            var fechaCierre = model.FechaCierre == default ? DateTime.Now : model.FechaCierre;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CIERRE DE CAJA");
            if (!string.IsNullOrWhiteSpace(empresa.NombreComercial))
                sb.AppendLine($"Empresa:  {empresa.NombreComercial.Trim()}");
            if (!string.IsNullOrWhiteSpace(nombreSucursal))
                sb.AppendLine($"Sucursal: {nombreSucursal}");
            sb.AppendLine($"Caja #:   {model.IdCajaCierre}");
            sb.AppendLine($"Apertura: {apertura.FechaApertura:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine($"Cierre:   {fechaCierre:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine($"Usuario:  {nombreUsuario}");
            sb.AppendLine();

            sb.AppendLine("RESUMEN DE VENTAS");
            sb.AppendLine($"Ventas brutas : RD$ {model.VentasBrutas:N2}");
            sb.AppendLine($"Descuentos    : RD$ {model.TotalDescuento:N2}");
            sb.AppendLine($"Ingresos caja : RD$ {model.TotalIngresosExtra:N2}");
            sb.AppendLine($"Gastos caja   : RD$ {model.TotalGastos:N2}");
            sb.AppendLine($"TOTAL VENDIDO : RD$ {model.TotalIngresosNetos:N2}");
            sb.AppendLine();

            sb.AppendLine("FORMAS DE PAGO");
            if (metodos.Count == 0)
            {
                sb.AppendLine("Sin cobros en esta caja");
            }
            else
            {
                foreach (var m in metodos)
                {
                    var nombre = string.IsNullOrWhiteSpace(m.FormaPago) ? "Otro" : m.FormaPago.Trim();
                    sb.AppendLine($"{nombre}: RD$ {m.Total:N2}");
                }
            }
            sb.AppendLine($"TOTAL COBRADO : RD$ {totalGeneral:N2}");
            sb.AppendLine();

            sb.AppendLine("CUADRE DE CAJA");
            sb.AppendLine($"Fondo inicial : RD$ {apertura.MontoInicial:N2}");
            sb.AppendLine($"+ Ventas efect.: RD$ {efectivo:N2}");
            sb.AppendLine($"+ Ingresos caja: RD$ {model.TotalIngresosExtra:N2}");
            sb.AppendLine($"- Gastos caja  : RD$ {model.TotalGastos:N2}");
            sb.AppendLine($"DEBE HABER    : RD$ {model.DebeHaber:N2}");
            sb.AppendLine($"Total contado : RD$ {model.MontoRealCaja:N2}");
            sb.AppendLine($"DIFERENCIA    : RD$ {model.Diferencia:N2}");
            sb.AppendLine();

            if (productos.Count > 0)
            {
                sb.AppendLine("PRODUCTOS VENDIDOS");
                foreach (var item in productos)
                {
                    var nombreProd = string.IsNullOrWhiteSpace(item.Producto)
                        ? $"Producto #{item.IdProducto}"
                        : item.Producto.Trim();
                    sb.AppendLine(nombreProd);
                    sb.AppendLine(
                        $"  Cant: {item.CantidadVendida:N2}  |  Total: RD$ {item.TotalVendido:N2}  |  Exist: {item.ExistenciaActual:N2}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(model.Observacion))
            {
                sb.AppendLine("OBSERVACION");
                sb.AppendLine(model.Observacion.Trim());
            }

            await _notificaciones.PublicarAsync(new NotificacionEvento
            {
                Tipo = NotificacionTipos.CierreCaja,
                IdEmpresa = model.IdEmpresa,
                DestinoTipo = NotificacionDestinos.Empresa,
                Prioridad = model.Diferencia != 0
                    ? NotificacionPrioridades.Advertencia
                    : NotificacionPrioridades.Exito,
                Titulo = $"Cierre de caja #{model.IdCajaCierre}",
                Mensaje = sb.ToString().Trim(),
                Ruta = "/cierrecaja",
                ReferenciaTipo = "CierreCaja",
                ReferenciaId = model.IdCajaCierre,
                CorreoDestino = string.Join(";", correos),
                NombreEmpresa = empresa.NombreComercial
            });
        }
    }
}