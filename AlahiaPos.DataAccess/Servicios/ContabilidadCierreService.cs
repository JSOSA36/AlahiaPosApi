using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadCierreService : IContabilidadCierreService
    {
        private readonly AlahiaPosContext _context;

        private static readonly string[] Meses =
        {
            "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        public ContabilidadCierreService(AlahiaPosContext context)
        {
            _context = context;
        }

        public async Task<PeriodoContableDto?> GetPeriodoAsync(int idEmpresa, int anio, int mes)
        {
            ValidarPeriodo(anio, mes);

            var periodo = await _context.PeriodosContables
                .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.Anio == anio && p.Mes == mes);

            return periodo == null ? CrearPeriodoDtoVacio(idEmpresa, anio, mes) : MapToDto(periodo);
        }

        public async Task<IEnumerable<PeriodoContableDto>> GetPeriodosAsync(int idEmpresa, int anio)
        {
            var periodos = await _context.PeriodosContables
                .Where(p => p.IdEmpresa == idEmpresa && p.Anio == anio)
                .OrderBy(p => p.Mes)
                .ToListAsync();

            var resultado = new List<PeriodoContableDto>();
            for (int mes = 1; mes <= 12; mes++)
            {
                var existente = periodos.FirstOrDefault(p => p.Mes == mes);
                resultado.Add(existente != null
                    ? MapToDto(existente)
                    : CrearPeriodoDtoVacio(idEmpresa, anio, mes));
            }

            return resultado;
        }

        public async Task<PeriodoContableDto> CerrarPeriodoAsync(CerrarPeriodoRequest request)
        {
            ValidarPeriodo(request.Anio, request.Mes);

            if (request.IdEmpresa <= 0)
                throw new InvalidOperationException("Empresa no válida.");

            var periodo = await _context.PeriodosContables
                .FirstOrDefaultAsync(p =>
                    p.IdEmpresa == request.IdEmpresa &&
                    p.Anio == request.Anio &&
                    p.Mes == request.Mes);

            if (periodo?.Estado == ContabilidadConstantes.PeriodoCerrado)
                throw new InvalidOperationException("El período ya está cerrado.");

            var inicio = new DateTime(request.Anio, request.Mes, 1);
            var fin = inicio.AddMonths(1).AddTicks(-1);

            var asientosSinConfirmar = await _context.AsientosContables
                .CountAsync(a =>
                    a.IdEmpresa == request.IdEmpresa &&
                    a.Fecha >= inicio &&
                    a.Fecha <= fin &&
                    a.Estado == ContabilidadConstantes.EstadoAsientoBorrador);

            if (asientosSinConfirmar > 0)
                throw new InvalidOperationException(
                    $"Existen {asientosSinConfirmar} asiento(s) en borrador en el período.");

            if (periodo == null)
            {
                periodo = new PeriodoContable
                {
                    IdEmpresa = request.IdEmpresa,
                    Anio = request.Anio,
                    Mes = request.Mes,
                    FechaInseccion = DateTime.Now
                };
                _context.PeriodosContables.Add(periodo);
            }

            periodo.Estado = ContabilidadConstantes.PeriodoCerrado;
            periodo.FechaCierre = DateTime.Now;
            periodo.IdUsuarioCierre = request.IdUsuario;
            periodo.Observacion = request.Observacion;

            await _context.SaveChangesAsync();

            var dto = MapToDto(periodo);
            dto.Mensaje = "Período cerrado. No se permiten nuevos asientos ni ediciones con fecha en este período.";
            return dto;
        }

        /// <summary>
        /// True cuando existe un período marcado como Cerrado para la fecha del asiento.
        /// Períodos no materializados se consideran abiertos.
        /// </summary>
        public async Task<bool> EstaPeriodoBloqueadoAsync(int idEmpresa, DateTime fecha)
        {
            var periodo = await _context.PeriodosContables.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.IdEmpresa == idEmpresa
                    && p.Anio == fecha.Year
                    && p.Mes == fecha.Month);

            return periodo != null
                && string.Equals(periodo.Estado, ContabilidadConstantes.PeriodoCerrado, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Primera fecha válida del período abierto actual (hoy si el mes está abierto;
        /// si no, el día 1 del próximo mes abierto conocido; null si no hay abierto).
        /// </summary>
        public async Task<DateTime?> ResolverFechaContableAbiertaAsync(int idEmpresa, DateTime preferida)
        {
            if (!await EstaPeriodoBloqueadoAsync(idEmpresa, preferida))
                return preferida.Date;

            // Buscar próximos 36 meses abiertos (materializados o inexistentes = abiertos).
            var cursor = new DateTime(preferida.Year, preferida.Month, 1).AddMonths(1);
            for (var i = 0; i < 36; i++)
            {
                if (!await EstaPeriodoBloqueadoAsync(idEmpresa, cursor))
                    return cursor;

                cursor = cursor.AddMonths(1);
            }

            return null;
        }

        private static void ValidarPeriodo(int anio, int mes)
        {
            if (anio < 2000 || anio > 2100)
                throw new InvalidOperationException("Año no válido.");

            if (mes < 1 || mes > 12)
                throw new InvalidOperationException("Mes no válido.");
        }

        private static PeriodoContableDto CrearPeriodoDtoVacio(int idEmpresa, int anio, int mes)
        {
            return new PeriodoContableDto
            {
                IdEmpresa = idEmpresa,
                Anio = anio,
                Mes = mes,
                Estado = ContabilidadConstantes.PeriodoAbierto,
                NombreMes = Meses[mes],
                BloqueoActivo = false,
                Mensaje = "Período abierto."
            };
        }

        private static PeriodoContableDto MapToDto(PeriodoContable periodo)
        {
            var cerrado = string.Equals(
                periodo.Estado, ContabilidadConstantes.PeriodoCerrado, StringComparison.OrdinalIgnoreCase);

            return new PeriodoContableDto
            {
                IdPeriodoContable = periodo.IdPeriodoContable,
                IdEmpresa = periodo.IdEmpresa,
                Anio = periodo.Anio,
                Mes = periodo.Mes,
                Estado = periodo.Estado,
                FechaCierre = periodo.FechaCierre,
                Observacion = periodo.Observacion,
                NombreMes = Meses[periodo.Mes],
                BloqueoActivo = cerrado,
                Mensaje = cerrado
                    ? "Período cerrado. No se permiten asientos con fecha en este período."
                    : "Período abierto."
            };
        }
    }
}
