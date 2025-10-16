using DataLayer.Entities;
using MongoDB.Driver;

namespace Server.Repositories
{
    public interface IEmployeeRepository: IRepository<Employee> { }
    public class EmployeeRepository : Repository<Employee>,IEmployeeRepository
    {
        public EmployeeRepository(IMongoDatabase db, ILogger<Repository<Employee>> logger) : base(db,logger) { }
    }
}
