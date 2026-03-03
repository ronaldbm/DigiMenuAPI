using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public TagService(ApplicationDbContext context, IMapper mapper, IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<TagReadDto>>> GetAll()
        {
            var tags = await _context.Tags
                .AsNoTracking()
                .ProjectTo<TagReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return OperationResult<List<TagReadDto>>.Ok(tags);
        }

        public async Task<OperationResult<TagReadDto>> GetById(int id)
        {
            var tag = await _context.Tags
                .AsNoTracking()
                .ProjectTo<TagReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag is null)
                return OperationResult<TagReadDto>.Fail("Etiqueta no encontrada");

            return OperationResult<TagReadDto>.Ok(tag);
        }

        public async Task<OperationResult<TagReadDto>> Create(TagCreateDto dto)
        {
            var tag = _mapper.Map<Tag>(dto);
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<TagReadDto>.Ok(_mapper.Map<TagReadDto>(tag));
        }

        public async Task<OperationResult<bool>> Update(TagUpdateDto dto)
        {
            var tag = await _context.Tags.FindAsync(dto.Id);
            if (tag is null)
                return OperationResult<bool>.Fail("Etiqueta no encontrada");

            _mapper.Map(dto, tag);
            await _context.SaveChangesAsync();

            // Si se cambia el nombre de "Vegan" a "Vegano", el caché debe morir
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag is null)
                return OperationResult<bool>.Fail("Etiqueta no encontrada");

            tag.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<bool>.Ok(true);
        }
    }
}