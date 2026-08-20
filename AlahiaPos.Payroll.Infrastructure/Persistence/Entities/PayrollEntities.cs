using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Infrastructure.Persistence.Entities;

public class PayrollEmployeeEntity
{
    public int IdEmpresa { get; set; }
    public int IdEmpleados { get; set; }
    public bool Activo { get; set; } = true;
    public string? Nombre { get; set; }
}

public class PayrollContractAttributeEntity
{
    public long Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdEmpleados { get; set; }
    public string AttributeKey { get; set; } = "";
    public string AttributeValue { get; set; } = "";
}

public class PayrollConceptAssignmentEntity
{
    public long Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdEmpleados { get; set; }
    public string ConceptCode { get; set; } = "";
    public decimal? FixedAmount { get; set; }
    public decimal? Rate { get; set; }
    public string? FormulaOrRuleId { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? AttributesJson { get; set; }
}

/// <summary>Hecho del período ya interpretado (asistencia u otros). Sin semántica fiscal.</summary>
public class PayrollPeriodFactEntity
{
    public long Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdEmpleados { get; set; }
    public string PeriodKey { get; set; } = "";
    public string FactType { get; set; } = "";
    public string? ConceptCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = "OTHER"; // ATTENDANCE | OTHER
    public string? AttributesJson { get; set; }
}

public class PayrollRunEntity
{
    public Guid PayrollRunId { get; set; }
    public int IdEmpresa { get; set; }
    public string PeriodKey { get; set; } = "";
    public string Intent { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string RulePackId { get; set; } = "";
    public string RulePackVersion { get; set; } = "";
    public string Status { get; set; } = nameof(PayrollRunStatus.Draft);
    public string CurrencyCode { get; set; } = "DOP";
    public int DecimalPlaces { get; set; }
    public string RoundingMode { get; set; } = "";
    public bool RoundPerLine { get; set; }
    public bool RoundAggregates { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? SnapshotHash { get; set; }
    public string? SnapshotJson { get; set; }

    public List<PayrollLineEntity> Lines { get; set; } = new();
    public List<PayrollTraceEntity> Traces { get; set; } = new();
}

public class PayrollLineEntity
{
    public long Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public string ConceptCode { get; set; } = "";
    public string Origin { get; set; } = "";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string? Description { get; set; }
    public string? AttributesJson { get; set; }
    public PayrollRunEntity? Run { get; set; }
}

public class PayrollTraceEntity
{
    public long Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public string ConceptCode { get; set; } = "";
    public decimal? BaseAmount { get; set; }
    public string RuleDescription { get; set; } = "";
    public decimal ResultAmount { get; set; }
    public string CalculatorId { get; set; } = "";
    public string RulePackId { get; set; } = "";
    public string RulePackVersion { get; set; } = "";
    public string? DetailsJson { get; set; }
    public PayrollRunEntity? Run { get; set; }
}
