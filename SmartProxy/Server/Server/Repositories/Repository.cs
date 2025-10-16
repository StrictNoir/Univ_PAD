using DataLayer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server.Repositories
{
    public class Repository<T> : IRepository<T> where T : Document
    {
        private readonly IMongoCollection<T> _collection;
        private readonly ILogger<Repository<T>> _logger;
        public Repository(IMongoDatabase db, ILogger<Repository<T>> logger)
        {
            _collection = db.GetCollection<T>(typeof(T).Name);
            _logger = logger;
        }
        public async Task CreateAsync(T entity)
        {
            try
            {
                await _collection.InsertOneAsync(entity);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "MongoDB error while inserting {EntityType}.", typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while inserting {EntityType}.", typeof(T).Name);
                throw;
            }
        }

        public async Task DeleteAsync(string id)
        {
            var objectId = new ObjectId(id);
            try
            {
                await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", objectId));
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "MongoDB error while deleting {EntityType}.", typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while inserting {EntityType}.", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var objectId = new ObjectId(id);
            return await _collection.Find(Builders<T>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(T entity)
        {
      
            try
            {
               await _collection.ReplaceOneAsync(doc => doc.Id == entity.Id, entity, new ReplaceOptions() { IsUpsert = true});
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "MongoDB error while deleting {EntityType}.", typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while inserting {EntityType}.", typeof(T).Name);
                throw;
            }
        }
    }
}
