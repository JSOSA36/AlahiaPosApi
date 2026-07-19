using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Produccion
{
    /// <summary>
    /// Adapter del módulo POS. Construye snapshot en memoria y publica al outbox.
    /// El motor de producción no se invoca desde aquí; solo el contrato de evento.
    /// </summary>
    public class ProduccionPosAdapter : IProduccionPosAdapter
    {
        public const int IdTipoDocumentoOrden = 10;
        public const string IdempotencyPrefix = "Produccion:POS_ORDEN";

        private readonly IDomainEventPublisher _publisher;
        private readonly IProduccionConfiguracionService _config;
        private readonly IEmpresaModulos _empresaModulos;
        private readonly IModulo _modulos;
        private readonly IProductos _productos;
        private readonly ILogger<ProduccionPosAdapter> _logger;

        public ProduccionPosAdapter(
            IDomainEventPublisher publisher,
            IProduccionConfiguracionService config,
            IEmpresaModulos empresaModulos,
            IModulo modulos,
            IProductos productos,
            ILogger<ProduccionPosAdapter> logger)
        {
            _publisher = publisher;
            _config = config;
            _empresaModulos = empresaModulos;
            _modulos = modulos;
            _productos = productos;
            _logger = logger;
        }

        public static string BuildIdempotencyKey(int idEmpresa, int idFacturaHeader)
            => $"{IdempotencyPrefix}:{idEmpresa}:{idFacturaHeader}";

        public async Task PublicarOrdenSiAplicaAsync(FacturaHeaders header, int? origenIdAnterior = null)
        {
            if (header == null || header.IdEmpresa <= 0 || header.IdFacturaHeader <= 0)
                return;

            if (header.IdTipoDocumentos != IdTipoDocumentoOrden)
                return;

            try
            {
                if (!await MotorDisponibleAsync(header.IdEmpresa))
                    return;

                var items = ConstruirItems(header);
                if (items.Count == 0)
                {
                    _logger.LogWarning(
                        "POS_ORDEN sin ítems nombrados. FacturaHeader {Id} empresa {Empresa}",
                        header.IdFacturaHeader, header.IdEmpresa);
                    return;
                }

                var esActualizacion = origenIdAnterior.HasValue
                    && origenIdAnterior.Value > 0
                    && origenIdAnterior.Value != header.IdFacturaHeader;

                if (esActualizacion)
                {
                    var upd = ConstruirEventoActualizacion(header, origenIdAnterior!.Value, items);
                    await _publisher.PublishAsync(upd);
                }
                else
                {
                    var crear = ConstruirEventoCreacion(header, items);
                    await _publisher.PublishAsync(crear);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Fallo aislado al publicar evento de producción. FacturaHeader {Id} empresa {Empresa}",
                    header.IdFacturaHeader, header.IdEmpresa);
            }
        }

        private async Task<bool> MotorDisponibleAsync(int idEmpresa)
        {
            if (!await _config.EstaActivoAsync(idEmpresa))
                return false;

            var modulo = await _modulos.GetModuloByCodigo(ProduccionConstantes.CodigoModulo);
            if (modulo == null)
                return false;

            return await _empresaModulos.EmpresaTieneModulo(idEmpresa, modulo.Id);
        }

        private ProduccionTrabajoSolicitadoEvent ConstruirEventoCreacion(
            FacturaHeaders header,
            List<ProduccionTrabajoItemSolicitudDto> items)
        {
            var nombreVisible = ResolverNombreVisible(header);
            var tipoOrden = FormatearTipoOrden(header.TipoOrden);

            return new ProduccionTrabajoSolicitadoEvent
            {
                IdEmpresa = header.IdEmpresa,
                IdUsuario = header.IdUsuario ?? 0,
                Fecha = DateTime.Now,
                ReferenciaId = header.IdFacturaHeader,
                ReferenciaTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                TipoTrabajo = ProduccionConstantes.TipoPosOrden,
                OrigenModulo = ProduccionConstantes.OrigenModuloPos,
                OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                OrigenId = header.IdFacturaHeader,
                IdempotencyKey = BuildIdempotencyKey(header.IdEmpresa, header.IdFacturaHeader),
                NumeroVisible = string.IsNullOrWhiteSpace(header.NumeroDocumento)
                    ? header.IdFacturaHeader.ToString()
                    : header.NumeroDocumento.Trim(),
                NombreVisible = nombreVisible,
                Referencia = tipoOrden,
                EtiquetaContexto = tipoOrden,
                Observacion = string.IsNullOrWhiteSpace(header.Nota) ? null : header.Nota.Trim(),
                IdUsuarioSolicita = header.IdUsuario,
                Prioridad = ProduccionConstantes.PrioridadNormal,
                PlantillaCodigo = null,
                Items = items
            };
        }

        private ProduccionTrabajoActualizadoEvent ConstruirEventoActualizacion(
            FacturaHeaders header,
            int origenIdAnterior,
            List<ProduccionTrabajoItemSolicitudDto> items)
        {
            var nombreVisible = ResolverNombreVisible(header);
            var tipoOrden = FormatearTipoOrden(header.TipoOrden);

            return new ProduccionTrabajoActualizadoEvent
            {
                IdEmpresa = header.IdEmpresa,
                IdUsuario = header.IdUsuario ?? 0,
                Fecha = DateTime.Now,
                ReferenciaId = header.IdFacturaHeader,
                ReferenciaTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                TipoTrabajo = ProduccionConstantes.TipoPosOrden,
                OrigenModulo = ProduccionConstantes.OrigenModuloPos,
                OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                OrigenId = header.IdFacturaHeader,
                OrigenIdAnterior = origenIdAnterior,
                IdempotencyKey = BuildIdempotencyKey(header.IdEmpresa, header.IdFacturaHeader),
                NumeroVisible = string.IsNullOrWhiteSpace(header.NumeroDocumento)
                    ? header.IdFacturaHeader.ToString()
                    : header.NumeroDocumento.Trim(),
                NombreVisible = nombreVisible,
                Referencia = tipoOrden,
                EtiquetaContexto = tipoOrden,
                Observacion = string.IsNullOrWhiteSpace(header.Nota) ? null : header.Nota.Trim(),
                IdUsuarioSolicita = header.IdUsuario,
                Prioridad = ProduccionConstantes.PrioridadNormal,
                PlantillaCodigo = null,
                Items = items
            };
        }

        private List<ProduccionTrabajoItemSolicitudDto> ConstruirItems(FacturaHeaders header)
        {
            var items = new List<ProduccionTrabajoItemSolicitudDto>();
            var detalles = header.FacturaDetalles?.ToList() ?? new List<FacturaDetalles>();

            foreach (var d in detalles)
            {
                var nombre = ResolverNombreItem(d);
                if (string.IsNullOrWhiteSpace(nombre))
                    continue;

                items.Add(new ProduccionTrabajoItemSolicitudDto
                {
                    OrigenDetalleId = d.IdFacturaDetalle > 0 ? d.IdFacturaDetalle : null,
                    CodigoItem = ResolverCodigoItem(d),
                    NombreItem = nombre.Trim(),
                    Cantidad = d.Cantidad <= 0 ? 1 : d.Cantidad,
                    Observacion = string.IsNullOrWhiteSpace(d.Comentario) ? null : d.Comentario.Trim(),
                    VariacionesTexto = ConstruirVariaciones(d),
                    EstacionCodigo = null
                });
            }

            return items;
        }

        private string ResolverNombreItem(FacturaDetalles d)
        {
            if (d.Productos != null && !string.IsNullOrWhiteSpace(d.Productos.Nombre))
                return d.Productos.Nombre;

            try
            {
                var prod = _productos.GetProductoById(d.IdProducto);
                return prod?.Nombre ?? $"Producto {d.IdProducto}";
            }
            catch
            {
                return $"Producto {d.IdProducto}";
            }
        }

        private string? ResolverCodigoItem(FacturaDetalles d)
        {
            if (d.Productos != null && !string.IsNullOrWhiteSpace(d.Productos.CodigoBarra))
                return d.Productos.CodigoBarra.Trim();

            try
            {
                var prod = _productos.GetProductoById(d.IdProducto);
                return string.IsNullOrWhiteSpace(prod?.CodigoBarra) ? null : prod.CodigoBarra.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static string? ConstruirVariaciones(FacturaDetalles d)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(d.TipoMasa))
                partes.Add($"Masa: {d.TipoMasa.Trim()}");
            if (!string.IsNullOrWhiteSpace(d.TipoRelleno))
                partes.Add($"Relleno: {d.TipoRelleno.Trim()}");
            if (d.Libras > 0)
                partes.Add($"Libras: {d.Libras:0.##}");
            return partes.Count == 0 ? null : string.Join(" | ", partes);
        }

        private static string ResolverNombreVisible(FacturaHeaders header)
        {
            if (!string.IsNullOrWhiteSpace(header.NombreCuenta))
                return header.NombreCuenta.Trim();
            if (header.Clientes != null && !string.IsNullOrWhiteSpace(header.Clientes.NombreComercial))
                return header.Clientes.NombreComercial.Trim();
            return "Sin nombre";
        }

        /// <summary>
        /// Etiqueta legible para el tablero (Llevar / Comer aquí / Delivery…).
        /// </summary>
        private static string? FormatearTipoOrden(string? tipoOrden)
        {
            if (string.IsNullOrWhiteSpace(tipoOrden))
                return null;

            return tipoOrden.Trim() switch
            {
                "Llevar" => "Para llevar",
                "ComerAqui" => "Comer aquí",
                "Delivery" => "Delivery",
                "DeliveryExterno" => "Delivery externo",
                _ => tipoOrden.Trim()
            };
        }
    }
}
