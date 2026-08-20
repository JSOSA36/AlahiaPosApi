using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Dag;

namespace AlahiaPos.Payroll.Core.Validation;

/// <summary>Validaciones previas al pipeline (multiempresa, money, pack).</summary>
public sealed class PayrollValidationService
{
    public void ValidateRequest(PayrollCalculationRequest request, IPayrollRulePack rulePack)
    {
        if (request is null) throw new PayrollValidationException("Request is required.");
        if (rulePack is null) throw new PayrollValidationException("RulePack is required.");
        if (request.IdEmpresa <= 0)
            throw new PayrollValidationException("IdEmpresa is required.");
        if (request.ExecutionKey is null)
            throw new PayrollValidationException("ExecutionKey is required.");
        if (request.ExecutionKey.IdEmpresa != request.IdEmpresa)
            throw new PayrollValidationException("ExecutionKey.IdEmpresa must match request.IdEmpresa.");
        if (string.IsNullOrWhiteSpace(request.ExecutionKey.PeriodKey))
            throw new PayrollValidationException("ExecutionKey.PeriodKey is required.");
        if (request.Period is null)
            throw new PayrollValidationException("Period is required.");
        if (rulePack.Metadata is null)
            throw new PayrollValidationException("RulePack.Metadata is required.");
        if (string.IsNullOrWhiteSpace(rulePack.Metadata.PackId))
            throw new PayrollValidationException("RulePack PackId is required.");
        if (rulePack.Metadata.DefaultMoneyPolicy is null)
            throw new PayrollValidationException("MoneyPolicy is required.");
        if (string.IsNullOrWhiteSpace(rulePack.Metadata.DefaultMoneyPolicy.CurrencyCode))
            throw new PayrollValidationException("CurrencyCode is required.");
        if (rulePack.Metadata.DefaultMoneyPolicy.DecimalPlaces < 0)
            throw new PayrollValidationException("DecimalPlaces must be >= 0.");
        if (request.EmployeeContexts is null || request.EmployeeContexts.Count == 0)
            throw new PayrollValidationException("At least one EvaluationContext is required.");

        foreach (var ctx in request.EmployeeContexts)
        {
            if (ctx.IdEmpresa != request.IdEmpresa)
                throw new PayrollValidationException(
                    $"EvaluationContext IdEmpresa {ctx.IdEmpresa} does not match run IdEmpresa {request.IdEmpresa}.");
            if (ctx.Money is null || string.IsNullOrWhiteSpace(ctx.Money.CurrencyCode))
                throw new PayrollValidationException("EvaluationContext.Money.CurrencyCode is required.");
            if (ctx.IdEmpleados <= 0)
                throw new PayrollValidationException("EvaluationContext.IdEmpleados is required.");
        }
    }

    public void ValidateApproval(PayrollRun draft, PayrollApprovalRequest request)
    {
        if (draft is null) throw new PayrollValidationException("Draft run is required.");
        if (request is null) throw new PayrollValidationException("Approval request is required.");
        if (draft.Status != PayrollRunStatus.Draft)
            throw new PayrollValidationException("Only Draft runs can be approved.");
        if (draft.IdEmpresa != request.IdEmpresa)
            throw new PayrollValidationException("IdEmpresa mismatch on approval.");
        if (draft.PayrollRunId != request.PayrollRunId)
            throw new PayrollValidationException("PayrollRunId mismatch on approval.");
        if (draft.ExecutionKey != request.ExecutionKey)
            throw new PayrollValidationException("ExecutionKey mismatch on approval (idempotency).");
    }

    /// <summary>
    /// Hard deps cuyo prerequisite no tiene calculator registrado → error de grafo.
    /// Soft deps desconocidos se toleran (se tratarán como vacíos en runtime).
    /// </summary>
    public void ValidateHardDependenciesRegistered(
        ConceptDependencyGraph graph,
        IConceptCalculatorFactory factory)
    {
        var registered = new HashSet<string>(
            factory.AllMetadata.Select(m => m.ConceptCode),
            StringComparer.Ordinal);

        foreach (var meta in factory.AllMetadata)
        {
            foreach (var dep in meta.DependsOn ?? Array.Empty<ConceptDependency>())
            {
                if (dep.Kind != ConceptDependencyKind.Hard) continue;
                if (!registered.Contains(dep.ConceptCode))
                    throw new PayrollDependencyException(meta.ConceptCode, dep.ConceptCode);
            }
        }
    }
}
