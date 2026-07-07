using AlahiaPos.Entities.Dto.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDgiiClientService
    {
        // 1️⃣ Obtener semilla
        Task<string> ObtenerSemilla();
        Task<object> EnviarRFCE_Debug(string xmlFirmado,string NombreArchivo);
        // 2️⃣ Validar semilla firmada
        Task<string> ValidarSemilla(string semillaFirmadaXml);

        // 3️⃣ Orquestador completo
        Task<string> Autenticar();

        // ============================================
        // 📤 ENVÍO DE DOCUMENTOS
        // ============================================

        // Enviar e-CF firmado
        Task<object> EnviarECF(string xmlFirmado);

        // Enviar RFCE (resumen facturas consumo)
        Task<object> EnviarRFCE(string xmlFirmado);

        // ============================================
        // 🔎 CONSULTAS
        // ============================================

        // Consultar estado documento
        Task<object> ConsultarResultado(string trackId);
    }
}
