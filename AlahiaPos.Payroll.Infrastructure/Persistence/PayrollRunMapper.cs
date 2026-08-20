using System.Text.Json;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Persistence.Entities;

namespace AlahiaPos.Payroll.Infrastructure.Persistence;

internal static class JsonMap
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string? ToJson(IReadOnlyDictionary<string, string>? dict) =>
        dict is null || dict.Count == 0 ? null : JsonSerializer.Serialize(dict, Options);

    public static IReadOnlyDictionary<string, string>? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options);
    }
}

internal static class PayrollRunMapper
{
    public static PayrollRunEntity ToEntity(PayrollRun run, string? snapshotJson = null)
    {
        var entity = new PayrollRunEntity
        {
            PayrollRunId = run.PayrollRunId,
            IdEmpresa = run.IdEmpresa,
            PeriodKey = run.ExecutionKey.PeriodKey,
            Intent = run.ExecutionKey.Intent,
            PeriodStart = run.Period.StartInclusive,
            PeriodEnd = run.Period.EndInclusive,
            RulePackId = run.RulePackId,
            RulePackVersion = run.RulePackVersion,
            Status = run.Status.ToString(),
            CurrencyCode = run.Money.CurrencyCode,
            DecimalPlaces = run.Money.DecimalPlaces,
            RoundingMode = run.Money.RoundingMode.ToString(),
            RoundPerLine = run.Money.RoundPerLine,
            RoundAggregates = run.Money.RoundAggregates,
            CreatedAt = run.CreatedAt,
            ApprovedAt = run.ApprovedAt,
            SnapshotHash = run.SnapshotHash,
            SnapshotJson = snapshotJson,
            Lines = run.Lines.Select(l => new PayrollLineEntity
            {
                PayrollRunId = run.PayrollRunId,
                ConceptCode = l.ConceptCode,
                Origin = l.Origin.ToString(),
                Amount = l.Amount,
                CurrencyCode = l.CurrencyCode,
                Description = l.Description,
                AttributesJson = JsonMap.ToJson(l.Attributes)
            }).ToList(),
            Traces = run.Traces.Select(t => new PayrollTraceEntity
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
            }).ToList()
        };
        return entity;
    }

    public static PayrollRun ToDomain(PayrollRunEntity e)
    {
        if (!Enum.TryParse<PayrollRunStatus>(e.Status, out var status))
            status = PayrollRunStatus.Draft;
        if (!Enum.TryParse<MoneyRoundingMode>(e.RoundingMode, out var rounding))
            rounding = MoneyRoundingMode.AwayFromZero;

        var money = new MoneyPolicy(
            e.CurrencyCode,
            e.DecimalPlaces,
            rounding,
            e.RoundPerLine,
            e.RoundAggregates);

        var lines = e.Lines.Select(l =>
        {
            Enum.TryParse<PayrollLineOrigin>(l.Origin, out var origin);
            return new PayrollLine(
                l.ConceptCode,
                origin,
                l.Amount,
                l.CurrencyCode,
                l.Description,
                JsonMap.FromJson(l.AttributesJson));
        }).ToList();

        var traces = e.Traces.Select(t => new CalculationTrace(
            t.ConceptCode,
            t.BaseAmount,
            t.RuleDescription,
            t.ResultAmount,
            t.CalculatorId,
            t.RulePackId,
            t.RulePackVersion,
            JsonMap.FromJson(t.DetailsJson))).ToList();

        return new PayrollRun(
            e.PayrollRunId,
            e.IdEmpresa,
            new ExecutionKey(e.IdEmpresa, e.PeriodKey, e.Intent),
            new PayrollPeriod(e.PeriodStart, e.PeriodEnd, e.PeriodKey),
            e.RulePackId,
            e.RulePackVersion,
            status,
            money,
            lines,
            traces,
            e.CreatedAt,
            e.ApprovedAt,
            e.SnapshotHash);
    }
}
