using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    public sealed class RrhhAusenciaService : IRrhhAusenciaService
    {
        private readonly AlahiaPosContext _db;

        public RrhhAusenciaService(AlahiaPosContext db) => _db = db;

        public async Task<RrhhSolicitudAusencia> SolicitarAsync(RrhhSolicitudAusencia row, int idUsuario)
        {
            if (row.IdEmpresa <= 0 || row.IdEmpleados <= 0 || row.IdTipoAusencia <= 0)
                throw new ArgumentException("Empresa, empleado y tipo son obligatorios.");
            if (row.FechaFin.Date < row.FechaInicio.Date)
                throw new ArgumentException("La fecha fin no puede ser anterior al inicio.");

            var tipo = await _db.RrhhTipoAusencia.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdTipoAusencia == row.IdTipoAusencia && t.IdEmpresa == row.IdEmpresa)
                ?? throw new InvalidOperationException("Tipo de ausencia no encontrado.");

            row.Unidad = string.IsNullOrWhiteSpace(row.Unidad) ? tipo.UnidadDefault : row.Unidad.Trim().ToUpperInvariant();
            if (row.Cantidad <= 0)
            {
                row.Cantidad = row.Unidad == "HORAS"
                    ? CalcularHoras(row)
                    : (decimal)(row.FechaFin.Date - row.FechaInicio.Date).TotalDays + 1;
            }
            row.ConGoceSueldo = tipo.ConGoceSueldo;
            row.Estado = tipo.RequiereAprobacion ? RrhhEstados.Pendiente : RrhhEstados.Aprobada;
            row.IdUsuarioSolicita = idUsuario;
            row.FechaInicio = row.FechaInicio.Date;
            row.FechaFin = row.FechaFin.Date;
            _db.RrhhSolicitudAusencia.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhSolicitudAusencia>> ListarAsync(
            int idEmpresa, int? idEmpleados, DateTime? desde, DateTime? hasta, string? estado)
        {
            var q = _db.RrhhSolicitudAusencia.AsNoTracking().Where(s => s.IdEmpresa == idEmpresa);
            if (idEmpleados is > 0) q = q.Where(s => s.IdEmpleados == idEmpleados);
            if (desde.HasValue) q = q.Where(s => s.FechaFin >= desde.Value.Date);
            if (hasta.HasValue) q = q.Where(s => s.FechaInicio <= hasta.Value.Date);
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(s => s.Estado == estado);
            return await q.OrderByDescending(s => s.FechaSolicitud).Take(400).ToListAsync();
        }

        public async Task<RrhhSolicitudAusencia> DecidirAsync(int idSolicitud, bool aprobar, int idUsuario, string? comentario)
        {
            var row = await _db.RrhhSolicitudAusencia.AsTracking().FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud)
                ?? throw new InvalidOperationException("Solicitud no encontrada.");
            if (row.Estado != RrhhEstados.Pendiente)
                throw new InvalidOperationException("La solicitud ya fue decidida.");
            row.Estado = aprobar ? RrhhEstados.Aprobada : RrhhEstados.Rechazada;
            row.IdUsuarioAprueba = idUsuario;
            row.FechaDecision = DateTime.UtcNow;
            row.ComentarioDecision = comentario;
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhSolicitudAusencia>> GetAprobadasEnRangoAsync(
            int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta)
        {
            var d = desde.Date;
            var h = hasta.Date;
            return await _db.RrhhSolicitudAusencia.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa
                            && s.IdEmpleados == idEmpleados
                            && s.Estado == RrhhEstados.Aprobada
                            && s.FechaInicio <= h
                            && s.FechaFin >= d)
                .ToListAsync();
        }

        public async Task<RrhhPrestamo> CrearPrestamoAsync(RrhhPrestamo row, int idUsuario)
        {
            if (row.Monto <= 0 || row.Cuotas <= 0)
                throw new ArgumentException("Monto y cuotas deben ser mayores a cero.");
            row.Saldo = row.Monto;
            row.MontoCuota = Math.Round(row.Monto / row.Cuotas, 2, MidpointRounding.AwayFromZero);
            row.IdUsuarioCrea = idUsuario;
            row.Estado = "ACTIVO";
            row.FechaInicio = row.FechaInicio.Date;
            for (var i = 1; i <= row.Cuotas; i++)
            {
                row.CuotasDetalle.Add(new RrhhPrestamoCuota
                {
                    Numero = i,
                    FechaProgramada = row.FechaInicio.AddMonths(i - 1),
                    Monto = i == row.Cuotas
                        ? row.Monto - row.MontoCuota * (row.Cuotas - 1)
                        : row.MontoCuota,
                    Estado = "PENDIENTE"
                });
            }
            _db.RrhhPrestamo.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhPrestamo>> GetPrestamosAsync(int idEmpresa, int? idEmpleados)
        {
            var q = _db.RrhhPrestamo.AsNoTracking().Include(p => p.CuotasDetalle)
                .Where(p => p.IdEmpresa == idEmpresa);
            if (idEmpleados is > 0) q = q.Where(p => p.IdEmpleados == idEmpleados);
            return await q.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        }

        public async Task<RrhhAnticipo> CrearAnticipoAsync(RrhhAnticipo row, int idUsuario)
        {
            if (row.Monto <= 0)
                throw new ArgumentException("El monto del anticipo debe ser mayor a cero.");
            row.IdUsuarioCrea = idUsuario;
            row.Estado = "PENDIENTE";
            row.Fecha = row.Fecha.Date;
            _db.RrhhAnticipo.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        public async Task<IReadOnlyList<RrhhAnticipo>> GetAnticiposAsync(int idEmpresa, int? idEmpleados)
        {
            var q = _db.RrhhAnticipo.AsNoTracking().Where(a => a.IdEmpresa == idEmpresa);
            if (idEmpleados is > 0) q = q.Where(a => a.IdEmpleados == idEmpleados);
            return await q.OrderByDescending(a => a.Fecha).ToListAsync();
        }

        private static decimal CalcularHoras(RrhhSolicitudAusencia row)
        {
            if (!row.HoraInicio.HasValue || !row.HoraFin.HasValue) return 0;
            return Math.Round((decimal)(row.HoraFin.Value - row.HoraInicio.Value).TotalHours, 2);
        }
    }
}
