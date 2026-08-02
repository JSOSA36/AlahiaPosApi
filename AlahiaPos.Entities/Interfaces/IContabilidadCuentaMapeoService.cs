namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadCuentaMapeoService
    {
        Task<int?> ResolverAsync(int idEmpresa, string codigoConcepto);

        /// <summary>
        /// Resuelve cuenta o lanza si falta mapeo (asiento incompleto).
        /// </summary>
        Task<int> ResolverRequeridoAsync(int idEmpresa, string codigoConcepto);

        /// <summary>
        /// CAJA vs BANCO según TipoCuenta financiera o texto de forma de pago.
        /// </summary>
        Task<int> ResolverTesoreriaAsync(
            int idEmpresa,
            string? formaPago = null,
            string? tipoCuentaFinanciera = null);

        /// <summary>
        /// Resuelve tesorería priorizando la cuenta contable configurada en la CuentaFinanciera;
        /// si no hay, cae al mapeo CAJA/BANCO por forma de pago o tipo de cuenta.
        /// </summary>
        Task<int> ResolverTesoreriaPorCuentaAsync(
            int idEmpresa,
            int? idCuentaFinanciera,
            string? formaPago = null,
            string? tipoCuentaFinanciera = null);

        Task EnsureMapeoDefaultAsync(int idEmpresa);
    }
}
