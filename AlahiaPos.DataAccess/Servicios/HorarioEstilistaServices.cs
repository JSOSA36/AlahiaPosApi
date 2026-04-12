using AlahiaPos.Entities.Domain;

using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class HorarioEstilistaServices : IHorariosEstilista
    {
        private readonly IRepository<HorariosEstilista> _repository;
        private readonly IRepository<Cita> _citasRepository;

        public HorarioEstilistaServices(IRepository<HorariosEstilista> repository, IRepository<Cita> citasRepository)
        {
            _repository = repository;
            _citasRepository = citasRepository;
        }

        public void DeleteHorario(int idHorario)
        {
            _repository.Delete(idHorario);
        }

        public async Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpleadoByEmpresa(int idEmpleado, int IdEmpresa )
        {
            return await _repository.GetAllByExpresionAsync(h => h.IdEmpleado == idEmpleado && h.IdEmpresa==IdEmpresa);
        }
        public async Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpleado(int idEmpleado)
        {
            return await _repository.GetAllByExpresionAsync(h => h.IdEmpleado == idEmpleado);
        }

        public async Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpresa(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(h => h.IdEmpresa == idEmpresa);
        }

        public async Task InsertHorario(HorariosEstilista horario)
        {
            await _repository.Save(horario);
        }

        public void UpdateHorario(HorariosEstilista horario)
        {
            _repository.Update(horario.IdHorario, horario);
        }

        // 🔹 Devuelve las horas libres de un estilista en una fecha
        public async Task<IEnumerable<string>> GetDisponibilidad(int idEmpleado, DateTime fecha)
        {
            int diaSemana = (int)fecha.DayOfWeek;

            // 🔹 1. Horarios del estilista
            var horarios = await _repository.GetAllByExpresionAsync(h =>
                h.IdEmpleado == idEmpleado &&
                h.DiaSemana == diaSemana);

            if (horarios == null || !horarios.Any())
                return new List<string>();

            // 🔹 2. Citas del día
            var citas = await _citasRepository.GetAllByExpresionAsync(c =>
                c.IdEmpleado == idEmpleado &&
                c.Fecha.Date == fecha.Date);

            var horasOcupadas = new HashSet<string>(
                citas.Select(c => c.Hora.ToString(@"hh\:mm"))
            );

            // 🔹 3. Generar disponibilidad
            var disponibles = new List<string>();

            foreach (var h in horarios)
            {
                var horaActual = h.HoraInicio;

                while (horaActual < h.HoraFin)
                {
                    // 🚨 BLOQUEAR HORA DE COMIDA
                    if (h.RecesoInicio.HasValue && h.RecesoFin.HasValue)
                    {
                        if (horaActual >= h.RecesoInicio.Value &&
                            horaActual < h.RecesoFin.Value)
                        {
                            horaActual = horaActual.Add(TimeSpan.FromMinutes(30));
                            continue;
                        }
                    }

                    var horaStr = horaActual.ToString(@"hh\:mm");

                    if (!horasOcupadas.Contains(horaStr))
                    {
                        disponibles.Add(horaStr);
                    }

                    horaActual = horaActual.Add(TimeSpan.FromMinutes(30));
                }

            }

            return disponibles;
        }


        // 🔹 Verifica si una hora está disponible
        public async Task<bool> IsHoraDisponible(int idEmpleado, DateTime fecha, TimeSpan hora)
        {
            var existeCita = await _citasRepository.GetAllByExpresionAsync(c => c.IdEmpleado == idEmpleado
                                                                                && c.Fecha.Date == fecha.Date
                                                                                && c.Hora == hora);
            return !existeCita.Any();
        }
    }
}
