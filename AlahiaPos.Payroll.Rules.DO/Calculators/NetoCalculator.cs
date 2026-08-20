using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

/// <summary>
/// Proyección neto = ingresos − deducciones empleado. Sin lógica fiscal.
/// No depende de aportes patronales.
/// </summary>
public sealed class NetoCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.Neto;
    public string CalculatorId => "do.neto";

    public IReadOnlyList<ConceptDependency> DependsOn { get; } = new[]
    {
        new ConceptDependency(DoConceptCodes.SueldoBase, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.HorasExtra, ConceptDependencyKind.Soft),
        new ConceptDependency(DoConceptCodes.AfpEmpleado, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.SfsEmpleado, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.IsrEmpleado, ConceptDependencyKind.Hard)
    };

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var incomes = DoConceptCodes.Incomes.Sum(resolvedDependencies.SumAmount);
        var deductions = DoConceptCodes.EmployeeDeductions.Sum(resolvedDependencies.SumAmount);
        var neto = DoMoney.Round(incomes - deductions, context.Money);

        var line = new PayrollLine(
            ConceptCode,
            PayrollLineOrigin.Rule,
            neto,
            context.Money.CurrencyCode,
            Description: "Neto = ingresos − deducciones empleado");

        var trace = new CalculationTrace(
            ConceptCode,
            BaseAmount: incomes,
            RuleDescription: "Projection: incomes - employee deductions (excludes employer contributions)",
            ResultAmount: neto,
            CalculatorId: CalculatorId,
            RulePackId: context.RulePackId,
            RulePackVersion: context.RulePackVersion,
            Details: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Incomes"] = incomes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Deductions"] = deductions.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

        return ConceptCalculationResult.FromLine(line, trace);
    }
}
