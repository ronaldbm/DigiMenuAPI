using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public CategoryService(ApplicationDbContext context, IMapper mapper, IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<CategoryReadDto>>> GetAll()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<CategoryReadDto>>.Ok(categories);
        }

        public async Task<OperationResult<CategoryReadDto>> GetById(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null) 
                return OperationResult<CategoryReadDto>.Fail("Categoría no encontrada");

            return OperationResult<CategoryReadDto>.Ok(category);
        }

        public async Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto)
        {
            var category = _mapper.Map<Category>(dto);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<CategoryReadDto>.Ok(_mapper.Map<CategoryReadDto>(category));
        }

        public async Task<OperationResult<bool>> Update(CategoryUpdateDto dto)
        {
            var category = await _context.Categories.FindAsync(dto.Id);
            if (category is null) 
                return OperationResult<bool>.Fail("Categoría no encontrada");

            _mapper.Map(dto, category);
            await _context.SaveChangesAsync();

            // Al actualizar visibilidad o nombre, el menú público debe refrescarse
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            // 1. Verificamos si la categoría existe
            var category = await _context.Categories.FindAsync(id);
            if (category is null) 
                return OperationResult<bool>.Fail("Categoría no encontrada");

            // 2. Verificamos si tiene productos SIN traerlos a memoria
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
                return OperationResult<bool>.Fail("No se puede eliminar una categoría que tiene productos asociados.");

            category.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }
    }
}