using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class PedidoOnlineMenuDto
    {
        public string Slug { get; set; } = "";
        public Guid GuidPublico { get; set; }
        public string NombrePublico { get; set; } = "";
        public string? WhatsApp { get; set; }
        public string? LogoUrl { get; set; }
        public string? NombreEmpresa { get; set; }
        public List<PedidoOnlineCategoriaDto> Categorias { get; set; } = new();
        public List<PedidoOnlineProductoDto> Productos { get; set; } = new();
        public List<PedidoOnlineGuarnicionDto> Guarniciones { get; set; } = new();
    }

    public class PedidoOnlineGuarnicionDto
    {
        public int IdGuarnicion { get; set; }
        public string Nombre { get; set; } = "";
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
        public bool ManejaGuarniciones { get; set; }
    }

    public class PedidoOnlineCheckoutRequest
    {
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string TipoEntrega { get; set; } = "Delivery";
        public string? Direccion { get; set; }
        public string? Referencia { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public string? VoucherBase64 { get; set; }
        public string? Observacion { get; set; }
        public string? IdempotencyKey { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool PagarDeliveryAlRecibir { get; set; }
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
        public decimal CostoDelivery { get; set; }
        public decimal DistanciaDeliveryKm { get; set; }
        public bool DeliveryCobrarAlEntregar { get; set; }
        public decimal MontoTransferir { get; set; }
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
        public List<ClienteDireccionDto> Direcciones { get; set; } = new();
    }

    public class ClienteDireccionDto
    {
        public int IdClienteDireccion { get; set; }
        public string Direccion { get; set; } = "";
        public string? Referencia { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? NombreDestinatario { get; set; }
        public string? Telefono { get; set; }
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
        public bool TieneVoucher { get; set; }
        public bool PagoValidado { get; set; }
        public DateTime? FechaValidacionPago { get; set; }
        public bool EnviadoCocina { get; set; }
        public string? Observacion { get; set; }
        public decimal Total { get; set; }
        public decimal CostoDelivery { get; set; }
        public decimal DistanciaDeliveryKm { get; set; }
        public bool DeliveryCobrarAlEntregar { get; set; }
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

    public class ValidarPagoPedidoRequest
    {
        public int IdUsuario { get; set; }
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
        public Guid GuidPublico { get; set; }
        public string NombrePublico { get; set; } = "";
        public string? WhatsApp { get; set; }
        public bool Activo { get; set; }
    }

    public class DeliveryCotizacionRequest
    {
        public string TipoEntrega { get; set; } = "Delivery";
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
    }

    public class DeliveryCotizacionDto
    {
        public decimal DistanciaKm { get; set; }
        public int? DuracionSegundos { get; set; }
        public decimal CostoDelivery { get; set; }
        public string? Proveedor { get; set; }
        public bool FueraDeZona { get; set; }
        public bool PuedeConfirmar { get; set; }
        public bool CalculoAutomatico { get; set; }
        public string? Mensaje { get; set; }
    }

    public class DeliveryConfigGuardarRequest
    {
        public string Alcance { get; set; } = "empresa";
        public bool DeliveryHabilitado { get; set; }
        public bool CalculoPorDistancia { get; set; }
        public decimal TarifaBase { get; set; }
        public decimal PrecioPorKilometro { get; set; }
        public decimal CostoMinimoDelivery { get; set; }
        public decimal RedondeoDelivery { get; set; }
        public decimal? DistanciaMaximaKm { get; set; }
        public string AccionFueraDeZona { get; set; } = "SIN_LIMITE";
        public string Proveedor { get; set; } = "OSRM";
        public int MinutosCache { get; set; } = 30;
        public bool ExigirGps { get; set; } = true;
        public int? IdSucursalCanal { get; set; }
    }

    public class DeliverySucursalOpcionDto
    {
        public int IdSucursal { get; set; }
        public string Nombre { get; set; } = "";
        public bool EsPrincipal { get; set; }
        public bool TieneCoordenadas { get; set; }
    }

    public class DeliveryConfigPantallaDto
    {
        public bool Existe { get; set; }
        public string Alcance { get; set; } = "ninguna";
        public bool DeliveryHabilitado { get; set; }
        public bool CalculoPorDistancia { get; set; }
        public decimal TarifaBase { get; set; }
        public decimal PrecioPorKilometro { get; set; }
        public decimal CostoMinimoDelivery { get; set; }
        public decimal RedondeoDelivery { get; set; }
        public decimal? DistanciaMaximaKm { get; set; }
        public string AccionFueraDeZona { get; set; } = "SIN_LIMITE";
        public string Proveedor { get; set; } = "OSRM";
        public int MinutosCache { get; set; }
        public bool ExigirGps { get; set; } = true;
        public int IdSucursalSesion { get; set; }
        public int? IdSucursalCanal { get; set; }
        public string? NombreSucursalCanal { get; set; }
        public bool SucursalTieneCoordenadas { get; set; }
        public bool EmpresaTieneCoordenadas { get; set; }
        public List<DeliverySucursalOpcionDto> Sucursales { get; set; } = new();
    }
}
