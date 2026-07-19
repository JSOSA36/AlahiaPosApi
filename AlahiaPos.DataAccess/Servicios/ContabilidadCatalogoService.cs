using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadCatalogoService : IContabilidadCatalogoService
    {
        private readonly IRepository<CuentaContable> _repository;

        private static readonly (string Codigo, string Nombre, string Tipo, bool PermiteMovimiento, string? PadreCodigo)[] CatalogoDefault =
        {
            ("1", "ACTIVO", ContabilidadConstantes.TipoActivo, false, null),
            ("1.1", "ACTIVO CORRIENTE", ContabilidadConstantes.TipoActivo, false, "1"),
            ("1.1.1", "Caja", ContabilidadConstantes.TipoActivo, true, "1.1"),
            ("1.1.2", "Banco", ContabilidadConstantes.TipoActivo, true, "1.1"),
            ("1.1.3", "Inventario", ContabilidadConstantes.TipoActivo, true, "1.1"),
            ("1.1.4", "Cuentas por Cobrar", ContabilidadConstantes.TipoActivo, true, "1.1"),
            ("1.1.5", "ITBIS por Cobrar", ContabilidadConstantes.TipoActivo, true, "1.1"),
            ("1.2", "ACTIVO NO CORRIENTE", ContabilidadConstantes.TipoActivo, false, "1"),
            ("1.2.1", "Mobiliario y Equipo", ContabilidadConstantes.TipoActivo, true, "1.2"),
            ("1.2.2", "Depreciación Acumulada", ContabilidadConstantes.TipoActivo, true, "1.2"),

            ("2", "PASIVO", ContabilidadConstantes.TipoPasivo, false, null),
            ("2.1", "PASIVO CORRIENTE", ContabilidadConstantes.TipoPasivo, false, "2"),
            ("2.1.1", "Cuentas por Pagar", ContabilidadConstantes.TipoPasivo, true, "2.1"),
            ("2.1.2", "ITBIS por Pagar", ContabilidadConstantes.TipoPasivo, true, "2.1"),
            ("2.1.3", "Nómina por Pagar", ContabilidadConstantes.TipoPasivo, true, "2.1"),
            ("2.2", "PASIVO NO CORRIENTE", ContabilidadConstantes.TipoPasivo, false, "2"),
            ("2.2.1", "Préstamos a Largo Plazo", ContabilidadConstantes.TipoPasivo, true, "2.2"),

            ("3", "CAPITAL", ContabilidadConstantes.TipoCapital, false, null),
            ("3.1", "Capital Social", ContabilidadConstantes.TipoCapital, true, "3"),
            ("3.2", "Utilidades Retenidas", ContabilidadConstantes.TipoCapital, true, "3"),
            ("3.3", "Utilidad del Ejercicio", ContabilidadConstantes.TipoCapital, true, "3"),

            ("4", "INGRESOS", ContabilidadConstantes.TipoIngresos, false, null),
            ("4.1", "Ventas", ContabilidadConstantes.TipoIngresos, true, "4"),
            ("4.2", "Ingresos por Servicios", ContabilidadConstantes.TipoIngresos, true, "4"),
            ("4.3", "Otros Ingresos", ContabilidadConstantes.TipoIngresos, true, "4"),

            ("5", "GASTOS", ContabilidadConstantes.TipoGastos, false, null),
            ("5.1", "Gastos Operativos", ContabilidadConstantes.TipoGastos, true, "5"),
            ("5.2", "Gastos Administrativos", ContabilidadConstantes.TipoGastos, true, "5"),
            ("5.3", "Gastos de Personal", ContabilidadConstantes.TipoGastos, true, "5"),
            ("5.4", "Depreciación", ContabilidadConstantes.TipoGastos, true, "5"),

            ("6", "COSTOS", ContabilidadConstantes.TipoCostos, false, null),
            ("6.1", "Costo de Ventas", ContabilidadConstantes.TipoCostos, true, "6"),
            ("6.2", "Costo de Servicios", ContabilidadConstantes.TipoCostos, true, "6")
        };

        public ContabilidadCatalogoService(IRepository<CuentaContable> repository)
        {
            _repository = repository;
        }

        public async Task<bool> TieneCatalogoAsync(int idEmpresa)
        {
            var cuentas = await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == idEmpresa);
            return cuentas.Any();
        }

        public async Task SeedCatalogoDefaultAsync(int idEmpresa)
        {
            if (await TieneCatalogoAsync(idEmpresa))
                return;

            var idsPorCodigo = new Dictionary<string, int>();

            foreach (var item in CatalogoDefault)
            {
                int? idPadre = null;
                if (!string.IsNullOrEmpty(item.PadreCodigo) && idsPorCodigo.ContainsKey(item.PadreCodigo))
                    idPadre = idsPorCodigo[item.PadreCodigo];

                var nivel = string.IsNullOrEmpty(item.PadreCodigo)
                    ? 1
                    : item.Codigo.Count(c => c == '.') + 1;

                var cuenta = new CuentaContable
                {
                    IdEmpresa = idEmpresa,
                    Codigo = item.Codigo,
                    Nombre = item.Nombre,
                    TipoCuenta = item.Tipo,
                    IdCuentaPadre = idPadre,
                    Nivel = nivel,
                    PermiteMovimiento = item.PermiteMovimiento,
                    Activa = true,
                    FechaInseccion = DateTime.Now
                };

                await _repository.Save(cuenta);
                idsPorCodigo[item.Codigo] = cuenta.IdCuentaContable;
            }
        }
    }
}
