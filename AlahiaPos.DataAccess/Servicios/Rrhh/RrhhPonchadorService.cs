using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhPonchadorService : IRrhhPonchadorService
    {
        private readonly AlahiaPosContext _db;

        public RrhhPonchadorService(AlahiaPosContext db) => _db = db;

        public int ResolverIdEmpleados(int idEmpresa, int idUsuario, int idEmpleadosSolicitado)
        {
            if (idEmpleadosSolicitado > 0)
                return idEmpleadosSolicitado;
            var usuario = _db.Usuarios.AsNoTracking()
                .FirstOrDefault(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa);
            return usuario?.IdEmpleado ?? 0;
        }

        public async Task<RrhhPonchada> PoncharAsync(RrhhPoncharRequest req, int idUsuario, string? ip)
        {
            var idEmpleados = ResolverIdEmpleados(req.IdEmpresa, idUsuario, req.IdEmpleados);
            if (req.IdEmpresa <= 0 || idEmpleados <= 0)
                throw new InvalidOperationException("No se pudo resolver el empleado para ponchar.");

            var emp = await _db.EmpleadosP.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == req.IdEmpresa && e.IdEmpleados == idEmpleados);
            if (emp is null)
                throw new InvalidOperationException("Empleado no existe en EmpleadosP.");

            var cuando = req.FechaHora ?? DateTime.Now;
            if (!string.IsNullOrWhiteSpace(req.ClaveExterna))
            {
                var existente = await _db.RrhhPonchada.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdEmpresa == req.IdEmpresa && p.ClaveExterna == req.ClaveExterna);
                if (existente != null) return existente;
            }

            var tipo = string.IsNullOrWhiteSpace(req.Tipo)
                ? await InferirTipoAsync(req.IdEmpresa, idEmpleados, cuando)
                : req.Tipo.Trim().ToUpperInvariant();

            var row = new RrhhPonchada
            {
                IdEmpresa = req.IdEmpresa,
                IdEmpleados = idEmpleados,
                FechaHora = cuando,
                Tipo = tipo,
                Origen = string.IsNullOrWhiteSpace(req.Origen) ? "WEB" : req.Origen.Trim().ToUpperInvariant(),
                Dispositivo = req.Dispositivo,
                Ip = ip,
                IdUsuarioRegistra = idUsuario,
                Nota = req.Nota,
                ClaveExterna = string.IsNullOrWhiteSpace(req.ClaveExterna) ? null : req.ClaveExterna.Trim()
            };
            _db.RrhhPonchada.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhPonchada>> GetOriginalesAsync(
            int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta)
        {
            var fin = hasta.Date.AddDays(1);
            return await _db.RrhhPonchada.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.IdEmpleados == idEmpleados
                            && p.FechaHora >= desde.Date && p.FechaHora < fin)
                .OrderBy(p => p.FechaHora)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<RrhhPonchadaVistaDto>> GetEfectivasAsync(
            int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta)
        {
            var originales = await GetOriginalesAsync(idEmpresa, idEmpleados, desde, hasta);
            var ids = originales.Select(o => o.IdPonchada).ToList();
            var correcciones = await _db.RrhhPonchadaCorreccion.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.IdEmpleados == idEmpleados
                            && c.Estado == RrhhEstados.Aprobada
                            && (ids.Contains(c.IdPonchada ?? 0)
                                || (c.TipoCorreccion == RrhhEstados.CorrInsercion
                                    && c.FechaHoraNueva >= desde.Date
                                    && c.FechaHoraNueva < hasta.Date.AddDays(1))))
                .ToListAsync();

            return ConstruirEfectivas(originales, correcciones);
        }

        public static List<RrhhPonchadaVistaDto> ConstruirEfectivas(
            IReadOnlyList<RrhhPonchada> originales,
            IReadOnlyList<RrhhPonchadaCorreccion> aprobadas)
        {
            var porPonchada = aprobadas
                .Where(c => c.IdPonchada.HasValue)
                .GroupBy(c => c.IdPonchada!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.FechaDecision ?? x.FechaSolicitud).First());

            var list = new List<RrhhPonchadaVistaDto>();
            foreach (var o in originales)
            {
                porPonchada.TryGetValue(o.IdPonchada, out var corr);
                if (corr != null && corr.TipoCorreccion == RrhhEstados.CorrAnulacion)
                {
                    list.Add(new RrhhPonchadaVistaDto
                    {
                        IdPonchada = o.IdPonchada,
                        IdEmpleados = o.IdEmpleados,
                        FechaHora = o.FechaHora,
                        Tipo = o.Tipo,
                        Origen = o.Origen,
                        EsCorreccion = true,
                        Anulada = true,
                        Nota = o.Nota
                    });
                    continue;
                }

                list.Add(new RrhhPonchadaVistaDto
                {
                    IdPonchada = o.IdPonchada,
                    IdEmpleados = o.IdEmpleados,
                    FechaHora = corr?.FechaHoraNueva ?? o.FechaHora,
                    Tipo = corr?.TipoNuevo ?? o.Tipo,
                    Origen = o.Origen,
                    EsCorreccion = corr != null,
                    Anulada = false,
                    Nota = o.Nota
                });
            }

            foreach (var ins in aprobadas.Where(c =>
                         c.TipoCorreccion == RrhhEstados.CorrInsercion && c.FechaHoraNueva.HasValue))
            {
                list.Add(new RrhhPonchadaVistaDto
                {
                    IdPonchada = null,
                    IdEmpleados = ins.IdEmpleados,
                    FechaHora = ins.FechaHoraNueva!.Value,
                    Tipo = ins.TipoNuevo ?? RrhhEstados.Entrada,
                    Origen = "CORRECCION",
                    EsCorreccion = true,
                    Anulada = false,
                    Nota = ins.Motivo
                });
            }

            return list
                .Where(x => !x.Anulada)
                .OrderBy(x => x.FechaHora)
                .ToList();
        }

        public async Task<RrhhPonchadaCorreccion> SolicitarCorreccionAsync(RrhhCorreccionRequest req, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(req.Motivo) || req.Motivo.Trim().Length < 8)
                throw new ArgumentException("La corrección requiere un motivo (mínimo 8 caracteres).");

            var tipo = req.TipoCorreccion.Trim().ToUpperInvariant();
            DateTime? originalHora = null;
            string? originalTipo = null;

            if (req.IdPonchada.HasValue)
            {
                var orig = await _db.RrhhPonchada.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPonchada == req.IdPonchada && p.IdEmpresa == req.IdEmpresa);
                if (orig is null)
                    throw new InvalidOperationException("La ponchada original no existe. No se modifica el registro.");
                originalHora = orig.FechaHora;
                originalTipo = orig.Tipo;
            }
            else if (tipo != RrhhEstados.CorrInsercion)
            {
                throw new ArgumentException("Indique la ponchada original o use inserción de marcación omitida.");
            }

            var row = new RrhhPonchadaCorreccion
            {
                IdEmpresa = req.IdEmpresa,
                IdEmpleados = req.IdEmpleados,
                IdPonchada = req.IdPonchada,
                TipoCorreccion = tipo,
                Estado = RrhhEstados.Pendiente,
                FechaHoraOriginal = originalHora,
                TipoOriginal = originalTipo,
                FechaHoraNueva = req.FechaHoraNueva,
                TipoNuevo = req.TipoNuevo,
                Motivo = req.Motivo.Trim(),
                IdUsuarioSolicita = idUsuario
            };
            _db.RrhhPonchadaCorreccion.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhPonchadaCorreccion>> GetCorreccionesAsync(int idEmpresa, string? estado = null)
        {
            var q = _db.RrhhPonchadaCorreccion.AsNoTracking().Where(c => c.IdEmpresa == idEmpresa);
            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(c => c.Estado == estado);
            return await q.OrderByDescending(c => c.FechaSolicitud).Take(300).ToListAsync();
        }

        public async Task<RrhhPonchadaCorreccion> DecidirCorreccionAsync(
            int idCorreccion, bool aprobar, int idUsuario, string? comentario)
        {
            var row = await _db.RrhhPonchadaCorreccion.AsTracking().FirstOrDefaultAsync(c => c.IdCorreccion == idCorreccion)
                ?? throw new InvalidOperationException("Corrección no encontrada.");
            if (row.Estado != RrhhEstados.Pendiente)
                throw new InvalidOperationException("La corrección ya fue decidida.");

            row.Estado = aprobar ? RrhhEstados.Aprobada : RrhhEstados.Rechazada;
            row.IdUsuarioAprueba = idUsuario;
            row.FechaDecision = DateTime.UtcNow;
            row.MotivoDecision = comentario;
            await _db.SaveChangesAsync();
            return row;
        }

        private async Task<string> InferirTipoAsync(int idEmpresa, int idEmpleados, DateTime cuando)
        {
            var efectivas = await GetEfectivasAsync(idEmpresa, idEmpleados, cuando.Date, cuando.Date);
            var ultima = efectivas.LastOrDefault();
            if (ultima is null) return RrhhEstados.Entrada;
            return ultima.Tipo == RrhhEstados.Entrada || ultima.Tipo == RrhhEstados.RetornoReceso
                ? RrhhEstados.Salida
                : RrhhEstados.Entrada;
        }
    }
}
