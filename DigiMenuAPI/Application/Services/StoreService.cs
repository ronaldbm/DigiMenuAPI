using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using static DigiMenuAPI.Application.Common.Constants;

namespace DigiMenuAPI.Application.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly LogMessageDispatcher<StoreService> _logger;

        public StoreService(
            ApplicationDbContext context,
            IMapper mapper,
            LogMessageDispatcher<StoreService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<MenuStoreDto>> GetStoreMenu(string slug)
        {
            try
            {
                // 1. Resolver empresa por slug (endpoint público — sin JWT)
                var company = await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Slug == slug.ToLower().Trim() && c.IsActive);

                if (company is null)
                    return OperationResult<MenuStoreDto>.Fail("Menú no encontrado.");

                int companyId = company.Id;

                // 2. Configuración del negocio
                var settings = await _context.Settings
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.CompanyId == companyId);

                if (settings is null)
                    return OperationResult<MenuStoreDto>.Fail("Configuración del negocio no disponible.");

                // 3. Categorías y productos visibles del tenant
                var categories = await _context.Categories
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(c => !c.IsDeleted && c.IsVisible && c.CompanyId == companyId)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new CategoryReadDto(
                        c.Id,
                        c.Name,
                        c.DisplayOrder,
                        c.Products
                            .Where(p => !p.IsDeleted && p.IsVisible)
                            .OrderBy(p => p.DisplayOrder)
                            .Select(p => new ProductReadDto(
                                p.Id, p.Name,
                                p.ShortDescription ?? "",
                                p.BasePrice, p.OfferPrice ?? 0,
                                p.MainImageUrl ?? "",
                                p.Tags.Select(t => new TagReadDto(t.Id, t.Name, t.Color)).ToList()
                            )).ToList()
                    ))
                    .ToListAsync();

                // 4. Footer links del tenant
                var footerLinks = await _context.FooterLinks
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(f => !f.IsDeleted && f.IsVisible && f.CompanyId == companyId)
                    .Include(f => f.StandardIcon)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new FooterLinkReadDto(
                        f.Id, f.Label, f.Url,
                        f.StandardIcon != null ? f.StandardIcon.SvgContent : (f.CustomSvgContent ?? ""),
                        f.DisplayOrder))
                    .ToListAsync();

                // 5. Módulos activos (para que el frontend sepa qué mostrar)
                var activeModules = await _context.CompanyModules
                    .AsNoTracking()
                    .Where(cm =>
                        cm.CompanyId == companyId &&
                        cm.IsActive &&
                        (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow))
                    .Select(cm => cm.PlatformModule.Code)
                    .ToListAsync();

                // 6. Construir DTO final
                var storeMenu = new MenuStoreDto(
                    BusinessName: settings.BusinessName,
                    Tagline: settings.Tagline,
                    LogoUrl: settings.LogoUrl,
                    FaviconUrl: settings.FaviconUrl,
                    BackgroundImageUrl: settings.BackgroundImageUrl,
                    IsDarkMode: settings.IsDarkMode,
                    PageBackgroundColor: settings.PageBackgroundColor,
                    HeaderBackgroundColor: settings.HeaderBackgroundColor,
                    HeaderTextColor: settings.HeaderTextColor,
                    TabBackgroundColor: settings.TabBackgroundColor,
                    TabTextColor: settings.TabTextColor,
                    PrimaryColor: settings.PrimaryColor,
                    PrimaryTextColor: settings.PrimaryTextColor,
                    SecondaryColor: settings.SecondaryColor,
                    TitlesColor: settings.TitlesColor,
                    TextColor: settings.TextColor,
                    BrowserThemeColor: settings.BrowserThemeColor,
                    HeaderStyle: settings.HeaderStyle,
                    MenuLayout: settings.MenuLayout,
                    ProductDisplay: settings.ProductDisplay,
                    ShowProductDetails: settings.ShowProductDetails,
                    ShowSearchButton: settings.ShowSearchButton,
                    ShowContactButton: settings.ShowContactButton,
                    CountryCode: settings.CountryCode,
                    Currency: settings.Currency,
                    CurrencyLocale: settings.CurrencyLocale,
                    Language: settings.Language,
                    Decimals: settings.Decimals,
                    MetaTitle: settings.MetaTitle,
                    MetaDescription: settings.MetaDescription,
                    GoogleAnalyticsId: settings.GoogleAnalyticsId,
                    FacebookPixelId: settings.FacebookPixelId,
                    Categories: categories,
                    FooterLinks: footerLinks,
                    ActiveModules: activeModules
                );

                return OperationResult<MenuStoreDto>.Ok(storeMenu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el menú de la tienda");
                return OperationResult<MenuStoreDto>.Fail("Error inesperado al cargar el menú.");
            }
        }
    }
}