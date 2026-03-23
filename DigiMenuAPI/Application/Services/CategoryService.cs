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
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly ICacheService _cache;

        public CategoryService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            ICacheService cache)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _cache = cache;
        }

        public async Task<OperationResult<List<CategoryListItemDto>>> GetAll(string? lang = null)
        {
            var companyId = _tenantService.GetCompanyId();

            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Translations)
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var result = categories.Select(c => new CategoryListItemDto
            {
                Id           = c.Id,
                CompanyId    = c.CompanyId,
                DisplayOrder = c.DisplayOrder,
                IsVisible    = c.IsVisible,
                Name         = ResolveName(c.Translations, lang),
            }).ToList();

            return OperationResult<List<CategoryListItemDto>>.Ok(result);
        }

        public async Task<OperationResult<CategoryReadDto>> GetById(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var category = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Translations)
                .Where(c => c.Id == id && c.CompanyId == companyId)
                .ProjectTo<CategoryReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (category is null)
                return OperationResult<CategoryReadDto>.NotFound("Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            return OperationResult<CategoryReadDto>.Ok(category);
        }

        public async Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            Category category = null!;

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var maxOrder = await _context.Categories
                    .Where(c => c.CompanyId == companyId)
                    .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

                category = _mapper.Map<Category>(dto);
                category.CompanyId    = companyId;
                category.DisplayOrder = maxOrder + 1;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                // Guardar traducciones en la misma transacción
                foreach (var t in dto.Translations.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
                {
                    _context.CategoryTranslations.Add(new CategoryTranslation
                    {
                        CategoryId   = category.Id,
                        LanguageCode = t.LanguageCode.Trim().ToLowerInvariant(),
                        Name         = t.Name.Trim(),
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            await _cache.EvictMenuByCompanyAsync(companyId);

            // Recargar con traducciones para la respuesta
            await _context.Entry(category).Collection(c => c.Translations).LoadAsync();
            return OperationResult<CategoryReadDto>.Ok(_mapper.Map<CategoryReadDto>(category));
        }

        public async Task<OperationResult<bool>> Update(CategoryUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var category = await _context.Categories
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.CompanyId == companyId);

            if (category is null)
                return OperationResult<bool>.NotFound("Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                // Actualizar campos escalares
                category.IsVisible = dto.IsVisible;

                // Replace-all para traducciones
                var incoming = dto.Translations
                    .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                    .ToDictionary(t => t.LanguageCode.Trim().ToLowerInvariant());

                // Eliminar las que ya no vienen
                var toDelete = category.Translations
                    .Where(t => !incoming.ContainsKey(t.LanguageCode))
                    .ToList();
                _context.CategoryTranslations.RemoveRange(toDelete);

                // Upsert las que vienen
                foreach (var (code, input) in incoming)
                {
                    var existing = category.Translations.FirstOrDefault(t => t.LanguageCode == code);
                    if (existing is null)
                    {
                        _context.CategoryTranslations.Add(new CategoryTranslation
                        {
                            CategoryId   = category.Id,
                            LanguageCode = code,
                            Name         = input.Name.Trim(),
                        });
                    }
                    else
                    {
                        existing.Name = input.Name.Trim();
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            await _cache.EvictMenuByCompanyAsync(companyId);

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

            await _cache.EvictMenuByCompanyAsync(companyId);

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
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<bool>.Ok(true);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string ResolveName(IEnumerable<CategoryTranslation> translations, string? lang)
        {
            var list = translations.ToList();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var match = list.FirstOrDefault(t => t.LanguageCode == lang);
                if (match is not null) return match.Name;
            }
            return list.FirstOrDefault()?.Name ?? string.Empty;
        }
    }
}
