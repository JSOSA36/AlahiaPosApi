using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Plantillas de módulos por giro para demos de calle / alta rápida.
    /// Internos MacroBits nunca se incluyen.
    /// </summary>
    public static class DemoVerticalPresets
    {
        public sealed class VerticalPreset
        {
            public string Codigo { get; init; } = "";
            public string Nombre { get; init; } = "";
            public string Descripcion { get; init; } = "";
            public string[] CodigosModulo { get; init; } = Array.Empty<string>();
        }

        /// <summary>Solo internos / plataforma — nunca licenciar a cliente.</summary>
        public static readonly HashSet<string> CodigosInternosNunca = new(StringComparer.OrdinalIgnoreCase)
        {
            "ALAHIA_AI",
            "MACROBITS_ADMIN",
            "EMPRESAS_ADMIN",
            "SUSCRIPCIONES_COBROS",
            "PAGO_SUSCRIPCION",
            "TICKETS_ADMIN",
            "POLITICAS_VERSIONES",
            "POLITICAS_ACEPTACIONES",
            "NCF_SECUENCIAS",
            "FE_CERTIFICACION",
        };

        private static readonly string[] Nucleo =
        {
            "EMPRESA", "USUARIOS", "PERFILES", "PARAMETROS", "DASHBOARD",
            "PRODUCTOS", "CATEGORIAS", "CLIENTES", "POS", "ORDENES",
            "HISTORICO_FACTURAS", "FE_SECUENCIAS", "IMPRESION_TERMICA",
            "DESCUENTOS", "EMPLEADOS", "TICKETS",
            "LISTADO_DEVOLUCIONES", "NOTAS_CREDITO_APLICADAS",
            "REPORTE_VENTA", "REPORTE_607", "CUENTAS_COBRAR",
        };

        private static readonly string[] Caja =
        {
            "CIERRE_CAJA", "LISTADO_CAJA", "MOVIMIENTO_CAJA",
        };

        private static readonly string[] Inventario =
        {
            "ALMACENES", "MOVIMIENTO_INVENTARIO",
        };

        private static readonly string[] Compras =
        {
            "PROVEEDORES", "FACTURAS_COMPRA", "ORDENES_COMPRA", "CUENTAS_PAGAR_PROVEEDOR",
        };

        private static readonly string[] Tesoreria =
        {
            "CUENTAS_FINANCIERAS", "METODO_PAGO_CUENTAS", "MOVIMIENTO_FINANCIERO",
            "GASTOS", "INGRESOS", "LISTADO_PAGOS", "ACTIVOS_FIJOS",
        };

        private static readonly string[] Contabilidad =
        {
            "CONTABILIDAD",
            "CONTABILIDAD_CONFIGURACION_INTEGRACION",
        };

        private static readonly string[] Produccion =
        {
            "CENTRO_PRODUCCION",
            "PRODUCCION_CONFIG",
            "PRODUCCION_GESTIONAR",
            "PRODUCCION_PRIORIDAD",
            "PRODUCCION_CANCELAR",
        };

        private static readonly string[] ComisionesServicio =
        {
            "AREAS", "EMPLEADOS_COMISION", "REPORTE_COMISIONES", "REPORTE_SERVICIOS",
        };

        private static readonly string[] SalonExtra =
        {
            "CITAS", "HORARIO_ESTILISTA", "CUMPLEANEROS", "HISTORIAL_SERVICIOS",
        };

        public static IReadOnlyList<VerticalPreset> All { get; } = new[]
        {
            new VerticalPreset
            {
                Codigo = "comida",
                Nombre = "Comida / restaurante",
                Descripcion = "POS + cocina/producci\u00f3n. Ref: La Leyenda Hotdog.",
                CodigosModulo = Merge(Nucleo, Caja, Produccion, Tesoreria, Contabilidad),
            },
            new VerticalPreset
            {
                Codigo = "reposteria",
                Nombre = "Reposter\u00eda / pasteler\u00eda",
                Descripcion = "POS + bizcocho por encargo + producci\u00f3n. Ref: De Laura Pasteler\u00eda.",
                CodigosModulo = Merge(Nucleo, Caja, Inventario, Compras, Tesoreria, Contabilidad, Produccion,
                    new[] { "BIZCOCHO_ENCARGO", "CONDUCES" }),
            },
            new VerticalPreset
            {
                Codigo = "carwash",
                Nombre = "Carwash / lavadero",
                Descripcion = "Servicios + comisiones + consumo lavadores. Ref: TotalClean.",
                CodigosModulo = Merge(Nucleo, Caja, Tesoreria, Contabilidad, ComisionesServicio,
                    new[] { "CONSUMO_LAVADORES", "CITAS", "CUMPLEANEROS" }),
            },
            new VerticalPreset
            {
                Codigo = "salon",
                Nombre = "Sal\u00f3n de belleza",
                Descripcion = "Citas, \u00e1reas, comisiones y CRM ligero.",
                CodigosModulo = Merge(Nucleo, Caja, Tesoreria, Contabilidad, ComisionesServicio, SalonExtra),
            },
            new VerticalPreset
            {
                Codigo = "ferreteria",
                Nombre = "Ferreter\u00eda",
                Descripcion = "Inventario + compras + CxC/CxP para mostrador.",
                CodigosModulo = Merge(Nucleo, Caja, Inventario, Compras, Tesoreria, Contabilidad,
                    new[] { "CONDUCES" }),
            },
            new VerticalPreset
            {
                Codigo = "repuestos",
                Nombre = "Repuestos / auto parts",
                Descripcion = "Inventario denso + compras + conduce de entrega.",
                CodigosModulo = Merge(Nucleo, Caja, Inventario, Compras, Tesoreria, Contabilidad,
                    new[] { "CONDUCES" }),
            },
        };

        public static VerticalPreset? Get(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            return All.FirstOrDefault(p =>
                p.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string[] Merge(params IEnumerable<string>[] blocks)
        {
            return blocks
                .SelectMany(b => b)
                .Where(c => !string.IsNullOrWhiteSpace(c) && !CodigosInternosNunca.Contains(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
