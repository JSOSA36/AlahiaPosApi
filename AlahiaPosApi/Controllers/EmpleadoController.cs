using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpleadoController : ControllerBase
    {
        private readonly IEmpleados _empleados;
        private readonly IRepository<Sucursal> _sucursales;

        public EmpleadoController(IEmpleados empleados, IRepository<Sucursal> sucursales)
        {
            _empleados = empleados;
            _sucursales = sucursales;
        }

        // =====================================================
        // 📋 LISTAR EMPLEADOS POR EMPRESA
        // =====================================================
        [HttpGet("empresa/{idEmpresa}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEmpleados(int idEmpresa)
        {
            var empleados = await _empleados.GetAllEmpleados(idEmpresa);

            var result = empleados.Select(e => new EmpleadosDto
            {
                IdEmpleados = e.IdEmpleados,
                IdEmpresa = e.IdEmpresa,
                Nombre = e.Nombre,
                Ocupacion = e.Ocupacion,
                Direccion = e.Direccion,
                Celular = e.Celular,
                Estado = e.Estado,
                IdSucursal = e.IdSucursal
            });

            return Ok(result);
        }

        // =====================================================
        // ➕ CREAR EMPLEADO
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CrearEmpleado([FromBody] EmpleadosDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var esAdmin = EsOcupacionAdministrador(dto.Ocupacion);
            var errorSucursal = await ValidarSucursalAsync(dto, esAdmin);
            if (errorSucursal != null)
                return BadRequest(new { success = false, message = errorSucursal });

            var empleado = new Empleados
            {
                Nombre = dto.Nombre,
                Ocupacion = dto.Ocupacion,
                Direccion = dto.Direccion ?? string.Empty,
                Celular = dto.Celular ?? string.Empty,
                Estado = dto.Estado,
                IdEmpresa = dto.IdEmpresa,
                IdSucursal = esAdmin ? null : dto.IdSucursal
            };

            await _empleados.InsertEmpleados(empleado);

            return Ok(new
            {
                message = "Empleado creado correctamente",
                idEmpleado = empleado.IdEmpleados
            });
        }

        // =====================================================
        // ✏️ ACTUALIZAR EMPLEADO
        // =====================================================
        [HttpPut("{idEmpleados}")]
        public async Task<IActionResult> ActualizarEmpleado(
            int idEmpleados,
            [FromBody] EmpleadosDto dto
        )
        {
            var empleado = await _empleados.GetEmpleadoById(idEmpleados);

            if (empleado == null)
                return NotFound("Empleado no encontrado");

            var esAdmin = EsOcupacionAdministrador(dto.Ocupacion);
            var errorSucursal = await ValidarSucursalAsync(dto, esAdmin);
            if (errorSucursal != null)
                return BadRequest(new { success = false, message = errorSucursal });

            empleado.Nombre = dto.Nombre;
            empleado.Ocupacion = dto.Ocupacion;
            empleado.Direccion = dto.Direccion ?? string.Empty;
            empleado.Celular = dto.Celular ?? string.Empty;
            empleado.Estado = dto.Estado;
            empleado.IdSucursal = esAdmin ? null : dto.IdSucursal;

            _empleados.UpdateEmpleados(idEmpleados, empleado);

          

            return Ok(new
            {
                success = true,
                message = "Empleado actualizado correctamente"
            });
        }

        // =====================================================
        // ❌ ELIMINAR EMPLEADO (LÓGICO)
        // =====================================================
        [HttpDelete("{idEmpleado}")]
        public async Task<IActionResult> EliminarEmpleado(int idEmpleado)
        {
            var empleado = await _empleados.GetEmpleadoById(idEmpleado);

            if (empleado == null)
                return NotFound("Empleado no encontrado");

            empleado.Estado = false;
            _empleados.UpdateEmpleados(idEmpleado, empleado);

            return Ok("Empleado eliminado correctamente");
        }

        private static bool EsOcupacionAdministrador(string? ocupacion)
        {
            var nombre = ocupacion?.Trim() ?? "";
            return string.Equals(nombre, "Administrador", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string?> ValidarSucursalAsync(EmpleadosDto dto, bool esAdministrador)
        {
            if (esAdministrador)
                return null;
            if (dto.IdSucursal is not > 0)
                return "Debe indicar la sucursal del empleado.";

            var sucursalOk = await _sucursales.GetAny(s =>
                s.IdSucursal == dto.IdSucursal
                && s.IdEmpresa == dto.IdEmpresa
                && s.Activa);
            if (!sucursalOk)
                return "La sucursal no pertenece a esta empresa.";
            return null;
        }

        // =====================================================
        // 👤 OBTENER EMPLEADO POR ID
        // =====================================================
        [HttpGet("{idEmpleado:int}")]
        public async Task<IActionResult> GetEmpleadoById(int idEmpleado)
        {
            var empleado = await _empleados.GetEmpleadoById(idEmpleado);

            if (empleado == null)
                return NotFound("Empleado no encontrado");

            return Ok(empleado);
        }
    }
}
