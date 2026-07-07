using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    
        public interface ISecuenciaDocumentoService
        {
            public Task<string> GenerarDocumentoAsync(
                int idEmpresa,
                int idTipoDocumento
            );
        }
    }

