using AutoMapper;
using AppCore.Application.Common;
using AppCore.Application.Utils;
using AppCore.Domain.Entities;
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
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ICacheService _cache;
        private readonly IMapper _mapper;

        public BranchService(
            ApplicationDbContext context,
            ITenantService tenantService,
            ICacheService cache,
            IMapper mapper)
        {
            _context = context;
            _tenantService = tenantService;
            _cache = cache;
            _mapper = mapper;
        }

        // ── GET ALL ───────────────────────────────────────────────────
        public async Task<OperationResult<List<BranchSummaryDto>>> GetAll()
        {
            var companyId = _tenantService.GetCompanyId();

            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new BranchSummaryDto(
                    b.Id,
                    b.Name,
                    b.Slug,
                    b.IsActive))
                .ToListAsync();

            return OperationResult<List<BranchSummaryDto>>.Ok(branches);
        }

        // ── GET BY ID ─────────────────────────────────────────────────
        public async Task<OperationResult<BranchReadDto>> GetById(int branchId)
        {
            var companyId = _tenantService.GetCompanyId();

            var branch = await _context.Branches
                .AsNoTracking()
                .Include(b => b.Locale)
                .FirstOrDefaultAsync(b =>
                    b.Id == branchId &&
                    b.CompanyId == companyId &&
                    !b.IsDeleted);

            if (branch is null)
                return OperationResult<BranchReadDto>.NotFound(
                    "Sucursal no encontrada.",
                    ErrorKeys.BranchNotFound);

            return OperationResult<BranchReadDto>.Ok(_mapper.Map<BranchReadDto>(branch));
        }

        // ── CREATE ────────────────────────────────────────────────────
        public async Task<OperationResult<BranchReadDto>> Create(BranchCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            // Validar límite de branches del plan
            var company = await _context.Companies
                .AsNoTracking()
                .FirstAsync(c => c.Id == companyId);

            if (company.MaxBranches != -1)
            {
                var currentCount = await _context.Branches
                    .CountAsync(b => b.CompanyId == companyId && !b.IsDeleted);

                if (currentCount >= company.MaxBranches)
                    return OperationResult<BranchReadDto>.Conflict(
                        $"Tu plan permite un máximo de {company.MaxBranches} sucursales.",
                        ErrorKeys.BranchLimitReached);
            }

            // Generar/validar slug único dentro de la Company
            var existingSlugs = await _context.Branches
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Select(b => b.Slug)
                .ToListAsync();

            var slug = string.IsNullOrWhiteSpace(dto.Slug)
                ? SlugHelper.GenerateUnique(dto.Name, existingSlugs)
                : SlugHelper.Slugify(dto.Slug);

            if (existingSlugs.Contains(slug))
                return OperationResult<BranchReadDto>.Conflict(
                    "El slug ya está en uso por otra sucursal de tu empresa.",
                    ErrorKeys.BranchSlugAlreadyExists);

            // Crear Branch
            var branch = new Branch
            {
                CompanyId = companyId,
                Name = dto.Name.Trim(),
                Slug = slug,
                Address = dto.Address?.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim().ToLower(),
                IsActive = true
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // Inicializar las 4 entidades de configuración con valores por defecto
            // Los mismos defaults que AuthService usa al registrar la primera branch
            InitializeBranchConfigAsync(branch.Id, dto.Name.Trim(), company);

            await _context.SaveChangesAsync();

            var result = _mapper.Map<BranchReadDto>(branch);
            return OperationResult<BranchReadDto>.Ok(result);
        }

        // ── UPDATE ────────────────────────────────────────────────────
        public async Task<OperationResult<BranchReadDto>> Update(BranchUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b =>
                    b.Id == dto.Id &&
                    b.CompanyId == companyId &&
                    !b.IsDeleted);

            if (branch is null)
                return OperationResult<BranchReadDto>.NotFound(
                    "Sucursal no encontrada.",
                    ErrorKeys.BranchNotFound);

            // Validar slug único si cambió
            var newSlug = SlugHelper.Slugify(dto.Slug);
            if (newSlug != branch.Slug)
            {
                var slugExists = await _context.Branches
                    .AnyAsync(b =>
                        b.CompanyId == companyId &&
                        b.Slug == newSlug &&
                        b.Id != dto.Id &&
                        !b.IsDeleted);

                if (slugExists)
                    return OperationResult<BranchReadDto>.Conflict(
                        "El slug ya está en uso por otra sucursal de tu empresa.",
                        ErrorKeys.BranchSlugAlreadyExists);
            }

            branch.Name = dto.Name.Trim();
            branch.Slug = newSlug;
            branch.Address = dto.Address?.Trim();
            branch.Phone = dto.Phone?.Trim();
            branch.Email = dto.Email?.Trim().ToLower();

            await _context.SaveChangesAsync();

            // Invalidar cache del menú público — el slug pudo haber cambiado
            await _cache.EvictMenuByBranchAsync(branch.Id);

            return OperationResult<BranchReadDto>.Ok(_mapper.Map<BranchReadDto>(branch));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────
        public async Task<OperationResult<bool>> ToggleActive(int branchId)
        {
            var companyId = _tenantService.GetCompanyId();

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b =>
                    b.Id == branchId &&
                    b.CompanyId == companyId &&
                    !b.IsDeleted);

            if (branch is null)
                return OperationResult<bool>.NotFound(
                    "Sucursal no encontrada.",
                    ErrorKeys.BranchNotFound);

            branch.IsActive = !branch.IsActive;
            await _context.SaveChangesAsync();

            // Invalidar cache — el menú público debe dejar de responder si se desactiva
            await _cache.EvictMenuByBranchAsync(branchId);

            return OperationResult<bool>.Ok(true);
        }

        // ── DELETE ────────────────────────────────────────────────────
        public async Task<OperationResult<bool>> Delete(int branchId)
        {
            var companyId = _tenantService.GetCompanyId();

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b =>
                    b.Id == branchId &&
                    b.CompanyId == companyId &&
                    !b.IsDeleted);

            if (branch is null)
                return OperationResult<bool>.NotFound(
                    "Sucursal no encontrada.",
                    ErrorKeys.BranchNotFound);

            // No eliminar si tiene usuarios activos asignados
            var hasActiveUsers = await _context.Users
                .AnyAsync(u =>
                    u.BranchId == branchId &&
                    u.IsActive &&
                    !u.IsDeleted);

            if (hasActiveUsers)
                return OperationResult<bool>.Conflict(
                    "No puedes eliminar una sucursal con usuarios activos asignados. " +
                    "Desactívalos o reasígnalos primero.",
                    ErrorKeys.BranchHasActiveUsers);

            branch.IsDeleted = true;
            branch.IsActive = false;
            await _context.SaveChangesAsync();

            // Invalidar cache — el menú de esta branch ya no debe responder
            await _cache.EvictMenuByBranchAsync(branchId);

            return OperationResult<bool>.Ok(true);
        }

        // ── HELPERS ───────────────────────────────────────────────────

        /// <summary>
        /// Inicializa las entidades de configuración de nivel Branch para una branch nueva
        /// con valores por defecto.
        /// Info, Theme y Seo ahora son de nivel Company — solo se inicializa Locale aquí.
        /// </summary>
        private void InitializeBranchConfigAsync(
            int branchId, string branchName, Company company)
        {
            // Resolver CountryCode desde la empresa para aplicar defaults regionales
            var countryCode = company.CountryCode;

            _context.BranchLocales.Add(
                BranchLocaleInitializer.Create(branchId, countryCode));
        }
    }
}