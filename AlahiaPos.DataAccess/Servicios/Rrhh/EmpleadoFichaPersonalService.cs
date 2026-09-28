using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class EmpleadoFichaPersonalService : IEmpleadoFichaPersonalService
    {
        private readonly AlahiaPosContext _db;

        public EmpleadoFichaPersonalService(AlahiaPosContext db) => _db = db;

        public async Task<EmpleadoFichaPersonalDto> GetAsync(int idEmpresa, int idEmpleados)
        {
            var emp = await _db.EmpleadosP.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.IdEmpleados == idEmpleados)
                ?? throw new InvalidOperationException("Empleado no existe en EmpleadosP.");

            var ficha = await _db.EmpleadoFichaPersonal.AsNoTracking()
                .FirstOrDefaultAsync(f => f.IdEmpresa == idEmpresa && f.IdEmpleados == idEmpleados);
            var familiares = await _db.EmpleadoFamiliar.AsNoTracking()
                .Where(f => f.IdEmpresa == idEmpresa && f.IdEmpleados == idEmpleados)
                .OrderBy(f => f.Tipo)
                .ThenBy(f => f.Orden)
                .ThenBy(f => f.IdFamiliar)
                .ToListAsync();

            return ToDto(emp, ficha, familiares);
        }

        public async Task<EmpleadoFichaPersonalDto> UpsertAsync(EmpleadoFichaPersonalDto dto, int? idUsuario = null)
        {
            if (dto.IdEmpresa <= 0 || dto.IdEmpleados <= 0)
                throw new ArgumentException("Empresa y empleado son obligatorios.");

            var emp = await _db.EmpleadosP.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa && e.IdEmpleados == dto.IdEmpleados)
                ?? throw new InvalidOperationException("Empleado no existe en EmpleadosP.");

            emp.Cedula = NullIfEmpty(dto.Cedula);
            emp.Telefono = NullIfEmpty(dto.Telefono);

            var ficha = await _db.EmpleadoFichaPersonal.AsTracking()
                .FirstOrDefaultAsync(f => f.IdEmpresa == dto.IdEmpresa && f.IdEmpleados == dto.IdEmpleados);
            if (ficha is null)
            {
                ficha = new EmpleadoFichaPersonal
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdEmpleados = dto.IdEmpleados
                };
                _db.EmpleadoFichaPersonal.Add(ficha);
            }

            ficha.FechaNacimiento = dto.FechaNacimiento?.Date;
            ficha.Sexo = NullIfEmpty(dto.Sexo);
            ficha.Nacionalidad = string.IsNullOrWhiteSpace(dto.Nacionalidad)
                ? "Dominicana"
                : dto.Nacionalidad.Trim();
            ficha.EstadoCivil = NullIfEmpty(dto.EstadoCivil);
            ficha.Profesion = NullIfEmpty(dto.Profesion);
            ficha.Nss = NullIfEmpty(dto.Nss);
            ficha.FechaSalida = dto.FechaSalida?.Date;
            ficha.Alergias = NullIfEmpty(dto.Alergias);
            ficha.TipoSangre = NullIfEmpty(dto.TipoSangre);
            ficha.ObservacionesMedicas = NullIfEmpty(dto.ObservacionesMedicas);
            ficha.FechaActualizacion = DateTime.UtcNow;
            ficha.IdUsuario = idUsuario;

            var incoming = NormalizarFamiliares(dto.Familiares ?? new List<EmpleadoFamiliarDto>(), dto.IdEmpresa, dto.IdEmpleados);
            if (incoming.Count(f => f.Tipo == EmpleadoFamiliarTipos.Conyuge) > 1)
                throw new InvalidOperationException("Solo se registra un cónyuge.");

            var actuales = await _db.EmpleadoFamiliar.AsTracking()
                .Where(f => f.IdEmpresa == dto.IdEmpresa && f.IdEmpleados == dto.IdEmpleados)
                .ToListAsync();
            var keepIds = incoming.Where(x => x.IdFamiliar > 0).Select(x => x.IdFamiliar).ToHashSet();
            foreach (var extra in actuales.Where(a => !keepIds.Contains(a.IdFamiliar)))
                _db.EmpleadoFamiliar.Remove(extra);

            foreach (var row in incoming)
            {
                EmpleadoFamiliar entity;
                if (row.IdFamiliar > 0)
                {
                    entity = actuales.FirstOrDefault(a => a.IdFamiliar == row.IdFamiliar)
                        ?? throw new InvalidOperationException("Familiar no encontrado.");
                }
                else
                {
                    entity = new EmpleadoFamiliar
                    {
                        IdEmpresa = dto.IdEmpresa,
                        IdEmpleados = dto.IdEmpleados
                    };
                    _db.EmpleadoFamiliar.Add(entity);
                }

                entity.Tipo = row.Tipo;
                entity.Nombre = row.Nombre.Trim();
                entity.Cedula = NullIfEmpty(row.Cedula);
                entity.FechaNacimiento = row.FechaNacimiento?.Date;
                entity.Sexo = NullIfEmpty(row.Sexo);
                entity.Parentesco = NullIfEmpty(row.Parentesco);
                entity.Telefono = NullIfEmpty(row.Telefono);
                entity.Celular = NullIfEmpty(row.Celular);
                entity.Direccion = NullIfEmpty(row.Direccion);
                entity.Ocupacion = NullIfEmpty(row.Ocupacion);
                entity.ViveConEmpleado = row.ViveConEmpleado;
                entity.EsDependiente = row.EsDependiente;
                entity.Nota = NullIfEmpty(row.Nota);
                entity.Orden = row.Orden;
            }

            await _db.SaveChangesAsync();
            return await GetAsync(dto.IdEmpresa, dto.IdEmpleados);
        }

        private static List<EmpleadoFamiliarDto> NormalizarFamiliares(
            IEnumerable<EmpleadoFamiliarDto> rows, int idEmpresa, int idEmpleados)
        {
            var tipos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                EmpleadoFamiliarTipos.Conyuge,
                EmpleadoFamiliarTipos.Hijo,
                EmpleadoFamiliarTipos.Emergencia,
                EmpleadoFamiliarTipos.Otro
            };
            var list = new List<EmpleadoFamiliarDto>();
            var orden = 0;
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Nombre))
                    continue;
                var tipo = (row.Tipo ?? "").Trim().ToUpperInvariant();
                if (!tipos.Contains(tipo))
                    tipo = EmpleadoFamiliarTipos.Otro;
                row.Tipo = tipo;
                row.IdEmpresa = idEmpresa;
                row.IdEmpleados = idEmpleados;
                row.Orden = orden++;
                list.Add(row);
            }
            return list;
        }

        private static EmpleadoFichaPersonalDto ToDto(
            Empleados emp, EmpleadoFichaPersonal? ficha, IReadOnlyList<EmpleadoFamiliar> familiares)
        {
            return new EmpleadoFichaPersonalDto
            {
                IdFichaPersonal = ficha?.IdFichaPersonal ?? 0,
                IdEmpresa = emp.IdEmpresa,
                IdEmpleados = emp.IdEmpleados,
                NombreEmpleado = emp.Nombre,
                Cedula = emp.Cedula,
                Telefono = emp.Telefono,
                Celular = emp.Celular,
                Direccion = emp.Direccion,
                FechaNacimiento = ficha?.FechaNacimiento,
                Sexo = ficha?.Sexo,
                Nacionalidad = string.IsNullOrWhiteSpace(ficha?.Nacionalidad) ? "Dominicana" : ficha!.Nacionalidad,
                EstadoCivil = ficha?.EstadoCivil,
                Profesion = ficha?.Profesion,
                Nss = ficha?.Nss,
                FechaSalida = ficha?.FechaSalida,
                Alergias = ficha?.Alergias,
                TipoSangre = ficha?.TipoSangre,
                ObservacionesMedicas = ficha?.ObservacionesMedicas,
                CantidadHijos = familiares.Count(f => f.Tipo == EmpleadoFamiliarTipos.Hijo),
                Familiares = familiares.Select(f => new EmpleadoFamiliarDto
                {
                    IdFamiliar = f.IdFamiliar,
                    IdEmpresa = f.IdEmpresa,
                    IdEmpleados = f.IdEmpleados,
                    Tipo = f.Tipo,
                    Nombre = f.Nombre,
                    Cedula = f.Cedula,
                    FechaNacimiento = f.FechaNacimiento,
                    Sexo = f.Sexo,
                    Parentesco = f.Parentesco,
                    Telefono = f.Telefono,
                    Celular = f.Celular,
                    Direccion = f.Direccion,
                    Ocupacion = f.Ocupacion,
                    ViveConEmpleado = f.ViveConEmpleado,
                    EsDependiente = f.EsDependiente,
                    Nota = f.Nota,
                    Orden = f.Orden
                }).ToList()
            };
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
