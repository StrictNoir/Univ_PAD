using AutoMapper;
using DataLayer.Dtos;
using DataLayer.Entities;
using Server.RabbitMq;
using Server.Repositories;

namespace Server.Services
{
    public interface IEmployeeService
        : IEntityService<Employee, GetEmployeeDto, UpsertEmployeeDto>
    { }

    public class EmployeeService
        : EntityService<Employee, GetEmployeeDto, UpsertEmployeeDto>, IEmployeeService
    {
        public EmployeeService(
            IEmployeeRepository repo,
            IMapper mapper,
            IRabbitMQService<Employee> rabbitMqService,
            ILogger<EntityService<Employee, GetEmployeeDto, UpsertEmployeeDto>> logger)
            : base(repo, mapper, rabbitMqService, logger)
        {
        }
    }
}
