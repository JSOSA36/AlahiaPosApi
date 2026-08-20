using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Validation;

namespace AlahiaPos.Payroll.Core.Engine;

/// <summary>
/// Implementación del motor (ALAHIA-PE-01). Sin RulePacks concretos, sin I/O.
/// </summary>
public sealed class PayrollEngine : IPayrollEngine
{
    private readonly PayrollRunner _runner;
    private readonly PayrollValidationService _validation;

    public PayrollEngine(
        PayrollRunner? runner = null,
        PayrollValidationService? validation = null)
    {
        _validation = validation ?? new PayrollValidationService();
        _runner = runner ?? new PayrollRunner(_validation);
    }

    public PayrollRun Calculate(PayrollCalculationRequest request, IPayrollRulePack rulePack) =>
        _runner.Run(request, rulePack);

    public PayrollRun Approve(PayrollRun draft, PayrollApprovalRequest request)
    {
        _validation.ValidateApproval(draft, request);

        var approved = draft with
        {
            Status = PayrollRunStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow
        };

        var hash = PayrollSnapshotHasher.Compute(approved);
        return approved with { SnapshotHash = hash };
    }
}
