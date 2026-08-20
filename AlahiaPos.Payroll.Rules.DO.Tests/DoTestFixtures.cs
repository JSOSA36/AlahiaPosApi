using System.Globalization;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO;

namespace AlahiaPos.Payroll.Rules.DO.Tests;

internal static class DoTestFixtures
{
    public static readonly PayrollPeriod Period = new(
        StartInclusive: new DateOnly(2026, 1, 1),
        EndInclusive: new DateOnly(2026, 1, 15),
        PeriodKey: "2026-01-A");

    public static DominicanRulePack Pack { get; } = DominicanRulePack.Create();

    public static ExecutionKey Key(int idEmpresa = 1) =>
        new(idEmpresa, Period.PeriodKey, "REGULAR");

    public static ConceptAssignment SueldoAssignment(
        decimal amount,
        DateOnly? from = null,
        DateOnly? to = null) =>
        new(
            ConceptCode: DoConceptCodes.SueldoBase,
            FixedAmount: amount,
            Rate: null,
            FormulaOrRuleId: null,
            EffectiveFrom: from ?? new DateOnly(2025, 1, 1),
            EffectiveTo: to);

    public static PayrollFact HorasExtraFact(decimal amount) =>
        new(
            FactType: DoConceptCodes.HorasExtra,
            ConceptCode: DoConceptCodes.HorasExtra,
            Quantity: 0m,
            Amount: amount);

    public static EvaluationContext Context(
        decimal sueldo,
        decimal? horasExtra = null,
        DateOnly? assignmentFrom = null,
        DateOnly? assignmentTo = null,
        IReadOnlyDictionary<string, string>? parameterOverrides = null)
    {
        var assignments = new List<ConceptAssignment>
        {
            SueldoAssignment(sueldo, assignmentFrom, assignmentTo)
        };

        var facts = new List<PayrollFact>();
        if (horasExtra is not null)
            facts.Add(HorasExtraFact(horasExtra.Value));

        var parameters = new Dictionary<string, string>(
            DoPackParameters.DefaultParameters,
            StringComparer.Ordinal);

        if (parameterOverrides is not null)
        {
            foreach (var kv in parameterOverrides)
                parameters[kv.Key] = kv.Value;
        }

        return new EvaluationContext(
            IdEmpresa: 1,
            IdEmpleados: 42,
            Period: Period,
            RulePackId: Pack.Metadata.PackId,
            RulePackVersion: Pack.Metadata.Version,
            Money: Pack.Metadata.DefaultMoneyPolicy,
            PackParameters: parameters,
            Assignments: assignments,
            Facts: facts);
    }

    public static PayrollCalculationRequest Request(EvaluationContext ctx) =>
        new(
            IdEmpresa: 1,
            ExecutionKey: Key(),
            Period: Period,
            RulePackId: Pack.Metadata.PackId,
            EmployeeContexts: new[] { ctx });

    public static decimal Rate(string key) =>
        decimal.Parse(DoPackParameters.DefaultParameters[key], CultureInfo.InvariantCulture);

    public static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
