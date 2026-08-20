using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class SfsPatronalCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.SfsPatronal;
    public string CalculatorId => "do.sfs_patronal";

    public IReadOnlyList<ConceptDependency> DependsOn { get; } = new[]
    {
        new ConceptDependency(DoConceptCodes.SueldoBase, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.HorasExtra, ConceptDependencyKind.Soft)
    };

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var includeOt = PackParameterReader.GetBool(context, DoPackParameters.OvertimeIncludedInTssBase, true);
        var rate = PackParameterReader.RequireRate(context, DoPackParameters.SfsEmployerRate);
        var baseAmount = ContributionBase.IncomeBase(resolvedDependencies, context, includeOt);

        return ContributionBase.RateLine(
            context,
            ConceptCode,
            CalculatorId,
            baseAmount,
            rate,
            "SFS patronal = base cotizable × SFS_EMPLOYER_RATE");
    }
}
