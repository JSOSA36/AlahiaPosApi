using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlahiaPos.Payroll.Infrastructure.Tests;

internal static class TestDb
{
    public static PayrollDbContext Create()
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PayrollDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static readonly MoneyPolicy Money = new(
        "DOP", 2, MoneyRoundingMode.AwayFromZero, true, true);

    public static readonly PayrollPeriod Period = new(
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 1, 15),
        "2026-01-A");
}

public sealed class EvaluationContextBuilderTests
{
    [Fact]
    public async Task Builds_context_from_assignments_and_facts()
    {
        await using var db = TestDb.Create();
        db.Employees.Add(new PayrollEmployeeEntity { IdEmpresa = 1, IdEmpleados = 10, Activo = true, Nombre = "Test" });
        db.ConceptAssignments.Add(new PayrollConceptAssignmentEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 10,
            ConceptCode = "BASE_PAY",
            FixedAmount = 20000m,
            EffectiveFrom = new DateOnly(2025, 1, 1)
        });
        db.ContractAttributes.Add(new PayrollContractAttributeEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 10,
            AttributeKey = "Schedule",
            AttributeValue = "FULL"
        });
        db.PeriodFacts.Add(new PayrollPeriodFactEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 10,
            PeriodKey = TestDb.Period.PeriodKey,
            FactType = "OVERTIME",
            ConceptCode = "OVERTIME",
            Amount = 1500m,
            Source = "ATTENDANCE"
        });
        db.PeriodFacts.Add(new PayrollPeriodFactEntity
        {
            IdEmpresa = 1,
            IdEmpleados = 10,
            PeriodKey = TestDb.Period.PeriodKey,
            FactType = "LOAN",
            ConceptCode = "LOAN_DEDUCTION",
            Amount = 500m,
            Source = "OTHER"
        });
        await db.SaveChangesAsync();

        var builder = new Context.EvaluationContextBuilder(
            new Providers.EfConceptAssignmentProvider(db),
            new Providers.EfEmployeeContractAttributeProvider(db),
            new Providers.EfAttendanceFactProvider(db),
            new Providers.EfFactProvider(db));

        var ctx = await builder.BuildAsync(new EvaluationContextBuildRequest(
            1, 10, TestDb.Period, "TEST-PACK", "1.0.0", TestDb.Money,
            new Dictionary<string, string> { ["X"] = "1" }));

        Assert.Equal(1, ctx.IdEmpresa);
        Assert.Equal(10, ctx.IdEmpleados);
        Assert.Single(ctx.Assignments);
        Assert.Equal(20000m, ctx.Assignments[0].FixedAmount);
        Assert.Equal(2, ctx.Facts.Count);
        Assert.Equal("FULL", ctx.ContractAttributes!["Schedule"]);
        Assert.Equal("1", ctx.PackParameters["X"]);
    }
}

public sealed class PayrollRunStoreTests
{
    [Fact]
    public async Task Save_draft_and_reload_by_execution_key()
    {
        await using var db = TestDb.Create();
        var store = new Stores.EfPayrollRunStore(db);
        var key = new ExecutionKey(1, TestDb.Period.PeriodKey, "REGULAR");
        var run = new PayrollRun(
            Guid.NewGuid(),
            1,
            key,
            TestDb.Period,
            "TEST-PACK",
            "1.0.0",
            PayrollRunStatus.Draft,
            TestDb.Money,
            new[] { new PayrollLine("BASE_PAY", PayrollLineOrigin.Assignment, 1000m, "DOP") },
            new[]
            {
                new CalculationTrace("BASE_PAY", 1000m, "test", 1000m, "calc", "TEST-PACK", "1.0.0")
            },
            DateTimeOffset.UtcNow);

        await store.SaveDraftAsync(run);

        var loaded = await store.GetByExecutionKeyAsync(key);
        Assert.NotNull(loaded);
        Assert.Equal(run.PayrollRunId, loaded!.PayrollRunId);
        Assert.Equal(PayrollRunStatus.Draft, loaded.Status);
        Assert.Single(loaded.Lines);
        Assert.Single(loaded.Traces);
        Assert.True(await store.ExistsByExecutionKeyAsync(key));
    }

    [Fact]
    public async Task Duplicate_execution_key_on_draft_fails()
    {
        await using var db = TestDb.Create();
        var store = new Stores.EfPayrollRunStore(db);
        var key = new ExecutionKey(1, TestDb.Period.PeriodKey, "REGULAR");

        PayrollRun Make() => new(
            Guid.NewGuid(), 1, key, TestDb.Period, "P", "1",
            PayrollRunStatus.Draft, TestDb.Money,
            Array.Empty<PayrollLine>(), Array.Empty<CalculationTrace>(),
            DateTimeOffset.UtcNow);

        await store.SaveDraftAsync(Make());
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveDraftAsync(Make()));
    }

    [Fact]
    public async Task Approve_persists_snapshot_hash_and_is_idempotent()
    {
        await using var db = TestDb.Create();
        var store = new Stores.EfPayrollRunStore(db);
        var id = Guid.NewGuid();
        var key = new ExecutionKey(1, TestDb.Period.PeriodKey, "REGULAR");

        var draft = new PayrollRun(
            id, 1, key, TestDb.Period, "P", "1",
            PayrollRunStatus.Draft, TestDb.Money,
            new[] { new PayrollLine("BASE_PAY", PayrollLineOrigin.Rule, 100m, "DOP") },
            Array.Empty<CalculationTrace>(),
            DateTimeOffset.UtcNow);
        await store.SaveDraftAsync(draft);

        var approved = draft with
        {
            Status = PayrollRunStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow,
            SnapshotHash = "ABC123"
        };

        await store.SaveApprovedAsync(approved);
        await store.SaveApprovedAsync(approved); // idempotent

        var loaded = await store.GetByIdAsync(1, id);
        Assert.Equal(PayrollRunStatus.Approved, loaded!.Status);
        Assert.Equal("ABC123", loaded.SnapshotHash);
    }
}

public sealed class InfrastructureIsolationTests
{
    [Fact]
    public void Infrastructure_csproj_does_not_reference_core_or_rules()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "AlahiaPos.Payroll.Infrastructure",
            "AlahiaPos.Payroll.Infrastructure.csproj"));
        var text = File.ReadAllText(path);
        Assert.Contains("AlahiaPos.Payroll.Abstractions", text);
        Assert.DoesNotContain("AlahiaPos.Payroll.Core", text);
        Assert.DoesNotContain("AlahiaPos.Payroll.Rules", text);
    }
}
