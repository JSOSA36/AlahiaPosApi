using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INotificacionSuscripcionCanal
    {
        string Canal { get; }
        Task EnviarAsync(NotificacionSuscripcionMensaje mensaje, CancellationToken ct = default);
    }

    public interface ISuscripcionCobroService
    {
        Task ProcesarCicloDiarioAsync(CancellationToken ct = default);
        Task ActualizarEstadoEmpresaAsync(int idEmpresa);
        bool PuedeOperar(Empresas empresa);
        bool EstaBloqueada(Empresas empresa);
        AlertaPagoDto? ObtenerAlertaPago(Empresas empresa);
        Task RegistrarEventoAsync(int idEmpresa, string tipo, string? detalle, string? canal = null, int? idUsuario = null, int? idCiclo = null, string? metadataJson = null);
        Task<SuscripcionCiclo?> ObtenerOCrearCicloActualAsync(Empresas empresa);
        Task<List<SuscripcionCicloDto>> ListarCiclosAsync(int? idEmpresa = null);
        Task<List<SuscripcionEventoDto>> ListarEventosAsync(int idEmpresa, int top = 100);
        Task<List<SuscripcionLineaFacturaDto>> ListarDetalleCicloAsync(int idCiclo);
        Task<SuscripcionResumenCobrosDto> ObtenerResumenAsync();
        Task<List<SuscripcionEmpresaCobroDto>> ListarEmpresasCobroAsync();
        Task<SuscripcionCalculoFacturaDto> CalcularFacturaAsync(int idEmpresa, DateTime? fechaReferencia = null);
        Task RecalcularCicloAbiertoAsync(int idEmpresa);
        Task<SuscripcionCalculoFacturaDto> ActualizarPrecioPlanEspecialAsync(ActualizarPrecioPlanEspecialDto dto);
        Task OnPagoReportadoAsync(int idEmpresa, int idPago, int? idCiclo);
        Task OnPagoAprobadoAsync(int idEmpresa, int idPago, int? idCiclo, string? usuarioValida);
        Task OnPagoRechazadoAsync(int idEmpresa, int idPago, string? observacion);
        Task OnPlanCambiadoAsync(int idEmpresa, int idPlanAnterior, int idPlanNuevo, int? idUsuario);
    }
}
