using AutoMapper;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.Entities.Views;

namespace DigiMenuAPI.Application.Common
{
    public class AutoMapperProfiles: Profile
    {
        public AutoMapperProfiles()
        {
            CategoryMappingConfiguration();
            SubcategoryMappingConfiguration();
            ProductMappingConfiguration();
            SocialLinkMappingConfiguration();
        }

        private void ProductMappingConfiguration()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.ImagePath))
            ;

            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Image))
            ;

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Image))
            .ReverseMap();

            CreateMap<vwProductVisibleList, MenuProductsDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.ProductLabel))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.ProductPrice)) 
                .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.ProductImage))
                .ForMember(dest => dest.Subcategory, opt => opt.MapFrom(src => new SubcategoryCategoryDto
                {
                    Id = src.SubcategoryId,
                    Label = src.SubcategoryLabel,
                    Position = src.SubcategoryPosition,
                    Category = new CategoryInfoDto
                    {
                        Id = src.CategoryId,
                        Label = src.CategoryLabel,
                        Position = src.CategoryPosition
                    }
                }))
            ;

        }

        private void CategoryMappingConfiguration()
        {
            CreateMap<Category, CategoryDto>();

            CreateMap<Category,CategoryInfoDto>();

            CreateMap<Category, CategorySelectInformation>()
                .ForMember(dest => dest.Subcategory,
                           opt => opt.MapFrom(src =>
                               src.Subcategories
                                  .Where(s => s.Alive && s.IsVisible)
                                  .OrderBy(s => s.Position)
                           )
            );

            CreateMap<CategoryCreateDto, Category>();

            CreateMap<CategoryUpdateDto, Category>();

        }

        private void SubcategoryMappingConfiguration()
        {
            CreateMap<Subcategory, SubcategoryDto>();

            CreateMap<Subcategory, SubcategoryInfo>();

            CreateMap<SubcategoryCreateDto, Subcategory>();

            CreateMap<SubcategoryUpdateDto, Subcategory>();

        }

        private void SocialLinkMappingConfiguration()
        {
            CreateMap<SocialLink, SocialLinkDto>();

            CreateMap<SocialLinkUpdateDto, SocialLink>();

        }
    }
}
