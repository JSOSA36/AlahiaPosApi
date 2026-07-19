using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadIntegracionController : ControllerBase
    {
        private readonly AlahiaPosContext _context;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly IContabilidadGatekeeper _gatekeeper;

        public ContabilidadIntegracionController(
            AlahiaPosContext context,
            IDomainEventDispatcher dispatcher,
            IContabilidadGatekeeper gatekeeper)
        {
            _context = context;
            _dispatcher = dispatcher;
            _gatekeeper = gatekeeper;
        }

        [HttpGet("estado/{idEmpresa}")]
        public async Task<ActionResult<ContabilidadGatekeeperStatus>> GetEstado(int idEmpresa)
        {
            return await _gatekeeper.ObtenerEstadoAsync(idEmpresa);
        }

        [HttpGet("log/{idEmpresa}")]
        public async Task<IEnumerable<ContabilidadIntegracionLog>> GetLog(int idEmpresa, [FromQuery] int limite = 50)
        {
            return await _context.ContabilidadIntegracionLog
                .Where(l => l.IdEmpresa == idEmpresa)
                .OrderByDescending(l => l.Fecha)
                .Take(limite)
                .ToListAsync();
        }

        [HttpGet("outbox-pendientes/{idEmpresa}")]
        public async Task<IEnumerable<EventoOutbox>> GetOutboxPendientes(int idEmpresa)
        {
            return await _context.EventosOutbox
                .Where(e => e.IdEmpresa == idEmpresa &&
                    (e.Estado == EventoOutboxEstados.Pendiente || e.Estado == EventoOutboxEstados.Error))
                .OrderBy(e => e.FechaCreacion)
                .ToListAsync();
        }

        [HttpPost("reprocesar-outbox/{idEventoOutbox}")]
        public async Task<IActionResult> ReprocesarOutbox(int idEventoOutbox)
        {
            var evento = await _context.EventosOutbox
                .FirstOrDefaultAsync(e => e.IdEventoOutbox == idEventoOutbox);

            if (evento == null)
                return NotFound();

            evento.Estado = EventoOutboxEstados.Pendiente;
            _context.EventosOutbox.Update(evento);
            await _context.SaveChangesAsync();

            await _dispatcher.DispatchAsync(evento);
            return Ok();
        }
    }
}
