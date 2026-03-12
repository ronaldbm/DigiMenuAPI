using AppCore.Domain.Entities;
using AutoMapper;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Infrastructure.Entities;

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
            CreateMap<Branch, BranchReadDto>();
            CreateMap<Branch, BranchSummaryDto>();
            CreateMap<BranchCreateDto, Branch>();
            CreateMap<BranchUpdateDto, Branch>().ReverseMap();
        }

        // ── AppUser ───────────────────────────────────────────────────
        private void AppUserMappings()
        {
            CreateMap<AppUser, AppUserReadDto>()
                .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company.Name))
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.Name : null));

            CreateMap<AppUser, AppUserSummaryDto>()
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.Name : null));

            CreateMap<AppUserCreateDto, AppUser>()
                .ForMember(d => d.PasswordHash, o => o.Ignore()); // servicio aplica BCrypt

            CreateMap<AppUserUpdateDto, AppUser>().ReverseMap();
        }

        // ── Category ──────────────────────────────────────────────────
        private void CategoryMappings()
        {
            CreateMap<Category, CategoryReadDto>()
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // CategoryMenuDto: nombre resuelto al idioma en el servicio (fallback al base)
            CreateMap<Category, CategoryMenuDto>()
                .ForMember(d => d.Products, o => o.Ignore()); // servicio inyecta BranchProducts

            // CompanyId NO viene del DTO — servicio lo inyecta desde JWT
            CreateMap<CategoryCreateDto, Category>()
                .ForMember(d => d.CompanyId, o => o.Ignore());

            CreateMap<CategoryUpdateDto, Category>().ReverseMap();
        }

        // ── Product ───────────────────────────────────────────────────
        private void ProductMappings()
        {
            CreateMap<Product, ProductReadDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags))
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // ProductAdminReadDto: igual pero incluye ModifiedAt desde BaseEntity
            CreateMap<Product, ProductAdminReadDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags))
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            // CompanyId NO viene del DTO — servicio lo inyecta desde JWT
            CreateMap<ProductCreateDto, Product>()
                .ForMember(d => d.CompanyId, o => o.Ignore())
                .ForMember(d => d.MainImageUrl, o => o.Ignore()); // servicio sube imagen

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(d => d.MainImageUrl, o => o.Ignore()) // servicio sube imagen
                .ReverseMap();
        }

        // ── BranchProduct ─────────────────────────────────────────────
        private void BranchProductMappings()
        {
            CreateMap<BranchProduct, BranchProductReadDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.BaseImageUrl, o => o.MapFrom(s => s.Product.MainImageUrl));

            // BranchProductMenuDto: nombre e imagen resueltos en el servicio
            CreateMap<BranchProduct, BranchProductMenuDto>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name, o => o.Ignore())  // servicio aplica traducción
                .ForMember(d => d.ShortDescription, o => o.Ignore())  // servicio aplica traducción
                .ForMember(d => d.ImageUrl, o => o.MapFrom(s =>
                    s.ImageOverrideUrl ?? s.Product.MainImageUrl))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Product.Tags));

            CreateMap<BranchProductCreateDto, BranchProduct>()
                .ForMember(d => d.ImageOverrideUrl, o => o.Ignore()); // servicio sube imagen

            CreateMap<BranchProductUpdateDto, BranchProduct>()
                .ForMember(d => d.ImageOverrideUrl, o => o.Ignore()) // servicio sube imagen
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
            CreateMap<TagCreateDto, Tag>()
                .ForMember(d => d.CompanyId, o => o.Ignore());

            CreateMap<TagUpdateDto, Tag>().ReverseMap();
        }

        // ── BranchSettings ────────────────────────────────────────────────────────
        private void SettingMappings()
        {
            // BranchInfo
            CreateMap<BranchInfo, BranchInfoReadDto>();
            CreateMap<BranchInfoUpdateDto, BranchInfo>()
                .ForMember(d => d.LogoUrl, o => o.Ignore())           // servicio sube imagen
                .ForMember(d => d.FaviconUrl, o => o.Ignore())        // servicio sube imagen
                .ForMember(d => d.BackgroundImageUrl, o => o.Ignore()); // servicio sube imagen

            // BranchTheme
            CreateMap<BranchTheme, BranchThemeReadDto>();
            CreateMap<BranchThemeUpdateDto, BranchTheme>().ReverseMap();

            // BranchLocale
            CreateMap<BranchLocale, BranchLocaleReadDto>();
            CreateMap<BranchLocaleUpdateDto, BranchLocale>().ReverseMap();

            // BranchSeo
            CreateMap<BranchSeo, BranchSeoReadDto>();
            CreateMap<BranchSeoUpdateDto, BranchSeo>().ReverseMap();

            // BranchReservationForm
            CreateMap<BranchReservationForm, BranchReservationFormReadDto>();
            CreateMap<BranchReservationFormUpdateDto, BranchReservationForm>().ReverseMap();

            // MenuBranchDto: se construye desde BranchInfo + BranchTheme + BranchLocale + BranchSeo
            // La construcción se hace manualmente en StoreService — no hay un AutoMapper directo
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