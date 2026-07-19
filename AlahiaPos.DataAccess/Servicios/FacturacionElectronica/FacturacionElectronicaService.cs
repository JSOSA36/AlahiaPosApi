using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public class FacturacionElectronicaService : IFacturacionElectronicaService
    {
        private readonly ISecuenciaEcfService _secuencias;
        private readonly IDocumentoOrigenResolverFactory _resolverFactory;
        private readonly IFiscalFeatureService _features;
        private readonly IFiscalGateway _gateway;
        private readonly IFiscalDocumentoValidator _validator;
        private readonly AlahiaPosContext _ctx;
        private readonly ILogger<FacturacionElectronicaService> _logger;

        public FacturacionElectronicaService(
            ISecuenciaEcfService secuencias,
            IDocumentoOrigenResolverFactory resolverFactory,
            IFiscalFeatureService features,
            IFiscalGateway gateway,
            IFiscalDocumentoValidator validator,
            AlahiaPosContext ctx,
            ILogger<FacturacionElectronicaService> logger)
        {
            _secuencias = secuencias;
            _resolverFactory = resolverFactory;
            _features = features;
            _gateway = gateway;
            _validator = validator;
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<EmisionEcfResultado> EmitirDocumentoAsync(EmisionEcfRequest request)
        {
            var flags = await _features.GetFeaturesAsync(request.IdEmpresa);
            if (!flags.FacturacionElectronicaActiva)
            {
                return EmisionEcfResultado.Fallo(
                    "Facturación electrónica no está activa para esta empresa");
            }

            // 1. Reservar e-NCF atómicamente
            var reserva = await _secuencias.ReservarSiguienteAsync(
                request.IdEmpresa, request.TipoEcfDgii);
            if (!reserva.Exitoso)
            {
                _logger.LogWarning(
                    "Reserva e-NCF fallida: Empresa={Emp}, Tipo={Tipo}, Error={Err}",
                    request.IdEmpresa, request.TipoEcfDgii, reserva.MensajeError);
                return EmisionEcfResultado.Fallo(reserva.MensajeError!);
            }

            // 2. Resolver obtiene datos del documento comercial
            var resolver = _resolverFactory.Get(request.OrigenDocumento);
            var docInfo = await resolver.ObtenerDocumentoAsync(
                request.IdOrigen, request.IdEmpresa);

            // 3. Crear fotografía fiscal
            await resolver.CrearFotografiaAsync(docInfo);

            // 4. Crear ECFEncabezado
            var ecf = new ECFEncabezado
            {
                IdEmpresa = request.IdEmpresa,
                TipoECF = request.TipoEcfDgii.ToString(),
                ENCF = reserva.Encf!,
                FechaEmision = docInfo.FechaDocumento,
                RncReceptor = docInfo.RncCliente,
                NombreReceptor = docInfo.NombreCliente,
                MontoGravado = docInfo.SubTotal,
                TotalITBIS = docInfo.TotalItbis,
                TotalGeneral = docInfo.Total,
                OrigenDocumento = (int)request.OrigenDocumento,
                IdOrigen = request.IdOrigen,
                NumeroFacturaInterna = docInfo.NumeroDocumentoInterno,
                EstadoDocumento = EstadoDocumentoElectronico.PendienteEnvio,
                EstadoDGII = "Pendiente",
                FechaCreacion = DateTime.Now
            };

            _ctx.ECFEncabezados.Add(ecf);

            // 5. Encolar envío asíncrono al Gateway via outbox
            var outboxPayload = new
            {
                request.IdEmpresa,
                IdEcf = 0, // placeholder, se actualiza abajo
                request.OrigenDocumento,
                request.IdOrigen,
                request.TipoEcfDgii,
                Encf = reserva.Encf,
                request.IdUsuario
            };

            var outbox = new EventoOutbox
            {
                IdEmpresa = request.IdEmpresa,
                TipoEvento = "ECF_ENVIAR_GATEWAY",
                ReferenciaTipo = "ECFEncabezado",
                Payload = JsonSerializer.Serialize(outboxPayload),
                Estado = EventoOutboxEstados.Pendiente,
                IdempotencyKey = $"ECF_{request.OrigenDocumento}_{request.IdOrigen}_{request.TipoEcfDgii}",
                FechaCreacion = DateTime.Now
            };

            _ctx.EventosOutbox.Add(outbox);

            await _ctx.SaveChangesAsync();

            // Actualizar payload con IdECF real
            outbox.ReferenciaId = ecf.IdECF;
            await _ctx.SaveChangesAsync();

            _logger.LogInformation(
                "e-CF emitido: {Encf}, Origen={Origen}, IdOrigen={IdOrigen}, IdECF={IdEcf}",
                reserva.Encf, request.OrigenDocumento, request.IdOrigen, ecf.IdECF);

            return new EmisionEcfResultado
            {
                Exitoso = true,
                Encf = reserva.Encf,
                IdEcf = ecf.IdECF,
                SecuenciasRestantes = reserva.SecuenciasRestantes
            };
        }

        public async Task<EmisionEcfResultadoCompleto> EmitirYEnviarAsync(EmisionEcfRequest request)
        {
            var flags = await _features.GetFeaturesAsync(request.IdEmpresa);
            if (!flags.FacturacionElectronicaActiva)
                return new EmisionEcfResultadoCompleto { Exitoso = false, MensajeError = "Facturación electrónica no está activa" };

            var resolver = _resolverFactory.Get(request.OrigenDocumento);
            var docInfo = await resolver.ObtenerDocumentoAsync(request.IdOrigen, request.IdEmpresa);

            // Referencia para E33/E34 (ND/NC) cuando el origen no la trae
            if (!string.IsNullOrWhiteSpace(request.NcfModificado))
            {
                docInfo.NcfModificado = request.NcfModificado;
                docInfo.FechaDocumentoModificado = request.FechaNcfModificado ?? docInfo.FechaDocumento;
                docInfo.CodigoModificacion = request.CodigoModificacion ?? 1;
                docInfo.RazonModificacion = request.RazonModificacion;
            }

            var encfPeek = await _secuencias.PeekSiguienteAsync(request.IdEmpresa, request.TipoEcfDgii);
            if (string.IsNullOrWhiteSpace(encfPeek))
                return new EmisionEcfResultadoCompleto
                {
                    Exitoso = false,
                    MensajeError = "No hay secuencia e-NCF disponible para este tipo"
                };

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == request.IdEmpresa);
            var secuencia = await _ctx.SecuenciasECF.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdEmpresa == request.IdEmpresa
                    && s.TipoEcfDgii == request.TipoEcfDgii && s.Activo);

            // Validación previa (Motor de Definiciones) sin consumir secuencia
            var ecfProvisional = new ECFEncabezado
            {
                IdEmpresa = request.IdEmpresa,
                TipoECF = request.TipoEcfDgii.ToString(),
                ENCF = encfPeek,
                FechaEmision = docInfo.FechaDocumento,
                RncReceptor = docInfo.RncCliente,
                NombreReceptor = docInfo.NombreCliente,
                MontoGravado = docInfo.SubTotal,
                TotalITBIS = docInfo.TotalItbis,
                TotalGeneral = docInfo.Total,
                OrigenDocumento = (int)request.OrigenDocumento,
                IdOrigen = request.IdOrigen,
                NumeroFacturaInterna = docInfo.NumeroDocumentoInterno
            };

            var docPrevio = FiscalDocumentoBuilder.Build(
                ecfProvisional, docInfo, request.IdOrigen, (int)request.OrigenDocumento,
                request.TipoEcfDgii, empresa, secuencia);

            var validacion = _validator.Validar(docPrevio);
            if (!validacion.Ok)
            {
                _logger.LogWarning(
                    "Validación FE fallida (sin reservar e-NCF): Empresa={Emp} Tipo={Tipo} Origen={Origen}/{Id} → {Msg}",
                    request.IdEmpresa, request.TipoEcfDgii, request.OrigenDocumento, request.IdOrigen, validacion.Mensaje);
                return new EmisionEcfResultadoCompleto
                {
                    Exitoso = false,
                    MensajeError = validacion.Mensaje,
                    MensajesDgii = validacion.Mensajes,
                    RncEmisor = empresa?.RNC,
                    RazonSocialEmisor = empresa?.NombreComercial
                };
            }

            var reserva = await _secuencias.ReservarSiguienteAsync(request.IdEmpresa, request.TipoEcfDgii);
            if (!reserva.Exitoso)
                return new EmisionEcfResultadoCompleto { Exitoso = false, MensajeError = reserva.MensajeError };

            await resolver.CrearFotografiaAsync(docInfo);

            var ecf = new ECFEncabezado
            {
                IdEmpresa = request.IdEmpresa,
                TipoECF = request.TipoEcfDgii.ToString(),
                ENCF = reserva.Encf!,
                FechaEmision = docInfo.FechaDocumento,
                RncReceptor = docInfo.RncCliente,
                NombreReceptor = docInfo.NombreCliente,
                MontoGravado = docInfo.SubTotal,
                TotalITBIS = docInfo.TotalItbis,
                TotalGeneral = docInfo.Total,
                OrigenDocumento = (int)request.OrigenDocumento,
                IdOrigen = request.IdOrigen,
                NumeroFacturaInterna = docInfo.NumeroDocumentoInterno,
                EstadoDocumento = EstadoDocumentoElectronico.PendienteEnvio,
                EstadoDGII = "Pendiente",
                FechaCreacion = DateTime.Now
            };

            _ctx.ECFEncabezados.Add(ecf);
            await _ctx.SaveChangesAsync();

            var docElectronico = FiscalDocumentoBuilder.Build(
                ecf, docInfo, request.IdOrigen, (int)request.OrigenDocumento,
                request.TipoEcfDgii, empresa, secuencia);

            _logger.LogInformation("Envío síncrono ECF {Encf} (Origen={Origen}, Id={Id})",
                ecf.ENCF, request.OrigenDocumento, request.IdOrigen);

            var resultado = await _gateway.EnviarDocumentoAsync(docElectronico, CancellationToken.None);

            ecf.TrackId = resultado.TrackId;
            ecf.TransmissionJobId = resultado.TransmissionJobId;
            ecf.SecurityCode = resultado.SecurityCode;
            ecf.UrlQR = resultado.UrlQR;
            ecf.FechaFirma = resultado.FechaFirma;
            ecf.FechaEnvio = DateTime.Now;

            if (resultado.Exitoso)
            {
                ecf.EstadoDocumento = EstadoDocumentoElectronico.Enviado;
                ecf.EstadoDGII = resultado.Estado;
            }
            else
            {
                ecf.EstadoDocumento = EstadoDocumentoElectronico.Error;
                ecf.EstadoDGII = "Error";
                ecf.MensajeRespuesta = string.Join("; ", resultado.Mensajes);
            }

            await _ctx.SaveChangesAsync();

            // Propagar e-NCF al documento comercial (NC/ND referencian FacturaHeaders.NCF).
            // DbContext global es NoTracking → hace falta AsTracking / Attach.
            if (request.OrigenDocumento == OrigenDocumento.Pos && resultado.Exitoso
                && !string.IsNullOrWhiteSpace(reserva.Encf))
            {
                var factura = await _ctx.FacturaHeaders
                    .AsTracking()
                    .FirstOrDefaultAsync(f => f.IdFacturaHeader == request.IdOrigen);
                if (factura != null && string.IsNullOrWhiteSpace(factura.NCF))
                {
                    factura.NCF = reserva.Encf;
                    await _ctx.SaveChangesAsync();
                }
            }

            return new EmisionEcfResultadoCompleto
            {
                Exitoso = resultado.Exitoso,
                Encf = reserva.Encf,
                IdEcf = ecf.IdECF,
                SecuenciasRestantes = reserva.SecuenciasRestantes,
                TrackId = resultado.TrackId,
                TransmissionJobId = resultado.TransmissionJobId,
                EstadoDgii = ecf.EstadoDGII,
                UrlQR = resultado.UrlQR,
                SecurityCode = resultado.SecurityCode,
                MensajesDgii = resultado.Mensajes,
                MensajeError = resultado.Exitoso ? null : string.Join("; ", resultado.Mensajes),
                RncEmisor = empresa?.RNC,
                RazonSocialEmisor = empresa?.NombreComercial
            };
        }

        public async Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerSecuenciasDisponiblesAsync(int idEmpresa)
        {
            return await _secuencias.ObtenerDisponiblesAsync(idEmpresa);
        }
    }
}
