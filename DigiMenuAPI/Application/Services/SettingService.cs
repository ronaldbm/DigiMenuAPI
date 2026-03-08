using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
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

        // ── LECTURA ───────────────────────────────────────────────────

        public async Task<OperationResult<BranchSettingsReadDto>> GetAll(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            // Verificar módulo RESERVATIONS para incluir o no el formulario
            var companyId = _tenantService.GetCompanyId();
            var hasReservations = await _moduleGuard.HasModuleAsync(
                companyId, ModuleCodes.Reservations);

            var info = await GetInfoInternal(branchId);
            var theme = await GetThemeInternal(branchId);
            var locale = await GetLocaleInternal(branchId);
            var seo = await GetSeoInternal(branchId);

            BranchReservationFormReadDto? reservationForm = null;
            if (hasReservations)
                reservationForm = await GetReservationFormInternal(branchId);

            if (info is null || theme is null || locale is null || seo is null)
                return OperationResult<BranchSettingsReadDto>.Fail(
                    "Configuración incompleta. Contacta a soporte.");

            return OperationResult<BranchSettingsReadDto>.Ok(
                new BranchSettingsReadDto(info, theme, locale, seo, reservationForm));
        }

        public async Task<OperationResult<BranchInfoReadDto>> GetInfo(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var result = await GetInfoInternal(branchId);
            return result is null
                ? OperationResult<BranchInfoReadDto>.Fail("Información no encontrada.")
                : OperationResult<BranchInfoReadDto>.Ok(result);
        }

        public async Task<OperationResult<BranchThemeReadDto>> GetTheme(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var result = await GetThemeInternal(branchId);
            return result is null
                ? OperationResult<BranchThemeReadDto>.Fail("Tema no encontrado.")
                : OperationResult<BranchThemeReadDto>.Ok(result);
        }

        public async Task<OperationResult<BranchLocaleReadDto>> GetLocale(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var result = await GetLocaleInternal(branchId);
            return result is null
                ? OperationResult<BranchLocaleReadDto>.Fail("Localización no encontrada.")
                : OperationResult<BranchLocaleReadDto>.Ok(result);
        }

        public async Task<OperationResult<BranchSeoReadDto>> GetSeo(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);
            var result = await GetSeoInternal(branchId);
            return result is null
                ? OperationResult<BranchSeoReadDto>.Fail("SEO no encontrado.")
                : OperationResult<BranchSeoReadDto>.Ok(result);
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

        // ── ACTUALIZACIÓN ─────────────────────────────────────────────

        public async Task<OperationResult<BranchInfoReadDto>> UpdateInfo(BranchInfoUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var info = await _context.BranchInfos
                .FirstOrDefaultAsync(i => i.BranchId == dto.BranchId);

            if (info is null)
                return OperationResult<BranchInfoReadDto>.Fail("Información no encontrada.");

            // Procesar imágenes — solo se reemplazan si se envía un archivo nuevo
            if (dto.Logo is { Length: > 0 })
            {
                AssertImageExtension(dto.Logo.FileName);
                _fileStorage.DeleteFile(info.LogoUrl ?? "", "branch-info");
                info.LogoUrl = await _fileStorage.SaveFile(dto.Logo, "branch-info");
            }

            if (dto.Favicon is { Length: > 0 })
            {
                AssertImageExtension(dto.Favicon.FileName);
                _fileStorage.DeleteFile(info.FaviconUrl ?? "", "branch-info");
                info.FaviconUrl = await _fileStorage.SaveFile(dto.Favicon, "branch-info");
            }

            if (dto.BackgroundImage is { Length: > 0 })
            {
                AssertImageExtension(dto.BackgroundImage.FileName);
                _fileStorage.DeleteFile(info.BackgroundImageUrl ?? "", "branch-info");
                info.BackgroundImageUrl = await _fileStorage.SaveFile(
                    dto.BackgroundImage, "branch-info");
            }

            info.BusinessName = dto.BusinessName.Trim();
            info.Tagline = dto.Tagline?.Trim();

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return OperationResult<BranchInfoReadDto>.Ok(_mapper.Map<BranchInfoReadDto>(info));
        }

        public async Task<OperationResult<BranchThemeReadDto>> UpdateTheme(
            BranchThemeUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var theme = await _context.BranchThemes
                .FirstOrDefaultAsync(t => t.BranchId == dto.BranchId);

            if (theme is null)
                return OperationResult<BranchThemeReadDto>.Fail("Tema no encontrado.");

            _mapper.Map(dto, theme);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return OperationResult<BranchThemeReadDto>.Ok(_mapper.Map<BranchThemeReadDto>(theme));
        }

        public async Task<OperationResult<BranchLocaleReadDto>> UpdateLocale(
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

        public async Task<OperationResult<BranchSeoReadDto>> UpdateSeo(BranchSeoUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var seo = await _context.BranchSeos
                .FirstOrDefaultAsync(s => s.BranchId == dto.BranchId);

            if (seo is null)
                return OperationResult<BranchSeoReadDto>.Fail("SEO no encontrado.");

            _mapper.Map(dto, seo);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return OperationResult<BranchSeoReadDto>.Ok(_mapper.Map<BranchSeoReadDto>(seo));
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

        private async Task<BranchInfoReadDto?> GetInfoInternal(int branchId)
            => _mapper.Map<BranchInfoReadDto?>(
                await _context.BranchInfos.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.BranchId == branchId));

        private async Task<BranchThemeReadDto?> GetThemeInternal(int branchId)
            => _mapper.Map<BranchThemeReadDto?>(
                await _context.BranchThemes.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.BranchId == branchId));

        private async Task<BranchLocaleReadDto?> GetLocaleInternal(int branchId)
            => _mapper.Map<BranchLocaleReadDto?>(
                await _context.BranchLocales.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BranchId == branchId));

        private async Task<BranchSeoReadDto?> GetSeoInternal(int branchId)
            => _mapper.Map<BranchSeoReadDto?>(
                await _context.BranchSeos.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BranchId == branchId));

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