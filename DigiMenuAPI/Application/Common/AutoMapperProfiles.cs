using AutoMapper;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.Application.Common
{
    public class AutoMapperProfiles: Profile
    {
        public AutoMapperProfiles()
        {
            CategoryMappingConfiguration();
            SubcategoryMappingConfiguration();
            ProductMappingConfiguration();
        }

        private void ProductMappingConfiguration()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.ImagePath)
            );

            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Image)
            );

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Image)
            )
           .ReverseMap();

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
    }
}
