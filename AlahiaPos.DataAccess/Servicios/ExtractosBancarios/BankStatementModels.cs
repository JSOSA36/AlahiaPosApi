namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Entrada normalizada para el parseo de un extracto bancario,
    /// independiente del DTO de importación del API.
    /// </summary>
    public sealed class BankStatementParseRequest
    {
        /// <summary>CSV | TXT | PDF | XLSX | XLS (ya resuelto por el llamador).</summary>
        public string Formato { get; init; } = "CSV";

        public string? NombreArchivo { get; init; }

        /// <summary>Contenido textual (CSV/TXT) cuando aplica.</summary>
        public string? ContenidoCsv { get; init; }

        /// <summary>Texto ya extraído (p.ej. de un PDF) cuando aplica.</summary>
        public string? TextoExtraido { get; init; }

        /// <summary>Bytes crudos del archivo (Excel) cuando aplica.</summary>
        public byte[]? ContenidoArchivo { get; init; }
    }

    public sealed class BankStatementLine
    {
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal? Balance { get; set; }
    }

    public sealed class BankStatementParseResult
    {
        /// <summary>Nombre del adapter que produjo el resultado.</summary>
        public string AdapterUsado { get; set; } = string.Empty;

        public string? Banco { get; set; }
        public string? NumeroCuentaBanco { get; set; }
        public string? Moneda { get; set; }
        public DateTime? PeriodoDesde { get; set; }
        public DateTime? PeriodoHasta { get; set; }
        public decimal? SaldoInicial { get; set; }
        public decimal? SaldoFinal { get; set; }
        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }
        public List<BankStatementLine> Lineas { get; } = new();

        /// <summary>Advertencias no bloqueantes detectadas durante el parseo.</summary>
        public List<string> Warnings { get; } = new();
    }
}
