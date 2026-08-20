using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO;
using AlahiaPos.Payroll.Rules.DO.Calculators;
using AlahiaPos.Payroll.Rules.DO.Tables;
using Xunit;

namespace AlahiaPos.Payroll.Rules.DO.Tests;

public sealed class IsrBracketTableTests
{
    [Fact]
    public void Annual_tax_zero_below_first_bracket()
    {
        var brackets = IsrBracketTable.Resolve(DoPackParameters.DefaultIsrTableVersion);
        Assert.Equal(0m, IsrBracketTable.ComputeAnnualTax(100_000m, brackets));
    }

    [Fact]
    public void Annual_tax_applies_second_bracket_rate()
    {
        var brackets = IsrBracketTable.Resolve(DoPackParameters.DefaultIsrTableVersion);
        // 500_000: excess over 416_220 = 83_780 × 15% = 12_567
        var tax = IsrBracketTable.ComputeAnnualTax(500_000m, brackets);
        Assert.Equal(12_567m, tax);
    }
}

public sealed class AssignmentResolverTests
{
    [Fact]
    public void SueldoBase_calculator_uses_fixed_amount()
    {
        var calc = new SueldoBaseCalculator();
        var ctx = DoTestFixtures.Context(18_500m);
        var result = calc.Execute(ctx, EmptyDeps.Instance);

        Assert.Equal(18_500m, Assert.Single(result.Lines).Amount);
        Assert.Equal(PayrollLineOrigin.Assignment, result.Lines[0].Origin);
    }
}

internal sealed class EmptyDeps : IResolvedLinesView
{
    public static EmptyDeps Instance { get; } = new();
    public IReadOnlyList<PayrollLine> All => Array.Empty<PayrollLine>();
    public IReadOnlyList<PayrollLine> ForConcept(string conceptCode) => Array.Empty<PayrollLine>();
    public decimal SumAmount(string conceptCode) => 0m;
}
