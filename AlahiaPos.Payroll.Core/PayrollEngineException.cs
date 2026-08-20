namespace AlahiaPos.Payroll.Core;

/// <summary>Error de dominio del Payroll Engine (ALAHIA-PE-01).</summary>
public class PayrollEngineException : Exception
{
    public PayrollEngineException(string message) : base(message) { }
    public PayrollEngineException(string message, Exception inner) : base(message, inner) { }
}

public sealed class PayrollCycleException : PayrollEngineException
{
    public IReadOnlyList<string> CyclePath { get; }

    public PayrollCycleException(IReadOnlyList<string> cyclePath)
        : base($"Dependency cycle detected: {string.Join(" → ", cyclePath)}")
    {
        CyclePath = cyclePath;
    }
}

public sealed class PayrollDependencyException : PayrollEngineException
{
    public string ConceptCode { get; }
    public string MissingDependency { get; }

    public PayrollDependencyException(string conceptCode, string missingDependency)
        : base($"Hard dependency '{missingDependency}' missing for concept '{conceptCode}'.")
    {
        ConceptCode = conceptCode;
        MissingDependency = missingDependency;
    }
}

public sealed class PayrollValidationException : PayrollEngineException
{
    public PayrollValidationException(string message) : base(message) { }
}
