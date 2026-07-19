using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Cada módulo comercial implementa su propio resolver.
    /// El Motor Fiscal nunca conoce entidades comerciales directamente.
    /// Principio Open/Closed: agregar un nuevo módulo = crear un nuevo resolver.
    /// </summary>
    public interface IDocumentoOrigenResolver
    {
        OrigenDocumento Origen { get; }

        Task<DocumentoOrigenInfo> ObtenerDocumentoAsync(int idOrigen, int idEmpresa);

        Task CrearFotografiaAsync(DocumentoOrigenInfo documento);
    }

    /// <summary>
    /// Factory que resuelve el resolver correcto según OrigenDocumento.
    /// Construye un diccionario desde DI (auto-descubrimiento, sin switch).
    /// </summary>
    public interface IDocumentoOrigenResolverFactory
    {
        IDocumentoOrigenResolver Get(OrigenDocumento origen);
    }
}
