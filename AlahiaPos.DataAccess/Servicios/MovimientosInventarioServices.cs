using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
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

        // ======================================================
        // 🔥 CONSTRUCTOR
        // ======================================================

        public MovimientosInventarioServices(

            IRepository<MovimientosInventario> repository,

            IRepository<MovimientosInventarioDetalle>
                detalleRepository,

            IRepository<Productos> productos
        )
        {
            _repository = repository;

            _detalleRepository = detalleRepository;

            _productos = productos;
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

                    // =========================================
                    // 🔥 STOCK ACTUAL
                    // =========================================

                    decimal stockActual =
                        Convert.ToDecimal(
                            producto.Cantidad);

                    // =========================================
                    // 🔥 STOCK ANTERIOR
                    // =========================================

                    item.StockAnterior =
                        stockActual;

                    // =========================================
                    // 🔥 ENTRADA
                    // =========================================

                    if (
                        movimiento.TipoMovimiento
                        == "ENTRADA"
                    )
                    {

                        producto.Cantidad =
                            stockActual
                            +
                            item.Cantidad;
                    }

                    // =========================================
                    // 🔥 SALIDA
                    // =========================================

                    else if (
                        movimiento.TipoMovimiento
                        == "SALIDA"
                    )
                    {

                        decimal nuevoStock =
                            stockActual
                            -
                            item.Cantidad;

                        // =====================================
                        // 🔥 VALIDAR NEGATIVO
                        // =====================================

                        if (nuevoStock < 0)
                        {
                            throw new Exception(
                                $"Stock insuficiente para: {producto.Nombre}");
                        }

                        producto.Cantidad =
                            nuevoStock;
                    }

                    // =========================================
                    // 🔥 STOCK NUEVO
                    // =========================================

                    item.StockNuevo =
                        Convert.ToDecimal(
                            producto.Cantidad);

                    // =========================================
                    // 🔥 SUBTOTAL
                    // =========================================

                    item.SubTotal =
                        Convert.ToDecimal(
                            item.Precio
                        )
                        *
                        item.Cantidad;

                    // =========================================
                    // 🔥 UPDATE PRODUCTO
                    // =========================================

                    _productos.Update(
                        producto.IdProducto,
                        producto);
                }

                // =============================================
                // 🔥 GUARDAR CABECERA
                // =============================================

                await _repository.Save(movimiento);

                // =============================================
                // 🔥 LIMPIAR NUEVAMENTE
                // =============================================

                

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

     int? idUsuario
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
                        ),

                    // =====================================
                    // 🔥 INCLUDE
                    // =====================================

                    "Usuario",
                    "Detalles",
                    "Detalles.Producto"
                );

            // =============================================
            // 🔥 DTO
            // =============================================

            return result

                .OrderByDescending(x => x.Fecha)

                .Select(x =>

                    new MovimientoInventarioHistorialDto
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
                            x.Usuario != null
                            ?
                            x.Usuario.UserName
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
                    }

                ).ToList();
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