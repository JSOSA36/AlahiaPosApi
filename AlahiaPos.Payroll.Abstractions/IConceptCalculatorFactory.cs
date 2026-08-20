namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Resuelve <see cref="IConceptCalculator"/> por <c>ConceptCode</c> para el DAG.
/// Implementación típica: registro en memoria construido desde el RulePack activo.
/// Sin I/O.
/// </summary>
public interface IConceptCalculatorFactory
{
    /// <summary>Calculators / metadata disponibles en el grafo actual.</summary>
    IReadOnlyList<IConceptCalculatorMetadata> AllMetadata { get; }

    bool TryGet(string conceptCode, out IConceptCalculator? calculator);

    /// <summary>Falla si el concepto no está registrado.</summary>
    IConceptCalculator GetRequired(string conceptCode);
}
