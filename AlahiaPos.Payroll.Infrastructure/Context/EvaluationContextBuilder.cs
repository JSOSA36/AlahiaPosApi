using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Infrastructure.Context;

/// <summary>
/// Arma EvaluationContext desde providers. No calcula conceptos ni conoce packs legales.
/// </summary>
public sealed class EvaluationContextBuilder : IEvaluationContextBuilder
{
    private readonly IConceptAssignmentProvider _assignments;
    private readonly IEmployeeContractAttributeProvider _contracts;
    private readonly IAttendanceFactProvider _attendance;
    private readonly IFactProvider _otherFacts;

    public EvaluationContextBuilder(
        IConceptAssignmentProvider assignments,
        IEmployeeContractAttributeProvider contracts,
        IAttendanceFactProvider attendance,
        IFactProvider otherFacts)
    {
        _assignments = assignments;
        _contracts = contracts;
        _attendance = attendance;
        _otherFacts = otherFacts;
    }

    public async Task<EvaluationContext> BuildAsync(
        EvaluationContextBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdEmpresa <= 0)
            throw new ArgumentException("IdEmpresa is required.", nameof(request));
        if (request.IdEmpleados <= 0)
            throw new ArgumentException("IdEmpleados is required.", nameof(request));

        var assignments = await _assignments.GetAssignmentsAsync(
            request.IdEmpresa, request.IdEmpleados, request.Period, cancellationToken);

        var contract = await _contracts.GetContractAttributesAsync(
            request.IdEmpresa, request.IdEmpleados, request.Period, cancellationToken);

        var attendance = await _attendance.GetFactsAsync(
            request.IdEmpresa, request.IdEmpleados, request.Period, cancellationToken);

        var other = await _otherFacts.GetFactsAsync(
            request.IdEmpresa, request.IdEmpleados, request.Period, cancellationToken);

        var facts = attendance.Concat(other).ToList();

        return new EvaluationContext(
            IdEmpresa: request.IdEmpresa,
            IdEmpleados: request.IdEmpleados,
            Period: request.Period,
            RulePackId: request.RulePackId,
            RulePackVersion: request.RulePackVersion,
            Money: request.Money,
            PackParameters: request.PackParameters,
            Assignments: assignments,
            Facts: facts,
            ContractAttributes: contract.Count == 0 ? null : contract);
    }
}
