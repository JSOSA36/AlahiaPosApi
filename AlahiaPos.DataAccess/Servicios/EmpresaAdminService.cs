using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpresaAdminService : IEmpresaAdminService
    {
        private readonly AlahiaPosContext _db;
        private readonly IEmpresas _empresas;
        private readonly IUsuarios _usuarios;
        private readonly ICategorias _categorias;
        private readonly IEmpleados _empleados;
        private readonly IContabilidadCatalogoService _contabilidadCatalogo;
        private readonly IDemoEmpresaBootstrap _bootstrap;
        private readonly IEmpresaOperativaSeed _operativaSeed;
        private readonly IModulo _modulos;
        private readonly IEmpresaModulos _empresaModulos;
        private readonly IPerfilRoles _perfilRoles;
        private readonly IPerfiles _perfiles;
        private readonly IContabilidadConfiguracionService _contabilidadConfig;

        public EmpresaAdminService(
            AlahiaPosContext db,
            IEmpresas empresas,
            IUsuarios usuarios,
            ICategorias categorias,
            IEmpleados empleados,
            IContabilidadCatalogoService contabilidadCatalogo,
            IDemoEmpresaBootstrap bootstrap,
            IEmpresaOperativaSeed operativaSeed,
            IModulo modulos,
            IEmpresaModulos empresaModulos,
            IPerfilRoles perfilRoles,
            IPerfiles perfiles,
            IContabilidadConfiguracionService contabilidadConfig)
        {
            _db = db;
            _empresas = empresas;
            _usuarios = usuarios;
            _categorias = categorias;
            _empleados = empleados;
            _contabilidadCatalogo = contabilidadCatalogo;
            _bootstrap = bootstrap;
            _operativaSeed = operativaSeed;
            _modulos = modulos;
            _empresaModulos = empresaModulos;
            _perfilRoles = perfilRoles;
            _perfiles = perfiles;
            _contabilidadConfig = contabilidadConfig;
        }

        public async Task<List<EmpresaAdminListItemDto>> ListarAsync()
        {
            var hoy = DateTime.Today;
            var empresas = await _db.Empresas.AsNoTracking()
                .Where(e => !e.EsEmpresaSistema)
                .OrderBy(e => e.NombreComercial)
                .ToListAsync();

            var counts = await _db.Empresa_Modulos.AsNoTracking()
                .Where(em => em.Activo)
                .GroupBy(em => em.EmpresaId)
                .Select(g => new { g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.N);

            return empresas.Select(e => MapListItem(e, counts.GetValueOrDefault(e.IdEmpresa), hoy)).ToList();
        }

        public async Task<EmpresaAdminDetalleDto?> ObtenerAsync(int idEmpresa)
        {
            var e = await _db.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && !x.EsEmpresaSistema);
            if (e == null) return null;

            var codigos = await (
                from em in _db.Empresa_Modulos.AsNoTracking()
                join m in _db.Modulos.AsNoTracking() on em.ModuloId equals m.Id
                where em.EmpresaId == idEmpresa && em.Activo && m.Activo
                select m.Codigo
            ).ToListAsync();

            var item = MapListItem(e, codigos.Count, DateTime.Today);
            return new EmpresaAdminDetalleDto
            {
                IdEmpresa = item.IdEmpresa,
                NombreComercial = item.NombreComercial,
                RNC = item.RNC,
                CorreElectronico = item.CorreElectronico,
                Telefono = item.Telefono,
                Estado = item.Estado,
                EstadoServicio = item.EstadoServicio,
                MontoServicio = item.MontoServicio,
                FechaTerminacion = item.FechaTerminacion,
                EsDemoVigente = item.EsDemoVigente,
                CantidadModulos = item.CantidadModulos,
                LimiteUsuario = item.LimiteUsuario,
                NivelSoporte = item.NivelSoporte,
                Direccion = e.Direccion,
                CodigosModulo = codigos,
                ModulosDisponibles = await CatalogoModulosAsync(idEmpresa),
                Perfiles = await ListarPerfilesAsync(idEmpresa)
            };
        }

        public async Task<List<ModuloCatalogoItemDto>> CatalogoModulosAsync(int? idEmpresaSeleccion = null)
        {
            var seleccion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (idEmpresaSeleccion is > 0)
            {
                var codes = await (
                    from em in _db.Empresa_Modulos.AsNoTracking()
                    join m in _db.Modulos.AsNoTracking() on em.ModuloId equals m.Id
                    where em.EmpresaId == idEmpresaSeleccion && em.Activo
                    select m.Codigo
                ).ToListAsync();
                foreach (var c in codes) seleccion.Add(c);
            }

            var all = (await _modulos.GetAllModulos())
                .Where(m => m.Activo && !string.IsNullOrWhiteSpace(m.Codigo))
                .OrderBy(m => m.Nombre)
                .ToList();

            return all.Select(m =>
            {
                var codigo = m.Codigo.Trim();
                var excluido = DemoVerticalPresets.CodigosInternosNunca.Contains(codigo);
                return new ModuloCatalogoItemDto
                {
                    Id = m.Id,
                    Codigo = codigo,
                    Nombre = m.Nombre ?? codigo,
                    Asignable = !excluido,
                    Seleccionado = seleccion.Contains(codigo)
                };
            }).ToList();
        }

        public Task<List<EmpresaAdminVerticalPresetDto>> ListarVerticalesAsync()
        {
            var list = DemoVerticalPresets.All.Select(p => new EmpresaAdminVerticalPresetDto
            {
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                CodigosModulo = p.CodigosModulo.ToList()
            }).ToList();
            return Task.FromResult(list);
        }

        public async Task<EmpresaAdminAltaResultDto> AltaAsync(EmpresaAdminAltaRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.NombreComercial)
                || string.IsNullOrWhiteSpace(req.Direccion)
                || string.IsNullOrWhiteSpace(req.CorreElectronico)
                || string.IsNullOrWhiteSpace(req.AdminPassword))
            {
                throw new InvalidOperationException("Nombre, dirección, correo y contraseña son obligatorios.");
            }

            var correo = req.CorreElectronico.Trim();
            if (!correo.Contains('@'))
                throw new InvalidOperationException("Indica un correo válido.");

            if (req.AdminPassword.Trim().Length < 6)
                throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

            if (!req.EsDemo && req.MontoServicio <= 0)
                throw new InvalidOperationException("Sin demo, MontoServicio debe ser mayor que 0 (USD).");

            if (await _usuarios.ExisteCorreo(correo) || await _usuarios.ExisteUserName(correo))
                throw new InvalidOperationException("Este correo ya está registrado.");

            var dias = req.DiasDemo > 0 ? req.DiasDemo : 15;
            var empresa = new Empresas
            {
                NombreComercial = req.NombreComercial.Trim(),
                RNC = req.RNC ?? "",
                Direccion = req.Direccion.Trim(),
                Telefono = req.Telefono ?? "",
                CorreElectronico = correo,
                FechaInseccion = DateTime.Now,
                FechaTerminacion = req.EsDemo ? DateTime.Now.AddDays(dias) : DateTime.Now.AddYears(50),
                GuidPublico = Guid.NewGuid(),
                Estado = true,
                EstadoServicio = "ACTIVA",
                PagadoServicio = true,
                PoliticasAceptadas = true,
                IdPlan = 1,
                LimiteUsuario = req.LimiteUsuario > 0 ? req.LimiteUsuario : 5,
                NivelSoporte = NivelesSoporte.Normalizar(req.NivelSoporte),
                MontoServicio = req.EsDemo ? 0m : req.MontoServicio,
                PrecioPlanEspecialUsd = req.EsDemo ? 0m : req.MontoServicio,
                PrimaryColor = "#0a3d91",
                SecondaryColor = "#f5c518",
                TertiaryColor = "#072a66",
                titleColor = "#072a66",
                UsaSSL = true,
                PuertoSMTP = 587,
            };

            await _empresas.InsertEmpresas(empresa);

            empresa.UrlCitas = Utility.GenerarUrlCita(empresa.GuidPublico);
            empresa.UrlCatalogo = Utility.GenerarUrlCatalogo(empresa.GuidPublico);
            _empresas.UpdateEmpresas(empresa.IdEmpresa, empresa);

            var prioridad = 1;
            foreach (var (nombre, tipo, tipoOperacion) in new[]
                     {
                         ("Productos", "Producto", "VENTA"),
                         ("Servicios", "Servicio", "VENTA"),
                     })
            {
                await _categorias.InsertCategorias(new Categorias
                {
                    Nombre = nombre,
                    Tipo = tipo,
                    TipoOperacion = tipoOperacion,
                    IsActiva = true,
                    IdEmpresa = empresa.IdEmpresa,
                    Prioridad = prioridad++
                });
            }

            await _contabilidadCatalogo.SeedCatalogoDefaultAsync(empresa.IdEmpresa);
            await _operativaSeed.SeedDesdePlantillaAsync(empresa.IdEmpresa);

            var idPerfilAdmin = await _bootstrap.ConfigurarAsync(empresa.IdEmpresa, req.CodigosModulo);

            var empleadoAdmin = new Empleados
            {
                Nombre = "Administrador",
                Ocupacion = "Administrador",
                Estado = true,
                IdEmpresa = empresa.IdEmpresa
            };
            await _empleados.InsertEmpleados(empleadoAdmin);

            var passwordPlano = req.AdminPassword.Trim();
            await _usuarios.Crear(new Usuarios
            {
                Correo = correo,
                UserName = correo,
                PasswordHash = Utility.EncriptarPassword(passwordPlano),
                IdPerfil = idPerfilAdmin,
                Estado = true,
                IdEmpresa = empresa.IdEmpresa,
                FechaCreacion = DateTime.Now,
                IdEmpleado = empleadoAdmin.IdEmpleados
            });

            try
            {
                var asunto = req.EsDemo
                    ? "Credenciales de acceso - Alahia ERP (Demo)"
                    : "Credenciales de acceso - Alahia ERP";
                var cuerpoDemo = req.EsDemo
                    ? $"<p>Tu demo está lista ({dias} días).</p>"
                    : "<p>Tu cuenta Alahia ERP está lista.</p>";
                Utility.Send(
                    "smtp.gmail.com", 587, true,
                    "ing.joelarielsosa@gmail.com",
                    "wrcsdhewqdgrtula",
                    "MacroBits Software",
                    correo,
                    asunto,
                    $@"
                    <h3>Bienvenido a Alahia ERP</h3>
                    {cuerpoDemo}
                    <p>
                    <strong>Usuario:</strong> {correo}<br/>
                    <strong>Contraseña:</strong> {passwordPlano}
                    </p>
                    <p>Ya puedes acceder al sistema.</p>");
            }
            catch { /* no crítico */ }

            return new EmpresaAdminAltaResultDto
            {
                IdEmpresa = empresa.IdEmpresa,
                User = correo,
                Password = passwordPlano,
                FechaTerminacion = empresa.FechaTerminacion,
                EsDemo = req.EsDemo,
                IdPerfil = idPerfilAdmin,
                Message = "Empresa creada correctamente."
            };
        }

        public async Task ActualizarNivelSoporteAsync(int idEmpresa, EmpresaAdminNivelSoporteRequest req)
        {
            var e = await _db.Empresas.AsTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && !x.EsEmpresaSistema)
                ?? throw new InvalidOperationException("Empresa no encontrada.");

            e.NivelSoporte = NivelesSoporte.Normalizar(req?.NivelSoporte);
            await _db.SaveChangesAsync();
        }

        public async Task ActualizarDemoAsync(int idEmpresa, EmpresaAdminDemoRequest req)
        {
            var e = await _db.Empresas.AsTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && !x.EsEmpresaSistema)
                ?? throw new InvalidOperationException("Empresa no encontrada.");

            if (req.EsDemo)
            {
                var dias = req.DiasDemo > 0 ? req.DiasDemo : 15;
                e.MontoServicio = 0;
                e.PrecioPlanEspecialUsd = 0;
                e.FechaTerminacion = DateTime.Now.AddDays(dias);
                e.EstadoServicio = "ACTIVA";
                e.PagadoServicio = true;
            }
            else
            {
                if (req.MontoServicio <= 0)
                    throw new InvalidOperationException("Para desactivar el demo indica MontoServicio > 0 (USD).");
                e.MontoServicio = req.MontoServicio;
                e.PrecioPlanEspecialUsd = req.MontoServicio;
                e.EstadoServicio = "ACTIVA";
                e.PagadoServicio = true;
                // Fecha lejana: ya no aplica ventana de prueba
                if (e.FechaTerminacion.Date <= DateTime.Today)
                    e.FechaTerminacion = DateTime.Now.AddYears(50);
            }

            await _db.SaveChangesAsync();
        }

        public async Task SincronizarModulosAsync(int idEmpresa, EmpresaAdminModulosRequest req)
        {
            var existe = await _db.Empresas.AsNoTracking()
                .AnyAsync(x => x.IdEmpresa == idEmpresa && !x.EsEmpresaSistema);
            if (!existe) throw new InvalidOperationException("Empresa no encontrada.");

            var catalogo = (await _modulos.GetAllModulos())
                .Where(m => m.Activo && !string.IsNullOrWhiteSpace(m.Codigo))
                .GroupBy(m => m.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var deseados = new List<Modulo>();
            foreach (var codigo in req.CodigosModulo ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(codigo)) continue;
                var c = codigo.Trim();
                if (DemoEmpresaBootstrapService.CodigosExcluidos.Contains(c)) continue;
                if (catalogo.TryGetValue(c, out var m)) deseados.Add(m);
            }

            foreach (var m in catalogo.Values.Where(x =>
                         x.Codigo.Equals("CONTABILIDAD", StringComparison.OrdinalIgnoreCase)
                         || x.Codigo.StartsWith("CONTABILIDAD_", StringComparison.OrdinalIgnoreCase)))
            {
                if (deseados.All(d => d.Id != m.Id)) deseados.Add(m);
            }

            deseados = deseados.GroupBy(m => m.Id).Select(g => g.First()).ToList();
            if (deseados.Count == 0)
                throw new InvalidOperationException("Debes seleccionar al menos un módulo.");

            var idsDeseados = deseados.Select(m => m.Id).ToHashSet();
            var existentes = await _db.Empresa_Modulos.AsTracking()
                .Where(em => em.EmpresaId == idEmpresa)
                .ToListAsync();

            foreach (var em in existentes)
            {
                if (idsDeseados.Contains(em.ModuloId))
                {
                    if (!em.Activo)
                    {
                        em.Activo = true;
                        em.FechaActivacion = DateTime.Now;
                        em.FechaDesactivacion = null;
                    }
                }
                else if (em.Activo)
                {
                    em.Activo = false;
                    em.FechaDesactivacion = DateTime.Now;
                }
            }

            var idsExistentes = existentes.Select(em => em.ModuloId).ToHashSet();
            foreach (var m in deseados.Where(m => !idsExistentes.Contains(m.Id)))
            {
                await _empresaModulos.InsertEmpresaModulo(new EmpresaModulo
                {
                    EmpresaId = idEmpresa,
                    ModuloId = m.Id,
                    Activo = true,
                    FechaActivacion = DateTime.Now
                });
                await _contabilidadConfig.InicializarSiEsModuloContabilidadAsync(idEmpresa, m.Id, activo: true);
            }

            await _db.SaveChangesAsync();

            var idPerfil = await ObtenerIdPerfilAdminAsync(idEmpresa);
            if (idPerfil > 0)
                await _perfilRoles.AsignarModulos(idPerfil, idEmpresa, idsDeseados);
        }

        public async Task<List<EmpresaAdminPerfilDto>> ListarPerfilesAsync(int idEmpresa)
        {
            await AsegurarEmpresaClienteAsync(idEmpresa);
            var perfiles = await _perfiles.ObtenerPorEmpresa(idEmpresa);
            return perfiles
                .OrderByDescending(p => p.Activo)
                .ThenBy(p => p.Nombre)
                .Select(MapPerfil)
                .ToList();
        }

        public async Task<EmpresaAdminPerfilDto> CrearPerfilAsync(int idEmpresa, EmpresaAdminPerfilRequest req)
        {
            await AsegurarEmpresaClienteAsync(idEmpresa);
            var ids = await NormalizarIdsModuloPerfilAsync(idEmpresa, req?.IdsModulo);
            var nombre = (req?.Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("El nombre del perfil es obligatorio.");

            var id = await _perfiles.CrearPerfilCompleto(new PerfilCreateDto
            {
                IdEmpresa = idEmpresa,
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(req?.Descripcion) ? null : req!.Descripcion.Trim(),
                Activo = req?.Activo ?? true,
                Modulos = ids
            });

            var creado = (await _perfiles.ObtenerPorEmpresa(idEmpresa))
                .FirstOrDefault(p => p.IdPerfil == id)
                ?? throw new InvalidOperationException("No se pudo leer el perfil creado.");
            return MapPerfil(creado);
        }

        public async Task<EmpresaAdminPerfilDto> ActualizarPerfilAsync(int idEmpresa, int idPerfil, EmpresaAdminPerfilRequest req)
        {
            await AsegurarEmpresaClienteAsync(idEmpresa);
            var perfil = await _perfiles.ObtenerPorId(idPerfil)
                ?? throw new InvalidOperationException("Perfil no encontrado.");
            if (perfil.IdEmpresa != idEmpresa)
                throw new InvalidOperationException("El perfil no pertenece a esta empresa.");

            var ids = await NormalizarIdsModuloPerfilAsync(idEmpresa, req?.IdsModulo);
            var nombre = (req?.Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("El nombre del perfil es obligatorio.");

            var ok = await _perfiles.ActualizarPerfilCompleto(new PerfilUpdateDto
            {
                IdPerfil = idPerfil,
                IdEmpresa = idEmpresa,
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(req?.Descripcion) ? null : req!.Descripcion.Trim(),
                Activo = req?.Activo ?? true,
                Modulos = ids
            });
            if (!ok) throw new InvalidOperationException("No se pudo actualizar el perfil.");

            var actualizado = (await _perfiles.ObtenerPorEmpresa(idEmpresa))
                .FirstOrDefault(p => p.IdPerfil == idPerfil)
                ?? throw new InvalidOperationException("No se pudo leer el perfil actualizado.");
            return MapPerfil(actualizado);
        }

        public async Task EliminarPerfilAsync(int idEmpresa, int idPerfil)
        {
            await AsegurarEmpresaClienteAsync(idEmpresa);
            var perfil = await _perfiles.ObtenerPorId(idPerfil)
                ?? throw new InvalidOperationException("Perfil no encontrado.");
            if (perfil.IdEmpresa != idEmpresa)
                throw new InvalidOperationException("El perfil no pertenece a esta empresa.");

            var esAdmin = !string.IsNullOrWhiteSpace(perfil.Nombre)
                && perfil.Nombre.Contains("Administrador", StringComparison.OrdinalIgnoreCase);
            if (esAdmin)
                throw new InvalidOperationException("No se puede eliminar el perfil Administrador.");

            var enUso = await _db.Usuarios.AsNoTracking()
                .AnyAsync(u => u.IdPerfil == idPerfil && u.IdEmpresa == idEmpresa && u.Estado);
            if (enUso)
                throw new InvalidOperationException("Hay usuarios activos con este perfil. Reasígnelos antes de eliminarlo.");

            await _perfiles.Eliminar(idPerfil);
        }

        private async Task AsegurarEmpresaClienteAsync(int idEmpresa)
        {
            var ok = await _db.Empresas.AsNoTracking()
                .AnyAsync(x => x.IdEmpresa == idEmpresa && !x.EsEmpresaSistema);
            if (!ok) throw new InvalidOperationException("Empresa no encontrada.");
        }

        private async Task<List<int>> NormalizarIdsModuloPerfilAsync(int idEmpresa, List<int>? ids)
        {
            var pedidos = (ids ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (pedidos.Count == 0)
                throw new InvalidOperationException("Selecciona al menos un módulo para el perfil.");

            var licenciados = await _db.Empresa_Modulos.AsNoTracking()
                .Where(em => em.EmpresaId == idEmpresa && em.Activo)
                .Select(em => em.ModuloId)
                .ToListAsync();
            var set = licenciados.ToHashSet();
            var validos = pedidos.Where(set.Contains).ToList();
            if (validos.Count == 0)
                throw new InvalidOperationException("Los módulos del perfil deben estar licenciados en la empresa. Guarda la licencia primero.");
            return validos;
        }

        private static EmpresaAdminPerfilDto MapPerfil(PerfilWithModulosDto p) => new()
        {
            IdPerfil = p.IdPerfil,
            IdEmpresa = p.IdEmpresa,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Activo = p.Activo,
            IdsModulo = p.Modulos ?? new List<int>()
        };

        private async Task<int> ObtenerIdPerfilAdminAsync(int idEmpresa)
        {
            var id = await _db.Perfiles.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.Activo
                    && (p.Nombre == "Administrador" || p.Nombre!.Contains("Administrador")))
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();

            if (id > 0) return id;

            return await _db.Perfiles.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.Activo)
                .OrderBy(p => p.IdPerfil)
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();
        }

        private static EmpresaAdminListItemDto MapListItem(Empresas e, int modulos, DateTime hoy)
        {
            var demo = e.MontoServicio <= 0m && e.FechaTerminacion.Date >= hoy;
            return new EmpresaAdminListItemDto
            {
                IdEmpresa = e.IdEmpresa,
                NombreComercial = e.NombreComercial ?? "",
                RNC = e.RNC,
                CorreElectronico = e.CorreElectronico,
                Telefono = e.Telefono,
                Estado = e.Estado,
                EstadoServicio = e.EstadoServicio ?? "",
                MontoServicio = e.MontoServicio,
                FechaTerminacion = e.FechaTerminacion,
                EsDemoVigente = demo,
                CantidadModulos = modulos,
                LimiteUsuario = e.LimiteUsuario,
                NivelSoporte = NivelesSoporte.Normalizar(e.NivelSoporte)
            };
        }
    }
}
