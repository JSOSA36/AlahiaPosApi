using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class PedidoOnlineMenuDto
    {
        public string Slug { get; set; } = "";
        public string NombrePublico { get; set; } = "";
        public string? WhatsApp { get; set; }
        public string? LogoUrl { get; set; }
        public string? NombreEmpresa { get; set; }
        public List<PedidoOnlineCategoriaDto> Categorias { get; set; } = new();
        public List<PedidoOnlineProductoDto> Productos { get; set; } = new();
    }

    public class PedidoOnlineCategoriaDto
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = "";
        public string? ImagenPath { get; set; }
    }

    public class PedidoOnlineProductoDto
    {
        public int IdProducto { get; set; }
        public int? IdCategoria { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? Imagen { get; set; }
        public decimal Precio { get; set; }
        public decimal Itbis { get; set; }
        public decimal PrecioConItbis { get; set; }
        public bool EsServicio { get; set; }
    }

    public class PedidoOnlineCheckoutRequest
    {
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string TipoEntrega { get; set; } = "Delivery";
        public string? Direccion { get; set; }
        public string? Referencia { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public string? Observacion { get; set; }
        public string? IdempotencyKey { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public List<PedidoOnlineLineaRequest> Lineas { get; set; } = new();
    }

    public class PedidoOnlineLineaRequest
    {
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; } = 1;
        public string? Observacion { get; set; }
    }

    public class PedidoOnlineConfirmacionDto
    {
        public int IdPedidoOnline { get; set; }
        public int IdFacturaHeader { get; set; }
        public string NumeroPedido { get; set; } = "";
        public decimal Total { get; set; }
        public string TipoEntrega { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string EstadoUnificado { get; set; } = "";
        public string Mensaje { get; set; } = "";
    }

    public class PedidoOnlineSeguimientoDto
    {
        public int IdPedidoOnline { get; set; }
        public string NumeroPedido { get; set; } = "";
        public string TipoEntrega { get; set; } = "";
        public string EstadoCocina { get; set; } = "";
        public string EstadoLogistico { get; set; } = "";
        public string EstadoUnificado { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public List<PedidoDeliveryItemDto> Items { get; set; } = new();
    }

    public class PedidoOnlinePerfilDto
    {
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string? Direccion { get; set; }
        public string? Referencia { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int Pedidos { get; set; }
    }

    public class PedidoOnlinePerfilRequest
    {
        public string Telefono { get; set; } = "";
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Referencia { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
    }

    public class PedidoOnlineHistorialItemDto
    {
        public int IdPedidoOnline { get; set; }
        public string NumeroPedido { get; set; } = "";
        public string TipoEntrega { get; set; } = "";
        public string EstadoUnificado { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public string? Direccion { get; set; }
        public List<PedidoDeliveryItemDto> Items { get; set; } = new();
    }

    public class PedidoDeliveryListadoDto
    {
        public int IdPedidoOnline { get; set; }
        public int IdFacturaHeader { get; set; }
        public string NumeroPedido { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string TipoEntrega { get; set; } = "";
        public string? Direccion { get; set; }
        public string? Referencia { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string MetodoPago { get; set; } = "";
        public string? Observacion { get; set; }
        public decimal Total { get; set; }
        public string EstadoCocina { get; set; } = "";
        public string EstadoLogistico { get; set; } = "";
        public string EstadoUnificado { get; set; } = "";
        public int? IdUsuarioRepartidor { get; set; }
        public string? NombreRepartidor { get; set; }
        public DateTime Fecha { get; set; }
        public List<PedidoDeliveryItemDto> Items { get; set; } = new();
    }

    public class PedidoDeliveryItemDto
    {
        public string Nombre { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string? Observacion { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class DeliveryRepartidorDto
    {
        public int IdRepartidor { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string UserName { get; set; } = "";
        public bool Disponible { get; set; }
        public bool Activo { get; set; }
        public int PedidosActivos { get; set; }
    }

    public class DeliveryAsignarRequest
    {
        public int IdPedidoOnline { get; set; }
        public int IdUsuarioRepartidor { get; set; }
        public int IdUsuarioAsigna { get; set; }
    }

    public class DeliveryTransicionRequest
    {
        public int IdUsuario { get; set; }
        public string Estado { get; set; } = "";
    }

    public class DeliveryRepartidorUpsertRequest
    {
        public int IdUsuario { get; set; }
        public bool Disponible { get; set; } = true;
        public bool Activo { get; set; } = true;
    }

    public class PedidoOnlineCanalEmpresaDto
    {
        public int IdCanal { get; set; }
        public string Slug { get; set; } = "";
        public string NombrePublico { get; set; } = "";
        public string? WhatsApp { get; set; }
        public bool Activo { get; set; }
    }
}
