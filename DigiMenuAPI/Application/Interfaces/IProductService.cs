using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IProductService
    {
        Task<OperationResult<int>> Create(ProductCreateDto productDto);
        Task<OperationResult<bool>> Update(ProductUpdateDto productDto);
        Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate productDto);
        Task<OperationResult<bool>> Delete(int Id);
        Task<List<ProductDto>> GetAll();
        Task<ProductUpdateDto?> GetOne(int productId);
        Task<List<MenuProductsDto>> GetMenu();
    }
}
