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
            return await _ctx.SecuenciasECF
                .Where(s => s.IdEmpresa == idEmpresa && s.TipoEcfDgii > 0)
                .AsNoTracking()
                .OrderBy(s => s.TipoEcfDgii)
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        public async Task<SecuenciaEcfDto?> GetByIdAsync(int id)
        {
            var s = await _ctx.SecuenciasECF.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdSecuencia == id);
            return s == null ? null : MapToDto(s);
        }

        public async Task<SecuenciaEcfDto> CreateAsync(SecuenciaEcfCreateDto dto)
        {
            var entity = new SecuenciaECF
            {
                IdEmpresa = dto.IdEmpresa,
                TipoEcfDgii = dto.TipoEcfDgii,
                TipoNCF = $"E{dto.TipoEcfDgii}",
                Descripcion = dto.Descripcion ?? DescripcionPorTipo(dto.TipoEcfDgii),
                Serie = dto.Serie,
                SecuenciaInicial = dto.SecuenciaInicial,
                SecuenciaActual = dto.SecuenciaInicial,
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
            return MapToDto(entity);
        }

        public async Task UpdateAsync(int id, SecuenciaEcfUpdateDto dto)
        {
            var entity = await _ctx.SecuenciasECF.FindAsync(id)
                ?? throw new InvalidOperationException($"SecuenciaECF {id} no encontrada");

            if (dto.SecuenciaFinal.HasValue)
                entity.SecuenciaFinal = dto.SecuenciaFinal.Value;
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
            if (entity != null)
            {
                entity.Activo = false;
                await _ctx.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Reserva atómica via SQL UPDATE TOP(1)...OUTPUT con ADO.NET directo.
        /// Segura bajo concurrencia (múltiples cajas simultáneas).
        /// </summary>
        public async Task<ReservaEcfResultado> ReservarSiguienteAsync(int idEmpresa, int tipoEcfDgii)
        {
            const string sql = @"
                UPDATE TOP(1) SecuenciasECF
                SET SecuenciaActual = SecuenciaActual + 1
                OUTPUT
                    inserted.Serie,
                    inserted.SecuenciaActual - 1 AS NumeroReservado,
                    inserted.SecuenciaFinal
                WHERE IdEmpresa = @idEmpresa
                  AND TipoEcfDgii = @tipoEcf
                  AND Activo = 1
                  AND SecuenciaActual <= SecuenciaFinal
                  AND fechaVencimiento > GETDATE()";

            var conn = _ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@idEmpresa", idEmpresa));
            cmd.Parameters.Add(new SqlParameter("@tipoEcf", tipoEcfDgii));

            if (_ctx.Database.CurrentTransaction != null)
                cmd.Transaction = _ctx.Database.CurrentTransaction.GetDbTransaction();

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return ReservaEcfResultado.Fallo(
                    $"No hay secuencia e-CF activa disponible para empresa {idEmpresa}, tipo {tipoEcfDgii}");
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

        public async Task<string?> PeekSiguienteAsync(int idEmpresa, int tipoEcfDgii)
        {
            var seq = await _ctx.SecuenciasECF
                .AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa
                    && s.TipoEcfDgii == tipoEcfDgii
                    && s.Activo
                    && s.SecuenciaActual <= s.SecuenciaFinal
                    && s.fechaVencimiento > DateTime.Now)
                .FirstOrDefaultAsync();

            if (seq == null) return null;
            return $"{seq.Serie}{seq.SecuenciaActual:D10}";
        }

        public async Task<SecuenciaAlertaDto> ValidarDisponibilidadAsync(int idEmpresa, int tipoEcfDgii)
        {
            var seq = await _ctx.SecuenciasECF
                .AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.TipoEcfDgii == tipoEcfDgii && s.Activo)
                .FirstOrDefaultAsync();

            if (seq == null)
            {
                return new SecuenciaAlertaDto
                {
                    TipoEcfDgii = tipoEcfDgii,
                    Disponible = false,
                    MensajeAlerta = "No existe secuencia configurada"
                };
            }

            int restantes = seq.SecuenciaFinal - seq.SecuenciaActual + 1;
            bool agotada = restantes <= 0;
            bool vencida = seq.fechaVencimiento <= DateTime.Now;
            bool stockBajo = restantes > 0 && restantes <= seq.stockMinimo;
            bool proximaAVencer = !vencida && seq.fechaVencimiento <= DateTime.Now.AddDays(30);

            string? alerta = null;
            if (agotada) alerta = "Secuencia agotada";
            else if (vencida) alerta = "Secuencia vencida";
            else if (stockBajo) alerta = $"Stock bajo: {restantes} restantes";
            else if (proximaAVencer) alerta = "Próxima a vencer";

            return new SecuenciaAlertaDto
            {
                TipoEcfDgii = tipoEcfDgii,
                Descripcion = seq.Descripcion ?? seq.TipoNCF,
                Disponible = !agotada && !vencida,
                Restantes = Math.Max(0, restantes),
                StockBajo = stockBajo,
                ProximaAVencer = proximaAVencer,
                MensajeAlerta = alerta
            };
        }

        public async Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerDisponiblesAsync(int idEmpresa)
        {
            return await _ctx.SecuenciasECF
                .Where(s => s.IdEmpresa == idEmpresa && s.Activo && s.TipoEcfDgii > 0)
                .AsNoTracking()
                .Select(s => new SecuenciaEcfDisponibleDto
                {
                    TipoEcfDgii = s.TipoEcfDgii ?? 0,
                    Descripcion = s.Descripcion ?? s.TipoNCF,
                    Serie = s.Serie,
                    Restantes = Math.Max(0, s.SecuenciaFinal - s.SecuenciaActual + 1),
                    StockMinimo = s.stockMinimo,
                    Agotada = s.SecuenciaActual > s.SecuenciaFinal,
                    Vencida = s.fechaVencimiento <= DateTime.Now,
                    FechaVencimiento = s.fechaVencimiento
                })
                .OrderBy(d => d.TipoEcfDgii)
                .ToListAsync();
        }

        private static SecuenciaEcfDto MapToDto(SecuenciaECF s) => new()
        {
            IdSecuencia = s.IdSecuencia,
            IdEmpresa = s.IdEmpresa,
            TipoEcfDgii = s.TipoEcfDgii ?? 0,
            TipoNCF = s.TipoNCF,
            Descripcion = s.Descripcion ?? s.TipoNCF,
            Serie = s.Serie,
            SecuenciaInicial = s.SecuenciaInicial,
            SecuenciaActual = s.SecuenciaActual,
            SecuenciaFinal = s.SecuenciaFinal,
            FechaVencimiento = s.fechaVencimiento,
            StockMinimo = s.stockMinimo,
            Activo = s.Activo,
            Ambiente = s.Ambiente,
            FechaCreacion = s.FechaCreacion
        };

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
    }

}
