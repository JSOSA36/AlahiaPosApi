using AlahiaPos.DataAccess.Data;
using AlahiaPos.DGII.Sync.Interfaces;
using AlahiaPos.Entities.Domain;
using CsvHelper;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace AlahiaPos.DGII.Sync.Servicios
{
    public class DgiiProcessor : IDgiiProcessor
    {
        private readonly AlahiaPosContext _context;
        private readonly ILogger<DgiiProcessor> _logger;

        public DgiiProcessor(AlahiaPosContext context, ILogger<DgiiProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }



        public async Task ProcesarArchivoAsync(string carpeta, CancellationToken cancellationToken = default)
        {
            try
            {
                var archivo = Directory.GetFiles(carpeta, "*.csv").FirstOrDefault();

                if (archivo == null)
                    throw new Exception("No se encontró archivo CSV");

                _logger.LogInformation("Procesando archivo DGII con CsvHelper...");

                // 🔥 traer RNC existentes
                var lista = await _context.ClientesDGII
                    .Select(c => c.RNC)
                    .ToListAsync(cancellationToken);

                var existentes = lista.ToHashSet();

                var nuevos = new List<ClientesDGII>();

                using var reader = new StreamReader(archivo);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                // 🔥 leer encabezado automáticamente
                await csv.ReadAsync();
                csv.ReadHeader();

                int procesados = 0;
                int insertados = 0;

                while (await csv.ReadAsync())
                {
                    try
                    {
                        var rnc = csv.GetField(0)?.Trim();

                        if (string.IsNullOrEmpty(rnc))
                            continue;

                        if (existentes.Contains(rnc))
                            continue;

                        var razon = csv.GetField(1);
                        var estado = csv.GetField(4);
                        var regimen = csv.GetField(5);

                        nuevos.Add(new ClientesDGII
                        {
                            RNC = rnc,
                            RazonSocial = razon,
                            NombreComercial = razon, // DGII no trae comercial real
                            Estado = estado,
                            Regimen = regimen,
                            FechaActualizacion = DateTime.Now
                        });

                        existentes.Add(rnc);
                        insertados++;
                        procesados++;

                        // 🔥 guardar por bloques
                        if (nuevos.Count >= 5000)
                        {
                            await _context.ClientesDGII.AddRangeAsync(nuevos, cancellationToken);
                            await _context.SaveChangesAsync(cancellationToken);

                            nuevos.Clear();

                            _logger.LogInformation($"Insertados parciales: {insertados}");
                        }
                    }
                    catch (Exception exLine)
                    {
                        _logger.LogWarning(exLine, "Error en una línea del CSV, se omite");
                    }
                }

                // 🔥 guardar restante
                if (nuevos.Any())
                {
                    await _context.ClientesDGII.AddRangeAsync(nuevos, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation($"Proceso completado. Procesados: {procesados}, Insertados: {insertados}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando archivo DGII");
                throw;
            }
        }
    }
}
