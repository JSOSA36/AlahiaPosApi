using System.Text.Json;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.Payroll.Infrastructure.Stores;

public sealed class EfPayrollRunStore : IPayrollRunStore
{
    private readonly PayrollDbContext _db;

    public EfPayrollRunStore(PayrollDbContext db) => _db = db;

    public Task<bool> ExistsByExecutionKeyAsync(ExecutionKey executionKey, CancellationToken cancellationToken = default) =>
        _db.Runs.AnyAsync(
            r => r.IdEmpresa == executionKey.IdEmpresa
                 && r.PeriodKey == executionKey.PeriodKey
                 && r.Intent == executionKey.Intent,
            cancellationToken);

    public async Task<PayrollRun?> GetByIdAsync(int idEmpresa, Guid payrollRunId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Runs
            .AsNoTracking()
            .Include(r => r.Lines)
            .Include(r => r.Traces)
            .FirstOrDefaultAsync(r => r.IdEmpresa == idEmpresa && r.PayrollRunId == payrollRunId, cancellationToken);
        return entity is null ? null : PayrollRunMapper.ToDomain(entity);
    }

    public async Task<PayrollRun?> GetByExecutionKeyAsync(ExecutionKey executionKey, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Runs
            .AsNoTracking()
            .Include(r => r.Lines)
            .Include(r => r.Traces)
            .FirstOrDefaultAsync(
                r => r.IdEmpresa == executionKey.IdEmpresa
                     && r.PeriodKey == executionKey.PeriodKey
                     && r.Intent == executionKey.Intent,
                cancellationToken);
        return entity is null ? null : PayrollRunMapper.ToDomain(entity);
    }

    public async Task SaveDraftAsync(PayrollRun run, CancellationToken cancellationToken = default)
    {
        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("SaveDraftAsync requires Draft status.");

        if (await ExistsByExecutionKeyAsync(run.ExecutionKey, cancellationToken))
            throw new InvalidOperationException(
                $"ExecutionKey already exists ({run.ExecutionKey.IdEmpresa}/{run.ExecutionKey.PeriodKey}/{run.ExecutionKey.Intent}).");

        _db.Runs.Add(PayrollRunMapper.ToEntity(run));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveApprovedAsync(PayrollRun run, CancellationToken cancellationToken = default)
    {
        if (run.Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException("SaveApprovedAsync requires Approved status.");
        if (string.IsNullOrWhiteSpace(run.SnapshotHash))
            throw new InvalidOperationException("Approved run requires SnapshotHash.");

        var existing = await _db.Runs
            .Include(r => r.Lines)
            .Include(r => r.Traces)
            .FirstOrDefaultAsync(r => r.PayrollRunId == run.PayrollRunId, cancellationToken);

        var snapshotJson = JsonSerializer.Serialize(new
        {
            run.PayrollRunId,
            run.IdEmpresa,
            run.ExecutionKey.PeriodKey,
            run.ExecutionKey.Intent,
            PeriodStart = run.Period.StartInclusive.ToString("yyyy-MM-dd"),
            PeriodEnd = run.Period.EndInclusive.ToString("yyyy-MM-dd"),
            run.RulePackId,
            run.RulePackVersion,
            Status = run.Status.ToString(),
            run.SnapshotHash,
            run.ApprovedAt,
            Lines = run.Lines,
            Traces = run.Traces
        });

        if (existing is null)
        {
            if (await ExistsByExecutionKeyAsync(run.ExecutionKey, cancellationToken))
                throw new InvalidOperationException("ExecutionKey conflict on approve.");

            _db.Runs.Add(PayrollRunMapper.ToEntity(run, snapshotJson));
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (existing.IdEmpresa != run.IdEmpresa)
            throw new InvalidOperationException("IdEmpresa mismatch.");

        if (existing.Status == nameof(PayrollRunStatus.Approved)
            && existing.SnapshotHash == run.SnapshotHash)
            return; // idempotent

        if (existing.Status == nameof(PayrollRunStatus.Approved))
            throw new InvalidOperationException("Run already approved with a different snapshot.");

        _db.Lines.RemoveRange(existing.Lines);
        _db.Traces.RemoveRange(existing.Traces);

        existing.Status = nameof(PayrollRunStatus.Approved);
        existing.ApprovedAt = run.ApprovedAt;
        existing.SnapshotHash = run.SnapshotHash;
        existing.SnapshotJson = snapshotJson;
        existing.RulePackId = run.RulePackId;
        existing.RulePackVersion = run.RulePackVersion;

        existing.Lines = run.Lines.Select(l => new PayrollLineEntity
        {
            PayrollRunId = run.PayrollRunId,
            ConceptCode = l.ConceptCode,
            Origin = l.Origin.ToString(),
            Amount = l.Amount,
            CurrencyCode = l.CurrencyCode,
            Description = l.Description,
            AttributesJson = JsonMap.ToJson(l.Attributes)
        }).ToList();

        existing.Traces = run.Traces.Select(t => new PayrollTraceEntity
        {
            PayrollRunId = run.PayrollRunId,
            ConceptCode = t.ConceptCode,
            BaseAmount = t.BaseAmount,
            RuleDescription = t.RuleDescription,
            ResultAmount = t.ResultAmount,
            CalculatorId = t.CalculatorId,
            RulePackId = t.RulePackId,
            RulePackVersion = t.RulePackVersion,
            DetailsJson = JsonMap.ToJson(t.Details)
        }).ToList();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
