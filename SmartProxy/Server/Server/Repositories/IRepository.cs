using DataLayer.Entities;

namespace Server.Repositories
{
    public interface IRepository<T> where T : Document
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task<string> CreateAsync(T entity);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpsertAsync(T entity,string id);

    }
}
