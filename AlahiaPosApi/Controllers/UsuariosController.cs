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
        public UsuariosController(IUsuarios usuarios, IEmpresas empresa)
        {
            _usuarios = usuarios;
            _Empresa = empresa;
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

        // 🔹 Listar usuarios por empresa
        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> GetByEmpresa(int empresaId)
        {
            var data = await _usuarios.ObtenerPorEmpresa(empresaId);
            return Ok(data);
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

            // 🚫 3. Validar límite
            if (totalUsuarios >= empresa.LimiteUsuario)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Ha alcanzado el límite de usuarios permitidos ({empresa.LimiteUsuario}) en su plan."
                });
            }

            // 👤 4. Crear usuario
            var usuario = new Usuarios
            {
                IdEmpresa = dto.IdEmpresa,
                IdEmpleado = dto.IdEmpleado,
                IdPerfil = dto.IdPerfil,
                Correo = dto.Correo,
                UserName = dto.Correo,
                PasswordHash = Utility.EncriptarPassword(dto.Password),
                Estado = dto.Activo,
                FechaCreacion = DateTime.Now,
                PuedeEliminarOrden = dto.PuedeEliminarOrden,
                PuedeEliminarItemCarrito = dto.PuedeEliminarItemCarrito,
                PuedeDisminuirCantidadCarrito = dto.PuedeDisminuirCantidadCarrito,
                PuedeEditarPrecioCarrito = dto.PuedeEditarPrecioCarrito
            };

            await _usuarios.Crear(usuario);

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
            // 🔎 1. Buscar usuario real en DB
            var usuario = await _usuarios.ObtenerPorId(idusuario);

            if (usuario == null)
                return NotFound("Usuario no encontrado");

            // 🔄 2. Actualizar solo campos editables
            usuario.IdEmpresa = dto.IdEmpresa;
            usuario.IdEmpleado = dto.IdEmpleado;
            usuario.IdPerfil = dto.IdPerfil;
            usuario.Correo = dto.Correo;
            usuario.UserName = dto.Correo;
            usuario.Estado = dto.Activo;
            usuario.PuedeEliminarOrden = dto.PuedeEliminarOrden;
            usuario.PuedeEliminarItemCarrito = dto.PuedeEliminarItemCarrito;
            usuario.PuedeDisminuirCantidadCarrito = dto.PuedeDisminuirCantidadCarrito;
            usuario.PuedeEditarPrecioCarrito = dto.PuedeEditarPrecioCarrito;

            // 🔐 3. Solo actualizar password si viene nuevo
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.PasswordHash = Utility.EncriptarPassword(dto.Password);
            }

            // ❌ NO tocar FechaCreacion
            // FechaCreacion debe mantenerse intacta

            var ok = await _usuarios.Actualizar(usuario);

            if (!ok)
                return StatusCode(500, "Error actualizando usuario");

            return Ok(new
            {
                success = true,
                message = "Usuario actualizado correctamente"
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
    }
}
