using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Manufactura
{
    public class ManufacturaService : IManufacturaService
    {
        private readonly AlahiaPosContext _db;
        private readonly IAlmacenExistencia _existencias;
        private readonly IMovimientosInventarioService _movimientos;
        private readonly IComprasService _compras;

        public ManufacturaService(
            AlahiaPosContext db,
            IAlmacenExistencia existencias,
            IMovimientosInventarioService movimientos,
            IComprasService compras)
        {
            _db = db;
            _existencias = existencias;
            _movimientos = movimientos;
            _compras = compras;
        }

        public async Task<List<RecetaDto>> ListarRecetasAsync(int idEmpresa, int? idProducto = null, bool soloActivas = false)
        {
            var q = _db.RecetaProduccion.AsNoTracking().Where(r => r.IdEmpresa == idEmpresa);
            if (idProducto.HasValue && idProducto.Value > 0)
                q = q.Where(r => r.IdProductoTerminado == idProducto.Value);
            if (soloActivas)
                q = q.Where(r => r.Activa);

            var recetas = await q.OrderBy(r => r.Nombre).ToListAsync();
            var result = new List<RecetaDto>();
            foreach (var r in recetas)
                result.Add(await MapRecetaAsync(r, incluirItems: false));
            return result;
        }

        public async Task<RecetaDto?> ObtenerRecetaAsync(int idEmpresa, int idReceta)
        {
            var receta = await _db.RecetaProduccion
                .FirstOrDefaultAsync(r => r.IdEmpresa == idEmpresa && r.IdReceta == idReceta);
            return receta == null ? null : await MapRecetaAsync(receta, incluirItems: true);
        }

        public async Task<RecetaDto> GuardarRecetaAsync(GuardarRecetaRequest request)
        {
            if (request.IdEmpresa <= 0) throw new ArgumentException("Empresa requerida.");
            if (request.IdProductoTerminado <= 0) throw new ArgumentException("Indique el producto terminado.");
            if (request.RendimientoBase <= 0) throw new ArgumentException("El rendimiento base debe ser mayor que cero.");
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("La receta necesita al menos un componente.");

            var producto = await ProductoEmpresaAsync(request.IdEmpresa, request.IdProductoTerminado)
                ?? throw new ArgumentException("El producto terminado no pertenece a la empresa.");

            foreach (var item in request.Items)
            {
                if (item.IdProducto <= 0 || item.Cantidad <= 0)
                    throw new ArgumentException("Cada componente debe tener producto y cantidad mayor que cero.");
                if (item.IdProducto == request.IdProductoTerminado)
                    throw new ArgumentException("Un producto no puede ser componente de sí mismo.");
                if (await ProductoEmpresaAsync(request.IdEmpresa, item.IdProducto) == null)
                    throw new ArgumentException($"El componente {item.IdProducto} no pertenece a la empresa.");
            }

            RecetaProduccion receta;
            if (request.IdReceta > 0)
            {
                receta = await _db.RecetaProduccion
                    .FirstOrDefaultAsync(r => r.IdReceta == request.IdReceta && r.IdEmpresa == request.IdEmpresa)
                    ?? throw new KeyNotFoundException("Receta no encontrada.");
            }
            else
            {
                receta = new RecetaProduccion
                {
                    IdEmpresa = request.IdEmpresa,
                    FechaCreacion = DateTime.Now,
                    IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : null
                };
                _db.RecetaProduccion.Add(receta);
            }

            receta.IdProductoTerminado = request.IdProductoTerminado;
            receta.Nombre = string.IsNullOrWhiteSpace(request.Nombre) ? (NombreVisible(producto) ?? "Receta") : request.Nombre.Trim();
            receta.RendimientoBase = request.RendimientoBase;
            receta.IdUnidadMedida = request.IdUnidadMedida ?? producto.IdUnidadMedida;
            receta.Activa = request.Activa;
            receta.Observacion = request.Observacion;

            await _db.SaveChangesAsync();

            var viejos = _db.RecetaProduccionItem.Where(i => i.IdReceta == receta.IdReceta);
            _db.RecetaProduccionItem.RemoveRange(viejos);

            var orden = 0;
            foreach (var item in request.Items)
            {
                _db.RecetaProduccionItem.Add(new RecetaProduccionItem
                {
                    IdReceta = receta.IdReceta,
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    IdUnidadMedida = item.IdUnidadMedida,
                    Orden = item.Orden > 0 ? item.Orden : orden++,
                    Activo = true
                });
            }

            await _db.SaveChangesAsync();
            return (await ObtenerRecetaAsync(request.IdEmpresa, receta.IdReceta))!;
        }

        public async Task<ExplosionDto> ExplotarAsync(int idEmpresa, int idReceta, decimal cantidad, int? idAlmacenOrigen)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad a producir debe ser mayor que cero.");
            var receta = await _db.RecetaProduccion
                .FirstOrDefaultAsync(r => r.IdEmpresa == idEmpresa && r.IdReceta == idReceta)
                ?? throw new KeyNotFoundException("Receta no encontrada.");
            if (receta.RendimientoBase <= 0)
                throw new InvalidOperationException("La receta no tiene rendimiento base válido.");

            var factor = cantidad / receta.RendimientoBase;
            var items = await _db.RecetaProduccionItem
                .Where(i => i.IdReceta == idReceta && i.Activo)
                .OrderBy(i => i.Orden)
                .ToListAsync();

            var materiales = new List<ExplosionMaterialDto>();
            foreach (var item in items)
            {
                materiales.Add(await ArmarMaterialAsync(idEmpresa, idAlmacenOrigen, item.IdProducto, item.IdUnidadMedida, item.Cantidad * factor, null));
            }

            var producto = await ProductoEmpresaAsync(idEmpresa, receta.IdProductoTerminado);
            return new ExplosionDto
            {
                IdReceta = receta.IdReceta,
                IdProductoTerminado = receta.IdProductoTerminado,
                NombreProducto = NombreVisible(producto),
                RendimientoBase = receta.RendimientoBase,
                Cantidad = cantidad,
                Factor = factor,
                IdAlmacenOrigen = idAlmacenOrigen,
                HayFaltantes = materiales.Any(m => m.Faltante > 0),
                Materiales = materiales
            };
        }

        public async Task<List<OrdenProduccionDto>> ListarOrdenesAsync(int idEmpresa, string? estado = null)
        {
            var q = _db.OrdenProduccion.AsNoTracking().Where(o => o.IdEmpresa == idEmpresa);
            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(o => o.Estado == estado);
            var ordenes = await q.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.IdOrdenProduccion).Take(200).ToListAsync();
            var list = new List<OrdenProduccionDto>();
            foreach (var o in ordenes)
                list.Add(await MapOrdenAsync(o, incluirMateriales: false));
            return list;
        }

        public async Task<OrdenProduccionDto?> ObtenerOrdenAsync(int idEmpresa, int idOrden)
        {
            var orden = await OrdenEmpresaAsync(idEmpresa, idOrden);
            return orden == null ? null : await MapOrdenAsync(orden, incluirMateriales: true);
        }

        public async Task<OrdenProduccionDto> GuardarOrdenAsync(GuardarOrdenProduccionRequest request)
        {
            if (request.CantidadPlanificada <= 0) throw new ArgumentException("Indique la cantidad a producir.");
            if (request.IdAlmacenOrigen <= 0 || request.IdAlmacenDestino <= 0)
                throw new ArgumentException("Indique almacén de materias primas y almacén del producto terminado.");

            await ValidarAlmacenAsync(request.IdEmpresa, request.IdAlmacenOrigen);
            await ValidarAlmacenAsync(request.IdEmpresa, request.IdAlmacenDestino);

            var receta = await _db.RecetaProduccion
                .FirstOrDefaultAsync(r => r.IdEmpresa == request.IdEmpresa && r.IdReceta == request.IdReceta && r.Activa)
                ?? throw new ArgumentException("Receta no encontrada o inactiva.");

            OrdenProduccion orden;
            if (request.IdOrdenProduccion > 0)
            {
                orden = await OrdenEmpresaAsync(request.IdEmpresa, request.IdOrdenProduccion)
                    ?? throw new KeyNotFoundException("Orden no encontrada.");
                if (orden.Estado is not ("BORRADOR" or "PLANIFICADA"))
                    throw new InvalidOperationException("Solo se editan órdenes en borrador o planificadas.");
            }
            else
            {
                orden = new OrdenProduccion
                {
                    IdEmpresa = request.IdEmpresa,
                    Numero = await SiguienteNumeroAsync(request.IdEmpresa),
                    Estado = "BORRADOR",
                    FechaCreacion = DateTime.Now,
                    IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : null
                };
                _db.OrdenProduccion.Add(orden);
            }

            orden.IdReceta = receta.IdReceta;
            orden.IdProductoTerminado = receta.IdProductoTerminado;
            orden.CantidadPlanificada = request.CantidadPlanificada;
            orden.IdAlmacenOrigen = request.IdAlmacenOrigen;
            orden.IdAlmacenDestino = request.IdAlmacenDestino;
            orden.Fecha = request.Fecha ?? DateTime.Now;
            orden.IdUsuarioResponsable = request.IdUsuarioResponsable ?? (request.IdUsuario > 0 ? request.IdUsuario : null);
            orden.Observacion = request.Observacion;

            await _db.SaveChangesAsync();
            await ReemplazarMaterialesDesdeExplosionAsync(orden);
            if (request.IdOrdenProduccion <= 0)
                orden.Estado = "BORRADOR";
            await _db.SaveChangesAsync();
            return (await ObtenerOrdenAsync(request.IdEmpresa, orden.IdOrdenProduccion))!;
        }

        public async Task<OrdenProduccionDto> PlanificarAsync(int idEmpresa, int idOrden, int idUsuario)
        {
            var orden = await OrdenEditableAsync(idEmpresa, idOrden, "BORRADOR", "PLANIFICADA");
            await ReemplazarMaterialesDesdeExplosionAsync(orden);
            orden.Estado = "PLANIFICADA";
            orden.IdUsuario = idUsuario > 0 ? idUsuario : orden.IdUsuario;
            await _db.SaveChangesAsync();
            return (await ObtenerOrdenAsync(idEmpresa, idOrden))!;
        }

        public async Task<OrdenProduccionDto> IniciarAsync(int idEmpresa, int idOrden, int idUsuario)
        {
            var orden = await OrdenEditableAsync(idEmpresa, idOrden, "PLANIFICADA");
            await ReemplazarMaterialesDesdeExplosionAsync(orden);
            var faltantes = await _db.OrdenProduccionMaterial
                .Where(m => m.IdOrdenProduccion == idOrden && m.Faltante > 0)
                .ToListAsync();
            if (faltantes.Count > 0)
                throw new InvalidOperationException("Hay materiales faltantes. Compre o reciba inventario antes de iniciar.");

            orden.Estado = "EN_PROCESO";
            orden.FechaInicio = DateTime.Now;
            orden.IdUsuario = idUsuario > 0 ? idUsuario : orden.IdUsuario;
            await _db.SaveChangesAsync();
            return (await ObtenerOrdenAsync(idEmpresa, idOrden))!;
        }

        public async Task<OrdenProduccionDto> CompletarAsync(int idOrden, CompletarOrdenProduccionRequest request)
        {
            if (request.CantidadReal <= 0)
                throw new ArgumentException("Indique la cantidad realmente producida.");

            var orden = await OrdenEditableAsync(request.IdEmpresa, idOrden, "EN_PROCESO", "PLANIFICADA");
            var materiales = await _db.OrdenProduccionMaterial
                .Where(m => m.IdOrdenProduccion == idOrden)
                .ToListAsync();
            if (materiales.Count == 0)
                throw new InvalidOperationException("La orden no tiene materiales calculados. Planifíquela primero.");

            foreach (var mat in materiales)
            {
                var overrideQty = request.Consumos?.FirstOrDefault(c => c.IdProducto == mat.IdProducto);
                mat.CantidadReal = overrideQty != null && overrideQty.CantidadReal > 0
                    ? overrideQty.CantidadReal
                    : mat.CantidadTeorica;
                var disp = await DisponibleAsync(request.IdEmpresa, orden.IdAlmacenOrigen, mat.IdProducto);
                mat.Disponible = disp;
                mat.Faltante = Math.Max(0, (mat.CantidadReal ?? 0) - disp);
                mat.CostoLinea = Math.Round((mat.CantidadReal ?? 0) * mat.PrecioCompra, 2, MidpointRounding.AwayFromZero);
            }

            if (materiales.Any(m => m.Faltante > 0))
                throw new InvalidOperationException("No hay inventario suficiente para el consumo real. Revise los faltantes.");

            var costo = materiales.Sum(m => m.CostoLinea);
            var detallesSalida = materiales.Select(m => new MovimientosInventarioDetalle
            {
                IdProducto = m.IdProducto,
                Cantidad = m.CantidadReal ?? 0,
                Precio = m.PrecioCompra,
                Fecha = DateTime.Now,
                Observacion = orden.Numero
            }).ToList();

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var salida = await _movimientos.GuardarMovimiento(new MovimientosInventario
                {
                    TipoMovimiento = "SALIDA",
                    Motivo = "PRODUCCION",
                    Referencia = orden.Numero,
                    Observacion = $"Consumo OP {orden.Numero}",
                    Fecha = DateTime.Now,
                    IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : null,
                    IdEmpresa = request.IdEmpresa,
                    IdAlmacen = orden.IdAlmacenOrigen,
                    Activo = true,
                    Detalles = detallesSalida
                });

                var costoUnitario = request.CantidadReal > 0
                    ? Math.Round(costo / request.CantidadReal, 4, MidpointRounding.AwayFromZero)
                    : 0;

                var entrada = await _movimientos.GuardarMovimiento(new MovimientosInventario
                {
                    TipoMovimiento = "ENTRADA",
                    Motivo = "PRODUCCION",
                    Referencia = orden.Numero,
                    Observacion = $"Producto terminado OP {orden.Numero}",
                    Fecha = DateTime.Now,
                    IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : null,
                    IdEmpresa = request.IdEmpresa,
                    IdAlmacen = orden.IdAlmacenDestino,
                    Activo = true,
                    Detalles = new List<MovimientosInventarioDetalle>
                    {
                        new()
                        {
                            IdProducto = orden.IdProductoTerminado,
                            Cantidad = request.CantidadReal,
                            Precio = costoUnitario,
                            Fecha = DateTime.Now,
                            Observacion = orden.Numero
                        }
                    }
                });

                orden.CantidadReal = request.CantidadReal;
                orden.CostoMateriales = costo;
                orden.CostoUnitario = costoUnitario;
                orden.IdMovimientoSalida = salida.Id;
                orden.IdMovimientoEntrada = entrada.Id;
                orden.Estado = "COMPLETADA";
                orden.FechaCompletado = DateTime.Now;
                orden.IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : orden.IdUsuario;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException && ex is not KeyNotFoundException)
            {
                await tx.RollbackAsync();
                throw new InvalidOperationException(ex.Message);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return (await ObtenerOrdenAsync(request.IdEmpresa, idOrden))!;
        }

        public async Task<OrdenProduccionDto> CancelarAsync(int idEmpresa, int idOrden, int idUsuario)
        {
            var orden = await OrdenEmpresaAsync(idEmpresa, idOrden)
                ?? throw new KeyNotFoundException("Orden no encontrada.");
            if (orden.Estado == "COMPLETADA")
                throw new InvalidOperationException("Una orden completada no se cancela: el inventario ya se movió.");
            if (orden.Estado == "CANCELADA")
                return (await ObtenerOrdenAsync(idEmpresa, idOrden))!;

            orden.Estado = "CANCELADA";
            orden.IdUsuario = idUsuario > 0 ? idUsuario : orden.IdUsuario;
            await _db.SaveChangesAsync();
            return (await ObtenerOrdenAsync(idEmpresa, idOrden))!;
        }

        public async Task<RequerimientoCompraResultadoDto> CrearRequerimientoCompraAsync(int idEmpresa, int idOrden, int idUsuario)
        {
            var orden = await OrdenEmpresaAsync(idEmpresa, idOrden)
                ?? throw new KeyNotFoundException("Orden no encontrada.");
            if (orden.Estado == "COMPLETADA" || orden.Estado == "CANCELADA")
                throw new InvalidOperationException("Esta orden ya no genera compras.");

            await ReemplazarMaterialesDesdeExplosionAsync(orden);
            await _db.SaveChangesAsync();

            var faltantes = await _db.OrdenProduccionMaterial
                .Where(m => m.IdOrdenProduccion == idOrden && m.Faltante > 0)
                .ToListAsync();
            if (faltantes.Count == 0)
                return new RequerimientoCompraResultadoDto { Mensaje = "No hay materiales faltantes." };

            var ids = faltantes.Select(f => f.IdProducto).Distinct().ToList();
            var productos = await _db.Productos.AsNoTracking()
                .Where(p => ids.Contains(p.IdProducto) && p.IdEmpresa == idEmpresa)
                .ToListAsync();

            var sinProveedor = productos.Where(p => p.IdProveedor <= 0).Select(NombreVisible).ToList();
            if (sinProveedor.Count > 0)
                throw new InvalidOperationException("Asigne un proveedor a: " + string.Join(", ", sinProveedor));

            var creadas = new List<int>();
            foreach (var grupo in productos.GroupBy(p => p.IdProveedor))
            {
                var request = new GuardarFacturaCompraRequest
                {
                    IdEmpresa = idEmpresa,
                    IdProveedor = grupo.Key,
                    FechaDocumento = DateTime.Now,
                    IdAlmacen = orden.IdAlmacenOrigen,
                    Comentario = $"Faltantes producción {orden.Numero}",
                    CondicionFactura = "Credito",
                    Detalles = grupo.Select(p =>
                    {
                        var mat = faltantes.First(f => f.IdProducto == p.IdProducto);
                        return new GuardarFacturaCompraDetalleRequest
                        {
                            IdProducto = p.IdProducto,
                            Cantidad = mat.Faltante,
                            PrecioCompra = p.PrecioCompra,
                            Descuento = 0,
                            Itbis = 0
                        };
                    }).ToList()
                };
                var oc = await _compras.GuardarBorradorOrdenAsync(request);
                creadas.Add(oc.IdOrdenCompraHeader);
            }

            return new RequerimientoCompraResultadoDto
            {
                IdOrdenesCompra = creadas,
                Mensaje = creadas.Count == 1
                    ? "Se creó una orden de compra en borrador con los faltantes."
                    : $"Se crearon {creadas.Count} órdenes de compra en borrador (una por proveedor)."
            };
        }

        private async Task ReemplazarMaterialesDesdeExplosionAsync(OrdenProduccion orden)
        {
            var explosion = await ExplotarAsync(orden.IdEmpresa, orden.IdReceta, orden.CantidadPlanificada, orden.IdAlmacenOrigen);
            var viejos = _db.OrdenProduccionMaterial.Where(m => m.IdOrdenProduccion == orden.IdOrdenProduccion);
            _db.OrdenProduccionMaterial.RemoveRange(viejos);
            foreach (var m in explosion.Materiales)
            {
                _db.OrdenProduccionMaterial.Add(new OrdenProduccionMaterial
                {
                    IdOrdenProduccion = orden.IdOrdenProduccion,
                    IdProducto = m.IdProducto,
                    IdUnidadMedida = m.IdUnidadMedida,
                    CantidadTeorica = m.CantidadTeorica,
                    CantidadReal = null,
                    Disponible = m.Disponible,
                    Faltante = m.Faltante,
                    PrecioCompra = m.PrecioCompra,
                    CostoLinea = m.CostoLinea
                });
            }
        }

        private async Task<ExplosionMaterialDto> ArmarMaterialAsync(
            int idEmpresa, int? idAlmacen, int idProducto, int? idUnidad, decimal cantidadTeorica, decimal? cantidadReal)
        {
            var producto = await ProductoEmpresaAsync(idEmpresa, idProducto);
            var disp = idAlmacen.HasValue && idAlmacen.Value > 0
                ? await DisponibleAsync(idEmpresa, idAlmacen.Value, idProducto)
                : 0;
            var requerida = cantidadReal ?? cantidadTeorica;
            var faltante = idAlmacen.HasValue && idAlmacen.Value > 0 ? Math.Max(0, requerida - disp) : 0;
            string? proveedor = null;
            if (producto != null && producto.IdProveedor > 0)
            {
                proveedor = await _db.Proveedores.AsNoTracking()
                    .Where(p => p.IdProveedor == producto.IdProveedor)
                    .Select(p => p.NombreComercial)
                    .FirstOrDefaultAsync();
            }

            return new ExplosionMaterialDto
            {
                IdProducto = idProducto,
                NombreProducto = NombreVisible(producto),
                IdUnidadMedida = idUnidad ?? producto?.IdUnidadMedida,
                CantidadTeorica = Decimal.Round(cantidadTeorica, 4, MidpointRounding.AwayFromZero),
                CantidadReal = cantidadReal,
                Disponible = disp,
                Faltante = Decimal.Round(faltante, 4, MidpointRounding.AwayFromZero),
                PrecioCompra = producto?.PrecioCompra ?? 0,
                CostoLinea = Math.Round(requerida * (producto?.PrecioCompra ?? 0), 2, MidpointRounding.AwayFromZero),
                IdProveedor = producto is { IdProveedor: > 0 } ? producto.IdProveedor : null,
                NombreProveedor = proveedor
            };
        }

        private async Task<RecetaDto> MapRecetaAsync(RecetaProduccion receta, bool incluirItems)
        {
            var producto = await ProductoEmpresaAsync(receta.IdEmpresa, receta.IdProductoTerminado);
            var dto = new RecetaDto
            {
                IdReceta = receta.IdReceta,
                IdEmpresa = receta.IdEmpresa,
                IdProductoTerminado = receta.IdProductoTerminado,
                NombreProducto = NombreVisible(producto),
                Nombre = receta.Nombre,
                RendimientoBase = receta.RendimientoBase,
                IdUnidadMedida = receta.IdUnidadMedida,
                Activa = receta.Activa,
                Observacion = receta.Observacion
            };
            if (!incluirItems) return dto;

            var items = await _db.RecetaProduccionItem.AsNoTracking()
                .Where(i => i.IdReceta == receta.IdReceta && i.Activo)
                .OrderBy(i => i.Orden)
                .ToListAsync();
            foreach (var i in items)
            {
                var p = await ProductoEmpresaAsync(receta.IdEmpresa, i.IdProducto);
                dto.Items.Add(new RecetaItemDto
                {
                    IdRecetaItem = i.IdRecetaItem,
                    IdProducto = i.IdProducto,
                    NombreProducto = NombreVisible(p),
                    Cantidad = i.Cantidad,
                    IdUnidadMedida = i.IdUnidadMedida ?? p?.IdUnidadMedida,
                    Orden = i.Orden
                });
            }
            return dto;
        }

        private async Task<OrdenProduccionDto> MapOrdenAsync(OrdenProduccion orden, bool incluirMateriales)
        {
            var producto = await ProductoEmpresaAsync(orden.IdEmpresa, orden.IdProductoTerminado);
            var receta = await _db.RecetaProduccion.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdReceta == orden.IdReceta);
            var origen = await _db.Almacenes.AsNoTracking()
                .FirstOrDefaultAsync(a => a.IdAlmacen == orden.IdAlmacenOrigen);
            var destino = await _db.Almacenes.AsNoTracking()
                .FirstOrDefaultAsync(a => a.IdAlmacen == orden.IdAlmacenDestino);

            var dto = new OrdenProduccionDto
            {
                IdOrdenProduccion = orden.IdOrdenProduccion,
                IdEmpresa = orden.IdEmpresa,
                Numero = orden.Numero,
                IdReceta = orden.IdReceta,
                NombreReceta = receta?.Nombre,
                IdProductoTerminado = orden.IdProductoTerminado,
                NombreProducto = NombreVisible(producto),
                CantidadPlanificada = orden.CantidadPlanificada,
                CantidadReal = orden.CantidadReal,
                IdAlmacenOrigen = orden.IdAlmacenOrigen,
                NombreAlmacenOrigen = origen?.Nombre,
                IdAlmacenDestino = orden.IdAlmacenDestino,
                NombreAlmacenDestino = destino?.Nombre,
                Fecha = orden.Fecha,
                IdUsuarioResponsable = orden.IdUsuarioResponsable,
                Observacion = orden.Observacion,
                Estado = orden.Estado,
                IdMovimientoSalida = orden.IdMovimientoSalida,
                IdMovimientoEntrada = orden.IdMovimientoEntrada,
                CostoMateriales = orden.CostoMateriales,
                CostoUnitario = orden.CostoUnitario,
                FechaInicio = orden.FechaInicio,
                FechaCompletado = orden.FechaCompletado
            };

            if (!incluirMateriales) return dto;

            var mats = await _db.OrdenProduccionMaterial.AsNoTracking()
                .Where(m => m.IdOrdenProduccion == orden.IdOrdenProduccion)
                .ToListAsync();
            foreach (var m in mats)
            {
                var p = await ProductoEmpresaAsync(orden.IdEmpresa, m.IdProducto);
                string? proveedor = null;
                if (p != null && p.IdProveedor > 0)
                {
                    proveedor = await _db.Proveedores.AsNoTracking()
                        .Where(pr => pr.IdProveedor == p.IdProveedor)
                        .Select(pr => pr.NombreComercial)
                        .FirstOrDefaultAsync();
                }
                dto.Materiales.Add(new ExplosionMaterialDto
                {
                    IdProducto = m.IdProducto,
                    NombreProducto = NombreVisible(p),
                    IdUnidadMedida = m.IdUnidadMedida,
                    CantidadTeorica = m.CantidadTeorica,
                    CantidadReal = m.CantidadReal,
                    Disponible = m.Disponible,
                    Faltante = m.Faltante,
                    PrecioCompra = m.PrecioCompra,
                    CostoLinea = m.CostoLinea,
                    IdProveedor = p is { IdProveedor: > 0 } ? p.IdProveedor : null,
                    NombreProveedor = proveedor
                });
            }
            dto.HayFaltantes = dto.Materiales.Any(x => x.Faltante > 0);
            return dto;
        }

        private async Task<string> SiguienteNumeroAsync(int idEmpresa)
        {
            var anio = DateTime.Now.Year;
            var prefijo = $"OP-{anio}-";
            var ultimo = await _db.OrdenProduccion.AsNoTracking()
                .Where(o => o.IdEmpresa == idEmpresa && o.Numero.StartsWith(prefijo))
                .Select(o => o.Numero)
                .ToListAsync();
            var max = 0;
            foreach (var n in ultimo)
            {
                var cola = n.Length > prefijo.Length ? n[prefijo.Length..] : "0";
                if (int.TryParse(cola, out var v) && v > max) max = v;
            }
            return prefijo + (max + 1).ToString("0000");
        }

        private async Task<decimal> DisponibleAsync(int idEmpresa, int idAlmacen, int idProducto)
        {
            var ex = await _existencias.GetExistencia(idAlmacen, idProducto, idEmpresa);
            return ex?.Cantidad ?? 0;
        }

        private static string NombreVisible(Productos? p)
        {
            if (p == null) return "";
            if (!string.IsNullOrWhiteSpace(p.Nombre)) return p.Nombre;
            return p.Descripcion ?? "";
        }

        private Task<Productos?> ProductoEmpresaAsync(int idEmpresa, int idProducto) =>
            _db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.IdProducto == idProducto && p.IdEmpresa == idEmpresa);

        private async Task ValidarAlmacenAsync(int idEmpresa, int idAlmacen)
        {
            var ok = await _db.Almacenes.AsNoTracking()
                .AnyAsync(a => a.IdAlmacen == idAlmacen && a.IdEmpresa == idEmpresa && a.Activo);
            if (!ok) throw new ArgumentException("Almacén no válido para esta empresa.");
        }

        private async Task<OrdenProduccion?> OrdenEmpresaAsync(int idEmpresa, int idOrden) =>
            await _db.OrdenProduccion.FirstOrDefaultAsync(o => o.IdEmpresa == idEmpresa && o.IdOrdenProduccion == idOrden);

        private async Task<OrdenProduccion> OrdenEditableAsync(int idEmpresa, int idOrden, params string[] estados)
        {
            var orden = await OrdenEmpresaAsync(idEmpresa, idOrden)
                ?? throw new KeyNotFoundException("Orden no encontrada.");
            if (!estados.Contains(orden.Estado, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"La orden está en {orden.Estado} y no admite esta acción.");
            return orden;
        }
    }
}
