namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Persistencia de <see cref="PayrollRun"/> (borrador, aprobación, snapshot, idempotencia).
/// Implementación en Infrastructure; el Core no la referencia.
/// </summary>
public interface IPayrollRunStore
{
    /// <summary>True si ya existe un run (cualquier estado) para la clave.</summary>
    Task<bool> ExistsByExecutionKeyAsync(
        ExecutionKey executionKey,
        CancellationToken cancellationToken = default);

    Task<PayrollRun?> GetByIdAsync(
        int idEmpresa,
        Guid payrollRunId,
        CancellationToken cancellationToken = default);

    Task<PayrollRun?> GetByExecutionKeyAsync(
        ExecutionKey executionKey,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste un Draft. Falla si la ExecutionKey ya existe.</summary>
    Task SaveDraftAsync(
        PayrollRun run,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste el run aprobado con snapshot hash obligatorio.
    /// Debe ser idempotente si el mismo run ya está Approved con el mismo hash.
    /// </summary>
    Task SaveApprovedAsync(
        PayrollRun run,
        CancellationToken cancellationToken = default);
}
