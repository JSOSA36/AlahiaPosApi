using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.Rrhh;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Expediente laboral anclado a EmpleadosP. Sincroniza asignaciones hacia PayrollDbContext
    /// para el EvaluationContextBuilder (sin lógica AFP/ISR).
    /// </summary>
    public sealed class EmpleadoLaboralService : IEmpleadoLaboralService
    {
        private readonly AlahiaPosContext _db;
        private readonly PayrollDbContext _payroll;

        public EmpleadoLaboralService(AlahiaPosContext db, PayrollDbContext payroll)
        {
            _db = db;
            _payroll = payroll;
        }

        public Task<EmpleadoLaboral?> GetLaboralAsync(int idEmpresa, int idEmpleados) =>
            _db.EmpleadoLaboral.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.IdEmpleados == idEmpleados);

        public async Task<EmpleadoLaboralVistaDto?> GetLaboralVistaAsync(int idEmpresa, int idEmpleados)
        {
            var row = await GetLaboralAsync(idEmpresa, idEmpleados);
            if (row is null) return null;
            return await ToVistaAsync(row);
        }

        public async Task<EmpleadoLaboralVistaDto> UpsertLaboralAsync(
            EmpleadoLaboral laboral,
            string? motivoCambioSalario = null,
            int? idUsuario = null)
        {
            var emp = await _db.EmpleadosP.FirstOrDefaultAsync(e =>
                e.IdEmpresa == laboral.IdEmpresa && e.IdEmpleados == laboral.IdEmpleados);
            if (emp is null)
                throw new InvalidOperationException("Empleado no existe en EmpleadosP.");
            if (laboral.IdDepartamento is not > 0)
                throw new ArgumentException("Asigne el departamento del empleado.");
            if (laboral.IdCargo is not > 0)
                throw new ArgumentException("Asigne el cargo del empleado.");

            var cargo = await _db.RrhhCargo.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCargo == laboral.IdCargo && c.IdEmpresa == laboral.IdEmpresa)
                ?? throw new InvalidOperationException("El cargo no existe.");

            var existing = await _db.EmpleadoLaboral
                .AsTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == laboral.IdEmpresa && x.IdEmpleados == laboral.IdEmpleados);

            if (existing is null)
            {
                laboral.FechaActualizacion = DateTime.UtcNow;
                laboral.SalarioBase = cargo.SalarioBase;
                laboral.FrecuenciaPago = cargo.FrecuenciaPago;
                laboral.Moneda = cargo.Moneda;
                laboral.IdJornada = cargo.IdJornada;
                laboral.TipoEmpleado = string.IsNullOrWhiteSpace(laboral.TipoEmpleado) ? cargo.TipoEmpleado : laboral.TipoEmpleado;
                laboral.Correo = string.IsNullOrWhiteSpace(laboral.Correo) ? null : laboral.Correo.Trim();
                _db.EmpleadoLaboral.Add(laboral);
                existing = laboral;
            }
            else
            {
                existing.TipoEmpleado = string.IsNullOrWhiteSpace(laboral.TipoEmpleado) ? cargo.TipoEmpleado : laboral.TipoEmpleado;
                existing.IdDepartamento = laboral.IdDepartamento;
                existing.IdCargo = laboral.IdCargo;
                existing.FechaIngreso = laboral.FechaIngreso;
                existing.EstadoLaboral = laboral.EstadoLaboral;
                existing.Correo = string.IsNullOrWhiteSpace(laboral.Correo) ? null : laboral.Correo.Trim();
                existing.SalarioBase = cargo.SalarioBase;
                existing.FrecuenciaPago = cargo.FrecuenciaPago;
                existing.Moneda = cargo.Moneda;
                existing.IdJornada = cargo.IdJornada;
                existing.FechaActualizacion = DateTime.UtcNow;
            }

            await EnsureCatalogoBaseAsync(laboral.IdEmpresa);
            await FillCatalogNamesAsync(existing);
            await AplicarPaqueteCargoAsync(existing, cargo, idUsuario);
            await _db.SaveChangesAsync();
            await SyncPayrollMirrorAsync(existing);
            return await ToVistaAsync(existing);
        }

        public async Task SincronizarEmpleadosDelCargoAsync(int idEmpresa, int idCargo, int? idUsuario = null)
        {
            var cargo = await _db.RrhhCargo.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCargo == idCargo && c.IdEmpresa == idEmpresa);
            if (cargo is null) return;

            var empleados = await _db.EmpleadoLaboral.AsTracking()
                .Where(e => e.IdEmpresa == idEmpresa && e.IdCargo == idCargo)
                .ToListAsync();
            foreach (var emp in empleados)
            {
                emp.SalarioBase = cargo.SalarioBase;
                emp.FrecuenciaPago = cargo.FrecuenciaPago;
                emp.Moneda = cargo.Moneda;
                emp.IdJornada = cargo.IdJornada;
                emp.FechaActualizacion = DateTime.UtcNow;
                await AplicarPaqueteCargoAsync(emp, cargo, idUsuario);
            }

            if (empleados.Count > 0)
                await _db.SaveChangesAsync();
            foreach (var emp in empleados)
                await SyncPayrollMirrorAsync(emp);
        }

        public async Task<IReadOnlyList<EmpleadoSalarioHistorial>> GetHistorialSalarialAsync(int idEmpresa, int idEmpleados) =>
            await _db.EmpleadoSalarioHistorial.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa && h.IdEmpleados == idEmpleados)
                .OrderByDescending(h => h.FechaRegistro)
                .ToListAsync();

        private async Task FillCatalogNamesAsync(EmpleadoLaboral laboral)
        {
            if (laboral.IdDepartamento is > 0)
            {
                var d = await _db.RrhhDepartamento.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdDepartamento == laboral.IdDepartamento && x.IdEmpresa == laboral.IdEmpresa);
                if (d != null) laboral.Departamento = d.Nombre;
            }
            if (laboral.IdCargo is > 0)
            {
                var c = await _db.RrhhCargo.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdCargo == laboral.IdCargo && x.IdEmpresa == laboral.IdEmpresa);
                if (c != null) laboral.Cargo = c.Nombre;
            }
        }

        public async Task EnsureCatalogoBaseAsync(int idEmpresa)
        {
            var codes = new (string Code, string Nombre, string Cat, bool Legal)[]
            {
                ("SUELDO_BASE", "Sueldo base", "INGRESO", false),
                ("HORAS_EXTRA", "Horas extras", "INGRESO", false),
                ("COMISION", "Comisiones", "INGRESO", false),
                ("BONIFICACION", "Bonificaciones", "INGRESO", false),
                ("COMBUSTIBLE", "Combustible", "BENEFICIO", false),
                ("SEGURO_MEDICO", "Seguro médico privado", "BENEFICIO", false),
                ("PRESTAMO", "Préstamo empleado", "DEDUCCION", false),
                ("ANTICIPO", "Anticipo de salario", "DEDUCCION", false),
                ("CONSUMO_EMPLEADO", "Consumo colaborador", "DEDUCCION", false),
                ("DESCUENTO_ASISTENCIA", "Descuento por asistencia", "DEDUCCION", false),
                ("AFP_EMPLEADO", "AFP empleado", "DEDUCCION", true),
                ("SFS_EMPLEADO", "SFS empleado", "DEDUCCION", true),
                ("ISR_EMPLEADO", "ISR empleado", "DEDUCCION", true)
            };

            var existing = await _db.NominaConcepto
                .Where(c => c.IdEmpresa == idEmpresa)
                .Select(c => c.ConceptCode)
                .ToListAsync();

            foreach (var (code, nombre, cat, legal) in codes)
            {
                if (existing.Contains(code)) continue;
                _db.NominaConcepto.Add(new NominaConcepto
                {
                    IdEmpresa = idEmpresa,
                    ConceptCode = code,
                    Nombre = nombre,
                    Categoria = cat,
                    EsLegal = legal,
                    Activo = true
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<NominaConcepto>> GetConceptosAsync(int idEmpresa)
        {
            await EnsureCatalogoBaseAsync(idEmpresa);
            return await _db.NominaConcepto.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                .OrderBy(c => c.Categoria).ThenBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<NominaConcepto> UpsertConceptoAsync(NominaConcepto concepto)
        {
            var existing = await _db.NominaConcepto.AsTracking().FirstOrDefaultAsync(c =>
                c.IdEmpresa == concepto.IdEmpresa && c.ConceptCode == concepto.ConceptCode);

            if (existing is null)
            {
                _db.NominaConcepto.Add(concepto);
                await _db.SaveChangesAsync();
                return concepto;
            }

            existing.Nombre = concepto.Nombre;
            existing.Categoria = concepto.Categoria;
            existing.EsLegal = concepto.EsLegal;
            existing.Activo = concepto.Activo;
            existing.Descripcion = concepto.Descripcion;
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<IReadOnlyList<NominaConceptoAsignacion>> GetAsignacionesAsync(int idEmpresa, int idEmpleados) =>
            await _db.NominaConceptoAsignacion.AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == idEmpleados && a.Activo)
                .OrderBy(a => a.ConceptCode)
                .ToListAsync();

        public async Task<NominaConceptoAsignacion> UpsertAsignacionAsync(NominaConceptoAsignacion asignacion)
        {
            var existing = await _db.NominaConceptoAsignacion.AsTracking().FirstOrDefaultAsync(a =>
                a.IdEmpresa == asignacion.IdEmpresa
                && a.IdEmpleados == asignacion.IdEmpleados
                && a.ConceptCode == asignacion.ConceptCode
                && a.Activo);

            if (existing is null)
            {
                _db.NominaConceptoAsignacion.Add(asignacion);
                await _db.SaveChangesAsync();
                await SyncAssignmentToPayrollAsync(asignacion);
                return asignacion;
            }

            existing.MontoFijo = asignacion.MontoFijo;
            existing.Tasa = asignacion.Tasa;
            existing.Periodicidad = asignacion.Periodicidad;
            existing.VigenteDesde = asignacion.VigenteDesde;
            existing.VigenteHasta = asignacion.VigenteHasta;
            existing.Activo = asignacion.Activo;
            existing.Nota = asignacion.Nota;
            await _db.SaveChangesAsync();
            await SyncAssignmentToPayrollAsync(existing);
            return existing;
        }

        private async Task<EmpleadoLaboralVistaDto> ToVistaAsync(EmpleadoLaboral laboral)
        {
            await FillCatalogNamesAsync(laboral);
            var dto = new EmpleadoLaboralVistaDto
            {
                IdEmpleadoLaboral = laboral.IdEmpleadoLaboral,
                IdEmpresa = laboral.IdEmpresa,
                IdEmpleados = laboral.IdEmpleados,
                TipoEmpleado = laboral.TipoEmpleado,
                IdDepartamento = laboral.IdDepartamento,
                IdCargo = laboral.IdCargo,
                FechaIngreso = laboral.FechaIngreso,
                EstadoLaboral = laboral.EstadoLaboral,
                Correo = laboral.Correo,
                Departamento = laboral.Departamento,
                Cargo = laboral.Cargo
            };
            if (laboral.IdCargo is > 0)
                dto.PaqueteCargo = await CargarPaqueteAsync(laboral.IdEmpresa, laboral.IdCargo.Value);
            return dto;
        }

        private async Task<RrhhCargoPaqueteDto?> CargarPaqueteAsync(int idEmpresa, int idCargo)
        {
            var cargo = await _db.RrhhCargo.AsNoTracking()
                .Include(c => c.Beneficios)
                .FirstOrDefaultAsync(c => c.IdCargo == idCargo && c.IdEmpresa == idEmpresa);
            if (cargo is null) return null;
            var jornada = cargo.IdJornada is > 0
                ? await _db.RrhhJornada.AsNoTracking().Include(j => j.Dias)
                    .FirstOrDefaultAsync(j => j.IdJornada == cargo.IdJornada)
                : null;
            var dias = jornada is null
                ? new List<RrhhJornadaDiaDto>()
                : (jornada.Dias ?? new List<RrhhJornadaDia>()).OrderBy(d => d.DiaSemana).Select(d => new RrhhJornadaDiaDto
                {
                    IdJornadaDia = d.IdJornadaDia,
                    DiaSemana = d.DiaSemana,
                    EsLaborable = d.EsLaborable,
                    HoraEntrada = RrhhTime.Format(d.HoraEntrada),
                    HoraSalida = RrhhTime.Format(d.HoraSalida),
                    RecesoInicio = RrhhTime.Format(d.RecesoInicio),
                    RecesoFin = RrhhTime.Format(d.RecesoFin),
                    MinutosEsperados = d.MinutosEsperados
                }).ToList();
            var benIds = cargo.Beneficios.Select(b => b.IdBeneficio).ToList();
            var beneficios = await _db.RrhhBeneficio.AsNoTracking()
                .Where(b => benIds.Contains(b.IdBeneficio) && b.Activo)
                .OrderBy(b => b.Nombre)
                .ToListAsync();
            return new RrhhCargoPaqueteDto
            {
                IdCargo = cargo.IdCargo,
                Nombre = cargo.Nombre,
                SalarioBase = cargo.SalarioBase,
                Moneda = cargo.Moneda,
                FrecuenciaPago = cargo.FrecuenciaPago,
                TipoEmpleado = cargo.TipoEmpleado,
                IdJornada = cargo.IdJornada,
                NombreJornada = jornada?.Nombre,
                ResumenHorario = dias.Count == 0 ? "Sin horario" : string.Join(" · ",
                    dias.Where(d => d.EsLaborable).Select(d =>
                    {
                        string[] n = { "", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
                        return $"{n[d.DiaSemana]} {d.HoraEntrada}–{d.HoraSalida}";
                    })),
                Dias = dias,
                Beneficios = beneficios
            };
        }

        private async Task AplicarPaqueteCargoAsync(EmpleadoLaboral laboral, RrhhCargo cargo, int? idUsuario)
        {
            await UpsertSueldoBaseAsignacionAsync(laboral, cargo);
            var links = await _db.RrhhCargoBeneficio.AsNoTracking()
                .Where(x => x.IdCargo == cargo.IdCargo)
                .Select(x => x.IdBeneficio)
                .ToListAsync();
            var beneficios = await _db.RrhhBeneficio.AsNoTracking()
                .Where(b => links.Contains(b.IdBeneficio)
                            && b.Activo
                            && b.AfectaNomina
                            && b.TipoCalculo != "DESCUENTO_CONSUMO")
                .ToListAsync();

            var actuales = await _db.NominaConceptoAsignacion.AsTracking()
                .Where(a => a.IdEmpresa == laboral.IdEmpresa
                            && a.IdEmpleados == laboral.IdEmpleados
                            && a.Nota == "Desde cargo")
                .ToListAsync();

            var codes = beneficios.Select(b => b.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            codes.Add("SUELDO_BASE");
            foreach (var extra in actuales.Where(a => a.ConceptCode != "SUELDO_BASE" && !codes.Contains(a.ConceptCode)))
                extra.Activo = false;

            foreach (var ben in beneficios)
            {
                var monto = MontoPeriodoBeneficio(ben, cargo.SalarioBase, cargo.FrecuenciaPago);
                var row = actuales.FirstOrDefault(a =>
                    a.ConceptCode.Equals(ben.Codigo, StringComparison.OrdinalIgnoreCase) && a.Activo)
                    ?? await _db.NominaConceptoAsignacion.AsTracking().FirstOrDefaultAsync(a =>
                        a.IdEmpresa == laboral.IdEmpresa
                        && a.IdEmpleados == laboral.IdEmpleados
                        && a.ConceptCode == ben.Codigo);

                if (row is null)
                {
                    _db.NominaConceptoAsignacion.Add(new NominaConceptoAsignacion
                    {
                        IdEmpresa = laboral.IdEmpresa,
                        IdEmpleados = laboral.IdEmpleados,
                        ConceptCode = ben.Codigo,
                        MontoFijo = monto,
                        Periodicidad = cargo.FrecuenciaPago,
                        VigenteDesde = laboral.FechaIngreso ?? DateTime.UtcNow.Date,
                        Activo = true,
                        Nota = "Desde cargo"
                    });
                }
                else
                {
                    row.MontoFijo = monto;
                    row.Periodicidad = cargo.FrecuenciaPago;
                    row.Activo = true;
                    row.Nota = "Desde cargo";
                }
            }
        }

        private static decimal MontoPeriodoBeneficio(RrhhBeneficio ben, decimal salarioMensual, string frecuenciaCargo)
        {
            var mensual = ben.TipoCalculo == "PORCENTAJE_SALARIO"
                ? Math.Round(salarioMensual * (ben.Monto / 100m), 2, MidpointRounding.AwayFromZero)
                : ben.Periodicidad.ToUpperInvariant() switch
                {
                    "SEMANAL" => ben.Monto * 4m,
                    "QUINCENAL" => ben.Monto * 2m,
                    _ => ben.Monto
                };
            return ToPeriodAmount(mensual, frecuenciaCargo);
        }

        private async Task UpsertSueldoBaseAsignacionAsync(EmpleadoLaboral laboral, RrhhCargo cargo)
        {
            var periodAmount = ToPeriodAmount(cargo.SalarioBase, cargo.FrecuenciaPago);
            var row = await _db.NominaConceptoAsignacion.AsTracking().FirstOrDefaultAsync(a =>
                a.IdEmpresa == laboral.IdEmpresa
                && a.IdEmpleados == laboral.IdEmpleados
                && a.ConceptCode == "SUELDO_BASE");

            if (row is null)
            {
                _db.NominaConceptoAsignacion.Add(new NominaConceptoAsignacion
                {
                    IdEmpresa = laboral.IdEmpresa,
                    IdEmpleados = laboral.IdEmpleados,
                    ConceptCode = "SUELDO_BASE",
                    MontoFijo = periodAmount,
                    Periodicidad = cargo.FrecuenciaPago,
                    VigenteDesde = laboral.FechaIngreso ?? DateTime.UtcNow.Date,
                    Activo = true,
                    Nota = "Desde cargo"
                });
            }
            else
            {
                row.MontoFijo = periodAmount;
                row.Periodicidad = cargo.FrecuenciaPago;
                row.Activo = true;
                row.Nota = "Desde cargo";
            }
        }

        private static decimal ToPeriodAmount(decimal salarioBaseMensual, string frecuencia) =>
            frecuencia?.ToUpperInvariant() switch
            {
                "SEMANAL" => Math.Round(salarioBaseMensual / 4m, 2, MidpointRounding.AwayFromZero),
                "QUINCENAL" => Math.Round(salarioBaseMensual / 2m, 2, MidpointRounding.AwayFromZero),
                _ => salarioBaseMensual
            };

        private async Task SyncPayrollMirrorAsync(EmpleadoLaboral laboral)
        {
            var emp = await _payroll.Employees.FindAsync(laboral.IdEmpresa, laboral.IdEmpleados);
            if (emp is null)
            {
                _payroll.Employees.Add(new PayrollEmployeeEntity
                {
                    IdEmpresa = laboral.IdEmpresa,
                    IdEmpleados = laboral.IdEmpleados,
                    Activo = laboral.EstadoLaboral == "ACTIVO"
                });
            }
            else
            {
                emp.Activo = laboral.EstadoLaboral == "ACTIVO";
            }

            await UpsertContractAttr(laboral.IdEmpresa, laboral.IdEmpleados, "Cargo", laboral.Cargo ?? "");
            await UpsertContractAttr(laboral.IdEmpresa, laboral.IdEmpleados, "Departamento", laboral.Departamento ?? "");
            await UpsertContractAttr(laboral.IdEmpresa, laboral.IdEmpleados, "FrecuenciaPago", laboral.FrecuenciaPago);
            await UpsertContractAttr(laboral.IdEmpresa, laboral.IdEmpleados, "TipoEmpleado", laboral.TipoEmpleado);
            await UpsertContractAttr(laboral.IdEmpresa, laboral.IdEmpleados, "EstadoLaboral", laboral.EstadoLaboral);

            var asignaciones = await _db.NominaConceptoAsignacion
                .Where(a => a.IdEmpresa == laboral.IdEmpresa && a.IdEmpleados == laboral.IdEmpleados && a.Activo)
                .ToListAsync();

            foreach (var a in asignaciones)
                await SyncAssignmentToPayrollAsync(a, save: false);

            await _payroll.SaveChangesAsync();
        }

        private async Task UpsertContractAttr(int idEmpresa, int idEmpleados, string key, string value)
        {
            var row = await _payroll.ContractAttributes
                .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == idEmpleados && a.AttributeKey == key);
            if (row is null)
            {
                _payroll.ContractAttributes.Add(new PayrollContractAttributeEntity
                {
                    IdEmpresa = idEmpresa,
                    IdEmpleados = idEmpleados,
                    AttributeKey = key,
                    AttributeValue = value
                });
            }
            else
            {
                row.AttributeValue = value;
            }
        }

        private async Task SyncAssignmentToPayrollAsync(NominaConceptoAsignacion a, bool save = true)
        {
            var row = await _payroll.ConceptAssignments.FirstOrDefaultAsync(x =>
                x.IdEmpresa == a.IdEmpresa
                && x.IdEmpleados == a.IdEmpleados
                && x.ConceptCode == a.ConceptCode);

            DateOnly? from = a.VigenteDesde.HasValue ? DateOnly.FromDateTime(a.VigenteDesde.Value) : null;
            DateOnly? to = a.VigenteHasta.HasValue ? DateOnly.FromDateTime(a.VigenteHasta.Value) : null;

            if (row is null)
            {
                _payroll.ConceptAssignments.Add(new PayrollConceptAssignmentEntity
                {
                    IdEmpresa = a.IdEmpresa,
                    IdEmpleados = a.IdEmpleados,
                    ConceptCode = a.ConceptCode,
                    FixedAmount = a.MontoFijo,
                    Rate = a.Tasa,
                    EffectiveFrom = from,
                    EffectiveTo = to
                });
            }
            else
            {
                row.FixedAmount = a.MontoFijo;
                row.Rate = a.Tasa;
                row.EffectiveFrom = from;
                row.EffectiveTo = to;
            }

            if (save)
                await _payroll.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ConsumoColaboradorDto>> GetConsumoColaboradoresAsync(int idEmpresa)
        {
            var empleados = await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa && e.Estado)
                .OrderBy(e => e.Nombre)
                .Select(e => new { e.IdEmpleados, e.Nombre })
                .ToListAsync();
            if (empleados.Count == 0)
                return Array.Empty<ConsumoColaboradorDto>();

            var laborales = await _db.EmpleadoLaboral.AsNoTracking()
                .Where(l => l.IdEmpresa == idEmpresa && l.EstadoLaboral == "ACTIVO" && l.IdCargo != null)
                .Select(l => new { l.IdEmpleados, l.IdCargo })
                .ToListAsync();
            var cargoIds = laborales.Select(l => l.IdCargo!.Value).Distinct().ToList();
            Dictionary<int, (decimal Porcentaje, bool Descontar)> descPorCargo = new();
            if (cargoIds.Count > 0)
            {
                var rows = await (
                    from link in _db.RrhhCargoBeneficio.AsNoTracking()
                    join b in _db.RrhhBeneficio.AsNoTracking() on link.IdBeneficio equals b.IdBeneficio
                    where cargoIds.Contains(link.IdCargo)
                          && b.Activo
                          && b.TipoCalculo == "DESCUENTO_CONSUMO"
                    select new { link.IdCargo, b.Monto, b.DescontarConsumoNomina }
                ).ToListAsync();

                descPorCargo = rows
                    .GroupBy(x => x.IdCargo)
                    .ToDictionary(
                        g => g.Key,
                        g => (Porcentaje: g.Max(x => x.Monto), Descontar: g.Any(x => x.DescontarConsumoNomina)));
            }

            var cargoPorEmp = laborales.ToDictionary(x => x.IdEmpleados, x => x.IdCargo!.Value);

            return empleados.Select(e =>
            {
                decimal pct = 0;
                var descontar = false;
                if (cargoPorEmp.TryGetValue(e.IdEmpleados, out var idCargo)
                    && descPorCargo.TryGetValue(idCargo, out var d))
                {
                    pct = d.Porcentaje;
                    descontar = d.Descontar;
                }
                return new ConsumoColaboradorDto
                {
                    IdEmpleados = e.IdEmpleados,
                    Nombre = e.Nombre ?? $"Empleado {e.IdEmpleados}",
                    Porcentaje = pct,
                    DescontarNomina = descontar
                };
            }).ToList();
        }
    }
}
