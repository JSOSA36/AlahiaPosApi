using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Reglas de facturación cuando la empresa activa
    /// <see cref="ClaveParametro"/> en Parámetros.
    /// No reserva e-NCF ni escribe comprobantes: solo decide.
    /// </summary>
    public static class PoliticaFiscalAutomatica
    {
        public const string ClaveParametro = "AplicarPoliticaFiscalAutomatica";
        public const string ClaveFacturarConItbis = "FACTURAR_CON_ITBIS";
        public const string ClaveFacturacionElectronica = "FACTURACION_ELECTRONICA";

        public const string TipoConsumo = "Consumidor Final";
        public const int TipoEcfConsumo = 32;
        public const int TipoEcfCreditoFiscal = 31;
        public const int TipoEcfGubernamental = 45;

        public static bool ValorActivo(string? valor)
        {
            var v = (valor ?? "").Trim();
            return v.Equals("true", StringComparison.OrdinalIgnoreCase)
                || v == "1";
        }

        /// <summary>
        /// Fila de sucursal gana sobre la de empresa. Sin fila, la política queda apagada.
        /// </summary>
        public static string? ResolverValor(IEnumerable<ParametroFiscalValor>? rows, int? idSucursal)
        {
            var lista = rows?.ToList() ?? new List<ParametroFiscalValor>();
            if (idSucursal is > 0 && lista.Any(r => r.IdSucursal == idSucursal))
                return lista.First(r => r.IdSucursal == idSucursal).Valor;

            return lista.FirstOrDefault(r => r.IdSucursal is null or 0).Valor;
        }

        /// <summary>
        /// Billetera BHD del catálogo real: "Billet BHD".
        /// "BHD" y "Transferencia BHD" son banco, no la billetera.
        /// </summary>
        public static bool EsBilleteraBhd(string? metodo)
        {
            var n = CargoPagoMetodoMatcher.Normalizar(metodo);
            if (n.Length == 0 || n.Contains("TRANSFER"))
                return false;

            return (n.Contains("BILLET") && n.Contains("BHD"))
                || (n.Contains("BILLETERA") && n.Contains("BHD"));
        }

        /// <summary>
        /// Tarjeta según el agrupador de cargos (Tarjeta, Visa, Azul, Cardnet…).
        /// La billetera BHD no se trata como tarjeta.
        /// </summary>
        public static bool EsTarjeta(string? metodo)
        {
            if (EsBilleteraBhd(metodo))
                return false;

            return CargoPagoMetodoMatcher.EsTarjeta(CargoPagoMetodoMatcher.Normalizar(metodo));
        }

        public static bool RncValido(string? rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc))
                return false;
            var digits = new string(rnc.Where(char.IsDigit).ToArray());
            if (digits.Length is not (9 or 11))
                return false;
            return digits.Any(c => c != '0');
        }

        /// <summary>
        /// Crédito fiscal con RNC, o gubernamental: no se pisa por la regla de consumo.
        /// </summary>
        public static bool SeleccionLegitima(string? tipoComprobante, int? tipoEcf, string? rnc)
        {
            if (tipoEcf == TipoEcfGubernamental || EsGubernamental(tipoComprobante))
                return true;

            var credito = tipoEcf == TipoEcfCreditoFiscal || EsCreditoFiscal(tipoComprobante);
            return credito && RncValido(rnc);
        }

        /// <summary>
        /// Solo bloquea pasar FACTURAR_CON_ITBIS de activo a inactivo.
        /// Una empresa que ya factura sin ITBIS puede guardar la política.
        /// </summary>
        public static bool BloqueaApagadoItbisGlobal(
            bool politicaActiva,
            bool itbisEstabaActivo,
            string? clave,
            string? valorNuevo)
        {
            if (!politicaActiva || !itbisEstabaActivo)
                return false;
            if (!string.Equals((clave ?? "").Trim(), ClaveFacturarConItbis, StringComparison.OrdinalIgnoreCase))
                return false;
            return !ValorActivo(valorNuevo);
        }

        public static ResultadoPoliticaFiscal Evaluar(EntradaPoliticaFiscal entrada)
        {
            var tipo = entrada.TipoComprobante ?? "";
            var tipoEcf = entrada.TipoEcfDgii;

            if (!entrada.PoliticaActiva)
            {
                return Passthrough(tipo, tipoEcf, "Ninguna", null);
            }

            if (entrada.EmpresaFacturaConItbis && IntentoApagarItbisEnLineas(entrada.Lineas))
            {
                return new ResultadoPoliticaFiscal
                {
                    Rechazar = true,
                    Mensaje = "La política fiscal automática no permite desactivar el ITBIS de la venta.",
                    TipoComprobante = tipo,
                    TipoEcfDgii = tipoEcf,
                    Regla = "ItbisBloqueado",
                    ConsumioSecuencia = false
                };
            }

            var disparo = MetodoDisparo(entrada.Pagos);
            if (disparo.Metodo == null)
                return Passthrough(tipo, tipoEcf, "Ninguna", null);

            if (SeleccionLegitima(tipo, tipoEcf, entrada.Rnc))
            {
                return Passthrough(tipo, tipoEcf, "ExcepcionAutorizada", disparo.Metodo);
            }

            if (!entrada.FacturacionElectronica)
            {
                return Passthrough(tipo, tipoEcf, "OmitidaSinFacturacionElectronica", disparo.Metodo);
            }

            return new ResultadoPoliticaFiscal
            {
                Rechazar = false,
                ForzarConsumo = true,
                TipoComprobante = TipoConsumo,
                TipoEcfDgii = TipoEcfConsumo,
                Regla = disparo.Regla,
                MetodoPago = disparo.Metodo,
                ConsumioSecuencia = false
            };
        }

        private static bool IntentoApagarItbisEnLineas(IReadOnlyList<LineaPoliticaFiscal>? lineas)
        {
            if (lineas == null || lineas.Count == 0)
                return false;

            var gravadas = lineas.Where(l => l.GravaItbis).ToList();
            if (gravadas.Count == 0)
                return false;

            return gravadas.All(l => l.Itbis <= 0.009m);
        }

        private static (string? Metodo, string Regla) MetodoDisparo(
            IReadOnlyList<PagoPoliticaFiscal>? pagos)
        {
            var vigentes = (pagos ?? Array.Empty<PagoPoliticaFiscal>())
                .Where(p => p.Monto > 0.009m)
                .ToList();

            var billetera = vigentes.FirstOrDefault(p => EsBilleteraBhd(p.Metodo));
            if (billetera != null)
                return (billetera.Metodo, "BilleteraBhd");

            var tarjeta = vigentes.FirstOrDefault(p => EsTarjeta(p.Metodo));
            if (tarjeta != null)
                return (tarjeta.Metodo, "Tarjeta");

            return (null, "Ninguna");
        }

        private static ResultadoPoliticaFiscal Passthrough(
            string tipo,
            int? tipoEcf,
            string regla,
            string? metodo)
        {
            return new ResultadoPoliticaFiscal
            {
                Rechazar = false,
                ForzarConsumo = false,
                TipoComprobante = tipo,
                TipoEcfDgii = tipoEcf,
                Regla = regla,
                MetodoPago = metodo,
                ConsumioSecuencia = false
            };
        }

        private static bool EsGubernamental(string? tipo)
        {
            var t = (tipo ?? "").Trim();
            return t.Equals("Gubernamental", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsCreditoFiscal(string? tipo)
        {
            var t = (tipo ?? "").Trim();
            return t.Equals("Crédito Fiscal", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Credito Fiscal", StringComparison.OrdinalIgnoreCase);
        }
    }

    public readonly struct ParametroFiscalValor
    {
        public ParametroFiscalValor(int? idSucursal, string? valor)
        {
            IdSucursal = idSucursal;
            Valor = valor;
        }

        public int? IdSucursal { get; }
        public string? Valor { get; }
    }

    public sealed class LineaPoliticaFiscal
    {
        public bool GravaItbis { get; set; }
        public decimal Itbis { get; set; }
    }

    public sealed class PagoPoliticaFiscal
    {
        public string? Metodo { get; set; }
        public decimal Monto { get; set; }
    }

    public sealed class EntradaPoliticaFiscal
    {
        public bool PoliticaActiva { get; set; }
        public bool EmpresaFacturaConItbis { get; set; }
        public bool FacturacionElectronica { get; set; }
        public string? TipoComprobante { get; set; }
        public int? TipoEcfDgii { get; set; }
        public string? Rnc { get; set; }
        public IReadOnlyList<PagoPoliticaFiscal> Pagos { get; set; } = Array.Empty<PagoPoliticaFiscal>();
        public IReadOnlyList<LineaPoliticaFiscal> Lineas { get; set; } = Array.Empty<LineaPoliticaFiscal>();
    }

    public sealed class ResultadoPoliticaFiscal
    {
        public bool Rechazar { get; set; }
        public string? Mensaje { get; set; }
        public bool ForzarConsumo { get; set; }
        public string? TipoComprobante { get; set; }
        public int? TipoEcfDgii { get; set; }
        public string Regla { get; set; } = "Ninguna";
        public string? MetodoPago { get; set; }
        public bool ConsumioSecuencia { get; set; }
    }
}
