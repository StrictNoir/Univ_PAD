using AutoMapper;
using DataLayer.Entities;
using Server.Repositories;

namespace Server.Services
{
    public interface IEntityService<TEntity, TGetDto,TInsertDto> where TEntity : Document
    {
        Task<IEnumerable<TGetDto>> GetAllAsync();
        Task<TGetDto?> GetByIdAsync(string id);
        Task CreateAsync(TInsertDto dto);
        Task UpsertAsync(TInsertDto dto);
        Task DeleteAsync(string id);
    }
    public class EntityService<TEntity, TGetDto,TInsertDto> : IEntityService<TEntity, TGetDto,TInsertDto> where TEntity: Document
    {
        private readonly IRepository<TEntity> _repository;
        private readonly IMapper _mapper;

        public EntityService(IRepository<TEntity> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task CreateAsync(TInsertDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _repository.CreateAsync(entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _repository.DeleteAsync(id);
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

        public async Task UpsertAsync(TInsertDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _repository.UpsertAsync(entity);
        }
    }
}
