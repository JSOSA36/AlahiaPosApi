using System;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    public class SecuenciaEcfDto
    {
        public int IdSecuencia { get; set; }
        public int IdEmpresa { get; set; }
        public int TipoEcfDgii { get; set; }
        public string TipoNCF { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Serie { get; set; } = "";
        public int SecuenciaInicial { get; set; }
        public int SecuenciaActual { get; set; }
        public int SecuenciaFinal { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int StockMinimo { get; set; }
        public bool Activo { get; set; }
        public string Ambiente { get; set; } = "PRUEBAS";
        public DateTime FechaCreacion { get; set; }
    }

    public class SecuenciaEcfCreateDto
    {
        public int IdEmpresa { get; set; }
        public int TipoEcfDgii { get; set; }
        public string? Descripcion { get; set; }
        public string Serie { get; set; } = "";
        public int SecuenciaInicial { get; set; } = 1;
        public int SecuenciaFinal { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int StockMinimo { get; set; } = 50;
        public string Ambiente { get; set; } = "PRUEBAS";
        public string? NumeroResolucion { get; set; }
    }

    public class SecuenciaEcfUpdateDto
    {
        public int? SecuenciaFinal { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int? StockMinimo { get; set; }
        public bool? Activo { get; set; }
    }

    public class ReservaEcfResultado
    {
        public bool Exitoso { get; set; }
        public string? Encf { get; set; }
        public int? NumeroReservado { get; set; }
        public int? SecuenciaFinal { get; set; }
        public string? MensajeError { get; set; }
        public int SecuenciasRestantes => (SecuenciaFinal ?? 0) - (NumeroReservado ?? 0);

        public static ReservaEcfResultado Fallo(string mensaje) => new()
        {
            Exitoso = false,
            MensajeError = mensaje
        };
    }

    public class SecuenciaEcfDisponibleDto
    {
        public int TipoEcfDgii { get; set; }
        public string Descripcion { get; set; } = "";
        public string Serie { get; set; } = "";
        public int Restantes { get; set; }
        public int StockMinimo { get; set; }
        public bool Agotada { get; set; }
        public bool Vencida { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }

    public class SecuenciaAlertaDto
    {
        public int TipoEcfDgii { get; set; }
        public string Descripcion { get; set; } = "";
        public bool Disponible { get; set; }
        public int Restantes { get; set; }
        public bool StockBajo { get; set; }
        public bool ProximaAVencer { get; set; }
        public string? MensajeAlerta { get; set; }
    }
}
