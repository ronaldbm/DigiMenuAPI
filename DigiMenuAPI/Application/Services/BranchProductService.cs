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
    public class BranchProductService : IBranchProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public BranchProductService(
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

        public async Task<OperationResult<List<BranchProductReadDto>>> GetByBranch(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var products = await _context.BranchProducts
                .AsNoTracking()
                .Where(bp => bp.BranchId == branchId)
                .Include(bp => bp.Product)
                .Include(bp => bp.Category)
                .OrderBy(bp => bp.CategoryId).ThenBy(bp => bp.DisplayOrder)
                .ProjectTo<BranchProductReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<BranchProductReadDto>>.Ok(products);
        }

        public async Task<OperationResult<List<BranchCategoryVisibilityDto>>> GetCategoriesWithVisibility(
            int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var companyId = _tenantService.GetCompanyId();

            // Agrupar BranchProducts por categoría para obtener conteos
            var stats = await _context.BranchProducts
                .AsNoTracking()
                .Where(bp => bp.BranchId == branchId)
                .GroupBy(bp => bp.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    TotalProducts = g.Count(),
                    VisibleProducts = g.Count(bp => bp.IsVisible)
                })
                .ToListAsync();

            if (stats.Count == 0)
                return OperationResult<List<BranchCategoryVisibilityDto>>.Ok([]);

            var categoryIds = stats.Select(s => s.CategoryId).ToList();

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id) && c.CompanyId == companyId)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Id, c.Name, c.DisplayOrder })
                .ToListAsync();

            var result = categories.Select(c =>
            {
                var s = stats.First(x => x.CategoryId == c.Id);
                return new BranchCategoryVisibilityDto(
                    c.Id,
                    c.Name,
                    c.DisplayOrder,
                    s.VisibleProducts > 0,
                    s.TotalProducts,
                    s.VisibleProducts);
            }).ToList();

            return OperationResult<List<BranchCategoryVisibilityDto>>.Ok(result);
        }

        public async Task<OperationResult<BranchProductReadDto>> Create(BranchProductCreateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);
            var companyId = _tenantService.GetCompanyId();

            // Validar que el producto pertenece al mismo tenant (QueryFilter cubre !IsDeleted)
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == dto.ProductId && p.CompanyId == companyId);
            if (!productExists)
                return OperationResult<BranchProductReadDto>.NotFound(
                    "Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            // Validar que la categoría pertenece al mismo tenant
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);
            if (!categoryExists)
                return OperationResult<BranchProductReadDto>.NotFound(
                    "Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            // Un producto solo puede activarse una vez por Branch (incluye los soft-deleted)
            var alreadyExists = await _context.BranchProducts
                .IgnoreQueryFilters()
                .AnyAsync(bp => bp.BranchId == dto.BranchId && bp.ProductId == dto.ProductId);
            if (alreadyExists)
                return OperationResult<BranchProductReadDto>.Conflict(
                    "Este producto ya está activado en esta sucursal.",
                    ErrorKeys.BranchProductAlreadyExists);

            var branchProduct = _mapper.Map<BranchProduct>(dto);

            if (dto.ImageOverride is not null)
                branchProduct.ImageOverrideUrl = await _fileStorage.SaveFile(
                    dto.ImageOverride, "branch-products");

            _context.BranchProducts.Add(branchProduct);
            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            // Recargar con navegaciones para construir el DTO completo
            var result = await _context.BranchProducts
                .AsNoTracking()
                .Where(bp => bp.Id == branchProduct.Id)
                .Include(bp => bp.Product)
                .Include(bp => bp.Category)
                .ProjectTo<BranchProductReadDto>(_mapper.ConfigurationProvider)
                .FirstAsync();

            return OperationResult<BranchProductReadDto>.Ok(result);
        }

        public async Task<OperationResult<bool>> Update(BranchProductUpdateDto dto)
        {
            // Cargar primero para obtener BranchId y verificar ownership
            var branchProduct = await _context.BranchProducts
                .FirstOrDefaultAsync(bp => bp.Id == dto.Id);

            if (branchProduct is null)
                return OperationResult<bool>.NotFound(
                    "BranchProduct no encontrado.", errorKey: ErrorKeys.BranchProductNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(branchProduct.BranchId);
            var companyId = _tenantService.GetCompanyId();

            // Validar categoría destino
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);
            if (!categoryExists)
                return OperationResult<bool>.NotFound(
                    "Categoría no encontrada.", errorKey: ErrorKeys.CategoryNotFound);

            _mapper.Map(dto, branchProduct);

            if (dto.ImageOverride is not null)
            {
                _fileStorage.DeleteFile(branchProduct.ImageOverrideUrl ?? "", "branch-products");
                branchProduct.ImageOverrideUrl = await _fileStorage.SaveFile(
                    dto.ImageOverride, "branch-products");
            }

            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> ToggleVisibility(int id)
        {
            var branchProduct = await _context.BranchProducts
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (branchProduct is null)
                return OperationResult<bool>.NotFound(
                    "BranchProduct no encontrado.", errorKey: ErrorKeys.BranchProductNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(branchProduct.BranchId);

            branchProduct.IsVisible = !branchProduct.IsVisible;
            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(branchProduct.IsVisible);
        }

        public async Task<OperationResult<bool>> SetCategoryVisibility(
            int branchId, int categoryId, BranchCategoryVisibilityUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var affected = await _context.BranchProducts
                .Where(bp => bp.BranchId == branchId && bp.CategoryId == categoryId)
                .ExecuteUpdateAsync(s => s.SetProperty(bp => bp.IsVisible, dto.IsVisible));

            if (affected == 0)
                return OperationResult<bool>.NotFound(
                    "No se encontraron productos de esa categoría en esta sucursal.",
                    errorKey: ErrorKeys.CategoryNotFound);

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var branchProduct = await _context.BranchProducts
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (branchProduct is null)
                return OperationResult<bool>.NotFound(
                    "BranchProduct no encontrado.", errorKey: ErrorKeys.BranchProductNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(branchProduct.BranchId);

            branchProduct.IsDeleted = true;
            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }
    }
}
