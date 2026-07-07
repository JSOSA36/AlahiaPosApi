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
        public EmpresaController(
            IEmpresas empresas,
            IUsuarios usuarios,
            IProductos productos,
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IAreas area,
            ICategorias categorias,
            IEmpleados empleados,
            IPlanesCloud planesCloud)
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

                    var empleadoAdmin = new Empleados
                    {
                        Nombre = "Administrador",
                        Ocupacion = "Administrador",
                        Estado = true,
                        IdEmpresa = empresa.IdEmpresa
                    };

                    await _empleados.InsertEmpleados(empleadoAdmin);

                    var passwordPlano = Utility.GenerarPasswordAleatoria(6);

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
                        empresa.IdEmpresa,
                        empresa.UrlCitas,
                        empresa.UrlCatalogo,
                        user = value.CorreElectronico,
                        password = passwordPlano,
                        idempresa = empresa.IdEmpresa,
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

            return Ok(new { puedeOperar = puede });
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
