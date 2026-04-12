using Alahia.eCF.Api.Dto;

namespace Alahia.eCF.Api.Interfaces
{
    
        public interface IExcelMapperService
        {
            Task<List<EcfDto>> Mapear(Stream excelStream, int tipoeCF);
        }
    }

