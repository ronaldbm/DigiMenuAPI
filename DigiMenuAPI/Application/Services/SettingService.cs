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

        public async Task<OperationResult<SettingReadDto>> Get(int branchId)
        {
            // Valida que la Branch existe y pertenece al tenant autenticado
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            // Setting es 1:1 con Branch — se filtra por BranchId, NO por CompanyId
            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BranchId == branchId);

            if (setting is null)
                return OperationResult<SettingReadDto>.Fail("Configuración no encontrada.");

            return OperationResult<SettingReadDto>.Ok(_mapper.Map<SettingReadDto>(setting));
        }

        public async Task<OperationResult<bool>> Update(SettingUpdateDto dto)
        {
            // Valida que la Branch pertenece al tenant antes de cualquier operación
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            // Setting es 1:1 con Branch — filtro por BranchId
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.BranchId == dto.BranchId);

            if (setting is null)
                return OperationResult<bool>.Fail("Configuración no encontrada.");

            var oldLogoUrl = setting.LogoUrl;
            _mapper.Map(dto, setting);

            if (dto.Logo is { Length: > 0 })
            {
                var ext = Path.GetExtension(dto.Logo.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };

                if (!allowed.Contains(ext))
                    return OperationResult<bool>.Fail("Formato no permitido. Usa JPG, PNG, WEBP o SVG.");

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