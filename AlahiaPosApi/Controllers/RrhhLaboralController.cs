using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/rrhh")]
    public class RrhhLaboralController : ControllerBase
    {
        private readonly IEmpleadoLaboralService _svc;
        private readonly IEvaluationContextBuilder _contextBuilder;
        private readonly IPayrollEngine _engine;

        public RrhhLaboralController(
            IEmpleadoLaboralService svc,
            IEvaluationContextBuilder contextBuilder,
            IPayrollEngine engine)
        {
            _svc = svc;
            _contextBuilder = contextBuilder;
            _engine = engine;
        }

        [HttpGet("laboral/{idEmpresa:int}/{idEmpleados:int}")]
        public async Task<IActionResult> GetLaboral(int idEmpresa, int idEmpleados) =>
            Ok(await _svc.GetLaboralVistaAsync(idEmpresa, idEmpleados));

        [HttpPut("laboral")]
        public async Task<IActionResult> UpsertLaboral([FromBody] EmpleadoLaboral body, [FromQuery] string? motivo = null)
        {
            try
            {
                return Ok(await _svc.UpsertLaboralAsync(body, motivo));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("historial-salarial/{idEmpresa:int}/{idEmpleados:int}")]
        public async Task<IActionResult> Historial(int idEmpresa, int idEmpleados) =>
            Ok(await _svc.GetHistorialSalarialAsync(idEmpresa, idEmpleados));

        [HttpGet("conceptos/{idEmpresa:int}")]
        public async Task<IActionResult> Conceptos(int idEmpresa) =>
            Ok(await _svc.GetConceptosAsync(idEmpresa));

        [HttpPut("conceptos")]
        public async Task<IActionResult> UpsertConcepto([FromBody] NominaConcepto body) =>
            Ok(await _svc.UpsertConceptoAsync(body));

        [HttpGet("asignaciones/{idEmpresa:int}/{idEmpleados:int}")]
        public async Task<IActionResult> Asignaciones(int idEmpresa, int idEmpleados) =>
            Ok(await _svc.GetAsignacionesAsync(idEmpresa, idEmpleados));

        [HttpPut("asignaciones")]
        public async Task<IActionResult> UpsertAsignacion([FromBody] NominaConceptoAsignacion body) =>
            Ok(await _svc.UpsertAsignacionAsync(body));

        [HttpGet("consumo-colaborador/{idEmpresa:int}")]
        public async Task<IActionResult> ConsumoColaborador(int idEmpresa) =>
            Ok(await _svc.GetConsumoColaboradoresAsync(idEmpresa));

        /// <summary>Smoke: contexto desde expediente + Draft DO-2026.01.</summary>
        [HttpPost("calcular-draft")]
        public async Task<IActionResult> CalcularDraft([FromBody] RrhhCalcularDraftRequest req)
        {
            var pack = DominicanRulePack.Create();
            var period = new PayrollPeriod(
                DateOnly.FromDateTime(req.Inicio),
                DateOnly.FromDateTime(req.Fin),
                req.PeriodKey);

            var ctx = await _contextBuilder.BuildAsync(new EvaluationContextBuildRequest(
                req.IdEmpresa,
                req.IdEmpleados,
                period,
                pack.Metadata.PackId,
                pack.Metadata.Version,
                pack.Metadata.DefaultMoneyPolicy,
                pack.Metadata.Parameters));

            var calcReq = new PayrollCalculationRequest(
                req.IdEmpresa,
                new ExecutionKey(req.IdEmpresa, req.PeriodKey, req.Intent ?? "REGULAR"),
                period,
                pack.Metadata.PackId,
                new[] { ctx });

            try
            {
                var draft = _engine.Calculate(calcReq, pack);
                return Ok(new
                {
                    draft.PayrollRunId,
                    draft.Status,
                    draft.RulePackId,
                    draft.RulePackVersion,
                    lines = draft.Lines,
                    traces = draft.Traces
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public sealed class RrhhCalcularDraftRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public string PeriodKey { get; set; } = "";
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string? Intent { get; set; }
    }
}
