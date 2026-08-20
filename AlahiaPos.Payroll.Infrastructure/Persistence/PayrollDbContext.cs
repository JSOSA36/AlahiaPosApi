using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AlahiaPos.Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options) : base(options) { }

    public DbSet<PayrollEmployeeEntity> Employees => Set<PayrollEmployeeEntity>();
    public DbSet<PayrollContractAttributeEntity> ContractAttributes => Set<PayrollContractAttributeEntity>();
    public DbSet<PayrollConceptAssignmentEntity> ConceptAssignments => Set<PayrollConceptAssignmentEntity>();
    public DbSet<PayrollPeriodFactEntity> PeriodFacts => Set<PayrollPeriodFactEntity>();
    public DbSet<PayrollRunEntity> Runs => Set<PayrollRunEntity>();
    public DbSet<PayrollLineEntity> Lines => Set<PayrollLineEntity>();
    public DbSet<PayrollTraceEntity> Traces => Set<PayrollTraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnly = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));
        var dateOnlyN = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        modelBuilder.Entity<PayrollEmployeeEntity>(e =>
        {
            e.ToTable("Payroll_Employees");
            e.HasKey(x => new { x.IdEmpresa, x.IdEmpleados });
        });

        modelBuilder.Entity<PayrollContractAttributeEntity>(e =>
        {
            e.ToTable("Payroll_ContractAttributes");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.IdEmpresa, x.IdEmpleados, x.AttributeKey }).IsUnique();
            e.Property(x => x.AttributeKey).HasMaxLength(100);
            e.Property(x => x.AttributeValue).HasMaxLength(500);
        });

        modelBuilder.Entity<PayrollConceptAssignmentEntity>(e =>
        {
            e.ToTable("Payroll_ConceptAssignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.IdEmpresa, x.IdEmpleados, x.ConceptCode });
            e.Property(x => x.ConceptCode).HasMaxLength(80);
            e.Property(x => x.FixedAmount).HasPrecision(18, 4);
            e.Property(x => x.Rate).HasPrecision(18, 8);
            e.Property(x => x.EffectiveFrom).HasConversion(dateOnlyN);
            e.Property(x => x.EffectiveTo).HasConversion(dateOnlyN);
        });

        modelBuilder.Entity<PayrollPeriodFactEntity>(e =>
        {
            e.ToTable("Payroll_PeriodFacts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.IdEmpresa, x.IdEmpleados, x.PeriodKey, x.Source });
            e.Property(x => x.PeriodKey).HasMaxLength(40);
            e.Property(x => x.FactType).HasMaxLength(80);
            e.Property(x => x.ConceptCode).HasMaxLength(80);
            e.Property(x => x.Source).HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.Amount).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PayrollRunEntity>(e =>
        {
            e.ToTable("Payroll_Runs");
            e.HasKey(x => x.PayrollRunId);
            e.HasIndex(x => new { x.IdEmpresa, x.PeriodKey, x.Intent }).IsUnique();
            e.Property(x => x.PeriodKey).HasMaxLength(40);
            e.Property(x => x.Intent).HasMaxLength(40);
            e.Property(x => x.RulePackId).HasMaxLength(80);
            e.Property(x => x.RulePackVersion).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
            e.Property(x => x.RoundingMode).HasMaxLength(40);
            e.Property(x => x.SnapshotHash).HasMaxLength(128);
            e.Property(x => x.PeriodStart).HasConversion(dateOnly);
            e.Property(x => x.PeriodEnd).HasConversion(dateOnly);
            e.HasMany(x => x.Lines).WithOne(x => x.Run!).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Traces).WithOne(x => x.Run!).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollLineEntity>(e =>
        {
            e.ToTable("Payroll_Lines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ConceptCode).HasMaxLength(80);
            e.Property(x => x.Origin).HasMaxLength(20);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
        });

        modelBuilder.Entity<PayrollTraceEntity>(e =>
        {
            e.ToTable("Payroll_Traces");
            e.HasKey(x => x.Id);
            e.Property(x => x.ConceptCode).HasMaxLength(80);
            e.Property(x => x.BaseAmount).HasPrecision(18, 4);
            e.Property(x => x.ResultAmount).HasPrecision(18, 4);
            e.Property(x => x.CalculatorId).HasMaxLength(120);
            e.Property(x => x.RulePackId).HasMaxLength(80);
            e.Property(x => x.RulePackVersion).HasMaxLength(40);
        });
    }
}
