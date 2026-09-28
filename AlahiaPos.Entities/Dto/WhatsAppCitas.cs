namespace AlahiaPos.Entities.Dto
{
    public enum WhatsAppCitaTipo
    {
        Recibida = 1,
        Confirmada = 2,
        Recordatorio = 3,
        Cancelada = 4
    }

    public class WhatsAppCitaMensaje
    {
        public WhatsAppCitaTipo Tipo { get; set; }
        public string Telefono { get; set; } = "";
        public int IdEmpresa { get; set; }
        public int? IdCita { get; set; }
        public bool EsPrueba { get; set; }
        public string NombreCliente { get; set; } = "";
        public string NombreSalon { get; set; } = "";
        public string Servicio { get; set; } = "";
        public string Estilista { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
    }

    /// <summary>
    /// Un solo número de Alahia Citas (Twilio / WhatsApp Business).
    /// Luego se cambia solo <see cref="From"/>.
    /// </summary>
    public class WhatsAppCitasOptions
    {
        public const string Section = "WhatsAppCitas";

        public bool Enabled { get; set; } = true;

        public string? AccountSid { get; set; }
        public string? AuthToken { get; set; }

        /// <summary>Ej: whatsapp:+1809XXXXXXX o +1809XXXXXXX</summary>
        public string? From { get; set; }

        /// <summary>Hora local RD (HH:mm) para el recordatorio del día.</summary>
        public string HoraRecordatorio { get; set; } = "08:00";

        public WhatsAppCitasTemplateSids Templates { get; set; } = new();

        /// <summary>Lo que se le carga al salón por cada mensaje enviado.</summary>
        public decimal PrecioClienteDop { get; set; } = 5m;

        /// <summary>Costo estimado Alahia (Meta + Twilio) para evaluar margen.</summary>
        public decimal CostoAlahiaDop { get; set; } = 3m;
    }

    /// <summary>ContentSid (HX…) de Twilio Content / plantillas Meta. Vacío = se busca por nombre o texto sandbox.</summary>
    public class WhatsAppCitasTemplateSids
    {
        public string? Recibida { get; set; }
        public string? Confirmada { get; set; }
        public string? Recordatorio { get; set; }
        public string? Cancelada { get; set; }
    }

    public class WhatsAppCitasPlantillaDef
    {
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "UTILITY";
        public string Idioma { get; set; } = "es";
        public string Cuerpo { get; set; } = "";
        public Dictionary<string, string> VariablesEjemplo { get; set; } = new();
    }

    public class WhatsAppCitasEstadoDto
    {
        public bool Listo { get; set; }
        public bool Enabled { get; set; }
        public bool TieneAccountSid { get; set; }
        public bool TieneAuthToken { get; set; }
        public string? From { get; set; }
        public string HoraRecordatorio { get; set; } = "08:00";
        public string Nota { get; set; } = "";
        public List<WhatsAppCitasPlantillaRegistroDto> Plantillas { get; set; } = new();
    }

    public class WhatsAppCitasPlantillaRegistroDto
    {
        public string Nombre { get; set; } = "";
        public string? ContentSid { get; set; }
        public string? EstadoAprobacion { get; set; }
        public string? Detalle { get; set; }
        public bool CreadaAhora { get; set; }
    }

    public class WhatsAppCitasConsumoDto
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public int? IdCita { get; set; }
        public string Tipo { get; set; } = "";
        public string? Telefono { get; set; }
        public decimal PrecioClienteDop { get; set; }
        public decimal CostoAlahiaDop { get; set; }
        public decimal MargenDop { get; set; }
        public bool EsPrueba { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class WhatsAppCitasConsumoEmpresaDto
    {
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public int Mensajes { get; set; }
        public decimal PrecioClienteDop { get; set; }
        public decimal CostoAlahiaDop { get; set; }
        public decimal MargenDop { get; set; }
    }

    public class WhatsAppCitasConsumoResumenDto
    {
        public decimal PrecioPorMensajeDop { get; set; }
        public decimal CostoPorMensajeDop { get; set; }
        public int Mensajes { get; set; }
        public decimal PrecioClienteDop { get; set; }
        public decimal CostoAlahiaDop { get; set; }
        public decimal MargenDop { get; set; }
        public List<WhatsAppCitasConsumoEmpresaDto> PorEmpresa { get; set; } = new();
        public List<WhatsAppCitasConsumoDto> Ultimos { get; set; } = new();
    }
}
