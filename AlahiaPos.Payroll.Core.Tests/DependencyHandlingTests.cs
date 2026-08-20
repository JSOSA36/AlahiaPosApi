using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core;
using AlahiaPos.Payroll.Core.Engine;
using Xunit;

namespace AlahiaPos.Payroll.Core.Tests;

public sealed class DependencyHandlingTests
{
    [Fact]
    public void Hard_dependency_missing_registered_calculator_fails()
    {
        var pack = TestFixtures.Pack(
            new FakeCalculator("EXTRA",
                new[] { new ConceptDependency("GHOST", ConceptDependencyKind.Hard) }));

        var engine = new PayrollEngine();

        var ex = Assert.Throws<PayrollDependencyException>(
            () => engine.Calculate(TestFixtures.Request(pack), pack));

        Assert.Equal("EXTRA", ex.ConceptCode);
        Assert.Equal("GHOST", ex.MissingDependency);
    }

    [Fact]
    public void Hard_dependency_registered_but_no_lines_fails_at_runtime()
    {
        var pack = TestFixtures.Pack(
            new FakeCalculator("BASE", execute: (_, _) => ConceptCalculationResult.Empty),
            new FakeCalculator("EXTRA",
                new[] { new ConceptDependency("BASE", ConceptDependencyKind.Hard) },
                execute: (_, deps) => ConceptCalculationResult.FromLine(
                    new PayrollLine("EXTRA", PayrollLineOrigin.Rule, deps.SumAmount("BASE"), "DOP"))));

        var engine = new PayrollEngine();

        var ex = Assert.Throws<PayrollDependencyException>(
            () => engine.Calculate(TestFixtures.Request(pack), pack));

        Assert.Equal("EXTRA", ex.ConceptCode);
        Assert.Equal("BASE", ex.MissingDependency);
    }

    [Fact]
    public void Soft_dependency_missing_continues()
    {
        var pack = TestFixtures.Pack(
            new FakeCalculator("BASE",
                new[] { new ConceptDependency("OPTIONAL", ConceptDependencyKind.Soft) },
                execute: (_, deps) =>
                {
                    var optional = deps.SumAmount("OPTIONAL");
                    return ConceptCalculationResult.FromLine(
                        new PayrollLine("BASE", PayrollLineOrigin.Rule, 100m + optional, "DOP"));
                }));

        var engine = new PayrollEngine();
        var run = engine.Calculate(TestFixtures.Request(pack), pack);

        Assert.Equal(PayrollRunStatus.Draft, run.Status);
        Assert.Single(run.Lines);
        Assert.Equal(100m, run.Lines[0].Amount);
    }
}
