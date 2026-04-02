using AutoMapper;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class SettingService : ISettingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly ICacheService _cache;
        private readonly IModuleGuard _moduleGuard;

        public SettingService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            IFileStorageService fileStorage,
            ICacheService cache,
            IModuleGuard moduleGuard)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _fileStorage = fileStorage;
            _cache = cache;
            _moduleGuard = moduleGuard;
        }

        // ── Company-level: LECTURA ────────────────────────────────────

        public async Task<OperationResult<CompanySettingsReadDto>> GetCompanySettings()
        {
            var companyId = _tenantService.GetCompanyId();

            var info = await GetCompanyInfoInternal(companyId);
            var theme = await GetCompanyThemeInternal(companyId);
            var seo = await GetCompanySeoInternal(companyId);

            if (info is null || theme is null || seo is null)
                return OperationResult<CompanySettingsReadDto>.Fail(
                    "Configuración de empresa incompleta. Contacta a soporte.");

            return OperationResult<CompanySettingsReadDto>.Ok(
                new CompanySettingsReadDto(info, theme, seo));
        }

        public async Task<OperationResult<CompanyInfoReadDto>> GetCompanyInfo()
        {
            var companyId = _tenantService.GetCompanyId();
            var result = await GetCompanyInfoInternal(companyId);
            return result is null
                ? OperationResult<CompanyInfoReadDto>.Fail("Información no encontrada.")
                : OperationResult<CompanyInfoReadDto>.Ok(result);
        }

        public async Task<OperationResult<CompanyThemeReadDto>> GetCompanyTheme()
        {
            var companyId = _tenantService.GetCompanyId();
            var result = await GetCompanyThemeInternal(companyId);
            return result is null
                ? OperationResult<CompanyThemeReadDto>.Fail("Tema no encontrado.")
                : OperationResult<CompanyThemeReadDto>.Ok(result);
        }

        public async Task<OperationResult<CompanySeoReadDto>> GetCompanySeo()
        {
            var companyId = _tenantService.GetCompanyId();
            var result = await GetCompanySeoInternal(companyId);
            return result is null
                ? OperationResult<CompanySeoReadDto>.Fail("SEO no encontrado.")
                : OperationResult<CompanySeoReadDto>.Ok(result);
        }

        // ── Company-level: ACTUALIZACIÓN ─────────────────────────────

        public async Task<OperationResult<CompanyInfoReadDto>> UpdateCompanyInfo(
            CompanyInfoUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var info = await _context.CompanyInfos
                .FirstOrDefaultAsync(i => i.CompanyId == companyId);

            if (info is null)
                return OperationResult<CompanyInfoReadDto>.Fail("Información no encontrada.");

            // Procesar imágenes — solo se reemplazan si se envía un archivo nuevo
            if (dto.Logo is { Length: > 0 })
            {
                AssertImageExtension(dto.Logo.FileName);
                _fileStorage.DeleteFile(info.LogoUrl ?? "", "company-info");
                info.LogoUrl = await _fileStorage.SaveFile(dto.Logo, "company-info");
            }

            if (dto.Favicon is { Length: > 0 })
            {
                AssertImageExtension(dto.Favicon.FileName);
                _fileStorage.DeleteFile(info.FaviconUrl ?? "", "company-info");
                info.FaviconUrl = await _fileStorage.SaveFile(dto.Favicon, "company-info");
            }

            if (dto.BackgroundImage is { Length: > 0 })
            {
                AssertImageExtension(dto.BackgroundImage.FileName);
                _fileStorage.DeleteFile(info.BackgroundImageUrl ?? "", "company-info");
                info.BackgroundImageUrl = await _fileStorage.SaveFile(
                    dto.BackgroundImage, "company-info");
            }

            info.BusinessName = dto.BusinessName.Trim();
            info.Tagline = dto.Tagline?.Trim();

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<CompanyInfoReadDto>.Ok(_mapper.Map<CompanyInfoReadDto>(info));
        }

        public async Task<OperationResult<CompanyThemeReadDto>> UpdateCompanyTheme(
            CompanyThemeUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var theme = await _context.CompanyThemes
                .FirstOrDefaultAsync(t => t.CompanyId == companyId);

            if (theme is null)
                return OperationResult<CompanyThemeReadDto>.Fail("Tema no encontrado.");

            _mapper.Map(dto, theme);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<CompanyThemeReadDto>.Ok(
                _mapper.Map<CompanyThemeReadDto>(theme));
        }

        public async Task<OperationResult<CompanySeoReadDto>> UpdateCompanySeo(
            CompanySeoUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var seo = await _context.CompanySeos
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (seo is null)
                return OperationResult<CompanySeoReadDto>.Fail("SEO no encontrado.");

            _mapper.Map(dto, seo);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<CompanySeoReadDto>.Ok(_mapper.Map<CompanySeoReadDto>(seo));
        }

        // ── Branch-level: LECTURA ─────────────────────────────────────

        public async Task<OperationResult<BranchSettingsReadDto>> GetBranchSettings(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var companyId = _tenantService.GetCompanyId();
            var hasReservations = await _moduleGuard.HasModuleAsync(
                companyId, ModuleCodes.Reservations);

            var locale = await GetLocaleInternal(branchId);

            BranchReservationFormReadDto? reservationForm = null;
            if (hasReservations)
                reservationForm = await GetReservationFormInternal(branchId);

            if (locale is null)
                return OperationResult<BranchSettingsReadDto>.Fail(
                    "Configuración de sucursal incompleta. Contacta a soporte.");

            return OperationResult<BranchSettingsReadDto>.Ok(
                new BranchSettingsReadDto(locale, reservationForm));
        }

        public async Task<OperationResult<BranchLocaleReadDto>> GetBranchLocale(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var result = await GetLocaleInternal(branchId);
            return result is null
                ? OperationResult<BranchLocaleReadDto>.Fail("Localización no encontrada.")
                : OperationResult<BranchLocaleReadDto>.Ok(result);
        }

        public async Task<OperationResult<BranchReservationFormReadDto>> GetReservationForm(
            int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var companyId = _tenantService.GetCompanyId();
            await _moduleGuard.AssertModuleAsync(companyId, ModuleCodes.Reservations);

            var result = await GetReservationFormInternal(branchId);
            return result is null
                ? OperationResult<BranchReservationFormReadDto>.Fail(
                    "Configuración del formulario no encontrada.")
                : OperationResult<BranchReservationFormReadDto>.Ok(result);
        }

        // ── Branch-level: ACTUALIZACIÓN ───────────────────────────────

        public async Task<OperationResult<BranchLocaleReadDto>> UpdateBranchLocale(
            BranchLocaleUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var locale = await _context.BranchLocales
                .FirstOrDefaultAsync(l => l.BranchId == dto.BranchId);

            if (locale is null)
                return OperationResult<BranchLocaleReadDto>.Fail("Localización no encontrada.");

            _mapper.Map(dto, locale);
            await _context.SaveChangesAsync();
            // Locale afecta la renderización del menú público
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return OperationResult<BranchLocaleReadDto>.Ok(
                _mapper.Map<BranchLocaleReadDto>(locale));
        }

        public async Task<OperationResult<BranchReservationFormReadDto>> UpdateReservationForm(
            BranchReservationFormUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);
            var companyId = _tenantService.GetCompanyId();
            await _moduleGuard.AssertModuleAsync(companyId, ModuleCodes.Reservations);

            // Validar regla de negocio: un campo no puede ser requerido si no se muestra
            var validationError = ValidateFormFields(dto);
            if (validationError is not null)
                return OperationResult<BranchReservationFormReadDto>.Fail(validationError);

            var form = await _context.BranchReservationForms
                .FirstOrDefaultAsync(f => f.BranchId == dto.BranchId);

            if (form is null)
                return OperationResult<BranchReservationFormReadDto>.Fail(
                    "Configuración del formulario no encontrada.");

            _mapper.Map(dto, form);
            await _context.SaveChangesAsync();

            return OperationResult<BranchReservationFormReadDto>.Ok(
                _mapper.Map<BranchReservationFormReadDto>(form));
        }

        // ── Helpers privados ──────────────────────────────────────────

        private async Task<CompanyInfoReadDto?> GetCompanyInfoInternal(int companyId)
            => _mapper.Map<CompanyInfoReadDto?>(
                await _context.CompanyInfos.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.CompanyId == companyId));

        private async Task<CompanyThemeReadDto?> GetCompanyThemeInternal(int companyId)
            => _mapper.Map<CompanyThemeReadDto?>(
                await _context.CompanyThemes.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.CompanyId == companyId));

        private async Task<CompanySeoReadDto?> GetCompanySeoInternal(int companyId)
            => _mapper.Map<CompanySeoReadDto?>(
                await _context.CompanySeos.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CompanyId == companyId));

        private async Task<BranchLocaleReadDto?> GetLocaleInternal(int branchId)
            => _mapper.Map<BranchLocaleReadDto?>(
                await _context.BranchLocales.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BranchId == branchId));

        private async Task<BranchReservationFormReadDto?> GetReservationFormInternal(int branchId)
            => _mapper.Map<BranchReservationFormReadDto?>(
                await _context.BranchReservationForms.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.BranchId == branchId));

        /// <summary>
        /// Valida que ningún campo sea requerido si no está visible.
        /// Devuelve mensaje de error o null si todo está bien.
        /// </summary>
        private static string? ValidateFormFields(BranchReservationFormUpdateDto dto)
        {
            if (dto.FormRequireName && !dto.FormShowName)
                return "El campo 'Nombre' no puede ser requerido si no está visible.";
            if (dto.FormRequirePhone && !dto.FormShowPhone)
                return "El campo 'Teléfono' no puede ser requerido si no está visible.";
            if (dto.FormRequireTable && !dto.FormShowTable)
                return "El campo 'Mesa' no puede ser requerido si no está visible.";
            if (dto.FormRequirePersons && !dto.FormShowPersons)
                return "El campo 'Personas' no puede ser requerido si no está visible.";
            if (dto.FormRequireAllergies && !dto.FormShowAllergies)
                return "El campo 'Alergias' no puede ser requerido si no está visible.";
            if (dto.FormRequireBirthday && !dto.FormShowBirthday)
                return "El campo 'Cumpleaños' no puede ser requerido si no está visible.";
            if (dto.FormRequireComments && !dto.FormShowComments)
                return "El campo 'Comentarios' no puede ser requerido si no está visible.";
            return null;
        }

        // ── Contact: LECTURA ──────────────────────────────────────────

        public async Task<OperationResult<CompanyContactReadDto>> GetCompanyContact()
        {
            var role = _tenantService.GetUserRole();
            if (UserRoles.NeedsBranch(role))
                return OperationResult<CompanyContactReadDto>.Fail(
                    "No tienes permiso para ver los datos de contacto de la empresa.");

            var companyId = _tenantService.GetCompanyId();
            var company = await _context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company is null)
                return OperationResult<CompanyContactReadDto>.Fail("Empresa no encontrada.");

            return OperationResult<CompanyContactReadDto>.Ok(
                new CompanyContactReadDto(company.Id, company.Name, company.Email,
                    company.Phone, company.CountryCode));
        }

        public async Task<OperationResult<BranchContactReadDto>> GetBranchContact(int branchId)
        {
            var role = _tenantService.GetUserRole();
            if (role == UserRoles.Staff)
                return OperationResult<BranchContactReadDto>.Fail(
                    "No tienes permiso para ver los datos de contacto de la sucursal.");

            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var branch = await _context.Branches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch is null)
                return OperationResult<BranchContactReadDto>.Fail("Sucursal no encontrada.");

            return OperationResult<BranchContactReadDto>.Ok(
                new BranchContactReadDto(branch.Id, branch.Name, branch.Address,
                    branch.Phone, branch.Email));
        }

        // ── Contact: ACTUALIZACIÓN ────────────────────────────────────

        public async Task<OperationResult<CompanyContactReadDto>> UpdateCompanyContact(
            CompanyContactUpdateDto dto)
        {
            var role = _tenantService.GetUserRole();
            if (UserRoles.NeedsBranch(role))
                return OperationResult<CompanyContactReadDto>.Fail(
                    "No tienes permiso para modificar los datos de contacto de la empresa.");

            var companyId = _tenantService.GetCompanyId();
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company is null)
                return OperationResult<CompanyContactReadDto>.Fail("Empresa no encontrada.");

            company.Name        = dto.Name.Trim();
            company.Email       = dto.Email?.Trim() ?? company.Email;
            company.Phone       = dto.Phone?.Trim();
            company.CountryCode = dto.CountryCode?.Trim();

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<CompanyContactReadDto>.Ok(
                new CompanyContactReadDto(company.Id, company.Name, company.Email,
                    company.Phone, company.CountryCode));
        }

        public async Task<OperationResult<BranchContactReadDto>> UpdateBranchContact(
            int branchId, BranchContactUpdateDto dto)
        {
            var role = _tenantService.GetUserRole();
            if (role == UserRoles.Staff)
                return OperationResult<BranchContactReadDto>.Fail(
                    "No tienes permiso para modificar los datos de contacto de la sucursal.");

            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch is null)
                return OperationResult<BranchContactReadDto>.Fail("Sucursal no encontrada.");

            branch.Name    = dto.Name.Trim();
            branch.Address = dto.Address?.Trim();
            branch.Phone   = dto.Phone?.Trim();
            branch.Email   = dto.Email?.Trim();

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(branchId);

            return OperationResult<BranchContactReadDto>.Ok(
                new BranchContactReadDto(branch.Id, branch.Name, branch.Address,
                    branch.Phone, branch.Email));
        }

        // ── Tabs: ACTUALIZACIÓN ───────────────────────────────────────

        public async Task<OperationResult<CompanyInfoReadDto>> UpdateCompanyTabs(
            CompanyTabsUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var info = await _context.CompanyInfos
                .FirstOrDefaultAsync(i => i.CompanyId == companyId);

            if (info is null)
                return OperationResult<CompanyInfoReadDto>.Fail("Información no encontrada.");

            info.TabsEnabled                 = dto.TabsEnabled;
            info.DefaultMaxOpenTabs          = dto.DefaultMaxOpenTabs;
            info.DefaultMaxTabAmount         = dto.DefaultMaxTabAmount;
            info.TabRequiresManagerApproval   = dto.TabRequiresManagerApproval;
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<CompanyInfoReadDto>.Ok(_mapper.Map<CompanyInfoReadDto>(info));
        }

        /// <summary>Valida extensión de imagen. Lanza si no es permitida.</summary>
        private static void AssertImageExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
            if (!allowed.Contains(ext))
                throw new InvalidOperationException(
                    "Formato de imagen no permitido. Usa JPG, PNG, WEBP o SVG.");
        }
    }
}
