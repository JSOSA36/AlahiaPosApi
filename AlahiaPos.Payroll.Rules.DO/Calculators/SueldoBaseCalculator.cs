using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class SueldoBaseCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.SueldoBase;
    public string CalculatorId => "do.sueldo_base";
    public IReadOnlyList<ConceptDependency> DependsOn { get; } = Array.Empty<ConceptDependency>();

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var assignment = AssignmentResolver.FindActive(context, ConceptCode);
        if (assignment is null)
            throw new DoRulePackException(
                $"No active assignment for '{ConceptCode}' covering period '{context.Period.PeriodKey}'.");

        if (assignment.FixedAmount is null)
            throw new DoRulePackException($"Assignment for '{ConceptCode}' requires FixedAmount.");

        var amount = DoMoney.Round(assignment.FixedAmount.Value, context.Money);
        var line = new PayrollLine(
            ConceptCode,
            PayrollLineOrigin.Assignment,
            amount,
            context.Money.CurrencyCode,
            Description: "Sueldo base vigente");

        var trace = new CalculationTrace(
            ConceptCode,
            BaseAmount: amount,
            RuleDescription: "Assignment.FixedAmount within period vigency",
            ResultAmount: amount,
            CalculatorId: CalculatorId,
            RulePackId: context.RulePackId,
            RulePackVersion: context.RulePackVersion);

        return ConceptCalculationResult.FromLine(line, trace);
    }
}
