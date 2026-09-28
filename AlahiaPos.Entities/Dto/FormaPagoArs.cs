using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.Entities.Dto
{
    public static class FormaPagoArs
    {
        public const string Metodo = "ARS";
        public const string ParametroClave = "UTILIZAR_ARS";

        public const string EstadoPendiente = "Pendiente";
        public const string EstadoParcial = "Parcial";
        public const string EstadoPagada = "Pagada";
        public const string EstadoAnulada = "Anulada";

        public static bool EsArs(string? metodo)
        {
            var texto = (metodo ?? string.Empty).Trim();
            return texto.Equals(Metodo, StringComparison.OrdinalIgnoreCase)
                || texto.Equals("Aseguradora", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ParametroActivo(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim().ToLowerInvariant();
            return texto is "true" or "1" or "si" or "sí";
        }

        public static string ResolverEstado(decimal cubierto, decimal pagado, decimal pendiente, bool anulada)
        {
            if (anulada)
                return EstadoAnulada;

            if (cubierto <= 0.009m)
                return string.Empty;

            if (pendiente <= 0.009m)
                return EstadoPagada;

            if (pagado > 0.009m)
                return EstadoParcial;

            return EstadoPendiente;
        }

        public static void RecalcularSaldos(FacturaHeaders header)
        {
            if (header == null)
                return;

            header.MontoCubiertoArs = Math.Round(header.MontoCubiertoArs, 2);
            header.PagadoArs = Math.Round(header.PagadoArs, 2);
            if (header.PagadoArs < 0)
                header.PagadoArs = 0;
            if (header.PagadoArs > header.MontoCubiertoArs)
                header.PagadoArs = header.MontoCubiertoArs;

            header.PendienteArs = Math.Round(header.MontoCubiertoArs - header.PagadoArs, 2);
            if (header.PendienteArs < 0)
                header.PendienteArs = 0;

            var pendienteCliente = Math.Round(header.Total - header.Pagado - header.MontoCubiertoArs, 2);
            if (pendienteCliente < 0)
                pendienteCliente = 0;
            header.Pendiente = pendienteCliente;

            if (header.EstaCancelada)
            {
                header.Pendiente = 0;
                header.PendienteArs = 0;
                header.Estado = EstadoAnulada;
                header.EstadoArs = header.MontoCubiertoArs > 0.009m ? EstadoAnulada : header.EstadoArs;
                return;
            }

            header.EstadoArs = ResolverEstado(
                header.MontoCubiertoArs,
                header.PagadoArs,
                header.PendienteArs,
                false);

            header.Estado = header.Pendiente > 0.009m ? EstadoPendiente : "Pagada";
        }

        public static List<(int IdFactura, decimal Monto)> DistribuirPagoLote(
            IEnumerable<(int IdFactura, decimal Pendiente)> documentos,
            decimal monto)
        {
            var docs = (documentos ?? Enumerable.Empty<(int, decimal)>())
                .Where(d => d.IdFactura > 0 && d.Pendiente > 0.009m)
                .ToList();

            var aplicado = new List<(int IdFactura, decimal Monto)>();
            if (docs.Count == 0)
                return aplicado;

            var totalPendiente = Math.Round(docs.Sum(d => d.Pendiente), 2);
            var porAplicar = monto <= 0.009m
                ? totalPendiente
                : Math.Round(monto, 2);

            if (porAplicar > totalPendiente)
                porAplicar = totalPendiente;

            var resto = porAplicar;
            foreach (var doc in docs)
            {
                if (resto <= 0.009m)
                    break;
                var slice = Math.Round(Math.Min(doc.Pendiente, resto), 2);
                if (slice <= 0.009m)
                    continue;
                aplicado.Add((doc.IdFactura, slice));
                resto = Math.Round(resto - slice, 2);
            }

            return aplicado;
        }
    }
}
