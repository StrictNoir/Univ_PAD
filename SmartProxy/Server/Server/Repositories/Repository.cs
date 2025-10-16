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
        public async Task<string> CreateAsync(T entity)
        {
            try
            {
                await _collection.InsertOneAsync(entity);
                return entity.Id;

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

        public async Task<bool> DeleteAsync(string id)
        {
            var objectId = new ObjectId(id);
            try
            {
                var result = await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", objectId));
                return result.DeletedCount > 0;
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

        public async Task<bool> UpsertAsync(T entity,string id)
        {
            try
            {
                entity.Id = id;
               var result = await _collection.ReplaceOneAsync(doc => doc.Id == id, entity, new ReplaceOptions() { IsUpsert = true});
                bool isCreated = result.MatchedCount == 0 || result.UpsertedId != null;
                return isCreated;
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
