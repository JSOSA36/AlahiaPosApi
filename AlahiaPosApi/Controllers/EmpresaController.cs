using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;
using System.Net.Http.Headers;
using System.Numerics;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresaController : ControllerBase
    {
        private readonly IEmpresas _Empresas;
        private readonly IUsuarios _Usuarios; // ✅ USUARIOS DEL SISTEMA
        private readonly IAreas _Area;
        private readonly IMapper _Mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProductos _IProductos;
        private readonly ICategorias _Categorias;
        private readonly IPlanesCloud _PlanesCloud;
        private IEmpleados _empleados;
        private readonly IContabilidadCatalogoService _contabilidadCatalogo;
        private readonly IDemoEmpresaBootstrap _demoEmpresaBootstrap;
        private readonly IEmpresaOperativaSeed _operativaSeed;
        public EmpresaController(
            IEmpresas empresas,
            IUsuarios usuarios,
            IProductos productos,
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IAreas area,
            ICategorias categorias,
            IEmpleados empleados,
            IPlanesCloud planesCloud,
            IContabilidadCatalogoService contabilidadCatalogo,
            IDemoEmpresaBootstrap demoEmpresaBootstrap,
            IEmpresaOperativaSeed operativaSeed)
        {
            _Empresas = empresas;
            _Usuarios = usuarios;
            _Area = area;
            _Mapper = mapper;
            _IProductos = productos;
            _httpClientFactory = httpClientFactory;
            _Categorias = categorias;
            _empleados = empleados;
            _PlanesCloud = planesCloud;
            _contabilidadCatalogo = contabilidadCatalogo;
            _demoEmpresaBootstrap = demoEmpresaBootstrap;
            _operativaSeed = operativaSeed;
        }

        // =====================================================
        // 🔹 OBTENER EMPRESA POR ID
        // =====================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<EmpresaDto>> Get(int id)
        {
            var empresa = await _Empresas.GetEmpresaById(id);
            if (empresa == null) return NotFound();

            var dto = _Mapper.Map<EmpresaDto>(empresa);

            // 🔥 LOGO
            dto.LogoUrl = !string.IsNullOrEmpty(empresa.Logo)
                ? $"{Request.Scheme}://{Request.Host}/api/empresa/GetLogo/{id}"
                : null;

            // ============================
            // 🔥 PLAN
            // ============================
            var plan = await _PlanesCloud.GetPlanById((int)empresa.IdPlan);
            dto.NombrePlan = plan?.Nombre ?? "Demo";

            return Ok(dto);
        }

        // =====================================================
        // 🔹 OBTENER EMPRESA POR GUID
        // =====================================================
        [HttpGet("GetEmpresa/{guid}")]
        public async Task<ActionResult<EmpresaDto>> GetEmpresa(Guid guid)
        {
            var empresa = await _Empresas.GetEmpresaByGUID(guid);
            if (empresa == null) return NotFound();

            var dto = _Mapper.Map<EmpresaDto>(empresa);

            dto.LogoUrl = !string.IsNullOrEmpty(empresa.Logo)
                ? $"{Request.Scheme}://{Request.Host}/api/empresa/GetLogo/{empresa.IdEmpresa}"
                : null;

            return Ok(dto);
        }

        // =====================================================
        // 🔹 LOGO
        // =====================================================
        [HttpGet("GetLogo/{id}")]
        public async Task<IActionResult> GetLogo(int id)
        {
            var empresa = await _Empresas.GetEmpresaById(id);
            if (empresa == null || string.IsNullOrEmpty(empresa.Logo))
                return NotFound("Empresa o logo no encontrado");

            var client = _httpClientFactory.CreateClient();
            var bytes = await client.GetByteArrayAsync(empresa.Logo);

            var contentType = "image/png";
            if (empresa.Logo.EndsWith(".jpg") || empresa.Logo.EndsWith(".jpeg"))
                contentType = "image/jpeg";
            else if (empresa.Logo.EndsWith(".gif"))
                contentType = "image/gif";

            return File(bytes, contentType);
        }

        // =====================================================
        // 🔹 DEMO (15 días) — alta pública desde el sitio
        // Cualquier empresa. Módulos guiados por un perfil con contabilidad.
        // =====================================================
        [HttpPost("demo")]
        public async Task<IActionResult> CrearDemo([FromForm] EmpresaDto value)
        {
            try
            {
                if (value == null
                    || string.IsNullOrWhiteSpace(value.NombreComercial)
                    || string.IsNullOrWhiteSpace(value.Direccion)
                    || string.IsNullOrWhiteSpace(value.CorreElectronico)
                    || string.IsNullOrWhiteSpace(value.AdminPassword))
                {
                    return BadRequest(new
                    {
                        message = "Nombre, dirección, correo y contraseña son obligatorios."
                    });
                }

                var correo = value.CorreElectronico.Trim();
                if (!correo.Contains('@'))
                    return BadRequest(new { message = "Indica un correo válido." });

                if (value.AdminPassword.Trim().Length < 6)
                    return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

                if (await _Usuarios.ExisteCorreo(correo) || await _Usuarios.ExisteUserName(correo))
                {
                    return Conflict(new
                    {
                        message = "Este correo ya está registrado. Usa otro correo o inicia sesión en el ERP."
                    });
                }

                var empresa = new Empresas
                {
                    NombreComercial = value.NombreComercial.Trim(),
                    RNC = value.RNC ?? "",
                    Direccion = value.Direccion.Trim(),
                    Telefono = value.Telefono ?? "",
                    CorreElectronico = correo,
                    FechaInseccion = DateTime.Now,
                    FechaTerminacion = DateTime.Now.AddDays(15),
                    GuidPublico = Guid.NewGuid(),
                    Estado = true,
                    EstadoServicio = "ACTIVA",
                    PagadoServicio = true,
                    PoliticasAceptadas = true,
                    IdPlan = 1,
                    LimiteUsuario = 5,
                    PrimaryColor = string.IsNullOrWhiteSpace(value.PrimaryColor) ? "#0a3d91" : value.PrimaryColor,
                    SecondaryColor = string.IsNullOrWhiteSpace(value.SecondaryColor) ? "#f5c518" : value.SecondaryColor,
                    TertiaryColor = string.IsNullOrWhiteSpace(value.TertiaryColor) ? "#072a66" : value.TertiaryColor,
                    titleColor = string.IsNullOrWhiteSpace(value.titleColor) ? "#072a66" : value.titleColor,
                    UsaSSL = value.UsaSSL ?? true,
                    PuertoSMTP = value.PuertoSMTP ?? 587,
                };

                await _Empresas.InsertEmpresas(empresa);

                empresa.UrlCitas = Utility.GenerarUrlCita(empresa.GuidPublico);
                empresa.UrlCatalogo = Utility.GenerarUrlCatalogo(empresa.GuidPublico);
                _Empresas.UpdateEmpresas(empresa.IdEmpresa, empresa);

                var categoriasBase = new[]
                {
                    ("Productos", "Producto", "VENTA"),
                    ("Servicios", "Servicio", "VENTA"),
                };

                var prioridad = 1;
                foreach (var (nombre, tipo, tipoOperacion) in categoriasBase)
                {
                    await _Categorias.InsertCategorias(new Categorias
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

                // Módulos + perfil Administrador (dinámico; guía = perfil con contabilidad)
                var idPerfilAdmin = await _demoEmpresaBootstrap.ConfigurarAsync(empresa.IdEmpresa);

                var empleadoAdmin = new Empleados
                {
                    Nombre = "Administrador",
                    Ocupacion = "Administrador",
                    Estado = true,
                    IdEmpresa = empresa.IdEmpresa
                };
                await _empleados.InsertEmpleados(empleadoAdmin);

                var passwordPlano = value.AdminPassword.Trim();
                await _Usuarios.Crear(new Usuarios
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
                    Utility.Send(
                        "smtp.gmail.com",
                        587,
                        true,
                        "ing.joelarielsosa@gmail.com",
                        "wrcsdhewqdgrtula",
                        "MacroBits Software",
                        correo,
                        "Credenciales de acceso - Alahia ERP (Demo)",
                        $@"
                        <h3>Bienvenido a Alahia ERP</h3>
                        <p>Tu demo está lista (15 días).</p>
                        <p>
                        <strong>Usuario:</strong> {correo}<br/>
                        <strong>Contraseña:</strong> {passwordPlano}
                        </p>
                        <p>Ya puedes acceder al sistema.</p>
                       "
                    );
                }
                catch
                {
                    // El alta no debe fallar si el correo no sale
                }

                return Ok(new
                {
                    message = "Demo creada correctamente ✅",
                    idEmpresa = empresa.IdEmpresa,
                    urlCitas = empresa.UrlCitas,
                    urlCatalogo = empresa.UrlCatalogo,
                    user = correo,
                    password = passwordPlano,
                    guidPublico = empresa.GuidPublico,
                    fechaTerminacion = empresa.FechaTerminacion,
                    idPerfil = idPerfilAdmin
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al crear la demo ❌",
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // =====================================================
        // 🔹 CREAR / ACTUALIZAR EMPRESA
        // =====================================================
        [HttpPut]
        public async Task<IActionResult> Put([FromForm] EmpresaDto value)
        {
            try
            {
                var empresa = await _Empresas.GetEmpresaByGUID(value.GuidPublico);

                // =====================================================
                // 🔥 CREAR EMPRESA
                // =====================================================
                if (empresa == null)
                {
                    empresa = new Empresas
                    {
                        NombreComercial = value.NombreComercial,
                        RNC = value.RNC,
                        Direccion = value.Direccion,
                        Telefono = value.Telefono,
                        CorreElectronico = value.CorreElectronico,
                        FechaInseccion = DateTime.Now,
                        FechaTerminacion = DateTime.Now.AddDays(15),
                        GuidPublico = Guid.NewGuid(),

                        PrimaryColor = value.PrimaryColor,
                        SecondaryColor = value.SecondaryColor,
                        TertiaryColor = value.TertiaryColor,
                        titleColor = value.titleColor,

                        IdPlan = 1,

                        CorreoSMTP = value.CorreoSMTP,
                        PasswordSMTP = value.PasswordSMTP,
                        ServidorSMTP = value.ServidorSMTP,
                        PuertoSMTP = value.PuertoSMTP,
                        UsaSSL = value.UsaSSL,
                        NombreRemitente = value.NombreRemitente,
                    };

                    if (value.Imagen != null && value.Imagen.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await value.Imagen.CopyToAsync(ms);

                        empresa.Logo = Utility.UploadFileFtp(
                            ms.ToArray(),
                            Guid.NewGuid() + Path.GetExtension(value.Imagen.FileName)
                        );
                    }

                    await _Empresas.InsertEmpresas(empresa);

                    empresa.UrlCitas = Utility.GenerarUrlCita(empresa.GuidPublico);
                    empresa.UrlCatalogo = Utility.GenerarUrlCatalogo(empresa.GuidPublico);
                    _Empresas.UpdateEmpresas(empresa.IdEmpresa, empresa);

                    string[] areas =
                    {
                "Peluquería", "Barbería", "Uñas", "Estética",
                "Depilación", "Cejas y Pestañas", "Maquillaje", "Spa"
            };

                    foreach (var nombre in areas)
                    {
                        await _Area.InsertArea(new Area
                        {
                            Nombre = nombre,
                            IsActivo = true,
                            IdEmpresa = empresa.IdEmpresa
                        });
                    }

                    var categorias = new[]
                    {
                "Peluquería", "Uñas", "Estética", "Depilación", "Cejas", "Maquillaje", "Spa"
            };

                    int prioridad = 1;
                    foreach (var c in categorias)
                    {
                        await _Categorias.InsertCategorias(new Categorias
                        {
                            Nombre = $"Servicios de {c}",
                            Tipo = "Servicio",
                            IsActiva = true,
                            IdEmpresa = empresa.IdEmpresa,
                            Prioridad = prioridad++
                        });
                    }

                    await _contabilidadCatalogo.SeedCatalogoDefaultAsync(empresa.IdEmpresa);
                    await _operativaSeed.SeedDesdePlantillaAsync(empresa.IdEmpresa);

                    var empleadoAdmin = new Empleados
                    {
                        Nombre = "Administrador",
                        Ocupacion = "Administrador",
                        Estado = true,
                        IdEmpresa = empresa.IdEmpresa
                    };

                    await _empleados.InsertEmpleados(empleadoAdmin);

                    var passwordPlano = string.IsNullOrWhiteSpace(value.AdminPassword)
                        ? Utility.GenerarPasswordAleatoria(6)
                        : value.AdminPassword.Trim();

                    var usuarioAdmin = new Usuarios
                    {
                        Correo = value.CorreElectronico,
                        UserName = value.CorreElectronico,
                        PasswordHash = Utility.EncriptarPassword(passwordPlano),
                        IdPerfil = 1,
                        Estado = true,
                        IdEmpresa = empresa.IdEmpresa,
                        FechaCreacion = DateTime.Now,
                        IdEmpleado = empleadoAdmin.IdEmpleados
                    };

                    await _Usuarios.Crear(usuarioAdmin);

                    // ===============================
                    // 📧 ENVÍO DESDE MACROBITS
                    // ===============================
                    Utility.Send(
                        "smtp.gmail.com",
                        587,
                        true,
                        "ing.joelarielsosa@gmail.com",   // TU CORREO
                        "wrcsdhewqdgrtula",             // 🔑 App Password
                        "MacroBits Software",
                        value.CorreElectronico,
                        "Credenciales de acceso - MacroBits",
                        $@"
                        <h3>Bienvenido a MacroBits</h3>
                        <p>Tu empresa ha sido registrada correctamente.</p>
                        <p>
                        <strong>Usuario:</strong> {value.CorreElectronico}<br/>
                        <strong>Contraseña:</strong> {passwordPlano}
                        </p>
                        <p>Ya puedes acceder al sistema.</p>
                       "
                    );


                    return Ok(new
                    {
                        message = "Empresa creada correctamente ✅",
                        idEmpresa = empresa.IdEmpresa,
                        urlCitas = empresa.UrlCitas,
                        urlCatalogo = empresa.UrlCatalogo,
                        user = value.CorreElectronico,
                        password = passwordPlano,
                        guidPublico = empresa.GuidPublico
                    });
                }

                // =====================================================
                // 🔄 ACTUALIZAR EMPRESA
                // =====================================================
                empresa.NombreComercial = value.NombreComercial;
                empresa.RNC = value.RNC;
                empresa.Direccion = value.Direccion;
                empresa.Telefono = value.Telefono;
                empresa.CorreElectronico = value.CorreElectronico;

                empresa.PrimaryColor = value.PrimaryColor;
                empresa.SecondaryColor = value.SecondaryColor;
                empresa.TertiaryColor = value.TertiaryColor;
                empresa.titleColor = value.titleColor;

                empresa.CorreoSMTP = value.CorreoSMTP;
                empresa.PasswordSMTP = value.PasswordSMTP;
                empresa.ServidorSMTP = value.ServidorSMTP;
                empresa.PuertoSMTP = value.PuertoSMTP;
                empresa.UsaSSL = value.UsaSSL;
                empresa.NombreRemitente = value.NombreRemitente;

                if (value.Imagen != null && value.Imagen.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await value.Imagen.CopyToAsync(ms);

                    empresa.Logo = Utility.UploadFileFtp(
                        ms.ToArray(),
                        Guid.NewGuid() + Path.GetExtension(value.Imagen.FileName)
                    );
                }

                _Empresas.UpdateEmpresas(empresa.IdEmpresa, empresa);

                return Ok(new { message = "Empresa actualizada correctamente ✅" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error interno al procesar la empresa ❌",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
        }
        [HttpPost("ActualizarEstadoEmpresa/{empresaId}")]
        public async Task<IActionResult> ActualizarEstadoEmpresa(int empresaId)
        {
            await _Empresas.ActualizarEstadoEmpresa(empresaId);

            return Ok(new
            {
                message = "Estado actualizado correctamente 🔄"
            });
        }
        [HttpGet("PuedeOperar/{empresaId}")]
        public async Task<IActionResult> PuedeOperar(int empresaId)
        {
            var empresa = await _Empresas.GetEmpresaById(empresaId);

            if (empresa == null)
                return NotFound();

            var puede = _Empresas.PuedeOperar(empresa);

            return Ok(new
            {
                puedeOperar = puede,
                estadoServicio = empresa.EstadoServicio ?? "ACTIVA",
                pagadoServicio = empresa.PagadoServicio
            });
        }
        [HttpPost("ActualizarEstado")]
        public async Task<IActionResult> ActualizarEstado()
        {
            await _Empresas.ActualizarEstadoAutomatico();
            return Ok(new { message = "Estados actualizados correctamente 🔄" });
        }
        [HttpPost("MarcarPago/{empresaId}")]
        public async Task<IActionResult> MarcarPago(int empresaId)
        {
            await _Empresas.MarcarPago(empresaId);
            return Ok(new { message = "Pago aprobado y servicio activado ✅" });
        }
        [HttpPost("MarcarPendiente/{empresaId}")]
        public async Task<IActionResult> MarcarPendiente(int empresaId)
        {
            await _Empresas.MarcarPendiente(empresaId);
            return Ok(new { message = "Pago en revisión ⏳" });
        }

        // =====================================================
        // 🔹 CATÁLOGO PÚBLICO
        // =====================================================
        [HttpGet("CatalogoGuid/{guid}")]
        public async Task<ActionResult<CatalogoDto>> GetCatalogo(Guid guid)
        {
            var empresa = await _Empresas.GetEmpresaByGUID(guid);
            if (empresa == null) return NotFound();

            var areas = await _Area.GetAllAreas(empresa.IdEmpresa);
            var servicios = await _IProductos.GetAllProductos(empresa.IdEmpresa);

            return Ok(new CatalogoDto
            {
                IdEmpresa = empresa.IdEmpresa,
                NombreComercial = empresa.NombreComercial,
                Telefono = empresa.Telefono,
                Direccion = empresa.Direccion,
                LogoUrl = empresa.Logo,
                Areas = areas.Select(a => new AreaDto
                {
                    IdArea = a.IdArea,
                    Nombre = a.Nombre,
                    Servicios = servicios
                        .Where(s => s.IdArea == a.IdArea)
                        .Select(s => new ServicioDto
                        {
                            IdServicio = s.IdProducto,
                            Nombre = s.Nombre,
                            Imagen = s.Imagen1,
                            Precio = s.PrecioVenta,
                            Descripcion = s.Descripcion
                        }).ToList()
                }).ToList()
            });
        }
    }
}
