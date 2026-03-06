using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DigiMenuAPI.Application.Services
{
    public class ModuleService : IModuleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IMemoryCache _cache;

        public ModuleService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IMemoryCache cache)
        {
            _context = context;
            _tenantService = tenantService;
            _cache = cache;
        }

        // ── CATÁLOGO (SuperAdmin) ────────────────────────────────────
        public async Task<OperationResult<List<PlatformModuleReadDto>>> GetAllPlatformModules()
        {
            var modules = await _context.PlatformModules
                .AsNoTracking()
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new PlatformModuleReadDto(
                    m.Id, m.Code, m.Name, m.Description, m.IsActive, m.DisplayOrder))
                .ToListAsync();

            return OperationResult<List<PlatformModuleReadDto>>.Ok(modules);
        }

        // ── ACTIVACIONES POR EMPRESA (SuperAdmin) ────────────────────
        public async Task<OperationResult<List<CompanyModuleReadDto>>> GetCompanyModules(int companyId)
        {
            var modules = await _context.CompanyModules
                .AsNoTracking()
                .Include(cm => cm.Company)
                .Include(cm => cm.PlatformModule)
                .Where(cm => cm.CompanyId == companyId)
                .OrderBy(cm => cm.PlatformModule.DisplayOrder)
                .ToListAsync();

            return OperationResult<List<CompanyModuleReadDto>>.Ok(
                modules.Select(MapToDto).ToList());
        }

        public async Task<OperationResult<CompanyModuleReadDto>> ActivateModule(ActivateModuleDto dto)
        {
            // Verificar que la empresa existe
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == dto.CompanyId);
            if (!companyExists)
                return OperationResult<CompanyModuleReadDto>.Fail("Empresa no encontrada.");

            // Verificar que el módulo existe en el catálogo
            var platformModule = await _context.PlatformModules
                .FirstOrDefaultAsync(m => m.Id == dto.PlatformModuleId && m.IsActive);

            if (platformModule is null)
                return OperationResult<CompanyModuleReadDto>.Fail("Módulo no encontrado o no disponible.");

            // Verificar si ya está activado
            var existing = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == dto.CompanyId
                                        && cm.PlatformModuleId == dto.PlatformModuleId);

            if (existing is not null)
            {
                existing.IsActive = true;
                existing.ExpiresAt = dto.ExpiresAt;
                existing.Notes = dto.Notes;
                existing.ActivatedAt = DateTime.UtcNow;
                existing.ActivatedByUserId = _tenantService.GetCurrentUserId();
            }
            else
            {
                existing = new CompanyModule
                {
                    CompanyId = dto.CompanyId,
                    PlatformModuleId = dto.PlatformModuleId,
                    IsActive = true,
                    ExpiresAt = dto.ExpiresAt,
                    Notes = dto.Notes,
                    ActivatedAt = DateTime.UtcNow,
                    ActivatedByUserId = _tenantService.GetCurrentUserId()
                };
                _context.CompanyModules.Add(existing);
            }

            await _context.SaveChangesAsync();

            await _context.Entry(existing).Reference(cm => cm.PlatformModule).LoadAsync();

            return OperationResult<CompanyModuleReadDto>.Ok(MapToDto(existing));
        }

        public async Task<OperationResult<bool>> DeactivateModule(int companyModuleId)
        {
            var module = await _context.CompanyModules.FindAsync(companyModuleId);
            if (module is null)
                return OperationResult<bool>.Fail("Activación no encontrada.");

            module.IsActive = false;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> UpdateModuleExpiry(UpdateModuleExpiryDto dto)
        {
            var module = await _context.CompanyModules.FindAsync(dto.Id);
            if (module is null)
                return OperationResult<bool>.Fail("Activación no encontrada.");

            module.ExpiresAt = dto.ExpiresAt;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── CONSULTA PROPIA (Tenant) ─────────────────────────────────
        public async Task<OperationResult<List<CompanyModuleReadDto>>> GetMyModules()
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var modules = await _context.CompanyModules
                .AsNoTracking()
                .Include(cm => cm.PlatformModule)
                .Where(cm => cm.CompanyId == companyId && cm.IsActive)
                .OrderBy(cm => cm.PlatformModule.DisplayOrder)
                .ToListAsync();

            return OperationResult<List<CompanyModuleReadDto>>.Ok(
                modules.Select(MapToDto).ToList());
        }

        // ── Helpers ──────────────────────────────────────────────────
        private static CompanyModuleReadDto MapToDto(CompanyModule cm) => new(
            cm.Id,
            cm.CompanyId,
            cm.PlatformModuleId,
            cm.PlatformModule.Name,
            cm.PlatformModule.Code,
            cm.IsActive,
            cm.ActivatedAt,
            cm.ExpiresAt,
            cm.Notes
        );
    }
}