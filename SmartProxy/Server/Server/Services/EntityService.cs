using AutoMapper;
using DataLayer.Entities;
using Server.Repositories;

namespace Server.Services
{
    public interface IEntityService<TEntity, TGetDto, TInsertDto>
        where TEntity : Document
    {
        Task<IEnumerable<TGetDto>> GetAllAsync();
        Task<TGetDto?> GetByIdAsync(string id);
        Task<string> CreateAsync(TInsertDto dto);
        Task<bool> UpsertAsync(TInsertDto dto, string id);
        Task<bool> DeleteAsync(string id);
    }

    public class EntityService<TEntity, TGetDto, TInsertDto>
        : IEntityService<TEntity, TGetDto, TInsertDto>
        where TEntity : Document
    {
        private readonly IRepository<TEntity> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<EntityService<TEntity, TGetDto, TInsertDto>> _logger;

        public EntityService(
            IRepository<TEntity> repository,
            IMapper mapper,
            ILogger<EntityService<TEntity, TGetDto, TInsertDto>> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string> CreateAsync(TInsertDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            var id = await _repository.CreateAsync(entity);

            return id;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _repository.DeleteAsync(id);

            return result;
        }

        public async Task<IEnumerable<TGetDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TGetDto>>(entities);
        }

        public async Task<TGetDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? default : _mapper.Map<TGetDto>(entity);
        }

        public async Task<bool> UpsertAsync(TInsertDto dto, string id)
        {
            var entity = _mapper.Map<TEntity>(dto);
            var result = await _repository.UpsertAsync(entity, id);

            return result;
        }
    }
}
