namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadCatalogoService
    {
        Task SeedCatalogoDefaultAsync(int idEmpresa);
        Task<bool> TieneCatalogoAsync(int idEmpresa);
    }
}
