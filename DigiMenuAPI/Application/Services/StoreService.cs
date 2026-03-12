using AppCore.Application.Common;
using AppCore.Application.Utils;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly LogMessageDispatcher<StoreService> _logger;

        // AutoMapper eliminado: MenuBranchDto se construye manualmente
        // desde 4 entidades distintas — no hay un mapeo 1:1 posible.
        public StoreService(
            ApplicationDbContext context,
            ITenantService tenantService,
            LogMessageDispatcher<StoreService> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
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

                // 2. Leer las 4 entidades de configuración en paralelo.
                //    BranchSeo es opcional — el menú funciona sin SEO configurado.
                var infoTask = _context.BranchInfos.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.BranchId == branchId.Value);
                var themeTask = _context.BranchThemes.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.BranchId == branchId.Value);
                var localeTask = _context.BranchLocales.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BranchId == branchId.Value);
                var seoTask = _context.BranchSeos.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BranchId == branchId.Value);

                await Task.WhenAll(infoTask, themeTask, localeTask, seoTask);

                var info = await infoTask;
                var theme = await themeTask;
                var locale = await localeTask;
                var seo = await seoTask;

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

                // 6. Construir CategoryMenuDto con traducción aplicada y fallback al base
                var categoryDtos = categories.Select(cat =>
                {
                    var catName = cat.Translations
                        .FirstOrDefault(t => t.LanguageCode == lang)?.Name
                        ?? cat.Name;

                    var products = branchProducts
                        .Where(bp => bp.CategoryId == cat.Id)
                        .Select(bp =>
                        {
                            var prodName = bp.Product.Translations
                                .FirstOrDefault(t => t.LanguageCode == lang)?.Name
                                ?? bp.Product.Name;

                            var prodShortDesc = bp.Product.Translations
                                .FirstOrDefault(t => t.LanguageCode == lang)?.ShortDescription
                                ?? bp.Product.ShortDescription;

                            var tags = bp.Product.Tags
                                .Select(t => new TagMenuDto(
                                    t.Id,
                                    t.Translations
                                        .FirstOrDefault(tr => tr.LanguageCode == lang)?.Name
                                        ?? t.Name,
                                    t.Color))
                                .ToList();

                            return new BranchProductMenuDto(
                                bp.Id,
                                bp.ProductId,
                                prodName,
                                prodShortDesc,
                                bp.ImageOverrideUrl ?? bp.Product.MainImageUrl,
                                bp.Price,
                                bp.OfferPrice,
                                bp.DisplayOrder,
                                tags);
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

                // 9. Días especiales próximos — desde hoy, máximo 30 días
                var today = DateTime.UtcNow.Date;
                var upcomingSpecialDays = await _context.BranchSpecialDays
                    .AsNoTracking()
                    .Where(d =>
                        d.BranchId == branchId.Value &&
                        d.Date >= today &&
                        d.Date <= today.AddDays(30))
                    .OrderBy(d => d.Date)
                    .Select(d => new BranchSpecialDayReadDto(
                        d.Id,
                        d.Date,
                        d.IsClosed,
                        d.OpenTime,
                        d.CloseTime,
                        d.Reason,
                        d.CreatedAt))
                    .ToListAsync();

                // 10. Construir MenuBranchDto manualmente desde las 4 entidades.
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
                    theme.ShowSearchButton,
                    theme.ShowContactButton,
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
                    // Contenido dinámico
                    categoryDtos,
                    footerLinks,
                    //Horarios
                    WeeklySchedule: weeklySchedule.Any() ? weeklySchedule : null,
                    UpcomingSpecialDays: upcomingSpecialDays.Any() ? upcomingSpecialDays : null
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