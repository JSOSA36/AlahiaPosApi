using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Copia parámetros y secuencias de documento desde una empresa plantilla
    /// (p.ej. IdEmpresa=60) para que el cliente pueda facturar desde el día 1.
    /// </summary>
    public class EmpresaOperativaSeedService : IEmpresaOperativaSeed
    {
        private readonly AlahiaPosContext _db;
        private readonly ILogger<EmpresaOperativaSeedService> _logger;
        private readonly ISucursalService _sucursales;

        public EmpresaOperativaSeedService(
            AlahiaPosContext db,
            ILogger<EmpresaOperativaSeedService> logger,
            ISucursalService sucursales)
        {
            _db = db;
            _logger = logger;
            _sucursales = sucursales;
        }

        public async Task SeedDesdePlantillaAsync(int idEmpresaNueva, int idEmpresaPlantilla = 60)
        {
            if (idEmpresaNueva <= 0) throw new ArgumentOutOfRangeException(nameof(idEmpresaNueva));

            // Si la plantilla no existe, usar la primera empresa no-sistema con secuencias
            var plantillaOk = await _db.Empresas.AsNoTracking()
                .AnyAsync(e => e.IdEmpresa == idEmpresaPlantilla);
            if (!plantillaOk)
            {
                idEmpresaPlantilla = await _db.SecuenciaDocumentos.AsNoTracking()
                    .Join(_db.Empresas.AsNoTracking(), s => s.IdEmpresa, e => e.IdEmpresa, (s, e) => e)
                    .Where(e => !e.EsEmpresaSistema)
                    .Select(e => e.IdEmpresa)
                    .FirstOrDefaultAsync();
                if (idEmpresaPlantilla <= 0)
                    throw new InvalidOperationException("No hay empresa plantilla para clonar parámetros/secuencias.");
            }

            await ClonarParametrosAsync(idEmpresaNueva, idEmpresaPlantilla);
            await ClonarSecuenciasAsync(idEmpresaNueva, idEmpresaPlantilla);
            await AsegurarAlmacenPrincipalAsync(idEmpresaNueva);
            await _sucursales.AsegurarPrincipalAsync(idEmpresaNueva);
            await AsegurarClienteAlPortadorAsync(idEmpresaNueva);

            _logger.LogInformation(
                "Seed operativo empresa {Nueva} desde plantilla {Plantilla}",
                idEmpresaNueva, idEmpresaPlantilla);
        }

        private async Task ClonarParametrosAsync(int idEmpresaNueva, int idEmpresaPlantilla)
        {
            var yaTiene = await _db.Parametros.AsNoTracking()
                .AnyAsync(p => p.IdEmpresa == idEmpresaNueva);
            if (yaTiene) return;

            var origen = await _db.Parametros.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresaPlantilla && p.Activo)
                .ToListAsync();

            if (origen.Count == 0) return;

            foreach (var p in origen)
            {
                var valor = p.Valor;
                // Nueva empresa: FE y preview POS apagados hasta que se configuren
                if (string.Equals(p.Clave, "FACTURACION_ELECTRONICA", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Clave, "PREVIEW_DGII", StringComparison.OrdinalIgnoreCase))
                    valor = "false";
                // Cierre por billetes por defecto. Solo Sena usa el simplificado.
                else if (string.Equals(p.Clave, "ControlEfectivoPorDenominacion", StringComparison.OrdinalIgnoreCase))
                    valor = "true";

                _db.Parametros.Add(new Parametros
                {
                    IdEmpresa = idEmpresaNueva,
                    Tipo = p.Tipo,
                    CodigoPOS = p.CodigoPOS,
                    Clave = p.Clave,
                    Valor = valor,
                    Descripcion = p.Descripcion,
                    FechaCreacion = DateTime.Now,
                    Activo = true
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task ClonarSecuenciasAsync(int idEmpresaNueva, int idEmpresaPlantilla)
        {
            var yaTiene = await _db.SecuenciaDocumentos.AsNoTracking()
                .AnyAsync(s => s.IdEmpresa == idEmpresaNueva);
            if (yaTiene) return;

            var origen = await _db.SecuenciaDocumentos.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresaPlantilla)
                .ToListAsync();

            if (origen.Count == 0)
            {
                // Fallback mínimo (plantilla sin secuencias): tipos estándar del ERP
                var prefijos = new Dictionary<int, string>
                {
                    [1] = "Fact-0000", [2] = "COT-0000", [3] = "PE-0000", [4] = "CO-0000",
                    [5] = "ORD-0000", [6] = "DS-0000", [7] = "NC-0000", [8] = "ND-0000",
                    [9] = "CA-0000", [10] = "PFACT-0000", [11] = "FACTC-0000",
                    [12] = "DC-0000", [14] = "BZ-0000"
                };

                foreach (var (idTipo, prefijo) in prefijos)
                {
                    _db.SecuenciaDocumentos.Add(new SecuenciaDocumentos
                    {
                        IdEmpresa = idEmpresaNueva,
                        IdTipoDocumento = idTipo,
                        Prefijo = prefijo,
                        SecuenciaInicial = 0,
                        SecuenciaActual = 0,
                        FechaInseccion = DateTime.Now
                    });
                }
            }
            else
            {
                foreach (var s in origen)
                {
                    _db.SecuenciaDocumentos.Add(new SecuenciaDocumentos
                    {
                        IdEmpresa = idEmpresaNueva,
                        IdTipoDocumento = s.IdTipoDocumento,
                        Prefijo = s.Prefijo,
                        SecuenciaInicial = s.SecuenciaInicial,
                        // Empieza en cero: la primera factura será ...0001
                        SecuenciaActual = 0,
                        FechaInseccion = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task AsegurarAlmacenPrincipalAsync(int idEmpresa)
        {
            var existe = await _db.Almacenes.AsNoTracking()
                .AnyAsync(a => a.IdEmpresa == idEmpresa);
            if (existe) return;

            _db.Almacenes.Add(new Almacen
            {
                Nombre = "Principal",
                Descripcion = "Almacén principal",
                IdEmpresa = idEmpresa,
                EsPrincipal = true,
                Activo = true,
                FechaCreacion = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        private async Task AsegurarClienteAlPortadorAsync(int idEmpresa)
        {
            var existe = await _db.Clientes.AsNoTracking()
                .AnyAsync(c => c.IdEmpresa == idEmpresa
                    && c.NombreComercial == "Al Portador");
            if (existe) return;

            _db.Clientes.Add(new Clientes
            {
                NombreComercial = "Al Portador",
                Estado = true,
                LimiteCredito = 0,
                IdEmpresa = idEmpresa,
                FechaInseccion = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }
}
