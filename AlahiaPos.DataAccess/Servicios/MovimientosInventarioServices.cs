using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class MovimientosInventarioServices
        : IMovimientosInventarioService
    {

        // ======================================================
        // 🔥 REPOSITORIES
        // ======================================================

        IRepository<MovimientosInventario>
            _repository;

        IRepository<MovimientosInventarioDetalle>
            _detalleRepository;

        IRepository<Productos>
            _productos;

        IAlmacenExistencia
            _almacenExistencia;

        IAlmacenes
            _almacenes;

        IRepository<Usuarios>
            _usuarios;

        private readonly IContabilidadEventPublisher _contabilidadEvents;

        // ======================================================
        // 🔥 CONSTRUCTOR
        // ======================================================

        public MovimientosInventarioServices(

            IRepository<MovimientosInventario> repository,

            IRepository<MovimientosInventarioDetalle>
                detalleRepository,

            IRepository<Productos> productos,

            IAlmacenExistencia almacenExistencia,

            IAlmacenes almacenes,

            IRepository<Usuarios> usuarios,

            IContabilidadEventPublisher contabilidadEvents
        )
        {
            _repository = repository;

            _detalleRepository = detalleRepository;

            _productos = productos;

            _almacenExistencia = almacenExistencia;

            _almacenes = almacenes;

            _usuarios = usuarios;

            _contabilidadEvents = contabilidadEvents;
        }

        private static string ResolverNombreUsuario(
            Usuarios? usuario)
        {
            if (usuario == null)
            {
                return "Usuario";
            }

            if (
                usuario.Empleado != null
                &&
                !string.IsNullOrWhiteSpace(
                    usuario.Empleado.Nombre)
            )
            {
                return usuario.Empleado.Nombre.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                usuario.UserName))
            {
                return usuario.UserName.Trim();
            }

            return "Usuario";
        }

        // ======================================================
        // 🔥 GUARDAR MOVIMIENTO
        // ======================================================

        public async Task<MovimientosInventario>
    GuardarMovimiento(
        MovimientosInventario movimiento)
        {

            try
            {

                // =============================================
                // 🔥 VALIDAR DETALLES
                // =============================================

                if (
                    movimiento.Detalles == null ||

                    !movimiento.Detalles.Any()
                )
                {
                    throw new Exception(
                        "Debe agregar productos.");
                }

                if (!movimiento.IdAlmacen.HasValue ||
                    movimiento.IdAlmacen.Value <= 0)
                {
                    var principal =
                        await _almacenes.GetAlmacenPrincipal(
                            movimiento.IdEmpresa
                        );

                    if (principal == null)
                    {
                        throw new Exception(
                            "Debe seleccionar un almacén.");
                    }

                    movimiento.IdAlmacen =
                        principal.IdAlmacen;
                }

                var esTransferencia =
                    movimiento.TipoMovimiento
                    == "TRANSFERENCIA";

                if (esTransferencia)
                {
                    if (
                        !movimiento.IdAlmacenDestino.HasValue
                        ||
                        movimiento.IdAlmacenDestino.Value <= 0
                    )
                    {
                        throw new Exception(
                            "Debe seleccionar el almacén destino.");
                    }

                    if (
                        movimiento.IdAlmacenDestino.Value
                        ==
                        movimiento.IdAlmacen.Value
                    )
                    {
                        throw new Exception(
                            "El almacén origen y destino deben ser diferentes.");
                    }

                    if (string.IsNullOrWhiteSpace(
                        movimiento.Motivo))
                    {
                        movimiento.Motivo =
                            "TRANSFERENCIA";
                    }
                }

                // =============================================
                // 🔥 LIMPIAR NAVEGACION
                // =============================================

                movimiento.Detalles
                .ToList()
                .ForEach(x =>

                    x.Producto = null
                );

                // =============================================
                // 🔥 RECORRER DETALLES
                // =============================================

                foreach (
                    var item in movimiento.Detalles.ToList()
                )
                {

                    // =========================================
                    // 🔥 PRODUCTO
                    // =========================================

                    var producto =
                        _productos.GetById(
                            item.IdProducto);

                    if (producto == null)
                    {
                        throw new Exception(
                            $"Producto no encontrado: {item.IdProducto}");
                    }

                    var existencia =
                        await _almacenExistencia.GetExistencia(
                            movimiento.IdAlmacen.Value,
                            item.IdProducto,
                            movimiento.IdEmpresa
                        );

                    decimal stockAlmacen =
                        existencia?.Cantidad ?? 0;

                    decimal stockTotal =
                        await _almacenExistencia.GetTotalPorProducto(
                            item.IdProducto,
                            movimiento.IdEmpresa
                        );

                    item.StockAnterior = stockTotal;

                    if (esTransferencia)
                    {
                        if (
                            stockAlmacen - item.Cantidad < 0
                        )
                        {
                            throw new Exception(
                                $"Stock insuficiente en el almacén origen para: {producto.Nombre}");
                        }

                        await _almacenExistencia.AjustarExistencia(
                            movimiento.IdAlmacen.Value,
                            item.IdProducto,
                            movimiento.IdEmpresa,
                            -item.Cantidad
                        );

                        await _almacenExistencia.AjustarExistencia(
                            movimiento.IdAlmacenDestino.Value,
                            item.IdProducto,
                            movimiento.IdEmpresa,
                            item.Cantidad
                        );

                        item.StockNuevo =
                            await _almacenExistencia.GetTotalPorProducto(
                                item.IdProducto,
                                movimiento.IdEmpresa
                            );
                    }
                    else
                    {
                    decimal delta =
                        movimiento.TipoMovimiento == "ENTRADA"
                            ? item.Cantidad
                            : -item.Cantidad;

                    if (
                        movimiento.TipoMovimiento == "SALIDA"
                        &&
                        stockAlmacen - item.Cantidad < 0
                    )
                    {
                        throw new Exception(
                            $"Stock insuficiente en el almacén para: {producto.Nombre}");
                    }

                    await _almacenExistencia.AjustarExistencia(
                        movimiento.IdAlmacen.Value,
                        item.IdProducto,
                        movimiento.IdEmpresa,
                        delta
                    );

                    item.StockNuevo =
                        await _almacenExistencia.GetTotalPorProducto(
                            item.IdProducto,
                            movimiento.IdEmpresa
                        );
                    }

                    item.SubTotal =
                        Convert.ToDecimal(
                            item.Precio
                        )
                        *
                        item.Cantidad;
                }

                // =============================================
                // 🔥 GUARDAR CABECERA
                // =============================================

                await _repository.Save(movimiento);

                var monto = movimiento.Detalles?.Sum(d => d.SubTotal) ?? 0;
                if (monto <= 0 && movimiento.Detalles != null)
                {
                    foreach (var d in movimiento.Detalles)
                    {
                        var p = _productos.GetById(d.IdProducto);
                        if (p != null)
                            monto += p.PrecioCompra * d.Cantidad;
                    }
                }

                await _contabilidadEvents.TryPublishAsync(new InventarioMovimientoRegistradoEvent
                {
                    IdEmpresa = movimiento.IdEmpresa,
                    IdUsuario = movimiento.IdUsuario ?? 0,
                    Fecha = movimiento.Fecha == default ? DateTime.Now : movimiento.Fecha,
                    ReferenciaId = movimiento.Id,
                    ReferenciaTipo = "Inventario",
                    TipoMovimiento = movimiento.TipoMovimiento ?? string.Empty,
                    Motivo = movimiento.Motivo,
                    Monto = monto
                });

                return movimiento;
            }

            catch (Exception ex)
            {

                // =============================================
                // 🔥 ERROR DETALLADO
                // =============================================

                throw new Exception(

                    "Error Guardando Movimiento: "

                    +

                    ex.Message

                    +

                    " | INNER: "

                    +

                    ex.InnerException?.Message
                );
            }
        }

        // ======================================================
        // 🔥 OBTENER POR ID
        // ======================================================
        public async Task<
     List<MovimientoInventarioHistorialDto>>
     FiltrarHistorial(

     int idEmpresa,

     DateTime? desde,

     DateTime? hasta,

     string? tipoMovimiento,

     string? motivo,

     int? idUsuario,

     int? idProducto
 )
        {

            var result =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        == idEmpresa

                        &&

                        (
                            desde == null ||

                            x.Fecha.Date >=
                            desde.Value.Date
                        )

                        &&

                        (
                            hasta == null ||

                            x.Fecha.Date <=
                            hasta.Value.Date
                        )

                        &&

                        (
                            string.IsNullOrEmpty(
                                tipoMovimiento
                            )

                            ||

                            x.TipoMovimiento
                            == tipoMovimiento
                        )

                        &&

                        (
                            string.IsNullOrEmpty(
                                motivo
                            )

                            ||

                            x.Motivo
                            == motivo
                        )

                        &&

                        (
                            idUsuario == null ||

                            x.IdUsuario
                            == idUsuario
                        )

                        &&

                        (
                            !idProducto.HasValue
                            ||
                            idProducto.Value <= 0
                            ||
                            x.Detalles.Any(
                                d =>
                                    d.IdProducto
                                    == idProducto.Value
                            )
                        ),

                    // =====================================
                    // 🔥 INCLUDE
                    // =====================================

                    "Usuario",
                    "Usuario.Empleado",
                    "Detalles",
                    "Detalles.Producto"
                );

            // =============================================
            // 🔥 DTO
            // =============================================

            var almacenes =
                (await _almacenes.GetAllAlmacenes(idEmpresa))
                .ToDictionary(
                    x => x.IdAlmacen,
                    x => x.Nombre
                );

            var lista =
                result
                .OrderByDescending(x => x.Fecha)
                .ToList();

            var idsUsuarios =
                lista
                .Where(
                    x =>
                        x.IdUsuario.HasValue
                        &&
                        x.IdUsuario.Value > 0
                )
                .Select(x => x.IdUsuario!.Value)
                .Distinct()
                .ToList();

            var mapaUsuarios =
                new Dictionary<int, string>();

            if (idsUsuarios.Any())
            {
                var usuariosDb =
                    await _usuarios
                    .GetAllByExpresionAsync(
                        u =>
                            idsUsuarios.Contains(
                                u.IdUsuario
                            ),
                        "Empleado"
                    );

                mapaUsuarios =
                    usuariosDb.ToDictionary(
                        u => u.IdUsuario,
                        ResolverNombreUsuario
                    );
            }

            return lista
                .Select(x =>
                {
                    var nombreUsuario =
                        x.IdUsuario.HasValue
                        &&
                        mapaUsuarios.TryGetValue(
                            x.IdUsuario.Value,
                            out var nombreMapa
                        )
                        ?
                        nombreMapa
                        :
                        ResolverNombreUsuario(x.Usuario);

                    return new MovimientoInventarioHistorialDto
                    {
                        Id =
                            x.Id,

                        TipoMovimiento =
                            x.TipoMovimiento,

                        Motivo =
                            x.Motivo,

                        Referencia =
                            x.Referencia,

                        Observacion =
                            x.Observacion,

                        Fecha =
                            x.Fecha,

                        IdUsuario =
                            x.IdUsuario,

                        Usuario =
                            nombreUsuario,

                        NombreUsuario =
                            nombreUsuario,

                        IdAlmacen =
                            x.IdAlmacen,

                        NombreAlmacen =
                            x.IdAlmacen.HasValue
                            &&
                            almacenes.TryGetValue(
                                x.IdAlmacen.Value,
                                out var nombreAlmacen
                            )
                            ?
                            nombreAlmacen
                            :
                            "",

                        IdAlmacenDestino =
                            x.IdAlmacenDestino,

                        NombreAlmacenDestino =
                            x.IdAlmacenDestino.HasValue
                            &&
                            almacenes.TryGetValue(
                                x.IdAlmacenDestino.Value,
                                out var nombreDestino
                            )
                            ?
                            nombreDestino
                            :
                            "",

                        Detalles =

                            x.Detalles
                            .Select(d =>

                                new MovimientoInventarioDetalleDto
                                {
                                    IdProducto =
                                        d.IdProducto,

                                    Producto =
                                        d.Producto != null
                                        ?
                                        d.Producto.Nombre
                                        :
                                        "",

                                    Cantidad =
                                        d.Cantidad,

                                    StockAnterior =
                                        d.StockAnterior,

                                    StockNuevo =
                                        d.StockNuevo,

                                    Precio =
                                        d.Precio,

                                    SubTotal =
                                        d.SubTotal
                                }

                            ).ToList()
                    };
                })
                .ToList();
        }
        public async Task<MovimientosInventario?>
            ObtenerPorId(int id)
        {

            return await _repository
                .GetByExpresionAsync(

                    x => x.Id == id,

                    "Detalles"
                );
        }

        // ======================================================
        // 🔥 LISTAR
        // ======================================================

        public async Task<List<MovimientosInventario>>
            Listar(int idEmpresa)
        {

            var result =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        == idEmpresa,

                    "Detalles"
                );

            return result
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        // ======================================================
        // 🔥 FILTRAR FECHA
        // ======================================================

        public async Task<List<MovimientosInventario>>
            FiltrarPorFecha(
                int idEmpresa,
                DateTime desde,
                DateTime hasta)
        {

            var result =
                await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        == idEmpresa

                        &&

                        x.Fecha.Date
                        >= desde.Date

                        &&

                        x.Fecha.Date
                        <= hasta.Date,

                    "Detalles"
                );

            return result
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        // ======================================================
        // 🔥 ELIMINAR
        // ======================================================

        public async Task<bool>
            Eliminar(int id)
        {

            var movimiento =
                _repository.GetById(id);

            if (movimiento == null)
                return false;

            // =============================================
            // 🔥 DETALLES
            // =============================================

            var detalles =
                _detalleRepository
                .GetAllByExpresionNoAsync(

                    x =>
                    x.IdMovimientoInventario == id
                );

            // =============================================
            // 🔥 REVERSAR STOCK
            // =============================================

            foreach (var item in detalles)
            {

                var producto =
                    _productos.GetById(
                        item.IdProducto);

                if (producto == null)
                    continue;

                decimal stockActual =
                    Convert.ToDecimal(
                        producto.Cantidad);

                // =========================================
                // 🔥 REVERSAR ENTRADA
                // =========================================

                if (
                    movimiento.TipoMovimiento
                    == "ENTRADA"
                )
                {

                    producto.Cantidad =
                        stockActual
                        -
                        item.Cantidad;
                }

                // =========================================
                // 🔥 REVERSAR SALIDA
                // =========================================

                else
                {

                    producto.Cantidad =
                        stockActual
                        +
                        item.Cantidad;
                }

                _productos.Update(
                    producto.IdProducto,
                    producto);

                _detalleRepository.Delete(item.Id);
            }

            // =============================================
            // 🔥 CONTABILIDAD: reverso del ajuste (no-op si apagada)
            // =============================================

            await _contabilidadEvents.TryPublishAsync(new InventarioMovimientoAnuladoEvent
            {
                IdEmpresa = movimiento.IdEmpresa,
                IdUsuario = movimiento.IdUsuario ?? 0,
                Fecha = DateTime.Now,
                ReferenciaId = movimiento.Id,
                ReferenciaTipo = "Inventario",
                Motivo = "Eliminación ajuste"
            });

            // =============================================
            // 🔥 ELIMINAR HEADER
            // =============================================

            _repository.Delete(id);

            return true;
        }

        // ======================================================
        // 🔥 VALIDAR STOCK
        // ======================================================

        public async Task<bool>
            ValidarStock(
                int idProducto,
                decimal cantidad)
        {

            var producto =
                _productos.GetById(idProducto);

            if (producto == null)
                return false;

            decimal stockActual =
                Convert.ToDecimal(
                    producto.Cantidad);

            return stockActual >= cantidad;
        }

        // ======================================================
        // 🔥 UPDATE STOCK
        // ======================================================

        public async Task<bool>
            ActualizarStockProducto(
                int idProducto,
                decimal nuevoStock)
        {

            var producto =
                _productos.GetById(idProducto);

            if (producto == null)
                return false;

            producto.Cantidad =
                nuevoStock;

            _productos.Update(
                producto.IdProducto,
                producto);

            return true;
        }

        // ======================================================
        // 🔥 KARDEX
        // ======================================================

        public async Task<List<MovimientosInventarioDetalle>>
            KardexProducto(
                int idProducto,
                DateTime? desde,
                DateTime? hasta)
        {

            var result =
                await _detalleRepository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdProducto
                        == idProducto

                        &&

                        (
                            desde == null ||

                            x.Fecha.Date >=
                            desde.Value.Date
                        )

                        &&

                        (
                            hasta == null ||

                            x.Fecha.Date <=
                            hasta.Value.Date
                        ),

                    "MovimientoInventario"
                );

            return result
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        // ======================================================
        // 🔥 STOCK BAJO
        // ======================================================

        public async Task<List<Productos>>
            ProductosStockBajo(
                int idEmpresa)
        {

            var result =
                await _productos
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        == idEmpresa

                        &&

                        x.ControlarStock
                        == true

                        &&

                        Convert.ToDecimal(
                            x.Cantidad)
                        <=
                        Convert.ToDecimal(
                            x.Stock)
                );

            return result.ToList();
        }
    }
}