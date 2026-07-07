using AlahiaPos.Entities.Dto;

public class CajaListadoDto
{
    public int IdCajaApertura { get; set; }

    public int IdCajaCierre { get; set; }

    public int IdEmpresa { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    /* =====================================
    🔥 RESUMEN DEL CIERRE
    ====================================== */

    public decimal MontoInicial { get; set; }

    public decimal VentasBrutas { get; set; }

    public decimal TotalDescuento { get; set; }

    public decimal TotalIngresosExtra { get; set; }

    public decimal TotalGastos { get; set; }

    public decimal TotalIngresosNetos { get; set; }

    /* =====================================
    🔥 FORMAS DE PAGO
    ====================================== */

    public List<CajaMetodoPagoDto> MetodosPago { get; set; }
        = new();

    /* =====================================
    🔥 CUADRE
    ====================================== */

    public decimal DebeHaber { get; set; }

    public decimal MontoRealCaja { get; set; }

    public decimal Diferencia { get; set; }

    /* =====================================
    🔥 DATOS
    ====================================== */

    public string Estado { get; set; } = "ABIERTA";

    public string? Observacion { get; set; }

    /* =====================================
    🔥 USUARIOS
    ====================================== */

    public string? UsuarioApertura { get; set; }

    public string? UsuarioCierre { get; set; }

    /* =====================================
    🔥 PRODUCTOS
    ====================================== */

    public List<CajaProductoDto> ProductosVendidos { get; set; }
        = new();
}