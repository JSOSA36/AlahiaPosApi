using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DocumentosClinicosService : IDocumentosClinicos
    {
        private readonly IRepository<DocumentosClinicos> _repository;

        public DocumentosClinicosService(
            IRepository<DocumentosClinicos> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DocumentosClinicos>> GetByEmpresa(int idEmpresa)
        {
            var result = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == idEmpresa,
                "Cliente",
                "Plantilla");

            return result
                .OrderByDescending(d => d.FechaEmision)
                .ThenByDescending(d => d.IdDocumentoClinico);
        }

        public async Task<IEnumerable<DocumentosClinicos>> GetByCliente(
            int idEmpresa,
            int idCliente)
        {
            var result = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == idEmpresa && d.IdCliente == idCliente,
                "Cliente",
                "Plantilla");

            return result
                .OrderByDescending(d => d.FechaEmision)
                .ThenByDescending(d => d.IdDocumentoClinico);
        }

        public async Task<DocumentosClinicos?> GetById(int idDocumentoClinico)
        {
            var result = await _repository.GetAllByExpresionAsync(
                d => d.IdDocumentoClinico == idDocumentoClinico,
                "Cliente",
                "Plantilla");

            return result.FirstOrDefault();
        }

        public async Task Insert(DocumentosClinicos documento)
        {
            if (documento.FechaCreacion == default)
                documento.FechaCreacion = DateTime.Now;

            if (documento.FechaEmision == default)
                documento.FechaEmision = DateTime.Now;

            if (string.IsNullOrWhiteSpace(documento.Estado))
                documento.Estado = "EMITIDO";

            if (string.IsNullOrWhiteSpace(documento.NumeroDocumento))
            {
                documento.NumeroDocumento = await GenerarSiguienteNumero(
                    documento.IdEmpresa,
                    documento.TipoDocumento);
            }

            documento.ContenidoHTMLFinal = ReemplazarNumeroEnHtml(
                documento.ContenidoHTMLFinal,
                documento.NumeroDocumento);

            await _repository.Save(documento);
        }

        public Task Update(int idDocumentoClinico, DocumentosClinicos documento)
        {
            _repository.Update(idDocumentoClinico, documento);
            return Task.CompletedTask;
        }

        public Task Delete(int idDocumentoClinico)
        {
            _repository.Delete(idDocumentoClinico);
            return Task.CompletedTask;
        }

        private async Task<string> GenerarSiguienteNumero(int idEmpresa, string tipoDocumento)
        {
            var existentes = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == idEmpresa
                     && d.TipoDocumento == tipoDocumento);

            var maximo = 0;
            foreach (var doc in existentes)
            {
                if (int.TryParse((doc.NumeroDocumento ?? "").Trim(), out var numero)
                    && numero > maximo)
                {
                    maximo = numero;
                }
            }

            return (maximo + 1).ToString();
        }

        private static string ReemplazarNumeroEnHtml(string html, string numero)
        {
            if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(numero))
                return html ?? string.Empty;

            return html.Replace("{{NUMERO_DOCUMENTO}}", numero);
        }
    }
}
