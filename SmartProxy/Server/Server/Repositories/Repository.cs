using DataLayer.Entities;
using Dapper;
using System.Data;
using Server.Repositories;

namespace Server.Repositories
{
    public class Repository<T> : IRepository<T> where T : Document
    {
        private readonly IDbConnection _db;
        private readonly ILogger<Repository<T>> _logger;

        public Repository(IDbConnection db, ILogger<Repository<T>> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<string> CreateAsync(T entity)
        {
            try
            {
                var sql = $"INSERT INTO {typeof(T).Name}s (Id, /* other columns */) VALUES (@Id, /* params */)";
                await _db.ExecuteAsync(sql, entity);
                return entity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var sql = $"DELETE FROM {typeof(T).Name}s WHERE Id = @Id";
                var rows = await _db.ExecuteAsync(sql, new { Id = id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var sql = $"SELECT * FROM {typeof(T).Name}s";
            return await _db.QueryAsync<T>(sql);
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var sql = $"SELECT * FROM {typeof(T).Name}s WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        public async Task<bool> UpsertAsync(T entity, string id)
        {
            try
            {
                var sql = $@"
                INSERT INTO {typeof(T).Name}s (Id, /* columns */) VALUES (@Id, /* params */)
                ON CONFLICT (Id) DO UPDATE SET /* column=@column, ... */";
                var rows = await _db.ExecuteAsync(sql, entity);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting {EntityType}", typeof(T).Name);
                throw;
            }
        }
    }
}
