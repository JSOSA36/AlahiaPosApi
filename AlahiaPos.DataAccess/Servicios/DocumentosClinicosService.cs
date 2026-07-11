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
    }
}
