using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Engine;
using AlahiaPos.Payroll.Rules.DO;
using AlahiaPos.Payroll.Rules.DO.Tables;
using Xunit;

namespace AlahiaPos.Payroll.Rules.DO.Tests;

public sealed class DominicanRulePackEndToEndTests
{
    [Fact]
    public void Full_run_produces_incomes_deductions_employer_and_neto()
    {
        const decimal sueldo = 30_000m;
        const decimal he = 5_000m;
        var baseCotizable = sueldo + he;

        var afpEmp = DoTestFixtures.Round(baseCotizable * DoTestFixtures.Rate(DoPackParameters.AfpEmployeeRate));
        var sfsEmp = DoTestFixtures.Round(baseCotizable * DoTestFixtures.Rate(DoPackParameters.SfsEmployeeRate));
        var taxable = baseCotizable - afpEmp - sfsEmp;
        var brackets = IsrBracketTable.Resolve(DoPackParameters.DefaultIsrTableVersion);
        var isr = IsrBracketTable.ComputePeriodTax(taxable, brackets, DoMoney.DefaultDopPolicy);

        var afpPat = DoTestFixtures.Round(baseCotizable * DoTestFixtures.Rate(DoPackParameters.AfpEmployerRate));
        var sfsPat = DoTestFixtures.Round(baseCotizable * DoTestFixtures.Rate(DoPackParameters.SfsEmployerRate));
        var infotep = DoTestFixtures.Round(baseCotizable * DoTestFixtures.Rate(DoPackParameters.InfotepRate));
        var neto = DoTestFixtures.Round(sueldo + he - afpEmp - sfsEmp - isr);

        var engine = new PayrollEngine();
        var draft = engine.Calculate(
            DoTestFixtures.Request(DoTestFixtures.Context(sueldo, he)),
            DoTestFixtures.Pack);

        Assert.Equal(PayrollRunStatus.Draft, draft.Status);
        Assert.Equal(DoPackParameters.PackId, draft.RulePackId);
        Assert.Equal(DoPackParameters.Version, draft.RulePackVersion);

        Assert.Equal(sueldo, Line(draft, DoConceptCodes.SueldoBase));
        Assert.Equal(he, Line(draft, DoConceptCodes.HorasExtra));
        Assert.Equal(afpEmp, Line(draft, DoConceptCodes.AfpEmpleado));
        Assert.Equal(sfsEmp, Line(draft, DoConceptCodes.SfsEmpleado));
        Assert.Equal(isr, Line(draft, DoConceptCodes.IsrEmpleado));
        Assert.Equal(afpPat, Line(draft, DoConceptCodes.AfpPatronal));
        Assert.Equal(sfsPat, Line(draft, DoConceptCodes.SfsPatronal));
        Assert.Equal(infotep, Line(draft, DoConceptCodes.InfotepPatronal));
        Assert.Equal(neto, Line(draft, DoConceptCodes.Neto));

        // Patronales no entran en neto (ya verificado por fórmula).
        Assert.True(neto < sueldo + he);

        var approved = engine.Approve(draft, new PayrollApprovalRequest(
            draft.IdEmpresa, draft.PayrollRunId, draft.ExecutionKey));
        Assert.Equal(PayrollRunStatus.Approved, approved.Status);
        Assert.False(string.IsNullOrWhiteSpace(approved.SnapshotHash));
    }

    [Fact]
    public void Without_horas_extra_fact_continues_with_zero()
    {
        var engine = new PayrollEngine();
        var run = engine.Calculate(
            DoTestFixtures.Request(DoTestFixtures.Context(20_000m, horasExtra: null)),
            DoTestFixtures.Pack);

        Assert.Equal(0m, Line(run, DoConceptCodes.HorasExtra));
        Assert.Equal(20_000m, Line(run, DoConceptCodes.SueldoBase));
        Assert.Contains(run.Lines, l => l.ConceptCode == DoConceptCodes.Neto);
    }

    [Fact]
    public void Low_salary_isr_is_zero()
    {
        // 10_000 × 24 = 240_000 < primer tramo gravable ilustrativo.
        var engine = new PayrollEngine();
        var run = engine.Calculate(
            DoTestFixtures.Request(DoTestFixtures.Context(10_000m)),
            DoTestFixtures.Pack);

        Assert.Equal(0m, Line(run, DoConceptCodes.IsrEmpleado));
    }

    [Fact]
    public void High_salary_isr_is_positive()
    {
        var engine = new PayrollEngine();
        var run = engine.Calculate(
            DoTestFixtures.Request(DoTestFixtures.Context(40_000m, 2_000m)),
            DoTestFixtures.Pack);

        Assert.True(Line(run, DoConceptCodes.IsrEmpleado) > 0m);
    }

    [Fact]
    public void Every_calculator_emits_calculation_trace()
    {
        var engine = new PayrollEngine();
        var run = engine.Calculate(
            DoTestFixtures.Request(DoTestFixtures.Context(25_000m, 1_000m)),
            DoTestFixtures.Pack);

        foreach (var code in DoConceptCodes.All)
        {
            var trace = Assert.Single(run.Traces, t => t.ConceptCode == code);
            Assert.Equal(DoPackParameters.PackId, trace.RulePackId);
            Assert.Equal(DoPackParameters.Version, trace.RulePackVersion);
            Assert.False(string.IsNullOrWhiteSpace(trace.CalculatorId));
            Assert.False(string.IsNullOrWhiteSpace(trace.RuleDescription));
        }
    }

    [Fact]
    public void Assignment_outside_period_fails_clearly()
    {
        var ctx = DoTestFixtures.Context(
            20_000m,
            assignmentFrom: new DateOnly(2024, 1, 1),
            assignmentTo: new DateOnly(2024, 12, 31));

        var engine = new PayrollEngine();
        var ex = Assert.Throws<DoRulePackException>(
            () => engine.Calculate(DoTestFixtures.Request(ctx), DoTestFixtures.Pack));

        Assert.Contains(DoConceptCodes.SueldoBase, ex.Message);
    }

    [Fact]
    public void Pack_project_does_not_reference_core()
    {
        var csproj = File.ReadAllText(
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "AlahiaPos.Payroll.Rules.DO",
                "AlahiaPos.Payroll.Rules.DO.csproj")));

        Assert.DoesNotContain("AlahiaPos.Payroll.Core", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AlahiaPos.Payroll.Abstractions", csproj, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal Line(PayrollRun run, string conceptCode) =>
        run.Lines.Single(l => l.ConceptCode == conceptCode).Amount;
}
