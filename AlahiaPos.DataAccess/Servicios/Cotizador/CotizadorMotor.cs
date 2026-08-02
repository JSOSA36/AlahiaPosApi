using System;
using System.Collections.Generic;
using System.Linq;
using AlahiaPos.Entities.Domain.Cotizador;
using AlahiaPos.Entities.Dto.Cotizador;

namespace AlahiaPos.DataAccess.Servicios.Cotizador
{
    public sealed class DependenciaMatch
    {
        public string CodigoOrigen { get; init; } = string.Empty;
        public string CodigoRequerido { get; init; } = string.Empty;
        public string Tipo { get; init; } = "RECOMIENDA";
        public string Mensaje { get; init; } = string.Empty;
    }

    public static class DependenciaValidator
    {
        public static List<DependenciaMatch> Evaluar(
            IEnumerable<string> seleccionados,
            IEnumerable<ModuloDependencia> dependencias,
            IReadOnlyDictionary<int, string> idToCodigo)
        {
            var set = new HashSet<string>(
                seleccionados.Where(c => !string.IsNullOrWhiteSpace(c)),
                StringComparer.OrdinalIgnoreCase);

            var resultado = new List<DependenciaMatch>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dep in dependencias.Where(d => d.Activo))
            {
                if (!idToCodigo.TryGetValue(dep.ModuloId, out var origen))
                    continue;
                if (!idToCodigo.TryGetValue(dep.ModuloRequeridoId, out var requerido))
                    continue;
                if (!set.Contains(origen))
                    continue;
                if (set.Contains(requerido))
                    continue;

                var key = $"{origen}|{requerido}|{dep.Tipo}";
                if (!vistos.Add(key))
                    continue;

                resultado.Add(new DependenciaMatch
                {
                    CodigoOrigen = origen,
                    CodigoRequerido = requerido,
                    Tipo = string.IsNullOrWhiteSpace(dep.Tipo) ? "RECOMIENDA" : dep.Tipo.Trim().ToUpperInvariant(),
                    Mensaje = string.IsNullOrWhiteSpace(dep.Mensaje)
                        ? $"Para aprovechar {origen} recomendamos incluir {requerido}."
                        : dep.Mensaje!
                });
            }

            return resultado
                .OrderByDescending(x => x.Tipo == "REQUIERE")
                .ThenBy(x => x.CodigoRequerido)
                .ToList();
        }

        public static HashSet<string> ExpandirIncluidos(
            IEnumerable<string> seleccionados,
            IEnumerable<DependenciaMatch> matches)
        {
            var set = new HashSet<string>(
                seleccionados.Where(c => !string.IsNullOrWhiteSpace(c)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var m in matches.Where(x => x.Tipo == "REQUIERE"))
                set.Add(m.CodigoRequerido);

            return set;
        }
    }

    public sealed class PrecioCalculoInput
    {
        public IReadOnlyList<(string Codigo, string Nombre, decimal PrecioBase, bool ParticipaPrecio)> Modulos { get; init; }
            = Array.Empty<(string, string, decimal, bool)>();

        public int Usuarios { get; init; }
        public int Sucursales { get; init; }
        public bool UsaFacturacionElectronica { get; init; }
        public int DocumentosElectronicosMensuales { get; init; }
        public int UsuariosIncluidos { get; init; } = 1;
        public decimal PrecioUsuarioExtra { get; init; }
        public decimal PrecioSucursalExtra { get; init; }
        public decimal SucursalesIncluidas { get; init; } = 1;
        public decimal PisoMensualUSD { get; init; } = 30m;
        public IReadOnlyList<CotizadorTramoDocumento> Tramos { get; init; }
            = Array.Empty<CotizadorTramoDocumento>();
    }

    public sealed class PrecioCalculoResult
    {
        public List<LineaPrecioDto> Desglose { get; } = new();
        public decimal SubtotalUSD { get; set; }
        public decimal AjustePisoUSD { get; set; }
        public decimal PrecioMensualUSD { get; set; }
        public bool AplicoPisoMinimo { get; set; }
    }

    public static class PrecioCalculator
    {
        public static PrecioCalculoResult Calcular(PrecioCalculoInput input)
        {
            var result = new PrecioCalculoResult();
            decimal subtotal = 0m;

            foreach (var m in input.Modulos.Where(x => x.ParticipaPrecio && x.PrecioBase > 0))
            {
                result.Desglose.Add(new LineaPrecioDto
                {
                    Concepto = m.Nombre,
                    TipoLinea = "MODULO",
                    CodigoModulo = m.Codigo,
                    MontoUSD = m.PrecioBase
                });
                subtotal += m.PrecioBase;
            }

            var usuariosExtra = Math.Max(0, input.Usuarios - Math.Max(1, input.UsuariosIncluidos));
            if (usuariosExtra > 0 && input.PrecioUsuarioExtra > 0)
            {
                var monto = usuariosExtra * input.PrecioUsuarioExtra;
                result.Desglose.Add(new LineaPrecioDto
                {
                    Concepto = $"Usuarios adicionales ({usuariosExtra})",
                    TipoLinea = "USUARIOS",
                    MontoUSD = monto
                });
                subtotal += monto;
            }

            var sucursalesExtra = Math.Max(0, input.Sucursales - (int)Math.Max(1, input.SucursalesIncluidas));
            if (sucursalesExtra > 0 && input.PrecioSucursalExtra > 0)
            {
                var monto = sucursalesExtra * input.PrecioSucursalExtra;
                result.Desglose.Add(new LineaPrecioDto
                {
                    Concepto = $"Sucursales adicionales ({sucursalesExtra})",
                    TipoLinea = "SUCURSALES",
                    MontoUSD = monto
                });
                subtotal += monto;
            }

            if (input.UsaFacturacionElectronica)
            {
                var docs = Math.Max(0, input.DocumentosElectronicosMensuales);
                var tramo = input.Tramos
                    .Where(t => t.Activo)
                    .OrderBy(t => t.Orden)
                    .ThenBy(t => t.DesdeDocs)
                    .FirstOrDefault(t =>
                        docs >= t.DesdeDocs
                        && (!t.HastaDocs.HasValue || docs <= t.HastaDocs.Value));

                if (tramo != null && tramo.CargoUSD > 0)
                {
                    result.Desglose.Add(new LineaPrecioDto
                    {
                        Concepto = tramo.Etiqueta
                            ?? $"Facturación electrónica e-CF (~{docs}/mes)",
                        TipoLinea = "ECF",
                        MontoUSD = tramo.CargoUSD
                    });
                    subtotal += tramo.CargoUSD;
                }
            }

            result.SubtotalUSD = decimal.Round(subtotal, 2);
            var piso = input.PisoMensualUSD <= 0 ? 30m : input.PisoMensualUSD;
            if (result.SubtotalUSD < piso)
            {
                result.AjustePisoUSD = decimal.Round(piso - result.SubtotalUSD, 2);
                result.AplicoPisoMinimo = true;
                result.Desglose.Add(new LineaPrecioDto
                {
                    Concepto = $"Ajuste a precio mínimo mensual ({piso:0.##} USD)",
                    TipoLinea = "AJUSTE_PISO",
                    MontoUSD = result.AjustePisoUSD
                });
                result.PrecioMensualUSD = piso;
            }
            else
            {
                result.PrecioMensualUSD = result.SubtotalUSD;
            }

            return result;
        }
    }

    public static class ExplicacionBuilder
    {
        public static List<string> Construir(
            CotizadorCalcularRequest request,
            IEnumerable<DependenciaMatch> matches,
            PrecioCalculoResult precio,
            IReadOnlyDictionary<string, string> nombres)
        {
            var list = new List<string>();

            foreach (var m in matches)
            {
                var nombreReq = nombres.GetValueOrDefault(m.CodigoRequerido, m.CodigoRequerido);
                var nombreOrig = nombres.GetValueOrDefault(m.CodigoOrigen, m.CodigoOrigen);
                if (!string.IsNullOrWhiteSpace(m.Mensaje))
                    list.Add(m.Mensaje);
                else if (m.Tipo == "REQUIERE")
                    list.Add($"Incluimos {nombreReq} porque es necesario para usar {nombreOrig}.");
                else
                    list.Add($"Recomendamos {nombreReq} para aprovechar mejor {nombreOrig}.");
            }

            if (request.UsaFacturacionElectronica)
            {
                var docs = Math.Max(0, request.DocumentosElectronicosMensuales);
                list.Add(docs <= 0
                    ? "Activó facturación electrónica e-CF. Indique el volumen mensual estimado para afinar el cargo por documentos."
                    : $"Con un volumen estimado de {docs} documentos electrónicos (e-CF) mensuales, este paquete cubre su operación fiscal proyectada.");
            }

            if (precio.AplicoPisoMinimo)
            {
                list.Add(
                    $"El precio mensual se ajustó al piso comercial de {precio.PrecioMensualUSD:0.##} USD (antes de impuestos).");
            }

            if (list.Count == 0)
            {
                list.Add(
                    "Esta propuesta refleja los módulos seleccionados, usuarios, sucursales y volumen fiscal indicado.");
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string Resumen(
            CotizadorCalcularRequest request,
            IEnumerable<string> incluidos,
            decimal precioMensual)
        {
            var mods = incluidos.Count();
            return
                $"Propuesta estimada: {mods} módulo(s), {request.Usuarios} usuario(s), {request.Sucursales} sucursal(es)"
                + (request.UsaFacturacionElectronica
                    ? $", ~{request.DocumentosElectronicosMensuales} e-CF/mes"
                    : "")
                + $". Inversión mensual estimada: {precioMensual:0.##} USD (antes de impuestos).";
        }
    }
}
