using System.Collections.ObjectModel;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Dag;
using AlahiaPos.Payroll.Core.Validation;

namespace AlahiaPos.Payroll.Core.Engine;

/// <summary>
/// Prepara DAG + factory y ejecuta el pipeline por cada empleado del request.
/// </summary>
public sealed class PayrollRunner
{
    private readonly PayrollValidationService _validation;

    public PayrollRunner(PayrollValidationService? validation = null)
    {
        _validation = validation ?? new PayrollValidationService();
    }

    public PayrollRun Run(PayrollCalculationRequest request, IPayrollRulePack rulePack)
    {
        _validation.ValidateRequest(request, rulePack);

        var factory = ConceptCalculatorFactory.FromRulePack(rulePack);
        var graph = DagBuilder.Build(factory.AllMetadata);
        CycleDetector.EnsureAcyclic(graph);
        _validation.ValidateHardDependenciesRegistered(graph, factory);

        var order = TopologicalSorter.Sort(graph);
        var pipeline = new CalculationPipeline(factory, graph, order);

        var allLines = new List<PayrollLine>();
        var allTraces = new List<CalculationTrace>();

        foreach (var rawCtx in request.EmployeeContexts)
        {
            var ctx = ContextFreezer.Freeze(rawCtx);
            var (lines, traces) = pipeline.Execute(ctx);
            allLines.AddRange(lines);
            allTraces.AddRange(traces);
        }

        return new PayrollRun(
            PayrollRunId: Guid.NewGuid(),
            IdEmpresa: request.IdEmpresa,
            ExecutionKey: request.ExecutionKey,
            Period: request.Period,
            RulePackId: rulePack.Metadata.PackId,
            RulePackVersion: rulePack.Metadata.Version,
            Status: PayrollRunStatus.Draft,
            Money: rulePack.Metadata.DefaultMoneyPolicy,
            Lines: allLines,
            Traces: allTraces,
            CreatedAt: DateTimeOffset.UtcNow);
    }
}

/// <summary>Copia defensiva: colecciones de solo lectura para que calculators no muten el contexto.</summary>
internal static class ContextFreezer
{
    public static EvaluationContext Freeze(EvaluationContext ctx)
    {
        var pack = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(ctx.PackParameters, StringComparer.Ordinal));

        IReadOnlyDictionary<string, string>? contract = null;
        if (ctx.ContractAttributes is not null)
        {
            contract = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(ctx.ContractAttributes, StringComparer.Ordinal));
        }

        return ctx with
        {
            PackParameters = pack,
            Assignments = ctx.Assignments.ToArray(),
            Facts = ctx.Facts.ToArray(),
            ContractAttributes = contract
        };
    }
}
