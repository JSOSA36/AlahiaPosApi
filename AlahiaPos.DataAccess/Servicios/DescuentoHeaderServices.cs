using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DescuentoHeaderServices : IDescuentoHeader
    {
        private readonly IRepository<DescuentoHeader> _repository;
        private readonly IRepository<DescuentoDetalle> _repositoryDetalle;
        private readonly IRepository<DescuentoAreaDetalle> _repositoryArea;
        private readonly IRepository<DescuentoCategoriaDetalle> _repositoryCategoria;

        public DescuentoHeaderServices(
            IRepository<DescuentoHeader> repository,
            IRepository<DescuentoDetalle> repositoryDetalle,
            IRepository<DescuentoAreaDetalle> repositoryArea,
            IRepository<DescuentoCategoriaDetalle> repositoryCategoria
        )
        {
            _repository = repository;
            _repositoryDetalle = repositoryDetalle;
            _repositoryArea = repositoryArea;
            _repositoryCategoria = repositoryCategoria;
        }

        // ============================================================
        // CRUD BÁSICO
        // ============================================================
        public void ToggleEstado(int id)
        {
            var header = _repository.GetById(id);

            if (header == null)
                throw new Exception("Descuento no encontrado.");

            header.Activo = !header.Activo;

            _repository.Update(id, header);
        }
        public async Task<DescuentoAplicadoDto> GetDescuentoAplicado(
          int idEmpresa, int idProducto, int idArea, int idCategoria)
        {
            var hoy = DateTime.Now;
            var diaSemana = (int)hoy.DayOfWeek; // 0 = Domingo

            // 1. Obtener todos los descuentos activos de la empresa
            var headers = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == idEmpresa && d.Activo == true
            );

            foreach (var h in headers)
            {
                h.Detalles = (await _repositoryDetalle
                    .GetAllByExpresionAsync(x => x.IdDescuentoHeader == h.IdDescuentoHeader)).ToList();

                h.Areas = (await _repositoryArea
                    .GetAllByExpresionAsync(x => x.IdDescuentoHeader == h.IdDescuentoHeader)).ToList();

                h.Categorias = (await _repositoryCategoria
                    .GetAllByExpresionAsync(x => x.IdDescuentoHeader == h.IdDescuentoHeader)).ToList();
            }

            // 2. Filtrar por fecha
            headers = headers.Where(h =>
                (!h.FechaInicio.HasValue || h.FechaInicio <= hoy) &&
                (!h.FechaFin.HasValue || h.FechaFin >= hoy)
            ).ToList();

            // 3. Filtrar por hora
            var horaActual = hoy.TimeOfDay;
            headers = headers.Where(h =>
                (!h.HoraInicio.HasValue || h.HoraInicio <= horaActual) &&
                (!h.HoraFin.HasValue || h.HoraFin >= horaActual)
            ).ToList();

            // 4. Filtrar por día de la semana
            headers = headers.Where(h =>
                string.IsNullOrEmpty(h.DiasSemana) ||
                h.DiasSemana.Split(',').Contains(diaSemana.ToString())
            ).ToList();

            // PRIORIDAD 1: por producto
            var porProducto = headers
                .Where(h => h.Detalles.Any(d => d.IdProducto == idProducto))
                .FirstOrDefault();

            if (porProducto != null)
            {
                return MapDescuentoAplicado(porProducto);
            }

            // PRIORIDAD 2: por categoría
            if (idCategoria > 0)
            {
                var porCategoria = headers
                    .Where(h => h.Categorias.Any(c => c.IdCategoria == idCategoria))
                    .FirstOrDefault();

                if (porCategoria != null)
                {
                    return MapDescuentoAplicado(porCategoria);
                }
            }

            // PRIORIDAD 3: por área
            if (idArea > 0)
            {
                var porArea = headers
                    .Where(h => h.Areas.Any(a => a.IdArea == idArea))
                    .FirstOrDefault();

                if (porArea != null)
                {
                    return MapDescuentoAplicado(porArea);
                }
            }

            // PRIORIDAD 4: descuento general
            var general = headers
                .Where(h => h.AplicaATodos)
                .FirstOrDefault();

            if (general != null)
            {
                return MapDescuentoAplicado(general);
            }

            // Si no aplica nada
            return new DescuentoAplicadoDto { Aplica = false };
        }

        private static DescuentoAplicadoDto MapDescuentoAplicado(DescuentoHeader header)
        {
            return new DescuentoAplicadoDto
            {
                Aplica = true,
                Tipo = header.TipoDescuento,
                Valor = header.Valor,
                NombreEvento = header.NombreEvento
            };
        }

        public void DeleteDescuentoHeader(int IdDescuentoHeader)
        {
            _repository.Delete(IdDescuentoHeader);
        }

        public async Task<IEnumerable<DescuentoHeaderDto>> GetAllDescuentoHeader(int IdEmpresa)
        {
            var headers = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa
            );

            var resultado = new List<DescuentoHeaderDto>();

            foreach (var h in headers)
            {
                var servicios = await _repositoryDetalle.GetAllByExpresionAsync(
                    x => x.IdDescuentoHeader == h.IdDescuentoHeader
                );

                var areas = await _repositoryArea.GetAllByExpresionAsync(
                    x => x.IdDescuentoHeader == h.IdDescuentoHeader
                );

                var categorias = await _repositoryCategoria.GetAllByExpresionAsync(
                    x => x.IdDescuentoHeader == h.IdDescuentoHeader
                );

                resultado.Add(new DescuentoHeaderDto
                {
                    IdDescuentoHeader = h.IdDescuentoHeader,
                    IdEmpresa = h.IdEmpresa,
                    NombreEvento = h.NombreEvento,
                    Descripcion = h.Descripcion,
                    TipoDescuento = h.TipoDescuento,
                    Valor = h.Valor,
                    DiasSemana = h.DiasSemana,
                    FechaInicio = h.FechaInicio,
                    FechaFin = h.FechaFin,
                    HoraInicio = h.HoraInicio,
                    HoraFin = h.HoraFin,
                    AplicaATodos = h.AplicaATodos,
                    Activo = h.Activo,

                    // 🔥 DEVUELVE SOLO LOS IDs COMO NECESITA ANGULAR
                    Servicios = servicios.Select(s => s.IdProducto).ToList(),
                    Areas = areas.Select(a => a.IdArea).ToList(),
                    Categorias = categorias.Select(c => c.IdCategoria).ToList()
                });
            }

            return resultado;
        }


        public async Task<DescuentoHeader> GetDescuentoHeaderById(int IdDescuentoHeader)
        {
            var h = await _repository.GetByIdAsync(IdDescuentoHeader);

            if (h == null) return null;

            h.Detalles = (await _repositoryDetalle.GetAllByExpresionAsync(
                x => x.IdDescuentoHeader == IdDescuentoHeader
            )).ToList();

            h.Areas = (await _repositoryArea.GetAllByExpresionAsync(
                x => x.IdDescuentoHeader == IdDescuentoHeader
            )).ToList();

            h.Categorias = (await _repositoryCategoria.GetAllByExpresionAsync(
                x => x.IdDescuentoHeader == IdDescuentoHeader
            )).ToList();

            return h;
        }

        public async Task InsertDescuentoHeader(DescuentoHeaderDto dto)
        {
            // 1️⃣ Convertir el DTO en entidad
            var header = new DescuentoHeader
            {
                IdEmpresa = dto.IdEmpresa,
                NombreEvento = dto.NombreEvento,
                Descripcion = dto.Descripcion,
                TipoDescuento = dto.TipoDescuento,
                Valor = dto.Valor,
                DiasSemana = dto.DiasSemana,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                HoraInicio = dto.HoraInicio,
                HoraFin = dto.HoraFin,
                AplicaATodos = dto.AplicaATodos,
                Activo = dto.Activo
            };

            // 2️⃣ Guardar Header y obtener Id
            await _repository.Save(header);

            // 3️⃣ Guardar servicios (DescuentoDetalle)
            foreach (var idProducto in dto.Servicios ?? new List<int>())
            {
                await _repositoryDetalle.Save(new DescuentoDetalle
                {
                    IdDescuentoHeader = header.IdDescuentoHeader,
                    IdProducto = idProducto
                });
            }

            // 4️⃣ Guardar áreas (DescuentoAreaDetalle)
            foreach (var idArea in dto.Areas ?? new List<int>())
            {
                await _repositoryArea.Save(new DescuentoAreaDetalle
                {
                    IdDescuentoHeader = header.IdDescuentoHeader,
                    IdArea = idArea
                });
            }

            foreach (var idCategoria in dto.Categorias ?? new List<int>())
            {
                await _repositoryCategoria.Save(new DescuentoCategoriaDetalle
                {
                    IdDescuentoHeader = header.IdDescuentoHeader,
                    IdCategoria = idCategoria
                });
            }
        }
        public void UpdateDescuentoHeader(int Id, DescuentoHeaderDto dto)
        {
            // 1️⃣ Obtener el header real
            var header =  _repository.GetById(Id);

            if (header == null)
                throw new Exception("Descuento no encontrado.");

            // 2️⃣ Actualizar campos del header
            header.NombreEvento = dto.NombreEvento;
            header.Descripcion = dto.Descripcion;
            header.TipoDescuento = dto.TipoDescuento;
            header.Valor = dto.Valor;
            header.DiasSemana = dto.DiasSemana;
            header.FechaInicio = dto.FechaInicio;
            header.FechaFin = dto.FechaFin;
            header.HoraInicio = dto.HoraInicio;
            header.HoraFin = dto.HoraFin;
            header.AplicaATodos = dto.AplicaATodos;
            header.Activo = dto.Activo;

            // 3️⃣ Guardar cambios del header (igual que Insert)
             _repository.Update(header.IdDescuentoHeader,header);

            // =============================================================
            // 🔥 4️⃣ ACTUALIZAR SERVICIOS
            // =============================================================

            // Borrar servicios existentes
            var serviciosActuales =  _repositoryDetalle.GetAllByExpresionNoAsync(
                x => x.IdDescuentoHeader == Id
            );

            foreach (var s in serviciosActuales)
                 _repositoryDetalle.Delete(s.IdDescuentoDetalle);

            // Insertar los servicios nuevos
            foreach (var idProducto in dto.Servicios ?? new List<int>())
            {
                 _repositoryDetalle.SaveNoAsync(new DescuentoDetalle
                {
                    IdDescuentoHeader = Id,
                    IdProducto = idProducto
                });
            }

            // =============================================================
            // 🔥 5️⃣ ACTUALIZAR ÁREAS
            // =============================================================

            // Borrar áreas existentes
            var areasActuales =  _repositoryArea.GetAllByExpresionNoAsync(
                x => x.IdDescuentoHeader == Id
            );

            foreach (var a in areasActuales)
                 _repositoryArea.Delete(a.IdDescuentoAreaDetalle);

            // Insertar áreas nuevas
            foreach (var idArea in dto.Areas ?? new List<int>())
            {
                 _repositoryArea.SaveNoAsync(new DescuentoAreaDetalle
                {
                    IdDescuentoHeader = Id,
                    IdArea = idArea
                });
            }

            var categoriasActuales = _repositoryCategoria.GetAllByExpresionNoAsync(
                x => x.IdDescuentoHeader == Id
            );

            foreach (var c in categoriasActuales)
                _repositoryCategoria.Delete(c.IdDescuentoCategoriaDetalle);

            foreach (var idCategoria in dto.Categorias ?? new List<int>())
            {
                _repositoryCategoria.SaveNoAsync(new DescuentoCategoriaDetalle
                {
                    IdDescuentoHeader = Id,
                    IdCategoria = idCategoria
                });
            }
        }



        // ============================================================
        // ACTIVOS
        // ============================================================

        public async Task<IEnumerable<DescuentoHeader>> GetDescuentosActivos(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa &&
                     d.Activo
            );
        }

        // ============================================================
        // POR DÍA
        // ============================================================

        public async Task<IEnumerable<DescuentoHeader>> GetDescuentosByDia(int IdEmpresa, int diaSemana)
        {
            string dia = diaSemana.ToString();

            return await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa &&
                     d.Activo &&
                     (string.IsNullOrWhiteSpace(d.DiasSemana) ||
                      ("," + d.DiasSemana + ",").Contains("," + dia + ","))
            );
        }

        // ============================================================
        // POR HORARIO
        // ============================================================

        public async Task<IEnumerable<DescuentoHeader>> GetDescuentosPorHorario(int IdEmpresa, TimeSpan horaActual)
        {
            return await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa &&
                     d.Activo &&
                     (!d.HoraInicio.HasValue || horaActual >= d.HoraInicio) &&
                     (!d.HoraFin.HasValue || horaActual <= d.HoraFin)
            );
        }

        // ============================================================
        // POR SERVICIO / ÁREA
        // ============================================================

        public async Task<IEnumerable<DescuentoHeader>> GetDescuentosPorServicio(
            int IdEmpresa,
            int IdProducto,
            int IdArea)
        {
            var headers = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa &&
                     d.Activo
            );

            foreach (var h in headers)
            {
                h.Detalles = (await _repositoryDetalle.GetAllByExpresionAsync(
                   x => x.IdDescuentoHeader == h.IdDescuentoHeader
                )).ToList();

                h.Areas = (await _repositoryArea.GetAllByExpresionAsync(
                   x => x.IdDescuentoHeader == h.IdDescuentoHeader
                )).ToList();
            }

            return headers.Where(h =>
                h.AplicaATodos ||
                h.Detalles.Any(x => x.IdProducto == IdProducto) ||
                h.Areas.Any(a => a.IdArea == IdArea)
            );
        }

        // ============================================================
        // VIGENTES AHORA (FULL CHECK)
        // ============================================================

        public async Task<IEnumerable<DescuentoHeader>> GetDescuentosVigentesAhora(
            int IdEmpresa,
            int diaSemana,
            TimeSpan horaActual,
            int IdProducto,
            int IdArea)
        {
            var hoy = DateTime.Now.Date;
            string dia = diaSemana.ToString();

            var headers = await _repository.GetAllByExpresionAsync(
                d => d.IdEmpresa == IdEmpresa &&
                     d.Activo &&
                     (!d.FechaInicio.HasValue || d.FechaInicio <= hoy) &&
                     (!d.FechaFin.HasValue || d.FechaFin >= hoy) &&
                     (string.IsNullOrWhiteSpace(d.DiasSemana) ||
                      ("," + d.DiasSemana + ",").Contains("," + dia + ",")) &&
                     (!d.HoraInicio.HasValue || horaActual >= d.HoraInicio) &&
                     (!d.HoraFin.HasValue || horaActual <= d.HoraFin)
            );

            foreach (var h in headers)
            {
                h.Detalles = (await _repositoryDetalle.GetAllByExpresionAsync(
                   x => x.IdDescuentoHeader == h.IdDescuentoHeader
                )).ToList();

                h.Areas = (await _repositoryArea.GetAllByExpresionAsync(
                   x => x.IdDescuentoHeader == h.IdDescuentoHeader
                )).ToList();
            }

            return headers.Where(h =>
                h.AplicaATodos ||
                h.Detalles.Any(x => x.IdProducto == IdProducto) ||
                h.Areas.Any(a => a.IdArea == IdArea)
            );
        }
    }
}
