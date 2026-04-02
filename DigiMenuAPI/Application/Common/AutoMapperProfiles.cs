using AppCore.Domain.Entities;
using AutoMapper;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Infrastructure.Entities;
using NetTopologySuite.Geometries;

namespace DigiMenuAPI.Application.Common
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            PlanMappings();
            CompanyMappings();
            BranchMappings();
            AppUserMappings();
            CategoryMappings();
            ProductMappings();
            BranchProductMappings();
            TagMappings();
            SettingMappings();
            FooterLinkMappings();
            ReservationMappings();
            TranslationMappings();
            ModuleMappings();
        }

        // ── Plan ─────────────────────────────────────────────────────
        private void PlanMappings()
        {
            CreateMap<Plan, PlanReadDto>();
            CreateMap<PlanCreateDto, Plan>();
            CreateMap<PlanUpdateDto, Plan>().ReverseMap();
        }

        // ── Company ───────────────────────────────────────────────────
        private void CompanyMappings()
        {
            CreateMap<Company, CompanyReadDto>()
                .ForMember(d => d.PlanName, o => o.MapFrom(s => s.Plan.Name))
                .ForMember(d => d.CurrentBranches, o => o.Ignore())   // calculado con Count() en servicio
                .ForMember(d => d.CurrentUsers, o => o.Ignore())   // calculado con Count() en servicio
                .ForMember(d => d.ActiveModules, o => o.MapFrom(s =>
                    s.CompanyModules.Where(m => m.IsActive).ToList()));

            CreateMap<Company, CompanySummaryDto>()
                .ForMember(d => d.PlanName, o => o.MapFrom(s => s.Plan.Name))
                .ForMember(d => d.CurrentBranches, o => o.Ignore())
                .ForMember(d => d.CurrentUsers, o => o.Ignore());

            CreateMap<CompanyCreateDto, Company>()
                .ForMember(d => d.MaxBranches, o => o.Ignore())  // servicio aplica Plan.MaxBranches
                .ForMember(d => d.MaxUsers, o => o.Ignore()); // servicio aplica Plan.MaxUsers

            CreateMap<CompanyUpdateDto, Company>().ReverseMap();
        }

        // ── Branch ────────────────────────────────────────────────────
        private void BranchMappings()
        {
            // BranchReadDto is a positional record — use ConstructUsing to avoid
            // AutoMapper trying to resolve Latitude/Longitude via property convention.
            CreateMap<Branch, BranchReadDto>()
                .ConstructUsing(s => new BranchReadDto(
                    s.Id, s.CompanyId, s.Name, s.Slug,
                    s.Address, s.Phone, s.Email, s.IsActive,
                    s.CreatedAt, s.ModifiedAt,
                    s.Location != null ? (decimal?)s.Location.Y : null,
                    s.Location != null ? (decimal?)s.Location.X : null));

            CreateMap<Branch, BranchSummaryDto>();

            // BranchService.Create / Update build Branch manually and set Location
            // directly — these maps exist for completeness but AutoMapper is not
            // the path that writes Location to the entity.
            CreateMap<BranchCreateDto, Branch>()
                .ForMember(d => d.Location, o => o.Ignore());

            CreateMap<BranchUpdateDto, Branch>()
                .ForMember(d => d.Location, o => o.Ignore());
        }

        // ── AppUser ───────────────────────────────────────────────────
        private void AppUserMappings()
        {
            CreateMap<AppUser, AppUserReadDto>()
                .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.Name))
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.Name : null));

            CreateMap<AppUser, AppUserSummaryDto>()
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
                .ForMember(d => d.BranchId, o => o.MapFrom(s => s.BranchId));

            CreateMap<AppUserCreateDto, AppUser>()
                .ForMember(d => d.PasswordHash, o => o.Ignore()); // servicio aplica BCrypt

            CreateMap<AppUserUpdateDto, AppUser>().ReverseMap();
        }

        // ── Category ──────────────────────────────────────────────────
        private void CategoryMappings()
        {
            // Name ya no existe en la entidad — vive en CategoryTranslation
            CreateMap<Category, CategoryReadDto>()
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // CategoryMenuDto: nombre resuelto al idioma en el servicio (fallback al base)
            CreateMap<Category, CategoryMenuDto>()
                .ForMember(d => d.Name,                o => o.Ignore())      // servicio resuelve traducción
                .ForMember(d => d.HeaderImageUrl,      o => o.MapFrom(s => s.HeaderImageUrl))
                .ForMember(d => d.HeaderStyleOverride, o => o.MapFrom(s => s.HeaderStyleOverride))
                .ForMember(d => d.Products,            o => o.Ignore()); // servicio inyecta BranchProducts

            // CompanyId NO viene del DTO — servicio lo inyecta desde JWT
            // Translations se manejan manualmente en el servicio (transacción + replace-all)
            CreateMap<CategoryCreateDto, Category>()
                .ForMember(d => d.CompanyId,    o => o.Ignore())
                .ForMember(d => d.DisplayOrder, o => o.Ignore()) // servicio asigna max+1
                .ForMember(d => d.Translations, o => o.Ignore());

            CreateMap<CategoryUpdateDto, Category>()
                .ForMember(d => d.CompanyId,    o => o.Ignore())
                .ForMember(d => d.DisplayOrder, o => o.Ignore()) // endpoint /reorder lo gestiona
                .ForMember(d => d.Translations, o => o.Ignore())
                .ReverseMap();
        }

        // ── Product ───────────────────────────────────────────────────
        private void ProductMappings()
        {
            // Name/ShortDescription/LongDescription ya no existen en la entidad —
            // viven en ProductTranslation. CategoryName se resuelve desde la primera
            // traducción disponible de la Category relacionada.
            CreateMap<Product, ProductSummaryDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s =>
                    s.Category.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty));

            CreateMap<Product, ProductReadDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s =>
                    s.Category.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags))
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // ProductAdminReadDto: igual pero incluye ModifiedAt desde BaseEntity
            CreateMap<Product, ProductAdminReadDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s =>
                    s.Category.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags))
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // CompanyId NO viene del DTO — servicio lo inyecta desde JWT
            // Translations se manejan manualmente en el servicio (transacción + replace-all)
            // ImageObjectFit/Position se aplican explícitamente en el servicio (permite null=sin cambio)
            CreateMap<ProductCreateDto, Product>()
                .ForMember(d => d.CompanyId,          o => o.Ignore())
                .ForMember(d => d.MainImageUrl,        o => o.Ignore())
                .ForMember(d => d.Translations,        o => o.Ignore())
                .ForMember(d => d.ImageObjectFit,      o => o.Ignore())
                .ForMember(d => d.ImageObjectPosition, o => o.Ignore());

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(d => d.MainImageUrl,        o => o.Ignore())
                .ForMember(d => d.Translations,        o => o.Ignore())
                .ForMember(d => d.ImageObjectFit,      o => o.Ignore())
                .ForMember(d => d.ImageObjectPosition, o => o.Ignore())
                .ReverseMap();
        }

        // ── BranchProduct ─────────────────────────────────────────────
        private void BranchProductMappings()
        {
            // ProductName y CategoryName se resuelven desde la primera traducción disponible.
            CreateMap<BranchProduct, BranchProductReadDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s =>
                    s.Product.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s =>
                    s.Product.Category.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty))
                .ForMember(d => d.BaseImageUrl, o => o.MapFrom(s => s.Product.MainImageUrl));

            // BranchProductMenuDto: nombre e imagen resueltos en el servicio
            CreateMap<BranchProduct, BranchProductMenuDto>()
                .ForMember(d => d.ProductId,           o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name,                o => o.Ignore())  // servicio aplica traducción
                .ForMember(d => d.ShortDescription,    o => o.Ignore())  // servicio aplica traducción
                .ForMember(d => d.ImageUrl,            o => o.MapFrom(s =>
                    s.ImageOverrideUrl ?? s.Product.MainImageUrl))
                .ForMember(d => d.Tags,                o => o.MapFrom(s => s.Product.Tags))
                .ForMember(d => d.ImageObjectFit,      o => o.MapFrom(s =>
                    s.ImageOverrideUrl != null ? s.ImageObjectFit : s.Product.ImageObjectFit))
                .ForMember(d => d.ImageObjectPosition, o => o.MapFrom(s =>
                    s.ImageOverrideUrl != null ? s.ImageObjectPosition : s.Product.ImageObjectPosition));

            CreateMap<BranchProductCreateDto, BranchProduct>()
                .ForMember(d => d.ImageOverrideUrl,    o => o.Ignore())  // servicio sube imagen
                .ForMember(d => d.ImageObjectFit,      o => o.Ignore())  // servicio aplica con ?? "cover"
                .ForMember(d => d.ImageObjectPosition, o => o.Ignore());

            CreateMap<BranchProductUpdateDto, BranchProduct>()
                .ForMember(d => d.ImageOverrideUrl,    o => o.Ignore())  // servicio sube imagen
                .ForMember(d => d.ImageObjectFit,      o => o.Ignore())  // servicio aplica condicionalmente
                .ForMember(d => d.ImageObjectPosition, o => o.Ignore())
                .ReverseMap();
        }

        // ── Tag ───────────────────────────────────────────────────────
        private void TagMappings()
        {
            CreateMap<Tag, TagReadDto>()
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // TagMenuDto: nombre resuelto al idioma en el servicio
            CreateMap<Tag, TagMenuDto>()
                .ForMember(d => d.Name, o => o.Ignore()); // servicio aplica traducción

            // CompanyId NO viene del DTO — servicio lo inyecta desde JWT
            // Translations se manejan manualmente en el servicio (transacción + replace-all)
            CreateMap<TagCreateDto, Tag>()
                .ForMember(d => d.CompanyId, o => o.Ignore())
                .ForMember(d => d.Translations, o => o.Ignore());

            CreateMap<TagUpdateDto, Tag>()
                .ForMember(d => d.CompanyId, o => o.Ignore())
                .ForMember(d => d.Translations, o => o.Ignore())
                .ReverseMap();
        }

        // ── Settings ──────────────────────────────────────────────────
        private void SettingMappings()
        {
            // CompanyInfo
            CreateMap<CompanyInfo, CompanyInfoReadDto>();
            CreateMap<CompanyInfoUpdateDto, CompanyInfo>()
                .ForMember(d => d.LogoUrl, o => o.Ignore())           // servicio sube imagen
                .ForMember(d => d.FaviconUrl, o => o.Ignore())        // servicio sube imagen
                .ForMember(d => d.BackgroundImageUrl, o => o.Ignore()); // servicio sube imagen

            // CompanyTheme — JSON owned types require explicit nested mapping
            CreateMap<ColorPaletteData, ColorPaletteDto>();
            CreateMap<BackgroundSettingsData, BackgroundSettingsDto>();
            CreateMap<FrameSettingsData, FrameSettingsDto>();

            CreateMap<ColorPaletteUpdateDto, ColorPaletteData>().ReverseMap();
            CreateMap<BackgroundSettingsUpdateDto, BackgroundSettingsData>().ReverseMap();
            CreateMap<FrameSettingsUpdateDto, FrameSettingsData>().ReverseMap();

            CreateMap<CompanyTheme, CompanyThemeReadDto>()
                .ForMember(d => d.ColorPalette,        o => o.MapFrom(s => s.ColorPalette))
                .ForMember(d => d.DarkModePalette,     o => o.MapFrom(s => s.DarkModePalette))
                .ForMember(d => d.BackgroundSettings,  o => o.MapFrom(s => s.BackgroundSettings))
                .ForMember(d => d.FrameSettings,       o => o.MapFrom(s => s.FrameSettings));

            CreateMap<CompanyThemeUpdateDto, CompanyTheme>()
                .ForMember(d => d.ColorPalette,        o => o.MapFrom(s => s.ColorPalette))
                .ForMember(d => d.DarkModePalette,     o => o.MapFrom(s => s.DarkModePalette))
                .ForMember(d => d.BackgroundSettings,  o => o.MapFrom(s => s.BackgroundSettings))
                .ForMember(d => d.FrameSettings,       o => o.MapFrom(s => s.FrameSettings))
                .ReverseMap();

            // CompanySeo
            CreateMap<CompanySeo, CompanySeoReadDto>();
            CreateMap<CompanySeoUpdateDto, CompanySeo>().ReverseMap();

            // BranchLocale
            CreateMap<BranchLocale, BranchLocaleReadDto>();
            CreateMap<BranchLocaleUpdateDto, BranchLocale>().ReverseMap();

            // BranchReservationForm
            CreateMap<BranchReservationForm, BranchReservationFormReadDto>();
            CreateMap<BranchReservationFormUpdateDto, BranchReservationForm>().ReverseMap();

            // MenuBranchDto: se construye manualmente en StoreService desde CompanyInfo,
            // CompanyTheme, CompanySeo y BranchLocale — no hay un AutoMapper directo
            // porque el origen es multi-entidad y el destino es un DTO plano compuesto.
        }

        // ── FooterLink ────────────────────────────────────────────────
        private void FooterLinkMappings()
        {
            CreateMap<StandardIcon, StandardIconReadDto>();

            CreateMap<FooterLink, FooterLinkReadDto>()
                .ForMember(d => d.SvgContent, o => o.MapFrom(s =>
                    s.StandardIcon != null
                        ? s.StandardIcon.SvgContent
                        : (s.CustomSvgContent ?? "")));

            CreateMap<FooterLinkCreateDto, FooterLink>();
            CreateMap<FooterLinkUpdateDto, FooterLink>().ReverseMap();
        }

        // ── Reservation ───────────────────────────────────────────────
        private void ReservationMappings()
        {
            CreateMap<Reservation, ReservationReadDto>();
            CreateMap<ReservationCreateDto
                , Reservation>();
        }

        // ── Translations ──────────────────────────────────────────────
        private void TranslationMappings()
        {
            // CategoryTranslation → TranslationReadDto
            CreateMap<CategoryTranslation, TranslationReadDto>();
            CreateMap<CategoryTranslationCreateDto, CategoryTranslation>();
            CreateMap<CategoryTranslationUpdateDto, CategoryTranslation>().ReverseMap();

            // ProductTranslation → ProductTranslationReadDto (incluye descripciones)
            CreateMap<ProductTranslation, ProductTranslationReadDto>();
            CreateMap<ProductTranslationCreateDto, ProductTranslation>();
            CreateMap<ProductTranslationUpdateDto, ProductTranslation>().ReverseMap();

            // TagTranslation → TranslationReadDto
            CreateMap<TagTranslation, TranslationReadDto>();
            CreateMap<TagTranslationCreateDto, TagTranslation>();
            CreateMap<TagTranslationUpdateDto, TagTranslation>().ReverseMap();
        }

        // ── Modules ───────────────────────────────────────────────────
        private void ModuleMappings()
        {
            CreateMap<PlatformModule, PlatformModuleReadDto>();

            CreateMap<CompanyModule, CompanyModuleReadDto>()
                .ForMember(d => d.ModuleName, o => o.MapFrom(s => s.PlatformModule.Name))
                .ForMember(d => d.ModuleCode, o => o.MapFrom(s => s.PlatformModule.Code));

            CreateMap<CompanyModuleCreateDto, CompanyModule>()
                .ForMember(d => d.ActivatedAt, o => o.Ignore()) // servicio asigna DateTime.UtcNow
                .ForMember(d => d.ActivatedByUserId, o => o.Ignore()); // servicio toma del JWT

            CreateMap<CompanyModuleUpdateDto, CompanyModule>().ReverseMap();
        }
    }
}