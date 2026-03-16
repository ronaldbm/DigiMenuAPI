using AutoMapper;
using AutoMapper.QueryableExtensions;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public CategoryService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<CategoryReadDto>>> GetAll()
        {
            var companyId = _tenantService.GetCompanyId();

            // QueryFilter global ya aplica !IsDeleted — solo falta filtrar por tenant
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.DisplayOrder)
                .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<CategoryReadDto>>.Ok(categories);
        }

        public async Task<OperationResult<CategoryReadDto>> GetById(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            // CompanyId valida ownership — QueryFilter cubre !IsDeleted
            var category = await _context.Categories
                .AsNoTracking()
                .Where(c => c.Id == id && c.CompanyId == companyId)
                .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (category is null)
                return OperationResult<CategoryReadDto>.Fail("Categoría no encontrada.");

            return OperationResult<CategoryReadDto>.Ok(category);
        }

        public async Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var exists = await _context.Categories
                .AnyAsync(c => c.CompanyId == companyId && c.Name == dto.Name.Trim());
            if (exists)
                return OperationResult<CategoryReadDto>.Conflict(
                    "Ya existe una categoría con ese nombre en tu empresa.",
                    ErrorKeys.CategoryAlreadyExists);

            var category = _mapper.Map<Category>(dto);
            category.CompanyId = companyId; // ← siempre desde JWT, nunca del cliente

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<CategoryReadDto>.Ok(_mapper.Map<CategoryReadDto>(category));
        }

        public async Task<OperationResult<bool>> Update(CategoryUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            // CompanyId garantiza que no se puede modificar categoría de otro tenant
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.CompanyId == companyId);

            if (category is null)
                return OperationResult<bool>.NotFound("Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            _mapper.Map(dto, category);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (category is null)
                return OperationResult<bool>.NotFound("Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            category.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Reorder(List<ReorderItemDto> items)
        {
            var companyId = _tenantService.GetCompanyId();
            var ids = items.Select(i => i.Id).ToList();

            var categories = await _context.Categories
                .Where(c => c.CompanyId == companyId && ids.Contains(c.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                var category = categories.FirstOrDefault(c => c.Id == item.Id);
                if (category is not null)
                    category.DisplayOrder = item.DisplayOrder;
            }

            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

    }
}