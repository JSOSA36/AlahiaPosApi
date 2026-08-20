using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Demo/alta genérica: clona módulos de un perfil de referencia
    /// (o usa códigos explícitos), excluyendo módulos internos MacroBits.
    /// Verticales (salón, carwash, etc.) solo entran si se piden en códigos.
    /// </summary>
    public class DemoEmpresaBootstrapService : IDemoEmpresaBootstrap
    {
        /// <summary>Compat: solo internos. Preferir DemoVerticalPresets.CodigosInternosNunca.</summary>
        internal static readonly HashSet<string> CodigosExcluidos = DemoVerticalPresets.CodigosInternosNunca;

        /// <summary>
        /// Verticales / add-ons: no se copian del perfil guía; sí si el alta los pide.
        /// </summary>
        private static readonly HashSet<string> CodigosVerticalesSoloExplicitos = new(StringComparer.OrdinalIgnoreCase)
        {
            "AREAS",
            "CITAS",
            "HORARIO_ESTILISTA",
            "EMPLEADOS_COMISION",
            "DOCUMENTOS_CLINICOS",
            "FICHA_CLINICA",
            "HISTORIAL_SERVICIOS",
            "BIZCOCHO_ENCARGO",
            "CONSUMO_LAVADORES",
            "CENTRO_PRODUCCION",
            "PRODUCCION_CANCELAR",
            "PRODUCCION_CONFIG",
            "PRODUCCION_GESTIONAR",
            "PRODUCCION_PRIORIDAD",
        };

        private readonly AlahiaPosContext _db;
        private readonly IModulo _modulos;
        private readonly IEmpresaModulos _empresaModulos;
        private readonly IPerfiles _perfiles;
        private readonly IContabilidadConfiguracionService _contabilidadConfig;

        public DemoEmpresaBootstrapService(
            AlahiaPosContext db,
            IModulo modulos,
            IEmpresaModulos empresaModulos,
            IPerfiles perfiles,
            IContabilidadConfiguracionService contabilidadConfig)
        {
            _db = db;
            _modulos = modulos;
            _empresaModulos = empresaModulos;
            _perfiles = perfiles;
            _contabilidadConfig = contabilidadConfig;
        }

        public async Task<int> ConfigurarAsync(int idEmpresa, IEnumerable<string>? codigos = null)
        {
            var catalogo = (await _modulos.GetAllModulos())
                .Where(m => m.Activo && !string.IsNullOrWhiteSpace(m.Codigo))
                .GroupBy(m => m.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var tieneCodigosExplicitos = codigos != null && codigos.Any(c => !string.IsNullOrWhiteSpace(c));
            var origen = tieneCodigosExplicitos
                ? codigos!.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList()
                : await ObtenerCodigosPerfilReferenciaAsync();

            var elegidos = new List<Modulo>();
            foreach (var codigo in origen)
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    continue;
                var code = codigo.Trim();
                if (DemoVerticalPresets.CodigosInternosNunca.Contains(code))
                    continue;
                if (!tieneCodigosExplicitos && CodigosVerticalesSoloExplicitos.Contains(code))
                    continue;
                if (catalogo.TryGetValue(code, out var modulo))
                    elegidos.Add(modulo);
            }

            // Asegura bloque contabilidad aunque el perfil guía / checklist haya omitido algo
            foreach (var m in catalogo.Values.Where(x =>
                         x.Codigo.Equals("CONTABILIDAD", StringComparison.OrdinalIgnoreCase)
                         || x.Codigo.StartsWith("CONTABILIDAD_", StringComparison.OrdinalIgnoreCase)))
            {
                if (elegidos.All(e => e.Id != m.Id))
                    elegidos.Add(m);
            }

            if (elegidos.Any(m => m.Codigo.Equals("DOCUMENTOS_CLINICOS", StringComparison.OrdinalIgnoreCase))
                && catalogo.TryGetValue("FICHA_CLINICA", out var fichaClinica)
                && elegidos.All(e => e.Id != fichaClinica.Id))
            {
                elegidos.Add(fichaClinica);
            }

            elegidos = elegidos
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            if (elegidos.Count == 0)
                throw new InvalidOperationException("No hay módulos para configurar la empresa.");

            foreach (var modulo in elegidos)
            {
                await _empresaModulos.InsertEmpresaModulo(new EmpresaModulo
                {
                    EmpresaId = idEmpresa,
                    ModuloId = modulo.Id,
                    Activo = true,
                    FechaActivacion = DateTime.Now
                });

                await _contabilidadConfig.InicializarSiEsModuloContabilidadAsync(
                    idEmpresa, modulo.Id, activo: true);
            }

            return await _perfiles.CrearPerfilCompleto(new PerfilCreateDto
            {
                IdEmpresa = idEmpresa,
                Nombre = "Administrador",
                Descripcion = "Perfil administrador",
                Activo = true,
                Modulos = elegidos.Select(m => m.Id).ToList()
            });
        }

        private async Task<List<string>> ObtenerCodigosPerfilReferenciaAsync()
        {
            var perfilesConContabilidad = await (
                from pr in _db.PerfilRoles.AsNoTracking()
                join m in _db.Modulos.AsNoTracking() on pr.IdModulo equals m.Id
                where pr.Activo
                      && m.Activo
                      && (m.Codigo == "CONTABILIDAD" || m.Codigo.StartsWith("CONTABILIDAD_"))
                select pr.IdPerfil
            ).Distinct().ToListAsync();

            if (perfilesConContabilidad.Count == 0)
            {
                return (await _modulos.GetAllModulos())
                    .Where(m => m.Activo && !string.IsNullOrWhiteSpace(m.Codigo))
                    .Select(m => m.Codigo.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var perfilId = await (
                from pr in _db.PerfilRoles.AsNoTracking()
                where pr.Activo && perfilesConContabilidad.Contains(pr.IdPerfil)
                group pr by pr.IdPerfil into g
                orderby g.Count() descending
                select g.Key
            ).FirstOrDefaultAsync();

            return await (
                from pr in _db.PerfilRoles.AsNoTracking()
                join m in _db.Modulos.AsNoTracking() on pr.IdModulo equals m.Id
                where pr.IdPerfil == perfilId && pr.Activo && m.Activo
                select m.Codigo
            ).Distinct().ToListAsync();
        }
    }
}
