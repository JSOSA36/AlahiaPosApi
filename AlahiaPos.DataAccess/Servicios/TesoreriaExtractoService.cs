using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.ExtractosBancarios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class TesoreriaExtractoService : ITesoreriaExtractoService
    {
        private const long MaxFileBytes = 8 * 1024 * 1024;
        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase) { ".csv", ".txt", ".xlsx", ".xls", ".pdf" };

        private static readonly string[] CategoriasBancariasContables =
        {
            "COMISION_BANCARIA", "CARGO_BANCARIO", "IMPUESTO_BANCARIO",
            "INTERES_BANCARIO", "DEBITO_AUTOMATICO", "CREDITO_AUTOMATICO",
            "AJUSTE_BANCARIO", "REVERSO_BANCARIO", "OTRO_BANCARIO"
        };

        private readonly AlahiaPosContext _context;
        private readonly IMovimientoFinancieroService _movimientoService;
        private readonly IBankStatementParserOrchestrator _parserOrchestrator;
        private readonly IGastos _gastosService;
        private readonly IIngresos _ingresosService;

        public TesoreriaExtractoService(
            AlahiaPosContext context,
            IMovimientoFinancieroService movimientoService,
            IBankStatementParserOrchestrator parserOrchestrator,
            IGastos gastosService,
            IIngresos ingresosService)
        {
            _context = context;
            _movimientoService = movimientoService;
            _parserOrchestrator = parserOrchestrator;
            _gastosService = gastosService;
            _ingresosService = ingresosService;
        }

        public Task<TesoreriaExtractoImport> ImportarCsvAsync(ImportarExtractoDto dto)
            => ImportarAsync(dto);

        public async Task<TesoreriaExtractoImport> ImportarAsync(ImportarExtractoDto dto)
        {
            ValidarImportacion(dto);

            var cuenta = await _context.CuentaFinanciera.FirstOrDefaultAsync(c =>
                c.IdCuentaFinanciera == dto.IdCuentaFinanciera
                && c.IdEmpresa == dto.IdEmpresa)
                ?? throw new InvalidOperationException("Cuenta financiera no encontrada.");

            var hash = dto.HashArchivo;
            if (string.IsNullOrWhiteSpace(hash) && dto.ContenidoArchivo is { Length: > 0 })
                hash = ComputeSha256(dto.ContenidoArchivo);
            else if (string.IsNullOrWhiteSpace(hash) && !string.IsNullOrWhiteSpace(dto.ContenidoCsv))
                hash = ComputeSha256(Encoding.UTF8.GetBytes(dto.ContenidoCsv));
            else if (string.IsNullOrWhiteSpace(hash) && !string.IsNullOrWhiteSpace(dto.TextoExtraido))
                hash = ComputeSha256(Encoding.UTF8.GetBytes(dto.TextoExtraido));

            if (!string.IsNullOrWhiteSpace(hash))
            {
                var dup = await _context.TesoreriaExtractoImport.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IdEmpresa == dto.IdEmpresa
                    && x.IdCuentaFinanciera == dto.IdCuentaFinanciera
                    && x.HashArchivo == hash
                    && x.Estado != "ANULADO");

                if (dup != null)
                {
                    if (dup.Estado == "PREVIEW")
                        throw new InvalidOperationException(
                            $"Existe un preview pendiente con el mismo archivo (#{dup.IdTesoreriaExtractoImport}). " +
                            "Confírmelo o descártelo antes de importar directamente.");

                    throw new InvalidOperationException(
                        $"Ya existe un extracto importado con el mismo archivo (#{dup.IdTesoreriaExtractoImport}).");
                }
            }

            var parsed = ParseContenido(dto);
            if (parsed.Lineas.Count == 0)
                throw new InvalidOperationException("No se encontraron movimientos válidos en el estado de cuenta.");

            // Evitar reimportar mismo banco/cuenta/período (los PREVIEW no bloquean:
            // se validan al confirmar).
            if (parsed.PeriodoDesde.HasValue && parsed.PeriodoHasta.HasValue
                && !string.IsNullOrWhiteSpace(parsed.NumeroCuentaBanco))
            {
                var mismoPeriodo = await _context.TesoreriaExtractoImport.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IdEmpresa == dto.IdEmpresa
                    && x.IdCuentaFinanciera == dto.IdCuentaFinanciera
                    && x.NumeroCuentaBanco == parsed.NumeroCuentaBanco
                    && x.PeriodoDesde == parsed.PeriodoDesde
                    && x.PeriodoHasta == parsed.PeriodoHasta
                    && x.Estado != "ANULADO"
                    && x.Estado != "PREVIEW");

                if (mismoPeriodo != null)
                    throw new InvalidOperationException(
                        $"Ya existe un extracto para esa cuenta/período (#{mismoPeriodo.IdTesoreriaExtractoImport}).");
            }

            var import = new TesoreriaExtractoImport
            {
                IdEmpresa = dto.IdEmpresa,
                IdCuentaFinanciera = dto.IdCuentaFinanciera,
                NombreArchivo = SanitizeFileName(dto.NombreArchivo),
                Formato = ResolveFormato(dto),
                IdUsuario = dto.IdUsuario,
                Estado = "CARGADO",
                FechaCarga = DateTime.UtcNow,
                Banco = parsed.Banco,
                NumeroCuentaBanco = parsed.NumeroCuentaBanco,
                Moneda = parsed.Moneda ?? cuenta.Moneda ?? "DOP",
                PeriodoDesde = parsed.PeriodoDesde ?? parsed.Lineas.Min(x => x.Fecha),
                PeriodoHasta = parsed.PeriodoHasta ?? parsed.Lineas.Max(x => x.Fecha),
                SaldoInicial = parsed.SaldoInicial,
                SaldoFinal = parsed.SaldoFinal,
                TotalDebitos = parsed.TotalDebitos > 0
                    ? parsed.TotalDebitos
                    : parsed.Lineas.Sum(x => x.Debito),
                TotalCreditos = parsed.TotalCreditos > 0
                    ? parsed.TotalCreditos
                    : parsed.Lineas.Sum(x => x.Credito),
                HashArchivo = hash,
                AdapterUsado = parsed.AdapterUsado,
                ParserWarnings = SerializeWarnings(parsed.Warnings)
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.TesoreriaExtractoImport.Add(import);
            await _context.SaveChangesAsync();

            foreach (var row in parsed.Lineas)
            {
                _context.TesoreriaExtractoLinea.Add(new TesoreriaExtractoLinea
                {
                    IdTesoreriaExtractoImport = import.IdTesoreriaExtractoImport,
                    FechaMovimiento = row.Fecha,
                    Descripcion = Truncate(row.Descripcion, 250),
                    Referencia = Truncate(row.Referencia, 100),
                    Debito = row.Debito,
                    Credito = row.Credito,
                    Balance = row.Balance,
                    EstadoMatch = "PENDIENTE",
                    CategoriaSugerida = ClasificarCategoria(row.Descripcion, row.Referencia, row.Debito > 0)
                });
            }

            await _context.SaveChangesAsync();
            await SugerirMatchesInternoAsync(
                import.IdTesoreriaExtractoImport,
                dto.IdEmpresa,
                dto.IdCuentaFinanciera,
                dto.ToleranciaDiasMatch <= 0 ? 3 : dto.ToleranciaDiasMatch);

            import.Estado = "PROCESADO";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return import;
        }

        public async Task<ExtractoPreviewDto> PreviewAsync(ImportarExtractoDto dto)
        {
            ValidarImportacion(dto);

            var cuenta = await _context.CuentaFinanciera.FirstOrDefaultAsync(c =>
                c.IdCuentaFinanciera == dto.IdCuentaFinanciera
                && c.IdEmpresa == dto.IdEmpresa)
                ?? throw new InvalidOperationException("Cuenta financiera no encontrada.");

            var hash = ResolveHash(dto);

            // Dedupe de preview: permite reintentos. Si ya hay PREVIEW con el mismo hash,
            // lo regeneramos. Los definitivos (PROCESADO/CERRADO/CARGADO) sí bloquean.
            if (!string.IsNullOrWhiteSpace(hash))
            {
                var existentes = await _context.TesoreriaExtractoImport
                    .AsTracking()
                    .Where(x =>
                        x.IdEmpresa == dto.IdEmpresa
                        && x.IdCuentaFinanciera == dto.IdCuentaFinanciera
                        && x.HashArchivo == hash
                        && x.Estado != "ANULADO")
                    .ToListAsync();

                var definitivo = existentes.FirstOrDefault(x => x.Estado != "PREVIEW");
                if (definitivo != null)
                    throw new InvalidOperationException(
                        $"Ya existe un extracto definitivo con el mismo archivo (#{definitivo.IdTesoreriaExtractoImport}, estado {definitivo.Estado}).");

                foreach (var prev in existentes.Where(x => x.Estado == "PREVIEW"))
                {
                    // Anular en lugar de borrar: conserva rastro y libera el hash.
                    prev.Estado = "ANULADO";
                    prev.Observacion = Truncate(
                        $"Preview regenerado el {DateTime.UtcNow:u}.",
                        500);
                    var lineasPrevias = await _context.TesoreriaExtractoLinea
                        .Where(l => l.IdTesoreriaExtractoImport == prev.IdTesoreriaExtractoImport)
                        .ToListAsync();
                    _context.TesoreriaExtractoLinea.RemoveRange(lineasPrevias);
                }

                if (existentes.Count > 0)
                    await _context.SaveChangesAsync();
            }

            var parsed = ParseContenido(dto);
            if (parsed.Lineas.Count == 0)
                throw new InvalidOperationException("No se encontraron movimientos válidos en el estado de cuenta.");

            var import = new TesoreriaExtractoImport
            {
                IdEmpresa = dto.IdEmpresa,
                IdCuentaFinanciera = dto.IdCuentaFinanciera,
                NombreArchivo = SanitizeFileName(dto.NombreArchivo),
                Formato = ResolveFormato(dto),
                IdUsuario = dto.IdUsuario,
                Estado = "PREVIEW",
                FechaCarga = DateTime.UtcNow,
                Banco = parsed.Banco,
                NumeroCuentaBanco = parsed.NumeroCuentaBanco,
                Moneda = parsed.Moneda ?? cuenta.Moneda ?? "DOP",
                PeriodoDesde = parsed.PeriodoDesde ?? parsed.Lineas.Min(x => x.Fecha),
                PeriodoHasta = parsed.PeriodoHasta ?? parsed.Lineas.Max(x => x.Fecha),
                SaldoInicial = parsed.SaldoInicial,
                SaldoFinal = parsed.SaldoFinal,
                TotalDebitos = parsed.TotalDebitos > 0
                    ? parsed.TotalDebitos
                    : parsed.Lineas.Sum(x => x.Debito),
                TotalCreditos = parsed.TotalCreditos > 0
                    ? parsed.TotalCreditos
                    : parsed.Lineas.Sum(x => x.Credito),
                HashArchivo = hash,
                AdapterUsado = parsed.AdapterUsado,
                ParserWarnings = SerializeWarnings(parsed.Warnings)
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.TesoreriaExtractoImport.Add(import);
            await _context.SaveChangesAsync();

            foreach (var row in parsed.Lineas)
            {
                _context.TesoreriaExtractoLinea.Add(new TesoreriaExtractoLinea
                {
                    IdTesoreriaExtractoImport = import.IdTesoreriaExtractoImport,
                    FechaMovimiento = row.Fecha,
                    Descripcion = Truncate(row.Descripcion, 250),
                    Referencia = Truncate(row.Referencia, 100),
                    Debito = row.Debito,
                    Credito = row.Credito,
                    Balance = row.Balance,
                    EstadoMatch = "PENDIENTE",
                    CategoriaSugerida = ClasificarCategoria(row.Descripcion, row.Referencia, row.Debito > 0)
                });
            }

            await _context.SaveChangesAsync();
            // PREVIEW: no matching, no mutar MovimientoFinanciero.
            await transaction.CommitAsync();
            return await MapToPreviewDtoAsync(import);
        }

        public async Task<ExtractoPreviewDto> GetPreviewAsync(int idTesoreriaExtractoImport, int idEmpresa)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureEsPreview(import);
            return await MapToPreviewDtoAsync(import);
        }

        public async Task<ExtractoPreviewDto> ActualizarLineasPreviewAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            ActualizarLineasPreviewDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException("Payload requerido.");

            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureEsPreview(import);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (dto.Banco != null)
                import.Banco = Truncate(dto.Banco, 120);
            if (dto.NumeroCuentaBanco != null)
                import.NumeroCuentaBanco = Truncate(dto.NumeroCuentaBanco, 80);
            if (dto.Moneda != null)
                import.Moneda = Truncate(dto.Moneda, 10);
            if (dto.PeriodoDesde.HasValue)
                import.PeriodoDesde = dto.PeriodoDesde;
            if (dto.PeriodoHasta.HasValue)
                import.PeriodoHasta = dto.PeriodoHasta;
            if (dto.SaldoInicial.HasValue)
                import.SaldoInicial = dto.SaldoInicial;
            if (dto.SaldoFinal.HasValue)
                import.SaldoFinal = dto.SaldoFinal;
            if (dto.Observacion != null)
                import.Observacion = Truncate(dto.Observacion, 500);

            if (dto.ReemplazarTodas != null)
            {
                var actuales = await _context.TesoreriaExtractoLinea
                    .Where(x => x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                    .ToListAsync();
                _context.TesoreriaExtractoLinea.RemoveRange(actuales);

                foreach (var row in dto.ReemplazarTodas)
                {
                    ValidarLineaPreviewInput(row);
                    _context.TesoreriaExtractoLinea.Add(MapNuevaLinea(import.IdTesoreriaExtractoImport, row));
                }
            }
            else
            {
                if (dto.EliminarIds is { Count: > 0 })
                {
                    var aEliminar = await _context.TesoreriaExtractoLinea
                        .Where(x =>
                            x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                            && dto.EliminarIds.Contains(x.IdTesoreriaExtractoLinea))
                        .ToListAsync();

                    if (aEliminar.Count != dto.EliminarIds.Distinct().Count())
                        throw new InvalidOperationException(
                            "Una o más líneas a eliminar no pertenecen a este preview.");

                    _context.TesoreriaExtractoLinea.RemoveRange(aEliminar);
                }

                if (dto.Editar is { Count: > 0 })
                {
                    foreach (var edit in dto.Editar)
                    {
                        ValidarLineaPreviewInput(edit);
                        var linea = await _context.TesoreriaExtractoLinea.AsTracking().FirstOrDefaultAsync(x =>
                            x.IdTesoreriaExtractoLinea == edit.IdTesoreriaExtractoLinea
                            && x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                            ?? throw new InvalidOperationException(
                                $"Línea #{edit.IdTesoreriaExtractoLinea} no pertenece a este preview.");

                        AplicarCamposLinea(linea, edit);
                    }
                }

                if (dto.Agregar is { Count: > 0 })
                {
                    foreach (var row in dto.Agregar)
                    {
                        ValidarLineaPreviewInput(row);
                        _context.TesoreriaExtractoLinea.Add(MapNuevaLinea(import.IdTesoreriaExtractoImport, row));
                    }
                }
            }

            await _context.SaveChangesAsync();
            await RecalcularTotalesImportAsync(import);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return await MapToPreviewDtoAsync(import);
        }

        public async Task<TesoreriaExtractoImport> ConfirmarPreviewAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            int idUsuario,
            int toleranciaDiasMatch = 3)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureEsPreview(import);

            var lineas = await _context.TesoreriaExtractoLinea
                .Where(x => x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                .ToListAsync();

            if (lineas.Count == 0)
                throw new InvalidOperationException("El preview no tiene líneas. Agregue movimientos antes de confirmar.");

            foreach (var linea in lineas)
            {
                if (linea.Debito < 0 || linea.Credito < 0)
                    throw new InvalidOperationException(
                        $"Línea #{linea.IdTesoreriaExtractoLinea}: débito y crédito no pueden ser negativos.");
                if (linea.Debito == 0 && linea.Credito == 0)
                    throw new InvalidOperationException(
                        $"Línea #{linea.IdTesoreriaExtractoLinea}: debe tener débito o crédito.");
                if (linea.Debito > 0 && linea.Credito > 0)
                    throw new InvalidOperationException(
                        $"Línea #{linea.IdTesoreriaExtractoLinea}: no puede tener débito y crédito a la vez.");
            }

            // Dedupe definitivo: no confirmar si ya hay otro extracto definitivo
            // con el mismo hash o el mismo banco/cuenta/período.
            if (!string.IsNullOrWhiteSpace(import.HashArchivo))
            {
                var dupHash = await _context.TesoreriaExtractoImport.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdCuentaFinanciera == import.IdCuentaFinanciera
                    && x.HashArchivo == import.HashArchivo
                    && x.IdTesoreriaExtractoImport != import.IdTesoreriaExtractoImport
                    && x.Estado != "ANULADO"
                    && x.Estado != "PREVIEW");

                if (dupHash != null)
                    throw new InvalidOperationException(
                        $"Ya existe un extracto definitivo con el mismo archivo (#{dupHash.IdTesoreriaExtractoImport}).");
            }

            if (import.PeriodoDesde.HasValue && import.PeriodoHasta.HasValue
                && !string.IsNullOrWhiteSpace(import.NumeroCuentaBanco))
            {
                var mismoPeriodo = await _context.TesoreriaExtractoImport.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdCuentaFinanciera == import.IdCuentaFinanciera
                    && x.NumeroCuentaBanco == import.NumeroCuentaBanco
                    && x.PeriodoDesde == import.PeriodoDesde
                    && x.PeriodoHasta == import.PeriodoHasta
                    && x.IdTesoreriaExtractoImport != import.IdTesoreriaExtractoImport
                    && x.Estado != "ANULADO"
                    && x.Estado != "PREVIEW");

                if (mismoPeriodo != null)
                    throw new InvalidOperationException(
                        $"Ya existe un extracto definitivo para esa cuenta/período (#{mismoPeriodo.IdTesoreriaExtractoImport}).");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await RecalcularTotalesImportAsync(import);

            // Confirmar primero (commit) y recién después matching.
            // Si el matching falla, el extracto ya quedó PROCESADO y no puede
            // regenerarse/borrarase por un nuevo Preview del mismo hash.
            import.Estado = "PROCESADO";
            import.Observacion = Truncate(
                $"Confirmado por usuario {idUsuario} el {DateTime.UtcNow:u} (desde PREVIEW).",
                500);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await SugerirMatchesInternoAsync(
                    import.IdTesoreriaExtractoImport,
                    idEmpresa,
                    import.IdCuentaFinanciera,
                    toleranciaDiasMatch <= 0 ? 3 : toleranciaDiasMatch);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                import.Observacion = Truncate(
                    $"{import.Observacion} | Matching pendiente: {ex.Message}",
                    500);
                await _context.SaveChangesAsync();
            }

            return import;
        }

        public async Task DescartarPreviewAsync(int idTesoreriaExtractoImport, int idEmpresa, int idUsuario)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureEsPreview(import);

            // ANULADO libera el hash para reintentos sin borrar el rastro.
            import.Estado = "ANULADO";
            import.Observacion = Truncate(
                $"Preview descartado por usuario {idUsuario} el {DateTime.UtcNow:u}.",
                500);

            // Limpiar líneas para no dejar basura de matching futuro; no hay MFs tocados.
            var lineas = await _context.TesoreriaExtractoLinea
                .Where(x => x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                .ToListAsync();
            _context.TesoreriaExtractoLinea.RemoveRange(lineas);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ExtractoLineaMatchDto>> SugerirMatchesAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureNoEsPreview(import);
            await SugerirMatchesInternoAsync(
                import.IdTesoreriaExtractoImport,
                idEmpresa,
                import.IdCuentaFinanciera,
                3);
            await _context.SaveChangesAsync();
            return await ConstruirLineasMatchAsync(import);
        }

        public async Task ConfirmarMatchAsync(ConfirmarExtractoMatchDto dto)
        {
            await ResolverLineaAsync(new ResolverExtractoLineaDto
            {
                IdTesoreriaExtractoLinea = dto.IdTesoreriaExtractoLinea,
                IdEmpresa = dto.IdEmpresa,
                IdUsuario = dto.IdUsuario,
                Accion = AccionExtractoPendiente.ASOCIAR,
                IdMovimientoFinanciero = dto.IdMovimientoFinanciero
            });
        }

        public async Task DescartarLineaAsync(int idTesoreriaExtractoLinea, int idEmpresa, int idUsuario)
        {
            await ResolverLineaAsync(new ResolverExtractoLineaDto
            {
                IdTesoreriaExtractoLinea = idTesoreriaExtractoLinea,
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Accion = AccionExtractoPendiente.IGNORAR,
                Motivo = "Descartado"
            });
        }

        public async Task<int> CrearMovimientoDesdeLineaAsync(CrearMovimientoDesdeExtractoDto dto)
        {
            var linea = await _context.TesoreriaExtractoLinea
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea)
                ?? throw new InvalidOperationException("Línea de extracto no encontrada.");

            var accion = linea.Debito > 0
                ? AccionExtractoPendiente.CREAR_GASTO
                : AccionExtractoPendiente.CREAR_INGRESO;

            var result = await ResolverLineaAsync(new ResolverExtractoLineaDto
            {
                IdTesoreriaExtractoLinea = dto.IdTesoreriaExtractoLinea,
                IdEmpresa = dto.IdEmpresa,
                IdUsuario = dto.IdUsuario,
                Accion = accion,
                Motivo = dto.Motivo
            });

            return result.IdMovimientoFinanciero ?? 0;
        }

        public async Task<ResolverExtractoLineaResultadoDto> ResolverLineaAsync(ResolverExtractoLineaDto dto)
        {
            var linea = await _context.TesoreriaExtractoLinea
                .AsTracking()
                .Include(x => x.ExtractoImport)
                .FirstOrDefaultAsync(x => x.IdTesoreriaExtractoLinea == dto.IdTesoreriaExtractoLinea)
                ?? throw new InvalidOperationException("Línea de extracto no encontrada.");

            var import = linea.ExtractoImport
                ?? throw new InvalidOperationException("Importación no encontrada.");

            if (import.IdEmpresa != dto.IdEmpresa)
                throw new InvalidOperationException("La línea no pertenece a la empresa.");

            EnsureNoEsPreview(import);

            if (linea.EstadoMatch is "CONFIRMADO" or "AUTO_CONCILIADO" or "NUEVO_MOV" or "IGNORADO" or "RESUELTO"
                && !string.IsNullOrWhiteSpace(linea.AccionTomada))
            {
                return new ResolverExtractoLineaResultadoDto
                {
                    IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                    Accion = dto.Accion,
                    IdMovimientoFinanciero = linea.IdMovimientoFinanciero,
                    Categoria = linea.CategoriaSugerida,
                    EntidadCreada = "MOVIMIENTO_FINANCIERO",
                    YaResuelta = true
                };
            }

            var categoria = string.IsNullOrWhiteSpace(dto.Categoria)
                ? (linea.CategoriaSugerida ?? ClasificarCategoria(linea.Descripcion, linea.Referencia, linea.Debito > 0))
                : dto.Categoria.Trim().ToUpperInvariant();

            // CREAR_GASTO / CREAR_INGRESO reutilizan módulos ERP (no exigen categoría bancaria pura).
            // CREAR_AJUSTE y demás sí validan catálogo bancario.
            var esCreacionModulo = dto.Accion is AccionExtractoPendiente.CREAR_GASTO
                or AccionExtractoPendiente.CREAR_INGRESO;
            if (!esCreacionModulo
                && !CategoriasBancariasContables.Contains(categoria, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Categoría bancaria no soportada: {categoria}.");

            var motivo = string.IsNullOrWhiteSpace(dto.Motivo)
                ? (linea.Descripcion ?? categoria)
                : dto.Motivo.Trim();

            int? idMov = null;
            var entidad = "NINGUNA";

            switch (dto.Accion)
            {
                case AccionExtractoPendiente.IGNORAR:
                    if (string.IsNullOrWhiteSpace(dto.Motivo))
                        throw new InvalidOperationException("Ignorar una línea requiere un motivo.");
                    linea.EstadoMatch = "IGNORADO";
                    linea.AccionTomada = "IGNORAR";
                    linea.ExplicacionMatch = dto.Motivo.Trim();
                    break;

                case AccionExtractoPendiente.ASOCIAR:
                    idMov = dto.IdMovimientoFinanciero ?? linea.IdMovimientoFinanciero;
                    if (idMov is not > 0)
                        throw new InvalidOperationException("Debe indicar el movimiento a asociar.");

                    await AsociarMovimientoAsync(linea, idMov.Value, dto.IdEmpresa, dto.IdUsuario, auto: false);
                    linea.AccionTomada = "ASOCIAR";
                    entidad = "MOVIMIENTO_FINANCIERO";
                    break;

                case AccionExtractoPendiente.CREAR_GASTO:
                    if (linea.Debito <= 0)
                        throw new InvalidOperationException("CREAR_GASTO requiere una línea débito.");
                    {
                        var clave = $"EXT_{import.IdTesoreriaExtractoImport}_{linea.IdTesoreriaExtractoLinea}";
                        var tipoGasto = MapTipoGastoDesdeConciliacion(motivo, categoria);
                        var gastoRes = await _gastosService.RegistrarGastoCompletoAsync(new RegistrarGastoRequest
                        {
                            IdEmpresa = dto.IdEmpresa,
                            IdUsuario = dto.IdUsuario,
                            Monto = linea.Debito,
                            TipoGasto = tipoGasto,
                            Detalle = $"{motivo} | Extracto #{import.IdTesoreriaExtractoImport} línea #{linea.IdTesoreriaExtractoLinea}",
                            FormaPago = "TRANSFERENCIA",
                            IdCuentaFinanciera = import.IdCuentaFinanciera,
                            Referencia = string.IsNullOrWhiteSpace(linea.Referencia)
                                ? $"EXT-{linea.IdTesoreriaExtractoLinea}"
                                : linea.Referencia,
                            OrigenModulo = "CONCILIACION",
                            Fecha = linea.FechaMovimiento,
                            TipoComprobante = GastoComprobanteTipos.SinComprobante,
                            DesdeExtractoBancario = true,
                            IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                            IdTesoreriaConciliacion = import.IdTesoreriaConciliacion,
                            ClaveIdempotencia = clave,
                            FechaMovimiento = linea.FechaMovimiento
                        });
                        idMov = gastoRes.IdMovimientoFinanciero;
                        if (idMov is not > 0)
                            throw new InvalidOperationException("No se pudo crear el movimiento del gasto desde conciliación.");
                        linea.IdMovimientoFinanciero = idMov;
                        linea.EstadoMatch = "NUEVO_MOV";
                        linea.AccionTomada = "CREAR_GASTO";
                        linea.CategoriaSugerida = categoria;
                        linea.ExplicacionMatch = $"Gasto #{gastoRes.IdGasto} desde Conciliación Bancaria.";
                        entidad = "GASTO";
                    }
                    break;

                case AccionExtractoPendiente.CREAR_INGRESO:
                    if (linea.Credito <= 0)
                        throw new InvalidOperationException("CREAR_INGRESO requiere una línea crédito.");
                    {
                        var clave = $"EXT_{import.IdTesoreriaExtractoImport}_{linea.IdTesoreriaExtractoLinea}";
                        var catIngreso = MapCategoriaIngresoDesdeConciliacion(motivo, categoria);
                        var ingresoRes = await _ingresosService.RegistrarIngresoExtraCompletoAsync(
                            new RegistrarIngresoExtraRequest
                            {
                                IdEmpresa = dto.IdEmpresa,
                                IdUsuario = dto.IdUsuario,
                                Monto = linea.Credito,
                                Descripcion = motivo,
                                Categoria = catIngreso,
                                Origen = "Conciliación Bancaria",
                                FormaPago = "TRANSFERENCIA",
                                IdCuentaFinanciera = import.IdCuentaFinanciera,
                                Referencia = string.IsNullOrWhiteSpace(linea.Referencia)
                                    ? $"EXT-{linea.IdTesoreriaExtractoLinea}"
                                    : linea.Referencia,
                                Nota = $"Extracto #{import.IdTesoreriaExtractoImport} línea #{linea.IdTesoreriaExtractoLinea}",
                                Fecha = linea.FechaMovimiento,
                                DesdeExtractoBancario = true,
                                IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                                IdTesoreriaConciliacion = import.IdTesoreriaConciliacion,
                                ClaveIdempotencia = clave,
                                FechaMovimiento = linea.FechaMovimiento
                            });
                        idMov = ingresoRes.IdMovimientoFinanciero;
                        if (idMov is not > 0)
                            throw new InvalidOperationException("No se pudo crear el movimiento del ingreso desde conciliación.");
                        linea.IdMovimientoFinanciero = idMov;
                        linea.EstadoMatch = "NUEVO_MOV";
                        linea.AccionTomada = "CREAR_INGRESO";
                        linea.CategoriaSugerida = categoria;
                        linea.ExplicacionMatch = $"Ingreso #{ingresoRes.IdIngreso} desde Conciliación Bancaria.";
                        entidad = "INGRESO";
                    }
                    break;

                case AccionExtractoPendiente.CREAR_AJUSTE:
                    if (linea.Debito <= 0 && linea.Credito <= 0)
                        throw new InvalidOperationException("CREAR_AJUSTE requiere monto en la línea.");
                    idMov = await CrearMovimientoBancarioAsync(
                        import,
                        linea,
                        dto.IdUsuario,
                        dto.Accion,
                        "AJUSTE_BANCARIO",
                        motivo);
                    linea.IdMovimientoFinanciero = idMov;
                    linea.EstadoMatch = "NUEVO_MOV";
                    linea.AccionTomada = dto.Accion.ToString();
                    linea.CategoriaSugerida = "AJUSTE_BANCARIO";
                    entidad = "MOVIMIENTO_FINANCIERO";
                    break;

                default:
                    throw new InvalidOperationException("Acción no soportada.");
            }

            linea.FechaResolucion = DateTime.UtcNow;
            linea.IdUsuarioResolucion = dto.IdUsuario;
            await _context.SaveChangesAsync();

            return new ResolverExtractoLineaResultadoDto
            {
                IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                Accion = dto.Accion,
                IdMovimientoFinanciero = idMov ?? linea.IdMovimientoFinanciero,
                Categoria = categoria,
                EntidadCreada = entidad,
                YaResuelta = false
            };
        }

        public async Task<ExtractoResumenDto> GetResumenAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            decimal tolerancia = 0.01m)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);
            EnsureNoEsPreview(import);
            var lineas = await _context.TesoreriaExtractoLinea
                .AsNoTracking()
                .Where(x => x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                .ToListAsync();

            var conciliadas = lineas.Where(x => TesoreriaConciliacionEstados.EsConciliadaBanco(x.EstadoMatch)).ToList();
            var pendientes = lineas.Where(x => TesoreriaConciliacionEstados.EsPendienteBanco(x.EstadoMatch)).ToList();
            var ignorados = lineas.Where(x => TesoreriaConciliacionEstados.EsExcluidaBanco(x.EstadoMatch)).ToList();

            var gastos = lineas.Count(x =>
                x.AccionTomada == AccionExtractoPendiente.CREAR_GASTO.ToString()
                || string.Equals(x.CategoriaSugerida, "GASTO_BANCARIO", StringComparison.OrdinalIgnoreCase)
                || (x.EstadoMatch == "NUEVO_MOV" && x.Debito > 0
                    && (x.CategoriaSugerida?.Contains("COMISION") == true
                        || x.CategoriaSugerida?.Contains("CARGO") == true
                        || x.CategoriaSugerida?.Contains("IMPUESTO") == true
                        || x.CategoriaSugerida?.Contains("DEBITO") == true)));

            var ingresos = lineas.Count(x =>
                x.AccionTomada == AccionExtractoPendiente.CREAR_INGRESO.ToString()
                || string.Equals(x.CategoriaSugerida, "INGRESO_BANCARIO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.CategoriaSugerida, "INTERES_BANCARIO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.CategoriaSugerida, "CREDITO_AUTOMATICO", StringComparison.OrdinalIgnoreCase));

            var ajustes = lineas.Count(x =>
                x.AccionTomada == AccionExtractoPendiente.CREAR_AJUSTE.ToString()
                || x.CategoriaSugerida is "AJUSTE_BANCARIO" or "REVERSO_BANCARIO" or "OTRO_BANCARIO");

            var saldoLibros = await _movimientoService.GetEstadoCuentaAsync(
                import.IdCuentaFinanciera,
                import.PeriodoDesde,
                import.PeriodoHasta);

            var saldoFinalEstado = import.SaldoFinal
                ?? lineas.Where(x => x.Balance.HasValue).OrderByDescending(x => x.FechaMovimiento)
                    .ThenByDescending(x => x.IdTesoreriaExtractoLinea)
                    .Select(x => x.Balance)
                    .FirstOrDefault()
                ?? 0;

            var diferencia = saldoLibros.SaldoFinal - saldoFinalEstado;
            var puedeCerrar = pendientes.Count == 0 && Math.Abs(diferencia) <= tolerancia;

            return new ExtractoResumenDto
            {
                IdTesoreriaExtractoImport = import.IdTesoreriaExtractoImport,
                SaldoInicialEstado = import.SaldoInicial ?? 0,
                SaldoFinalEstado = saldoFinalEstado,
                TotalDebitos = import.TotalDebitos,
                TotalCreditos = import.TotalCreditos,
                CantidadConciliada = conciliadas.Count,
                MontoConciliado = conciliadas.Sum(x => x.Debito > 0 ? x.Debito : x.Credito),
                CantidadPendiente = pendientes.Count,
                MontoPendiente = pendientes.Sum(x => x.Debito > 0 ? x.Debito : x.Credito),
                Gastos = gastos,
                Ingresos = ingresos,
                Ajustes = ajustes,
                Ignorados = ignorados.Count,
                SaldoLibrosFinalConciliado = saldoLibros.SaldoFinal,
                Diferencia = diferencia,
                PuedeCerrar = puedeCerrar
            };
        }

        public async Task<ExtractoResumenDto> CerrarAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            int idUsuario,
            decimal tolerancia = 0.01m)
        {
            var import = await GetImportTrackedAsync(idTesoreriaExtractoImport, idEmpresa);

            EnsureNoEsPreview(import);

            // Un solo cierre de expediente: el Centro de Conciliación es dueño de
            // FechaUltimaConciliacion / UltimoSaldoConciliado. Evita saldos inconsistentes.
            if (import.IdTesoreriaConciliacion is > 0)
            {
                throw new InvalidOperationException(
                    "Este extracto está vinculado a una conciliación. " +
                    "Cierre el expediente desde el Centro de Conciliación Bancaria.");
            }

            var resumen = await GetResumenAsync(idTesoreriaExtractoImport, idEmpresa, tolerancia);
            if (!resumen.PuedeCerrar)
                throw new InvalidOperationException(
                    $"No se puede cerrar: pendientes={resumen.CantidadPendiente}, diferencia={resumen.Diferencia:N2}.");

            import.Estado = "CERRADO";
            import.Observacion = Truncate(
                $"Cerrado por usuario {idUsuario} el {DateTime.UtcNow:u}. Diff={resumen.Diferencia:N2}",
                500);

            // Legacy sin sesión: solo marca extracto. No toca marcadores de cuenta.
            await _context.SaveChangesAsync();
            return resumen;
        }

        public async Task<TesoreriaExtractoImport?> GetImportByIdAsync(int idTesoreriaExtractoImport, int idEmpresa)
        {
            return await _context.TesoreriaExtractoImport
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                    && x.IdEmpresa == idEmpresa);
        }

        public async Task<IEnumerable<TesoreriaExtractoLinea>> GetLineasAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa)
        {
            await ValidarImportAsync(idTesoreriaExtractoImport, idEmpresa);

            return await _context.TesoreriaExtractoLinea
                .AsNoTracking()
                .Where(x => x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport)
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdTesoreriaExtractoLinea)
                .ToListAsync();
        }

        private async Task AsociarMovimientoAsync(
            TesoreriaExtractoLinea linea,
            int idMovimiento,
            int idEmpresa,
            int idUsuario,
            bool auto)
        {
            var mov = await _context.MovimientoFinanciero.FirstOrDefaultAsync(m =>
                m.IdMovimientoFinanciero == idMovimiento
                && m.IdEmpresa == idEmpresa
                && m.Estado == "CONFIRMADO")
                ?? throw new InvalidOperationException("Movimiento financiero no encontrado.");

            // Un movimiento no puede estar en dos líneas.
            var yaUsado = await _context.TesoreriaExtractoLinea.AnyAsync(x =>
                x.IdMovimientoFinanciero == idMovimiento
                && x.IdTesoreriaExtractoLinea != linea.IdTesoreriaExtractoLinea
                && x.EstadoMatch != "PENDIENTE"
                && x.EstadoMatch != "SUGERIDO"
                && x.EstadoMatch != "DESCARTADO"
                && x.EstadoMatch != "IGNORADO");

            if (yaUsado)
                throw new InvalidOperationException("El movimiento ya está conciliado con otra línea del extracto.");

            linea.IdMovimientoFinanciero = idMovimiento;
            linea.EstadoMatch = auto ? "AUTO_CONCILIADO" : "CONFIRMADO";
            linea.EsAutoConciliado = auto;
            linea.FechaResolucion = DateTime.UtcNow;
            linea.IdUsuarioResolucion = idUsuario;
            linea.ReglaMatch ??= auto ? "AUTO_MONTO_FECHA_REF" : "MANUAL";
            linea.ExplicacionMatch ??= auto
                ? "Coincidencia inequívoca por monto, dirección y fecha/referencia."
                : "Asociación manual confirmada por el usuario.";

            mov.EstadoConciliacion = "CONCILIADO";
            mov.FechaConciliacion = DateTime.UtcNow;
            mov.IdUsuarioConciliacion = idUsuario;

            if (linea.ExtractoImport == null)
            {
                await _context.Entry(linea).Reference(x => x.ExtractoImport).LoadAsync();
            }

            if (linea.ExtractoImport?.IdTesoreriaConciliacion is > 0)
                mov.IdTesoreriaConciliacion = linea.ExtractoImport.IdTesoreriaConciliacion;
        }

        private async Task<int> CrearMovimientoBancarioAsync(
            TesoreriaExtractoImport import,
            TesoreriaExtractoLinea linea,
            int idUsuario,
            AccionExtractoPendiente accion,
            string categoria,
            string motivo)
        {
            var monto = linea.Debito > 0 ? linea.Debito : linea.Credito;
            if (monto <= 0)
                throw new InvalidOperationException("La línea no tiene monto válido.");

            var clave = $"EXT_{import.IdTesoreriaExtractoImport}_{linea.IdTesoreriaExtractoLinea}";
            var catFinal = MapCategoriaAccion(accion, categoria, linea.Debito > 0);

            if (linea.Debito > 0)
            {
                await _movimientoService.RegistrarSalidaAsync(
                    import.IdEmpresa,
                    idUsuario,
                    import.IdCuentaFinanciera,
                    monto,
                    motivo,
                    linea.Referencia,
                    categoria: catFinal,
                    referenciaId: linea.IdTesoreriaExtractoLinea,
                    referenciaTipo: "EXTRACTO",
                    claveIdempotencia: clave);
            }
            else
            {
                await _movimientoService.RegistrarEntradaAsync(
                    import.IdEmpresa,
                    idUsuario,
                    import.IdCuentaFinanciera,
                    monto,
                    motivo,
                    linea.Referencia,
                    categoria: catFinal,
                    referenciaId: linea.IdTesoreriaExtractoLinea,
                    referenciaTipo: "EXTRACTO",
                    claveIdempotencia: clave);
            }

            var mov = await _context.MovimientoFinanciero.FirstOrDefaultAsync(x =>
                x.IdEmpresa == import.IdEmpresa
                && x.ClaveIdempotencia == clave);

            if (mov == null)
                throw new InvalidOperationException("No se pudo crear el movimiento financiero.");

            // Conservar categoría bancaria específica en el registro.
            if (!string.Equals(mov.Categoria, catFinal, StringComparison.OrdinalIgnoreCase))
            {
                mov.Categoria = catFinal;
            }

            mov.EstadoConciliacion = "CONCILIADO";
            mov.FechaConciliacion = DateTime.UtcNow;
            mov.IdUsuarioConciliacion = idUsuario;
            mov.FechaMovimiento = linea.FechaMovimiento;
            return mov.IdMovimientoFinanciero;
        }

        private static string MapTipoGastoDesdeConciliacion(string motivo, string categoriaBancaria)
        {
            if (!string.IsNullOrWhiteSpace(motivo)
                && !string.Equals(motivo, categoriaBancaria, StringComparison.OrdinalIgnoreCase))
                return motivo.Trim();

            return categoriaBancaria?.Trim().ToUpperInvariant() switch
            {
                "COMISION_BANCARIA" or "COMISION" => "Comisión bancaria",
                "CARGO_BANCARIO" => "Cargo bancario",
                "IMPUESTO_BANCARIO" => "Impuesto bancario",
                "DEBITO_AUTOMATICO" => "Débito automático",
                "AJUSTE_BANCARIO" => "Ajuste bancario",
                "REVERSO_BANCARIO" => "Reverso bancario",
                _ => string.IsNullOrWhiteSpace(motivo) ? "Gasto bancario" : motivo.Trim()
            };
        }

        private static string MapCategoriaIngresoDesdeConciliacion(string motivo, string categoriaBancaria)
        {
            if (!string.IsNullOrWhiteSpace(motivo)
                && !string.Equals(motivo, categoriaBancaria, StringComparison.OrdinalIgnoreCase))
                return motivo.Trim();

            return categoriaBancaria?.Trim().ToUpperInvariant() switch
            {
                "INTERES_BANCARIO" => "Interés bancario",
                "CREDITO_AUTOMATICO" => "Crédito automático",
                "AJUSTE_BANCARIO" => "Ajuste bancario",
                "REVERSO_BANCARIO" => "Reverso bancario",
                _ => string.IsNullOrWhiteSpace(motivo) ? "Ingreso bancario" : motivo.Trim()
            };
        }

        private static string MapCategoriaAccion(AccionExtractoPendiente accion, string categoria, bool esDebito)
        {
            if (!string.IsNullOrWhiteSpace(categoria)
                && !string.Equals(categoria, "OTRO_BANCARIO", StringComparison.OrdinalIgnoreCase))
                return categoria.Trim().ToUpperInvariant();

            return accion switch
            {
                AccionExtractoPendiente.CREAR_AJUSTE => "AJUSTE_BANCARIO",
                _ => "OTRO_BANCARIO"
            };
        }

        private async Task SugerirMatchesInternoAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            int idCuentaFinanciera,
            int toleranciaDias)
        {
            // Rematch excepciones; preservar CONFIRMADO / AUTO / NUEVO_MOV / IGNORADO.
            var lineas = await _context.TesoreriaExtractoLinea
                .AsTracking()
                .Where(x =>
                    x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                    && (x.EstadoMatch == "PENDIENTE"
                        || x.EstadoMatch == "SUGERIDO"
                        || x.EstadoMatch == "AMBIGUO"
                        || x.EstadoMatch == "DUPLICADO"
                        || x.EstadoMatch == "DIFERENCIA"))
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdTesoreriaExtractoLinea)
                .ToListAsync();

            var usados = new HashSet<int>(
                await _context.TesoreriaExtractoLinea
                    .AsNoTracking()
                    .Where(x =>
                        x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                        && x.IdMovimientoFinanciero != null
                        && (x.EstadoMatch == "CONFIRMADO"
                            || x.EstadoMatch == "AUTO_CONCILIADO"
                            || x.EstadoMatch == "NUEVO_MOV"
                            || x.EstadoMatch == "RESUELTO"))
                    .Select(x => x.IdMovimientoFinanciero!.Value)
                    .ToListAsync());

            var minFecha = lineas.Count == 0
                ? DateTime.Today.AddDays(-30)
                : lineas.Min(x => x.FechaMovimiento).AddDays(-toleranciaDias);
            var maxFecha = lineas.Count == 0
                ? DateTime.Today.AddDays(30)
                : lineas.Max(x => x.FechaMovimiento).AddDays(toleranciaDias);

            var candidatos = await _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.Estado == "CONFIRMADO"
                    && m.EstadoConciliacion == "PENDIENTE"
                    && m.FechaMovimiento.Date >= minFecha
                    && m.FechaMovimiento.Date <= maxFecha
                    && (m.IdCuentaOrigen == idCuentaFinanciera || m.IdCuentaDestino == idCuentaFinanciera))
                .ToListAsync();

            foreach (var linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea.CategoriaSugerida))
                    linea.CategoriaSugerida = ClasificarCategoria(linea.Descripcion, linea.Referencia, linea.Debito > 0);

                var monto = linea.Debito > 0 ? linea.Debito : linea.Credito;
                var desde = linea.FechaMovimiento.AddDays(-toleranciaDias);
                var hasta = linea.FechaMovimiento.AddDays(toleranciaDias);

                var posibles = candidatos
                    .Where(m =>
                        !usados.Contains(m.IdMovimientoFinanciero)
                        && m.Monto == monto
                        && m.FechaMovimiento.Date >= desde
                        && m.FechaMovimiento.Date <= hasta
                        && TesoreriaMatchingEngine.MismaDireccion(linea, m, idCuentaFinanciera))
                    .Select(m => new
                    {
                        Mov = m,
                        Score = TesoreriaMatchingEngine.CalcularScore(linea, m)
                    })
                    .Where(x => x.Score >= TesoreriaMatchingEngine.ScoreMinimoCandidato)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => Math.Abs((x.Mov.FechaMovimiento.Date - linea.FechaMovimiento.Date).TotalDays))
                    .ToList();

                if (posibles.Count == 0)
                {
                    linea.EstadoMatch = "PENDIENTE";
                    linea.IdMovimientoFinanciero = null;
                    linea.ScoreSugerido = null;
                    linea.EsAutoConciliado = false;
                    linea.ReglaMatch = null;
                    linea.ExplicacionMatch = "Sin candidato 1:1 por monto/fecha/dirección.";
                    continue;
                }

                var mejor = posibles[0];
                var segundo = posibles.Count >= 2 ? posibles[1].Score : (decimal?)null;
                var decision = TesoreriaMatchingEngine.Decidir(
                    mejor.Score,
                    segundo,
                    posibles.Count,
                    TesoreriaMatchingEngine.TieneEvidenciaIdentificadora(linea, mejor.Mov));

                if (decision == MatchDecision.Ambiguo)
                {
                    linea.IdMovimientoFinanciero = mejor.Mov.IdMovimientoFinanciero;
                    linea.ScoreSugerido = mejor.Score;
                    linea.EstadoMatch = "AMBIGUO";
                    linea.EsAutoConciliado = false;
                    linea.ReglaMatch = "AMBIGUO_MULTI_CANDIDATO";
                    linea.ExplicacionMatch =
                        $"Hay {posibles.Count} candidatos similares (mejor score {mejor.Score:0.00}). Revise manualmente.";
                    continue;
                }

                if (decision == MatchDecision.AutoConciliar)
                {
                    var movTracked = await _context.MovimientoFinanciero.FirstAsync(m =>
                        m.IdMovimientoFinanciero == mejor.Mov.IdMovimientoFinanciero);

                    linea.IdMovimientoFinanciero = mejor.Mov.IdMovimientoFinanciero;
                    linea.ScoreSugerido = mejor.Score;
                    linea.EstadoMatch = "AUTO_CONCILIADO";
                    linea.EsAutoConciliado = true;
                    linea.AccionTomada = "ASOCIAR";
                    linea.FechaResolucion = DateTime.UtcNow;
                    linea.ReglaMatch = "AUTO_MONTO_FECHA_REF";
                    linea.ExplicacionMatch =
                        $"Coincidencia inequívoca (score {mejor.Score:0.00}).";

                    movTracked.EstadoConciliacion = "CONCILIADO";
                    movTracked.FechaConciliacion = DateTime.UtcNow;
                    usados.Add(mejor.Mov.IdMovimientoFinanciero);
                }
                else
                {
                    linea.IdMovimientoFinanciero = mejor.Mov.IdMovimientoFinanciero;
                    linea.ScoreSugerido = mejor.Score;
                    linea.EstadoMatch = "SUGERIDO";
                    linea.EsAutoConciliado = false;
                    linea.ReglaMatch = "SUGERIDO_MONTO_FECHA";
                    linea.ExplicacionMatch =
                        $"Sugerencia (score {mejor.Score:0.00}). Confirme o busque otro movimiento.";
                }
            }

            await SugerirMatchesCruzadosAsync(
                idTesoreriaExtractoImport,
                idEmpresa,
                idCuentaFinanciera,
                toleranciaDias,
                usados);
        }

        /// <summary>
        /// Para créditos bancarios sin candidato en la cuenta del extracto,
        /// busca cobros ENTRADA en otras cuentas (p. ej. Caja) — solo sugerencia, nunca auto.
        /// </summary>
        private async Task SugerirMatchesCruzadosAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            int idCuentaFinanciera,
            int toleranciaDias,
            HashSet<int> usados)
        {
            var lineasPendientes = await _context.TesoreriaExtractoLinea
                .AsTracking()
                .Where(x =>
                    x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                    && x.Credito > 0
                    && (x.EstadoMatch == "PENDIENTE"
                        || (x.EstadoMatch == "SUGERIDO" && x.ReglaMatch == null)))
                .ToListAsync();

            // Solo las que quedaron sin match útil en la misma cuenta.
            lineasPendientes = lineasPendientes
                .Where(x => x.EstadoMatch == "PENDIENTE" || x.IdMovimientoFinanciero is not > 0)
                .ToList();

            if (lineasPendientes.Count == 0)
                return;

            var minFecha = lineasPendientes.Min(x => x.FechaMovimiento).AddDays(-toleranciaDias);
            var maxFecha = lineasPendientes.Max(x => x.FechaMovimiento).AddDays(toleranciaDias);

            var yaReclasificados = await _context.PagoReclasificacion.AsNoTracking()
                .Where(r =>
                    r.IdEmpresa == idEmpresa
                    && (r.Estado == PagoReclasificacionEstados.Aplicada
                        || r.Estado == PagoReclasificacionEstados.PendienteContable)
                    && r.IdPagoReclasificacionReversaDe == null)
                .Select(r => r.IdMovimientoOriginal)
                .ToListAsync();
            var setReclas = yaReclasificados.ToHashSet();

            var cruzados = await _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.Estado == "CONFIRMADO"
                    && m.EstadoConciliacion == "PENDIENTE"
                    && m.FechaMovimiento.Date >= minFecha
                    && m.FechaMovimiento.Date <= maxFecha
                    && m.IdCuentaDestino != idCuentaFinanciera
                    && m.IdCuentaOrigen != idCuentaFinanciera
                    && (m.TipoMovimiento == "ENTRADA"
                        || m.Categoria == "VENTA"
                        || m.Categoria == "COBRO_CXC"))
                .ToListAsync();

            cruzados = cruzados
                .Where(m =>
                    !usados.Contains(m.IdMovimientoFinanciero)
                    && !setReclas.Contains(m.IdMovimientoFinanciero)
                    && (m.IdCuentaDestino is > 0 || m.IdCuentaOrigen is > 0))
                .ToList();

            var nombresCuentas = await _context.CuentaFinanciera.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(c => c.IdCuentaFinanciera, c => c.Nombre ?? $"Cuenta #{c.IdCuentaFinanciera}");

            foreach (var linea in lineasPendientes)
            {
                var monto = linea.Credito;
                var desde = linea.FechaMovimiento.AddDays(-toleranciaDias);
                var hasta = linea.FechaMovimiento.AddDays(toleranciaDias);

                var posibles = cruzados
                    .Where(m =>
                        !usados.Contains(m.IdMovimientoFinanciero)
                        && m.Monto == monto
                        && m.FechaMovimiento.Date >= desde
                        && m.FechaMovimiento.Date <= hasta
                        && TesoreriaMatchingEngine.MismaDireccionSinCuenta(linea, m))
                    .Select(m => new
                    {
                        Mov = m,
                        Score = TesoreriaMatchingEngine.CalcularScore(linea, m),
                        Evidencia = TesoreriaMatchingEngine.TieneEvidenciaIdentificadora(linea, m)
                    })
                    .Where(x => x.Score >= TesoreriaMatchingEngine.ScoreMinimoCandidato)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => Math.Abs((x.Mov.FechaMovimiento.Date - linea.FechaMovimiento.Date).TotalDays))
                    .ToList();

                if (posibles.Count == 0)
                    continue;

                var mejor = posibles[0];
                var segundo = posibles.Count >= 2 ? posibles[1].Score : (decimal?)null;
                var decision = TesoreriaMatchingEngine.DecidirCruzado(
                    mejor.Score, segundo, posibles.Count);

                if (decision == MatchDecision.SinMatch)
                    continue;

                var idCuentaMov = mejor.Mov.IdCuentaDestino ?? mejor.Mov.IdCuentaOrigen;
                var nombreCuenta = idCuentaMov is > 0 && nombresCuentas.TryGetValue(idCuentaMov.Value, out var n)
                    ? n
                    : "otra cuenta";

                var confianza = TesoreriaMatchingEngine.NivelConfianzaCruzado(
                    mejor.Score, mejor.Evidencia, posibles.Count);

                linea.IdMovimientoFinanciero = mejor.Mov.IdMovimientoFinanciero;
                linea.ScoreSugerido = mejor.Score;
                linea.EsAutoConciliado = false;
                linea.ReglaMatch = decision == MatchDecision.Ambiguo
                    ? "CRUZADO_METODO_PAGO_AMBIGUO"
                    : "CRUZADO_METODO_PAGO";
                linea.EstadoMatch = decision == MatchDecision.Ambiguo ? "AMBIGUO" : "SUGERIDO";
                linea.ClasificacionLinea ??= "OPERATIVO";
                linea.ModuloOrigenSugerido ??= "CUENTAS_COBRAR";
                linea.ExplicacionMatch =
                    $"Existe un cobro de {monto:N2} en {nombreCuenta} (confianza {confianza}). " +
                    "¿Desea reclasificar este pago hacia el banco del extracto?";
            }
        }

        private async Task<IEnumerable<ExtractoLineaMatchDto>> ConstruirLineasMatchAsync(TesoreriaExtractoImport import)
        {
            var lineas = await _context.TesoreriaExtractoLinea
                .AsNoTracking()
                .Where(x => x.IdTesoreriaExtractoImport == import.IdTesoreriaExtractoImport)
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdTesoreriaExtractoLinea)
                .ToListAsync();

            var resultado = new List<ExtractoLineaMatchDto>();
            foreach (var linea in lineas)
            {
                MovimientoFinancieroListadoDto? sugerido = null;
                if (linea.IdMovimientoFinanciero is > 0)
                {
                    var mov = await _context.MovimientoFinanciero
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.IdMovimientoFinanciero == linea.IdMovimientoFinanciero);

                    if (mov != null)
                    {
                        sugerido = new MovimientoFinancieroListadoDto
                        {
                            IdMovimientoFinanciero = mov.IdMovimientoFinanciero,
                            TipoMovimiento = mov.TipoMovimiento,
                            Categoria = mov.Categoria,
                            Monto = mov.Monto,
                            Motivo = mov.Motivo,
                            FechaMovimiento = mov.FechaMovimiento,
                            Estado = mov.Estado,
                            EstadoConciliacion = mov.EstadoConciliacion
                        };
                    }
                }

                resultado.Add(new ExtractoLineaMatchDto
                {
                    IdTesoreriaExtractoLinea = linea.IdTesoreriaExtractoLinea,
                    FechaMovimiento = linea.FechaMovimiento,
                    Descripcion = linea.Descripcion,
                    Referencia = linea.Referencia,
                    Debito = linea.Debito,
                    Credito = linea.Credito,
                    Balance = linea.Balance,
                    MontoNeto = linea.Debito > 0 ? -linea.Debito : linea.Credito,
                    EstadoMatch = linea.EstadoMatch,
                    IdMovimientoFinanciero = linea.IdMovimientoFinanciero,
                    ScoreSugerido = linea.ScoreSugerido,
                    CategoriaSugerida = linea.CategoriaSugerida,
                    AccionTomada = linea.AccionTomada,
                    EsAutoConciliado = linea.EsAutoConciliado,
                    AccionRecomendada = RecomendarAccion(linea),
                    MovimientoSugerido = sugerido
                });
            }

            return resultado;
        }

        private static string RecomendarAccion(TesoreriaExtractoLinea linea)
        {
            if (string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(linea.ReglaMatch, "CRUZADO_METODO_PAGO_AMBIGUO", StringComparison.OrdinalIgnoreCase))
                return AccionExtractoPendiente.RECLASIFICAR_PAGO.ToString();

            if (linea.EstadoMatch is "SUGERIDO" or "AUTO_CONCILIADO" or "CONFIRMADO")
                return AccionExtractoPendiente.ASOCIAR.ToString();

            var cat = (linea.CategoriaSugerida ?? string.Empty).ToUpperInvariant();
            if (cat.Contains("INTERES") || cat.Contains("CREDITO_AUTOMATICO") || cat == "INGRESO_BANCARIO")
                return AccionExtractoPendiente.CREAR_INGRESO.ToString();
            if (cat.Contains("AJUSTE") || cat.Contains("REVERSO"))
                return AccionExtractoPendiente.CREAR_AJUSTE.ToString();
            if (linea.Debito > 0)
                return AccionExtractoPendiente.CREAR_GASTO.ToString();
            return AccionExtractoPendiente.CREAR_INGRESO.ToString();
        }

        private ParsedExtracto ParseContenido(ImportarExtractoDto dto)
        {
            var request = new BankStatementParseRequest
            {
                Formato = ResolveFormato(dto),
                NombreArchivo = dto.NombreArchivo,
                ContenidoCsv = dto.ContenidoCsv,
                TextoExtraido = dto.TextoExtraido,
                ContenidoArchivo = dto.ContenidoArchivo
            };

            var parsed = _parserOrchestrator.Parse(request);
            return new ParsedExtracto
            {
                AdapterUsado = parsed.AdapterUsado,
                Banco = parsed.Banco,
                NumeroCuentaBanco = parsed.NumeroCuentaBanco,
                Moneda = parsed.Moneda,
                PeriodoDesde = parsed.PeriodoDesde,
                PeriodoHasta = parsed.PeriodoHasta,
                SaldoInicial = parsed.SaldoInicial,
                SaldoFinal = parsed.SaldoFinal,
                TotalDebitos = parsed.TotalDebitos,
                TotalCreditos = parsed.TotalCreditos,
                Warnings = parsed.Warnings.ToList(),
                Lineas = parsed.Lineas.Select(x => new ParsedLinea
                {
                    Fecha = x.Fecha,
                    Descripcion = x.Descripcion,
                    Referencia = x.Referencia,
                    Debito = x.Debito,
                    Credito = x.Credito,
                    Balance = x.Balance
                }).ToList()
            };
        }

        private async Task<ExtractoPreviewDto> MapToPreviewDtoAsync(TesoreriaExtractoImport import)
        {
            var lineas = await _context.TesoreriaExtractoLinea
                .AsNoTracking()
                .Where(x => x.IdTesoreriaExtractoImport == import.IdTesoreriaExtractoImport)
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdTesoreriaExtractoLinea)
                .ToListAsync();

            return new ExtractoPreviewDto
            {
                IdTesoreriaExtractoImport = import.IdTesoreriaExtractoImport,
                IdEmpresa = import.IdEmpresa,
                IdCuentaFinanciera = import.IdCuentaFinanciera,
                Estado = import.Estado,
                NombreArchivo = import.NombreArchivo,
                Formato = import.Formato,
                AdapterUsado = import.AdapterUsado,
                Banco = import.Banco,
                NumeroCuentaBanco = import.NumeroCuentaBanco,
                Moneda = import.Moneda,
                PeriodoDesde = import.PeriodoDesde,
                PeriodoHasta = import.PeriodoHasta,
                SaldoInicial = import.SaldoInicial,
                SaldoFinal = import.SaldoFinal,
                TotalDebitos = import.TotalDebitos,
                TotalCreditos = import.TotalCreditos,
                CantidadLineas = lineas.Count,
                Warnings = DeserializeWarnings(import.ParserWarnings),
                Lineas = lineas.Select(x => new ExtractoPreviewLineaDto
                {
                    IdTesoreriaExtractoLinea = x.IdTesoreriaExtractoLinea,
                    FechaMovimiento = x.FechaMovimiento,
                    Descripcion = x.Descripcion,
                    Referencia = x.Referencia,
                    Debito = x.Debito,
                    Credito = x.Credito,
                    Balance = x.Balance,
                    CategoriaSugerida = x.CategoriaSugerida
                }).ToList()
            };
        }

        private async Task RecalcularTotalesImportAsync(TesoreriaExtractoImport import)
        {
            var lineas = await _context.TesoreriaExtractoLinea
                .Where(x => x.IdTesoreriaExtractoImport == import.IdTesoreriaExtractoImport)
                .ToListAsync();

            import.TotalDebitos = lineas.Sum(x => x.Debito);
            import.TotalCreditos = lineas.Sum(x => x.Credito);
            if (lineas.Count > 0)
            {
                import.PeriodoDesde = lineas.Min(x => x.FechaMovimiento);
                import.PeriodoHasta = lineas.Max(x => x.FechaMovimiento);
                import.SaldoFinal = lineas
                    .Where(x => x.Balance.HasValue)
                    .OrderByDescending(x => x.FechaMovimiento)
                    .ThenByDescending(x => x.IdTesoreriaExtractoLinea)
                    .Select(x => x.Balance)
                    .FirstOrDefault()
                    ?? import.SaldoFinal;
            }
        }

        private static TesoreriaExtractoLinea MapNuevaLinea(int idImport, NuevaLineaPreviewDto row)
        {
            return new TesoreriaExtractoLinea
            {
                IdTesoreriaExtractoImport = idImport,
                FechaMovimiento = row.FechaMovimiento.Date,
                Descripcion = Truncate(row.Descripcion, 250),
                Referencia = Truncate(row.Referencia, 100),
                Debito = Math.Abs(row.Debito),
                Credito = Math.Abs(row.Credito),
                Balance = row.Balance,
                EstadoMatch = "PENDIENTE",
                CategoriaSugerida = ClasificarCategoria(row.Descripcion, row.Referencia, row.Debito > 0)
            };
        }

        private static void AplicarCamposLinea(TesoreriaExtractoLinea linea, NuevaLineaPreviewDto row)
        {
            linea.FechaMovimiento = row.FechaMovimiento.Date;
            linea.Descripcion = Truncate(row.Descripcion, 250);
            linea.Referencia = Truncate(row.Referencia, 100);
            linea.Debito = Math.Abs(row.Debito);
            linea.Credito = Math.Abs(row.Credito);
            linea.Balance = row.Balance;
            linea.CategoriaSugerida = ClasificarCategoria(row.Descripcion, row.Referencia, row.Debito > 0);
        }

        private static void ValidarLineaPreviewInput(NuevaLineaPreviewDto row)
        {
            if (row.Debito < 0 || row.Credito < 0)
                throw new InvalidOperationException("Débito y crédito no pueden ser negativos.");
            if (row.Debito == 0 && row.Credito == 0)
                throw new InvalidOperationException("Cada línea debe tener débito o crédito.");
            if (row.Debito > 0 && row.Credito > 0)
                throw new InvalidOperationException("Una línea no puede tener débito y crédito a la vez.");
        }

        private static void EnsureEsPreview(TesoreriaExtractoImport import)
        {
            if (!string.Equals(import.Estado, "PREVIEW", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"La operación solo aplica a extractos en PREVIEW (estado actual: {import.Estado}).");
        }

        private static void EnsureNoEsPreview(TesoreriaExtractoImport import)
        {
            if (string.Equals(import.Estado, "PREVIEW", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "El extracto está en PREVIEW. Confírmelo antes de conciliar o adjuntarlo.");
        }

        private static string? ResolveHash(ImportarExtractoDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.HashArchivo))
                return dto.HashArchivo;
            if (dto.ContenidoArchivo is { Length: > 0 })
                return ComputeSha256(dto.ContenidoArchivo);
            if (!string.IsNullOrWhiteSpace(dto.ContenidoCsv))
                return ComputeSha256(Encoding.UTF8.GetBytes(dto.ContenidoCsv));
            if (!string.IsNullOrWhiteSpace(dto.TextoExtraido))
                return ComputeSha256(Encoding.UTF8.GetBytes(dto.TextoExtraido));
            return null;
        }

        private static string? SerializeWarnings(IEnumerable<string>? warnings)
        {
            var list = warnings?.Where(w => !string.IsNullOrWhiteSpace(w)).ToList() ?? new List<string>();
            if (list.Count == 0) return null;
            return JsonSerializer.Serialize(list);
        }

        private static List<string> DeserializeWarnings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string> { json };
            }
        }

        public static string ClasificarCategoria(string? descripcion, string? referencia, bool esDebito)
        {
            var t = $"{referencia} {descripcion}".ToUpperInvariant();

            if (t.Contains("INTERES") || t.Contains("INTERÉS"))
                return "INTERES_BANCARIO";
            if (t.Contains("COMISION") || t.Contains("COMISIÓN") || t.Contains("COM-"))
                return "COMISION_BANCARIA";
            if (t.Contains("IMPUESTO") || t.Contains("ITBIS") || t.Contains("RETENC"))
                return "IMPUESTO_BANCARIO";
            if (t.Contains("CARGO") || t.Contains("SERVICIO BANCARIO"))
                return "CARGO_BANCARIO";
            if (t.Contains("REVERSO") || t.Contains("REVERSA") || t.Contains("ANUL"))
                return "REVERSO_BANCARIO";
            if (t.Contains("AJUSTE"))
                return "AJUSTE_BANCARIO";
            if (esDebito && (t.Contains("DEBITO AUTOMATICO") || t.Contains("DÉBITO AUTOMÁTICO") || t.Contains("PAGO")))
                return esDebito && (t.Contains("AUTOMAT") || t.Contains("DEBITO AUTOM"))
                    ? "DEBITO_AUTOMATICO"
                    : "CARGO_BANCARIO";
            if (!esDebito && (t.Contains("DEPOSITO") || t.Contains("DEPÓSITO") || t.Contains("DEP-") || t.Contains("TRANSFERENCIA RECIBIDA")))
                return "CREDITO_AUTOMATICO";

            return "OTRO_BANCARIO";
        }

        private static string ResolveFormato(ImportarExtractoDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Formato))
                return dto.Formato.Trim().ToUpperInvariant();

            var ext = Path.GetExtension(dto.NombreArchivo ?? string.Empty).ToLowerInvariant();
            return ext switch
            {
                ".xlsx" => "XLSX",
                ".xls" => "XLS",
                ".pdf" => "PDF",
                ".txt" => "TXT",
                _ => "CSV"
            };
        }

        private static void ValidarImportacion(ImportarExtractoDto dto)
        {
            if (dto.IdEmpresa <= 0 || dto.IdCuentaFinanciera <= 0)
                throw new InvalidOperationException("Empresa y cuenta son obligatorias.");

            var name = SanitizeFileName(dto.NombreArchivo);
            dto.NombreArchivo = name;
            var ext = Path.GetExtension(name);
            if (!string.IsNullOrEmpty(ext) && !ExtensionesPermitidas.Contains(ext))
                throw new InvalidOperationException($"Extensión no permitida: {ext}");

            var size = dto.ContenidoArchivo?.LongLength
                ?? Encoding.UTF8.GetByteCount(dto.ContenidoCsv ?? dto.TextoExtraido ?? string.Empty);
            if (size > MaxFileBytes)
                throw new InvalidOperationException("El archivo excede el tamaño máximo (8 MB).");
        }

        private static string SanitizeFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "extracto.csv";

            var file = Path.GetFileName(name.Trim());
            foreach (var c in Path.GetInvalidFileNameChars())
                file = file.Replace(c, '_');
            return Truncate(file, 260) ?? "extracto.csv";
        }

        private static string ComputeSha256(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value[..max];
        }

        private async Task ValidarImportAsync(int idTesoreriaExtractoImport, int idEmpresa)
        {
            var exists = await _context.TesoreriaExtractoImport.AnyAsync(x =>
                x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                && x.IdEmpresa == idEmpresa);

            if (!exists)
                throw new InvalidOperationException("Importación de extracto no encontrada.");
        }

        private async Task<TesoreriaExtractoImport> GetImportTrackedAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa)
        {
            return await _context.TesoreriaExtractoImport
                .AsTracking()
                .FirstOrDefaultAsync(x =>
                    x.IdTesoreriaExtractoImport == idTesoreriaExtractoImport
                    && x.IdEmpresa == idEmpresa)
                ?? throw new InvalidOperationException("Importación de extracto no encontrada.");
        }

        
        private sealed class ParsedExtracto
        {
            public string? AdapterUsado { get; set; }
            public string? Banco { get; set; }
            public string? NumeroCuentaBanco { get; set; }
            public string? Moneda { get; set; }
            public DateTime? PeriodoDesde { get; set; }
            public DateTime? PeriodoHasta { get; set; }
            public decimal? SaldoInicial { get; set; }
            public decimal? SaldoFinal { get; set; }
            public decimal TotalDebitos { get; set; }
            public decimal TotalCreditos { get; set; }
            public List<string> Warnings { get; set; } = new();
            public List<ParsedLinea> Lineas { get; set; } = new();
        }

        private sealed class ParsedLinea
        {
            public DateTime Fecha { get; set; }
            public string? Descripcion { get; set; }
            public string? Referencia { get; set; }
            public decimal Debito { get; set; }
            public decimal Credito { get; set; }
            public decimal? Balance { get; set; }
        }
    }
}
