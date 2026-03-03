using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class SettingService : ISettingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public SettingService(
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

        public async Task<OperationResult<SettingReadDto>> Get()
        {
            var companyId = _tenantService.GetCompanyId();

            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (setting is null)
                return OperationResult<SettingReadDto>.Fail("Configuración no encontrada.");

            return OperationResult<SettingReadDto>.Ok(_mapper.Map<SettingReadDto>(setting));
        }

        public async Task<OperationResult<bool>> Update(SettingUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (setting is null)
                return OperationResult<bool>.Fail("Configuración no encontrada.");

            // Guardar URL anterior por si hay que borrar archivo
            string? oldLogoUrl = setting.LogoUrl;

            _mapper.Map(dto, setting);

            // Procesar logo si se envió
            if (dto.Logo != null && dto.Logo.Length > 0)
            {
                var ext = Path.GetExtension(dto.Logo.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };

                if (!allowed.Contains(ext))
                    return OperationResult<bool>.Fail("Formato de imagen no permitido.");

                if (!string.IsNullOrEmpty(oldLogoUrl))
                    _fileStorage.DeleteFile(oldLogoUrl, "logos");

                setting.LogoUrl = await _fileStorage.SaveFile(dto.Logo, "logos");
            }

            await _context.SaveChangesAsync();
            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }
    }
}
