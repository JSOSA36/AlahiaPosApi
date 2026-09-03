using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CargoPagoService : ICargoPagoService
    {
        private readonly AlahiaPosContext _db;

        public CargoPagoService(AlahiaPosContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<CargoPagoRegla>> ListarAsync(
            int idEmpresa,
            CancellationToken ct = default)
        {
            return await _db.CargoPagoReglas
                .AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.IdCargoPagoRegla)
                .ToListAsync(ct);
        }

        public async Task<CargoPagoRegla> GuardarAsync(
            CargoPagoRegla regla,
            CancellationToken ct = default)
        {
            if (regla == null)
                throw new ArgumentNullException(nameof(regla));
            if (regla.IdEmpresa <= 0)
                throw new ArgumentOutOfRangeException(nameof(regla.IdEmpresa));

            regla.Nombre = (regla.Nombre ?? "").Trim();
            if (regla.Nombre.Length == 0)
                throw new InvalidOperationException("Indique el nombre del cargo.");

            regla.Tipo = NormalizarTipo(regla.Tipo);
            regla.GrupoMetodo = NormalizarGrupo(regla.GrupoMetodo);

            if (regla.Valor < 0)
                throw new InvalidOperationException("El valor del cargo no puede ser negativo.");
            if (regla.Tipo == CargoPagoTipos.Porcentaje && regla.Valor > 100)
                throw new InvalidOperationException("El porcentaje no puede superar 100.");

            if (regla.Orden <= 0)
                regla.Orden = 1;

            if (regla.IdCargoPagoRegla <= 0)
            {
                regla.FechaInseccion = DateTime.Now;
                _db.CargoPagoReglas.Add(regla);
            }
            else
            {
                var existente = await _db.CargoPagoReglas
                    .FirstOrDefaultAsync(
                        x => x.IdCargoPagoRegla == regla.IdCargoPagoRegla
                             && x.IdEmpresa == regla.IdEmpresa,
                        ct)
                    ?? throw new InvalidOperationException("Regla de cargo no encontrada.");

                existente.Nombre = regla.Nombre;
                existente.Tipo = regla.Tipo;
                existente.Valor = regla.Valor;
                existente.GrupoMetodo = regla.GrupoMetodo;
                existente.Activo = regla.Activo;
                existente.Orden = regla.Orden;
                regla = existente;
            }

            await _db.SaveChangesAsync(ct);
            return regla;
        }

        public async Task EliminarAsync(int idEmpresa, int id, CancellationToken ct = default)
        {
            var existente = await _db.CargoPagoReglas
                .FirstOrDefaultAsync(
                    x => x.IdCargoPagoRegla == id && x.IdEmpresa == idEmpresa,
                    ct);
            if (existente == null)
                return;

            _db.CargoPagoReglas.Remove(existente);
            await _db.SaveChangesAsync(ct);
        }

        public CargoPagoCalcularResult Calcular(
            IEnumerable<CargoPagoRegla> reglas,
            IEnumerable<string?> metodos,
            decimal baseCalculo)
        {
            var result = new CargoPagoCalcularResult
            {
                BaseCalculo = Math.Round(Math.Max(0, baseCalculo), 2)
            };

            var medios = (metodos ?? Enumerable.Empty<string?>())
                .Where(m => !string.IsNullOrWhiteSpace(m)
                            && !FormaPagoNotaCredito.EsNotaCredito(m))
                .Select(m => m!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (result.BaseCalculo <= 0)
            {
                result.MontoCargo = 0;
                result.TotalConCargo = 0;
                return result;
            }

            if (medios.Count == 0)
            {
                // Sin métodos de pago válidos para disparar reglas:
                // el cargo es 0 y el total debe quedarse en el base.
                result.MontoCargo = 0;
                result.TotalConCargo = result.BaseCalculo;
                return result;
            }

            foreach (var regla in (reglas ?? Enumerable.Empty<CargoPagoRegla>())
                         .Where(r => r.Activo)
                         .OrderBy(r => r.Orden)
                         .ThenBy(r => r.IdCargoPagoRegla))
            {
                var disparador = medios.FirstOrDefault(m =>
                    CargoPagoMetodoMatcher.Coincide(regla.GrupoMetodo, m));
                if (disparador == null)
                    continue;

                var monto = regla.Tipo == CargoPagoTipos.MontoFijo
                    ? Math.Round(regla.Valor, 2)
                    : Math.Round(result.BaseCalculo * (regla.Valor / 100m), 2);

                if (monto <= 0)
                    continue;

                result.Cargos.Add(new CargoPagoAplicadoDto
                {
                    IdCargoPagoRegla = regla.IdCargoPagoRegla > 0 ? regla.IdCargoPagoRegla : null,
                    Nombre = regla.Nombre,
                    Tipo = regla.Tipo,
                    Valor = regla.Valor,
                    BaseCalculo = result.BaseCalculo,
                    Monto = monto,
                    MetodoPago = disparador
                });
            }

            result.MontoCargo = Math.Round(result.Cargos.Sum(c => c.Monto), 2);
            result.TotalConCargo = Math.Round(result.BaseCalculo + result.MontoCargo, 2);
            return result;
        }

        public async Task<CargoPagoCalcularResult> CalcularAsync(
            int idEmpresa,
            IEnumerable<string?> metodos,
            decimal baseCalculo,
            CancellationToken ct = default)
        {
            var reglas = await _db.CargoPagoReglas
                .AsNoTracking()
                .Where(x => x.IdEmpresa == idEmpresa && x.Activo)
                .OrderBy(x => x.Orden)
                .ToListAsync(ct);

            return Calcular(reglas, metodos, baseCalculo);
        }

        public async Task<CargoPagoCalcularResult> AplicarEnFacturaAsync(
            FacturaHeaders header,
            IEnumerable<string?> metodos,
            CancellationToken ct = default)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            var baseSinCargo = Math.Round(header.Total - header.MontoCargo, 2);
            if (baseSinCargo < 0)
                baseSinCargo = 0;

            var existentes = await _db.FacturaCargos
                .Where(x => x.IdFacturaHeader == header.IdFacturaHeader)
                .ToListAsync(ct);
            if (existentes.Count > 0)
                _db.FacturaCargos.RemoveRange(existentes);

            var resultado = await CalcularAsync(header.IdEmpresa, metodos, baseSinCargo, ct);

            header.MontoCargo = resultado.MontoCargo;
            header.Total = resultado.TotalConCargo;

            foreach (var cargo in resultado.Cargos)
            {
                _db.FacturaCargos.Add(new FacturaCargo
                {
                    IdFacturaHeader = header.IdFacturaHeader,
                    IdCargoPagoRegla = cargo.IdCargoPagoRegla,
                    Nombre = cargo.Nombre,
                    Tipo = cargo.Tipo,
                    Valor = cargo.Valor,
                    BaseCalculo = cargo.BaseCalculo,
                    Monto = cargo.Monto,
                    MetodoPago = cargo.MetodoPago,
                    IdEmpresa = header.IdEmpresa,
                    FechaInseccion = DateTime.Now
                });
            }

            await _db.SaveChangesAsync(ct);
            return resultado;
        }

        private static string NormalizarTipo(string? tipo)
        {
            var t = (tipo ?? "").Trim().ToUpperInvariant();
            return t == CargoPagoTipos.MontoFijo
                ? CargoPagoTipos.MontoFijo
                : CargoPagoTipos.Porcentaje;
        }

        private static string NormalizarGrupo(string? grupo)
        {
            var g = (grupo ?? "").Trim().ToUpperInvariant();
            return g switch
            {
                "EFECTIVO" => CargoPagoGrupos.Efectivo,
                "TRANSFERENCIA" => CargoPagoGrupos.Transferencia,
                "CHEQUE" => CargoPagoGrupos.Cheque,
                "TODOS" => CargoPagoGrupos.Todos,
                _ => CargoPagoGrupos.Tarjeta
            };
        }
    }
}
