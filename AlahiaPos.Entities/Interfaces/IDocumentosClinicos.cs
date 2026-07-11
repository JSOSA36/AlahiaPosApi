using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDocumentosClinicos
    {
        Task<IEnumerable<DocumentosClinicos>> GetByEmpresa(int idEmpresa);

        Task<IEnumerable<DocumentosClinicos>> GetByCliente(int idEmpresa, int idCliente);

        Task<DocumentosClinicos?> GetById(int idDocumentoClinico);

        Task Insert(DocumentosClinicos documento);

        Task Update(int idDocumentoClinico, DocumentosClinicos documento);

        Task Delete(int idDocumentoClinico);
    }
}
