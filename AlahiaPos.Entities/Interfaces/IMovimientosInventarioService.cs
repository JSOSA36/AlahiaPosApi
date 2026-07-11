using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IMovimientosInventarioService
    {
        Task<MovimientosInventario>
           GuardarMovimiento(
               MovimientosInventario movimiento);

        // =========================================
        // 🔥 OBTENER POR ID
        // =========================================

        Task<MovimientosInventario?>
            ObtenerPorId(int id);

        // =========================================
        // 🔥 LISTAR
        // =========================================

        Task<List<MovimientosInventario>>
            Listar(
                int idEmpresa);

        Task<List<MovimientoInventarioHistorialDto>>
    FiltrarHistorial(

    int idEmpresa,

    DateTime? desde,

    DateTime? hasta,

    string? tipoMovimiento,

    string? motivo,

    int? idUsuario,

    int? idProducto
);

        // =========================================
        // 🔥 FILTRAR POR FECHA
        // =========================================

        Task<List<MovimientosInventario>>
            FiltrarPorFecha(
                int idEmpresa,
                DateTime desde,
                DateTime hasta);

        // =========================================
        // 🔥 ELIMINAR
        // =========================================

        Task<bool>
            Eliminar(int id);

        // =========================================
        // 🔥 VALIDAR STOCK
        // =========================================

        Task<bool>
            ValidarStock(
                int idProducto,
                decimal cantidad);

        // =========================================
        // 🔥 ACTUALIZAR STOCK
        // =========================================

        Task<bool>
            ActualizarStockProducto(
                int idProducto,
                decimal nuevoStock);

        // =========================================
        // 🔥 KARDEX PRODUCTO
        // =========================================

        Task<List<MovimientosInventarioDetalle>>
            KardexProducto(
                int idProducto,
                DateTime? desde,
                DateTime? hasta);

        // =========================================
        // 🔥 PRODUCTOS STOCK BAJO
        // =========================================

        Task<List<Productos>>
            ProductosStockBajo(
                int idEmpresa);
    }
}
