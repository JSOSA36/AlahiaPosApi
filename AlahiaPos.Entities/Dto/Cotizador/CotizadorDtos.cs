using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Cotizador
{
    public class CotizadorCatalogoDto
    {
        public List<TipoNegocioDto> TiposNegocio { get; set; } = new();
        public List<CategoriaModulosDto> Categorias { get; set; } = new();
        public List<ModuloComercialDto> Modulos { get; set; } = new();
        public List<DependenciaModuloDto> Dependencias { get; set; } = new();
        public Dictionary<string, string> Parametros { get; set; } = new();
        public List<TramoDocumentoDto> TramosDocumentos { get; set; } = new();
    }

    public class TipoNegocioDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public List<string> ModulosPreseleccionados { get; set; } = new();
    }

    public class CategoriaModulosDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ModuloComercialDto
    {
        public int ModuloId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? DescripcionComercial { get; set; }
        public string CategoriaComercial { get; set; } = string.Empty;
        public string Nivel { get; set; } = "BASE";
        public bool ParticipaPrecio { get; set; }
        public decimal PrecioBaseUSD { get; set; }
        public int Orden { get; set; }
        public string? Icono { get; set; }
        public List<string> TiposNegocio { get; set; } = new();
    }

    public class DependenciaModuloDto
    {
        public string CodigoModulo { get; set; } = string.Empty;
        public string CodigoRequerido { get; set; } = string.Empty;
        public string Tipo { get; set; } = "RECOMIENDA";
        public string? Mensaje { get; set; }
    }

    public class TramoDocumentoDto
    {
        public int DesdeDocs { get; set; }
        public int? HastaDocs { get; set; }
        public decimal CargoUSD { get; set; }
        public string? Etiqueta { get; set; }
    }

    public class CotizadorCalcularRequest
    {
        public string? TipoNegocioCodigo { get; set; }
        public List<string> CodigosModulos { get; set; } = new();
        public int Usuarios { get; set; } = 1;
        public int Sucursales { get; set; } = 1;
        public bool UsaFacturacionElectronica { get; set; }
        public int DocumentosElectronicosMensuales { get; set; }
    }

    public class CotizadorPropuestaDto
    {
        public string? TipoNegocioCodigo { get; set; }
        public List<string> ModulosSeleccionados { get; set; } = new();
        public List<string> ModulosIncluidos { get; set; } = new();
        public List<RecomendacionModuloDto> Recomendaciones { get; set; } = new();
        public List<LineaPrecioDto> Desglose { get; set; } = new();
        public decimal SubtotalUSD { get; set; }
        public decimal AjustePisoUSD { get; set; }
        public decimal PrecioMensualUSD { get; set; }
        public string Moneda { get; set; } = "USD";
        public bool AplicoPisoMinimo { get; set; }
        public decimal PisoMensualUSD { get; set; }
        public int UsuariosIncluidos { get; set; }
        public int Usuarios { get; set; }
        public int Sucursales { get; set; }
        public bool UsaFacturacionElectronica { get; set; }
        public int DocumentosElectronicosMensuales { get; set; }
        public List<string> Explicaciones { get; set; } = new();
        public string Resumen { get; set; } = string.Empty;
    }

    public class RecomendacionModuloDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "RECOMIENDA";
        public string Mensaje { get; set; } = string.Empty;
        public string OrigenCodigo { get; set; } = string.Empty;
    }

    public class LineaPrecioDto
    {
        public string Concepto { get; set; } = string.Empty;
        public string TipoLinea { get; set; } = "MODULO";
        public string? CodigoModulo { get; set; }
        public decimal MontoUSD { get; set; }
    }

    public class CotizadorGuardarRequest
    {
        public CotizadorCalcularRequest Seleccion { get; set; } = new();
        public CotizadorPropuestaDto? Propuesta { get; set; }
    }

    public class CotizadorGuardarResponse
    {
        public string Folio { get; set; } = string.Empty;
        public int CotizacionId { get; set; }
        public CotizadorPropuestaDto Propuesta { get; set; } = new();
    }

    public class CotizadorEnviarCorreoRequest
    {
        public string Folio { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Nombre { get; set; }
    }

    public class CotizadorSolicitarRequest
    {
        public string? Folio { get; set; }
        public string Tipo { get; set; } = "DEMO";
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Mensaje { get; set; }
        public CotizadorCalcularRequest? Seleccion { get; set; }
    }

    /// <summary>Vista pública de cotización (solo lectura, para compartir por link/WhatsApp).</summary>
    public class CotizadorVistaPublicaDto
    {
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string? TipoNegocioCodigo { get; set; }
        public int Usuarios { get; set; }
        public int Sucursales { get; set; }
        public bool UsaFacturacionElectronica { get; set; }
        public int DocumentosElectronicosMensuales { get; set; }
        public decimal PrecioMensualUSD { get; set; }
        public string Moneda { get; set; } = "USD";
        public string Resumen { get; set; } = string.Empty;
        public List<LineaPrecioDto> Desglose { get; set; } = new();
        public List<string> Explicaciones { get; set; } = new();
        public string Aviso { get; set; } =
            "Estimación orientativa. No constituye una oferta oficial. Sujeta a validación comercial.";
    }
}
