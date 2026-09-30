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

            var idSucursal = await ResolverIdSucursalAsync(request);
            var reserva = await _secuencias.ReservarSiguienteAsync(
                request.IdEmpresa, request.TipoEcfDgii, idSucursal);
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

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == request.IdEmpresa);

            // 3. Crear fotografía fiscal
            await resolver.CrearFotografiaAsync(docInfo);

            // 4. Crear ECFEncabezado
            var ecf = new ECFEncabezado
            {
                IdEmpresa = request.IdEmpresa,
                TipoECF = request.TipoEcfDgii.ToString(),
                ENCF = reserva.Encf!,
                FechaEmision = docInfo.FechaDocumento,
                RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
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
                request.IdUsuario,
                IdSucursal = idSucursal
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

        private const int MaxReintentosSecuenciaUtilizada = 5;

        public async Task<EmisionEcfResultadoCompleto> EmitirYEnviarAsync(EmisionEcfRequest request)
        {
            var flags = await _features.GetFeaturesAsync(request.IdEmpresa);
            if (!flags.FacturacionElectronicaActiva)
                return new EmisionEcfResultadoCompleto { Exitoso = false, MensajeError = "Facturación electrónica no está activa" };

            var resolver = _resolverFactory.Get(request.OrigenDocumento);
            var docInfo = await resolver.ObtenerDocumentoAsync(request.IdOrigen, request.IdEmpresa);

            if (!string.IsNullOrWhiteSpace(request.NcfModificado))
            {
                docInfo.NcfModificado = request.NcfModificado;
                docInfo.FechaDocumentoModificado = request.FechaNcfModificado ?? docInfo.FechaDocumento;
                docInfo.CodigoModificacion = request.CodigoModificacion ?? 1;
                docInfo.RazonModificacion = request.RazonModificacion;
            }

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == request.IdEmpresa);
            var idSucursal = await ResolverIdSucursalAsync(request, docInfo);
            var secuencia = await _secuencias.ObtenerActivaAsync(
                request.IdEmpresa, request.TipoEcfDgii, idSucursal);

            EmisionEcfResultadoCompleto? ultimo = null;
            var fotografiaCreada = false;

            for (var intento = 1; intento <= MaxReintentosSecuenciaUtilizada; intento++)
            {
                var ecfExistente = await BuscarEcfOrigenAsync(request);

                if (ecfExistente != null
                    && !string.IsNullOrWhiteSpace(ecfExistente.ENCF)
                    && (ecfExistente.EstadoDGII ?? "").Contains("Aceptado", StringComparison.OrdinalIgnoreCase))
                {
                    return new EmisionEcfResultadoCompleto
                    {
                        Exitoso = true,
                        Encf = ecfExistente.ENCF,
                        IdEcf = ecfExistente.IdECF,
                        TrackId = ecfExistente.TrackId,
                        EstadoDgii = ecfExistente.EstadoDGII,
                        RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
                        RazonSocialEmisor = empresa?.NombreComercial
                    };
                }

                ECFEncabezado ecf;
                string encfReservado;
                var reutilizarPendiente = ecfExistente != null
                    && !string.IsNullOrWhiteSpace(ecfExistente.ENCF)
                    && !EcfSecuenciaYaUtilizada.EncabezadoYaFueEnviado(ecfExistente);

                if (reutilizarPendiente)
                {
                    encfReservado = ecfExistente!.ENCF;
                    ecf = ecfExistente;

                    var docPrevioReintento = FiscalDocumentoBuilder.Build(
                        ecf, docInfo, request.IdOrigen, (int)request.OrigenDocumento,
                        request.TipoEcfDgii, empresa, secuencia);
                    await AplicarRazonSocialPadronAsync(docPrevioReintento);
                    var validacionReintento = _validator.Validar(docPrevioReintento);
                    if (!validacionReintento.Ok)
                    {
                        return new EmisionEcfResultadoCompleto
                        {
                            Exitoso = false,
                            Encf = encfReservado,
                            IdEcf = ecf.IdECF,
                            MensajeError = validacionReintento.Mensaje,
                            MensajesDgii = validacionReintento.Mensajes,
                            RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
                            RazonSocialEmisor = empresa?.NombreComercial
                        };
                    }
                }
                else
                {
                    var encfPeek = await _secuencias.PeekSiguienteAsync(request.IdEmpresa, request.TipoEcfDgii, idSucursal);
                    if (string.IsNullOrWhiteSpace(encfPeek))
                        return new EmisionEcfResultadoCompleto
                        {
                            Exitoso = false,
                            MensajeError = "No hay secuencia e-NCF disponible para este tipo"
                        };

                    var ecfProvisional = new ECFEncabezado
                    {
                        IdEmpresa = request.IdEmpresa,
                        TipoECF = request.TipoEcfDgii.ToString(),
                        ENCF = encfPeek,
                        FechaEmision = docInfo.FechaDocumento,
                        RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
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
                    await AplicarRazonSocialPadronAsync(docPrevio);

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
                            RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
                            RazonSocialEmisor = empresa?.NombreComercial
                        };
                    }

                    var reserva = await _secuencias.ReservarSiguienteAsync(request.IdEmpresa, request.TipoEcfDgii, idSucursal);
                    if (!reserva.Exitoso)
                        return new EmisionEcfResultadoCompleto { Exitoso = false, MensajeError = reserva.MensajeError };

                    encfReservado = reserva.Encf!;
                    if (!fotografiaCreada)
                    {
                        await resolver.CrearFotografiaAsync(docInfo);
                        fotografiaCreada = true;
                    }

                    ecf = new ECFEncabezado
                    {
                        IdEmpresa = request.IdEmpresa,
                        TipoECF = request.TipoEcfDgii.ToString(),
                        ENCF = encfReservado,
                        FechaEmision = docInfo.FechaDocumento,
                        RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
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
                }

                var docElectronico = FiscalDocumentoBuilder.Build(
                    ecf, docInfo, request.IdOrigen, (int)request.OrigenDocumento,
                    request.TipoEcfDgii, empresa, secuencia);
                await AplicarRazonSocialPadronAsync(docElectronico);

                _logger.LogInformation(
                    "Envío síncrono ECF {Encf} intento {Intento}/{Max} (Origen={Origen}, Id={Id})",
                    ecf.ENCF, intento, MaxReintentosSecuenciaUtilizada, request.OrigenDocumento, request.IdOrigen);

                var resultado = await EnviarDocumentoFiscalAsync(docElectronico, CancellationToken.None);
                var secuenciaYaUsada = EcfSecuenciaYaUtilizada.EnResultado(resultado);

                ecf.TrackId = resultado.TrackId;
                ecf.TransmissionJobId = resultado.TransmissionJobId;
                ecf.SecurityCode = resultado.SecurityCode;
                ecf.UrlQR = resultado.UrlQR;
                ecf.FechaFirma = resultado.FechaFirma;
                ecf.FechaEnvio = DateTime.Now;

                var aceptado = resultado.Exitoso && !secuenciaYaUsada
                    && !(resultado.Estado ?? "").Contains("Rechazado", StringComparison.OrdinalIgnoreCase);

                if (aceptado)
                {
                    ecf.EstadoDocumento = EstadoDocumentoElectronico.Enviado;
                    ecf.EstadoDGII = resultado.Estado;
                    if (resultado.Mensajes.Count > 0)
                        ecf.MensajeRespuesta = string.Join("; ", resultado.Mensajes);
                }
                else
                {
                    ecf.EstadoDocumento = EstadoDocumentoElectronico.Error;
                    var estadoProv = (resultado.Estado ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(estadoProv)
                        || estadoProv.Contains("Aceptado", StringComparison.OrdinalIgnoreCase))
                        ecf.EstadoDGII = "Error";
                    else
                        ecf.EstadoDGII = estadoProv;
                    ecf.MensajeRespuesta = string.Join("; ", resultado.Mensajes);
                }

                await _ctx.SaveChangesAsync();

                ultimo = new EmisionEcfResultadoCompleto
                {
                    Exitoso = aceptado,
                    Encf = encfReservado,
                    IdEcf = ecf.IdECF,
                    SecuenciasRestantes = 0,
                    TrackId = resultado.TrackId,
                    TransmissionJobId = resultado.TransmissionJobId,
                    EstadoDgii = ecf.EstadoDGII,
                    UrlQR = resultado.UrlQR,
                    SecurityCode = resultado.SecurityCode,
                    MensajesDgii = resultado.Mensajes,
                    MensajeError = aceptado ? null : string.Join("; ", resultado.Mensajes),
                    RncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa),
                    RazonSocialEmisor = empresa?.NombreComercial
                };

                if (aceptado)
                {
                    await PropagarNcfComercialAsync(request, encfReservado, ecf, resultado);
                    return ultimo;
                }

                if (request.OrigenDocumento == OrigenDocumento.NotaCredito && !secuenciaYaUsada)
                    await PropagarErrorNotaCreditoAsync(request, encfReservado, ecf, resultado);

                if (!secuenciaYaUsada)
                    return ultimo;

                _logger.LogWarning(
                    "e-NCF {Encf} ya utilizado en el proveedor. Se reserva el siguiente (intento {Intento}/{Max}). Empresa={Emp} Origen={Origen}/{Id}",
                    encfReservado, intento, MaxReintentosSecuenciaUtilizada, request.IdEmpresa, request.OrigenDocumento, request.IdOrigen);
            }

            return ultimo ?? new EmisionEcfResultadoCompleto
            {
                Exitoso = false,
                MensajeError = "No se pudo emitir un e-NCF nuevo: la secuencia ya estaba utilizada."
            };
        }

        private Task<ECFEncabezado?> BuscarEcfOrigenAsync(EmisionEcfRequest request) =>
            _ctx.ECFEncabezados
                .AsTracking()
                .Where(e =>
                    e.IdEmpresa == request.IdEmpresa
                    && e.OrigenDocumento == (int)request.OrigenDocumento
                    && e.IdOrigen == request.IdOrigen
                    && e.TipoECF == request.TipoEcfDgii.ToString())
                .OrderByDescending(e => e.IdECF)
                .FirstOrDefaultAsync();

        private async Task PropagarNcfComercialAsync(
            EmisionEcfRequest request,
            string encfReservado,
            ECFEncabezado ecf,
            FiscalEnvioResultado resultado)
        {
            if (request.OrigenDocumento == OrigenDocumento.Pos)
            {
                var factura = await _ctx.FacturaHeaders
                    .AsTracking()
                    .FirstOrDefaultAsync(f => f.IdFacturaHeader == request.IdOrigen);
                if (factura != null)
                {
                    factura.NCF = encfReservado;
                    await _ctx.SaveChangesAsync();
                }
                return;
            }

            if (request.OrigenDocumento == OrigenDocumento.NotaCredito)
            {
                var nc = await _ctx.NotasCredito
                    .AsTracking()
                    .FirstOrDefaultAsync(n => n.IdNotaCredito == request.IdOrigen);
                if (nc == null) return;
                nc.NCF = encfReservado;
                nc.IdEcf = ecf.IdECF;
                nc.TrackId = resultado.TrackId;
                nc.EstadoDgii = ecf.EstadoDGII;
                nc.FechaEmisionEcf = DateTime.Now;
                nc.CodigoTipoComprobanteDgii = "34";
                nc.MensajeEmision = null;
                await _ctx.SaveChangesAsync();
            }
        }

        private async Task PropagarErrorNotaCreditoAsync(
            EmisionEcfRequest request,
            string encfReservado,
            ECFEncabezado ecf,
            FiscalEnvioResultado resultado)
        {
            var nc = await _ctx.NotasCredito
                .AsTracking()
                .FirstOrDefaultAsync(n => n.IdNotaCredito == request.IdOrigen);
            if (nc == null) return;
            if (!string.IsNullOrWhiteSpace(encfReservado))
                nc.NCF = encfReservado;
            nc.IdEcf = ecf.IdECF;
            nc.TrackId = resultado.TrackId;
            nc.EstadoDgii = string.IsNullOrWhiteSpace(ecf.EstadoDGII) ? "Pendiente" : ecf.EstadoDGII;
            nc.FechaEmisionEcf = DateTime.Now;
            nc.CodigoTipoComprobanteDgii = "34";
            nc.MensajeEmision = string.Join("; ", resultado.Mensajes);
            await _ctx.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerSecuenciasDisponiblesAsync(
            int idEmpresa, int? idSucursal = null)
        {
            return await _secuencias.ObtenerDisponiblesAsync(idEmpresa, idSucursal);
        }

        public async Task<FiscalEnvioResultado> EnviarDocumentoFiscalAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            if (documento?.Encabezado == null)
                return FiscalEnvioResultado.Error("VALIDACION", "Documento fiscal inválido: falta Encabezado.");

            FiscalDocumentoBuilder.AlinearConDefinicionDgii(documento);
            await AplicarRazonSocialPadronAsync(documento, ct);
            var validacion = _validator.Validar(documento);
            if (!validacion.Ok)
                return FiscalEnvioResultado.Error("VALIDACION", validacion.Mensaje);

            return await _gateway.EnviarDocumentoAsync(documento, ct);
        }

        public Task<FiscalConsultaResultado> ConsultarEstadoDgiiAsync(
            string trackId,
            int idEmpresa = 0,
            CancellationToken ct = default)
            => _gateway.ConsultarEstadoAsync(trackId, idEmpresa, ct);

        private async Task AplicarRazonSocialPadronAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            if (documento?.Encabezado == null) return;
            // El set de datos y la simulación tienen que salir con el nombre del Excel de DGII.
            // La razón social del padrón se usa en la representación impresa, no en este XML.
            var tipo = documento.TipoDocumentoAlahia ?? "";
            if (tipo is "Certificacion" or "CertificacionSimulacion")
                return;
            var rnc = CertecfReceptorUrls.Digits(documento.Encabezado.RncEmisor);
            string? razon = null;
            string? comercial = null;
            if (!string.IsNullOrWhiteSpace(rnc))
            {
                try
                {
                    var hit = await _ctx.ClientesDGII.AsNoTracking()
                        .Where(c => c.RNC == rnc)
                        .Select(c => new { c.RazonSocial, c.NombreComercial })
                        .FirstOrDefaultAsync(ct);
                    razon = hit?.RazonSocial;
                    comercial = hit?.NombreComercial;
                }
                catch
                {
                    /* padrón opcional */
                }
            }
            CertecfArtefactos.AplicarIdentidadEmisorReal(documento, razon, comercial);
        }

        private async Task<int?> ResolverIdSucursalAsync(
            EmisionEcfRequest request,
            DocumentoOrigenInfo? doc = null)
        {
            if (request.IdSucursal is > 0)
                return request.IdSucursal;
            if (doc?.IdSucursal is > 0)
                return doc.IdSucursal;

            if (request.OrigenDocumento is OrigenDocumento.Pos or OrigenDocumento.Facturacion)
            {
                return await _ctx.FacturaHeaders.AsNoTracking()
                    .Where(f => f.IdFacturaHeader == request.IdOrigen)
                    .Select(f => f.IdSucursal)
                    .FirstOrDefaultAsync();
            }

            if (request.OrigenDocumento == OrigenDocumento.NotaCredito)
            {
                return await _ctx.NotasCredito.AsNoTracking()
                    .Where(n => n.IdNotaCredito == request.IdOrigen)
                    .Select(n => n.IdSucursal)
                    .FirstOrDefaultAsync();
            }

            if (request.OrigenDocumento == OrigenDocumento.Gasto)
            {
                return await _ctx.Set<Gastos>().AsNoTracking()
                    .Where(g => g.IdGasto == request.IdOrigen)
                    .Select(g => g.IdSucursal)
                    .FirstOrDefaultAsync();
            }

            return null;
        }
    }
}
