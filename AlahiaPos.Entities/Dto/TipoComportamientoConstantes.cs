using AlahiaPos.Entities.Domain;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Comportamiento ERP en compras/contabilidad. Independiente de EsServicio (naturaleza comercial).
    /// </summary>
    public static class TipoComportamientoConstantes
    {
        public const string Inventario = "Inventario";
        public const string Servicio = "Servicio";
        public const string Gasto = "Gasto";
        public const string ActivoFijo = "ActivoFijo";

        public static readonly string[] Todos =
        {
            Inventario,
            Servicio,
            Gasto,
            ActivoFijo
        };

        public static string Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return Inventario;

            var v = valor.Trim();
            foreach (var tipo in Todos)
            {
                if (string.Equals(tipo, v, StringComparison.OrdinalIgnoreCase))
                    return tipo;
            }

            return Inventario;
        }

        /// <summary>
        /// Resuelve comportamiento de compra. No usa EsServicio (naturaleza comercial).
        /// </summary>
        public static string ResolverComportamientoCompra(Productos producto)
        {
            if (!string.IsNullOrWhiteSpace(producto.TipoComportamiento))
                return Normalizar(producto.TipoComportamiento);

            if (producto.ControlarStock)
                return Inventario;

            return Gasto;
        }

        /// <summary>
        /// Persiste solo TipoComportamiento y flags de compra/stock operativo. No modifica EsServicio.
        /// </summary>
        public static void AplicarComportamientoErp(Productos producto, string? tipoComportamiento)
        {
            var tipo = Normalizar(tipoComportamiento ?? producto.TipoComportamiento);
            producto.TipoComportamiento = tipo;
            producto.SeCompra = true;

            switch (tipo)
            {
                case Inventario:
                    producto.ControlarStock = true;
                    break;
                case Gasto:
                case Servicio:
                case ActivoFijo:
                    // No forzar ControlarStock=false si el artículo controla stock en ventas.
                    break;
            }
        }

        public static bool RequiereAlmacen(string? tipoComportamiento)
            => Normalizar(tipoComportamiento) == Inventario;

        /// <summary>
        /// Líneas que deben pasar por recepción física en Almacén (no al confirmar compra).
        /// </summary>
        public static bool RequiereRecepcionFisica(string? tipoComportamiento)
        {
            var tipo = Normalizar(tipoComportamiento);
            return tipo == Inventario || tipo == ActivoFijo;
        }
    }
}
