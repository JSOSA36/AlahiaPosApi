using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AutoMapper;

namespace AlahiaPosApi.Mapper
{
    public class AlahiaPosMapper : Profile
    {
        public AlahiaPosMapper()
        {
            // ===============================
            // 🪑 MESAS
            // ===============================
            CreateMap<Mesas, MesasDto>();
            CreateMap<MesasDto, Mesas>();

            // ===============================
            // 🧾 FACTURACIÓN
            // ===============================
            CreateMap<FacturaHeaders, FacturaHeaderDto>();
            CreateMap<FacturaHeaderDto, FacturaHeaders>();

            CreateMap<FacturaDetalles, FacturaDetallesDto>();
            CreateMap<FacturaDetallesDto, FacturaDetalles>();

            // ===============================
            // 📍 ZONAS
            // ===============================
            CreateMap<Zonas, ZonasDto>();
            CreateMap<ZonasDto, Zonas>();

            // ===============================
            // 👥 EMPLEADOS / COMISIONES
            // ===============================
            CreateMap<EmpleadoAreaComision, EmpleadoAreaComisionDto>();
            CreateMap<EmpleadoAreaComisionDto, EmpleadoAreaComision>();

            CreateMap<EmpleadoServicioComisionDto, EmpleadoAreaComision>();
            CreateMap<EmpleadoAreaComision[], EmpleadoServicioComisionDto[]>();

            // ===============================
            // 👤 CLIENTES
            // ===============================
            CreateMap<Clientes, ClienteDto>();
            CreateMap<ClienteDto, Clientes>();

            // ===============================
            // 📆 CITAS
            // ===============================
            CreateMap<Cita, CitaDto>();
            CreateMap<CitaDto, Cita>();

            CreateMap<Cita[], CitaDto[]>();
            CreateMap<CitaDto[], Cita[]>();

            // ===============================
            // ⏰ HORARIOS
            // ===============================
            CreateMap<HorariosEstilista, HorarioEstilistaDto>();
            CreateMap<HorarioEstilistaDto, HorariosEstilista>();

            CreateMap<HorariosEstilista[], HorarioEstilistaDto[]>();
            CreateMap<HorarioEstilistaDto[], HorariosEstilista[]>();

            // ===============================
            // 🏢 EMPRESAS
            // ===============================
            CreateMap<Empresas, EmpresaDto>();
            CreateMap<EmpresaDto, Empresas>();

            // ===============================
            // 🧩 MÓDULOS
            // ===============================
            CreateMap<Modulo, ModuloDto>();
            CreateMap<ModuloDto, Modulo>();

            CreateMap<EmpresaModulo, EmpresaModuloDto>();
            CreateMap<EmpresaModuloDto, EmpresaModulo>();

            // ===============================
            // 🔐 PERFILES (NUEVO – IMPORTANTE)
            // ===============================
            // ===============================
            // 🔐 PERFILES
            // ===============================
            CreateMap<Perfiles, PerfilDto>();

            CreateMap<PerfilDto, Perfiles>();

            CreateMap<Parametros, ParametrosDto>();
            CreateMap<ParametrosDto, Parametros>();


        }
    }
}
