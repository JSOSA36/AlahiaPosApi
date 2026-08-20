namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Carga asignaciones de conceptos vigentes hacia el contexto (antes del DAG).
/// </summary>
public interface IConceptAssignmentProvider
{
    Task<IReadOnlyList<ConceptAssignment>> GetAssignmentsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Atributos de contrato laboral ya materializados (clave/valor), sin semántica legal.
/// </summary>
public interface IEmployeeContractAttributeProvider
{
    Task<IReadOnlyDictionary<string, string>> GetContractAttributesAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default);
}
