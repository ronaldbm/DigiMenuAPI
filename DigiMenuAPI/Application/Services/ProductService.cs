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
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public ProductService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            IFileStorageService fileStorage,
            IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _fileStorage = fileStorage;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<PagedResult<ProductListItemDto>>> GetAll(int page = 1, int pageSize = 20, string? lang = null)
        {
            var companyId = _tenantService.GetCompanyId();

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Translations)
                .Include(p => p.Tags)
                .Include(p => p.Category)
                    .ThenInclude(c => c.Translations)
                .Where(p => p.CompanyId == companyId)
                .OrderBy(p => p.CategoryId).ThenBy(p => p.Id);

            var total = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = products.Select(p => new ProductListItemDto
            {
                Id               = p.Id,
                CategoryId       = p.CategoryId,
                CategoryName     = ResolveCategoryName(p.Category?.Translations, lang),
                MainImageUrl     = p.MainImageUrl,
                Name             = ResolveProductName(p.Translations, lang),
                ShortDescription = ResolveShortDescription(p.Translations, lang),
                TagCount         = p.Tags.Count,
            }).ToList();

            return OperationResult<PagedResult<ProductListItemDto>>.Ok(
                PagedResult<ProductListItemDto>.Create(data, total, page, pageSize));
        }

        public async Task<OperationResult<List<ProductSummaryDto>>> GetAllSimple()
        {
            var companyId = _tenantService.GetCompanyId();

            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderBy(p => p.CategoryId).ThenBy(p => p.Id)
                .ProjectTo<ProductSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<ProductSummaryDto>>.Ok(data);
        }

        public async Task<OperationResult<ProductReadDto>> GetById(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == id && p.CompanyId == companyId)
                .ProjectTo<ProductReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (product is null)
                return OperationResult<ProductReadDto>.NotFound("Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            return OperationResult<ProductReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == id && p.CompanyId == companyId)
                .Include(p => p.Tags)
                    .ThenInclude(t => t.Translations)
                .Include(p => p.Translations)
                .ProjectTo<ProductAdminReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (product is null)
                return OperationResult<ProductAdminReadDto>.NotFound("Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            return OperationResult<ProductAdminReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var categoryBelongs = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);

            if (!categoryBelongs)
                return OperationResult<ProductReadDto>.NotFound("La categoría no se ha encontrado.", errorKey: ErrorKeys.CategoryNotFound);

            await using var tx = await _context.Database.BeginTransactionAsync();

            var product = _mapper.Map<Product>(dto);
            product.CompanyId = companyId;

            if (dto.Image is not null)
                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");

            if (dto.TagIds is { Count: > 0 })
            {
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id) && t.CompanyId == companyId)
                    .ToListAsync();
                product.Tags = tags;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            foreach (var t in dto.Translations.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
            {
                _context.ProductTranslations.Add(new ProductTranslation
                {
                    ProductId        = product.Id,
                    LanguageCode     = t.LanguageCode.Trim().ToLowerInvariant(),
                    Name             = t.Name.Trim(),
                    ShortDescription = t.ShortDescription?.Trim(),
                    LongDescription  = t.LongDescription?.Trim(),
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            await _context.Entry(product).Collection(p => p.Translations).LoadAsync();
            return OperationResult<ProductReadDto>.Ok(_mapper.Map<ProductReadDto>(product));
        }

        public async Task<OperationResult<bool>> Update(ProductUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .Include(p => p.Tags)
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == dto.Id && p.CompanyId == companyId);

            if (product is null)
                return OperationResult<bool>.NotFound("Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            var categoryBelongs = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);

            if (!categoryBelongs)
                return OperationResult<bool>.NotFound("La categoría no se ha encontrado a tu empresa.", errorKey: ErrorKeys.CategoryNotFound);

            await using var tx = await _context.Database.BeginTransactionAsync();

            _mapper.Map(dto, product);

            if (dto.Image is not null)
            {
                _fileStorage.DeleteFile(product.MainImageUrl ?? "", "products");
                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");
            }

            if (dto.TagIds is not null)
            {
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id) && t.CompanyId == companyId)
                    .ToListAsync();
                product.Tags = tags;
            }

            // Replace-all para traducciones
            var incoming = dto.Translations
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .ToDictionary(t => t.LanguageCode.Trim().ToLowerInvariant());

            var toDelete = product.Translations
                .Where(t => !incoming.ContainsKey(t.LanguageCode))
                .ToList();
            _context.ProductTranslations.RemoveRange(toDelete);

            foreach (var (code, input) in incoming)
            {
                var existing = product.Translations.FirstOrDefault(t => t.LanguageCode == code);
                if (existing is null)
                {
                    _context.ProductTranslations.Add(new ProductTranslation
                    {
                        ProductId        = product.Id,
                        LanguageCode     = code,
                        Name             = input.Name.Trim(),
                        ShortDescription = input.ShortDescription?.Trim(),
                        LongDescription  = input.LongDescription?.Trim(),
                    });
                }
                else
                {
                    existing.Name             = input.Name.Trim();
                    existing.ShortDescription = input.ShortDescription?.Trim();
                    existing.LongDescription  = input.LongDescription?.Trim();
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                return OperationResult<bool>.NotFound("Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            product.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<List<TagTooltipDto>>> GetTagNames(int productId, string? lang)
        {
            var companyId = _tenantService.GetCompanyId();

            var tags = await _context.Tags
                .AsNoTracking()
                .Where(t => t.Products.Any(p => p.Id == productId && p.CompanyId == companyId))
                .Select(t => new { t.Color, Translations = t.Translations.ToList() })
                .ToListAsync();

            var result = tags
                .Select(t => new TagTooltipDto(
                    Name:  ResolveTagNameFromTranslations(t.Translations, lang),
                    Color: t.Color))
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToList();

            return OperationResult<List<TagTooltipDto>>.Ok(result);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string ResolveTagNameFromTranslations(IEnumerable<TagTranslation> translations, string? lang)
        {
            var list = translations.ToList();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var match = list.FirstOrDefault(t => t.LanguageCode == lang);
                if (match is not null) return match.Name;
            }
            return list.FirstOrDefault()?.Name ?? string.Empty;
        }

        private static string ResolveProductName(IEnumerable<ProductTranslation> translations, string? lang)
        {
            var list = translations.ToList();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var match = list.FirstOrDefault(t => t.LanguageCode == lang);
                if (match is not null) return match.Name;
            }
            return list.FirstOrDefault()?.Name ?? string.Empty;
        }

        private static string? ResolveShortDescription(IEnumerable<ProductTranslation> translations, string? lang)
        {
            var list = translations.ToList();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var match = list.FirstOrDefault(t => t.LanguageCode == lang);
                if (match is not null) return match.ShortDescription;
            }
            return list.FirstOrDefault()?.ShortDescription;
        }

        private static string ResolveCategoryName(IEnumerable<CategoryTranslation>? translations, string? lang)
        {
            if (translations is null) return string.Empty;
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
