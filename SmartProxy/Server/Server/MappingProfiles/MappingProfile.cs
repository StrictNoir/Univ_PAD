using AutoMapper;
using DataLayer.Dtos;
using DataLayer.Entities;

namespace Server.MappingProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // From dto to entity
            CreateMap<UpsertEmployeeDto, Employee>();
            CreateMap<GetEmployeeDto, Employee>();  

            //From entity to dto
            CreateMap<Employee,GetEmployeeDto>();
            CreateMap<Employee,UpsertEmployeeDto>();
        }
    }
}
