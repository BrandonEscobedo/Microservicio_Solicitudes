using AutoMapper;
using Servicio2.Dtos;
using Servicio2.EndPoints;
using Servicio2.Models.DbModels;

namespace Servicio2.Utility
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Solicitud, SolicitudDTO>().ReverseMap();
            CreateMap<roles, RolDto>().ReverseMap();
            CreateMap<Empleados, EmpleadoDto>().ReverseMap();

            CreateMap<Empleados, EmpleadoResponse>().ReverseMap();
            CreateMap<Solicitud, SolicitudResponse>()
              .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo.ToString()))
              .ForMember(dest => dest.Estatus, opt => opt.MapFrom(src => src.Estatus.ToString()))
              .ForMember(x=>x.Jefe, opt => opt.MapFrom(src => src.Empleado.Jefe))
              .ReverseMap();
        }
    }
}
