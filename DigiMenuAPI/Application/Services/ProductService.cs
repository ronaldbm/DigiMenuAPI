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

        public async Task<OperationResult<PagedResult<ProductReadDto>>> GetAll(int page = 1, int pageSize = 20)
        {
            var companyId = _tenantService.GetCompanyId();

            // QueryFilter global ya aplica !IsDeleted — solo falta filtrar por tenant
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderBy(p => p.CategoryId).ThenBy(p => p.Name);

            var total = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ProductReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<PagedResult<ProductReadDto>>.Ok(
                PagedResult<ProductReadDto>.Create(data, total, page, pageSize));
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
                return OperationResult<ProductReadDto>.Fail("Producto no encontrado.");

            return OperationResult<ProductReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            // Incluye traducciones y tags completos para el formulario de edición
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == id && p.CompanyId == companyId)
                .Include(p => p.Tags)
                    .ThenInclude(t => t.Translations)
                .Include(p => p.Translations)
                .ProjectTo<ProductAdminReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (product is null)
                return OperationResult<ProductAdminReadDto>.Fail("Producto no encontrado.");

            return OperationResult<ProductAdminReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            // Validar que la categoría pertenece al mismo tenant (QueryFilter cubre !IsDeleted)
            var categoryBelongs = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);

            if (!categoryBelongs)
                return OperationResult<ProductReadDto>.Fail("La categoría no se ha encontrado.");

            var exists = await _context.Products
                .AnyAsync(p => p.CompanyId == companyId && p.Name == dto.Name.Trim());
            if (exists)
                return OperationResult<ProductReadDto>.Conflict(
                    "Ya existe un producto con ese nombre en tu empresa.",
                    ErrorKeys.ProductAlreadyExists);

            var product = _mapper.Map<Product>(dto);
            product.CompanyId = companyId; // ← siempre desde JWT, nunca del cliente

            if (dto.Image is not null)
                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");

            if (dto.TagIds is { Count: > 0 })
            {
                // Solo tags del mismo tenant — QueryFilter cubre !IsDeleted
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id) && t.CompanyId == companyId)
                    .ToListAsync();
                product.Tags = tags;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<ProductReadDto>.Ok(_mapper.Map<ProductReadDto>(product));
        }

        public async Task<OperationResult<bool>> Update(ProductUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == dto.Id && p.CompanyId == companyId);

            if (product is null)
                return OperationResult<bool>.NotFound("Producto no encontrado.", errorKey: ErrorKeys.ProductNotFound);

            // Validar que la nueva categoría pertenece al mismo tenant
            var categoryBelongs = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == companyId);

            if (!categoryBelongs)
                return OperationResult<bool>.NotFound("La categoría no se ha encontrado a tu empresa.", errorKey: ErrorKeys.CategoryNotFound);

            _mapper.Map(dto, product);

            if (dto.Image is not null)
            {
                _fileStorage.DeleteFile(product.MainImageUrl ?? "", "products");
                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");
            }

            if (dto.TagIds is not null)
            {
                // Solo tags del mismo tenant — QueryFilter cubre !IsDeleted
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id) && t.CompanyId == companyId)
                    .ToListAsync();
                product.Tags = tags;
            }

            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                return OperationResult<bool>.Forbidden("Producto no encontrado.", errorKey: ErrorKeys.Forbidden);

            product.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }
    }
}