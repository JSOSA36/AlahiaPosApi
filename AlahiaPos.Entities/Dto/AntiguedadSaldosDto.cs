using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class AntiguedadSaldosFiltroRequest
    {
        public int IdEmpresa { get; set; }
        public int IdTercero { get; set; }
        public string? Documento { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool SoloVencidas { get; set; }
        public bool SoloPendientes { get; set; } = true;
        public DateTime? FechaCorte { get; set; }
    }

    public class AntiguedadSaldosLineaDto
    {
        public int IdDocumento { get; set; }
        public string Documento { get; set; } = "";
        public int IdTercero { get; set; }
        public string TerceroNombre { get; set; } = "";
        public DateTime FechaDocumento { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int DiasVencidos { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal Rango0a30 { get; set; }
        public decimal Rango31a60 { get; set; }
        public decimal Rango61a90 { get; set; }
        public decimal RangoMas90 { get; set; }
        public string RangoCodigo { get; set; } = "0-30";
        public string RangoEtiqueta { get; set; } = "0-30 días";
        public string Estado { get; set; } = "";
    }

    public class AntiguedadSaldosTotalesDto
    {
        public decimal TotalPendiente { get; set; }
        public decimal Total0a30 { get; set; }
        public decimal Total31a60 { get; set; }
        public decimal Total61a90 { get; set; }
        public decimal TotalMas90 { get; set; }
        public int CantidadDocumentos { get; set; }
        public int CantidadTerceros { get; set; }
    }

    public class AntiguedadSaldosIndicadoresDto
    {
        public int TotalTerceros { get; set; }
        public decimal PromedioPorTercero { get; set; }
        public decimal SaldoPromedioDocumento { get; set; }
        public decimal MayorDeuda { get; set; }
        public string? TerceroMayorDeuda { get; set; }
        public int? IdTerceroMayorDeuda { get; set; }
        public decimal PromedioDiasVencidos { get; set; }
    }

    public class AntiguedadSaldosTopTerceroDto
    {
        public int IdTercero { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Saldo { get; set; }
        public int CantidadDocumentos { get; set; }
        public int MaxDiasVencidos { get; set; }
    }

    public class AntiguedadSaldosRangoDto
    {
        public string Codigo { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public decimal Monto { get; set; }
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class AntiguedadSaldosReporteDto
    {
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public DateTime FechaCorte { get; set; }
        public string Tipo { get; set; } = "";
        public List<AntiguedadSaldosLineaDto> Lineas { get; set; } = new();
        public AntiguedadSaldosTotalesDto Totales { get; set; } = new();
        public AntiguedadSaldosIndicadoresDto Indicadores { get; set; } = new();
        public List<AntiguedadSaldosRangoDto> Distribucion { get; set; } = new();
        public List<AntiguedadSaldosTopTerceroDto> TopTerceros { get; set; } = new();
    }
}
