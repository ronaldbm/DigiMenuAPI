using AppCore.Application.Common;
using AppCore.Application.Utils;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly LogMessageDispatcher<StoreService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // AutoMapper eliminado: MenuBranchDto se construye manualmente
        // desde 4 entidades distintas — no hay un mapeo 1:1 posible.
        public StoreService(
            ApplicationDbContext context,
            ITenantService tenantService,
            LogMessageDispatcher<StoreService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<MenuBranchDto>> GetStoreMenu(
            string companySlug, string branchSlug)
        {
            try
            {
                // 1. Resolver Branch por companySlug + branchSlug.
                //    Branch.Slug es único dentro de la Company — se necesitan ambos.
                //    ResolveBySlugAsync valida IsActive y !IsDeleted en ambos niveles.
                var (branchId, companyId) = await _tenantService
                    .ResolveBySlugAsync(companySlug, branchSlug);

                if (branchId is null || companyId is null)
                    return OperationResult<MenuBranchDto>.Fail("Menú no encontrado.");

                // Registrar los tags del tenant en la entrada de OutputCache.
                // Sin esto, EvictByTagAsync("menu-branch:{id}") no encuentra la entrada
                // y la invalidación no tiene efecto hasta que el TTL expire.
                var cacheFeature = _httpContextAccessor.HttpContext?.Features.Get<IOutputCacheFeature>();
                if (cacheFeature is not null)
                {
                    cacheFeature.Context.Tags.Add(CacheKeys.MenuBranch(branchId.Value));
                    cacheFeature.Context.Tags.Add(CacheKeys.MenuCompany(companyId.Value));
                }

                // 2. Leer las entidades de configuración secuencialmente.
                //    DbContext no es thread-safe — Task.WhenAll sobre el mismo contexto
                //    lanzaría "A second operation was started on this context instance".
                //    CompanyInfo, CompanyTheme y CompanySeo son de nivel Company.
                //    BranchLocale es de nivel Branch.
                //    CompanySeo es opcional — el menú funciona sin SEO configurado.
                var branch = await _context.Branches.AsNoTracking()
                    .IgnoreQueryFilters()
                    .Select(br => new { br.Id, br.Phone, br.Email, br.Location })
                    .FirstOrDefaultAsync(br => br.Id == branchId.Value);

                var info = await _context.CompanyInfos.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.CompanyId == companyId.Value);

                var theme = await _context.CompanyThemes.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.CompanyId == companyId.Value);

                var locale = await _context.BranchLocales.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BranchId == branchId.Value);

                var seo = await _context.CompanySeos.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CompanyId == companyId.Value);

                // Info, Theme y Locale son obligatorios — se crean al registrar la empresa
                if (info is null || theme is null || locale is null)
                    return OperationResult<MenuBranchDto>.Fail(
                        "Configuración del menú no disponible.");

                string lang = locale.Language;

                // 3. CategoryIds activas en esta Branch (via BranchProducts visibles)
                var categoryIds = await _context.BranchProducts
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(bp =>
                        bp.BranchId == branchId.Value &&
                        bp.IsVisible &&
                        !bp.IsDeleted)
                    .Select(bp => bp.CategoryId)
                    .Distinct()
                    .ToListAsync();

                // 4. Categorías del catálogo global filtradas por las activas en esta Branch
                var categories = await _context.Categories
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(c =>
                        categoryIds.Contains(c.Id) &&
                        c.CompanyId == companyId.Value &&
                        c.IsVisible &&
                        !c.IsDeleted)
                    .Include(c => c.Translations)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                // 5. BranchProducts visibles con productos, traducciones y tags
                var branchProducts = await _context.BranchProducts
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(bp =>
                        bp.BranchId == branchId.Value &&
                        bp.IsVisible &&
                        !bp.IsDeleted)
                    .Include(bp => bp.Product)
                        .ThenInclude(p => p.Translations)
                    .Include(bp => bp.Product)
                        .ThenInclude(p => p.Tags)
                            .ThenInclude(t => t.Translations)
                    .OrderBy(bp => bp.DisplayOrder)
                    .ToListAsync();

                // 6a. Idiomas habilitados para la Company (movido antes de la construcción del menú
                //     para poder calcular el idioma por defecto y usarlo como fallback)
                var availableLanguages = await _context.CompanyLanguages
                    .AsNoTracking()
                    .Where(cl => cl.CompanyId == companyId.Value)
                    .Include(cl => cl.Language)
                    .OrderBy(cl => cl.Language.DisplayOrder)
                    .Select(cl => new CompanyLanguageReadDto(
                        cl.LanguageCode,
                        cl.Language.Name,
                        cl.Language.Flag,
                        cl.IsDefault))
                    .ToListAsync();

                // 6. Construir CategoryMenuDto con traducción aplicada y fallback al idioma por defecto
                //    (entity.Name ya no existe — el texto vive exclusivamente en las traducciones)
                var defaultLang = availableLanguages.FirstOrDefault(l => l.IsDefault)?.Code ?? lang;

                var categoryDtos = categories.Select(cat =>
                {
                    var catName = cat.Translations
                        .FirstOrDefault(t => t.LanguageCode == lang)?.Name
                        ?? cat.Translations.FirstOrDefault(t => t.LanguageCode == defaultLang)?.Name
                        ?? cat.Translations.FirstOrDefault()?.Name
                        ?? string.Empty;

                    var products = branchProducts
                        .Where(bp => bp.CategoryId == cat.Id)
                        .Select(bp =>
                        {
                            var prodName = bp.Product.Translations
                                .FirstOrDefault(t => t.LanguageCode == lang)?.Name
                                ?? bp.Product.Translations.FirstOrDefault(t => t.LanguageCode == defaultLang)?.Name
                                ?? bp.Product.Translations.FirstOrDefault()?.Name
                                ?? string.Empty;

                            var prodShortDesc = bp.Product.Translations
                                .FirstOrDefault(t => t.LanguageCode == lang)?.ShortDescription
                                ?? bp.Product.Translations.FirstOrDefault(t => t.LanguageCode == defaultLang)?.ShortDescription
                                ?? bp.Product.Translations.FirstOrDefault()?.ShortDescription;

                            var prodLongDesc = bp.Product.Translations
                                .FirstOrDefault(t => t.LanguageCode == lang)?.LongDescription
                                ?? bp.Product.Translations.FirstOrDefault(t => t.LanguageCode == defaultLang)?.LongDescription
                                ?? bp.Product.Translations.FirstOrDefault()?.LongDescription;

                            var tags = bp.Product.Tags
                                .Select(t => new TagMenuDto(
                                    t.Id,
                                    t.Translations
                                        .FirstOrDefault(tr => tr.LanguageCode == lang)?.Name
                                        ?? t.Translations.FirstOrDefault(tr => tr.LanguageCode == defaultLang)?.Name
                                        ?? t.Translations.FirstOrDefault()?.Name
                                        ?? string.Empty,
                                    t.Color))
                                .ToList();

                            return new BranchProductMenuDto(
                                bp.Id,
                                bp.ProductId,
                                prodName,
                                prodShortDesc,
                                prodLongDesc,
                                bp.ImageOverrideUrl ?? bp.Product.MainImageUrl,
                                bp.Price,
                                bp.OfferPrice,
                                bp.DisplayOrder,
                                tags,
                                bp.ImageOverrideUrl != null ? bp.ImageObjectFit     : bp.Product.ImageObjectFit,
                                bp.ImageOverrideUrl != null ? bp.ImageObjectPosition : bp.Product.ImageObjectPosition);
                        })
                        .ToList();

                    return new CategoryMenuDto(cat.Id, catName, cat.DisplayOrder, products);
                }).ToList();

                // 7. FooterLinks de la Branch — QueryFilter aplica !IsDeleted
                var footerLinks = await _context.FooterLinks
                    .AsNoTracking()
                    .Where(f => f.BranchId == branchId.Value)
                    .Include(f => f.StandardIcon)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new FooterLinkReadDto(
                        f.Id,
                        f.Label,
                        f.Url,
                        f.StandardIcon != null
                            ? f.StandardIcon.SvgContent
                            : (f.CustomSvgContent ?? ""),
                        f.DisplayOrder))
                    .ToListAsync();

                // 8. Horario semanal — ordenado Lun-Dom
                var weeklySchedule = await _context.BranchSchedules
                    .AsNoTracking()
                    .Where(s => s.BranchId == branchId.Value)
                    .OrderBy(s => s.DayOfWeek == 0 ? 7 : s.DayOfWeek)
                    .Select(s => new BranchScheduleReadDto(
                        s.Id,
                        s.DayOfWeek,
                        DayNames[s.DayOfWeek],
                        s.IsOpen,
                        s.OpenTime,
                        s.CloseTime))
                    .ToListAsync();

                // 11. Días especiales próximos — desde hoy, máximo 30 días
                var today = DateTime.UtcNow.Date;
                var upcomingSpecialDaysRaw = await _context.BranchSpecialDays
                    .AsNoTracking()
                    .Where(d =>
                        d.BranchId == branchId.Value &&
                        d.Date >= today &&
                        d.Date <= today.AddDays(30))
                    .OrderBy(d => d.Date)
                    .ToListAsync();

                // DateOnly.FromDateTime no se puede traducir a SQL, se convierte en memoria
                var upcomingSpecialDays = upcomingSpecialDaysRaw
                    .Select(d => new BranchSpecialDayReadDto(
                        d.Id,
                        DateOnly.FromDateTime(d.Date),
                        d.IsClosed,
                        d.OpenTime,
                        d.CloseTime,
                        d.Reason,
                        d.CreatedAt))
                    .ToList();

                // 12. Construir MenuBranchDto manualmente desde las 4 entidades.
                //    No se usa AutoMapper porque el origen es multi-entidad
                //    y el destino es un DTO plano compuesto.
                var menuDto = new MenuBranchDto(
                    // Identidad — BranchInfo
                    info.BusinessName,
                    info.Tagline,
                    info.LogoUrl,
                    info.FaviconUrl,
                    info.BackgroundImageUrl,
                    // Tema visual — BranchTheme
                    theme.IsDarkMode,
                    theme.PageBackgroundColor,
                    theme.HeaderBackgroundColor,
                    theme.HeaderTextColor,
                    theme.TabBackgroundColor,
                    theme.TabTextColor,
                    theme.PrimaryColor,
                    theme.PrimaryTextColor,
                    theme.SecondaryColor,
                    theme.TitlesColor,
                    theme.TextColor,
                    theme.BrowserThemeColor,
                    theme.HeaderStyle,
                    theme.MenuLayout,
                    theme.ProductDisplay,
                    theme.ShowProductDetails,
                    theme.FilterMode,
                    theme.ShowContactButton,
                    theme.ShowModalProductInfo,
                    // Localización — BranchLocale
                    locale.Language,
                    locale.Currency,
                    locale.CurrencyLocale,
                    locale.Decimals,
                    // SEO — BranchSeo (opcional)
                    seo?.MetaTitle,
                    seo?.MetaDescription,
                    seo?.GoogleAnalyticsId,
                    seo?.FacebookPixelId,
                    // Contacto
                    branch?.Phone,
                    branch?.Email,
                    // Contenido dinámico
                    categoryDtos,
                    footerLinks,
                    //Horarios
                    WeeklySchedule: weeklySchedule.Any() ? weeklySchedule : null,
                    UpcomingSpecialDays: upcomingSpecialDays.Any() ? upcomingSpecialDays : null,
                    AvailableLanguages: availableLanguages,
                    BranchLatitude:  branch?.Location != null ? (decimal?)branch.Location.Y : null,
                    BranchLongitude: branch?.Location != null ? (decimal?)branch.Location.X : null,
                    ShowMapInMenu:   theme.ShowMapInMenu
                );

                return OperationResult<MenuBranchDto>.Ok(menuDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el menú de la sucursal");
                return OperationResult<MenuBranchDto>.Fail(
                    "Error inesperado al cargar el menú.");
            }
        }

        private static readonly string[] DayNames = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    }
}