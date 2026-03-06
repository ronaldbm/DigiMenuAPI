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

        public async Task<OperationResult<MenuBranchDto>> GetStoreMenu(string slug)
        {
            try
            {
                // 1. Resolver Branch por slug — el slug del menú público es de Branch, no de Company
                //    IgnoreQueryFilters porque IsActive es el control, no IsDeleted
                var branch = await _context.Branches
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.Slug == slug.ToLower().Trim() && b.IsActive && !b.IsDeleted)
                    .Select(b => new { b.Id, b.CompanyId })
                    .FirstOrDefaultAsync();

                if (branch is null)
                    return OperationResult<MenuBranchDto>.Fail("Menú no encontrado.");

                int branchId = branch.Id;
                int companyId = branch.CompanyId;

                // 2. Verificar que la Company está activa
                var companyActive = await _context.Companies
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == companyId && c.IsActive);

                if (!companyActive)
                    return OperationResult<MenuBranchDto>.Fail("Menú no disponible.");

                // 3. Configuración visual de la Branch (Setting es 1:1 con Branch via BranchId)
                var setting = await _context.Settings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BranchId == branchId);

                if (setting is null)
                    return OperationResult<MenuBranchDto>.Fail("Configuración del menú no disponible.");

                string lang = setting.Language;

                // 4. Categorías activas del catálogo global de la empresa
                //    IgnoreQueryFilters para controlar manualmente IsDeleted + IsVisible
                var categoryIds = await _context.BranchProducts
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(bp => bp.BranchId == branchId && bp.IsVisible && !bp.IsDeleted)
                    .Select(bp => bp.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var categories = await _context.Categories
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(c => categoryIds.Contains(c.Id)
                             && c.CompanyId == companyId
                             && c.IsVisible
                             && !c.IsDeleted)
                    .Include(c => c.Translations)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                // 5. BranchProducts visibles de esta Branch con sus productos y tags
                var branchProducts = await _context.BranchProducts
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(bp => bp.BranchId == branchId && bp.IsVisible && !bp.IsDeleted)
                    .Include(bp => bp.Product)
                        .ThenInclude(p => p.Translations)
                    .Include(bp => bp.Product)
                        .ThenInclude(p => p.Tags)
                            .ThenInclude(t => t.Translations)
                    .OrderBy(bp => bp.DisplayOrder)
                    .ToListAsync();

                // 6. Construir CategoryMenuDto con traducción aplicada
                var categoryDtos = categories.Select(cat =>
                {
                    // Nombre con fallback: idioma solicitado → base
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

                            var tags = bp.Product.Tags.Select(t => new TagMenuDto(
                                t.Id,
                                t.Translations.FirstOrDefault(tr => tr.LanguageCode == lang)?.Name ?? t.Name,
                                t.Color
                            )).ToList();

                            return new BranchProductMenuDto(
                                bp.Id,
                                bp.ProductId,
                                prodName,
                                prodShortDesc,
                                bp.ImageOverrideUrl ?? bp.Product.MainImageUrl,
                                bp.Price,
                                bp.OfferPrice,
                                bp.DisplayOrder,
                                tags
                            );
                        })
                        .ToList();

                    return new CategoryMenuDto(cat.Id, catName, cat.DisplayOrder, products);
                }).ToList();

                // 7. FooterLinks de la Branch — QueryFilter ya aplica !IsDeleted
                var footerLinks = await _context.FooterLinks
                    .AsNoTracking()
                    .Where(f => f.BranchId == branchId)
                    .Include(f => f.StandardIcon)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new FooterLinkReadDto(
                        f.Id,
                        f.Label,
                        f.Url,
                        f.StandardIcon != null ? f.StandardIcon.SvgContent : (f.CustomSvgContent ?? ""),
                        f.DisplayOrder))
                    .ToListAsync();

                // 8. Módulos activos de la empresa
                var activeModules = await _context.CompanyModules
                    .AsNoTracking()
                    .Where(cm =>
                        cm.CompanyId == companyId &&
                        cm.IsActive &&
                        (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow))
                    .Select(cm => cm.PlatformModule.Code)
                    .ToListAsync();

                // 9. Mapear Setting a MenuBranchDto e inyectar contenido dinámico
                var menuDto = _mapper.Map<MenuBranchDto>(setting) with
                {
                    Categories = categoryDtos,
                    FooterLinks = footerLinks
                };

                return OperationResult<MenuBranchDto>.Ok(menuDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el menú de la sucursal");
                return OperationResult<MenuBranchDto>.Fail("Error inesperado al cargar el menú.");
            }
        }
    }
}