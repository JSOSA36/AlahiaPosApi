using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Core.Tests;

internal static class TestFixtures
{
    public static readonly MoneyPolicy Money = new(
        CurrencyCode: "DOP",
        DecimalPlaces: 2,
        RoundingMode: MoneyRoundingMode.AwayFromZero,
        RoundPerLine: true,
        RoundAggregates: true);

    public static PayrollPeriod Period { get; } = new(
        StartInclusive: new DateOnly(2026, 1, 1),
        EndInclusive: new DateOnly(2026, 1, 15),
        PeriodKey: "2026-01-A");

    public static ExecutionKey Key(int idEmpresa = 1) =>
        new(idEmpresa, Period.PeriodKey, "REGULAR");

    public static RulePackMetadata PackMeta(string packId = "test.pack") =>
        new(
            PackId: packId,
            JurisdictionCode: "XX",
            Version: "1.0.0",
            DefaultMoneyPolicy: Money,
            Parameters: new Dictionary<string, string>());

    public static EvaluationContext Context(
        int idEmpresa = 1,
        int idEmpleados = 10,
        IReadOnlyDictionary<string, string>? packParameters = null) =>
        new(
            IdEmpresa: idEmpresa,
            IdEmpleados: idEmpleados,
            Period: Period,
            RulePackId: "test.pack",
            RulePackVersion: "1.0.0",
            Money: Money,
            PackParameters: packParameters ?? new Dictionary<string, string>(),
            Assignments: Array.Empty<ConceptAssignment>(),
            Facts: Array.Empty<PayrollFact>());

    public static PayrollCalculationRequest Request(
        IPayrollRulePack pack,
        params EvaluationContext[] contexts) =>
        new(
            IdEmpresa: 1,
            ExecutionKey: Key(),
            Period: Period,
            RulePackId: pack.Metadata.PackId,
            EmployeeContexts: contexts.Length > 0 ? contexts : new[] { Context() });

    public static FakeRulePack Pack(params IConceptCalculator[] calculators) =>
        new(PackMeta(), calculators);
}

internal sealed class FakeRulePack : IPayrollRulePack
{
    public FakeRulePack(RulePackMetadata metadata, IReadOnlyList<IConceptCalculator> calculators)
    {
        Metadata = metadata;
        Calculators = calculators;
        Concepts = Array.Empty<ConceptMetadata>();
    }

    public RulePackMetadata Metadata { get; }
    public IReadOnlyList<IConceptCalculator> Calculators { get; }
    public IReadOnlyList<ConceptMetadata> Concepts { get; }
}

/// <summary>Calculator genérico sin semántica legal — solo para pruebas del motor.</summary>
internal sealed class FakeCalculator : IConceptCalculator
{
    private readonly Func<EvaluationContext, IResolvedLinesView, ConceptCalculationResult> _execute;

    public FakeCalculator(
        string conceptCode,
        IReadOnlyList<ConceptDependency>? dependsOn = null,
        Func<EvaluationContext, IResolvedLinesView, ConceptCalculationResult>? execute = null)
    {
        ConceptCode = conceptCode;
        CalculatorId = "fake:" + conceptCode;
        DependsOn = dependsOn ?? Array.Empty<ConceptDependency>();
        _execute = execute ?? ((_, _) => ConceptCalculationResult.FromLine(
            new PayrollLine(conceptCode, PayrollLineOrigin.Rule, 1m, "DOP")));
    }

    public string ConceptCode { get; }
    public string CalculatorId { get; }
    public IReadOnlyList<ConceptDependency> DependsOn { get; }

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies) =>
        _execute(context, resolvedDependencies);
}
