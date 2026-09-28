using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public class SecuenciaEcfService : ISecuenciaEcfService
    {
        private readonly AlahiaPosContext _ctx;

        public SecuenciaEcfService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<SecuenciaEcfDto>> GetAllAsync(int idEmpresa)
        {
            var filas = await _ctx.SecuenciasECF
                .Where(s => s.IdEmpresa == idEmpresa && s.TipoEcfDgii > 0)
                .AsNoTracking()
                .OrderBy(s => s.TipoEcfDgii)
                .ThenBy(s => s.SecuenciaInicial)
                .ToListAsync();

            var ids = filas.Select(s => s.IdSecuencia).ToList();
            var asignaciones = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                .Where(a => ids.Contains(a.IdSecuencia))
                .OrderBy(a => a.SecuenciaInicial)
                .ToListAsync();

            var sucursales = await NombresSucursalAsync(idEmpresa);

            return filas.Select(s => MapToDto(
                s,
                asignaciones.Where(a => a.IdSecuencia == s.IdSecuencia).ToList(),
                sucursales)).ToList();
        }

        public async Task<SecuenciaEcfDto?> GetByIdAsync(int id)
        {
            var s = await _ctx.SecuenciasECF.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdSecuencia == id);
            if (s == null) return null;

            var asignaciones = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                .Where(a => a.IdSecuencia == id)
                .OrderBy(a => a.SecuenciaInicial)
                .ToListAsync();
            var sucursales = await NombresSucursalAsync(s.IdEmpresa);
            return MapToDto(s, asignaciones, sucursales);
        }

        public async Task<SecuenciaEcfDto> CreateAsync(SecuenciaEcfCreateDto dto)
        {
            var inicial = dto.SecuenciaInicial > 0 ? dto.SecuenciaInicial : 1;
            var proxima = dto.ProximaSecuencia ?? dto.SecuenciaActual ?? inicial;
            ValidarRango(inicial, proxima, dto.SecuenciaFinal);
            await ValidarSolapeAutorizacionAsync(
                dto.IdEmpresa, dto.TipoEcfDgii, inicial, dto.SecuenciaFinal, null);

            var entity = new SecuenciaECF
            {
                IdEmpresa = dto.IdEmpresa,
                TipoEcfDgii = dto.TipoEcfDgii,
                TipoNCF = $"E{dto.TipoEcfDgii}",
                Descripcion = dto.Descripcion ?? DescripcionPorTipo(dto.TipoEcfDgii),
                Serie = dto.Serie,
                SecuenciaInicial = inicial,
                SecuenciaActual = proxima,
                SecuenciaFinal = dto.SecuenciaFinal,
                fechaVencimiento = dto.FechaVencimiento ?? DateTime.MaxValue,
                stockMinimo = dto.StockMinimo,
                Activo = true,
                Ambiente = dto.Ambiente,
                NumeroResolucion = dto.NumeroResolucion,
                FechaCreacion = DateTime.Now,
                FechaAutorizacion = DateTime.Now
            };

            _ctx.SecuenciasECF.Add(entity);
            await _ctx.SaveChangesAsync();
            return MapToDto(entity, new List<SecuenciaECFAsignacion>(), new Dictionary<int, string>());
        }

        public async Task UpdateAsync(int id, SecuenciaEcfUpdateDto dto)
        {
            var entity = await _ctx.SecuenciasECF.FindAsync(id)
                ?? throw new InvalidOperationException($"SecuenciaECF {id} no encontrada");

            var inicial = dto.SecuenciaInicial ?? entity.SecuenciaInicial;
            var proxima = dto.ProximaSecuencia ?? dto.SecuenciaActual ?? entity.SecuenciaActual;
            var final = dto.SecuenciaFinal ?? entity.SecuenciaFinal;
            ValidarRango(inicial, proxima, final);
            await ValidarSolapeAutorizacionAsync(
                entity.IdEmpresa, entity.TipoEcfDgii ?? 0, inicial, final, entity.IdSecuencia);

            var asignaciones = await _ctx.SecuenciaECFAsignaciones
                .Where(a => a.IdSecuencia == id && a.Activo)
                .ToListAsync();
            if (asignaciones.Count > 0)
            {
                var minAsig = asignaciones.Min(a => a.SecuenciaInicial);
                var maxAsig = asignaciones.Max(a => a.SecuenciaFinal);
                if (inicial > minAsig || final < maxAsig)
                {
                    throw new InvalidOperationException(
                        $"El rango de la empresa ({inicial}–{final}) no cubre las asignaciones a sucursal ({minAsig}–{maxAsig}). Ajuste primero esas asignaciones.");
                }
            }

            entity.SecuenciaInicial = inicial;
            entity.SecuenciaActual = proxima;
            entity.SecuenciaFinal = final;
            if (dto.FechaVencimiento.HasValue)
                entity.fechaVencimiento = dto.FechaVencimiento.Value;
            if (dto.StockMinimo.HasValue)
                entity.stockMinimo = dto.StockMinimo.Value;
            if (dto.Activo.HasValue)
                entity.Activo = dto.Activo.Value;

            await _ctx.SaveChangesAsync();
        }

        public async Task DesactivarAsync(int id)
        {
            var entity = await _ctx.SecuenciasECF.FindAsync(id);
            if (entity == null) return;

            entity.Activo = false;
            var asignaciones = await _ctx.SecuenciaECFAsignaciones
                .Where(a => a.IdSecuencia == id && a.Activo)
                .ToListAsync();
            foreach (var a in asignaciones)
                a.Activo = false;

            await _ctx.SaveChangesAsync();
        }

        public async Task<SecuenciaEcfAsignacionDto> AsignarRangoAsync(int idSecuencia, SecuenciaEcfAsignarDto dto)
        {
            var parent = await _ctx.SecuenciasECF.FindAsync(idSecuencia)
                ?? throw new InvalidOperationException($"SecuenciaECF {idSecuencia} no encontrada");

            var proxima = dto.ProximaSecuencia ?? dto.SecuenciaActual ?? dto.SecuenciaInicial;
            ValidarRango(dto.SecuenciaInicial, proxima, dto.SecuenciaFinal);
            await ValidarAsignacionAsync(parent, dto.IdSucursal, dto.SecuenciaInicial, dto.SecuenciaFinal, null);

            var entity = new SecuenciaECFAsignacion
            {
                IdSecuencia = parent.IdSecuencia,
                IdEmpresa = parent.IdEmpresa,
                TipoEcfDgii = parent.TipoEcfDgii ?? 0,
                IdSucursal = dto.IdSucursal,
                SecuenciaInicial = dto.SecuenciaInicial,
                SecuenciaFinal = dto.SecuenciaFinal,
                SecuenciaActual = proxima,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _ctx.SecuenciaECFAsignaciones.Add(entity);
            await _ctx.SaveChangesAsync();

            var nombres = await NombresSucursalAsync(parent.IdEmpresa);
            return MapAsignacion(entity, nombres);
        }

        public async Task ActualizarAsignacionAsync(int idAsignacion, SecuenciaEcfAsignarDto dto)
        {
            var entity = await _ctx.SecuenciaECFAsignaciones.FindAsync(idAsignacion)
                ?? throw new InvalidOperationException($"Asignación {idAsignacion} no encontrada");

            var parent = await _ctx.SecuenciasECF.FindAsync(entity.IdSecuencia)
                ?? throw new InvalidOperationException("Secuencia de empresa no encontrada");

            var idSucursal = dto.IdSucursal > 0 ? dto.IdSucursal : entity.IdSucursal;
            var inicial = dto.SecuenciaInicial > 0 ? dto.SecuenciaInicial : entity.SecuenciaInicial;
            var final = dto.SecuenciaFinal > 0 ? dto.SecuenciaFinal : entity.SecuenciaFinal;
            var proxima = dto.ProximaSecuencia ?? dto.SecuenciaActual ?? entity.SecuenciaActual;
            ValidarRango(inicial, proxima, final);
            await ValidarAsignacionAsync(parent, idSucursal, inicial, final, entity.IdAsignacion);

            entity.IdSucursal = idSucursal;
            entity.SecuenciaInicial = inicial;
            entity.SecuenciaFinal = final;
            entity.SecuenciaActual = proxima;
            await _ctx.SaveChangesAsync();
        }

        public async Task DesactivarAsignacionAsync(int idAsignacion)
        {
            var entity = await _ctx.SecuenciaECFAsignaciones.FindAsync(idAsignacion);
            if (entity == null) return;
            entity.Activo = false;
            await _ctx.SaveChangesAsync();
        }

        public async Task<ReservaEcfResultado> ReservarSiguienteAsync(
            int idEmpresa, int tipoEcfDgii, int? idSucursal = null)
        {
            var cursor = await ObtenerCursorAsync(idEmpresa, tipoEcfDgii, idSucursal);
            if (cursor == null)
            {
                return ReservaEcfResultado.Fallo(
                    MensajeSinSecuencia(idEmpresa, tipoEcfDgii, idSucursal));
            }

            if (cursor.Asignacion != null)
            {
                await AlinearAsignacionConHistorialAsync(
                    idEmpresa, tipoEcfDgii, cursor.Asignacion.IdAsignacion);
                return await ReservarAsignacionAsync(
                    cursor.Asignacion.IdAsignacion, idEmpresa, tipoEcfDgii, idSucursal);
            }

            await AlinearConHistorialAsync(idEmpresa, tipoEcfDgii, cursor.Parent.IdSecuencia);
            return await ReservarPadreAsync(cursor.Parent.IdSecuencia, idEmpresa, tipoEcfDgii, idSucursal);
        }

        public async Task<string?> PeekSiguienteAsync(int idEmpresa, int tipoEcfDgii, int? idSucursal = null)
        {
            var cursor = await ObtenerCursorAsync(idEmpresa, tipoEcfDgii, idSucursal);
            if (cursor == null) return null;

            if (cursor.Asignacion != null)
                await AlinearAsignacionConHistorialAsync(idEmpresa, tipoEcfDgii, cursor.Asignacion.IdAsignacion);
            else
                await AlinearConHistorialAsync(idEmpresa, tipoEcfDgii, cursor.Parent.IdSecuencia);

            cursor = await ObtenerCursorAsync(idEmpresa, tipoEcfDgii, idSucursal);
            if (cursor == null) return null;
            return $"{cursor.Parent.Serie}{cursor.Actual:D10}";
        }

        public async Task<SecuenciaAlertaDto> ValidarDisponibilidadAsync(
            int idEmpresa, int tipoEcfDgii, int? idSucursal = null)
        {
            var cursor = await ObtenerCursorAsync(idEmpresa, tipoEcfDgii, idSucursal);

            if (cursor == null)
            {
                return new SecuenciaAlertaDto
                {
                    TipoEcfDgii = tipoEcfDgii,
                    Disponible = false,
                    MensajeAlerta = MensajeSinSecuencia(idEmpresa, tipoEcfDgii, idSucursal)
                };
            }

            int restantes = cursor.Final - cursor.Actual + 1;
            bool agotada = restantes <= 0;
            bool vencida = cursor.Parent.fechaVencimiento <= DateTime.Now;
            bool stockBajo = restantes > 0 && restantes <= cursor.Parent.stockMinimo;
            bool proximaAVencer = !vencida && cursor.Parent.fechaVencimiento <= DateTime.Now.AddDays(30);

            string? alerta = null;
            if (agotada) alerta = "Secuencia agotada";
            else if (vencida) alerta = "Secuencia vencida";
            else if (stockBajo) alerta = $"Stock bajo: {restantes} restantes";
            else if (proximaAVencer) alerta = "Próxima a vencer";

            return new SecuenciaAlertaDto
            {
                TipoEcfDgii = tipoEcfDgii,
                Descripcion = DescripcionPorTipo(tipoEcfDgii),
                Disponible = !agotada && !vencida,
                Restantes = Math.Max(0, restantes),
                StockBajo = stockBajo,
                ProximaAVencer = proximaAVencer,
                MensajeAlerta = alerta
            };
        }

        public async Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerDisponiblesAsync(
            int idEmpresa, int? idSucursal = null)
        {
            var ahora = DateTime.Now;
            var varias = await EmpresaTieneVariasSucursalesAsync(idEmpresa);

            if (idSucursal is > 0)
            {
                var asignadas = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                    .Where(a => a.IdEmpresa == idEmpresa
                        && a.IdSucursal == idSucursal
                        && a.Activo)
                    .ToListAsync();

                if (asignadas.Count > 0)
                {
                    var idsPadre = asignadas.Select(a => a.IdSecuencia).Distinct().ToList();
                    var padres = await _ctx.SecuenciasECF.AsNoTracking()
                        .Where(s => idsPadre.Contains(s.IdSecuencia) && s.Activo)
                        .ToListAsync();

                    return asignadas
                        .Join(padres, a => a.IdSecuencia, s => s.IdSecuencia, (a, s) => new { a, s })
                        .Select(x => ToDisponible(x.s, x.a.SecuenciaActual, x.a.SecuenciaFinal, ahora))
                        .ToList();
                }

                if (varias)
                    return Array.Empty<SecuenciaEcfDisponibleDto>();
            }

            var filas = await _ctx.SecuenciasECF.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.Activo && s.TipoEcfDgii > 0)
                .OrderBy(s => s.TipoEcfDgii)
                .ToListAsync();

            if (varias)
                return Array.Empty<SecuenciaEcfDisponibleDto>();

            return filas.Select(s => ToDisponible(s, s.SecuenciaActual, s.SecuenciaFinal, ahora)).ToList();
        }

        public async Task<SecuenciaECF?> ObtenerActivaAsync(
            int idEmpresa, int tipoEcfDgii, int? idSucursal = null)
        {
            var cursor = await ObtenerCursorAsync(idEmpresa, tipoEcfDgii, idSucursal);
            if (cursor == null) return null;

            var parent = cursor.Parent;
            if (cursor.Asignacion != null)
            {
                parent.SecuenciaInicial = cursor.Asignacion.SecuenciaInicial;
                parent.SecuenciaActual = cursor.Asignacion.SecuenciaActual;
                parent.SecuenciaFinal = cursor.Asignacion.SecuenciaFinal;
            }
            return parent;
        }

        private async Task<CursorEmision?> ObtenerCursorAsync(
            int idEmpresa, int tipoEcfDgii, int? idSucursal)
        {
            var ahora = DateTime.Now;

            if (idSucursal is > 0)
            {
                var asig = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                    .Where(a => a.IdEmpresa == idEmpresa
                        && a.TipoEcfDgii == tipoEcfDgii
                        && a.IdSucursal == idSucursal
                        && a.Activo
                        && a.SecuenciaActual <= a.SecuenciaFinal)
                    .OrderBy(a => a.SecuenciaInicial)
                    .FirstOrDefaultAsync();

                if (asig != null)
                {
                    var parent = await _ctx.SecuenciasECF.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.IdSecuencia == asig.IdSecuencia
                            && s.Activo
                            && s.fechaVencimiento > ahora);
                    if (parent == null) return null;
                    return new CursorEmision { Parent = parent, Asignacion = asig };
                }

                if (await EmpresaTieneVariasSucursalesAsync(idEmpresa))
                    return null;
            }

            var fila = await _ctx.SecuenciasECF.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa
                    && s.TipoEcfDgii == tipoEcfDgii
                    && s.Activo
                    && s.SecuenciaActual <= s.SecuenciaFinal
                    && s.fechaVencimiento > ahora)
                .OrderBy(s => s.SecuenciaInicial)
                .FirstOrDefaultAsync();

            if (fila == null) return null;

            var tieneAsignaciones = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                .AnyAsync(a => a.IdSecuencia == fila.IdSecuencia && a.Activo);
            if (tieneAsignaciones && await EmpresaTieneVariasSucursalesAsync(idEmpresa))
                return null;

            return new CursorEmision { Parent = fila, Asignacion = null };
        }

        private async Task<ReservaEcfResultado> ReservarPadreAsync(
            int idSecuencia, int idEmpresa, int tipoEcfDgii, int? idSucursal)
        {
            const string sql = @"
                UPDATE TOP(1) SecuenciasECF
                SET SecuenciaActual = SecuenciaActual + 1
                OUTPUT
                    inserted.Serie,
                    inserted.SecuenciaActual - 1 AS NumeroReservado,
                    inserted.SecuenciaFinal
                WHERE IdSecuencia = @idSecuencia
                  AND Activo = 1
                  AND SecuenciaActual <= SecuenciaFinal
                  AND fechaVencimiento > GETDATE()";

            return await EjecutarReservaAsync(sql, "@idSecuencia", idSecuencia, idEmpresa, tipoEcfDgii, idSucursal);
        }

        private async Task<ReservaEcfResultado> ReservarAsignacionAsync(
            int idAsignacion, int idEmpresa, int tipoEcfDgii, int? idSucursal)
        {
            const string sql = @"
                UPDATE a
                SET a.SecuenciaActual = a.SecuenciaActual + 1
                OUTPUT
                    s.Serie,
                    inserted.SecuenciaActual - 1 AS NumeroReservado,
                    inserted.SecuenciaFinal
                FROM dbo.SecuenciaECFAsignacion a
                INNER JOIN dbo.SecuenciasECF s ON s.IdSecuencia = a.IdSecuencia
                WHERE a.IdAsignacion = @idAsignacion
                  AND a.Activo = 1
                  AND s.Activo = 1
                  AND a.SecuenciaActual <= a.SecuenciaFinal
                  AND s.fechaVencimiento > GETDATE()";

            return await EjecutarReservaAsync(sql, "@idAsignacion", idAsignacion, idEmpresa, tipoEcfDgii, idSucursal);
        }

        private async Task<ReservaEcfResultado> EjecutarReservaAsync(
            string sql, string paramName, int id, int idEmpresa, int tipoEcfDgii, int? idSucursal)
        {
            var conn = _ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter(paramName, id));

            if (_ctx.Database.CurrentTransaction != null)
                cmd.Transaction = _ctx.Database.CurrentTransaction.GetDbTransaction();

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return ReservaEcfResultado.Fallo(
                    MensajeSinSecuencia(idEmpresa, tipoEcfDgii, idSucursal));
            }

            string serie = reader.GetString(0);
            int numeroReservado = reader.GetInt32(1);
            int secuenciaFinal = reader.GetInt32(2);
            string encf = $"{serie}{numeroReservado:D10}";

            return new ReservaEcfResultado
            {
                Exitoso = true,
                Encf = encf,
                NumeroReservado = numeroReservado,
                SecuenciaFinal = secuenciaFinal
            };
        }

        private async Task AlinearConHistorialAsync(int idEmpresa, int tipoEcfDgii, int idSecuencia)
        {
            const string sql = @"
                DECLARE @prefijo nvarchar(4) = N'E' + RIGHT(N'00' + CONVERT(varchar(10), @tipoEcf), 2);
                DECLARE @ini int, @fin int;
                SELECT @ini = SecuenciaInicial, @fin = SecuenciaFinal
                FROM dbo.SecuenciasECF WHERE IdSecuencia = @idSecuencia;

                ;WITH maxHist AS (
                    SELECT ISNULL(MAX(Num), 0) AS MaxNum
                    FROM (
                        SELECT TRY_CONVERT(int, RIGHT(e.ENCF, 10)) AS Num
                        FROM dbo.ECFEncabezado e
                        WHERE e.IdEmpresa = @idEmpresa
                          AND e.ENCF LIKE @prefijo + N'%'
                        UNION ALL
                        SELECT TRY_CONVERT(int, RIGHT(h.NCF, 10))
                        FROM dbo.FacturaHeaders h
                        WHERE h.IdEmpresa = @idEmpresa
                          AND h.NCF LIKE @prefijo + N'%'
                        UNION ALL
                        SELECT TRY_CONVERT(int, RIGHT(n.NCF, 10))
                        FROM dbo.NotasCredito n
                        WHERE n.IdEmpresa = @idEmpresa
                          AND n.NCF LIKE @prefijo + N'%'
                    ) x
                    WHERE Num IS NOT NULL
                      AND Num >= @ini AND Num <= @fin
                )
                UPDATE s
                SET SecuenciaActual = mh.MaxNum + 1
                FROM dbo.SecuenciasECF s
                CROSS JOIN maxHist mh
                WHERE s.IdSecuencia = @idSecuencia
                  AND s.Activo = 1
                  AND mh.MaxNum >= @ini
                  AND s.SecuenciaActual < mh.MaxNum + 1
                  AND mh.MaxNum + 1 <= s.SecuenciaFinal;";

            await _ctx.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@idEmpresa", idEmpresa),
                new SqlParameter("@tipoEcf", tipoEcfDgii),
                new SqlParameter("@idSecuencia", idSecuencia));
        }

        private async Task AlinearAsignacionConHistorialAsync(int idEmpresa, int tipoEcfDgii, int idAsignacion)
        {
            const string sql = @"
                DECLARE @prefijo nvarchar(4) = N'E' + RIGHT(N'00' + CONVERT(varchar(10), @tipoEcf), 2);
                DECLARE @ini int, @fin int;
                SELECT @ini = SecuenciaInicial, @fin = SecuenciaFinal
                FROM dbo.SecuenciaECFAsignacion WHERE IdAsignacion = @idAsignacion;

                ;WITH maxHist AS (
                    SELECT ISNULL(MAX(Num), 0) AS MaxNum
                    FROM (
                        SELECT TRY_CONVERT(int, RIGHT(e.ENCF, 10)) AS Num
                        FROM dbo.ECFEncabezado e
                        WHERE e.IdEmpresa = @idEmpresa
                          AND e.ENCF LIKE @prefijo + N'%'
                        UNION ALL
                        SELECT TRY_CONVERT(int, RIGHT(h.NCF, 10))
                        FROM dbo.FacturaHeaders h
                        WHERE h.IdEmpresa = @idEmpresa
                          AND h.NCF LIKE @prefijo + N'%'
                        UNION ALL
                        SELECT TRY_CONVERT(int, RIGHT(n.NCF, 10))
                        FROM dbo.NotasCredito n
                        WHERE n.IdEmpresa = @idEmpresa
                          AND n.NCF LIKE @prefijo + N'%'
                    ) x
                    WHERE Num IS NOT NULL
                      AND Num >= @ini AND Num <= @fin
                )
                UPDATE a
                SET SecuenciaActual = mh.MaxNum + 1
                FROM dbo.SecuenciaECFAsignacion a
                CROSS JOIN maxHist mh
                WHERE a.IdAsignacion = @idAsignacion
                  AND a.Activo = 1
                  AND mh.MaxNum >= @ini
                  AND a.SecuenciaActual < mh.MaxNum + 1
                  AND mh.MaxNum + 1 <= a.SecuenciaFinal;";

            await _ctx.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@idEmpresa", idEmpresa),
                new SqlParameter("@tipoEcf", tipoEcfDgii),
                new SqlParameter("@idAsignacion", idAsignacion));
        }

        private async Task ValidarSolapeAutorizacionAsync(
            int idEmpresa, int tipoEcfDgii, int inicial, int final, int? idExcluir)
        {
            if (tipoEcfDgii <= 0)
                throw new InvalidOperationException("Tipo e-CF inválido.");

            var otras = await _ctx.SecuenciasECF.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa
                    && s.TipoEcfDgii == tipoEcfDgii
                    && s.Activo
                    && (idExcluir == null || s.IdSecuencia != idExcluir))
                .ToListAsync();

            var solape = otras.FirstOrDefault(s =>
                inicial <= s.SecuenciaFinal && final >= s.SecuenciaInicial);
            if (solape != null)
            {
                throw new InvalidOperationException(
                    $"El rango {inicial}–{final} se cruza con otra autorización e{tipoEcfDgii} de la empresa ({solape.SecuenciaInicial}–{solape.SecuenciaFinal}).");
            }
        }

        private async Task ValidarAsignacionAsync(
            SecuenciaECF parent,
            int idSucursal,
            int inicial,
            int final,
            int? idExcluir)
        {
            if (idSucursal <= 0)
                throw new InvalidOperationException("Indique la sucursal que usará este rango.");

            var suc = await _ctx.Sucursales.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSucursal == idSucursal && s.IdEmpresa == parent.IdEmpresa);
            if (suc == null)
                throw new InvalidOperationException("La sucursal no pertenece a esta empresa.");

            if (inicial < parent.SecuenciaInicial || final > parent.SecuenciaFinal)
            {
                throw new InvalidOperationException(
                    $"El rango {inicial}–{final} debe estar dentro de la autorización de la empresa ({parent.SecuenciaInicial}–{parent.SecuenciaFinal}).");
            }

            var tipo = parent.TipoEcfDgii ?? 0;
            var otras = await _ctx.SecuenciaECFAsignaciones.AsNoTracking()
                .Where(a => a.IdEmpresa == parent.IdEmpresa
                    && a.TipoEcfDgii == tipo
                    && a.Activo
                    && (idExcluir == null || a.IdAsignacion != idExcluir))
                .ToListAsync();

            if (otras.Any(a => a.IdSucursal == idSucursal))
            {
                throw new InvalidOperationException(
                    $"Ya hay un rango e{tipo} activo para esa sucursal. Desactive el anterior o edítelo.");
            }

            var solape = otras.FirstOrDefault(a =>
                inicial <= a.SecuenciaFinal && final >= a.SecuenciaInicial);
            if (solape != null)
            {
                throw new InvalidOperationException(
                    $"El rango {inicial}–{final} se cruza con el de otra sucursal ({solape.SecuenciaInicial}–{solape.SecuenciaFinal}). Los e-NCF son de la empresa y no pueden repetirse.");
            }
        }

        private async Task<bool> EmpresaTieneVariasSucursalesAsync(int idEmpresa)
        {
            return await _ctx.Sucursales.CountAsync(s => s.IdEmpresa == idEmpresa && s.Activa) > 1;
        }

        private async Task<Dictionary<int, string>> NombresSucursalAsync(int idEmpresa)
        {
            return await _ctx.Sucursales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa)
                .ToDictionaryAsync(s => s.IdSucursal, s => s.Nombre);
        }

        private static string MensajeSinSecuencia(int idEmpresa, int tipoEcfDgii, int? idSucursal)
        {
            if (idSucursal is > 0)
            {
                return $"Esta sucursal no tiene un rango e{tipoEcfDgii} asignado. En FE → Secuencias e-CF asigne un tramo de la autorización de la empresa.";
            }

            return $"No hay secuencia e-CF activa disponible para empresa {idEmpresa}, tipo {tipoEcfDgii}";
        }

        private static SecuenciaEcfDisponibleDto ToDisponible(
            SecuenciaECF s, int actual, int final, DateTime ahora)
        {
            var tipo = s.TipoEcfDgii ?? 0;
            return new SecuenciaEcfDisponibleDto
            {
                TipoEcfDgii = tipo,
                Descripcion = DescripcionPorTipo(tipo),
                Serie = s.Serie,
                Restantes = Math.Max(0, final - actual + 1),
                StockMinimo = s.stockMinimo,
                Agotada = actual > final,
                Vencida = s.fechaVencimiento <= ahora,
                FechaVencimiento = s.fechaVencimiento
            };
        }

        private static SecuenciaEcfDto MapToDto(
            SecuenciaECF s,
            List<SecuenciaECFAsignacion> asignaciones,
            Dictionary<int, string> sucursales)
        {
            var activas = asignaciones.Where(a => a.Activo).OrderBy(a => a.SecuenciaInicial).ToList();
            var (sinAsignar, huecoIni, huecoFin) = CalcularHueco(s.SecuenciaInicial, s.SecuenciaFinal, activas);

            return new SecuenciaEcfDto
            {
                IdSecuencia = s.IdSecuencia,
                IdEmpresa = s.IdEmpresa,
                TipoEcfDgii = s.TipoEcfDgii ?? 0,
                TipoNCF = s.TipoNCF,
                Descripcion = DescripcionPorTipo(s.TipoEcfDgii ?? 0),
                Serie = s.Serie,
                SecuenciaInicial = s.SecuenciaInicial,
                SecuenciaActual = s.SecuenciaActual,
                ProximaSecuencia = s.SecuenciaActual,
                SecuenciaFinal = s.SecuenciaFinal,
                FechaVencimiento = s.fechaVencimiento,
                StockMinimo = s.stockMinimo,
                Activo = s.Activo,
                Ambiente = s.Ambiente,
                FechaCreacion = s.FechaCreacion,
                Asignaciones = asignaciones
                    .OrderByDescending(a => a.Activo)
                    .ThenBy(a => a.SecuenciaInicial)
                    .Select(a => MapAsignacion(a, sucursales))
                    .ToList(),
                NumerosSinAsignar = sinAsignar,
                SiguienteHuecoInicial = huecoIni,
                SiguienteHuecoFinal = huecoFin
            };
        }

        private static SecuenciaEcfAsignacionDto MapAsignacion(
            SecuenciaECFAsignacion a, Dictionary<int, string> sucursales) => new()
        {
            IdAsignacion = a.IdAsignacion,
            IdSecuencia = a.IdSecuencia,
            IdSucursal = a.IdSucursal,
            NombreSucursal = sucursales.TryGetValue(a.IdSucursal, out var n) ? n : null,
            SecuenciaInicial = a.SecuenciaInicial,
            SecuenciaActual = a.SecuenciaActual,
            ProximaSecuencia = a.SecuenciaActual,
            SecuenciaFinal = a.SecuenciaFinal,
            Activo = a.Activo
        };

        private static (int sinAsignar, int? huecoIni, int? huecoFin) CalcularHueco(
            int inicial, int final, List<SecuenciaECFAsignacion> activas)
        {
            var cubiertos = 0;
            int? huecoIni = null;
            int? huecoFin = null;
            var cursor = inicial;

            foreach (var a in activas)
            {
                if (a.SecuenciaInicial > cursor)
                {
                    huecoIni ??= cursor;
                    huecoFin ??= a.SecuenciaInicial - 1;
                }

                var from = Math.Max(a.SecuenciaInicial, inicial);
                var to = Math.Min(a.SecuenciaFinal, final);
                if (to >= from)
                    cubiertos += to - from + 1;

                cursor = Math.Max(cursor, a.SecuenciaFinal + 1);
            }

            if (cursor <= final)
            {
                huecoIni ??= cursor;
                huecoFin ??= final;
            }

            var total = Math.Max(0, final - inicial + 1);
            return (Math.Max(0, total - cubiertos), huecoIni, huecoFin);
        }

        private static void ValidarRango(int inicial, int proxima, int final)
        {
            if (inicial < 1)
                throw new InvalidOperationException("La secuencia inicial debe ser mayor o igual a 1.");
            if (final < inicial)
                throw new InvalidOperationException("La secuencia final debe ser mayor o igual a la inicial. Ejemplo: del 1 al 10.");
            if (proxima < inicial || proxima > final)
                throw new InvalidOperationException(
                    $"La próxima secuencia ({proxima}) debe estar entre la inicial ({inicial}) y la final ({final}).");
        }

        private static string DescripcionPorTipo(int tipo) => tipo switch
        {
            31 => "Factura de Crédito Fiscal Electrónica",
            32 => "Factura de Consumo Electrónica",
            33 => "Nota de Débito Electrónica",
            34 => "Nota de Crédito Electrónica",
            41 => "Compras Electrónica",
            43 => "Gastos Menores Electrónica",
            44 => "Regímenes Especiales Electrónica",
            45 => "Gubernamental Electrónica",
            46 => "Exportaciones Electrónica",
            47 => "Pagos al Exterior Electrónica",
            _ => $"Tipo {tipo}"
        };

        private sealed class CursorEmision
        {
            public SecuenciaECF Parent { get; init; } = null!;
            public SecuenciaECFAsignacion? Asignacion { get; init; }
            public int Actual => Asignacion?.SecuenciaActual ?? Parent.SecuenciaActual;
            public int Final => Asignacion?.SecuenciaFinal ?? Parent.SecuenciaFinal;
        }
    }
}
