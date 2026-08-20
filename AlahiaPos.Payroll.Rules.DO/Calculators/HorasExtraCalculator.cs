using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class HorasExtraCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.HorasExtra;
    public string CalculatorId => "do.horas_extra";
    public IReadOnlyList<ConceptDependency> DependsOn { get; } = Array.Empty<ConceptDependency>();

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var amount = DoMoney.Round(FactResolver.SumAmount(context, ConceptCode), context.Money);
        var line = new PayrollLine(
            ConceptCode,
            PayrollLineOrigin.Fact,
            amount,
            context.Money.CurrencyCode,
            Description: amount == 0m ? "Sin horas extra en el período" : "Horas extra del período");

        var trace = new CalculationTrace(
            ConceptCode,
            BaseAmount: amount,
            RuleDescription: "Sum of HORAS_EXTRA facts (Amount or Quantity×Rate)",
            ResultAmount: amount,
            CalculatorId: CalculatorId,
            RulePackId: context.RulePackId,
            RulePackVersion: context.RulePackVersion);

        return ConceptCalculationResult.FromLine(line, trace);
    }
}
