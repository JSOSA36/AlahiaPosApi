using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadIntegracionLogService : IContabilidadIntegracionLogService
    {
        private readonly AlahiaPosContext _context;

        public ContabilidadIntegracionLogService(AlahiaPosContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(
            int idEmpresa,
            string estado,
            string? mensaje = null,
            int? idEventoOutbox = null,
            string? origenModulo = null,
            int? origenReferenciaId = null,
            string? tipoOperacion = null,
            int? idAsientoContable = null)
        {
            _context.ContabilidadIntegracionLog.Add(new ContabilidadIntegracionLog
            {
                IdEmpresa = idEmpresa,
                IdEventoOutbox = idEventoOutbox,
                OrigenModulo = origenModulo,
                OrigenReferenciaId = origenReferenciaId,
                TipoOperacion = tipoOperacion,
                Estado = estado,
                IdAsientoContable = idAsientoContable,
                Mensaje = mensaje,
                Fecha = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
