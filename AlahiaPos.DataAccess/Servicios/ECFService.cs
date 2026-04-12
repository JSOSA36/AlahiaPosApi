using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ECFService : IECFService
    {
        IRepository<FacturaHeaders> _FacturaHeader;
        IRepository<FacturaDetalles> _FacturaDetalle;
        IRepository<ECFEncabezado> _ECFEncabezado;
        IRepository<ECFDetalle> _ECFDetalle;
        IRepository<ECFXml> _ECFXml;
        IRepository<ECFHistorialEstado> _Historial;
        IRepository<CertificadoDigital> _Certificado;
        IRepository<Empresas> _EmpresaRepository;
        IRepository<Clientes> _ClienteRepository;

        IECFBuilder _ecfBuilder;
        IECFSigner _signer;
        IECFValidator _validator;

        public ECFService(
            IRepository<FacturaHeaders> facturaHeader,
            IRepository<FacturaDetalles> facturaDetalle,
            IRepository<ECFEncabezado> ecfEncabezado,
            IRepository<ECFDetalle> ecfDetalle,
            IRepository<ECFXml> ecfXml,
            IRepository<ECFHistorialEstado> historial,
            IRepository<CertificadoDigital> certificado,
            IRepository<Empresas> empresaRepository,
            IRepository<Clientes> clienteRepository,
            IECFBuilder ecfBuilder,
            IECFSigner signer,
            IECFValidator validator)
        {
            _FacturaHeader = facturaHeader;
            _FacturaDetalle = facturaDetalle;
            _ECFEncabezado = ecfEncabezado;
            _ECFDetalle = ecfDetalle;
            _ECFXml = ecfXml;
            _Historial = historial;
            _Certificado = certificado;
            _EmpresaRepository = empresaRepository;
            _ClienteRepository = clienteRepository;

            _ecfBuilder = ecfBuilder;
            _signer = signer;
            _validator = validator;
        }

        // =====================================================
        // 🔥 PROCESAR FACTURA ELECTRÓNICA
        // =====================================================

       
        // =====================================================
        // REENVIAR
        // =====================================================

        public async Task<DtoRespuestaDgii> ReenviarECF(int IdECF, int IdEmpresa)
        {
            var ecf = await _ECFEncabezado.GetByIdAsync(IdECF);

            if (ecf == null)
                throw new Exception("ECF no encontrado");

            await _Historial.Save(new ECFHistorialEstado
            {
                IdECF = IdECF,
                Estado = "REENVIO_PENDIENTE",
                Codigo = "SIMULADO",
                Mensaje = "Reenvío pendiente integración DGII",
                Fecha = DateTime.Now
            });

            return new DtoRespuestaDgii
            {
                CodigoError = "SIMULADO",
                Mensaje = "Reenvío pendiente integración DGII",
                TrackId = ecf.TrackId
            };
        }

        // =====================================================
        // CONSULTAR ESTADO
        // =====================================================

        public async Task<DtoRespuestaDgii> ConsultarEstado(string TrackId, int IdEmpresa)
        {
            return new DtoRespuestaDgii
            {
                CodigoError = "SIMULADO",
                Mensaje = "Estado pendiente consulta DGII",
                TrackId = TrackId
            };
        }
    }
}