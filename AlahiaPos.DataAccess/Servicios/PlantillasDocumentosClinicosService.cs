using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PlantillasDocumentosClinicosService : IPlantillasDocumentosClinicos
    {
        private readonly IRepository<PlantillasDocumentosClinicos> _repository;

        public PlantillasDocumentosClinicosService(
            IRepository<PlantillasDocumentosClinicos> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PlantillasDocumentosClinicos>> GetByEmpresa(int idEmpresa)
        {
            var result = await _repository.GetAllByExpresionAsync(
                p => p.IdEmpresa == idEmpresa);

            return result
                .OrderByDescending(p => p.Activa)
                .ThenByDescending(p => p.EsPredeterminada)
                .ThenBy(p => p.Nombre);
        }

        public async Task<PlantillasDocumentosClinicos?> GetById(int idPlantilla)
        {
            return await _repository.GetByIdAsync(idPlantilla);
        }

        public async Task Insert(PlantillasDocumentosClinicos plantilla)
        {
            plantilla.FechaCreacion = DateTime.Now;

            if (plantilla.EsPredeterminada)
            {
                await QuitarPredeterminadaAnterior(
                    plantilla.IdEmpresa,
                    plantilla.TipoDocumento,
                    null);
            }

            await _repository.Save(plantilla);
        }

        public async Task Update(int idPlantilla, PlantillasDocumentosClinicos plantilla)
        {
            if (plantilla.EsPredeterminada)
            {
                await QuitarPredeterminadaAnterior(
                    plantilla.IdEmpresa,
                    plantilla.TipoDocumento,
                    idPlantilla);
            }

            _repository.Update(idPlantilla, plantilla);
        }

        public async Task CambiarEstado(int idPlantilla, bool activa)
        {
            var plantilla = await _repository.GetByIdAsync(idPlantilla);

            if (plantilla == null)
                throw new Exception("Plantilla no encontrada");

            plantilla.Activa = activa;

            if (!activa)
                plantilla.EsPredeterminada = false;

            _repository.Update(idPlantilla, plantilla);
        }

        private async Task QuitarPredeterminadaAnterior(
            int idEmpresa,
            string tipoDocumento,
            int? idPlantillaExcluir)
        {
            var predeterminadas = await _repository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa &&
                p.TipoDocumento == tipoDocumento &&
                p.EsPredeterminada &&
                (!idPlantillaExcluir.HasValue || p.IdPlantilla != idPlantillaExcluir.Value));

            foreach (var item in predeterminadas)
            {
                item.EsPredeterminada = false;
                _repository.Update(item.IdPlantilla, item);
            }
        }
    }
}
