using System.Collections.ObjectModel;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Engine;
using Xunit;

namespace AlahiaPos.Payroll.Core.Tests;

public sealed class CalculatorExecutionTests
{
    [Fact]
    public void Calculator_returns_lines_and_traces_on_draft_run()
    {
        var pack = TestFixtures.Pack(
            new FakeCalculator("BASE", execute: (ctx, _) =>
            {
                var line = new PayrollLine("BASE", PayrollLineOrigin.Rule, 250.50m, ctx.Money.CurrencyCode);
                var trace = new CalculationTrace(
                    ConceptCode: "BASE",
                    BaseAmount: 250.50m,
                    RuleDescription: "flat",
                    ResultAmount: 250.50m,
                    CalculatorId: "fake:BASE",
                    RulePackId: ctx.RulePackId,
                    RulePackVersion: ctx.RulePackVersion);
                return ConceptCalculationResult.FromLine(line, trace);
            }));

        var engine = new PayrollEngine();
        var draft = engine.Calculate(TestFixtures.Request(pack), pack);

        Assert.Equal(PayrollRunStatus.Draft, draft.Status);
        Assert.Null(draft.SnapshotHash);
        Assert.Single(draft.Lines);
        Assert.Equal(250.50m, draft.Lines[0].Amount);
        Assert.Equal("10", draft.Lines[0].Attributes!["IdEmpleados"]);
        Assert.Single(draft.Traces);
        Assert.Equal("BASE", draft.Traces[0].ConceptCode);

        var approved = engine.Approve(draft, new PayrollApprovalRequest(
            draft.IdEmpresa, draft.PayrollRunId, draft.ExecutionKey));

        Assert.Equal(PayrollRunStatus.Approved, approved.Status);
        Assert.False(string.IsNullOrWhiteSpace(approved.SnapshotHash));
        Assert.NotNull(approved.ApprovedAt);
    }

    [Fact]
    public void Calculator_cannot_mutate_evaluation_context_collections()
    {
        var mutablePack = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["k"] = "v"
        };

        var mutated = false;
        var pack = TestFixtures.Pack(
            new FakeCalculator("BASE", execute: (ctx, _) =>
            {
                if (ctx.PackParameters is IDictionary<string, string> dict)
                {
                    try
                    {
                        dict["k"] = "hacked";
                        mutated = true;
                    }
                    catch (NotSupportedException)
                    {
                        // ReadOnlyDictionary — esperado
                    }
                }

                Assert.IsType<ReadOnlyDictionary<string, string>>(ctx.PackParameters);
                Assert.Equal("v", ctx.PackParameters["k"]);
                return ConceptCalculationResult.FromLine(
                    new PayrollLine("BASE", PayrollLineOrigin.Rule, 1m, "DOP"));
            }));

        var engine = new PayrollEngine();
        var ctx = TestFixtures.Context(packParameters: mutablePack);
        var run = engine.Calculate(TestFixtures.Request(pack, ctx), pack);

        Assert.False(mutated);
        Assert.Equal("v", mutablePack["k"]);
        Assert.Single(run.Lines);
    }
}
