using AlahiaPos.Entities.Dto;



namespace AlahiaPos.Entities.Interfaces

{

    /// <summary>

    /// Motor de integración contable. Solo debe ser invocado por consumidores de eventos (Contabilidad).

    /// </summary>

    public interface IContabilidadIntegracionService

    {

        Task<int> RegistrarAsientoAutomaticoAsync(ContabilidadIntegracionRequest request);



        Task<int> RevertirAsientoAutomaticoAsync(

            int idEmpresa,

            string origenModulo,

            int origenReferenciaId,

            string tipoOperacion,

            int idUsuario,

            string? motivo = null);

    }

}

