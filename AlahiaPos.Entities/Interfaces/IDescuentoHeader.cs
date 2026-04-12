using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDescuentoHeader
{
    // CRUD BASICO
    Task<IEnumerable<DescuentoHeaderDto>> GetAllDescuentoHeader(int IdEmpresa);
    Task<DescuentoHeader> GetDescuentoHeaderById(int IdDescuentoHeader);
    Task InsertDescuentoHeader(DescuentoHeaderDto descuentoHeader);
    void UpdateDescuentoHeader(int Id, DescuentoHeaderDto dto);
    void DeleteDescuentoHeader(int IdDescuentoHeader);
    public void ToggleEstado(int id);
    public Task<DescuentoAplicadoDto> GetDescuentoAplicado(
         int idEmpresa, int idProducto, int idArea);
    // 🔹 Descuentos Activos
    Task<IEnumerable<DescuentoHeader>> GetDescuentosActivos(int IdEmpresa);

    // 🔹 Por día
    Task<IEnumerable<DescuentoHeader>> GetDescuentosByDia(
        int IdEmpresa,
        int diaSemana
    );

    // 🔹 Por horario
    Task<IEnumerable<DescuentoHeader>> GetDescuentosPorHorario(
        int IdEmpresa,
        TimeSpan horaActual
    );

    // 🔹 Por servicio o por área
    Task<IEnumerable<DescuentoHeader>> GetDescuentosPorServicio(
        int IdEmpresa,
        int IdProducto,
        int IdArea
    );

    // 🔹 Descuentos aplicables AHORA MISMO
    Task<IEnumerable<DescuentoHeader>> GetDescuentosVigentesAhora(
        int IdEmpresa,
        int diaSemana,
        TimeSpan horaActual,
        int IdProducto,
        int IdArea
    );
}
