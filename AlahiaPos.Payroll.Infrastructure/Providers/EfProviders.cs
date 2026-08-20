using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.Payroll.Infrastructure.Providers;

public sealed class EfConceptAssignmentProvider : IConceptAssignmentProvider
{
    private readonly PayrollDbContext _db;

    public EfConceptAssignmentProvider(PayrollDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConceptAssignment>> GetAssignmentsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.ConceptAssignments
            .AsNoTracking()
            .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == idEmpleados)
            .Where(a =>
                (a.EffectiveFrom == null || a.EffectiveFrom <= period.EndInclusive)
                && (a.EffectiveTo == null || a.EffectiveTo >= period.StartInclusive))
            .ToListAsync(cancellationToken);

        return rows.Select(a => new ConceptAssignment(
            a.ConceptCode,
            a.FixedAmount,
            a.Rate,
            a.FormulaOrRuleId,
            a.EffectiveFrom,
            a.EffectiveTo,
            JsonMap.FromJson(a.AttributesJson))).ToList();
    }
}

public sealed class EfEmployeeContractAttributeProvider : IEmployeeContractAttributeProvider
{
    private readonly PayrollDbContext _db;

    public EfEmployeeContractAttributeProvider(PayrollDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, string>> GetContractAttributesAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default)
    {
        _ = period;
        var rows = await _db.ContractAttributes
            .AsNoTracking()
            .Where(a => a.IdEmpresa == idEmpresa && a.IdEmpleados == idEmpleados)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.AttributeKey, r => r.AttributeValue, StringComparer.Ordinal);
    }
}

public sealed class EfAttendanceFactProvider : IAttendanceFactProvider
{
    private readonly PayrollDbContext _db;

    public EfAttendanceFactProvider(PayrollDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollFact>> GetFactsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.PeriodFacts
            .AsNoTracking()
            .Where(f => f.IdEmpresa == idEmpresa
                        && f.IdEmpleados == idEmpleados
                        && f.PeriodKey == period.PeriodKey
                        && f.Source == "ATTENDANCE")
            .ToListAsync(cancellationToken);

        return rows.Select(ToFact).ToList();
    }

    internal static PayrollFact ToFact(PayrollPeriodFactEntity f) =>
        new(f.FactType, f.ConceptCode, f.Quantity, f.Amount, JsonMap.FromJson(f.AttributesJson));
}

public sealed class EfFactProvider : IFactProvider
{
    private readonly PayrollDbContext _db;

    public EfFactProvider(PayrollDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollFact>> GetFactsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.PeriodFacts
            .AsNoTracking()
            .Where(f => f.IdEmpresa == idEmpresa
                        && f.IdEmpleados == idEmpleados
                        && f.PeriodKey == period.PeriodKey
                        && f.Source != "ATTENDANCE")
            .ToListAsync(cancellationToken);

        return rows.Select(EfAttendanceFactProvider.ToFact).ToList();
    }
}
