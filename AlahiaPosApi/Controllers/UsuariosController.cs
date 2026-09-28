using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;


namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarios _usuarios;
        private readonly IEmpresas _Empresa;
        private readonly IPerfiles _perfiles;
        private readonly ISucursalService _sucursales;

        public UsuariosController(
            IUsuarios usuarios,
            IEmpresas empresa,
            IPerfiles perfiles,
            ISucursalService sucursales)
        {
            _usuarios = usuarios;
            _Empresa = empresa;
            _perfiles = perfiles;
            _sucursales = sucursales;
        }

        /// <summary>Valida si el correo ya está registrado (debe ser único en el sistema).</summary>
        [HttpGet("existe-correo")]
        public async Task<IActionResult> ExisteCorreo([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
                return BadRequest(new { existe = false, message = "Correo inválido." });

            var existe = await _usuarios.ExisteCorreo(correo.Trim());
            if (!existe)
                existe = await _usuarios.ExisteUserName(correo.Trim());

            return Ok(new
            {
                existe,
                message = existe
                    ? "Este correo ya está registrado. Usa otro o inicia sesión."
                    : "Correo disponible."
            });
        }

        // 🔹 Listar usuarios por empresa (+ cupo de asientos)
        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> GetByEmpresa(int empresaId)
        {
            var data = await _usuarios.ObtenerPorEmpresa(empresaId);
            var empresa = await _Empresa.GetEmpresaById(empresaId);
            var registrados = await _usuarios.CountByEmpresa(empresaId);
            var limite = ResolverLimiteUsuarios(empresa?.LimiteUsuario);

            return Ok(new
            {
                usuarios = data,
                limiteUsuario = limite,
                usuariosRegistrados = registrados,
                puedeAgregar = registrados < limite
            });
        }

        // 🔹 Obtener usuario por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarios.ObtenerPorId(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        // 🔹 Crear usuario
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] UsuarioCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔎 1. Buscar empresa
            var empresa = await _Empresa.GetEmpresaById(dto.IdEmpresa);
            if (empresa == null)
                return NotFound("Empresa no encontrada.");

            // 🔢 2. Contar usuarios actuales
            var totalUsuarios = await _usuarios.CountByEmpresa(dto.IdEmpresa);
            var limite = ResolverLimiteUsuarios(empresa.LimiteUsuario);

            if (totalUsuarios >= limite)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Ha alcanzado el límite de usuarios permitidos ({limite}) en su plan."
                });
            }

            var esAdmin = await EsPerfilAdministradorAsync(dto.IdPerfil);
            var errorSucursal = ValidarSucursalDto(dto, esAdmin);
            if (errorSucursal != null)
                return BadRequest(new { success = false, message = errorSucursal });

            // 👤 4. Crear usuario
            var usuario = new Usuarios
            {
                IdEmpresa = dto.IdEmpresa,
                IdEmpleado = dto.IdEmpleado,
                IdPerfil = dto.IdPerfil,
                IdSucursalActiva = esAdmin ? null : dto.IdSucursal,
                Correo = dto.Correo,
                UserName = dto.Correo,
                PasswordHash = Utility.EncriptarPassword(dto.Password),
                Estado = dto.Activo,
                FechaCreacion = DateTime.Now,
                PuedeEliminarOrden = dto.PuedeEliminarOrden,
                PuedeEliminarItemCarrito = dto.PuedeEliminarItemCarrito,
                PuedeDisminuirCantidadCarrito = dto.PuedeDisminuirCantidadCarrito,
                PuedeEditarPrecioCarrito = dto.PuedeEditarPrecioCarrito,
                PuedeAnularFactura = dto.PuedeAnularFactura
            };

            await _usuarios.Crear(usuario);

            try
            {
                await _sucursales.AsignarOperativaAsync(
                    usuario.IdUsuario,
                    usuario.IdEmpresa,
                    dto.IdSucursal,
                    esAdmin);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            return Ok(new
            {
                success = true,
                message = "Usuario creado correctamente."
            });
        }


        // 🔹 Actualizar usuario
        [HttpPut("{idusuario}")]
        public async Task<IActionResult> Update(
      int idusuario,
      [FromBody] UsuarioCreateDto dto
  )
        {
            if (dto == null)
                return BadRequest("Datos inválidos");

            if (dto.IdPerfil <= 0)
                return BadRequest("Debe seleccionar un perfil.");

            if (dto.IdEmpleado <= 0)
                return BadRequest("Debe seleccionar un empleado.");

            var esAdmin = await EsPerfilAdministradorAsync(dto.IdPerfil);
            var errorSucursal = ValidarSucursalDto(dto, esAdmin);
            if (errorSucursal != null)
                return BadRequest(new { success = false, message = errorSucursal });

            var usuario = await _usuarios.ObtenerPorId(idusuario);

            if (usuario == null)
                return NotFound("Usuario no encontrado");

            // EF restaura el FK si Perfil/Empleado siguen cargados.
            usuario.Perfil = null;
            usuario.Empleado = null;
            usuario.Empresa = null;

            usuario.IdEmpresa = dto.IdEmpresa;
            usuario.IdEmpleado = dto.IdEmpleado;
            usuario.IdPerfil = dto.IdPerfil;
            usuario.IdSucursalActiva = esAdmin ? null : dto.IdSucursal;
            usuario.Correo = dto.Correo;
            usuario.UserName = dto.Correo;
            usuario.Estado = dto.Activo;
            usuario.PuedeEliminarOrden = dto.PuedeEliminarOrden;
            usuario.PuedeEliminarItemCarrito = dto.PuedeEliminarItemCarrito;
            usuario.PuedeDisminuirCantidadCarrito = dto.PuedeDisminuirCantidadCarrito;
            usuario.PuedeEditarPrecioCarrito = dto.PuedeEditarPrecioCarrito;
            usuario.PuedeAnularFactura = dto.PuedeAnularFactura;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.PasswordHash = Utility.EncriptarPassword(dto.Password);
            }

            var ok = await _usuarios.Actualizar(usuario);

            if (!ok)
                return StatusCode(500, "Error actualizando usuario");

            try
            {
                await _sucursales.AsignarOperativaAsync(
                    usuario.IdUsuario,
                    usuario.IdEmpresa,
                    dto.IdSucursal,
                    esAdmin);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            return Ok(new
            {
                success = true,
                message = "Usuario actualizado correctamente",
                idPerfil = usuario.IdPerfil
            });
        }


        // 🔹 Eliminar (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _usuarios.Eliminar(id);
            if (!ok) return NotFound();
            return Ok("Usuario desactivado");
        }

        private static int ResolverLimiteUsuarios(int? limite)
        {
            return limite is > 0 ? limite.Value : 1;
        }

        private static string? ValidarSucursalDto(UsuarioCreateDto dto, bool esAdministrador)
        {
            if (esAdministrador)
                return null;
            if (dto.IdSucursal is not > 0)
                return "Debe indicar la sucursal del usuario.";
            return null;
        }

        private async Task<bool> EsPerfilAdministradorAsync(int idPerfil)
        {
            if (idPerfil <= 0)
                return false;

            var perfil = await _perfiles.ObtenerPorId(idPerfil);
            var nombre = perfil?.Nombre?.Trim() ?? "";
            return string.Equals(nombre, "Administrador", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("ADMIN", StringComparison.OrdinalIgnoreCase);
        }
    }
}
