using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Engine;
using AlahiaPos.Payroll.Infrastructure.Context;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using AlahiaPos.Payroll.Infrastructure.Providers;
using AlahiaPos.Payroll.Rules.DO;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlahiaPos.Payroll.Infrastructure.Tests;

/// <summary>
/// Smoke: assignments en PayrollDbContext → EvaluationContextBuilder → Draft DO-2026.01.
/// </summary>
public sealed class ExpedienteToDraftSmokeTests
{
    [Fact]
    public async Task Seeded_sueldo_base_produces_draft_with_dominican_pack()
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PayrollDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Employees.Add(new PayrollEmployeeEntity { IdEmpresa = 1, IdEmpleados = 7, Activo = true });
        db.ConceptAssignments.Add(new PayrollConceptAssignmentEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 7,
            ConceptCode = DoConceptCodes.SueldoBase,
            FixedAmount = 20000m,
            EffectiveFrom = new DateOnly(2025, 1, 1)
        });
        db.ContractAttributes.Add(new PayrollContractAttributeEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 7,
            AttributeKey = "FrecuenciaPago",
            AttributeValue = "QUINCENAL"
        });
        await db.SaveChangesAsync();

        var builder = new EvaluationContextBuilder(
            new EfConceptAssignmentProvider(db),
            new EfEmployeeContractAttributeProvider(db),
            new EfAttendanceFactProvider(db),
            new EfFactProvider(db));

        var pack = DominicanRulePack.Create();
        var period = new PayrollPeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15), "2026-01-A");
        var ctx = await builder.BuildAsync(new EvaluationContextBuildRequest(
            1, 7, period, pack.Metadata.PackId, pack.Metadata.Version,
            pack.Metadata.DefaultMoneyPolicy, pack.Metadata.Parameters));

        Assert.Single(ctx.Assignments);
        Assert.Equal("QUINCENAL", ctx.ContractAttributes!["FrecuenciaPago"]);

        var engine = new PayrollEngine();
        var draft = engine.Calculate(
            new PayrollCalculationRequest(
                1,
                new ExecutionKey(1, period.PeriodKey, "REGULAR"),
                period,
                pack.Metadata.PackId,
                new[] { ctx }),
            pack);

        Assert.Equal(PayrollRunStatus.Draft, draft.Status);
        Assert.Equal(20000m, draft.Lines.Single(l => l.ConceptCode == DoConceptCodes.SueldoBase).Amount);
        Assert.Contains(draft.Lines, l => l.ConceptCode == DoConceptCodes.Neto);
        Assert.Contains(draft.Traces, t => t.ConceptCode == DoConceptCodes.AfpEmpleado);
    }
}
