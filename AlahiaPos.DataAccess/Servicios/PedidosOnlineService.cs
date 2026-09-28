using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PedidosOnlineService : IPedidosOnlineService
    {
        private readonly AlahiaPosContext _ctx;
        private readonly IProductos _productos;
        private readonly ICategorias _categorias;
        private readonly IClientes _clientes;
        private readonly IFacturaHeader _facturas;
        private readonly ISecuenciaDocumentoService _secuencia;
        private readonly IProduccionPosAdapter _produccion;
        private readonly IProduccionTrabajoService _trabajos;
        private readonly INotificacionCentro _notificaciones;

        public PedidosOnlineService(
            AlahiaPosContext ctx,
            IProductos productos,
            ICategorias categorias,
            IClientes clientes,
            IFacturaHeader facturas,
            ISecuenciaDocumentoService secuencia,
            IProduccionPosAdapter produccion,
            IProduccionTrabajoService trabajos,
            INotificacionCentro notificaciones)
        {
            _ctx = ctx;
            _productos = productos;
            _categorias = categorias;
            _clientes = clientes;
            _facturas = facturas;
            _secuencia = secuencia;
            _produccion = produccion;
            _trabajos = trabajos;
            _notificaciones = notificaciones;
        }

        public async Task<PedidoOnlineMenuDto> ObtenerMenuAsync(string slug)
        {
            var canal = await ObtenerCanalActivoAsync(slug);
            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == canal.IdEmpresa);

            var categorias = (await _categorias.GetAllCategoriasVentas(canal.IdEmpresa) ?? Enumerable.Empty<Categorias>())
                .Where(c => c.IsActiva && !EsCategoriaInterna(c.Nombre))
                .OrderBy(c => c.Prioridad)
                .ThenBy(c => c.Nombre)
                .Select(c => new PedidoOnlineCategoriaDto
                {
                    IdCategoria = c.IdCategoria,
                    Nombre = TextoCliente(c.Nombre),
                    ImagenPath = c.ImagenPath
                })
                .ToList();

            var guarniciones = await GuarnicionesPublicasAsync(canal.IdEmpresa);
            var pideGuarnicion = guarniciones.Count > 0;
            var idsPublicas = categorias.Select(c => c.IdCategoria).ToHashSet();
            var productos = (await _productos.GetAllProductosVenta(canal.IdEmpresa) ?? Enumerable.Empty<Productos>())
                .Where(p => p.PrecioVenta > 0 && p.IdCategoria is int idCat && idsPublicas.Contains(idCat))
                .OrderBy(p => p.Nombre)
                .Select(p =>
                {
                    var (precio, itbis) = PrecioItbis(p);
                    return new PedidoOnlineProductoDto
                    {
                        IdProducto = p.IdProducto,
                        IdCategoria = p.IdCategoria,
                        Nombre = TextoCliente(p.Nombre),
                        Descripcion = TextoCliente(p.Descripcion),
                        Imagen = FirstNonEmpty(p.Imagen1, p.Imagen2, p.Imagen3),
                        Precio = precio,
                        Itbis = itbis,
                        PrecioConItbis = precio + itbis,
                        EsServicio = p.EsServicio,
                        ManejaGuarniciones = pideGuarnicion && p.ManejaGuarniciones
                    };
                })
                .ToList();

            return new PedidoOnlineMenuDto
            {
                Slug = canal.Slug,
                GuidPublico = empresa?.GuidPublico ?? Guid.Empty,
                NombrePublico = TextoCliente(canal.NombrePublico),
                WhatsApp = canal.WhatsApp,
                LogoUrl = canal.LogoUrl,
                NombreEmpresa = TextoCliente(empresa?.NombreComercial),
                Categorias = categorias,
                Productos = productos,
                Guarniciones = guarniciones
            };
        }

        private async Task<List<PedidoOnlineGuarnicionDto>> GuarnicionesPublicasAsync(int idEmpresa)
        {
            return await _ctx.Guarniciones.AsNoTracking()
                .Where(g => g.IdEmpresa == idEmpresa && g.Activo && g.Nombre != "")
                .OrderBy(g => g.Nombre)
                .Select(g => new PedidoOnlineGuarnicionDto
                {
                    IdGuarnicion = g.IdGuarnicion,
                    Nombre = g.Nombre
                })
                .ToListAsync();
        }

        public async Task<PedidoOnlineConfirmacionDto> CrearPedidoAsync(string slug, PedidoOnlineCheckoutRequest request)
        {
            if (request == null)
                throw new ArgumentException("El pedido es obligatorio.");

            var canal = await ObtenerCanalActivoAsync(slug);
            var nombre = (request.Nombre ?? "").Trim();
            var telefono = SoloDigitos(request.Telefono);
            if (nombre.Length < 2)
                throw new ArgumentException("Indique su nombre.");
            if (telefono.Length < 10)
                throw new ArgumentException("Indique un teléfono válido.");

            var tipo = NormalizarTipoEntrega(request.TipoEntrega);
            if (tipo == PedidoOnlineTiposEntrega.Delivery && string.IsNullOrWhiteSpace(request.Direccion))
                throw new ArgumentException("La dirección es obligatoria para delivery.");

            var lineas = (request.Lineas ?? new List<PedidoOnlineLineaRequest>())
                .Where(l => l.IdProducto > 0 && l.Cantidad > 0)
                .ToList();
            if (lineas.Count == 0)
                throw new ArgumentException("Agregue al menos un producto.");

            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? null
                : request.IdempotencyKey.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                var existente = await _ctx.PedidoOnline.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdEmpresa == canal.IdEmpresa && p.IdempotencyKey == key);
                if (existente != null)
                {
                    if (EsTransferencia(existente.MetodoPago) && string.IsNullOrWhiteSpace(existente.VoucherRuta))
                    {
                        var tracked = await _ctx.PedidoOnline.FirstAsync(p => p.IdPedidoOnline == existente.IdPedidoOnline);
                        await AdjuntarVoucherAsync(tracked, request.VoucherBase64);
                        existente = tracked;
                    }
                    var fh = await _facturas.GetFacturaHeaderById(existente.IdFacturaHeader, canal.IdEmpresa);
                    var seg = await ObtenerPedidoAsync(canal.IdEmpresa, existente.IdPedidoOnline);
                    return MapConfirmacion(existente, fh, seg?.EstadoUnificado, seg?.EstadoUnificado);
                }
            }

            var cliente = await UpsertClienteAsync(canal.IdEmpresa, nombre, telefono, request.Direccion);

            var detalles = new List<FacturaDetalles>();
            foreach (var linea in lineas)
            {
                var prod = _productos.GetProductoById(linea.IdProducto);
                if (prod == null || prod.IdEmpresa != canal.IdEmpresa)
                    throw new ArgumentException("Un producto del carrito ya no está disponible.");

                var (precio, itbisUnit) = PrecioItbis(prod);
                var qty = Math.Round(linea.Cantidad, 2);
                var itbis = Math.Round(itbisUnit * qty, 2, MidpointRounding.AwayFromZero);
                var sub = Math.Round((precio * qty) + itbis, 2, MidpointRounding.AwayFromZero);

                detalles.Add(new FacturaDetalles
                {
                    IdEmpresa = canal.IdEmpresa,
                    IdProducto = prod.IdProducto,
                    Cantidad = qty,
                    PrecioOferta = precio,
                    Itbis = itbis,
                    SubTotal = sub,
                    Descuento = 0,
                    Comentario = string.IsNullOrWhiteSpace(linea.Observacion) ? "" : linea.Observacion.Trim(),
                    FechaInseccion = DateTime.Now,
                    StatuItem = false,
                    EnviadoCocina = false,
                    Productos = null
                });
            }

            var total = detalles.Sum(d => d.SubTotal);
            var totalItbis = detalles.Sum(d => d.Itbis);
            var metodo = string.IsNullOrWhiteSpace(request.MetodoPago) ? "Efectivo" : request.MetodoPago.Trim();
            (byte[] Bytes, string ContentType)? voucher = null;
            if (EsTransferencia(metodo))
                voucher = DecodificarVoucher(request.VoucherBase64);
            var lat = NormalizarCoord(request.Latitud, -90m, 90m);
            var lng = NormalizarCoord(request.Longitud, -180m, 180m);
            if (tipo != PedidoOnlineTiposEntrega.Delivery)
            {
                lat = null;
                lng = null;
            }
            var nota = ConstruirNota(tipo, request.Observacion);
            var (idEmpleado, idUsuario) = await ResolverResponsablePedidoAsync(canal.IdEmpresa);
            var idSucursal = await ResolverSucursalPrincipalAsync(canal.IdEmpresa);

            var header = new FacturaHeaders
            {
                IdEmpresa = canal.IdEmpresa,
                IdSucursal = idSucursal,
                IdTipoDocumentos = ProduccionPosAdapterTipo.Orden,
                TipoOrden = tipo,
                Estado_Orden = "Pendiente",
                Estado = "Pendiente",
                TipoFactura = "Contado",
                FormaPago = metodo,
                NombreCuenta = nombre,
                IDCliente = cliente.IDCliente,
                Nota = nota,
                SubTotal = total - totalItbis,
                TotalItbis = totalItbis,
                Total = total,
                Pendiente = total,
                Pagado = 0,
                FechaInseccion = DateTime.Now,
                FechaBencimiento = DateTime.Now,
                Hora = DateTime.Now.ToString("hh:mm tt"),
                PrintAcount = true,
                PrintPending = false,
                IdMesa = null,
                IdEmpleados = idEmpleado,
                IdEmpleadoComision = idEmpleado,
                IdUsuario = idUsuario,
                Clientes = null,
                Empleados = null,
                FacturaDetalles = detalles
            };

            header.NumeroDocumento = await _secuencia.GenerarDocumentoAsync(canal.IdEmpresa, ProduccionPosAdapterTipo.Orden);
            await _facturas.InsertFacturaHeader(header);

            var pedido = new PedidoOnline
            {
                IdEmpresa = canal.IdEmpresa,
                IdCanal = canal.IdCanal,
                IdFacturaHeader = header.IdFacturaHeader,
                IdCliente = cliente.IDCliente,
                NombreCliente = nombre,
                Telefono = telefono,
                TipoEntrega = tipo,
                Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim(),
                ReferenciaDireccion = string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim(),
                Latitud = lat,
                Longitud = lng,
                MetodoPago = metodo,
                Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim(),
                EstadoLogistico = PedidoOnlineEstados.Nuevo,
                IdempotencyKey = key,
                FechaCreacion = DateTime.Now
            };
            _ctx.PedidoOnline.Add(pedido);
            await _ctx.SaveChangesAsync();
            if (voucher.HasValue)
                await GuardarArchivoVoucherAsync(pedido, voucher.Value.Bytes, voucher.Value.ContentType);

            return MapConfirmacion(pedido, header, "Nuevo", MensajeSeguimiento("Nuevo"));
        }

        public async Task<PedidoOnlineSeguimientoDto> ObtenerSeguimientoAsync(string slug, int idPedidoOnline, string telefono)
        {
            var canal = await ObtenerCanalActivoAsync(slug);
            var tel = SoloDigitos(telefono);
            if (idPedidoOnline <= 0 || tel.Length < 10)
                throw new ArgumentException("Pedido no encontrado.");

            var pedido = await _ctx.PedidoOnline.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == idPedidoOnline && p.IdEmpresa == canal.IdEmpresa);
            if (pedido == null || SoloDigitos(pedido.Telefono) != tel)
                throw new ArgumentException("Pedido no encontrado.");

            var dto = await ObtenerPedidoAsync(canal.IdEmpresa, idPedidoOnline);
            if (dto == null)
                throw new ArgumentException("Pedido no encontrado.");

            return new PedidoOnlineSeguimientoDto
            {
                IdPedidoOnline = dto.IdPedidoOnline,
                NumeroPedido = dto.NumeroPedido,
                TipoEntrega = dto.TipoEntrega,
                EstadoCocina = dto.EstadoCocina,
                EstadoLogistico = dto.EstadoLogistico,
                EstadoUnificado = EstadoParaCliente(dto.EstadoUnificado),
                Mensaje = MensajeSeguimiento(EstadoParaCliente(dto.EstadoUnificado)),
                Total = dto.Total,
                Fecha = dto.Fecha,
                Items = dto.Items
            };
        }

        public async Task<PedidoOnlinePerfilDto> ObtenerPerfilClienteAsync(string slug, string telefono)
        {
            var (canal, tel, pedidos) = await PedidosDeClienteAsync(slug, telefono);
            var ultimo = pedidos.First();
            var cliente = await _clientes.BuscarPorTelefono(tel, canal.IdEmpresa);
            return new PedidoOnlinePerfilDto
            {
                Nombre = FirstNonEmpty(ultimo.NombreCliente, cliente?.NombreComercial) ?? "",
                Telefono = tel,
                Direccion = FirstNonEmpty(ultimo.Direccion, cliente?.Direccion),
                Referencia = ultimo.ReferenciaDireccion,
                Latitud = ultimo.Latitud,
                Longitud = ultimo.Longitud,
                Pedidos = pedidos.Count
            };
        }

        public async Task<PedidoOnlinePerfilDto> GuardarPerfilClienteAsync(string slug, PedidoOnlinePerfilRequest request)
        {
            var (canal, tel, _) = await PedidosDeClienteAsync(slug, request?.Telefono ?? "");
            var nombre = (request?.Nombre ?? "").Trim();
            var direccion = string.IsNullOrWhiteSpace(request?.Direccion) ? null : request!.Direccion.Trim();
            var cliente = await UpsertClienteAsync(canal.IdEmpresa, nombre.Length >= 2 ? nombre : "Cliente Pedir", tel, direccion);
            if (!string.IsNullOrWhiteSpace(nombre) && nombre.Length >= 2 && cliente.NombreComercial != nombre)
            {
                cliente.NombreComercial = nombre;
                _clientes.UpdateClientes(cliente.IDCliente, cliente);
            }
            if (!string.IsNullOrWhiteSpace(direccion) && cliente.Direccion != direccion)
            {
                cliente.Direccion = direccion;
                _clientes.UpdateClientes(cliente.IDCliente, cliente);
            }

            return await ObtenerPerfilClienteAsync(slug, tel);
        }

        public async Task<List<PedidoOnlineHistorialItemDto>> ListarPedidosClienteAsync(string slug, string telefono)
        {
            var (canal, _, pedidos) = await PedidosDeClienteAsync(slug, telefono);
            var ids = pedidos.Select(p => p.IdPedidoOnline).ToArray();
            var mapped = await MapearPedidosAsync(canal.IdEmpresa, null, ids);
            return mapped.Select(d =>
            {
                var unificado = EstadoParaCliente(d.EstadoUnificado);
                return new PedidoOnlineHistorialItemDto
                {
                    IdPedidoOnline = d.IdPedidoOnline,
                    NumeroPedido = d.NumeroPedido,
                    TipoEntrega = d.TipoEntrega,
                    EstadoUnificado = unificado,
                    Mensaje = MensajeSeguimiento(unificado),
                    Total = d.Total,
                    Fecha = d.Fecha,
                    Direccion = d.Direccion,
                    Items = d.Items
                };
            }).ToList();
        }

        private async Task<(PedidoOnlineCanal canal, string tel, List<PedidoOnline> pedidos)> PedidosDeClienteAsync(string slug, string telefono)
        {
            var canal = await ObtenerCanalActivoAsync(slug);
            var tel = SoloDigitos(telefono);
            if (tel.Length < 10)
                throw new ArgumentException("Indique un teléfono válido.");

            var cola = tel.Length > 10 ? tel[^10..] : tel;
            var candidatos = await _ctx.PedidoOnline.AsNoTracking()
                .Where(p => p.IdEmpresa == canal.IdEmpresa && p.Telefono != null && p.Telefono.Contains(cola))
                .OrderByDescending(p => p.FechaCreacion)
                .Take(80)
                .ToListAsync();
            var pedidos = candidatos
                .Where(p =>
                {
                    var t = SoloDigitos(p.Telefono);
                    return t == tel || t.EndsWith(cola);
                })
                .ToList();
            if (pedidos.Count == 0)
                throw new ArgumentException("No hay pedidos para este teléfono.");
            return (canal, tel, pedidos);
        }

        public async Task<PedidoOnlineCanalEmpresaDto?> ObtenerCanalEmpresaAsync(int idEmpresa)
        {
            var canal = await _ctx.PedidoOnlineCanal.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.IdCanal)
                .FirstOrDefaultAsync();
            if (canal == null)
                return null;
            var guid = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => e.GuidPublico)
                .FirstOrDefaultAsync();
            return new PedidoOnlineCanalEmpresaDto
            {
                IdCanal = canal.IdCanal,
                Slug = canal.Slug,
                GuidPublico = guid,
                NombrePublico = canal.NombrePublico,
                WhatsApp = canal.WhatsApp,
                Activo = canal.Activo
            };
        }

        public async Task<List<PedidoDeliveryListadoDto>> ListarColaDeliveryAsync(int idEmpresa)
        {
            var todos = await ListarTodosAsync(idEmpresa);
            return todos
                .Where(p => p.TipoEntrega == PedidoOnlineTiposEntrega.Delivery
                    && p.EstadoLogistico != PedidoOnlineEstados.Asignado
                    && p.EstadoLogistico != PedidoOnlineEstados.Recogido
                    && p.EstadoLogistico != PedidoOnlineEstados.EnCamino
                    && p.EstadoLogistico != PedidoOnlineEstados.Entregado
                    && p.EstadoLogistico != PedidoOnlineEstados.Cancelado)
                .ToList();
        }

        public async Task<List<PedidoDeliveryListadoDto>> ListarTodosAsync(int idEmpresa)
        {
            return await MapearPedidosAsync(idEmpresa, null);
        }

        public async Task<List<PedidoDeliveryListadoDto>> ListarMisPedidosAsync(int idEmpresa, int idUsuario)
        {
            var todos = await MapearPedidosAsync(idEmpresa, idUsuario);
            return todos
                .Where(p => p.IdUsuarioRepartidor == idUsuario
                    && p.EstadoLogistico != PedidoOnlineEstados.Entregado
                    && p.EstadoLogistico != PedidoOnlineEstados.Cancelado)
                .ToList();
        }

        public async Task<PedidoDeliveryListadoDto?> ObtenerPedidoAsync(int idEmpresa, int idPedidoOnline)
        {
            var lista = await MapearPedidosAsync(idEmpresa, null);
            return lista.FirstOrDefault(p => p.IdPedidoOnline == idPedidoOnline);
        }

        public async Task<List<DeliveryRepartidorDto>> ListarRepartidoresAsync(int idEmpresa)
        {
            var rows = await (
                from r in _ctx.DeliveryRepartidor.AsNoTracking()
                join u in _ctx.Usuarios.AsNoTracking() on r.IdUsuario equals u.IdUsuario
                join e in _ctx.EmpleadosP.AsNoTracking() on u.IdEmpleado equals e.IdEmpleados into emp
                from e in emp.DefaultIfEmpty()
                where r.IdEmpresa == idEmpresa && r.Activo
                select new { r, u, Nombre = e != null ? e.Nombre : u.UserName }
            ).ToListAsync();

            var activos = await _ctx.DeliveryAsignacion.AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activa && a.Estado != DeliveryEstados.Entregado)
                .GroupBy(a => a.IdUsuarioRepartidor)
                .Select(g => new { Id = g.Key, N = g.Count() })
                .ToListAsync();
            var mapActivos = activos.ToDictionary(x => x.Id, x => x.N);

            return rows.Select(x => new DeliveryRepartidorDto
            {
                IdRepartidor = x.r.IdRepartidor,
                IdUsuario = x.r.IdUsuario,
                Nombre = x.Nombre ?? x.u.UserName,
                UserName = x.u.UserName,
                Disponible = x.r.Disponible,
                Activo = x.r.Activo,
                PedidosActivos = mapActivos.TryGetValue(x.r.IdUsuario, out var n) ? n : 0
            }).OrderBy(x => x.Nombre).ToList();
        }

        public async Task<DeliveryRepartidorDto> UpsertRepartidorAsync(int idEmpresa, DeliveryRepartidorUpsertRequest request)
        {
            if (request == null || request.IdUsuario <= 0)
                throw new ArgumentException("Seleccione un usuario.");

            var usuario = await _ctx.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == request.IdUsuario && u.IdEmpresa == idEmpresa);
            if (usuario == null)
                throw new ArgumentException("El usuario no pertenece a esta empresa.");

            var row = await _ctx.DeliveryRepartidor
                .FirstOrDefaultAsync(r => r.IdEmpresa == idEmpresa && r.IdUsuario == request.IdUsuario);
            if (row == null)
            {
                row = new DeliveryRepartidor
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = request.IdUsuario,
                    FechaCreacion = DateTime.Now
                };
                _ctx.DeliveryRepartidor.Add(row);
            }
            row.Activo = request.Activo;
            row.Disponible = request.Disponible;
            await _ctx.SaveChangesAsync();

            var lista = await ListarRepartidoresAsync(idEmpresa);
            return lista.First(r => r.IdUsuario == request.IdUsuario);
        }

        public async Task<PedidoDeliveryListadoDto> AsignarAsync(int idEmpresa, DeliveryAsignarRequest request)
        {
            if (request == null)
                throw new ArgumentException("Datos de asignación inválidos.");

            var pedido = await _ctx.PedidoOnline
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == request.IdPedidoOnline && p.IdEmpresa == idEmpresa);
            if (pedido == null)
                throw new ArgumentException("Pedido no encontrado.");
            if (pedido.TipoEntrega != PedidoOnlineTiposEntrega.Delivery)
                throw new ArgumentException("Solo se asignan pedidos de delivery.");
            if (pedido.EstadoLogistico == PedidoOnlineEstados.Entregado)
                throw new ArgumentException("El pedido ya fue entregado.");
            if (pedido.EstadoLogistico == PedidoOnlineEstados.Cancelado)
                throw new ArgumentException("El pedido está cancelado.");

            var repartidor = await _ctx.DeliveryRepartidor
                .FirstOrDefaultAsync(r => r.IdEmpresa == idEmpresa && r.IdUsuario == request.IdUsuarioRepartidor && r.Activo);
            if (repartidor == null)
                throw new ArgumentException("El repartidor no está activo.");

            var previas = await _ctx.DeliveryAsignacion
                .Where(a => a.IdPedidoOnline == pedido.IdPedidoOnline && a.Activa)
                .ToListAsync();
            foreach (var p in previas)
                p.Activa = false;

            var asig = new DeliveryAsignacion
            {
                IdEmpresa = idEmpresa,
                IdPedidoOnline = pedido.IdPedidoOnline,
                IdUsuarioRepartidor = request.IdUsuarioRepartidor,
                IdUsuarioAsigna = request.IdUsuarioAsigna > 0 ? request.IdUsuarioAsigna : null,
                Estado = DeliveryEstados.Asignado,
                Activa = true,
                FechaAsignacion = DateTime.Now
            };
            _ctx.DeliveryAsignacion.Add(asig);
            pedido.EstadoLogistico = PedidoOnlineEstados.Asignado;
            _ctx.Entry(pedido).Property(x => x.EstadoLogistico).IsModified = true;
            await _ctx.SaveChangesAsync();

            try
            {
                await _notificaciones.PublicarAsync(new NotificacionEvento
                {
                    Tipo = NotificacionTipos.PedidoDeliveryAsignado,
                    IdEmpresa = idEmpresa,
                    DestinoTipo = "USUARIO",
                    IdUsuarioDestino = request.IdUsuarioRepartidor,
                    Prioridad = NotificacionPrioridades.Info,
                    Titulo = "Pedido asignado",
                    Mensaje = $"Te asignaron el pedido {pedido.NombreCliente}.",
                    Ruta = "/reparto",
                    ReferenciaTipo = "PedidoOnline",
                    ReferenciaId = pedido.IdPedidoOnline
                });
            }
            catch
            {
            }

            return (await ObtenerPedidoAsync(idEmpresa, pedido.IdPedidoOnline))!;
        }

        public async Task<PedidoDeliveryListadoDto> TransicionarDeliveryAsync(
            int idEmpresa,
            int idPedidoOnline,
            DeliveryTransicionRequest request)
        {
            if (request == null || request.IdUsuario <= 0)
                throw new ArgumentException("Usuario inválido.");

            var destino = (request.Estado ?? "").Trim();
            var permitidos = new[] { DeliveryEstados.Recogido, DeliveryEstados.EnCamino, DeliveryEstados.Entregado };
            if (!permitidos.Contains(destino, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Estado de delivery inválido.");

            var pedido = await _ctx.PedidoOnline
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == idPedidoOnline && p.IdEmpresa == idEmpresa);
            if (pedido == null)
                throw new ArgumentException("Pedido no encontrado.");

            var asig = await _ctx.DeliveryAsignacion
                .FirstOrDefaultAsync(a => a.IdPedidoOnline == idPedidoOnline && a.Activa);
            if (asig == null || asig.IdUsuarioRepartidor != request.IdUsuario)
                throw new ArgumentException("Este pedido no está asignado a usted.");

            destino = permitidos.First(x => x.Equals(destino, StringComparison.OrdinalIgnoreCase));
            ValidarFlujoDelivery(asig.Estado, destino);

            asig.Estado = destino;
            pedido.EstadoLogistico = destino switch
            {
                DeliveryEstados.Recogido => PedidoOnlineEstados.Recogido,
                DeliveryEstados.EnCamino => PedidoOnlineEstados.EnCamino,
                DeliveryEstados.Entregado => PedidoOnlineEstados.Entregado,
                _ => pedido.EstadoLogistico
            };
            _ctx.Entry(pedido).Property(x => x.EstadoLogistico).IsModified = true;
            if (destino == DeliveryEstados.Recogido)
                asig.FechaRecogido = DateTime.Now;
            if (destino == DeliveryEstados.EnCamino)
                asig.FechaEnCamino = DateTime.Now;
            if (destino == DeliveryEstados.Entregado)
            {
                if (asig.FechaEnCamino == null)
                    asig.FechaEnCamino = DateTime.Now;
                asig.FechaEntregado = DateTime.Now;
                asig.Activa = false;
                await IntentarEntregarCocinaAsync(idEmpresa, pedido.IdFacturaHeader, request.IdUsuario);
            }

            await _ctx.SaveChangesAsync();
            return (await ObtenerPedidoAsync(idEmpresa, idPedidoOnline))!;
        }

        private async Task IntentarEntregarCocinaAsync(int idEmpresa, int idFacturaHeader, int idUsuario)
        {
            try
            {
                var estados = await _trabajos.ObtenerEstadosPorOrigenAsync(
                    idEmpresa, ProduccionConstantes.OrigenTipoFacturaHeader, new[] { idFacturaHeader });
                var st = estados.FirstOrDefault(e => e.OrigenId == idFacturaHeader);
                if (st == null || st.CodigoEstado != "LISTA")
                    return;

                var trabajo = await _trabajos.ObtenerAsync(idEmpresa, st.IdTrabajo);
                if (trabajo == null)
                    return;

                await _trabajos.TransicionarAsync(idEmpresa, st.IdTrabajo, new ProduccionTransicionRequest
                {
                    CodigoEstadoEsperado = "LISTA",
                    CodigoEstadoNuevo = "ENTREGADA",
                    RowVersion = trabajo.RowVersion,
                    IdUsuario = idUsuario,
                    Motivo = "Entregado por delivery"
                });
            }
            catch
            {
            }
        }

        private static void ValidarFlujoDelivery(string actual, string destino)
        {
            var ok = (actual, destino) switch
            {
                (DeliveryEstados.Asignado, DeliveryEstados.Recogido) => true,
                (DeliveryEstados.Recogido, DeliveryEstados.EnCamino) => true,
                (DeliveryEstados.Recogido, DeliveryEstados.Entregado) => true,
                (DeliveryEstados.EnCamino, DeliveryEstados.Entregado) => true,
                _ => false
            };
            if (!ok)
                throw new ArgumentException($"No se puede pasar de {actual} a {destino}.");
        }

        private async Task<List<PedidoDeliveryListadoDto>> MapearPedidosAsync(
            int idEmpresa,
            int? idUsuarioFiltro,
            int[]? idsPedido = null)
        {
            var query = _ctx.PedidoOnline.AsNoTracking().Where(p => p.IdEmpresa == idEmpresa);
            if (idsPedido != null && idsPedido.Length > 0)
                query = query.Where(p => idsPedido.Contains(p.IdPedidoOnline));
            var pedidos = await query
                .OrderByDescending(p => p.FechaCreacion)
                .Take(idsPedido != null && idsPedido.Length > 0 ? 80 : 200)
                .ToListAsync();

            if (pedidos.Count == 0)
                return new List<PedidoDeliveryListadoDto>();

            var idsFh = pedidos.Select(p => p.IdFacturaHeader).Distinct().ToList();
            var headers = await _ctx.FacturaHeaders.AsNoTracking()
                .Where(f => idsFh.Contains(f.IdFacturaHeader))
                .ToListAsync();
            var detalles = await _ctx.FacturaDetalles.AsNoTracking()
                .Where(d => idsFh.Contains(d.IdFacturaHeader))
                .ToListAsync();
            var prodIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var prods = await _ctx.Productos.AsNoTracking()
                .Where(p => prodIds.Contains(p.IdProducto))
                .ToDictionaryAsync(p => p.IdProducto, p => p.Nombre ?? "");

            var asignaciones = await _ctx.DeliveryAsignacion.AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activa)
                .ToListAsync();
            if (idUsuarioFiltro.HasValue)
                asignaciones = asignaciones.Where(a => a.IdUsuarioRepartidor == idUsuarioFiltro.Value).ToList();

            var userIds = asignaciones.Select(a => a.IdUsuarioRepartidor).Distinct().ToList();
            var nombresReparto = new Dictionary<int, string>();
            if (userIds.Count > 0)
            {
                var rows = await (
                    from u in _ctx.Usuarios.AsNoTracking()
                    join e in _ctx.EmpleadosP.AsNoTracking() on u.IdEmpleado equals e.IdEmpleados into emp
                    from e in emp.DefaultIfEmpty()
                    where userIds.Contains(u.IdUsuario)
                    select new { u.IdUsuario, Nombre = e != null ? e.Nombre : u.UserName }
                ).ToListAsync();
                foreach (var r in rows)
                    nombresReparto[r.IdUsuario] = r.Nombre ?? "";
            }

            var cocina = await _trabajos.ObtenerEstadosPorOrigenAsync(
                idEmpresa, ProduccionConstantes.OrigenTipoFacturaHeader, idsFh);
            var cocinaMap = cocina.ToDictionary(c => c.OrigenId, c => c);

            var result = new List<PedidoDeliveryListadoDto>();
            foreach (var p in pedidos)
            {
                var fh = headers.FirstOrDefault(h => h.IdFacturaHeader == p.IdFacturaHeader);
                var asig = asignaciones.FirstOrDefault(a => a.IdPedidoOnline == p.IdPedidoOnline);
                cocinaMap.TryGetValue(p.IdFacturaHeader, out var coc);
                var items = detalles.Where(d => d.IdFacturaHeader == p.IdFacturaHeader)
                    .Select(d => new PedidoDeliveryItemDto
                    {
                        Nombre = prods.TryGetValue(d.IdProducto, out var n) ? n : $"Producto {d.IdProducto}",
                        Cantidad = d.Cantidad,
                        Observacion = d.Comentario,
                        SubTotal = d.SubTotal
                    })
                    .ToList();

                var estadoCocina = coc?.CodigoEstado ?? "";
                var estadoLog = SincronizarEstadoLogistico(p, estadoCocina, asig);
                var enviadoCocina = p.FechaEnvioCocina != null || !string.IsNullOrEmpty(estadoCocina);
                var unificado = UnificarEstado(p.TipoEntrega, estadoCocina, estadoLog);
                if (enviadoCocina && unificado == "Nuevo")
                    unificado = "En cocina";
                else if (!enviadoCocina && unificado == "Nuevo")
                    unificado = "Por confirmar";

                result.Add(new PedidoDeliveryListadoDto
                {
                    IdPedidoOnline = p.IdPedidoOnline,
                    IdFacturaHeader = p.IdFacturaHeader,
                    NumeroPedido = fh?.NumeroDocumento ?? p.IdPedidoOnline.ToString(),
                    NombreCliente = p.NombreCliente,
                    Telefono = p.Telefono,
                    TipoEntrega = p.TipoEntrega,
                    Direccion = p.Direccion,
                    Referencia = p.ReferenciaDireccion,
                    Latitud = p.Latitud,
                    Longitud = p.Longitud,
                    MetodoPago = p.MetodoPago,
                    TieneVoucher = !string.IsNullOrWhiteSpace(p.VoucherRuta),
                    PagoValidado = p.PagoValidado,
                    FechaValidacionPago = p.FechaValidacionPago,
                    EnviadoCocina = enviadoCocina,
                    Observacion = p.Observacion,
                    Total = fh?.Total ?? 0,
                    EstadoCocina = estadoCocina,
                    EstadoLogistico = estadoLog,
                    EstadoUnificado = unificado,
                    IdUsuarioRepartidor = asig?.IdUsuarioRepartidor,
                    NombreRepartidor = asig != null && nombresReparto.TryGetValue(asig.IdUsuarioRepartidor, out var nr) ? nr : null,
                    Fecha = p.FechaCreacion,
                    Items = items
                });
            }

            return result;
        }

        private static string SincronizarEstadoLogistico(PedidoOnline p, string cocina, DeliveryAsignacion? asig)
        {
            if (p.EstadoLogistico == PedidoOnlineEstados.Cancelado)
                return p.EstadoLogistico;
            if (asig != null)
                return asig.Estado;
            if (cocina == "LISTA" && p.EstadoLogistico == PedidoOnlineEstados.Nuevo)
                return PedidoOnlineEstados.Listo;
            return p.EstadoLogistico;
        }

        private static string UnificarEstado(string tipo, string cocina, string logistico)
        {
            var log = (logistico ?? "").Trim();
            var coc = (cocina ?? "").Trim();
            if (Es(log, PedidoOnlineEstados.Cancelado) || Es(coc, "CANCELADA")) return "Cancelado";
            if (Es(log, PedidoOnlineEstados.Entregado) || Es(coc, "ENTREGADA") || Es(coc, "ENTREGADO"))
                return "Entregado";
            if (Es(log, PedidoOnlineEstados.EnCamino) || string.Equals(log, "En camino", StringComparison.OrdinalIgnoreCase))
                return "En camino";
            if (Es(log, PedidoOnlineEstados.Recogido)) return "Recogido";
            if (Es(log, PedidoOnlineEstados.Asignado)) return "Asignado a delivery";
            if (Es(coc, "LISTA") && tipo == PedidoOnlineTiposEntrega.Delivery)
                return "Pendiente de asignación";
            if (Es(coc, "LISTA")) return "Listo";
            if (Es(coc, "EN_PREPARACION")) return "En preparación";
            if (Es(coc, "PENDIENTE") || string.IsNullOrEmpty(coc)) return "Nuevo";
            return "Nuevo";
        }

        private static bool Es(string valor, string esperado)
            => string.Equals(valor, esperado, StringComparison.OrdinalIgnoreCase);

        private static string EstadoParaCliente(string estadoUnificado)
        {
            return estadoUnificado switch
            {
                "Pendiente de asignación" or "Asignado a delivery" or "Recogido" or "En camino" => "En camino",
                _ => estadoUnificado
            };
        }

        private async Task<PedidoOnlineCanal> ObtenerCanalActivoAsync(string token)
        {
            var raw = (token ?? "").Trim();
            if (!Guid.TryParse(raw, out var guid) || guid == Guid.Empty)
                throw new ArgumentException("Este negocio no tiene pedidos en línea activos.");

            var idEmpresa = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.GuidPublico == guid)
                .Select(e => (int?)e.IdEmpresa)
                .FirstOrDefaultAsync();
            if (idEmpresa == null)
                throw new ArgumentException("Este negocio no tiene pedidos en línea activos.");

            var canal = await _ctx.PedidoOnlineCanal.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa.Value && c.Activo);
            if (canal == null)
                throw new ArgumentException("Este negocio no tiene pedidos en línea activos.");
            return canal;
        }

        private async Task<int?> ResolverSucursalPrincipalAsync(int idEmpresa)
        {
            var principal = await _ctx.Sucursales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.EsPrincipal)
                .Select(s => (int?)s.IdSucursal)
                .FirstOrDefaultAsync();
            if (principal is > 0)
                return principal;
            return await _ctx.Sucursales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa)
                .OrderBy(s => s.IdSucursal)
                .Select(s => (int?)s.IdSucursal)
                .FirstOrDefaultAsync();
        }

        private async Task<(int idEmpleados, int? idUsuario)> ResolverResponsablePedidoAsync(int idEmpresa)
        {
            var usuario = await _ctx.Usuarios.AsNoTracking()
                .Where(u => u.IdEmpresa == idEmpresa && u.Estado && u.IdEmpleado > 0)
                .OrderBy(u => u.IdUsuario)
                .FirstOrDefaultAsync();
            if (usuario != null)
            {
                var existe = await _ctx.EmpleadosP.AsNoTracking()
                    .AnyAsync(e => e.IdEmpleados == usuario.IdEmpleado && e.IdEmpresa == idEmpresa);
                if (existe)
                    return (usuario.IdEmpleado, usuario.IdUsuario);
            }

            var idEmpleado = await _ctx.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa && e.Estado)
                .OrderBy(e => e.IdEmpleados)
                .Select(e => (int?)e.IdEmpleados)
                .FirstOrDefaultAsync();
            if (idEmpleado == null || idEmpleado.Value <= 0)
                throw new ArgumentException("La empresa no tiene un empleado activo para registrar pedidos online.");

            return (idEmpleado.Value, usuario?.IdUsuario);
        }

        private async Task<Clientes> UpsertClienteAsync(int idEmpresa, string nombre, string telefono, string? direccion)
        {
            var existente = await _clientes.BuscarPorTelefono(telefono, idEmpresa);
            if (existente != null)
            {
                var dirty = false;
                if (string.IsNullOrWhiteSpace(existente.NombreComercial))
                {
                    existente.NombreComercial = nombre;
                    dirty = true;
                }
                if (!string.IsNullOrWhiteSpace(direccion) && string.IsNullOrWhiteSpace(existente.Direccion))
                {
                    existente.Direccion = direccion.Trim();
                    dirty = true;
                }
                if (dirty)
                    _clientes.UpdateClientes(existente.IDCliente, existente);
                return existente;
            }

            var nuevo = new Clientes
            {
                IdEmpresa = idEmpresa,
                NombreComercial = nombre,
                Telefono = telefono,
                Celular = telefono,
                Direccion = direccion?.Trim() ?? "",
                CedulaRNC = "",
                Email = "",
                Nota = "",
                LimiteCredito = 0,
                Estado = true,
                FechaInseccion = DateTime.Now
            };
            await _clientes.InsertClientes(nuevo);
            return nuevo;
        }

        private static (decimal precio, decimal itbis) PrecioItbis(Productos p)
        {
            var precio = Math.Round(p.PrecioVenta, 2, MidpointRounding.AwayFromZero);
            if (!p.Itbis)
                return (precio, 0);
            var tasa = p.TasaItbis ?? 18m;
            if (tasa > 0 && tasa <= 1m)
                tasa *= 100m;
            var itbis = Math.Round(precio * tasa / 100m, 2, MidpointRounding.AwayFromZero);
            return (precio, itbis);
        }

        private static string NormalizarTipoEntrega(string? tipo)
        {
            var t = (tipo ?? "").Trim();
            if (t.Equals("Llevar", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Recoger", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Pickup", StringComparison.OrdinalIgnoreCase))
                return PedidoOnlineTiposEntrega.Recoger;
            return PedidoOnlineTiposEntrega.Delivery;
        }

        private static string ConstruirNota(string tipo, string? observacion)
        {
            var partes = new List<string>
            {
                tipo == PedidoOnlineTiposEntrega.Delivery ? "Pedido online · Delivery" : "Pedido online · Recoger"
            };
            if (!string.IsNullOrWhiteSpace(observacion))
                partes.Add(observacion.Trim());
            return string.Join(" · ", partes);
        }

        private static decimal? NormalizarCoord(decimal? valor, decimal min, decimal max)
        {
            if (!valor.HasValue) return null;
            if (valor.Value < min || valor.Value > max) return null;
            if (valor.Value == 0) return null;
            return Math.Round(valor.Value, 7, MidpointRounding.AwayFromZero);
        }

        private static PedidoOnlineConfirmacionDto MapConfirmacion(
            PedidoOnline pedido,
            FacturaHeaders? fh,
            string? estadoUnificado = null,
            string? mensaje = null)
        {
            var unificado = string.IsNullOrWhiteSpace(estadoUnificado) ? pedido.EstadoLogistico : estadoUnificado;
            return new PedidoOnlineConfirmacionDto
            {
                IdPedidoOnline = pedido.IdPedidoOnline,
                IdFacturaHeader = pedido.IdFacturaHeader,
                NumeroPedido = fh?.NumeroDocumento ?? pedido.IdPedidoOnline.ToString(),
                Total = fh?.Total ?? 0,
                TipoEntrega = pedido.TipoEntrega,
                Estado = unificado,
                EstadoUnificado = unificado,
                Mensaje = string.IsNullOrWhiteSpace(mensaje) ? MensajeSeguimiento(unificado) : mensaje,
                Fecha = pedido.FechaCreacion
            };
        }

        private static string MensajeSeguimiento(string estadoUnificado)
        {
            return estadoUnificado switch
            {
                "Nuevo" or "Por confirmar" => "Recibimos tu pedido. El local lo confirma antes de pasarlo a cocina.",
                "En cocina" => "El local ya pasó tu pedido a cocina.",
                "En preparación" => "La cocina está preparando tu pedido.",
                "Pendiente de asignación" => "Ya está listo. Estamos asignando un repartidor.",
                "Listo" => "Tu pedido está listo para recoger.",
                "Asignado a delivery" => "Un repartidor ya tiene tu pedido.",
                "Recogido" => "El repartidor recogió tu pedido.",
                "En camino" => "Tu pedido va en camino.",
                "Entregado" => "Pedido entregado. ¡Buen provecho!",
                "Cancelado" => "Este pedido fue cancelado.",
                _ => "Estamos procesando tu pedido."
            };
        }

        public async Task<PedidoDeliveryListadoDto> ValidarPagoAsync(int idEmpresa, int idPedidoOnline, int idUsuario)
        {
            var pedido = await _ctx.PedidoOnline.AsTracking()
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == idPedidoOnline && p.IdEmpresa == idEmpresa);
            if (pedido == null)
                throw new ArgumentException("Pedido no encontrado.");
            if (!EsTransferencia(pedido.MetodoPago))
                throw new ArgumentException("Este pedido no es por transferencia.");
            if (string.IsNullOrWhiteSpace(pedido.VoucherRuta))
                throw new ArgumentException("Este pedido no tiene volante de pago.");
            if (!pedido.PagoValidado)
            {
                pedido.PagoValidado = true;
                pedido.FechaValidacionPago = DateTime.Now;
                pedido.IdUsuarioValidaPago = idUsuario > 0 ? idUsuario : null;
                await _ctx.SaveChangesAsync();
            }
            return await PedidoOError(idEmpresa, idPedidoOnline);
        }

        public async Task<PedidoDeliveryListadoDto> EnviarACocinaAsync(int idEmpresa, int idPedidoOnline, int idUsuario)
        {
            var pedido = await _ctx.PedidoOnline.AsTracking()
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == idPedidoOnline && p.IdEmpresa == idEmpresa);
            if (pedido == null)
                throw new ArgumentException("Pedido no encontrado.");
            if (pedido.EstadoLogistico == PedidoOnlineEstados.Cancelado)
                throw new ArgumentException("El pedido está cancelado.");

            if (pedido.FechaEnvioCocina == null)
            {
                var header = await _ctx.FacturaHeaders
                    .FirstOrDefaultAsync(h => h.IdFacturaHeader == pedido.IdFacturaHeader && h.IdEmpresa == idEmpresa);
                if (header == null)
                    throw new ArgumentException("No se encontró la orden.");
                header.FacturaDetalles = await _ctx.FacturaDetalles
                    .Where(d => d.IdFacturaHeader == header.IdFacturaHeader)
                    .ToListAsync();
                if (idUsuario > 0)
                    header.IdUsuario = idUsuario;
                await _produccion.PublicarOrdenSiAplicaAsync(header, null, ProduccionConstantes.OrigenModuloOnline);
                pedido.FechaEnvioCocina = DateTime.Now;
                await _ctx.SaveChangesAsync();
            }

            return await PedidoOError(idEmpresa, idPedidoOnline);
        }

        public async Task<(byte[] Contenido, string ContentType)> ObtenerVoucherAsync(int idEmpresa, int idPedidoOnline)
        {
            var pedido = await _ctx.PedidoOnline.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedidoOnline == idPedidoOnline && p.IdEmpresa == idEmpresa);
            if (pedido == null || string.IsNullOrWhiteSpace(pedido.VoucherRuta))
                throw new ArgumentException("Este pedido no tiene volante de pago.");

            var full = RutaVoucherSegura(pedido.VoucherRuta);
            if (!File.Exists(full))
                throw new ArgumentException("No se encontró la foto del volante.");

            var ext = Path.GetExtension(full).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            return (await File.ReadAllBytesAsync(full), contentType);
        }

        private async Task<PedidoDeliveryListadoDto> PedidoOError(int idEmpresa, int idPedidoOnline)
        {
            var dto = await ObtenerPedidoAsync(idEmpresa, idPedidoOnline);
            if (dto == null)
                throw new ArgumentException("Pedido no encontrado.");
            return dto;
        }

        private async Task AdjuntarVoucherAsync(PedidoOnline pedido, string? voucherBase64)
        {
            var voucher = DecodificarVoucher(voucherBase64);
            await GuardarArchivoVoucherAsync(pedido, voucher.Bytes, voucher.ContentType);
        }

        private async Task GuardarArchivoVoucherAsync(PedidoOnline pedido, byte[] bytes, string contentType)
        {
            var ext = contentType == "image/png" ? ".png" : contentType == "image/webp" ? ".webp" : ".jpg";
            var rel = $"{pedido.IdEmpresa}/{pedido.IdPedidoOnline}{ext}";
            var full = RutaVoucherSegura(rel);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllBytesAsync(full, bytes);
            pedido.VoucherRuta = rel;
            await _ctx.SaveChangesAsync();
        }

        private static bool EsTransferencia(string? metodo)
            => (metodo ?? "").Trim().StartsWith("Transfer", StringComparison.OrdinalIgnoreCase);

        private static (byte[] Bytes, string ContentType) DecodificarVoucher(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Para pagar por transferencia debe subir la foto del volante.");

            var raw = base64.Trim();
            var comma = raw.IndexOf(',');
            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                raw = raw[(comma + 1)..];

            byte[] bytes;
            try { bytes = Convert.FromBase64String(raw); }
            catch (FormatException) { throw new ArgumentException("El volante no se pudo leer. Suba la foto de nuevo."); }

            if (bytes.Length < 32 || bytes.Length > 2_500_000)
                throw new ArgumentException("La foto del volante debe pesar menos de 2 MB.");

            string contentType;
            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                contentType = "image/jpeg";
            else if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                contentType = "image/png";
            else if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
                contentType = "image/webp";
            else
                throw new ArgumentException("Suba una foto JPG o PNG del volante.");

            return (bytes, contentType);
        }

        private static string CarpetaVouchers()
            => Path.Combine(AppContext.BaseDirectory, "vouchers-pedidos");

        private static string RutaVoucherSegura(string relativo)
        {
            var limpio = (relativo ?? "").Replace('\\', '/').Trim().TrimStart('/');
            if (limpio.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(limpio))
                throw new ArgumentException("El volante no es válido.");
            var root = Path.GetFullPath(CarpetaVouchers());
            if (!root.EndsWith(Path.DirectorySeparatorChar))
                root += Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(root, limpio.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El volante no es válido.");
            return full;
        }

        private static string SoloDigitos(string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        private static bool EsCategoriaInterna(string? nombre)
        {
            var n = TextoCliente(nombre).ToLowerInvariant();
            return n.Contains("materia prima") || n.Contains("materias primas") || n.Contains("insumo");
        }

        /// <summary>
        /// Repara tildes rotas (UTF-8 leído como Latin-1) antes de mostrarlas al cliente.
        /// </summary>
        private static string TextoCliente(string? valor)
        {
            var t = (valor ?? "").Trim();
            if (t.Length == 0)
                return "";
            if (t.IndexOf('Ã') >= 0 || t.Contains("â€") || t.Contains("Â"))
            {
                try
                {
                    var latin1 = Encoding.GetEncoding(1252);
                    var reparado = Encoding.UTF8.GetString(latin1.GetBytes(t)).Trim();
                    if (reparado.Length > 0 && reparado.IndexOf('\uFFFD') < 0)
                        t = reparado;
                }
                catch
                {
                    // se deja el texto original
                }
            }
            return t;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return null;
        }
    }

    internal static class ProduccionPosAdapterTipo
    {
        public const int Orden = 10;
    }
}
