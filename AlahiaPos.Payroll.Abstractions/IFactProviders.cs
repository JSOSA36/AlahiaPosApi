namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Carga hechos de asistencia ya interpretados hacia el contexto (antes del DAG).
/// Puede usar infraestructura; nunca se invoca desde un calculator.
/// </summary>
public interface IAttendanceFactProvider
{
    Task<IReadOnlyList<PayrollFact>> GetFactsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Otros hechos del período (préstamos, comisiones POS, etc.).
/// </summary>
public interface IFactProvider
{
    Task<IReadOnlyList<PayrollFact>> GetFactsAsync(
        int idEmpresa,
        int idEmpleados,
        PayrollPeriod period,
        CancellationToken cancellationToken = default);
}
