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
        /// <summary>Próximo número a emitir. Se incrementa en cada generación.</summary>
        public int SecuenciaActual { get; set; }
        public int ProximaSecuencia { get; set; }
        public int SecuenciaFinal { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int StockMinimo { get; set; }
        public bool Activo { get; set; }
        public string Ambiente { get; set; } = "PRUEBAS";
        public DateTime FechaCreacion { get; set; }
        public int? IdSucursal { get; set; }
        public string? NombreSucursal { get; set; }
        public List<SecuenciaEcfAsignacionDto> Asignaciones { get; set; } = new();
        public int NumerosSinAsignar { get; set; }
        public int? SiguienteHuecoInicial { get; set; }
        public int? SiguienteHuecoFinal { get; set; }
    }

    public class SecuenciaEcfAsignacionDto
    {
        public int IdAsignacion { get; set; }
        public int IdSecuencia { get; set; }
        public int IdSucursal { get; set; }
        public string? NombreSucursal { get; set; }
        public int SecuenciaInicial { get; set; }
        public int SecuenciaActual { get; set; }
        public int ProximaSecuencia { get; set; }
        public int SecuenciaFinal { get; set; }
        public bool Activo { get; set; }
    }

    public class SecuenciaEcfAsignarDto
    {
        public int IdSucursal { get; set; }
        public int SecuenciaInicial { get; set; }
        public int SecuenciaFinal { get; set; }
        public int? ProximaSecuencia { get; set; }
        public int? SecuenciaActual { get; set; }
    }

    public class SecuenciaEcfCreateDto
    {
        public int IdEmpresa { get; set; }
        public int TipoEcfDgii { get; set; }
        public string? Descripcion { get; set; }
        public string Serie { get; set; } = "";
        public int SecuenciaInicial { get; set; } = 1;
        /// <summary>Si no se envía, la próxima es igual a la inicial.</summary>
        public int? SecuenciaActual { get; set; }
        public int? ProximaSecuencia { get; set; }
        public int SecuenciaFinal { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int StockMinimo { get; set; } = 50;
        public string Ambiente { get; set; } = "PRUEBAS";
        public string? NumeroResolucion { get; set; }
    }

    public class SecuenciaEcfUpdateDto
    {
        public int? SecuenciaInicial { get; set; }
        /// <summary>Próximo número a emitir (columna SecuenciaActual).</summary>
        public int? SecuenciaActual { get; set; }
        public int? ProximaSecuencia { get; set; }
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
