using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core;
using AlahiaPos.Payroll.Core.Engine;
using Xunit;

namespace AlahiaPos.Payroll.Core.Tests;

public sealed class CycleDetectionTests
{
    [Fact]
    public void Cycle_A_depends_B_and_B_depends_A_rejects_run()
    {
        var pack = TestFixtures.Pack(
            new FakeCalculator("A", new[] { new ConceptDependency("B", ConceptDependencyKind.Hard) }),
            new FakeCalculator("B", new[] { new ConceptDependency("A", ConceptDependencyKind.Hard) }));

        var engine = new PayrollEngine();

        var ex = Assert.Throws<PayrollCycleException>(
            () => engine.Calculate(TestFixtures.Request(pack), pack));

        Assert.Contains("A", ex.CyclePath);
        Assert.Contains("B", ex.CyclePath);
    }
}
