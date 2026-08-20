namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Tipo de dependencia entre conceptos en el DAG (ALAHIA-PE-01 §8.3).
/// </summary>
public enum ConceptDependencyKind
{
    /// <summary>Si falta, el cálculo del empleado/run falla.</summary>
    Hard = 0,

    /// <summary>Si falta, se trata como cero/vacío y se continúa.</summary>
    Soft = 1
}
