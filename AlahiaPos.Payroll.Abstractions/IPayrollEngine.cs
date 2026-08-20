namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Solicitud de cálculo de un run (sin persistencia).
/// </summary>
public sealed record PayrollCalculationRequest(
    int IdEmpresa,
    ExecutionKey ExecutionKey,
    PayrollPeriod Period,
    string RulePackId,
    IReadOnlyList<EvaluationContext> EmployeeContexts);

/// <summary>
/// Solicitud de aprobación idempotente.
/// </summary>
public sealed record PayrollApprovalRequest(
    int IdEmpresa,
    Guid PayrollRunId,
    ExecutionKey ExecutionKey);

/// <summary>
/// Motor de nómina (orquestación DAG). Implementación en Core.
/// </summary>
public interface IPayrollEngine
{
    /// <summary>
    /// Calcula un borrador. Debe respetar idempotencia de <see cref="ExecutionKey"/>
    /// a nivel de host/persistencia; el Core produce el run determinista.
    /// </summary>
    PayrollRun Calculate(PayrollCalculationRequest request, IPayrollRulePack rulePack);

    /// <summary>
    /// Marca el run como aprobado, exige snapshot/hash en la implementación de Core/host.
    /// Emisión del evento <c>NominaAprobada</c> es responsabilidad del host tras este paso.
    /// </summary>
    PayrollRun Approve(PayrollRun draft, PayrollApprovalRequest request);
}
