using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhCatalogoService : IRrhhCatalogoService
    {
        private readonly AlahiaPosContext _db;

        public RrhhCatalogoService(AlahiaPosContext db) => _db = db;

        public async Task<IReadOnlyList<RrhhDepartamento>> GetDepartamentosAsync(int idEmpresa) =>
            await _db.RrhhDepartamento.AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.Nombre)
                .ToListAsync();

        public async Task<RrhhDepartamento> UpsertDepartamentoAsync(RrhhDepartamento row)
        {
            if (row.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(row.Nombre))
                throw new ArgumentException("Empresa y nombre son obligatorios.");
            row.Codigo = string.IsNullOrWhiteSpace(row.Codigo)
                ? Slug(row.Nombre)
                : row.Codigo.Trim().ToUpperInvariant();

            if (row.IdDepartamento > 0)
            {
                var existing = await _db.RrhhDepartamento.AsTracking().FirstOrDefaultAsync(x =>
                    x.IdDepartamento == row.IdDepartamento && x.IdEmpresa == row.IdEmpresa)
                    ?? throw new InvalidOperationException("Departamento no encontrado.");
                existing.Codigo = row.Codigo;
                existing.Nombre = row.Nombre.Trim();
                existing.Descripcion = NullIfEmpty(row.Descripcion);
                existing.IdResponsable = row.IdResponsable is > 0 ? row.IdResponsable : null;
                existing.Ubicacion = NullIfEmpty(row.Ubicacion);
                existing.Telefono = NullIfEmpty(row.Telefono);
                existing.Email = NullIfEmpty(row.Email);
                existing.Activo = row.Activo;
                await _db.SaveChangesAsync();
                return existing;
            }

            row.Descripcion = NullIfEmpty(row.Descripcion);
            row.Ubicacion = NullIfEmpty(row.Ubicacion);
            row.Telefono = NullIfEmpty(row.Telefono);
            row.Email = NullIfEmpty(row.Email);
            row.IdResponsable = row.IdResponsable is > 0 ? row.IdResponsable : null;
            _db.RrhhDepartamento.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhCargoDto>> GetCargosAsync(int idEmpresa)
        {
            var rows = await _db.RrhhCargo.AsNoTracking()
                .Include(c => c.Beneficios)
                .Where(x => x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
            var jornadaIds = rows.Where(c => c.IdJornada.HasValue).Select(c => c.IdJornada!.Value).Distinct().ToList();
            var jornadas = await _db.RrhhJornada.AsNoTracking()
                .Include(j => j.Dias)
                .Where(j => jornadaIds.Contains(j.IdJornada))
                .ToDictionaryAsync(j => j.IdJornada);
            var benIds = rows.SelectMany(c => c.Beneficios.Select(b => b.IdBeneficio)).Distinct().ToList();
            var beneficios = await _db.RrhhBeneficio.AsNoTracking()
                .Where(b => benIds.Contains(b.IdBeneficio))
                .ToDictionaryAsync(b => b.IdBeneficio);
            return rows.Select(c => ToCargoDto(c, jornadas, beneficios)).ToList();
        }

        public async Task<RrhhCargoDto> UpsertCargoAsync(RrhhCargoDto row, int? idUsuario = null)
        {
            if (row.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(row.Nombre))
                throw new ArgumentException("Empresa y nombre son obligatorios.");
            row.Codigo = string.IsNullOrWhiteSpace(row.Codigo)
                ? Slug(row.Nombre)
                : row.Codigo.Trim().ToUpperInvariant();
            if (row.SalarioBase < 0)
                throw new ArgumentException("El salario del cargo no puede ser negativo.");

            if (row.Dias is { Count: > 0 })
            {
                var idJornada = row.IdJornada ?? 0;
                var compartida = idJornada > 0 && await _db.RrhhCargo.AsNoTracking().AnyAsync(c =>
                    c.IdEmpresa == row.IdEmpresa && c.IdJornada == idJornada && c.IdCargo != row.IdCargo);
                var jornada = await UpsertJornadaAsync(new RrhhJornadaDto
                {
                    IdJornada = compartida ? 0 : idJornada,
                    IdEmpresa = row.IdEmpresa,
                    Nombre = string.IsNullOrWhiteSpace(row.NombreJornada)
                        ? $"Horario · {row.Nombre.Trim()}"
                        : row.NombreJornada.Trim(),
                    MinutosTardanzaGracia = row.MinutosTardanzaGracia > 0 ? row.MinutosTardanzaGracia : 10,
                    HorasSemanales = 44,
                    Activo = true,
                    Dias = row.Dias
                });
                row.IdJornada = jornada.IdJornada;
            }

            RrhhCargo entity;
            decimal salarioAnterior = 0;
            string freqAnterior = "QUINCENAL";
            if (row.IdCargo > 0)
            {
                entity = await _db.RrhhCargo.AsTracking().Include(c => c.Beneficios)
                    .FirstOrDefaultAsync(x => x.IdCargo == row.IdCargo && x.IdEmpresa == row.IdEmpresa)
                    ?? throw new InvalidOperationException("Cargo no encontrado.");
                salarioAnterior = entity.SalarioBase;
                freqAnterior = entity.FrecuenciaPago;
            }
            else
            {
                entity = new RrhhCargo { IdEmpresa = row.IdEmpresa };
                _db.RrhhCargo.Add(entity);
            }

            var salarioCambio = entity.IdCargo > 0 &&
                (salarioAnterior != row.SalarioBase ||
                 !string.Equals(freqAnterior, row.FrecuenciaPago, StringComparison.OrdinalIgnoreCase));

            entity.Codigo = row.Codigo;
            entity.Nombre = row.Nombre.Trim();
            entity.Descripcion = NullIfEmpty(row.Descripcion);
            entity.IdJornada = row.IdJornada is > 0 ? row.IdJornada : null;
            entity.SalarioBase = row.SalarioBase;
            entity.Moneda = string.IsNullOrWhiteSpace(row.Moneda) ? "DOP" : row.Moneda.Trim().ToUpperInvariant();
            entity.FrecuenciaPago = string.IsNullOrWhiteSpace(row.FrecuenciaPago) ? "QUINCENAL" : row.FrecuenciaPago.Trim().ToUpperInvariant();
            entity.TipoEmpleado = string.IsNullOrWhiteSpace(row.TipoEmpleado) ? "FIJO" : row.TipoEmpleado.Trim().ToUpperInvariant();
            entity.Activo = row.Activo;

            if (salarioCambio)
            {
                _db.RrhhCargoSalarioHistorial.Add(new RrhhCargoSalarioHistorial
                {
                    IdCargo = entity.IdCargo,
                    IdEmpresa = entity.IdEmpresa,
                    SalarioAnterior = salarioAnterior,
                    SalarioNuevo = row.SalarioBase,
                    FrecuenciaPago = entity.FrecuenciaPago,
                    VigenteDesde = DateTime.UtcNow.Date,
                    Motivo = "Actualización salarial del cargo",
                    IdUsuario = idUsuario
                });
            }

            var ids = (row.IdBeneficios ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            var actuales = entity.Beneficios.ToList();
            foreach (var extra in actuales.Where(b => !ids.Contains(b.IdBeneficio)))
                _db.RrhhCargoBeneficio.Remove(extra);
            foreach (var idBen in ids.Where(id => actuales.All(b => b.IdBeneficio != id)))
                entity.Beneficios.Add(new RrhhCargoBeneficio { IdBeneficio = idBen });

            await _db.SaveChangesAsync();
            row.IdCargo = entity.IdCargo;
            var saved = await GetCargosAsync(row.IdEmpresa);
            return saved.First(c => c.IdCargo == entity.IdCargo);
        }

        public async Task<IReadOnlyList<RrhhBeneficio>> GetBeneficiosAsync(int idEmpresa) =>
            await _db.RrhhBeneficio.AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.Nombre)
                .ToListAsync();

        public async Task<RrhhBeneficio> UpsertBeneficioAsync(RrhhBeneficio row)
        {
            if (row.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(row.Nombre))
                throw new ArgumentException("Empresa y nombre son obligatorios.");
            row.Codigo = string.IsNullOrWhiteSpace(row.Codigo)
                ? Slug(row.Nombre)
                : row.Codigo.Trim().ToUpperInvariant();
            if (row.Monto < 0)
                throw new ArgumentException("El monto del beneficio no puede ser negativo.");
            row.TipoCalculo = string.IsNullOrWhiteSpace(row.TipoCalculo) ? "MONTO_FIJO" : row.TipoCalculo.Trim().ToUpperInvariant();
            if (row.TipoCalculo is not ("MONTO_FIJO" or "PORCENTAJE_SALARIO" or "DESCUENTO_CONSUMO"))
                throw new ArgumentException("Tipo de cálculo no válido.");
            if (row.TipoCalculo is "PORCENTAJE_SALARIO" or "DESCUENTO_CONSUMO")
            {
                if (row.Monto > 100)
                    throw new ArgumentException("El porcentaje no puede ser mayor a 100.");
            }
            row.Periodicidad = string.IsNullOrWhiteSpace(row.Periodicidad) ? "MENSUAL" : row.Periodicidad.Trim().ToUpperInvariant();
            if (row.TipoCalculo == "DESCUENTO_CONSUMO")
            {
                row.FormaDesembolso = RrhhBeneficioFormas.Ninguno;
                row.EnEspecie = true;
                row.AfectaNomina = false;
            }
            row.FormaDesembolso = NormalizarFormaDesembolso(row);

            RrhhBeneficio entity;
            if (row.IdBeneficio > 0)
            {
                entity = await _db.RrhhBeneficio.AsTracking().FirstOrDefaultAsync(x =>
                    x.IdBeneficio == row.IdBeneficio && x.IdEmpresa == row.IdEmpresa)
                    ?? throw new InvalidOperationException("Beneficio no encontrado.");
            }
            else
            {
                entity = new RrhhBeneficio { IdEmpresa = row.IdEmpresa };
                _db.RrhhBeneficio.Add(entity);
            }

            entity.Codigo = row.Codigo;
            entity.Nombre = row.Nombre.Trim();
            entity.Descripcion = NullIfEmpty(row.Descripcion);
            entity.TipoCalculo = row.TipoCalculo;
            entity.Monto = row.Monto;
            entity.Periodicidad = row.Periodicidad;
            entity.FormaDesembolso = row.FormaDesembolso;
            entity.AfectaNomina = row.TipoCalculo != "DESCUENTO_CONSUMO"
                && row.FormaDesembolso == RrhhBeneficioFormas.Nomina;
            entity.EnEspecie = row.EnEspecie;
            entity.DiaPagoMes = row.FormaDesembolso == RrhhBeneficioFormas.PagoAparte ? row.DiaPagoMes : null;
            entity.MetodoPago = row.FormaDesembolso == RrhhBeneficioFormas.PagoAparte ? row.MetodoPago : null;
            entity.DescontarConsumoNomina = row.TipoCalculo == "DESCUENTO_CONSUMO" && row.DescontarConsumoNomina;
            entity.Activo = row.Activo;

            if (entity.AfectaNomina)
                await AsegurarConceptoNominaAsync(entity.IdEmpresa, entity.Codigo, entity.Nombre);

            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<IReadOnlyList<int>> GetCargoIdsConBeneficioAsync(int idBeneficio) =>
            await _db.RrhhCargoBeneficio.AsNoTracking()
                .Where(x => x.IdBeneficio == idBeneficio)
                .Select(x => x.IdCargo)
                .Distinct()
                .ToListAsync();

        private static string NormalizarFormaDesembolso(RrhhBeneficio row)
        {
            var forma = (row.FormaDesembolso ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(forma))
                forma = row.AfectaNomina ? RrhhBeneficioFormas.Nomina : RrhhBeneficioFormas.Ninguno;

            if (forma is not (RrhhBeneficioFormas.Nomina or RrhhBeneficioFormas.PagoAparte or RrhhBeneficioFormas.Ninguno))
                throw new ArgumentException("Indique si el beneficio se paga con la nómina, en fecha propia o no se paga.");

            if (forma == RrhhBeneficioFormas.PagoAparte)
            {
                if (row.DiaPagoMes is not (>= 1 and <= 28))
                    throw new ArgumentException("Indique el día del mes (1 a 28) en que se paga este beneficio.");
                var metodo = (row.MetodoPago ?? "").Trim().ToUpperInvariant();
                if (metodo is not ("EFECTIVO" or "TRANSFERENCIA"))
                    throw new ArgumentException("Seleccione si el pago aparte es en efectivo o por transferencia.");
                row.MetodoPago = metodo;
            }

            return forma;
        }

        private async Task AsegurarConceptoNominaAsync(int idEmpresa, string codigo, string nombre)
        {
            var exists = await _db.NominaConcepto.AsNoTracking()
                .AnyAsync(c => c.IdEmpresa == idEmpresa && c.ConceptCode == codigo);
            if (exists) return;
            _db.NominaConcepto.Add(new NominaConcepto
            {
                IdEmpresa = idEmpresa,
                ConceptCode = codigo,
                Nombre = nombre,
                Categoria = "BENEFICIO",
                Activo = true
            });
        }

        private static RrhhCargoDto ToCargoDto(
            RrhhCargo c,
            IReadOnlyDictionary<int, RrhhJornada> jornadas,
            IReadOnlyDictionary<int, RrhhBeneficio> beneficios)
        {
            jornadas.TryGetValue(c.IdJornada ?? 0, out var jornada);
            var dias = jornada is null ? new List<RrhhJornadaDiaDto>() : ToDto(jornada).Dias;
            var ids = (c.Beneficios ?? new List<RrhhCargoBeneficio>()).Select(b => b.IdBeneficio).ToList();
            return new RrhhCargoDto
            {
                IdCargo = c.IdCargo,
                IdEmpresa = c.IdEmpresa,
                IdJornada = c.IdJornada,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                SalarioBase = c.SalarioBase,
                Moneda = c.Moneda,
                FrecuenciaPago = c.FrecuenciaPago,
                TipoEmpleado = c.TipoEmpleado,
                Activo = c.Activo,
                NombreJornada = jornada?.Nombre,
                ResumenHorario = ResumenHorario(dias),
                Dias = dias,
                MinutosTardanzaGracia = jornada?.MinutosTardanzaGracia ?? 10,
                IdBeneficios = ids,
                Beneficios = ids.Where(beneficios.ContainsKey).Select(id => beneficios[id]).ToList()
            };
        }

        private static string ResumenHorario(IEnumerable<RrhhJornadaDiaDto> dias)
        {
            var lab = dias.Where(d => d.EsLaborable).OrderBy(d => d.DiaSemana).ToList();
            if (lab.Count == 0) return "Sin horario";
            string[] n = { "", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            var first = lab[0];
            var same = lab.All(d => d.HoraEntrada == first.HoraEntrada && d.HoraSalida == first.HoraSalida);
            if (same)
                return $"{n[lab[0].DiaSemana]}–{n[lab[^1].DiaSemana]} {first.HoraEntrada}–{first.HoraSalida}";
            return string.Join(", ", lab.Select(d => $"{n[d.DiaSemana]} {d.HoraEntrada}–{d.HoraSalida}"));
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public async Task<IReadOnlyList<RrhhJornadaDto>> GetJornadasAsync(int idEmpresa)
        {
            var rows = await _db.RrhhJornada.AsNoTracking()
                .Include(j => j.Dias)
                .Where(j => j.IdEmpresa == idEmpresa)
                .OrderBy(j => j.Nombre)
                .ToListAsync();
            return rows.Select(ToDto).ToList();
        }

        public async Task<RrhhJornadaDto> UpsertJornadaAsync(RrhhJornadaDto dto)
        {
            if (dto.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ArgumentException("Empresa y nombre son obligatorios.");

            RrhhJornada entity;
            if (dto.IdJornada > 0)
            {
                entity = await _db.RrhhJornada.AsTracking().Include(j => j.Dias)
                    .FirstOrDefaultAsync(j => j.IdJornada == dto.IdJornada && j.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Jornada no encontrada.");
                entity.Nombre = dto.Nombre.Trim();
                entity.HorasSemanales = dto.HorasSemanales;
                entity.MinutosTardanzaGracia = dto.MinutosTardanzaGracia;
                entity.Activo = dto.Activo;
                _db.RrhhJornadaDia.RemoveRange(entity.Dias);
                await _db.SaveChangesAsync();
                entity.Dias.Clear();
            }
            else
            {
                entity = new RrhhJornada
                {
                    IdEmpresa = dto.IdEmpresa,
                    Nombre = dto.Nombre.Trim(),
                    HorasSemanales = dto.HorasSemanales,
                    MinutosTardanzaGracia = dto.MinutosTardanzaGracia,
                    Activo = dto.Activo
                };
                _db.RrhhJornada.Add(entity);
            }

            var dias = dto.Dias.Count == 0 ? DefaultDias() : dto.Dias;
            foreach (var d in dias)
            {
                var entrada = RrhhTime.Parse(d.HoraEntrada);
                var salida = RrhhTime.Parse(d.HoraSalida);
                var recesoIni = RrhhTime.Parse(d.RecesoInicio);
                var recesoFin = RrhhTime.Parse(d.RecesoFin);
                var minutos = d.MinutosEsperados;
                if (minutos <= 0 && d.EsLaborable && entrada.HasValue && salida.HasValue)
                {
                    minutos = RrhhTime.MinutosEntre(entrada.Value, salida.Value);
                    if (recesoIni.HasValue && recesoFin.HasValue)
                        minutos -= RrhhTime.MinutosEntre(recesoIni.Value, recesoFin.Value);
                    if (minutos < 0) minutos = 0;
                }

                entity.Dias.Add(new RrhhJornadaDia
                {
                    DiaSemana = d.DiaSemana,
                    EsLaborable = d.EsLaborable,
                    HoraEntrada = entrada,
                    HoraSalida = salida,
                    RecesoInicio = recesoIni,
                    RecesoFin = recesoFin,
                    MinutosEsperados = minutos
                });
            }

            await _db.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<IReadOnlyList<RrhhTurnoDto>> GetTurnosAsync(int idEmpresa)
        {
            var rows = await _db.RrhhTurno.AsNoTracking()
                .Where(t => t.IdEmpresa == idEmpresa)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            return rows.Select(t => new RrhhTurnoDto
            {
                IdTurno = t.IdTurno,
                IdEmpresa = t.IdEmpresa,
                Codigo = t.Codigo,
                Nombre = t.Nombre,
                HoraEntrada = RrhhTime.Format(t.HoraEntrada) ?? "08:00",
                HoraSalida = RrhhTime.Format(t.HoraSalida) ?? "17:00",
                RecesoInicio = RrhhTime.Format(t.RecesoInicio),
                RecesoFin = RrhhTime.Format(t.RecesoFin),
                Activo = t.Activo
            }).ToList();
        }

        public async Task<RrhhTurnoDto> UpsertTurnoAsync(RrhhTurnoDto dto)
        {
            if (dto.IdEmpresa <= 0 || string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ArgumentException("Empresa y nombre son obligatorios.");
            var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? Slug(dto.Nombre) : dto.Codigo.Trim().ToUpperInvariant();

            RrhhTurno entity;
            if (dto.IdTurno > 0)
            {
                entity = await _db.RrhhTurno.AsTracking().FirstOrDefaultAsync(t => t.IdTurno == dto.IdTurno && t.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Turno no encontrado.");
            }
            else
            {
                entity = new RrhhTurno { IdEmpresa = dto.IdEmpresa };
                _db.RrhhTurno.Add(entity);
            }

            entity.Codigo = codigo;
            entity.Nombre = dto.Nombre.Trim();
            entity.HoraEntrada = RrhhTime.ParseRequired(dto.HoraEntrada, "08:00");
            entity.HoraSalida = RrhhTime.ParseRequired(dto.HoraSalida, "17:00");
            entity.RecesoInicio = RrhhTime.Parse(dto.RecesoInicio);
            entity.RecesoFin = RrhhTime.Parse(dto.RecesoFin);
            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();
            dto.IdTurno = entity.IdTurno;
            dto.Codigo = entity.Codigo;
            return dto;
        }

        public async Task<IReadOnlyList<RrhhEmpleadoHorarioDto>> GetHorariosAsync(int idEmpresa, int? idEmpleados = null)
        {
            var q = from h in _db.RrhhEmpleadoHorario.AsNoTracking()
                    join e in _db.EmpleadosP.AsNoTracking() on h.IdEmpleados equals e.IdEmpleados
                    join j in _db.RrhhJornada.AsNoTracking() on h.IdJornada equals j.IdJornada
                    join t in _db.RrhhTurno.AsNoTracking() on h.IdTurno equals t.IdTurno into tj
                    from t in tj.DefaultIfEmpty()
                    where h.IdEmpresa == idEmpresa && e.IdEmpresa == idEmpresa
                    select new RrhhEmpleadoHorarioDto
                    {
                        IdEmpleadoHorario = h.IdEmpleadoHorario,
                        IdEmpresa = h.IdEmpresa,
                        IdEmpleados = h.IdEmpleados,
                        IdJornada = h.IdJornada,
                        IdTurno = h.IdTurno,
                        VigenteDesde = h.VigenteDesde,
                        VigenteHasta = h.VigenteHasta,
                        Activo = h.Activo,
                        NombreJornada = j.Nombre,
                        NombreTurno = t != null ? t.Nombre : null,
                        NombreEmpleado = e.Nombre
                    };
            if (idEmpleados.HasValue && idEmpleados.Value > 0)
                q = q.Where(x => x.IdEmpleados == idEmpleados.Value);
            return await q.OrderByDescending(x => x.VigenteDesde).ToListAsync();
        }

        public async Task<RrhhEmpleadoHorarioDto> UpsertHorarioAsync(RrhhEmpleadoHorarioDto dto)
        {
            if (dto.IdEmpresa <= 0 || dto.IdEmpleados <= 0 || dto.IdJornada <= 0)
                throw new ArgumentException("Empresa, empleado y jornada son obligatorios.");

            var emp = await _db.EmpleadosP.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa && e.IdEmpleados == dto.IdEmpleados);
            if (emp is null)
                throw new InvalidOperationException("Empleado no existe en EmpleadosP.");

            RrhhEmpleadoHorario entity;
            if (dto.IdEmpleadoHorario > 0)
            {
                entity = await _db.RrhhEmpleadoHorario.AsTracking().FirstOrDefaultAsync(h =>
                    h.IdEmpleadoHorario == dto.IdEmpleadoHorario && h.IdEmpresa == dto.IdEmpresa)
                    ?? throw new InvalidOperationException("Horario no encontrado.");
            }
            else
            {
                entity = new RrhhEmpleadoHorario { IdEmpresa = dto.IdEmpresa };
                _db.RrhhEmpleadoHorario.Add(entity);
            }

            entity.IdEmpleados = dto.IdEmpleados;
            entity.IdJornada = dto.IdJornada;
            entity.IdTurno = dto.IdTurno;
            entity.VigenteDesde = dto.VigenteDesde.Date;
            entity.VigenteHasta = dto.VigenteHasta?.Date;
            entity.Activo = dto.Activo;

            var laboral = await _db.EmpleadoLaboral.AsTracking().FirstOrDefaultAsync(x =>
                x.IdEmpresa == dto.IdEmpresa && x.IdEmpleados == dto.IdEmpleados);
            if (laboral != null)
                laboral.IdJornada = dto.IdJornada;

            await _db.SaveChangesAsync();
            dto.IdEmpleadoHorario = entity.IdEmpleadoHorario;
            dto.NombreEmpleado = emp.Nombre;
            return dto;
        }

        public async Task<IReadOnlyList<RrhhTipoAusencia>> GetTiposAusenciaAsync(int idEmpresa) =>
            await _db.RrhhTipoAusencia.AsNoTracking()
                .Where(t => t.IdEmpresa == idEmpresa && t.Activo)
                .OrderBy(t => t.Nombre)
                .ToListAsync();

        private static RrhhJornadaDto ToDto(RrhhJornada j) => new()
        {
            IdJornada = j.IdJornada,
            IdEmpresa = j.IdEmpresa,
            Nombre = j.Nombre,
            HorasSemanales = j.HorasSemanales,
            MinutosTardanzaGracia = j.MinutosTardanzaGracia,
            Activo = j.Activo,
            Dias = (j.Dias ?? new List<RrhhJornadaDia>())
                .OrderBy(d => d.DiaSemana)
                .Select(d => new RrhhJornadaDiaDto
                {
                    IdJornadaDia = d.IdJornadaDia,
                    DiaSemana = d.DiaSemana,
                    EsLaborable = d.EsLaborable,
                    HoraEntrada = RrhhTime.Format(d.HoraEntrada),
                    HoraSalida = RrhhTime.Format(d.HoraSalida),
                    RecesoInicio = RrhhTime.Format(d.RecesoInicio),
                    RecesoFin = RrhhTime.Format(d.RecesoFin),
                    MinutosEsperados = d.MinutosEsperados
                }).ToList()
        };

        private static List<RrhhJornadaDiaDto> DefaultDias()
        {
            var list = new List<RrhhJornadaDiaDto>();
            for (byte d = 1; d <= 7; d++)
            {
                var laborable = d <= 5;
                list.Add(new RrhhJornadaDiaDto
                {
                    DiaSemana = d,
                    EsLaborable = laborable,
                    HoraEntrada = laborable ? "08:00" : null,
                    HoraSalida = laborable ? "17:00" : null,
                    RecesoInicio = laborable ? "12:00" : null,
                    RecesoFin = laborable ? "13:00" : null,
                    MinutosEsperados = laborable ? 480 : 0
                });
            }
            return list;
        }

        private static string Slug(string nombre)
        {
            var raw = new string(nombre.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            return raw.Length > 40 ? raw[..40] : raw;
        }
    }
}
