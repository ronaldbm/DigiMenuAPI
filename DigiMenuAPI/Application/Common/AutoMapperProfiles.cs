using AutoMapper;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.Application.Common
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            StoreMappingConfiguration();
            CategoryMappingConfiguration();
            ProductMappingConfiguration();
            TagMappingConfiguration();
            FooterLinkMappingConfiguration();
            ReservationMappingConfiguration();
        }

        private void StoreMappingConfiguration()
        {
            // Mapeo de la configuración global del menú
            CreateMap<Setting, MenuStoreDto>();
        }

        private void ProductMappingConfiguration()
        {
            // LECTURA: Mapeamos MainImageUrl de la entidad a ImageUrl del Record
            CreateMap<Product, ProductReadDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.MainImageUrl))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>  src.Tags));

            CreateMap<Product, ProductAdminReadDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.MainImageUrl));

            // ESCRITURA:
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.MainImageUrl, opt => opt.Ignore());

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.MainImageUrl, opt => opt.Ignore())
                .ReverseMap();
        }

        private void CategoryMappingConfiguration()
        {
            // LECTURA: Filtrado de productos visibles y ordenados
            CreateMap<Category, CategoryReadDto>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src =>
                    src.Products.Where(p => p.IsVisible)
                                .OrderBy(p => p.DisplayOrder)));

            CreateMap<CategoryCreateDto, Category>();
            CreateMap<CategoryUpdateDto, Category>().ReverseMap();
        }

        private void TagMappingConfiguration()
        {
            CreateMap<Tag, TagReadDto>(); 
            CreateMap<TagCreateDto, Tag>();
            CreateMap<TagUpdateDto, Tag>().ReverseMap();
        }

        private void FooterLinkMappingConfiguration()
        {
            // Nota: Agregué StandardIconReadDto si es que lo usas por separado
            CreateMap<StandardIcon, StandardIconReadDto>();

            CreateMap<FooterLink, FooterLinkReadDto>()
                .ForMember(dest => dest.SvgContent, opt => opt.MapFrom(src =>
                    src.StandardIcon != null ? src.StandardIcon.SvgContent : (src.CustomSvgContent ?? "")));

            CreateMap<FooterLinkCreateDto, FooterLink>();
            CreateMap<FooterLinkUpdateDto, FooterLink>().ReverseMap();
        }

        private void ReservationMappingConfiguration()
        {
            CreateMap<Reservation, ReservationReadDto>();
            CreateMap<ReservationCreateDto, Reservation>();
        }
    }
}