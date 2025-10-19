using AutoMapper;
using DataLayer.Dtos;
using DataLayer.Entities;
using Server.RabbitMq;
using Server.Repositories;

namespace Server.Services
{
    public interface IEmployeeService : IEntityService<Employee, GetEmployeeDto, UpsertEmployeeDto> { }
    public class EmployeeService : EntityService<Employee, GetEmployeeDto, UpsertEmployeeDto>, IEmployeeService
    {
        public EmployeeService(IEmployeeRepository repo,IMapper mapper,
            ILogger<EntityService<Employee, GetEmployeeDto, UpsertEmployeeDto>> logger,
            IServiceScopeFactory _scopeFactory) : base(repo,mapper,_scopeFactory,logger)
        {
            
        }
    }
}
