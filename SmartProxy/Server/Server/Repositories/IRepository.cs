using DataLayer.Entities;

namespace Server.Repositories
{
    public interface IRepository<T> where T : Document
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task CreateAsync(T entity);
        Task DeleteAsync(string id);
        Task UpsertAsync(T entity);

    }
}
