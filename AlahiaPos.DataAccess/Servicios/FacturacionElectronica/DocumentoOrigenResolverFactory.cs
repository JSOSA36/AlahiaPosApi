using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public class DocumentoOrigenResolverFactory : IDocumentoOrigenResolverFactory
    {
        private readonly Dictionary<OrigenDocumento, IDocumentoOrigenResolver> _resolvers;

        public DocumentoOrigenResolverFactory(IEnumerable<IDocumentoOrigenResolver> resolvers)
        {
            _resolvers = resolvers.ToDictionary(r => r.Origen);
        }

        public IDocumentoOrigenResolver Get(OrigenDocumento origen)
        {
            if (_resolvers.TryGetValue(origen, out var resolver))
                return resolver;

            throw new InvalidOperationException(
                $"No existe un resolver registrado para OrigenDocumento.{origen}. " +
                $"Registre una implementación de IDocumentoOrigenResolver con Origen = {origen} en DI.");
        }
    }
}
