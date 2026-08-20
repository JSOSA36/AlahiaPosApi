namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Conceptos de mapeo contable (Configuración de Integración → cuenta del plan).
    /// </summary>
    public static class ContabilidadConceptosMapeo
    {
        public const string Caja = "CAJA";
        public const string Banco = "BANCO";
        public const string Inventario = "INVENTARIO";
        public const string Cxc = "CXC";
        public const string ItbisCobrar = "ITBIS_COBRAR";
        public const string Cxp = "CXP";
        public const string ItbisPagar = "ITBIS_PAGAR";
        public const string Ventas = "VENTAS";
        public const string OtrosIngresos = "OTROS_INGRESOS";
        public const string GastoOperativo = "GASTO_OPERATIVO";
        public const string GastoNomina = "GASTO_NOMINA";
        public const string AfpPorPagar = "AFP_POR_PAGAR";
        public const string SfsPorPagar = "SFS_POR_PAGAR";
        public const string IsrPorPagar = "ISR_POR_PAGAR";
        public const string PrestamosEmpleados = "PRESTAMOS_EMPLEADOS";
        public const string AnticiposEmpleados = "ANTICIPOS_EMPLEADOS";
        public const string CostoVentas = "COSTO_VENTAS";
        public const string ActivoFijo = "ACTIVO_FIJO";

        /// <summary>Código de cuenta del catálogo default asociado a cada concepto.</summary>
        public static readonly (string Concepto, string CodigoCuenta)[] Defaults =
        {
            (Caja, "1.1.1"),
            (Banco, "1.1.2"),
            (Inventario, "1.1.3"),
            (Cxc, "1.1.4"),
            (ItbisCobrar, "1.1.5"),
            (Cxp, "2.1.1"),
            (ItbisPagar, "2.1.2"),
            (Ventas, "4.1"),
            (OtrosIngresos, "4.3"),
            (GastoOperativo, "5.1"),
            (GastoNomina, "5.1"),
            (CostoVentas, "6.1"),
            (ActivoFijo, "1.2.1"),
        };
    }
}
