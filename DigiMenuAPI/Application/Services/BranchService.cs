using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
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
        /// Inicializa las 4 entidades de configuración de una branch nueva
        /// con valores por defecto. Mismo patrón que AuthService.RegisterCompany.
        /// </summary>
        private void InitializeBranchConfigAsync(
            int branchId, string branchName, Company company)
        {
            // Resolver CountryCode desde la empresa para aplicar defaults regionales
            var countryCode = company.CountryCode;

            _context.BranchInfos.Add(new BranchInfo
            {
                BranchId = branchId,
                BusinessName = branchName
            });

            _context.BranchThemes.Add(new BranchTheme
            {
                BranchId = branchId,
                IsDarkMode = false,
                PageBackgroundColor = "#F1FAEE",
                HeaderBackgroundColor = "#FFFFFF",
                HeaderTextColor = "#1D3557",
                TabBackgroundColor = "#1D3557",
                TabTextColor = "#FFFFFF",
                PrimaryColor = "#E63946",
                PrimaryTextColor = "#FFFFFF",
                SecondaryColor = "#457B9D",
                TitlesColor = "#1D3557",
                TextColor = "#1D3557",
                BrowserThemeColor = "#FFFFFF",
                HeaderStyle = 1,
                MenuLayout = 1,
                ProductDisplay = 1,
                ShowProductDetails = true,
                ShowSearchButton = false,
                ShowContactButton = false
            });

            _context.BranchLocales.Add(new BranchLocale
            {
                BranchId = branchId,
                CountryCode = countryCode?.ToUpper() ?? "CR",
                PhoneCode = LocaleHelper.ResolvePhoneCode(countryCode),
                Currency = LocaleHelper.ResolveCurrency(countryCode),
                CurrencyLocale = LocaleHelper.ResolveCurrencyLocale(countryCode),
                Language = "es",
                TimeZone = LocaleHelper.ResolveTimeZone(countryCode),
                Decimals = 2
            });

            _context.BranchSeos.Add(new BranchSeo
            {
                BranchId = branchId
            });

        }
    }
}