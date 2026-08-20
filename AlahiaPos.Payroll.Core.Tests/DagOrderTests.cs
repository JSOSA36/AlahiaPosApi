using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Dag;
using AlahiaPos.Payroll.Core.Engine;
using Xunit;

namespace AlahiaPos.Payroll.Core.Tests;

public sealed class DagOrderTests
{
    [Fact]
    public void Topological_order_runs_A_then_B_then_C()
    {
        var orderLog = new List<string>();

        Func<EvaluationContext, IResolvedLinesView, ConceptCalculationResult> Exec(string code) =>
            (_, deps) =>
            {
                orderLog.Add(code);
                var amount = code switch
                {
                    "A" => 10m,
                    "B" => deps.SumAmount("A") + 1m,
                    "C" => deps.SumAmount("B") + 1m,
                    _ => 0m
                };
                return ConceptCalculationResult.FromLine(
                    new PayrollLine(code, PayrollLineOrigin.Rule, amount, "DOP"));
            };

        var pack = TestFixtures.Pack(
            new FakeCalculator("A", execute: Exec("A")),
            new FakeCalculator("B",
                new[] { new ConceptDependency("A", ConceptDependencyKind.Hard) },
                Exec("B")),
            new FakeCalculator("C",
                new[] { new ConceptDependency("B", ConceptDependencyKind.Hard) },
                Exec("C")));

        var engine = new PayrollEngine();
        var run = engine.Calculate(TestFixtures.Request(pack), pack);

        Assert.Equal(new[] { "A", "B", "C" }, orderLog);
        Assert.Equal(PayrollRunStatus.Draft, run.Status);
        Assert.Equal(10m, run.Lines.Single(l => l.ConceptCode == "A").Amount);
        Assert.Equal(11m, run.Lines.Single(l => l.ConceptCode == "B").Amount);
        Assert.Equal(12m, run.Lines.Single(l => l.ConceptCode == "C").Amount);
    }

    [Fact]
    public void DagBuilder_and_TopologicalSorter_respect_prerequisites()
    {
        var factory = new ConceptCalculatorFactory(new IConceptCalculator[]
        {
            new FakeCalculator("C", new[] { new ConceptDependency("B") }),
            new FakeCalculator("B", new[] { new ConceptDependency("A") }),
            new FakeCalculator("A")
        });

        var graph = DagBuilder.Build(factory.AllMetadata);
        CycleDetector.EnsureAcyclic(graph);
        var order = TopologicalSorter.Sort(graph);

        Assert.Equal(new[] { "A", "B", "C" }, order.ToArray());
    }
}
