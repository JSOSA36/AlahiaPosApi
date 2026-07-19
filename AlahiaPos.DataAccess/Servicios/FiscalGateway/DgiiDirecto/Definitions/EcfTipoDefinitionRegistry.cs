namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Registro de definiciones e-CF. Extender aquí al agregar un tipo nuevo.</summary>
    public static class EcfTipoDefinitionRegistry
    {
        private static readonly IReadOnlyDictionary<int, IEcfTipoDefinition> PorTipo =
            new Dictionary<int, IEcfTipoDefinition>
            {
                [31] = new Ecf31Definition(),
                [32] = new Ecf32Definition(),
                [33] = new Ecf33Definition(),
                [34] = new Ecf34Definition(),
                [41] = new Ecf41Definition(),
                [43] = new Ecf43Definition(),
                [44] = new Ecf44Definition(),
                [45] = new Ecf45Definition(),
                [46] = new Ecf46Definition(),
                [47] = new Ecf47Definition(),
            };

        public static IEcfTipoDefinition Get(int tipoeCF)
        {
            if (PorTipo.TryGetValue(tipoeCF, out var def))
                return def;
            throw new NotSupportedException(
                $"No hay definición e-CF registrada para TipoeCF={tipoeCF}. " +
                $"Tipos disponibles: {string.Join(", ", PorTipo.Keys.OrderBy(k => k))}.");
        }

        public static bool TryGet(int tipoeCF, out IEcfTipoDefinition? def)
            => PorTipo.TryGetValue(tipoeCF, out def);

        public static IReadOnlyCollection<IEcfTipoDefinition> Todas => PorTipo.Values.ToList();
    }
}
