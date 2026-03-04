using AutoMapper;
using DigiMenuAPI.Application.DTOs.Add;
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
            // Todas las propiedades tienen el mismo nombre → sin configuración adicional
            CreateMap<Plan, PlanReadDto>();
            CreateMap<PlanCreateDto, Plan>();
            CreateMap<PlanUpdateDto, Plan>().ReverseMap();
        }

        // ── Company ───────────────────────────────────────────────────
        private void CompanyMappings()
        {
            CreateMap<Company, CompanyReadDto>()
                .ForMember(d => d.PlanName, o => o.MapFrom(s => s.Plan.Name))
                .ForMember(d => d.CurrentBranches, o => o.Ignore())       // calculado en servicio con Count()
                .ForMember(d => d.CurrentUsers, o => o.Ignore())        // calculado en servicio con Count()
                .ForMember(d => d.ActiveModules, o => o.MapFrom(s =>     // solo módulos activos
                    s.CompanyModules.Where(m => m.IsActive).ToList()));

            CreateMap<Company, CompanySummaryDto>()
                .ForMember(d => d.PlanName, o => o.MapFrom(s => s.Plan.Name))
                .ForMember(d => d.CurrentBranches, o => o.Ignore())       // calculado en servicio
                .ForMember(d => d.CurrentUsers, o => o.Ignore());       // calculado en servicio

            CreateMap<CompanyCreateDto, Company>()
                .ForMember(d => d.MaxBranches, o => o.Ignore())  // servicio aplica Plan.MaxBranches si null
                .ForMember(d => d.MaxUsers, o => o.Ignore());  // servicio aplica Plan.MaxUsers si null

            CreateMap<CompanyUpdateDto, Company>().ReverseMap();
        }

        // ── Branch ────────────────────────────────────────────────────
        private void BranchMappings()
        {
            // Todas las propiedades coinciden en nombre
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

            // CategoryMenuDto: el nombre resuelto al idioma se hace en el servicio,
            // no en AutoMapper, porque requiere lógica de fallback.
            CreateMap<Category, CategoryMenuDto>()
                .ForMember(d => d.Products, o => o.Ignore()); // servicio inyecta los BranchProducts

            CreateMap<CategoryCreateDto, Category>();
            CreateMap<CategoryUpdateDto, Category>().ReverseMap();
        }

        // ── Product ───────────────────────────────────────────────────
        private void ProductMappings()
        {
            CreateMap<Product, ProductReadDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags))
                .ForMember(d => d.Translations, o => o.MapFrom(s => s.Translations));

            CreateMap<ProductCreateDto, Product>()
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

            // BranchProductMenuDto: nombre e imagen resueltos en el servicio (fallback de idioma e imagen)
            CreateMap<BranchProduct, BranchProductMenuDto>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.Name, o => o.Ignore())            // servicio aplica traducción
                .ForMember(d => d.ShortDescription, o => o.Ignore()) // servicio aplica traducción
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

            CreateMap<TagCreateDto, Tag>();
            CreateMap<TagUpdateDto, Tag>().ReverseMap();
        }

        // ── Setting ───────────────────────────────────────────────────
        private void SettingMappings()
        {
            // Todas las propiedades tienen el mismo nombre → sin configuración adicional
            CreateMap<Setting, SettingReadDto>();

            // MenuBranchDto: comparte propiedades de nombre con Setting
            CreateMap<Setting, MenuBranchDto>()
                .ForMember(d => d.Categories, o => o.Ignore())   // servicio inyecta
                .ForMember(d => d.FooterLinks, o => o.Ignore()); // servicio inyecta

            CreateMap<SettingUpdateDto, Setting>().ReverseMap();
        }

        // ── FooterLink ────────────────────────────────────────────────
        private void FooterLinkMappings()
        {
            CreateMap<StandardIcon, StandardIconReadDto>();

            CreateMap<FooterLink, FooterLinkReadDto>()
                .ForMember(d => d.SvgContent, o => o.MapFrom(s =>
                    s.StandardIcon != null ? s.StandardIcon.SvgContent : (s.CustomSvgContent ?? "")));

            CreateMap<FooterLinkCreateDto, FooterLink>();
            CreateMap<FooterLinkUpdateDto, FooterLink>().ReverseMap();
        }

        // ── Reservation ───────────────────────────────────────────────
        private void ReservationMappings()
        {
            // Todas las propiedades tienen el mismo nombre → sin configuración adicional
            CreateMap<Reservation, ReservationReadDto>();
            CreateMap<ReservationCreateDto, Reservation>();
        }

        // ── Translations ──────────────────────────────────────────────
        private void TranslationMappings()
        {
            // CategoryTranslation → TranslationReadDto (comparten Id, LanguageCode, Name)
            CreateMap<CategoryTranslation, TranslationReadDto>();
            CreateMap<CategoryTranslationCreateDto, CategoryTranslation>();
            CreateMap<CategoryTranslationUpdateDto, CategoryTranslation>().ReverseMap();

            // ProductTranslation → ProductTranslationReadDto (mismos nombres)
            CreateMap<ProductTranslation, ProductTranslationReadDto>();
            CreateMap<ProductTranslationCreateDto, ProductTranslation>();
            CreateMap<ProductTranslationUpdateDto, ProductTranslation>().ReverseMap();

            // TagTranslation → TranslationReadDto (comparten Id, LanguageCode, Name)
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
                .ForMember(d => d.ActivatedAt, o => o.Ignore())          // servicio asigna DateTime.UtcNow
                .ForMember(d => d.ActivatedByUserId, o => o.Ignore());   // servicio toma del JWT

            CreateMap<CompanyModuleUpdateDto, CompanyModule>().ReverseMap();
        }
    }
}