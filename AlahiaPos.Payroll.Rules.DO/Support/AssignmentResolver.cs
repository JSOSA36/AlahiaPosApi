using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Support;

/// <summary>Resuelve asignaciones vigentes contra el período del contexto.</summary>
public static class AssignmentResolver
{
    public static ConceptAssignment? FindActive(
        EvaluationContext context,
        string conceptCode)
    {
        var periodStart = context.Period.StartInclusive;
        var periodEnd = context.Period.EndInclusive;

        return context.Assignments
            .Where(a => string.Equals(a.ConceptCode, conceptCode, StringComparison.Ordinal))
            .Where(a => IsEffective(a, periodStart, periodEnd))
            .OrderByDescending(a => a.EffectiveFrom ?? DateOnly.MinValue)
            .FirstOrDefault();
    }

    public static bool IsEffective(ConceptAssignment assignment, DateOnly periodStart, DateOnly periodEnd)
    {
        // Asignación vigente si solapa el período (ambos extremos inclusive).
        var from = assignment.EffectiveFrom ?? DateOnly.MinValue;
        var to = assignment.EffectiveTo ?? DateOnly.MaxValue;
        return from <= periodEnd && to >= periodStart;
    }
}
