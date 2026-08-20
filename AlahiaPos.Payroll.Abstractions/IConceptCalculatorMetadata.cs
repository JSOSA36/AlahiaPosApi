namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Metadata de un calculator para el DAG (ALAHIA-PE-01 §7.3 / §8).
/// Separada de la ejecución para permitir inspección sin invocar cálculo.
/// </summary>
public interface IConceptCalculatorMetadata
{
    /// <summary>Concepto primario que este calculator posee en el grafo.</summary>
    string ConceptCode { get; }

    /// <summary>Identificador estable del calculator (auditoría / traces).</summary>
    string CalculatorId { get; }

    /// <summary>Dependencias explícitas hacia otros ConceptCode.</summary>
    IReadOnlyList<ConceptDependency> DependsOn { get; }
}
