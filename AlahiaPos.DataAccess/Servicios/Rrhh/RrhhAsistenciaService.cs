using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhAsistenciaService : IRrhhAsistenciaService
    {
        private readonly AlahiaPosContext _db;
        private readonly IRrhhPonchadorService _ponchador;
        private readonly IRrhhAusenciaService _ausencias;

        public RrhhAsistenciaService(
            AlahiaPosContext db,
            IRrhhPonchadorService ponchador,
            IRrhhAusenciaService ausencias)
        {
            _db = db;
            _ponchador = ponchador;
            _ausencias = ausencias;
        }

        public async Task<IReadOnlyList<RrhhAsistenciaDiaDto>> CalcularAsync(RrhhCalcularAsistenciaRequest req)
        {
            if (req.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es obligatorio.");
            var desde = req.Desde.Date;
            var hasta = req.Hasta.Date;
            if (hasta < desde)
                throw new ArgumentException("El rango de fechas es inválido.");

            var empleadosQ = _db.EmpleadoLaboral.AsNoTracking()
                .Where(e => e.IdEmpresa == req.IdEmpresa && e.EstadoLaboral == "ACTIVO");
            if (req.IdEmpleados is > 0)
                empleadosQ = empleadosQ.Where(e => e.IdEmpleados == req.IdEmpleados.Value);
            var empleados = await empleadosQ.Select(e => e.IdEmpleados).Distinct().ToListAsync();
            if (empleados.Count == 0)
            {
                var personas = _db.EmpleadosP.AsNoTracking()
                    .Where(e => e.IdEmpresa == req.IdEmpresa && e.Estado);
                if (req.IdEmpleados is > 0)
                    personas = personas.Where(e => e.IdEmpleados == req.IdEmpleados.Value);
                empleados = await personas.Select(e => e.IdEmpleados).ToListAsync();
            }

            var nombres = await _db.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == req.IdEmpresa && empleados.Contains(e.IdEmpleados))
                .ToDictionaryAsync(e => e.IdEmpleados, e => e.Nombre ?? "");

            var resultado = new List<RrhhAsistenciaDiaDto>();
            foreach (var idEmp in empleados)
            {
                var dias = await CalcularEmpleadoAsync(req.IdEmpresa, idEmp, desde, hasta);
                foreach (var d in dias)
                {
                    resultado.Add(ToDto(d, nombres.GetValueOrDefault(idEmp)));
                }
            }

            return resultado.OrderBy(x => x.NombreEmpleado).ThenBy(x => x.Fecha).ToList();
        }

        public async Task<IReadOnlyList<RrhhAsistenciaDiaDto>> ListarAsync(
            int idEmpresa, DateTime desde, DateTime hasta, int? idEmpleados)
        {
            var q = from a in _db.RrhhAsistenciaDia.AsNoTracking()
                    join e in _db.EmpleadosP.AsNoTracking() on a.IdEmpleados equals e.IdEmpleados
                    where a.IdEmpresa == idEmpresa
                          && e.IdEmpresa == idEmpresa
                          && a.Fecha >= desde.Date
                          && a.Fecha <= hasta.Date
                    select new { a, e.Nombre };
            if (idEmpleados is > 0)
                q = q.Where(x => x.a.IdEmpleados == idEmpleados.Value);

            var rows = await q.OrderBy(x => x.Nombre).ThenBy(x => x.a.Fecha).ToListAsync();
            return rows.Select(x => ToDto(x.a, x.Nombre)).ToList();
        }

        private async Task<List<RrhhAsistenciaDia>> CalcularEmpleadoAsync(
            int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta)
        {
            var horarios = await _db.RrhhEmpleadoHorario.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa && h.IdEmpleados == idEmpleados && h.Activo)
                .ToListAsync();
            var laboral = await _db.EmpleadoLaboral.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.IdEmpleados == idEmpleados);
            int? jornadaCargo = null;
            if (laboral?.IdCargo is > 0)
            {
                jornadaCargo = await _db.RrhhCargo.AsNoTracking()
                    .Where(c => c.IdCargo == laboral.IdCargo && c.IdEmpresa == idEmpresa)
                    .Select(c => c.IdJornada)
                    .FirstOrDefaultAsync();
            }

            var jornadaIds = horarios.Select(h => h.IdJornada).ToList();
            if (jornadaCargo is > 0) jornadaIds.Add(jornadaCargo.Value);
            if (laboral?.IdJornada is > 0) jornadaIds.Add(laboral.IdJornada.Value);
            jornadaIds = jornadaIds.Distinct().ToList();
            var jornadas = await _db.RrhhJornada.AsNoTracking().Include(j => j.Dias)
                .Where(j => jornadaIds.Contains(j.IdJornada))
                .ToDictionaryAsync(j => j.IdJornada);
            var turnoIds = horarios.Where(h => h.IdTurno.HasValue).Select(h => h.IdTurno!.Value).Distinct().ToList();
            var turnos = await _db.RrhhTurno.AsNoTracking()
                .Where(t => turnoIds.Contains(t.IdTurno))
                .ToDictionaryAsync(t => t.IdTurno);

            var permisos = await _ausencias.GetAprobadasEnRangoAsync(idEmpresa, idEmpleados, desde, hasta);
            var tipos = await _db.RrhhTipoAusencia.AsNoTracking()
                .Where(t => t.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(t => t.IdTipoAusencia);

            var originales = await _ponchador.GetOriginalesAsync(idEmpresa, idEmpleados, desde, hasta);
            var corr = await _db.RrhhPonchadaCorreccion.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.IdEmpleados == idEmpleados && c.Estado == RrhhEstados.Aprobada)
                .ToListAsync();
            var efectivas = RrhhPonchadorService.ConstruirEfectivas(originales, corr);

            var existentes = await _db.RrhhAsistenciaDia.AsTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == idEmpleados
                            && a.Fecha >= desde && a.Fecha <= hasta)
                .ToListAsync();

            var calc = new List<RrhhAsistenciaDia>();
            for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            {
                var asign = horarios
                    .Where(h => h.VigenteDesde.Date <= fecha && (h.VigenteHasta == null || h.VigenteHasta.Value.Date >= fecha))
                    .OrderByDescending(h => h.VigenteDesde)
                    .FirstOrDefault();

                var idJornadaDia = asign?.IdJornada ?? jornadaCargo ?? laboral?.IdJornada ?? 0;
                jornadas.TryGetValue(idJornadaDia, out var jornada);
                var diaSemana = RrhhTime.DiaSemanaLunes(fecha);
                var diaJornada = jornada?.Dias?.FirstOrDefault(d => d.DiaSemana == diaSemana);
                turnos.TryGetValue(asign?.IdTurno ?? 0, out var turno);

                var permisoDia = permisos.FirstOrDefault(p => p.FechaInicio.Date <= fecha && p.FechaFin.Date >= fecha);
                tipos.TryGetValue(permisoDia?.IdTipoAusencia ?? 0, out var tipoAus);

                var row = existentes.FirstOrDefault(a => a.Fecha.Date == fecha) ?? new RrhhAsistenciaDia
                {
                    IdEmpresa = idEmpresa,
                    IdEmpleados = idEmpleados,
                    Fecha = fecha
                };
                if (row.IdAsistenciaDia == 0)
                    _db.RrhhAsistenciaDia.Add(row);

                row.IdJornada = asign?.IdJornada ?? jornadaCargo ?? laboral?.IdJornada;
                row.IdTurno = asign?.IdTurno;
                row.CalculadoEn = DateTime.UtcNow;
                row.IdSolicitudAusencia = permisoDia?.IdSolicitud;
                row.Observacion = null;

                var laborable = diaJornada?.EsLaborable ?? (diaSemana <= 5);
                TimeSpan? entradaEsp = turno?.HoraEntrada ?? diaJornada?.HoraEntrada;
                TimeSpan? salidaEsp = turno?.HoraSalida ?? diaJornada?.HoraSalida;
                TimeSpan? recesoIni = turno?.RecesoInicio ?? diaJornada?.RecesoInicio;
                TimeSpan? recesoFin = turno?.RecesoFin ?? diaJornada?.RecesoFin;
                var minutosEsp = diaJornada?.MinutosEsperados ?? 0;
                if (minutosEsp <= 0 && laborable && entradaEsp.HasValue && salidaEsp.HasValue)
                {
                    minutosEsp = RrhhTime.MinutosEntre(entradaEsp.Value, salidaEsp.Value);
                    if (recesoIni.HasValue && recesoFin.HasValue)
                        minutosEsp -= RrhhTime.MinutosEntre(recesoIni.Value, recesoFin.Value);
                }

                row.HoraEntradaEsperada = laborable ? entradaEsp : null;
                row.HoraSalidaEsperada = laborable ? salidaEsp : null;
                row.MinutosEsperados = laborable ? Math.Max(0, minutosEsp) : 0;

                if (!laborable)
                {
                    row.Estado = RrhhEstados.Descanso;
                    row.HoraEntradaReal = null;
                    row.HoraSalidaReal = null;
                    row.MinutosTrabajados = 0;
                    row.MinutosTardanza = 0;
                    row.MinutosSalidaAnticipada = 0;
                    row.MinutosExtra = 0;
                    row.EsJustificado = true;
                    calc.Add(row);
                    continue;
                }

                if (permisoDia != null && permisoDia.Unidad != "HORAS")
                {
                    row.Estado = tipoAus?.Categoria switch
                    {
                        "VACACIONES" => RrhhEstados.Vacaciones,
                        "LICENCIA" => RrhhEstados.Licencia,
                        _ => RrhhEstados.Permiso
                    };
                    row.EsJustificado = true;
                    row.HoraEntradaReal = null;
                    row.HoraSalidaReal = null;
                    row.MinutosTrabajados = 0;
                    row.MinutosTardanza = 0;
                    row.MinutosSalidaAnticipada = 0;
                    row.MinutosExtra = 0;
                    row.Observacion = tipoAus?.Nombre ?? "Permiso aprobado";
                    calc.Add(row);
                    continue;
                }

                var punchesDia = efectivas
                    .Where(p => p.FechaHora.Date == fecha)
                    .OrderBy(p => p.FechaHora)
                    .ToList();
                var entrada = punchesDia.FirstOrDefault(p => p.Tipo == RrhhEstados.Entrada || p.Tipo == RrhhEstados.RetornoReceso);
                var salida = punchesDia.LastOrDefault(p => p.Tipo == RrhhEstados.Salida || p.Tipo == RrhhEstados.SalidaReceso);

                row.HoraEntradaReal = entrada?.FechaHora;
                row.HoraSalidaReal = salida?.FechaHora;

                if (entrada == null && salida == null)
                {
                    row.Estado = RrhhEstados.Ausente;
                    row.EsJustificado = false;
                    row.MinutosTrabajados = 0;
                    row.MinutosTardanza = 0;
                    row.MinutosSalidaAnticipada = 0;
                    row.MinutosExtra = 0;
                    calc.Add(row);
                    continue;
                }

                if (entrada == null || salida == null)
                {
                    row.Estado = RrhhEstados.Incompleto;
                    row.EsJustificado = false;
                    row.MinutosTrabajados = 0;
                    row.MinutosTardanza = 0;
                    row.MinutosSalidaAnticipada = 0;
                    row.MinutosExtra = 0;
                    calc.Add(row);
                    continue;
                }

                var trabajados = (int)(salida.FechaHora - entrada.FechaHora).TotalMinutes;
                if (recesoIni.HasValue && recesoFin.HasValue)
                    trabajados -= RrhhTime.MinutosEntre(recesoIni.Value, recesoFin.Value);
                if (trabajados < 0) trabajados = 0;
                row.MinutosTrabajados = trabajados;

                var gracia = jornada?.MinutosTardanzaGracia ?? 10;
                var tardanza = 0;
                var anticipada = 0;
                if (entradaEsp.HasValue)
                {
                    var limite = fecha.Add(entradaEsp.Value).AddMinutes(gracia);
                    if (entrada.FechaHora > limite)
                        tardanza = (int)(entrada.FechaHora - fecha.Add(entradaEsp.Value)).TotalMinutes;
                }
                if (salidaEsp.HasValue && salida.FechaHora < fecha.Add(salidaEsp.Value))
                    anticipada = (int)(fecha.Add(salidaEsp.Value) - salida.FechaHora).TotalMinutes;

                if (permisoDia is { Unidad: "HORAS" } && permisoDia.HoraInicio.HasValue && permisoDia.HoraFin.HasValue)
                {
                    var pIni = fecha.Add(permisoDia.HoraInicio.Value);
                    var pFin = fecha.Add(permisoDia.HoraFin.Value);
                    if (entradaEsp.HasValue)
                    {
                        var entradaTeorica = fecha.Add(entradaEsp.Value);
                        if (entradaTeorica >= pIni && entradaTeorica <= pFin)
                            tardanza = 0;
                    }
                    if (salidaEsp.HasValue)
                    {
                        var salidaTeorica = fecha.Add(salidaEsp.Value);
                        if (salidaTeorica >= pIni && salidaTeorica <= pFin)
                            anticipada = 0;
                    }
                    row.Observacion = "Permiso por horas aprobado";
                    row.IdSolicitudAusencia = permisoDia.IdSolicitud;
                }

                row.MinutosTardanza = Math.Max(0, tardanza);
                row.MinutosSalidaAnticipada = Math.Max(0, anticipada);
                row.MinutosExtra = Math.Max(0, trabajados - row.MinutosEsperados);
                row.Estado = RrhhEstados.Presente;
                row.EsJustificado = true;
                calc.Add(row);
            }

            await _db.SaveChangesAsync();
            return calc;
        }

        private static RrhhAsistenciaDiaDto ToDto(RrhhAsistenciaDia a, string? nombre) => new()
        {
            IdAsistenciaDia = a.IdAsistenciaDia,
            IdEmpleados = a.IdEmpleados,
            NombreEmpleado = nombre,
            Fecha = a.Fecha,
            Estado = a.Estado,
            HoraEntradaEsperada = RrhhTime.Format(a.HoraEntradaEsperada),
            HoraSalidaEsperada = RrhhTime.Format(a.HoraSalidaEsperada),
            HoraEntradaReal = a.HoraEntradaReal,
            HoraSalidaReal = a.HoraSalidaReal,
            MinutosTrabajados = a.MinutosTrabajados,
            MinutosEsperados = a.MinutosEsperados,
            MinutosTardanza = a.MinutosTardanza,
            MinutosSalidaAnticipada = a.MinutosSalidaAnticipada,
            MinutosExtra = a.MinutosExtra,
            EsJustificado = a.EsJustificado,
            IdSolicitudAusencia = a.IdSolicitudAusencia,
            Observacion = a.Observacion
        };
    }
}
