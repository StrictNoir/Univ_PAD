using DataLayer.Entities;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Server.Repositories
{
    public interface IEmployeeRepository : IRepository<Employee> { }

    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(IDbConnection db, ILogger<Repository<Employee>> logger) 
            : base(db, logger) 
        { }
    }
}