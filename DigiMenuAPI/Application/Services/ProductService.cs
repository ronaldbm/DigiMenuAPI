using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
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
        private readonly IFileStorageService _fileStorage;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public ProductService(ApplicationDbContext context, IMapper mapper, IFileStorageService fileStorage, IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _fileStorage = fileStorage;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<ProductReadDto>>> GetAll()
        {
            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ProjectTo<ProductReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<ProductReadDto>>.Ok(products);
        }

        public async Task<OperationResult<ProductReadDto>> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Tags)
                .AsNoTracking()
                .ProjectTo<ProductReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
                return OperationResult<ProductReadDto>.Fail("Producto no encontrado");

            return OperationResult<ProductReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Tags)
                .AsNoTracking()
                .ProjectTo<ProductAdminReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return OperationResult<ProductAdminReadDto>.Fail("Producto no encontrado");

            return OperationResult<ProductAdminReadDto>.Ok(product);
        }

        public async Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto)
        {
            var product = _mapper.Map<Product>(dto);

            // Procesar Imagen si existe

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(dto.Image.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return OperationResult<ProductReadDto>.Fail("Formato de imagen no permitido");
                }
                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<ProductReadDto>.Ok(_mapper.Map<ProductReadDto>(product));
        }

        public async Task<OperationResult<bool>> Update(ProductUpdateDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product is null) return OperationResult<bool>.Fail("Producto no encontrado");

            // Guardamos la URL vieja por si hay que borrar el archivo después
            string? oldImageUrl = product.MainImageUrl;

            _mapper.Map(dto, product);

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(dto.Image.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return OperationResult<bool>.Fail("Formato de imagen no permitido");
                }

                // Borrar la anterior del disco
                if (!string.IsNullOrEmpty(oldImageUrl))
                    _fileStorage.DeleteFile(oldImageUrl, "products");

                product.MainImageUrl = await _fileStorage.SaveFile(dto.Image, "products");
            }

            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null)
                return OperationResult<bool>.Fail("Producto no encontrado");

            // 1. Borrar archivo físico
            if (!string.IsNullOrEmpty(product.MainImageUrl))
                _fileStorage.DeleteFile(product.MainImageUrl, "products");

            product.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<bool>.Ok(true);
        }

    }
}